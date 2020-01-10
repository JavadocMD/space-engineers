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

namespace SpaceEngineers.Daylight {
  public sealed class Program : MyGridProgram {
    #endregion

    public static class Config {
      // A group containing three axis-aligned solar panels to use as sensors. 
      public const string TestPanelsGroupName = "Daylight - Panels";
      public const float NightPowerThreshold = 0.010f;
      public const string DayTimerName = "Daylight - Day Timer";
      public const string NightTimerName = "Daylight - Night Timer";
    }

    public enum Time {
      Unknown, Day, Night
    }

    private bool ready = false;
    private Time wasTime = Time.Unknown;
    private List<IMySolarPanel> panels;
    private IMyTimerBlock dayTimer;
    private IMyTimerBlock nightTimer;

    public void Main(string argument) {
      var initOnly = argument.Equals("INIT");
      if (initOnly || !ready)
        Setup();
      if (initOnly || !ready)
        return;

      float output = 0f;
      for (int i = 0; i < panels.Count; i++) {
        output += panels[i].CurrentOutput;
      }

      var isTime = output >= Config.NightPowerThreshold ? Time.Day : Time.Night;
      if (isTime != wasTime) {
        var timer = isTime == Time.Day ? dayTimer : nightTimer;
        timer.GetActionWithName("Run Now").Apply(timer);
        wasTime = isTime;
      }
      Echo(isTime.ToString() + " (" + Math.Round(output, 3) + " MW)");
    }

    private void Setup() {
      Echo("<Init>");
      panels = GetGroupBlocks<IMySolarPanel>(Config.TestPanelsGroupName);
      dayTimer = GetBlock<IMyTimerBlock>(Config.DayTimerName);
      nightTimer = GetBlock<IMyTimerBlock>(Config.NightTimerName);

      ready = true;
      if (panels.Count == 0) {
        Echo("ERROR: Could not find test solar panels.");
        ready = false;
      }
      if (dayTimer == null) {
        Echo("ERROR: Could not find day timer.");
        ready = false;
      }
      if (nightTimer == null) {
        Echo("ERROR: Could not find night timer.");
        ready = false;
      }
    }

    // Util Functions

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
