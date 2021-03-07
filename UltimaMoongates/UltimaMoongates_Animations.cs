/* UltimaMoongates_Animations - Moongates really -can- rise from the ground and sink gracefully!
 * 
 * Author: Warped Dragon aka Dallen Wilson <dwjwilson@lavabit.com>
 *
 * This file contains a set of base classes for moongate managers, animation frames and timers. It also contains a set of basic, usable moongates of every colour available in the game files (Blue, Red, Black, Silver).
 *
 * Usage:
 * Add a moongate with '[add UltimaMoongate_[Blue/Red/Black/Silver] [double], where double is the amount of time in real-world seconds the gate should stay open.
 * The gate will rise gracefully from the ground, wait it's alloted open time, then slowly sink again. A time of 0 means the gate stays open indefinitely.
 * Target, Map, and other settings can be adjusted with '[props', or specified when creating the gate, see UltimaMoongate_Base for details.
 *
 * ItemID reference:
 * (gleaned directly from the game files with a verdata viewer)
 * (The first ID in each set causes the client to run through the entire animation sequence on it's own)
 *
 *			Blue	Red		Black	Silver
 *	Rising:	1AF3	1AE5	1FCB	1FDE
 *			1AF4	1AE6	1FCC	1FDF
 *			1AF5	1AE7	1FCD	1FE0
 *			1AF6	1AE8	1FCE	1FE1
 *			1AF7	1AE9	1FCF	1FE2
 *			1AF8	1AEA	1FD0	1FE3
 *			1AF9	1AEB	1FD1	1FE4
 *			1AFA	1AEC	1FD2	1FE5
 *			1AFB	1AED	1FD3	1FE6
 *
 *	Open:	0F6C	0DDA	1FD4	1FE7
 *			0F6D	0DDB	1FD5	1FE8
 *			0F6E	0DDC	1FD6	1FE9
 *			0F6F	0DDD	1FD7	1FEA
 *			0F70	0DDE	1FD8	1FEB
 */

using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Gumps;
using Server.Network;

namespace Warped.Items
{
	public enum MoongateColour
	{
		Blue,
		Red,
		Black,
		Silver
	}

	public enum MoongateFrame
	{
		Frame0,
		Frame1,
		Frame2,
		Frame3,
		Frame4,
		Frame5,
		Frame6,
		Frame7,
		Frame8,
		Frame9
	}
	
	// This is the manager for the moongate. It handles everything; Creation of the animations, teleporting, targeting, etc.
	// Some of it was lifted from Items/Skill Items/Magical/Misc/Moongate.cs and Items/Misc/PublicMoongate.cs
	public class UltimaMoongate : Item
	{
		private bool m_gateOpened = false;
		public UltimaMoongate_Frame currentGate = null;

		private double m_openTime = 0;
		private bool m_useStockRestrictions = false;
		private Point3D m_Target = Point3D.Zero;
		private Map m_TargetMap = null;
		private bool m_bDispellable = false;
		private bool m_createReturnGate = false;
		private MoongateColour m_Colour = MoongateColour.Blue;

		public Timer gateTimer;
		public static TimeSpan MoongateTransitionTime = TimeSpan.FromSeconds (0.25);

		[Constructable]
		public UltimaMoongate ( MoongateColour color = MoongateColour.Blue, bool dispel = false ) : this( Point3D.Zero, null, 0.0, color, dispel )
		{}

		[Constructable]
		public UltimaMoongate (Point3D target, Map targetmap ) : this( target, targetmap, 0.0 )
		{}

		[Constructable]
		public UltimaMoongate (Point3D target, Map map, double opentime, MoongateColour color = MoongateColour.Blue, bool dispel = false, bool restrict = false, bool returngate = false) : base( 0X1F13 )
		{
			Movable = false;
			Visible = false;
			Name = "Manager for an UltimaMoongate.";

			m_Target = target;
			m_TargetMap = map;
			m_openTime = opentime;
			
			m_bDispellable = dispel;
			m_useStockRestrictions = restrict;
			m_createReturnGate = returngate;
			m_Colour = color;

			Light = LightType.Empty;
			
			initGate ();
		}

