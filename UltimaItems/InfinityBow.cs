using System;
using Server;
//using Server.Network;
using Server.Items;
using Server.Spells;

namespace Warped.Items
{
	[FlipableAttribute( 0x13B2, 0x13B1 )]
	public class InfinityBow : BaseRanged
	{
		public override int EffectID{ get{ return 0x36D4; } }
		public override Type AmmoType{ get{ return typeof( Arrow ); } }
		public override Item Ammo{ get{ return new Arrow(); } }

		public override WeaponAbility PrimaryAbility{ get{ return WeaponAbility.ParalyzingBlow; } }
		public override WeaponAbility SecondaryAbility{ get{ return WeaponAbility.MortalStrike; } }

		public override int AosStrengthReq{ get{ return 30; } }
		public override int AosMinDamage{ get{ return Core.ML ? 15 : 16; } }
		public override int AosMaxDamage{ get{ return Core.ML ? 19 : 18; } }
		public override int AosSpeed{ get{ return 25; } }
		public override float MlSpeed{ get{ return 4.25f; } }

		public override int OldStrengthReq{ get{ return 20; } }
		public override int OldMinDamage{ get{ return 9; } }
		public override int OldMaxDamage{ get{ return 41; } }
		public override int OldSpeed{ get{ return 40; } }	// Default 20. Infinity Bow's should be .... fast.

		public override int DefMaxRange{ get{ return 10; } }

		public override int InitMinHits{ get{ return 31; } }
		public override int InitMaxHits{ get{ return 60; } }

		public override WeaponAnimation DefAnimation{ get{ return WeaponAnimation.ShootBow; } }

		[Constructable]
		public InfinityBow() : base( 0x13B2 )
		{
			Weight = 6.0;
			Hue = 0x455;
			Layer = Layer.TwoHanded;
		}

		public InfinityBow( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( Weight == 7.0 )
				Weight = 6.0;
		}

		// Apply damage as Fireball damage, not physical sticky-pokey-arrow damage
        public override void OnHit( Mobile attacker, Mobile defender, double damageBonus )
        {
			attacker.MovingParticles( defender, EffectID, 7,    1, false, true, 9502, 4019, 0x160 );
            attacker.PlaySound( Core.AOS ? 0x15E : 0x44B );

        	SpellHelper.CheckReflect( (int)SpellCircle.Third, ref attacker, ref defender );

            double damage;

			Server.Spells.Third.FireballSpell tempFireball = new Server.Spells.Third.FireballSpell ( attacker, null );

            if ( Core.AOS )
            {
                damage = tempFireball.GetNewAosDamage( 19, 1, 5, defender );
            }
            else
            {
                damage = Utility.Random( 10, 7 );

                if ( tempFireball.CheckResisted( defender ) )
                {
                    damage *= 0.75;

                    defender.SendLocalizedMessage( 501783 ); // You feel yourself resisting magical energy.
                }

				// TODO: Scale the damage somehow, because stock Fireball is bleh.
				// Use Magery & EvalInt, like the Fireball Spell? Use Archery & Tactics? Combination? I don't know.
                // Pretend it's a standard fireball spell for now. Naturally mages would get more damage out of a magical artifact?
				damage *= (tempFireball.GetDamageScalar( defender ) * 2);
            }
				
            SpellHelper.Damage( tempFireball, defender, damage, 0, 100, 0, 0, 0 );
		}


		// No ammo requirement whatsoever for this sucker.
		public override bool OnFired( Mobile attacker, Mobile defender )
		{
			PlaySwingAnimation( attacker );
			return true;
		}

		// No ammo drops on a miss because fireballs!
		public override void OnMiss( Mobile attacker, Mobile defender )
		{
			if ( Core.AOS )
                attacker.FixedParticles( 0x3735, 1, 30, 9503, EffectLayer.Waist );
            else
                attacker.FixedEffect( 0x3735, 6, 30 );

            attacker.PlaySound( 0x5C );
		}
	}
}
