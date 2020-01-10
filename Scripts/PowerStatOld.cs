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

// Change this namespace for each script you create.
namespace SpaceEngineers.PowerStatOld {
  public sealed class Program : MyGridProgram {
    // Your code goes between the next #endregion and #region
    #endregion

    public static class Config {
      public const string Tag = "[PowerStat]";
      public const int InitEvery = 30;
    }

    private bool ready = false;
    private int initCounter = 0;
    private List<IMySolarPanel> panels = null;
    private List<IMyBatteryBlock> batteries = null;
    private List<IMyReactor> reactors = null;
    private List<IMyTextPanel> displays = null;

    // The real meat of the program.
    private void Run(string argument) {
      var batteryIn = 0f;
      var batteryOut = 0f;
      var batteryCap = 0f;
      var batteryStored = 0f;
      ForEach(batteries, b => {
        batteryIn += b.CurrentInput;
        batteryOut += b.CurrentOutput;
        batteryCap += b.MaxStoredPower;
        batteryStored += b.CurrentStoredPower;
      });

      var chargeDelta = batteryIn - batteryOut;
      var capacityDelta = batteryCap - batteryStored;
      var chargeTime = 60f * capacityDelta / chargeDelta; // in mins
      var chargePrct = batteryStored / batteryCap;

      var flowChar = chargeDelta > 0f ? '+' : '-';
      var flowCharCount = (int)Math.Round(10 * chargePrct, 0);
      var flowIndication = "";
      for (int i = 0; i < flowCharCount; i++) {
        flowIndication += flowChar;
      }
      for (int i = 0; i < 10 - flowCharCount; i++) {
        flowIndication += "_";
      }

      var solarOut = 0f;
      var solarMax = 0f;
      ForEach(panels, p => {
        solarOut += p.CurrentOutput;
        solarMax += p.MaxOutput;
      });

      var reactorOut = 0f;
      var reactorMax = 0f;
      ForEach(reactors, r => {
        reactorOut += r.CurrentOutput;
        reactorMax += r.MaxOutput;
      });

      // Format floats.
      //var num = units: string => f: float => Math.Round(f, 2) + " " + units;
      var mw = Format("MW");
      var mwh = Format("MWh");
      var min = Format("min");
      const string outOf = " / ";
      const string pipe = " | ";
      const string eol = "\n";

      var text = "";
      text += "BATTERY SYSTEM STATUS\n";
      text += "In|Out: " + mw(batteryIn) + pipe + mw(batteryOut) + eol;
      text += "Capacity: " + mwh(batteryStored) + outOf + mwh(batteryCap) + eol;
      text += "Status: [" + flowIndication + "] " + min(chargeTime) + eol;

      text += "---------------------\n";
      text += "SOLAR SYSTEM STATUS\n";
      text += "Now|Max: " + mw(solarOut) + pipe + mw(solarMax) + eol;

      text += "---------------------\n";
      text += "REACTOR SYSTEM STATUS\n";
      text += "Now|Max: " + mw(reactorOut) + pipe + mw(reactorMax) + eol;

      ForEach(displays, d => d.WritePublicText(text));
    }

    // Set up the program.
    private void Setup() {
      panels = GetBlocks<IMySolarPanel>();
      batteries = GetBlocks<IMyBatteryBlock>();
      reactors = GetBlocks<IMyReactor>();
      displays = GetBlocksTagged<IMyTextPanel>(Config.Tag);
      ready = true;
    }

    // Main: Do housekeeping and process Setup(), Run(), or both.
    public void Main(string argument) {
      if (initCounter-- <= 0) {
        ready = false;
        initCounter = Config.InitEvery;
      }

      var initOnly = argument.Equals("INIT");
      if (initOnly || !ready) {
        Echo("<Init>");
        Setup();
      }
      if (initOnly || !ready)
        return;

      Echo("<Start>");
      Run(argument);
      Echo("<Done>");
    }

    // Util Functions

    public Func<float, string> Format(string units) {
      return f => Math.Round(f, 2) + " units";
    }

    public void ForEach<T>(List<T> xs, Action<T> f) {
      for (int i = 0; i < xs.Count; i++) {
        T x = xs[i];
        f(x);
      }
    }

    public List<T> GetBlocks<T>() where T : class, IMyTerminalBlock {
      var blocks = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(blocks);
      return blocks;
    }

    public List<T> GetBlocksTagged<T>(string tag) where T : class, IMyTerminalBlock {
      var blocks = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(blocks, b => b.CustomName.Contains(tag));
      return blocks;
    }

    public T GetBlock<T>(string name) where T : class, IMyTerminalBlock {
      var blocks = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(blocks, b => b.CustomName.Equals(name));
      return blocks.Count > 0 ? blocks[0] : null;
    }

    public List<T> GetGroupBlocks<T>(string groupName) where T : class, IMyTerminalBlock {
      var groups = new List<IMyBlockGroup>();
      GridTerminalSystem.GetBlockGroups(groups, b => b.Name.Equals(groupName));
      var blocks = new List<T>();
      if (groups.Count > 0) groups[0].GetBlocksOfType(blocks);
      return blocks;
    }

    #region PreludeFooter
  }
}
#endregion