		[Constructable]
		public UltimaMoongate (double opentime, MoongateColour color = MoongateColour.Blue, bool dispell = false, bool restrict = false, bool returngate = false) : this ( Point3D.Zero, null, opentime, color, dispell, restrict, returngate )
		{}

		[Constructable]
		public UltimaMoongate (double opentime) : this ( Point3D.Zero, null, opentime, MoongateColour.Blue, false, false, false) {}

		public UltimaMoongate ( Serial serial ) : base( serial ) { }
		
		public virtual void initGate ()
		{
			gateTimer = new TransitionTimer (this);
			gateTimer.Start();
		}
	 
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (bool) gateOpened );
			writer.Write( (double) m_openTime );
			writer.Write( (bool) m_bDispellable );
			writer.Write( (Map) m_TargetMap );
			writer.Write( (Point3D) m_Target );
			writer.Write( (bool) m_useStockRestrictions );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			gateOpened = reader.ReadBool();
			m_openTime = reader.ReadDouble();
			m_bDispellable = reader.ReadBool();
			m_TargetMap = reader.ReadMap ();
			m_Target = reader.ReadPoint3D ();
			m_useStockRestrictions = reader.ReadBool();

			if (m_openTime > 0)
				currentGate = new UltimaMoongate_Frame (this, MoongateFrame.Frame8, false, GetMoongateFrameID (m_Colour, MoongateFrame.Frame8));
			else
				this.Delete ();
		}

		public virtual bool ShowFeluccaWarning{ get{ return false; } }
		public MoongateColour Colour { get { return m_Colour; } }
		public bool gateOpened
		{
			get { return m_gateOpened; }
			set
			{
				m_gateOpened = value;
				if (value)
					Light = LightType.Circle300;
				else
					Light = LightType.Empty;
			}
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public double OpenTime
		{
			get { return m_openTime; }
			set { m_openTime = (double) value; }
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Target
		{
			get { return m_Target; }
			set { m_Target = value; }
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public Map TargetMap
		{
			get { return m_TargetMap; }
			set { m_TargetMap = value; }
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public bool Dispellable
		{
			get { return m_bDispellable; }
			set { m_bDispellable = value; }
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public bool StockTravelRestrictions
		{
			get { return m_useStockRestrictions; }
			set { m_useStockRestrictions = value; }
		}
		[CommandProperty( AccessLevel.GameMaster )]
		public bool ReturnGate
		{
			get { return m_createReturnGate; }
		}
		
		public override void Delete ()
		{
			if (currentGate != null)
				currentGate.Delete();
				
			base.Delete();
		}

		public override bool OnMoveOver (Mobile m)
		{
			if (!gateOpened)
				return false;

			if (m.Player)
				CheckGate (m, 0);

			return true;
		}

        public virtual void UseGate( Mobile m )
        {
			if (m_useStockRestrictions)
			{
	            ClientFlags flags = m.NetState == null ? ClientFlags.None : m.NetState.Flags;

    	        if ( Server.Factions.Sigil.ExistsOn( m ) )
        	    {
            	    m.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
	            	return;
				}
    	        else if ( m_TargetMap == Map.Felucca && m is PlayerMobile && ((PlayerMobile)m).Young )
        	    {
            	    m.SendLocalizedMessage( 1049543 ); // You decide against traveling to Felucca while you are still young.
	            	return;
				}
    	        else if ( (m.Kills >= 5 && m_TargetMap != Map.Felucca) || ( m_TargetMap == Map.Tokuno && (flags & ClientFlags.Tokuno) == 0 ) || ( m_TargetMap == Map.Malas && (flags & ClientFlags.Malas) == 0 ) || ( m_TargetMap == Map.Ilshenar && (flags & ClientFlags.Ilshenar) == 0 ) )
        	    {
            	    m.SendLocalizedMessage( 1019004 ); // You are not allowed to travel there.
	            	return;
				}
    	        else if ( m.Spell != null )
        	    {
            	    m.SendLocalizedMessage( 1049616 ); // You are too busy to do that at the moment.
	            	return;
				}
			}
            
			if ( m_TargetMap != null && m_TargetMap != Map.Internal )
            {
                BaseCreature.TeleportPets( m, m_Target, m_TargetMap );
                m.MoveToWorld( m_Target, m_TargetMap );

                if ( m.AccessLevel == AccessLevel.Player || !m.Hidden )
                    m.PlaySound( 0x1FE );

                OnGateUsed( m );
            }
            else
            {
                m.SendMessage( "This moongate does not seem to go anywhere." );
            }
        }

        public virtual void CheckGate( Mobile m, int range )
        {
            #region Mondains Legacy
            if ( m.Hidden && m.AccessLevel == AccessLevel.Player && Core.ML )
                m.RevealingAction();
            #endregion

            new DelayTimer( m, this, range ).Start();
        }

		public virtual void OnGateUsed( Mobile m )
		{}

        public static bool IsInTown( Point3D p, Map map )
        {
            if ( map == null )
                return false;

            GuardedRegion reg = (GuardedRegion) Region.Find( p, map ).GetRegion( typeof( GuardedRegion ) );

            return ( reg != null && !reg.IsDisabled() );
        }

        public virtual void BeginConfirmation( Mobile from )
        {
			if ( !m_useStockRestrictions )
				UseGate( from );

            if ( IsInTown( from.Location, from.Map ) && !IsInTown( m_Target, m_TargetMap ) || (from.Map != Map.Felucca && TargetMap == Map.Felucca && ShowFeluccaWarning) )
            {
                if ( from.AccessLevel == AccessLevel.Player || !from.Hidden )
                    from.Send( new PlaySound( 0x20E, from.Location ) );
                from.CloseGump( typeof( MoongateConfirmGump ) );
				Moongate tempGate = new Moongate ( m_Target, m_TargetMap );
                from.SendGump( new MoongateConfirmGump( from, tempGate ) );
				tempGate.Delete ();
            }
            else
            {
                EndConfirmation( from );
            }
        }

        public virtual void EndConfirmation( Mobile from )
        {
            if ( !ValidateUse( from, true ) )
                return;

            UseGate( from );
        }

        public virtual bool ValidateUse( Mobile from, bool message )
        {
            if ( from.Deleted || this.Deleted )
                return false;

            if ( from.Map != this.Map || !from.InRange( this, 1 ) )
            {
                if ( message )
                    from.SendLocalizedMessage( 500446 ); // That is too far away.

                return false;
            }

            return true;
        }

        public virtual void DelayCallback( Mobile from, int range )
        {
            if ( !ValidateUse( from, false ) || !from.InRange( this, range ) )
                return;

            if ( m_TargetMap != null )
                BeginConfirmation( from );
            else
                from.SendMessage( "This moongate does not seem to go anywhere." );
        }

        private class DelayTimer : Timer
        {
            private Mobile m_From;
            private UltimaMoongate m_Gate;
            private int m_Range;

            public DelayTimer( Mobile from, UltimaMoongate gate, int range ) : base( TimeSpan.FromSeconds( 1.0 ) )
            {
                m_From = from;
                m_Gate = gate;
                m_Range = range;
            }

            protected override void OnTick()
            {
                m_Gate.DelayCallback( m_From, m_Range );
            }
        }
        
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				if (Gate.ReturnGate)
				{
					UltimaMoongate returnGate = new UltimaMoongate (Gate.GetWorldLocation(), Gate.Map, Gate.OpenTime, Gate.Colour, Gate.Dispellable, Gate.StockTravelRestrictions, false);
					returnGate.MoveToWorld (Gate.Target, Gate.TargetMap);
				}

				UltimaMoongate_Frame nextGate = new UltimaMoongate_Frame (Gate, MoongateFrame.Frame0, false, GetMoongateFrameID (Gate.Colour, MoongateFrame.Frame0));
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}

		// Returns the ItemID of an animation frame for a moongate colour. Frame 8 is the pulsating 'open gate'.
		public static int GetMoongateFrameID (MoongateColour colour, MoongateFrame frame)
		{
			if (colour == MoongateColour.Blue)
			{
				switch (frame)
				{
					case MoongateFrame.Frame0: return 0x1AF4;
					case MoongateFrame.Frame1: return 0x1AF5;
					case MoongateFrame.Frame2: return 0x1AF6;
					case MoongateFrame.Frame3: return 0x1AF7;
					case MoongateFrame.Frame4: return 0x1AF8;
					case MoongateFrame.Frame5: return 0x1AF9;
					case MoongateFrame.Frame6: return 0x1AFA;
					case MoongateFrame.Frame7: return 0x1AFB;
					case MoongateFrame.Frame8: return 0x0F6C;
				}
			}
			else if (colour == MoongateColour.Red)
			{
	            switch (frame)
	            {
	                case MoongateFrame.Frame0: return 0x1AE6;
	                case MoongateFrame.Frame1: return 0x1AE7;
	                case MoongateFrame.Frame2: return 0x1AE8;
	                case MoongateFrame.Frame3: return 0x1AE9;
	                case MoongateFrame.Frame4: return 0x1AEA;
	                case MoongateFrame.Frame5: return 0x1AEB;
	                case MoongateFrame.Frame6: return 0x1AEC;
	                case MoongateFrame.Frame7: return 0x1AED;
	                 case MoongateFrame.Frame8: return 0x0DDA;
	             }
	         }
			 else if (colour == MoongateColour.Black)
			 {
	             switch (frame)
	             {
	                 case MoongateFrame.Frame0: return 0x1FCC;
	                 case MoongateFrame.Frame1: return 0x1FCD;
	                 case MoongateFrame.Frame2: return 0x1FCE;
	                 case MoongateFrame.Frame3: return 0x1FCF;
	                 case MoongateFrame.Frame4: return 0x1FD0;
	                 case MoongateFrame.Frame5: return 0x1FD1;
	                 case MoongateFrame.Frame6: return 0x1FD2;
	                 case MoongateFrame.Frame7: return 0x1FD3;
	                 case MoongateFrame.Frame8: return 0x1FD4;
	             }
	       	}
			else if (colour == MoongateColour.Silver)
			{
	            switch (frame)
	            {
	                case MoongateFrame.Frame0: return 0x1FDF;
	                case MoongateFrame.Frame1: return 0x1FE0;
	                case MoongateFrame.Frame2: return 0x1FE1;
	                case MoongateFrame.Frame3: return 0x1FE2;
	                case MoongateFrame.Frame4: return 0x1FE3;
	                case MoongateFrame.Frame5: return 0x1FE4;
	                case MoongateFrame.Frame6: return 0x1FE5;
	                case MoongateFrame.Frame7: return 0x1FE6;
	                case MoongateFrame.Frame8: return 0x1FE7;
	            }
			}
	
			return 0;
		}
	}

	public class UltimaMoongate_Frame : Item
	{
		public Timer GateTimer = null;
		private UltimaMoongate m_baseGate = null;
		private MoongateFrame m_Frame;
		private bool m_reverse = false;
		public static TimeSpan MoongateTransitionTime = TimeSpan.FromSeconds (0.25);

		[Constructable]
		public UltimaMoongate_Frame (UltimaMoongate gate, MoongateFrame frame = MoongateFrame.Frame8, bool reverse = false, int itemid = 0x0F6C) : base (itemid)
		{
			m_baseGate = gate;
			m_Frame = frame;
			m_baseGate.currentGate = this;
			m_reverse = reverse;
			
			Movable = false;
			Visible = true;
			Light = LightType.Circle300;

			if (m_Frame == MoongateFrame.Frame8)
			{	
				m_baseGate.gateOpened = true;
				
				if (gate.OpenTime > 0)
				{
					GateTimer = new TransitionTimer (gate, this, true, gate.OpenTime);
					GateTimer.Start();
				}
			}
			else
			{
				GateTimer = new TransitionTimer (gate, this, reverse);
	            GateTimer.Start();
			}
		}
		public UltimaMoongate_Frame (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		public MoongateFrame Frame { get { return m_Frame; } }
		public UltimaMoongate BaseGate
		{
			get { return m_baseGate; }
			set { m_baseGate = value; }
		}
		
		public bool Reverse
		{
			get { return m_reverse; }
			set { m_reverse = value; }
		}

		public override void OnDoubleClick( Mobile from )
		{
			if (m_Frame != MoongateFrame.Frame8)
				return;

			if (!m_baseGate.gateOpened)
				return;

			if ( !from.Player )
                return;

			// Double-clicking a gate for a gump only works if the gate is using the stock restrictions
			if (m_baseGate.StockTravelRestrictions)
			{
            	if ( from.InRange( GetWorldLocation(), 1 ) )
                	m_baseGate.CheckGate( from, 1 );
            	else
                	from.SendLocalizedMessage( 500446 ); // That is too far away.
        	}
		}

        private class TransitionTimer : MoongateTransitionTimer
        {
            public TransitionTimer (UltimaMoongate gate, UltimaMoongate_Frame thisgate, bool reverse = false, double opentime = 0.25) : base ( gate, thisgate, reverse, opentime ) {}
            protected override void OnTick ()
            {
                UltimaMoongate_Frame nextGate = null;

				int nextFrame = (int)ThisGate.Frame + 1;
				int prevFrame = (int)ThisGate.Frame - 1;

				// If this is frame 0, continue opening or delete, depending on reverse
				if ( ThisGate.Frame == MoongateFrame.Frame0 )
				{
                	if (!Reverse)
                	{
                    	nextGate = new UltimaMoongate_Frame (Gate, (MoongateFrame)nextFrame, false, UltimaMoongate.GetMoongateFrameID (Gate.Colour, (MoongateFrame)nextFrame));
                    	nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
                    	ThisGate.Delete ();
                	}
                	else
                	{
                    	Gate.Delete ();
                    	ThisGate.Delete ();
                	}
				}
				
				// If this is frame 8, begin reversing the animation.
				else if ( ThisGate.Frame == MoongateFrame.Frame8 )
				{
                	Gate.gateOpened = false;
                	nextGate = new UltimaMoongate_Frame (Gate, (MoongateFrame)prevFrame, true, UltimaMoongate.GetMoongateFrameID (Gate.Colour, (MoongateFrame)prevFrame));
                	nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
                	ThisGate.Delete ();
				}

				// Otherwise, advance or retreat through animation sequence
				else
				{
                	if (!Reverse)
                		nextGate = new UltimaMoongate_Frame (Gate, (MoongateFrame)nextFrame, false, UltimaMoongate.GetMoongateFrameID (Gate.Colour, (MoongateFrame)nextFrame));
					else
                    	nextGate = new UltimaMoongate_Frame (Gate, (MoongateFrame)prevFrame, true, UltimaMoongate.GetMoongateFrameID (Gate.Colour, (MoongateFrame)prevFrame));

                	nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
                	ThisGate.Delete ();
				}
            }
        }
    }

	// This is the timer used by all the moongate frames, to cycle through the rising / falling animations.
	public class MoongateTransitionTimer : Timer
	{
		private bool m_reverse = false;
		private UltimaMoongate m_Gate;
		private UltimaMoongate_Frame m_thisGate;

		public MoongateTransitionTimer (UltimaMoongate gate, bool reverse = false) : base ( TimeSpan.FromSeconds (0.25) )
		{
			m_Gate = gate;
			m_reverse = reverse;
			Priority = TimerPriority.TwentyFiveMS;
		}
		
		public MoongateTransitionTimer (UltimaMoongate gate, UltimaMoongate_Frame thisgate, bool reverse = false, double opentime = 0.25) : base ( TimeSpan.FromSeconds (opentime) )
		{
			m_Gate = gate;
			m_thisGate = thisgate;
			m_reverse = reverse;
			Priority = TimerPriority.TwentyFiveMS;
		}
		
		public bool Reverse
		{
			get { return m_reverse; }
			set { m_reverse = value; }
		}
		public UltimaMoongate Gate
		{
			get { return m_Gate; }
			set { m_Gate = value; }
		}
		public UltimaMoongate_Frame ThisGate
		{
			get { return m_thisGate; }
			set { m_thisGate = value; }
		}
	}
}
