/* UltimaMoongates_U4 - An Ultima IV-style moongate system for UO.
 * The phases of the moons once again control the moongates.
 *
 * ****
 * NOTE: This file requires "UltimaMoongates_Animations.cs" to function!
 * ****
 *
 * Author: Warped Dragon aka Dallen Wilson <dwjwilson@lavabit.com>
 *
 * A list of phases by number, name and destination is available in a comment block further down.
 *
 * Usage:
 * As Trammel cycles through it's phases, moongates around Britannia will open and close. As Felucca cycles, the open moongate's destination will change.
 * The command '[U4MoonGen' will auto-generate these moongates on both the Trammel and Felucca facets, removing any existing moongates (U4 or stock) from the moongate circles.
 *
 * Items:
 * [add UltimaMoongate [int 0-7]:		Creates a moongate that opens at Trammel phase (0 to 7). Destination while it's open is controlled by Felucca.
 * [add UltimaMoongate_U4 [double]:		Creates a moongate that opens immediately and stays open for [double] seconds before closing. It's destination changes with the phase of Felucca. Use 0.0 for a gate that never closes.
 *
 * Notes:
 * Uses RunUO's existing in-game time and moon phase system, details of which can be found in Scripts/Skill Items/Tinkering/Clocks.cs.
 * As a result, the length of the moon phases and their relationship to each other are a bit different than in Ultima IV.
 *
 * One in-game minute is five real-world seconds.
 * Trammel changes phase every 30 in-game minutes / 150 real-world seconds.
 * Felucca changes phase every 10 in-game minutes / 50 real-world seconds.
 *
 * In-game time, and as a result the phase of the moon, does vary slightly depending on where in the world the Mobile/Item is when calling the GetTime() function.
 * This may allow multiple gates to overlap slightly and be open at the same time for a few seconds.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Items;
using Warped.Items;

namespace Warped.Items
{
 #region UltimaIVMoongates

 	// The UltimaMoongate_U4 opens and closes the gate according to the moon phases.
	// It is represented by a worldgem bit and is invisible to players in-game.
	public class UltimaMoongate_U4 : Item
	{
		/*
		 * Trammel - Determines which moongate is open. Changes every 30 in-game minutes / 150 real-seconds
		 * Felucca - Determines the destination of the currently open gate. Changes every 10 in-game minutes / 50 real seconds
		 * 0	NewMoon				Moonglow	4467, 1283, 5
		 * 1	WaxingCrescentMoon	Britain		1336, 1997, 5
		 * 2	FirstQuarter		Jhelom		1499, 3771, 5
		 * 3	WaxingGibbous		Yew			771, 752, 5
		 * 4	Full moon			Minoc		2701, 692, 5		// Yes, Minoc. I don't care what UO players think, Vesper has never had a moongate.
		 * 5	WaningGibbous		Trinsic		1828, 2948, -20
		 * 6	LastQuarter			Skara Brae	643, 2067, 5
		 * 7	WaningCrescent		Magnicia	3563, 2139, 31
		 */
		
		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } }

		//	Default to Britain.
		public MoonPhase ThisGateIs = MoonPhase.WaxingCrescentMoon;
		
		public UltimaMoongate_U4_Gate Gate = null;

		private Timer m_Timer = null;

		[Constructable]
		public UltimaMoongate_U4 ( MoonPhase phase ) : base( 0x1F13 )
		{
			ThisGateIs = phase;
			Movable = false;
			Visible = false;
			Name = "An Ultima IV-style moongate opening at "+ getPhaseName ((int)phase);		
			
			initTimer ();
		}

		public UltimaMoongate_U4 ( Serial serial ) : base( serial ) {}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
			writer.Write( (int) ThisGateIs);
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			ThisGateIs = (MoonPhase)reader.ReadInt();
			
			initTimer ();
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public MoonPhase OpensAtPhase
		{
			get { return ThisGateIs; }
			set { ThisGateIs = value; }
		}

		public override void Delete ()
		{
			if (Gate != null)
				Gate.Delete();

			base.Delete ();
		}
		
		private void initTimer ()
		{
			m_Timer = new UltimaMoongateTimer (this, 1.0);
			m_Timer.Start ();
		}

		public static string getPhaseName (int x)
		{
			switch (x)
			{
				case 0: return "New Moon";
				case 1: return "Waxing Crescent";
				case 2: return "First Quarter";
				case 3: return "Waxing Gibbous";
				case 4: return "Full Moon";
				case 5: return "Waning Gibbous";
				case 6: return "Last Quarter";
				case 7: return "Waning Crescent";
				default: return "Unknown phase";
			}
		}

		public static double nextPhaseChange (int x)
		{
			int gameHours, gameMinutes, totalMinutes;
			Clock.GetTime ( Map.Trammel, x, 0, out gameHours, out gameMinutes, out totalMinutes );

			double curPhaseDiv = ((double)totalMinutes / 30);
			double result = (Math.Round((((int)curPhaseDiv + 1) - curPhaseDiv) * 30) * 5);
				
			return result;
		}
		
		private class UltimaMoongateTimer : Timer
		{
			private UltimaMoongate_U4 m_Gate;

			public UltimaMoongateTimer ( UltimaMoongate_U4 gate, double timerDuration = 20 ) : base ( TimeSpan.FromSeconds (timerDuration) )
			{
				m_Gate = gate;
				Priority = TimerPriority.TwentyFiveMS;
			}

			protected override void OnTick ()
			{
				double timeToNextPhase = nextPhaseChange(m_Gate.GetWorldLocation().X);	
				
				if (timeToNextPhase > 4.0)
				{
					// If this is Our Phase, open the gate if it isn't already open
					if (Clock.GetMoonPhase (Map.Trammel, m_Gate.GetWorldLocation().X, 0) == m_Gate.ThisGateIs)
					{
						if (m_Gate.Gate == null)
						{
							m_Gate.Gate = new UltimaMoongate_U4_Gate ( (timeToNextPhase - 2) );
							m_Gate.Gate.MoveToWorld (m_Gate.GetWorldLocation(), m_Gate.Map );
						}
					}
				}

				// A RunUO poor-man's Auto-reset.
				// Update the interval with a re-calculated time to the next phase change. Allows gate to open halfway through cycle on server re-load, and accounts for any drift due to lag.
				this.Stop();
				this.Interval = TimeSpan.FromSeconds (timeToNextPhase);
				this.Start();
			}
		}
		
		public static void Initialize()
		{
			CommandSystem.Register( "U4MoonGen", AccessLevel.Administrator, new CommandEventHandler( U4MoonGen_OnCommand ) );
		}
		
		[Usage( "U4MoonGen" )]
		[Description( "Generates Ultima IV-style moongates in the moongate circles." )]
		public static void U4MoonGen_OnCommand( CommandEventArgs e )
		{
			U4MoonGen_DeleteOld ();

			for (int x = 0; x < 8; x++)
			{
				Item felGate = new UltimaMoongate_U4 ((MoonPhase)x);
				felGate.MoveToWorld (UltimaMoongate_U4_Gate.MoonPhase2Destination ((MoonPhase)x), Map.Felucca);
				Item tramGate = new UltimaMoongate_U4 ((MoonPhase)x);
				tramGate.MoveToWorld (UltimaMoongate_U4_Gate.MoonPhase2Destination ((MoonPhase)x), Map.Trammel);
			}
		}
		private static void U4MoonGen_DeleteOld ()
		{
			List<Item> U4_list = new List<Item>();
			List<Item> Public_list = new List<Item>();

			int u4count = 0;
			int publiccount = 0;

			foreach (Item item in World.Items.Values)
			{
				if (item is UltimaMoongate_U4)
					U4_list.Add (item);
				else if (item is PublicMoongate)
					Public_list.Add (item);
			}

			// Only delete UltimaMoongates if they are in the moongate circles
			foreach (Item item in U4_list)
			{
				for (int x = 0; x < 8; x++)
				{
					if (item.GetWorldLocation() == UltimaMoongate_U4_Gate.MoonPhase2Destination ((MoonPhase)x))
					{
						item.Delete();
						u4count++;
						break;
					}
				}
			}

			// Only delete PublicMoongates if they are in Trammel or Felucca -and- in the moongate circles
			foreach (Item item in Public_list)
			{
				if ((item.Map == Map.Trammel) || (item.Map == Map.Felucca))
				{
					for (int x = 0; x < 8; x++)
					{
						if (item.GetWorldLocation() == UltimaMoongate_U4_Gate.MoonPhase2Destination ((MoonPhase)x))
						{
							item.Delete();
							publiccount++;
							break;
						}
					}
				}
			}

			if (U4_list.Count > 0)
				World.Broadcast (0x35, true, "{0} UltimaMoongates removed.", u4count);
			if (Public_list.Count > 0)
				World.Broadcast (0x35, true, "{0} PublicMoongates removed.", publiccount);
		}
	}
	
	// The UltimaMoongate_U4_Gate handles the gate opening/closing animations and the teleportation.
	// It is represented by a worldgem bit and is invisible to players in-game.
	public class UltimaMoongate_U4_Gate : UltimaMoongate
	{
		private MoonPhase m_targetPhase = MoonPhase.WaxingCrescentMoon;
		private bool m_usePhase = false;

		[Constructable]
		public UltimaMoongate_U4_Gate (double opentime) : base (opentime) {}
		public UltimaMoongate_U4_Gate (double opentime, MoonPhase targetPhase) : base (opentime)
		{
			m_targetPhase = targetPhase;
			m_usePhase = true;
		}
		public UltimaMoongate_U4_Gate ( Serial serial ) : base( serial ) {}
		
		public override void initGate ()
		{
			Name = "Manager for an Ultima IV-style moongate.";
			gateTimer = new TransitionTimer (this);
			gateTimer.Start ();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize ( writer );
			writer.Write ( (Item)currentGate );
		}
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize ( reader );
			currentGate = (UltimaMoongate_Frame)reader.ReadItem();

			// If opentime is non-zero, assume this was a summoned gate and delete it.
			// If summoned by an UltimaMoongate by the moon phase, it'll re-create it when it loads.
			// If summoned by spell or GM, it was intended to be temporary anyways.
			if (OpenTime > 0)
				this.Delete();

			// If zero, it was supposed to be open forever. Raise the gate!
			else
			{
				gateTimer = new TransitionTimer (this);
				gateTimer.Start ();
			}
		}
	   
		// Bare-bones teleporter. No extra checks like the ones in Items/Misc/PublicMoongate.cs.
		public override bool OnMoveOver (Mobile m)
		{
			if (!gateOpened)
				return false;
				
			if (!m.Player)
				return false;
			
			//	If no preset target phase, use the moon
			MoonPhase curPhase = Clock.GetMoonPhase (Map.Felucca, this.GetWorldLocation().X, 0);
			
			//	If a target phase is specified on gate creation, override the moon
			if (m_usePhase)
				curPhase = m_targetPhase;

			Point3D dest = MoonPhase2Destination (curPhase);
			
			BaseCreature.TeleportPets (m, dest, this.Map);
			m.MoveToWorld (dest, this.Map);
			
			if ( m.AccessLevel == AccessLevel.Player || !m.Hidden )
				m.PlaySound( 0x1FE );
			
			return false;
		}
		
		// Starts the moongate animation sequence
		private class TransitionTimer : MoongateTransitionTimer
		{
			public TransitionTimer (UltimaMoongate gate) : base ( gate ) {}
			protected override void OnTick ()
			{
				UltimaMoongate_Frame nextGate = new UltimaMoongate_Frame (Gate, MoongateFrame.Frame0, false, UltimaMoongate.GetMoongateFrameID (Gate.Colour, MoongateFrame.Frame0));
				nextGate.MoveToWorld (Gate.GetWorldLocation (), Gate.Map);
			}
		}

		//	Converts MoonPhase to Point3D destination. Coords lifted from Items/Misc/PublicMoongate.cs
		public static Point3D MoonPhase2Destination (MoonPhase phase)
		{
			Point3D dest = Point3D.Zero;

			switch (phase)
			{
				case MoonPhase.NewMoon:				dest = new Point3D( 4467, 1283, 5 ); break;
				case MoonPhase.WaxingCrescentMoon:	dest = new Point3D( 1336, 1997, 5 ); break;
				case MoonPhase.FirstQuarter:		dest = new Point3D( 1499, 3771, 5 ); break;
				case MoonPhase.WaxingGibbous:		dest = new Point3D(  771,  752, 5 ); break;
				case MoonPhase.FullMoon:			dest = new Point3D( 2701,  692, 5 ); break;
				case MoonPhase.WaningGibbous:		dest = new Point3D( 1828, 2948,-20); break;
				case MoonPhase.LastQuarter:			dest = new Point3D(  643, 2067, 5 ); break;
				case MoonPhase.WaningCrescent:		dest = new Point3D( 3563, 2139, 31); break;
			}			
			return dest;
		}
	}
#endregion
}
