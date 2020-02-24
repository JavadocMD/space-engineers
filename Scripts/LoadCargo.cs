#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace SpaceEngineers.UWBlockPrograms.LoadCargo {
  /* Load cargo to spec at the push of a button.
   * Assumes a StorageSystem-compatible cargo arrangement in a connected ship.
   *
   * Configuration is done via Custom Data. Each section is the name of a cargo container (or containers)
   * on the ship which you wish to load, each key in that section is a component type, and each value is
   * the quantity of the component to load. Cargo containers are free to share the same name: they will be
   * loaded identically.
   *
   * e.g.:
   * [Cargo 1]
   * SteelPlate=500
   * InteriorPlate=500
   * 
   * [Cargo 2]
   * SmallTube=150
   * Motor=50
   */
  public sealed class Program : MyGridProgram {
    #endregion

    MyIni ini = new MyIni();

    public Program() { }

    public void Main(string argument, UpdateType updateSource) {
      // Load storage system.
      var storageSystem = new StorageSystem(GridTerminalSystem, Me);

      // Read config.
      MyIniParseResult config;
      if (!ini.TryParse(Me.CustomData, out config)) {
        throw new Exception(config.ToString());
      }
      List<string> sections = new List<string>();
      ini.GetSections(sections); // section == name of a cargo container

      foreach (var s in sections) {
        List<IMyCargoContainer> cargo = new List<IMyCargoContainer>();
        GridTerminalSystem.GetBlocksOfType(cargo, x => x.IsSameConstructAs(Me) && x.CustomName == s);

        foreach (var c in cargo) {
          var inv = c.GetInventory();

          // Empty cargo first. (Easier this way.)
          var deposited = storageSystem.DepositAll(inv);
          if (!deposited) {
            throw new Exception("Failed to empty cargo container: " + c.CustomName);
          }

          // Withdraw all requested items.
          List<MyIniKey> keys = new List<MyIniKey>();
          ini.GetKeys(s, keys);
          foreach (var k in keys) {
            int amount;
            if (!ini.Get(k).TryGetInt32(out amount)) {
              throw new Exception("Unable to read ini value for: " + k);
            }
            var type = MyItemType.MakeComponent(k.Name); // assumes all components only
            var withdrew = storageSystem.Withdraw(inv, type, amount);
            if (!withdrew) {
              throw new Exception($@"Unable to withdraw: {amount.ToString()} {type.ToString()}");
            }
          }
        }
      }
    }

    /* UTILS */

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
