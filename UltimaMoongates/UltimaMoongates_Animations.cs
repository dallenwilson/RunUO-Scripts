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
using Server.Mobiles;

namespace Warped.Items
{
	// This is the timer used by all the moongate frames, to cycle through the rising / falling animations.
	public class MoongateTransitionTimer : Timer
	{
		private bool m_reverse = false;
		private UltimaMoongate_Base m_Gate;
		private UltimaMoongate_Frame_Base m_thisGate;

		public MoongateTransitionTimer (UltimaMoongate_Base gate, bool reverse = false) : base ( TimeSpan.FromSeconds (0.25) )
		{
			m_Gate = gate;
			m_reverse = reverse;
			Priority = TimerPriority.TwentyFiveMS;
		}
		
		public MoongateTransitionTimer (UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false, double opentime = 0.25) : base ( TimeSpan.FromSeconds (opentime) )
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
		public UltimaMoongate_Base Gate
		{
			get { return m_Gate; }
			set { m_Gate = value; }
		}
		public UltimaMoongate_Frame_Base ThisGate
		{
			get { return m_thisGate; }
			set { m_thisGate = value; }
		}
	}
	
	// This is the manager for the moongate. It everything; Creation of the animations, teleporting, targeting, etc.
	// Some of it was lifted from Items/Skill Items/Magical/Misc/Moongate.cs and Items/Misc/PublicMoongate.cs
	public class UltimaMoongate_Base : Item
	{
		public bool gateOpened = false;
		public UltimaMoongate_Frame_Base currentGate = null;

		private double m_openTime = 0;
		private bool m_useStockRestrictions = false;
		private Point3D m_Target = Point3D.Zero;
		private Map m_TargetMap = null;
		private bool m_bDispellable = false;
		private bool m_createReturnGate = false;

		public Timer gateTimer;

		[Constructable]
		public UltimaMoongate_Base (Point3D target, Map map, double opentime, bool dispel = false, bool restrict = false, bool returngate = false) : base( 0X1F13 )
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
		}
		[Constructable]
		public UltimaMoongate_Base (double opentime, bool dispell = false, bool restrict = false, bool returngate = false) : base ( 0x1F13 )
		{
			Movable = false;
			Visible = false;
			Name = "Manager for an UltimaMoongate.";

			m_openTime = opentime;
			m_bDispellable = dispell;
			m_useStockRestrictions = restrict;
			m_createReturnGate = returngate;
		}

		public UltimaMoongate_Base ( Serial serial ) : base( serial ) { }
	 
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

		// Derived classes will need to extend this for their own unique circumstances.
		// Re-creating the visible gate, deleting this, or something else.
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			gateOpened = reader.ReadBool();
			m_openTime = reader.ReadDouble();
			m_bDispellable = reader.ReadBool();
			m_TargetMap = reader.ReadMap ();
			m_Target = reader.ReadPoint3D ();
			m_useStockRestrictions = reader.ReadBool();
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

			if (!m.Player)
				return false;

			if (m_Target == Point3D.Zero)
				return false;

			if (m_TargetMap == null)
				return false;

			if (!m_useStockRestrictions)
			{
				BaseCreature.TeleportPets (m, m_Target, m_TargetMap);
				m.MoveToWorld (m_Target, m_TargetMap);

				if ( m.AccessLevel == AccessLevel.Player || !m.Hidden )
					m.PlaySound( 0x1FE );
			}

