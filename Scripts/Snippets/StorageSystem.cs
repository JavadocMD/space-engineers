#region Prelude
using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace SpaceEngineers.StorageSystem {
  /* Manager class for external storage systems: a set of cargo containers for the storage of
   * ore, ignots, and components, typically on a station or mothership.
   *
   * In order to qualify, the containers must be external -- not part of the current grid ("me").
   * There must also be at least one of each named:
   *   "Storage - Ore",
   *   "Storage - Ingot", and
   *   "Storage - Component".
   *
   * The class then facilitates making withdrawals and deposits so as to keep items sorted neatly.
   */
  public sealed class Program : MyGridProgram {
    #endregion

    class StorageSystem {
      private List<IMyInventory> ore;
      private List<IMyInventory> ingot;
      private List<IMyInventory> component;

      public StorageSystem(IMyGridTerminalSystem grid, IMyTerminalBlock me) {
        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
        grid.GetBlocksOfType(containers, x => !x.IsSameConstructAs(me));

        this.ore = containers.Where(x => x.CustomName == "Storage - Ore").Select(x => x.GetInventory()).ToList();
        this.ingot = containers.Where(x => x.CustomName == "Storage - Ingot").Select(x => x.GetInventory()).ToList();
        this.component = containers.Where(x => x.CustomName == "Storage - Component").Select(x => x.GetInventory()).ToList();

        if (this.ore.Count() == 0 || this.ingot.Count() == 0 || this.component.Count() == 0) {
          throw new Exception("Unable to initialize storage system. Missing containers.");
        }
      }

      private List<IMyInventory> GetCategory(MyItemType itemType) {
        if (itemType.TypeId == "MyObjectBuilder_Ingot") {
          return ingot;
        } else if (itemType.TypeId == "MyObjectBuilder_Ore") {
          return ore;
        } else {
          return component;
        }
      }

      private IMyInventory FindBest(List<IMyInventory> containers, MyItemType itemType, VRage.MyFixedPoint amount) {
        // Prefer container which has that type of item and enough space.
        var inv = containers.Find(x => x.FindItem(itemType) != null && x.CanItemsBeAdded(amount, itemType));
        if (inv == null) {
          // Fall back to just having enough space.
          inv = containers.Find(x => x.CanItemsBeAdded(amount, itemType));
        }
        return inv;
      }

      public bool DepositAll(IMyInventory src) {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        src.GetItems(items);
        var success = true;
        foreach (var i in items) {
          success &= Deposit(src, i);
        }
        return success;
      }

      public bool Deposit(IMyInventory src, MyInventoryItem item, VRage.MyFixedPoint? amount = null) {
        var category = GetCategory(item.Type);
        var dst = FindBest(category, item.Type, amount ?? item.Amount);
        if (dst == null) {
          return false;
        }
        return src.TransferItemTo(dst, item, amount);
      }

      public bool Withdraw(IMyInventory dst, MyItemType itemType, VRage.MyFixedPoint amount) {
        foreach (var inv in GetCategory(itemType)) {
          var maybe = inv.FindItem(itemType);
          if (maybe.HasValue) {
            var item = maybe.Value;
            if (item.Amount >= amount) {
              inv.TransferItemTo(dst, item, amount);
              return true;
            }
          }
        }
        return false;
      }
    }

    #region PreludeFooter
  }
}
#endregion
