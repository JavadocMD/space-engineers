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

namespace SpaceEngineers.ShipSystemStat {
  // Critical system stats for space ships.
  // Like ShipPowerStat, but count O2 as well.
  public sealed class Program : MyGridProgram {
    #endregion

    const int DisplaySurfaceId = 0;

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    List<IMyGasTank> o2Tanks = new List<IMyGasTank>();
    CockpitDisplay display;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      GridTerminalSystem.GetBlocksOfType(o2Tanks, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == "OxygenTankSmall");
      display = new CockpitDisplay(FindOne<IMyCockpit>(), DisplaySurfaceId);
    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = Percent.Zero;
      var usage = Percent.Zero;
      var o2 = Percent.Zero;
      foreach (var x in batteries) {
        stored.Add(x.CurrentStoredPower, x.MaxStoredPower);
        usage.Add(x.CurrentOutput, x.MaxOutput);
      }
      foreach (var x in o2Tanks) {
        o2.Add((float)(x.Capacity * x.FilledRatio), x.Capacity);
      }
      var msg = $@"=== SYSTEMS STATUS ===

= Battery: {BarGraph(stored.Percentage())}
{stored.value:0.00} MWh of {stored.max:0.00} MWh

= Power: {BarGraph(usage.Percentage())}
{usage.value:0.00} MW of {usage.max:0.00} MW

= Oxygen: {BarGraph(o2.Percentage())}
{o2.value:0.0} kL of {o2.max:0.0} kL";

      display.Write(msg);
    }

    // Utils

    struct Percent {
      public float value;
      public float max;

      public static Percent Zero = new Percent() {
        value = 0f,
        max = 0f
      };

      public void Add(float value, float max) {
        this.value += value;
        this.max += max;
      }

      public float Percentage() {
        return 100 * value / max;
      }
    }

    string BarGraph(float pct) {
      var s = "[";
      var i = 0f;
      for (; i < pct; i += 12.5f) {
        s += "#";
      }
      for (; i < 100; i += 12.5f) {
        s += "_";
      }
      s += $@"] ({pct:0}%)";
      return s;
    }

    T FindOne<T>() where T : class, IMyTerminalBlock {
      List<T> xs = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(xs, x => x.IsSameConstructAs(Me));
      if (xs.Count == 0) {
        Echo($@"Error: Missing required block of type {nameof(T)}");
        Me.Enabled = false;
        return null;
      } else {
        return xs[0];
      }
    }

    class CockpitDisplay {
      IMyTextSurface surface;

      public CockpitDisplay(IMyCockpit cockpit, int surface) {
        this.surface = cockpit.GetSurface(surface);
        this.surface.ContentType = ContentType.TEXT_AND_IMAGE;
        this.surface.BackgroundColor = new Color(12, 12, 12);
        this.surface.FontSize = 0.9f;
        this.surface.FontColor = new Color(65, 250, 0);
        this.surface.Font = "Monospace";
      }

      public void Write(string text) {
        this.surface.WriteText(text, false);
      }
    }

    #region PreludeFooter
  }
}
#endregion
