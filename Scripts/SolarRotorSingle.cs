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

namespace SpaceEngineers.SolarRotorSingle {
  // Control a single rotor to optimize solar panel alignment.
  public sealed class Program : MyGridProgram {
    #endregion

    // The name of the rotor controlling the solar panel array.
    const string RotorName = "Rotor - Solar Boom";
    // The name of a single panel in the array (used for sampling).
    const string PanelName = "Solar Panel 12";
    // How quickly to move the rotor. Adjust sign to match movement of the sun.
    const float SeekVelocity = -0.25f;

    IMyMotorStator rotor;
    IMySolarPanel panel;
    Boolean seek = false;

    public Program() {
      Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update10;
      rotor = FindOneNamed<IMyMotorStator>(RotorName);
      panel = FindOneNamed<IMySolarPanel>(PanelName);
    }

    public void Main(string argument, UpdateType updateSource) {
      var output = panel.MaxOutput * 1000f;
      if (!seek && output < 155f) {
        // Start seek.
        seek = true;
        rotor.TargetVelocityRad = SeekVelocity;
      }
      if (seek && output > 157f) {
        // End seek.
        seek = false;
        rotor.TargetVelocityRad = 0f;
      }
    }

    T FindOneNamed<T>(string name) where T : class, IMyTerminalBlock {
      List<T> xs = new List<T>();
      GridTerminalSystem.GetBlocksOfType<T>(xs, x => x.IsSameConstructAs(Me) && x.CustomName == name);
      if (xs.Count == 0) {
        Echo($@"Error: Missing required block named '{name}'");
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
