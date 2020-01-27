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

namespace SpaceEngineers.ShipPowerStat {
  // Battery stats for ships.
  public sealed class Program : MyGridProgram {
    #endregion

    private const int DisplaySurfaceId = 0;

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    CockpitDisplay display;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
      GridTerminalSystem.GetBlocksOfType(batteries, x => x.IsSameConstructAs(Me));
      display = new CockpitDisplay(FindOne<IMyCockpit>(), DisplaySurfaceId);
    }

    public void Main(string argument, UpdateType updateSource) {
      var stored = Percent.Zero;
      var usage = Percent.Zero;
      foreach (var b in batteries) {
        stored.Add(b.CurrentStoredPower, b.MaxStoredPower);
        usage.Add(b.CurrentOutput, b.MaxOutput);
      }
      var msg = $@"=== POWER SYSTEMS STATUS ===

= Storage: {BarGraph(stored.Percentage())}
{stored.value:0.00} MWh of {stored.max:0.00} MWh

= Usage: {BarGraph(usage.Percentage())}
{usage.value:0.00} MW of {usage.max:0.00} MW";

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
        this.surface.FontColor = new Color(64, 250, 0);
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
