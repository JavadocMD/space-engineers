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

namespace SpaceEngineers.LaunchController {
  // Manage the planet-to-orbit launch of a small ship.
  public sealed class Program : MyGridProgram {
    #endregion

    const double Epsilon = 0.000001;
    const double SafetyThrust = 0.01; // extra thrust when maintaining velocity for safety (percentage of total thrust)

    List<IMyThrust> thrusters = new List<IMyThrust>();
    IMyCockpit cockpit;

    bool running = false;
    double maxThrust = 0;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update100;
      // Assume all large thrusters are involved.
      GridTerminalSystem.GetBlocksOfType(thrusters, x => x.IsSameConstructAs(Me) && x.BlockDefinition.SubtypeName == "SmallBlockLargeHydrogenThrust");
      foreach (var x in thrusters) {
        x.Enabled = true;
        maxThrust += x.MaxEffectiveThrust;
      }
      // Assume we have one Cockpit as ship controller.
      cockpit = FindOne<IMyCockpit>();
    }

    // Start the launch sequence by powering on this script block.
    public void Main(string argument, UpdateType updateSource) {
      // Initialize.
      if (!running) {
        cockpit.DampenersOverride = false;
        foreach (var x in thrusters) {
          x.Enabled = true;
        }
        running = true;
      }

      // Run until we are out of the gravity well, then switch off.
      var gravity = cockpit.GetNaturalGravity().Length();
      if (argument == "STOP" || gravity < Epsilon) {
        // Reset managed state and power off.
        running = false;
        foreach (var x in thrusters) {
          x.ThrustOverridePercentage = 0f;
        }
        cockpit.DampenersOverride = true;
        Me.Enabled = false;
        return;
      }

      // Modulate thrust.
      var mass = (double)cockpit.CalculateShipMass().PhysicalMass;
      var speed = cockpit.GetShipSpeed();
      var thrustOverride = 1f;
      if (speed >= 99) {
        thrustOverride = (float)Math.Min(1, (mass * gravity / maxThrust) + SafetyThrust);
      }
      foreach (var x in thrusters) {
        x.ThrustOverridePercentage = thrustOverride;
      }
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

    #region PreludeFooter
  }
}
#endregion
