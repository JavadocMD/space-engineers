#region Prelude
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Executor {
  // Executor for IEnumerator. Guarantees:
  // 1. only one IEnumerator will run at a time, and
  // 2. it will run until it completes (no cancels).
  public sealed class Program : MyGridProgram {
    #endregion

    /* SAMPLE BOILERPLATE */

    Executor<object> executor;

    public void Main(string argument, UpdateType updateSource) {
      if (executor == null) {
        executor = new Executor<object>();
      }
      // On START when idle: trigger execution.
      var hasNext = false;
      if (argument == "START") {
        executor.Execute(MyFunctionThatReturnsIEnumerator);
        hasNext = true;
      } else if ((updateSource & UpdateType.Once) == UpdateType.Once) {
        hasNext = executor.Update();
      }
      // Continue to tick?
      if (hasNext) {
        Runtime.UpdateFrequency |= UpdateFrequency.Once;
      }
    }

    IEnumerator<object> MyFunctionThatReturnsIEnumerator() {
      yield return null;
    }

    /* THE ACTUAL UTIL */

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