			return false;
		}

	}

	public class UltimaMoongate_Frame_Base : Item
	{
		public Timer GateTimer = null;
		private UltimaMoongate_Base m_baseGate = null;
		private bool m_reverse = false;
		public static TimeSpan MoongateTransitionTime = TimeSpan.FromSeconds (0.25);

		[Constructable]
		public UltimaMoongate_Frame_Base (UltimaMoongate_Base gate, bool reverse = false, int itemid = 0x1AF4) : base (itemid)
		{
			m_baseGate = gate;
			m_baseGate.currentGate = this;
			m_reverse = reverse;
			
			Movable = false;
			Visible = true;
		}
		public UltimaMoongate_Frame_Base (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		public UltimaMoongate_Base BaseGate
		{
			get { return m_baseGate; }
			set { m_baseGate = value; }
		}
		
		public bool Reverse
		{
			get { return m_reverse; }
			set { m_reverse = value; }
		}
	}

#region BlueMoongateAnimation
	public class UltimaMoongate_Blue : UltimaMoongate_Base
	{
		[Constructable]
		public UltimaMoongate_Blue (Point3D target, Map map, double opentime, bool dispell = false, bool restrict = false, bool returngate = false) : base( target, map, opentime, dispell, restrict, returngate)
		{
			initGate ();
		}
		[Constructable]
		public UltimaMoongate_Blue (double opentime) : base (opentime)
		{
			initGate ();
		}
		public UltimaMoongate_Blue( Serial serial ) : base( serial ) {}

		public virtual void initGate ()
		{
			Name = "Manager for a Blue UltimaMoongate.";
			gateTimer = new TransitionTimer (this);
			gateTimer.Start();
		}

		public override void Serialize( GenericWriter writer ) { base.Serialize ( writer ); }
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize ( reader );
			
			if (OpenTime > 0)
				currentGate = new UltimaMoongate_Blue_Frame8 (this);
			else
				this.Delete ();
		}

		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				if (Gate.ReturnGate)
				{
					UltimaMoongate_Base returnGate = new UltimaMoongate_Blue (Gate.GetWorldLocation(), Gate.Map, Gate.OpenTime, Gate.Dispellable, Gate.StockTravelRestrictions, false);
					returnGate.MoveToWorld (Gate.Target, Gate.TargetMap);
				}

				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Blue_Frame0 (Gate);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}
	}

	public class UltimaMoongate_Blue_Frame0 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame0 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF4)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame0 (Serial serial) : base (serial) {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}			
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
				{
					nextGate = new UltimaMoongate_Blue_Frame1 (Gate);
					nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
					ThisGate.Delete ();
				}
				else
				{	
					Gate.Delete ();
					ThisGate.Delete ();
				}
			}
		}
	}
	public class UltimaMoongate_Blue_Frame1 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame1 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF5)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame1 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame2 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame0 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame2 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame2 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF6)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame2 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame3 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame1 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame3 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame3 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF7)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame3 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame4 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame2 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame4 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame4 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF8)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame4 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame5 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame3 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame5 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame5 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AF9)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame5 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame6 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame4 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame6 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame6 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AFA)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame6 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame7 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame5 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();	
			}
		}
	}
	public class UltimaMoongate_Blue_Frame7 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame7 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AFB)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Blue_Frame7 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Blue_Frame8 (Gate);
				else
					nextGate = new UltimaMoongate_Blue_Frame6 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Blue_Frame8 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Blue_Frame8 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x0F6C)
		{
			gate.currentGate = this;
			gate.gateOpened = true;
			if (gate.OpenTime > 0)
			{
				GateTimer = new TransitionTimer (gate, this, true, gate.OpenTime);
				GateTimer.Start ();
			}
		}
		
		public UltimaMoongate_Blue_Frame8 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false, double opentime = 0.25)  : base ( gate, thisgate, reverse, opentime ) {}
			protected override void OnTick ()
			{
				Gate.gateOpened = false;
				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Blue_Frame7 (Gate, true);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
#endregion

#region RedMoongateAnimation
	public class UltimaMoongate_Red : UltimaMoongate_Base
	{
		[Constructable]
		public UltimaMoongate_Red (Point3D target, Map map, double opentime, bool dispell = false, bool restrict = false, bool returngate = false) : base( target, map, opentime, dispell, restrict, returngate)
		{
			initGate ();
		}
		[Constructable]
		public UltimaMoongate_Red (double opentime) : base (opentime)
		{
			initGate ();
		}
		public UltimaMoongate_Red( Serial serial ) : base( serial ) {}

		public virtual void initGate ()
		{
			Name = "Manager for a Red UltimaMoongate.";
			gateTimer = new TransitionTimer (this);
			gateTimer.Start();
		}

		public override void Serialize( GenericWriter writer ) { base.Serialize ( writer ); }
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize ( reader );
			
			if (OpenTime > 0)
				currentGate = new UltimaMoongate_Red_Frame8 (this);
			else
				this.Delete ();
		}

		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				if (Gate.ReturnGate)
				{
					UltimaMoongate_Base returnGate = new UltimaMoongate_Red (Gate.GetWorldLocation(), Gate.Map, Gate.OpenTime, Gate.Dispellable, Gate.StockTravelRestrictions, false);
					returnGate.MoveToWorld (Gate.Target, Gate.TargetMap);
				}

				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Red_Frame0 (Gate);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}
	}

	public class UltimaMoongate_Red_Frame0 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame0 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AE6)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame0 (Serial serial) : base (serial) {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}			
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
				{
					nextGate = new UltimaMoongate_Red_Frame1 (Gate);
					nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
					ThisGate.Delete ();
				}
				else
				{	
					Gate.Delete ();
					ThisGate.Delete ();
				}
			}
		}
	}
	public class UltimaMoongate_Red_Frame1 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame1 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AE7)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame1 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame2 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame0 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame2 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame2 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AE8)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame2 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame3 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame1 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame3 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame3 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AE9)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame3 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame4 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame2 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame4 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame4 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AEA)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame4 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame5 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame3 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame5 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame5 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AEB)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame5 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame6 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame4 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame6 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame6 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AEC)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame6 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame7 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame5 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();	
			}
		}
	}
	public class UltimaMoongate_Red_Frame7 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame7 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1AED)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Red_Frame7 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Red_Frame8 (Gate);
				else
					nextGate = new UltimaMoongate_Red_Frame6 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Red_Frame8 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Red_Frame8 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x0DDA)
		{
			gate.currentGate = this;
			gate.gateOpened = true;
			if (gate.OpenTime > 0)
			{
				GateTimer = new TransitionTimer (gate, this, true, gate.OpenTime);
				GateTimer.Start ();
			}
		}
		
		public UltimaMoongate_Red_Frame8 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false, double opentime = 0.25)  : base ( gate, thisgate, reverse, opentime ) {}
			protected override void OnTick ()
			{
				Gate.gateOpened = false;
				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Red_Frame7 (Gate, true);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
#endregion

#region BlackMoongateAnimation
	public class UltimaMoongate_Black : UltimaMoongate_Base
	{
		[Constructable]
		public UltimaMoongate_Black (Point3D target, Map map, double opentime, bool dispell = false, bool restrict = false, bool returngate = false) : base( target, map, opentime, dispell, restrict, returngate)
		{
			initGate ();
		}
		[Constructable]
		public UltimaMoongate_Black (double opentime) : base (opentime)
		{
			initGate ();
		}
		public UltimaMoongate_Black( Serial serial ) : base( serial ) {}

		public virtual void initGate ()
		{
			Name = "Manager for a Black UltimaMoongate.";
			gateTimer = new TransitionTimer (this);
			gateTimer.Start();
		}

		public override void Serialize( GenericWriter writer ) { base.Serialize ( writer ); }
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize ( reader );
			
			if (OpenTime > 0)
				currentGate = new UltimaMoongate_Black_Frame8 (this);
			else
				this.Delete ();
		}

		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				if (Gate.ReturnGate)
				{
					UltimaMoongate_Base returnGate = new UltimaMoongate_Black (Gate.GetWorldLocation(), Gate.Map, Gate.OpenTime, Gate.Dispellable, Gate.StockTravelRestrictions, false);
					returnGate.MoveToWorld (Gate.Target, Gate.TargetMap);
				}

				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Black_Frame0 (Gate);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}
	}

	public class UltimaMoongate_Black_Frame0 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame0 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FCC)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame0 (Serial serial) : base (serial) {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}			
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
				{
					nextGate = new UltimaMoongate_Black_Frame1 (Gate);
					nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
					ThisGate.Delete ();
				}
				else
				{	
					Gate.Delete ();
					ThisGate.Delete ();
				}
			}
		}
	}
	public class UltimaMoongate_Black_Frame1 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame1 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FCD)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame1 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame2 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame0 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame2 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame2 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FCE)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame2 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame3 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame1 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame3 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame3 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FCF)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame3 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame4 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame2 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame4 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame4 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FD0)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame4 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame5 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame3 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame5 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame5 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FD1)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame5 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame6 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame4 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame6 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame6 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FD2)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame6 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame7 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame5 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();	
			}
		}
	}
	public class UltimaMoongate_Black_Frame7 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame7 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FD3)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Black_Frame7 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Black_Frame8 (Gate);
				else
					nextGate = new UltimaMoongate_Black_Frame6 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Black_Frame8 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Black_Frame8 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FD4)
		{
			gate.currentGate = this;
			gate.gateOpened = true;
			if (gate.OpenTime > 0)
			{
				GateTimer = new TransitionTimer (gate, this, true, gate.OpenTime);
				GateTimer.Start ();
			}
		}
		
		public UltimaMoongate_Black_Frame8 (Serial serial) : base (serial)	{}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false, double opentime = 0.25)  : base ( gate, thisgate, reverse, opentime ) {}
			protected override void OnTick ()
			{
				Gate.gateOpened = false;
				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Black_Frame7 (Gate, true);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
#endregion

#region SilverMoongateAnimation
	public class UltimaMoongate_Silver : UltimaMoongate_Base
	{
		[Constructable]
		public UltimaMoongate_Silver (Point3D target, Map map, double opentime, bool dispell = false, bool restrict = false, bool returngate = false) : base( target, map, opentime, dispell, restrict, returngate)
		{
			initGate ();
		}
		[Constructable]
		public UltimaMoongate_Silver (double opentime) : base (opentime)
		{
			initGate ();
		}
		public UltimaMoongate_Silver( Serial serial ) : base( serial ) {}

		public virtual void initGate ()
		{
			Name = "Manager for a Silver UltimaMoongate.";
			gateTimer = new TransitionTimer (this);
			gateTimer.Start();
		}

		public override void Serialize( GenericWriter writer ) { base.Serialize ( writer ); }
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize ( reader );
			
			if (OpenTime > 0)
				currentGate = new UltimaMoongate_Silver_Frame8 (this);
			else
				this.Delete ();
		}

		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				if (Gate.ReturnGate)
				{
					UltimaMoongate_Base returnGate = new UltimaMoongate_Silver (Gate.GetWorldLocation(), Gate.Map, Gate.OpenTime, Gate.Dispellable, Gate.StockTravelRestrictions, false);
					returnGate.MoveToWorld (Gate.Target, Gate.TargetMap);
				}

				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Silver_Frame0 (Gate);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}
	}

	public class UltimaMoongate_Silver_Frame0 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame0 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FDF)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame0 (Serial serial) : base (serial) {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}			
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
				{
					nextGate = new UltimaMoongate_Silver_Frame1 (Gate);
					nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
					ThisGate.Delete ();
				}
				else
				{	
					Gate.Delete ();
					ThisGate.Delete ();
				}
			}
		}
	}
	public class UltimaMoongate_Silver_Frame1 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame1 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE0)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame1 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame2 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame0 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame2 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame2 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE1)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame2 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame3 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame1 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame3 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame3 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE2)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame3 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame4 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame2 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame4 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame4 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE3)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame4 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame5 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame3 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame5 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame5 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE4)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame5 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame6 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame4 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame6 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame6 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE5)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame6 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame7 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame5 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();	
			}
		}
	}
	public class UltimaMoongate_Silver_Frame7 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame7 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE6)
		{
			gate.currentGate = this;
			GateTimer = new TransitionTimer (gate, this, reverse);
			GateTimer.Start();
		}
		public UltimaMoongate_Silver_Frame7 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false) : base ( gate, thisgate, reverse ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame_Base nextGate = null;
				
				if (!Reverse)
					nextGate = new UltimaMoongate_Silver_Frame8 (Gate);
				else
					nextGate = new UltimaMoongate_Silver_Frame6 (Gate, true);
				
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
	public class UltimaMoongate_Silver_Frame8 : UltimaMoongate_Frame_Base
	{
		[Constructable]
		public UltimaMoongate_Silver_Frame8 (UltimaMoongate_Base gate, bool reverse = false) : base (gate, reverse, 0x1FE7)
		{
			gate.currentGate = this;
			gate.gateOpened = true;
			if (gate.OpenTime > 0)
			{
				GateTimer = new TransitionTimer (gate, this, true, gate.OpenTime);
				GateTimer.Start ();
			}
		}
		
		public UltimaMoongate_Silver_Frame8 (Serial serial) : base (serial)  {}
		public override void Serialize( GenericWriter writer )		{}
		public override void Deserialize( GenericReader reader )	{}
		
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer ( UltimaMoongate_Base gate, UltimaMoongate_Frame_Base thisgate, bool reverse = false, double opentime = 0.25)  : base ( gate, thisgate, reverse, opentime ) {}
			protected override void OnTick ()
			{
				Gate.gateOpened = false;
				UltimaMoongate_Frame_Base nextGate = new UltimaMoongate_Silver_Frame7 (Gate, true);
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
				ThisGate.Delete ();
			}
		}
	}
#endregion
}
