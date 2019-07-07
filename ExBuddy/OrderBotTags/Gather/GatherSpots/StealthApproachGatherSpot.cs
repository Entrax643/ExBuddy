﻿namespace ExBuddy.OrderBotTags.Gather.GatherSpots
{
	using Buddy.Coroutines;
	using Clio.Utilities;
	using Clio.XmlEngine;
	using ExBuddy.Helpers;
	using ff14bot;
	using System.ComponentModel;
	using System.Threading.Tasks;
	using ff14bot.Behavior;
	using ff14bot.Managers;
	using ff14bot.Navigation;

    [XmlElement("StealthApproachGatherSpot")]
	public class StealthApproachGatherSpot : GatherSpot
	{
		[DefaultValue(true)]
		[XmlAttribute("ReturnToStealthLocation")]
		[XmlAttribute("ReturnToApproachLocation")]
		public bool ReturnToStealthLocation { get; set; }

	    [XmlAttribute("StealthLocation")]
	    [XmlAttribute("ApproachLocation")]
        public Vector3 StealthLocation { get; set; }

		[XmlAttribute("UnstealthAfter")]
		public bool UnstealthAfter { get; set; }

		public override async Task<bool> MoveFromSpot(ExGatherTag tag)
		{
			tag.StatusText = "Moving from " + this;

			var result = true;
			if (ReturnToStealthLocation)
			{
				result &= await StealthLocation.MoveToOnGroundNoMount(tag.Distance, tag.Node.EnglishName, tag.MovementStopCallback);
			}

			if (UnstealthAfter && Core.Player.HasAura((int)AbilityAura.Stealth))
			{
				result &= await tag.CastAura(Ability.Stealth);
			}

			return result;
		}

		public override async Task<bool> MoveToSpot(ExGatherTag tag)
		{
			tag.StatusText = "Moving to " + this;

			if (StealthLocation == Vector3.Zero)
			{
				return false;
			}

			var result =
				await
					StealthLocation.MoveTo(
						UseMesh,
						radius: tag.Radius,
						name: "Stealth Location",
						stopCallback: tag.MovementStopCallback,
						dismountAtDestination: true);

            if (!result) return false;

		    var landed = MovementManager.IsDiving || await NewLandingTask();
		    if (landed && Core.Player.IsMounted)
		        ActionManager.Dismount();

            Navigator.Stop();
            await Coroutine.Yield();

		    await tag.CastAura(Ability.Stealth, AbilityAura.Stealth);

		    result = await NodeLocation.MoveToOnGroundNoMount(tag.Distance, tag.Node.EnglishName, tag.MovementStopCallback);

            return result;
        }

        private async Task<bool> NewLandingTask()
        {
            if (!MovementManager.IsFlying) { return true; }

            var _en = "Mounted"; // Works on all Languages.
            //var _jp = "??"; Don't know Mounted name in JP
            //var _fr = "Sur une monture";
            //var _de = "Beritten";
            //var _cn ="??"; Don't know Mounted name in CN

            // statusoff is persistent thru all versions (CN is unknown).
            ChatManager.SendChat("/statusoff \"" + _en + "\"");

            while (MovementManager.IsFlying) { await Coroutine.Yield(); }

            return true;
        }

        public override string ToString()
		{
			return this.DynamicToString("UnstealthAfter");
		}
	}
}