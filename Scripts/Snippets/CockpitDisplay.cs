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

// Additional imports.
using VRage.Game.GUI.TextPanel;

namespace SpaceEngineers.CockpitDisplay {
	// Sets up a cockpit display.
	public sealed class Program : MyGridProgram {
		#endregion

		public Program() {
			// Example:
			var cockpit = FindOne<IMyCockpit>();
			var display = new CockpitDisplay(cockpit, 0);
			display.Write("hello space");
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
