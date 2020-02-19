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

namespace SpaceEngineers.Airlock {
  // Script to toggle airlock state.
  public sealed class Program : MyGridProgram {
    #endregion

    const string InnerDoorName = "Door - Airlock 1 Inner";
    const string OuterDoorName = "Door - Airlock 1 Outer";
    const string AirVentName = "Air Vent - Airlock 1";

    const float Epsilon = 0.000001f;
    IMyDoor innerDoor;
    IMyDoor outerDoor;
    IMyAirVent airVent;

    Executor<object> executor;

    public Program() {
      innerDoor = FindOneNamed<IMyDoor>(InnerDoorName);
      outerDoor = FindOneNamed<IMyDoor>(OuterDoorName);
      airVent = FindOneNamed<IMyAirVent>(AirVentName);
    }

    public void Main(string argument, UpdateType updateSource) {
      if (executor == null) {
        executor = new Executor<object>();
      }
      // On START when idle: trigger execution.
      var hasNext = false;
      if (argument == "START") {
        executor.Execute(ToggleAirlock);
        hasNext = true;
      } else if ((updateSource & UpdateType.Once) == UpdateType.Once) {
        hasNext = executor.Update();
      }
      // Continue to tick.
      if (hasNext) {
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
      }
    }

    IEnumerator<object> ToggleAirlock() {
      // Close both doors and wait for closed.
      innerDoor.CloseDoor();
      outerDoor.CloseDoor();
      var closed = false;
      while (!closed) {
        closed = innerDoor.Status == DoorStatus.Closed && outerDoor.Status == DoorStatus.Closed;
        yield return null;
      }

      // Setup target state.
      var goingOut = !airVent.Depressurize;
      var targetO2 = goingOut ? 0f : 1f;

      // Set vent and wait for target O2 level.
      airVent.Depressurize = !airVent.Depressurize;
      var done = false;
      while (!done) {
        // Note: vent status seems to never reach Depressurized. Use O2 level instead.
        done = Math.Abs(airVent.GetOxygenLevel() - targetO2) < Epsilon;
        yield return null;
      }

      // Open our exit door.
      var exitDoor = goingOut ? outerDoor : innerDoor;
      exitDoor.OpenDoor();
    }

    /* UTILS */

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

    class Executor<T> {
      IEnumerator<T> task = null;

      public bool Update() {
        if (task == null) {
          return false;
        }
        var hasNext = task.MoveNext();
        if (!hasNext) {
          task.Dispose();
          task = null;
        }
        return hasNext;
      }

      public void Execute(Func<IEnumerator<T>> task) {
        if (this.task == null) {
          this.task = task();
        }
      }
    }

    #region PreludeFooter
  }
}
#endregion
