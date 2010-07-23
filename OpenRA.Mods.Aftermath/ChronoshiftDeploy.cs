#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Aftermath.Orders;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Aftermath
{
	class ChronoshiftDeployInfo : TraitInfo<ChronoshiftDeploy>
	{
		public readonly int ChargeTime = 120; // Seconds
	}

	class ChronoshiftDeploy : IIssueOrder, IResolveOrder, ITick, IPips, IOrderCursor
	{
		// Recharge logic
		[Sync]
		int chargeTick = 0; // How long until we can chronoshift again?

		public void Tick(Actor self)
		{
			if (chargeTick > 0)
				chargeTick--;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && xy == self.Location && chargeTick <= 0)
				return new Order("Deploy", self);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.orderGenerator = new SetChronoTankDestination(self);
				return;
			}

			var movement = self.traits.GetOrDefault<IMove>();
			if (order.OrderString == "ChronoshiftSelf" && movement.CanEnterCell(order.TargetLocation))
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				self.CancelActivity();
				self.QueueActivity(new Teleport(order.TargetLocation));
				Sound.Play("chrotnk1.aud", self.CenterLocation);
				Sound.Play("chrotnk1.aud", Game.CellSize * order.TargetLocation.ToFloat2());
				chargeTick = 25 * self.Info.Traits.Get<ChronoshiftDeployInfo>().ChargeTime;

				foreach (var a in self.World.Queries.WithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();
			}
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Deploy") ? "deploy" : null;
		}
		
		// Display 5 pips indicating the current charge status
		public IEnumerable<PipType> GetPips(Actor self)
		{
			const int numPips = 5;
			for (int i = 0; i < numPips; i++)
			{
				if ((1 - chargeTick * 1.0f / (25 * self.Info.Traits.Get<ChronoshiftDeployInfo>().ChargeTime)) * numPips < i + 1)
				{
					yield return PipType.Transparent;
					continue;
				}

				switch (i)
				{
					case 0:
					case 1:
						yield return PipType.Red;
						break;
					case 2:
					case 3:
						yield return PipType.Yellow;
						break;
					case 4:
						yield return PipType.Green;
						break;
				}
			}
		}
	}
}
