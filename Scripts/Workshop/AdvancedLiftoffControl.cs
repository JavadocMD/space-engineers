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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.AdvancedLiftoffControl {
  // Advanced Liftoff Control by lye
  // https://steamcommunity.com/sharedfiles/filedetails/?id=637302443
  public sealed class Program : MyGridProgram {
    #endregion

    //This script allows for automated override management for planetary liftoff to save hydrogen and power.
    //Capable of controlling arbitrary set of thrusters, it will by default automatcally find thrusters that make sense to be controlled together and use those to lift off.
    //Requires a programmable, a ship controller and of course thrusters

    /************************************************************************************************************************************
        Quick setup: (e.g. autoGenerate==1)
        1. Build Your ship.
        2. Claim ownership on all blocks
        3. Take control of a ship controller that contains the "controlBlockName" string, defined below (default "_liftoff") in its name. Make sure this controller is the MAIN COCKPIT
        4. Have more than 0 hydrogen thrusters, make sure their display names contain directions (e.g. 'Up'). This is ensured by correctly following step 3.
           Note that the direction tag is relative to the seat/control you are in.
        (5. Run script to start.)

        Advanced setup: (e.g. autoGenerate==0)
        0. DO ALL STEPS FROM THE QUICK GUIDE (except for the run xD)
        1. Put thrusters you want to control in a group whichs name is specified below (see ascentHydroGroup, ascentAtmosphericGroup,...)
        2. If you have more than one control block and want to use a specific one, set its name below.
        3. Have blocks named as specified in TriggerOnStart, TriggerOnFinish and TriggerMidFlight, which will be trigger at specific stages.
        4. have a display named as specified below, have which text the script should write to,
           and have the display set to display the correct text(probably)
        5. adjust all settings to your linkings. (no warranties at all if settings are changed)
        (6. have a display named as in lcddebugName for debugging/spammed screens, looks nice :P)

        How to setup LCDs
        * Have a section [liftoff] in there
        * under that, have a line "N=TYPE" to show either information on the screen with index N. Note that LCDs only have one screen, hence their index is always 0
        * TYPE can be: "main" for the main output, "log"/"debug" for the debug log, "thrust" for the thruster collection information

        To manually stop, run the script with the argument "STOP" (case sensitive).
        Note that if dont have it set to (re)enable Inertias when doing that your ship might make a uncomfortable landing

        To reset the script, run it with the argument "INIT" (case sensitive). This will reset all data and generate all listings again, just like a recompile would.
        **/
    /************************************************************************************************************************************
    Additional Information for gravity drive users:
     if you want to use your gravity drive after some height has been reached (aka artificial gravity is a thing again)
      - set the gravityTreshold to a value where your gravity drive can lift the ship
      - set enableDampenersOnStop to false (otherwise your ship will start braking)
      - set TriggerOnFinish to the name of a Timer block which you use as a relay to enable your gravity drive.
      - fly using magic.

    *************************************************************************************************************************************/


    const string version = "6.2";
    /**SETTINGS
        modify those to your needs, dont modify settings you dont understand,
        AND keep your numeric changes reasonable, e.g. within the same order of magnitude as the original settings for factors.
        **/
    const int autoGenerate = 1;                                     //Try and perform an automatic setup of thrusters, and possibly other blocks:
                                                                    //0 == No, 1 == thruster only 2 == "everything", also adding to thrust groups if non grouped thrusters are found
                                                                    //autoGenerate will ALWAYS preferr set groups and not add found blocks to ascent Groups. It will however control steering thrusters
    const string controlBlockName = "_liftoff";                     //part of the name of the control block to be used.		/**(REQUIRED)**/
    const string MainThrustVectorMarker = "Up";                     //Main direction, requires a main cockpit.ENGLISH		/**(not required at autoGenerate < 1)**/
    const string ascentHydroGroup = "HydrogenMainVector";           //Group with liftoff hydrogens thrusters 				/**(not required at autoGenerate > 0)**/
    const string ascentAtmosphericGroup = "AtmosphericMainVector";  //Group with liftoff Atmospheric thrusters 				/**(not required)**/
    const string ascentIonGroup = "IonMainVector";                  //Group with liftoff Ion thrusters  					/**(not required)**/
    const string steeringHydroGroup = "HydroSteerGroup";            //Group steering hydro thrusters 						/**(not required)**/
    const string steeringAtmoGroup = "AtmoSteerGroup";              //Group steering atmo thrusters 						/**(not required)**/
    const string steeringIonGroup = "IonSteerGroup";                //Group steering ion thrusters 							/**(not required)**/

    const string TriggerOnStart = "TriggerOnStart";                 //trigger this block when script starts 				/**(not required)**/
    const string TriggerOnFinish = "TriggerOnFinish";               //trigger this when script stops 						/**(not required)**/
    const string TriggerMidFlight = "TriggerMidFlight";             //trigger this at specified g							/**(not required)**/
    const string TriggerOnAtmoCutoff = "TriggerOnAtmoCutoff";       //trigger this when the atmospheric thrusters go offline/**(not required)**/
    const string TriggerOnHydroCutoff = "TriggerOnHydroCutoff";     //trigger this when the hydrogen thrusters go offline	/**(not required)**/
    const string TriggerOnWarning = "warning_liftoff";              //trigger this when an notable event happens.			/**(not required)**/

    const string lcdGroup = "lcd_liftoff";                //name of a group of LCDs you want the main output on	/**(not required)**/
    const string lcdConfigTag = "liftoff";                          //tag to be used in the custom data of display blocks   /**(not required)**/
                                                                    //if you use custom data to configure your display setup
                                                                    //use this tag. See the guide.

    //DATA SETTINGS
    const double targetSpeed = 100.0;           //Scripts approximate targetspeed (basically set this to your worlds speedlimit for the most efficient flight. NOTE: this is dependent on the sim speed
    const double gravityTreshold = .05;         //Specifies at how many g the script will stop. Natural gravity cuts off at .05g, but you can set this to a higher value if you want the script to finish early.
    const double midFlightTreshold = .5;        //Specifies at how many g the TriggerMidFlight will be triggered.
    const bool disableDampenersOnStart = true;  //Turn off dampeners on start?
    const bool enableDampenersOnStop = true;    //Turn on dampeners on stop?

    //debug settings
    const bool cleardebug = true;               // if true, log will be cleared every 20 lines. otherwise it will just be appended. Note that 20 lines are the setting for a regular LCD.

    //additional thruster settings. Use these if autodetect cant identify your thrusters via their subtype. You should never need to use these unless u have strange modded thrusters.
    const string HydroMarker = "Hydrogen";                          //String that indicates Hydros 							/**(not required at autoGenerate < 1)**/
    const string AtmoMarker = "Atmospheric";                        //String that indicates Atmos 							/**(not required at autoGenerate < 1)**/
    const string IonMarker = "Ion";                                 //String that indicates Ions 							/**(not required at autoGenerate < 1)**/


    //sadly this is no longer allowed due to exploits.
    //#pragma warning disable 0162 //No more pesky unreachable code warnings :)

    //constans
    const double HalfPi = Math.PI / 2;
    const double RadToDeg = 180 / Math.PI;
    const double DegToRad = Math.PI / 180;
    const double Earthg = 9.8;

    const string debugFmtString = "{0,-5} {1,1} {2,5:0.0} {3,7} {4,7} {5,7} {6,7} {7,-4:.00}\n";
    //"tick", "s", "v", "weight", "Th_Hydro", "Th_Atmo", "Th_Ion", "OR");

    //magic. we turn the user input string for the thrust direction into a proper object so we can compare it.
    Vector3I MainDir = MainThrustVectorMarker == "Up" ? Vector3I.Down
        : MainThrustVectorMarker == "Down" ? Vector3I.Up
        : MainThrustVectorMarker == "Forward" ? Vector3I.Backward
        : MainThrustVectorMarker == "Backward" ? Vector3I.Forward
        : MainThrustVectorMarker == "Left" ? Vector3I.Right
        : MainThrustVectorMarker == "Right" ? Vector3I.Left
        : Vector3I.Zero;
    //vars
    double lastSpeed = 0;
    double continousTime = 0;
    Vector3 Pos, lastPos, startPos, initPos, midFlightPos, atmoPos, hydroPos, warningPos;
    double Time, lastTime, startTime, initTime, midFlightTime, atmoTime, hydroTime, warningTime;
    bool juststarted = true;
    bool initdone = false;
    int tickCount = 0;
    int stage;
    int lastStage;
    //int pVeloSign = 0;
    int debugLineCount = 0;
    bool angleblink = false;
    string Output;
    string debugheader;

    //blocks
    IMyShipController controller;
    IMyFunctionalBlock startblock, finishblock, midflightblock, atmoBlock, hydroBlock, warningBlock;

    private readonly SmartScreens screens;

    private ThrusterGroup Thruster_Hydro_Main = new ThrusterGroup(),
      Thruster_Hydro_Dir = new ThrusterGroup(),
      Thruster_Atmo_Main = new ThrusterGroup(),
      Thruster_Atmo_Dir = new ThrusterGroup(),
      Thruster_Ion_Main = new ThrusterGroup(),
      Thruster_Ion_Dir = new ThrusterGroup();

    private readonly List<IMyTerminalBlock> Ablocks = new List<IMyTerminalBlock>();

    private readonly Dictionary<int, string> SIPrefixDict = new Dictionary<int, string>
    {
  { -5, "f" },
  { -4, "p" },
  { -3, "n" },
  { -2, "\u03bc" },
  { -1, "m" },
  { 0, " " },
  { 1, "k" },
  { 2, "M" },
  { 3, "G" },
  { 4, "T" },
  { 5, "E" }
};

    // ***********************************

    void EnabledRelevantThrusters() {
      Thruster_Hydro_Main.Enabled = stage > 2 && stage < 6;
      Thruster_Hydro_Dir.Enabled = stage > 2 && stage < 6;
      Thruster_Atmo_Main.Enabled = stage < 4;
      Thruster_Atmo_Dir.Enabled = stage < 4;
      Thruster_Ion_Main.Enabled = stage > 4;
      Thruster_Ion_Dir.Enabled = stage > 4;
    }

    void Stop() {
      Thruster_Hydro_Main.Override = 0;
      Thruster_Atmo_Main.Override = 0;
      Thruster_Ion_Main.Override = 0;

      EnabledRelevantThrusters();

      juststarted = true; //reset for next startup

      Output += "Dampeners: " + (enableDampenersOnStop ? "ON" : "OFF");
      if (enableDampenersOnStop)  //enable inertias if desired
      { SetInertiaDampeners(true); }

      Runtime.UpdateFrequency = UpdateFrequency.None;
    }

    void Warn() {
      TriggerBlock(warningBlock);
      warningTime = continousTime;
      warningPos = Pos;
    }

    void SetInertiaDampeners(bool state) {
      List<IMyTerminalBlock> shipControllers = new List<IMyTerminalBlock>();
      GridTerminalSystem.GetBlocksOfType<IMyShipController>(shipControllers);

      foreach (IMyShipController C in shipControllers) { C.DampenersOverride = state; }
    }

    void TriggerBlock<T>(T block)
      where T : IMyTerminalBlock, IMyFunctionalBlock {
      if (block == null) { return; } else if (block is IMyLightingBlock) { block.Enabled ^= true; } else if (block is IMySoundBlock) { ((IMySoundBlock)block).Play(); } else if (block is IMyTimerBlock) { ((IMyTimerBlock)block).StartCountdown(); } else if (block is IMyProgrammableBlock) { ((IMyProgrammableBlock)block).TryRun(""); } else if (block is IMyShipMergeBlock) { block.Enabled ^= true; ; } else if (block is IMyDoor) { ((IMyDoor)block).ToggleDoor(); } else if (block is IMyWarhead) { ((IMyWarhead)block).Detonate(); }
    }

    bool CleanBlockLists() {
      Thruster_Atmo_Main.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));
      Thruster_Atmo_Dir.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));
      Thruster_Hydro_Main.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));
      Thruster_Hydro_Dir.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));
      Thruster_Ion_Main.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));
      Thruster_Ion_Dir.RemoveAll(x => x == null || !x.IsSameConstructAs(Me));

      screens.Cleanup();

      return Thruster_Hydro_Main.Any() || Thruster_Atmo_Main.Any() || Thruster_Ion_Main.Any();
    }

    int GenerateThrusterLists() {
      List<IMyThrust> Thruster_list = new List<IMyThrust>();

      // get thrusters from groups
      List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
      GridTerminalSystem.GetBlockGroups(groups);
      foreach (var g in groups) {
        if (g.Name.Contains(ascentHydroGroup)) {
          screens.WriteText("thrusters", append: true, things: g.Name + " detected as Main Hydro Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Hydro_Main = new ThrusterGroup(Thruster_list);
        } else if (g.Name.Contains(steeringHydroGroup)) {
          screens.WriteText("thrusters", append: true, things: g.Name + " detected as Dir Hydro Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Hydro_Dir = new ThrusterGroup(Thruster_list);
        } else if (g.Name.Contains(ascentAtmosphericGroup)) {
          screens.WriteText("thrusters", append: true, things: " detected as Main Atmo Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Atmo_Main = new ThrusterGroup(Thruster_list);
        } else if (g.Name.Contains(steeringAtmoGroup)) {
          screens.WriteText("thrusters", append: true, things: g.Name + " detected as Dir Atmo Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Atmo_Dir = new ThrusterGroup(Thruster_list);
        } else if (g.Name.Contains(ascentIonGroup)) {
          screens.WriteText("thrusters", append: true, things: g.Name + " detected as Main Ion Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Ion_Main = new ThrusterGroup(Thruster_list);
        } else if (g.Name.Contains(steeringIonGroup)) {
          screens.WriteText("thrusters", append: true, things: g.Name + " detected as Dir Ion Group");
          g.GetBlocksOfType(Thruster_list);
          Thruster_Ion_Dir = new ThrusterGroup(Thruster_list);
        }
      }

      //output thrusters found in groups
      string text = "FROM GROUPS:";
      bool ThrustersFromGroups = Thruster_Hydro_Main.Any() || Thruster_Hydro_Dir.Any() || Thruster_Atmo_Main.Any() || Thruster_Atmo_Dir.Any() || Thruster_Ion_Main.Any() || Thruster_Ion_Dir.Any();
      if (ThrustersFromGroups) {
        if (Thruster_Hydro_Main.Any()) {
          text += "\n  Main Hydros (" + Thruster_Hydro_Main.NElements + ") :";
          foreach (var b in Thruster_Hydro_Main) { text += "\n    " + b.CustomName; }
        }

        if (Thruster_Hydro_Dir.Any()) {
          text += "\n  Dir Hydros (" + Thruster_Hydro_Main.NElements + ") :";
          foreach (var b in Thruster_Hydro_Dir) { text += "\n    " + b.CustomName; }
        }

        if (Thruster_Atmo_Main.Any()) {
          text += "\n  Main Atmos (" + Thruster_Atmo_Main.NElements + ") :";
          foreach (var b in Thruster_Atmo_Main) { text += "\n    " + b.CustomName; }
        }

        if (Thruster_Atmo_Dir.Any()) {
          text += "\n  Dir Atmos (" + Thruster_Atmo_Dir.NElements + ") :";
          foreach (var b in Thruster_Atmo_Dir) { text += "\n    " + b.CustomName; }
        }

        if (Thruster_Ion_Main.Any()) {
          text += "\n  Main Ions (" + Thruster_Ion_Main.NElements + ") :";
          foreach (var b in Thruster_Ion_Main) { text += "\n    " + b.CustomName; }
        }

        if (Thruster_Ion_Dir.Any()) {
          text += "\n  Dir Ions (" + Thruster_Ion_Dir.NElements + ") :";
          foreach (var b in Thruster_Ion_Dir) { text += "\n    " + b.CustomName; }
        }
      } else { text += "   NONE\n"; }

      screens.WriteText("thrusters", append: true, EchoOnFail: true, things: text);

      Thruster_Hydro_Main.Update();
      Thruster_Atmo_Main.Update();
      Thruster_Ion_Main.Update();

      if (autoGenerate > 0) // IF AUTOGENERATE
      {
        if (controller == null) {
          screens.WriteText("main", append: false, EchoOnFail: true, things: "ERROR: no controller given. You need a cockpit containing \"" + controlBlockName + "\" in its name");
          screens.WriteText("log", append: true, things: "\nERROR: controller is null.");
          screens.WriteText("thrusters", append: true, things: "\nERROR: controller is null.");
          return 1;
        }
        if (!controller.IsUnderControl) {
          string err = "ERROR: controller " + controller.CustomName + " is not controlled";
          screens.WriteText("main", append: false, EchoOnFail: true, things: err);
          screens.WriteText("log", append: true, things: "\n" + err);
          screens.WriteText("thrusters", append: true, things: "\n" + err);
          return 1;
        }

        bool savestate = controller.IsMainCockpit;
        controller.IsMainCockpit = true;

        GridTerminalSystem.GetBlocksOfType<IMyThrust>(Ablocks);
        //WriteOut("AUTOMATICALLY COLLECTED:\n", l_display: l_lcd_thrst, append: true, EchoOnFail: true);
        text = "AUTOMATICALLY COLLECTED:\n";

        foreach (IMyThrust ThisThruster in Ablocks) {
          string cname = ThisThruster.CustomName;
          string ThisThrusterSubtype = ThisThruster.BlockDefinition.TypeIdString;

          text += String.Format("{0} ({1})", cname, FormatV3I(ThisThruster.GridThrustDirection));
          if (ThisThruster.GridThrustDirection == MainDir) //CASE: is in thrust direction
          {
            if (ThisThrusterSubtype.Contains("Hydro") || cname.Contains(HydroMarker)) //its a hydro
            {
              if (Thruster_Hydro_Main.Any() && autoGenerate > 1) { continue; }
              Thruster_Hydro_Main.Add(ThisThruster);
              text += "-> Main Hydro\n";
            } else if (ThisThrusterSubtype.Contains("Atmo") || cname.Contains(AtmoMarker)) //its a atmo
              {
              if (Thruster_Atmo_Main.Any() && autoGenerate > 1) { continue; }
              Thruster_Atmo_Main.Add(ThisThruster);
              text += "-> Main Atmo\n";
            } else if (ThisThrusterSubtype.Contains("Ion") || cname.Contains(IonMarker)) //its an ion
              {
              if (Thruster_Ion_Main.Any() && autoGenerate > 1) { continue; }
              Thruster_Ion_Main.Add(ThisThruster);
              text += "-> Main Ion\n";
            } else //it fucked up. make it a main ion. Reason: some (old?) ion thrusters dont report they are ion thrusters.
              {
              Thruster_Ion_Main.Add(ThisThruster);
              text += " : " + ThisThrusterSubtype + " ?> Main Ion\n";
            }
          } else //CASE: is not in thrust direction
            {
            if (ThisThrusterSubtype.Contains("Hydro") || cname.Contains(HydroMarker)) //its a hydro
            {
              Thruster_Hydro_Dir.Add(ThisThruster);
              text += "-> Dir Hydro\n";
            } else if (ThisThrusterSubtype.Contains("Atmo") || cname.Contains(AtmoMarker)) //its a atmo
              {
              Thruster_Atmo_Dir.Add(ThisThruster);
              text += "-> Dir Atmo\n";
            } else if (ThisThrusterSubtype.Contains("Ion") || cname.Contains(IonMarker)) //its an ion
              {
              Thruster_Ion_Dir.Add(ThisThruster);
              text += "-> Dir Ion\n";
            } else  //it fucked up. make it a dir ion.
              {
              Thruster_Ion_Dir.Add(ThisThruster);
              text += " : " + ThisThrusterSubtype + " ?> Dir Ion\n";
            }
          }
        }
        screens.WriteText("thrusters", append: true, EchoOnFail: true, things: text);

        controller.IsMainCockpit = savestate;
      }
      Ablocks.Clear();

      return 0;
    }

    int GenerateBlockLists() {
      int err = 0;

      startblock = GridTerminalSystem.GetBlockWithName(TriggerOnStart) as IMyFunctionalBlock;
      finishblock = GridTerminalSystem.GetBlockWithName(TriggerOnFinish) as IMyFunctionalBlock;
      midflightblock = GridTerminalSystem.GetBlockWithName(TriggerMidFlight) as IMyFunctionalBlock;
      atmoBlock = GridTerminalSystem.GetBlockWithName(TriggerOnAtmoCutoff) as IMyFunctionalBlock;
      hydroBlock = GridTerminalSystem.GetBlockWithName(TriggerOnHydroCutoff) as IMyFunctionalBlock;
      warningBlock = GridTerminalSystem.GetBlockWithName(TriggerOnWarning) as IMyFunctionalBlock;

      screens.BuildDatabase();
      screens.ConfigureAll(ContentType.TEXT_AND_IMAGE, "Monospace");
      screens.screens["main"].FontSize = .75f;
      screens.screens["log"].FontSize = .5f;
      screens.screens["thrusters"].FontSize = .75f;


      //CONTROLLER
      controller = (IMyShipController)GridTerminalSystem.GetBlockWithName(controlBlockName);
      if (controller == null) {
        GridTerminalSystem.GetBlocksOfType<IMyShipController>(Ablocks, x => x.CustomName.Contains(controlBlockName));
        if (Ablocks.Any()) { controller = Ablocks[0] as IMyShipController; }
      }

      //controller fallback
      if (autoGenerate > 1 && controller == null) {
        GridTerminalSystem.GetBlocksOfType<IMyShipController>(Ablocks);

        foreach (IMyShipController c in Ablocks) {
          if (c.IsMainCockpit) {
            controller = c;
            screens.WriteText("main", append: true, EchoOnFail: true, things: "Warning: no contol block given, \n\tfalling back to " + controller.CustomName);
            screens.WriteText("log", append: true, EchoOnFail: true, things: "Warning: no contol block given, \n\tfalling back to " + controller.CustomName);
          }
        }
      }
      Ablocks.Clear();

      err = GenerateThrusterLists();

      return err;
    }

    int Init() {
      if (autoGenerate > 0 && MainDir == Vector3I.Zero) {
        screens.WriteText("main", append: true, EchoOnFail: true, things: "Error in Main Dir. Could not resove \"" + MainThrustVectorMarker + "\"");
        return 1;
      }

      if (GenerateBlockLists() > 0) { return 1; }

      atmoPos = Vector3D.Zero;
      hydroPos = Vector3D.Zero;
      lastSpeed = 0;
      stage = 0;

      // setup debug header if we have a debug display
      debugheader = "ALCv" + version + "||";
      debugheader += ";AG=" + autoGenerate.ToString();
      debugheader += ";sb=" + ((startblock != null) ? "1" : "0");
      debugheader += ";mb=" + ((midflightblock != null) ? "1" : "0");
      debugheader += ";fb=" + ((finishblock != null) ? "1" : "0");
      debugheader += ";MV=" + MainThrustVectorMarker + "=" + FormatV3I(MainDir);
      debugheader += "\n";
      debugheader += ";#AM=" + Thruster_Atmo_Main.NElements.ToString();
      debugheader += ";#AD=" + Thruster_Atmo_Dir.NElements.ToString();
      debugheader += ";#HM=" + Thruster_Hydro_Main.NElements.ToString();
      debugheader += ";#HD=" + Thruster_Hydro_Dir.NElements.ToString();
      debugheader += ";#IM=" + Thruster_Ion_Main.NElements.ToString();
      debugheader += ";#ID=" + Thruster_Ion_Dir.NElements.ToString();
      debugheader += String.Format("\n" + debugFmtString, "tick", "s", "v", "weight", "Th_Hydro", "Th_Atmo", "Th_Ion", "OR");

      if (controller == null) {
        screens.WriteText("main", append: false, EchoOnFail: true, things: "ERROR: No control block found on grid.\nTerminating script.\n");
        return 1;
      }

      //set some variables.
      tickCount = 0;

      //write debug header
      screens.WriteText("log", append: false, EchoOnFail: false, things: debugheader);
      debugLineCount = 0;

      initPos = controller.GetPosition();
      initTime = continousTime;

      initdone = true;

      return 0;
    }

    bool EqualsTol(double a, double b, double tol = 1e-5) { return Math.Abs(a - b) < tol; }

    double GetEnclosedAngle(Vector3D a, Vector3D b) { return Math.Acos(Vector3D.Normalize(a).Dot(Vector3D.Normalize(b))); }

    string SIformat(double value, string fmt = "{0:0.0}") {
      int oom = (int)Math.Floor(Math.Log10(Math.Abs(value))) / 3;
      oom = oom < 0 ? 0 : oom;
      value /= Math.Pow(10, oom * 3);

      return String.Format(fmt, value) + SIPrefixDict[oom];
    }

    string FormatV3I(Vector3I v) {
      if (v.X < 0) { return "-X"; } else if (v.X > 0) { return "+X"; } else if (v.Y < 0) { return "-Y"; } else if (v.Y > 0) { return "+Y"; } else if (v.Z < 0) { return "-Z"; } else if (v.Z > 0) { return "+Z"; } else { return "ER"; }
    }


    Program() {
      string[] lcd_tags = { "main", "log", "thrusters" };
      screens = new SmartScreens(lcdConfigTag, lcd_tags, this);
      //this causes problems.And doesnt solve half of the issues with game reloading during liftoff.
      //int i = -1;
      //if (int.TryParse(Storage, out i))
      //{
      //	stage = i;
      //	GenerateBlockLists();
      //}
    }

    //void Save()
    //{
    //	Storage = stage.ToString();
    //}

    void Main(string args, UpdateType updateType) {
      Output = "Advaned Liftoff Control:\n";

      if (!initdone || (args == "INIT")) {
        int initerror = Init();
        screens.WriteText("main", append: true, EchoOnFail: true, things: "INIT " + (initerror == 0 ? "DONE" : "FAILED"));
        screens.WriteText("log", append: true, EchoOnFail: true, things: "INIT returned:" + initerror + "\n");
        ++debugLineCount;

        if (initerror != 0) { Warn(); return; }

        if (args == "INIT") { return; }
      }

      if (cleardebug && (debugLineCount % 33 == 0)) {
        screens.WriteText("log", append: false, things: debugheader);
        debugLineCount = 0;
      }

      Runtime.UpdateFrequency = UpdateFrequency.Update100;
      lastTime = Time;
      Time = continousTime;

      Output += String.Format("Tick: {0} ({1:0.0}s)\n", tickCount, Time - lastTime);

      const string fmtstring = "{0,8:0}m {1,6:0.0}s {2,-15}\n";
      Output += "Stats: \n";
      Output += String.Format(fmtstring, Math.Round((Pos - initPos).Length()), Time - initTime, "since last init");
      Output += String.Format(fmtstring, Math.Round((Pos - startPos).Length()), Time - startTime, "since last restart");
      if (warningTime != 0) { Output += String.Format(fmtstring, Math.Round((Pos - warningPos).Length()), Time - warningTime, "since last warning"); }
      if (atmoPos != Vector3D.Zero) { Output += String.Format(fmtstring, Math.Round((Pos - atmoPos).Length()), Time - atmoTime, "since atmocutoff"); }
      if (hydroPos != Vector3D.Zero) { Output += String.Format(fmtstring, Math.Round((Pos - hydroPos).Length()), Time - hydroTime, "since hydrocutoff"); }
      if (midFlightPos != Vector3D.Zero) { Output += String.Format(fmtstring, Math.Round((Pos - midFlightPos).Length()), Time - midFlightTime, "since midflight"); }

      lastStage = stage;

      Pos = controller.GetPosition();
      var gVec = controller.GetNaturalGravity();
      var vVec = controller.GetShipVelocities().LinearVelocity;
      lastPos = Pos;
      double speed = (double)controller.GetShipSpeed();
      double mass = controller.CalculateShipMass().PhysicalMass;
      double grav = (double)gVec.Length();
      double partsOfg = Math.Round(grav / Earthg, 2);
      double weightForce = mass * grav;

      if (juststarted || (stage > 6 && partsOfg > 0)) { stage = 1; }

      const double anglewarn = Math.PI / 9;
      var angle = GetEnclosedAngle(vVec, Vector3D.Negate(gVec));

      var angledanger = angle > anglewarn;

      Output += String.Format("Velocity Angle: {0:0.00} rad", angle);
      if (angledanger) {
        angleblink = !angleblink;
        Warn();
        if (angleblink) Output += " Danger!";
        EnabledRelevantThrusters();
        SetInertiaDampeners(true);
      }
      Output += "\n";

      const double safety_additive = .05;
      double OR = 0;

      Output += "Stage: " + stage.ToString() + "\n";
      const string ThrstFmt = "{0,8}: {1,3:0}%\n";
      switch (stage) {
        case 0: //Waiting
          Output += "Waiting...";
          if (args != "INIT") stage += 1;
          break;
        case 1: //Acceleration
          Output += "Accelerating...";
          if (juststarted) {
            //in case set, disable dampeners
            if (disableDampenersOnStart) { SetInertiaDampeners(false); }

            Thruster_Ion_Main.Enabled = false;
            Thruster_Ion_Dir.Enabled = false;
            if (!Thruster_Atmo_Main.Any() || Thruster_Atmo_Main.Thrust_Max_Eff < weightForce) {
              Thruster_Hydro_Main.Enabled = true;
              Thruster_Hydro_Main.Override = 1;
              Thruster_Atmo_Main.Enabled = true;
              Thruster_Atmo_Main.Override = 1;
            } else {
              Thruster_Hydro_Main.Enabled = false;
              Thruster_Hydro_Main.Override = 1;
              Thruster_Atmo_Main.Enabled = false;
              Thruster_Atmo_Main.Override = 1;
            }

            startPos = controller.GetPosition();
            startTime = continousTime;
            TriggerBlock(startblock);
            juststarted = false;
          }

          if (EqualsTol(speed, targetSpeed, 1)) {
            stage += 1;

            if (Thruster_Atmo_Main.Thrust_Max_Eff < weightForce + safety_additive) //atmos arent enough, skip the atmo only stage
            { stage += 1; }
          }
          break;
        case 2: //atmos Only
          if (Thruster_Atmo_Main.Any()) {
            OR = weightForce / Thruster_Atmo_Main.Thrust_Max_Eff + safety_additive;
            Thruster_Atmo_Main.Override = OR;
            Output += String.Format(ThrstFmt, "Atmos", OR * 100);
            if (Thruster_Atmo_Main.Thrust_Max_Eff < weightForce) //atmos arent enough
            { stage += 1; }
          } else {
            stage += 1;
          }
          break;
        case 3: //atmos + hydro
          if (Thruster_Atmo_Main.Any()) {
            Thruster_Atmo_Main.Override = 1;
            if (Thruster_Atmo_Main.Ratio < .1) //consider these thrusters to be ineffective
            {
              TriggerBlock(atmoBlock);
              atmoPos = Pos;
              atmoTime = continousTime;
              Thruster_Atmo_Main.Override = 0;
              Thruster_Atmo_Main.Enabled = false;
              Thruster_Atmo_Dir.Enabled = false;
              stage += 1;
            }
            Output += String.Format(ThrstFmt, "Atmos", 100);
          } else {
            stage += 1;
          }
          OR = (weightForce - Thruster_Atmo_Main.Thrust_Max_Eff) / Thruster_Hydro_Main.Thrust_Max_Eff + safety_additive;
          Thruster_Hydro_Main.Enabled = true;
          Thruster_Hydro_Main.Override = OR;
          Output += String.Format(ThrstFmt, "Hydros", OR * 100);
          //if ( t_info_main.Max_eff + t_info_help.Max_eff < weightForce) //this seems pointless.
          //{ stage += 2; }
          break;
        case 4: //hydro
          OR = weightForce / Thruster_Hydro_Main.Thrust_Max_Eff + safety_additive;
          Thruster_Hydro_Main.Override = OR;
          Output += String.Format(ThrstFmt, "Hydros", OR * 100);
          if ((Thruster_Ion_Main.Thrust_Max_Eff / Thruster_Ion_Main.Thrust_Max_Abs > 0.1)) //ions can work
          { stage += 1; }
          break;
        case 5: //hydro + ions
          if (Thruster_Ion_Main.Any()) {
            Thruster_Ion_Main.Enabled = true;
            Thruster_Ion_Main.Override = 1;
            Output += String.Format(ThrstFmt, "Ions", 100);
          } else {
            stage += 1;
          }

          OR = (weightForce - Thruster_Ion_Main.Thrust_Max_Eff) / Thruster_Hydro_Main.Thrust_Max_Eff + safety_additive;
          Thruster_Hydro_Main.Override = OR;
          Output += String.Format(ThrstFmt, "Hydros", OR * 100);
          if (weightForce < Thruster_Ion_Main.Thrust_Max_Eff) {
            hydroPos = Pos;
            hydroTime = continousTime;
            TriggerBlock(hydroBlock);
            Thruster_Hydro_Main.Override = 0;
            Thruster_Hydro_Main.Enabled = false;
            stage += 1;
          }
          break;
        case 6: //ions
          if (!Thruster_Ion_Main.Any()) {
            stage += 1;
            break;
          }
          OR = weightForce / Thruster_Ion_Main.Thrust_Max_Eff + safety_additive;
          Thruster_Ion_Main.Override = OR;
          Output += String.Format(ThrstFmt, "Ions", OR * 100);
          break;
        case 7: //done
          Stop();
          break;
        default:
          Echo("Critical Error");
          screens.WriteText("log", append: true, things: "Unknown stage " + stage.ToString());
          ++debugLineCount;
          break;
      }
      lastSpeed = speed;

      var debugtext = String.Format(
        debugFmtString, tickCount, lastStage, speed,
        SIformat(weightForce) + "N",
        SIformat(Thruster_Hydro_Main.Thrust_Current) + "N",
        SIformat(Thruster_Atmo_Main.Thrust_Current) + "N",
        SIformat(Thruster_Ion_Main.Thrust_Current) + "N",
        OR
      );

      screens.WriteText("log", append: true, things: debugtext);
      ++debugLineCount;

      //stop conditions
      if (partsOfg < gravityTreshold || partsOfg <= 0)  //Stop script below specified gravity
      {
        stage = 7;
        Output = "Launch manager:\n";
        Output += "Note: Out of Grav Threshhold\nStopping launch sequence.\n";
        Thruster_Ion_Dir.Enabled = true;
        Stop();
        TriggerBlock(finishblock);
        screens.WriteText("log", append: true, things: "SUCCESSFULLY Completed in above step\n");
        ++debugLineCount;
      } else if (args == "STOP")  //stop the script when stopped by a user
        {
        Output = "Launch manager:\n";
        stage = 0;
        if (partsOfg > gravityTreshold) {
          Output += "\nWARNING: Danger of Freefall\n";
        }

        Output += "Stopping launch sequence.\n";
        Stop();
        screens.WriteText("log", append: true, things: "ABORTED in above step\n");
        ++debugLineCount;
      }

      //trigger mid flight block
      if (midflightblock != null && partsOfg <= (double)midFlightTreshold) {
        TriggerBlock(midflightblock);

        screens.WriteText("log", append: true, things: "MidFlight Triggered in above step\n");
        ++debugLineCount;

        midFlightPos = controller.GetPosition();
        midFlightTime = continousTime;

        if (CleanBlockLists()) //special handling in case this does bad things.
        {
          if (Thruster_Ion_Main.Thrust_Max_Eff > weightForce) { stage = 6; } else {
            screens.WriteText("log", append: true, things: "WARNING: cleanup failure and Ions cant lift.\n");
            GenerateThrusterLists();
          }
        }
      }

      screens.WriteText("main", append: false, EchoOnFail: true, things: Output);
      continousTime += Runtime.TimeSinceLastRun.TotalSeconds + Runtime.LastRunTimeMs / 1000;
      Echo(String.Format("last RunTime: {0:0.00}ms", Runtime.LastRunTimeMs));
      ++tickCount;
    }

    public class AbstractCachingGroup<T> : IEnumerable<T>
      where T : class {
      protected const int TickSet = 3;
      public readonly int CachedTicks = TickSet;
      protected int TickCounter = TickSet;
      protected HashSet<T> L = new HashSet<T>();

      public AbstractCachingGroup(List<T> _L, int _CachedTicks = TickSet) {
        CachedTicks = _CachedTicks;
        foreach (var x in _L) { L.Add(x as T); }
      }
      public AbstractCachingGroup(HashSet<T> _L, int _CachedTicks = TickSet) {
        CachedTicks = _CachedTicks;
        L = _L;
      }

      public AbstractCachingGroup() { }

      public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)L).GetEnumerator(); }
      IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable<T>)L).GetEnumerator(); }

      public int NElements {
        get { return L.Count; }
      }

      public int RemoveAll(Func<T, bool> filter = null) {
        return L.RemoveWhere(x => filter(x));
      }

      public bool Add<S>(S value)
        where S : class, T {
        return L.Add(value);
      }
      public void Add<S>(List<S> value)
        where S : class, T {
        foreach (T x in value) { L.Add(x); }
      }

      public void Clear() { L.Clear(); }

      public bool Any() { return L.Any(); }

      public virtual void Update() { }
      public void Advance(bool update) {
        ++TickCounter;
        if (update) { Update(); }
      }
    }

    public class TerminalBlockGroup<T> : AbstractCachingGroup<T>
      where T : class, IMyTerminalBlock {
      private bool _ShowOnHUD = false;
      private bool _ShowInTerminal = true;
      private bool _ShowInToolbarConfig = true;
      private bool _ShowInInventory = true;

      public TerminalBlockGroup(List<T> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public TerminalBlockGroup(HashSet<T> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public TerminalBlockGroup() : base() { }


      public bool IsBeingHacked {
        get {
          foreach (var x in L) { if (x.IsBeingHacked) return true; }
          return false;
        }
      }
      public bool ShowOnHUD {
        get { return _ShowOnHUD; }
        set {
          if (_ShowOnHUD != value) {
            _ShowOnHUD = value;
            foreach (var x in L) { if (x != null) { x.ShowOnHUD = value; } }
          }
        }
      }
      public bool ShowInTerminal {
        get { return _ShowInTerminal; }
        set {
          if (_ShowInTerminal != value) {
            _ShowInTerminal = value;
            foreach (var x in L) { if (x != null) { x.ShowInTerminal = value; } }
          }
        }
      }
      public bool ShowInToolbarConfig {
        get { return _ShowInToolbarConfig; }
        set {
          if (_ShowInToolbarConfig != value) {
            _ShowInToolbarConfig = value;
            foreach (var x in L) { if (x != null) { x.ShowInToolbarConfig = value; } }
          }
        }
      }
      public bool ShowInInventory {
        get { return _ShowInInventory; }
        set {
          if (_ShowInInventory != value) {
            _ShowInInventory = value;
            foreach (var x in L) { if (x != null) { x.ShowInInventory = value; } }
          }
        }
      }
    }

    public class FunctionalBlockGroup<T> : TerminalBlockGroup<T>
      where T : class, IMyFunctionalBlock {
      protected bool _Enabled = false;

      public FunctionalBlockGroup(List<T> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public FunctionalBlockGroup(HashSet<T> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public FunctionalBlockGroup() : base() { }

      public bool Enabled {
        get { return _Enabled; }
        set {
          if (_Enabled != value) {
            _Enabled = value;
            foreach (var x in L) { if (x != null) { x.Enabled = value; } }
          }
        }
      }
    }

    public class SmartScreens {
      public readonly Dictionary<string, TextSurfaceGroup> screens = new Dictionary<string, TextSurfaceGroup>();
      private readonly MyGridProgram Program;
      private readonly string Identifier;

      public SmartScreens(string _identifier, string[] _group_names, MyGridProgram _program) {
        Identifier = _identifier;
        Program = _program;

        foreach (var tag in _group_names) { screens[tag] = new TextSurfaceGroup(); }

        BuildDatabase();
      }

      public void BuildDatabase() {
        List<IMyTerminalBlock> screen_blocks = new List<IMyTerminalBlock>();
        Program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(screen_blocks, x => { return x is IMyTextSurfaceProvider || x is IMyTextSurface; });
        foreach (var b in screen_blocks) {
          MyIni ini = new MyIni();
          ini.TryParse(b.CustomData);

          if (!ini.ContainsSection(Identifier)) { continue; }

          foreach (var group_name in screens.Keys) {
            if (b is IMyTextSurface) {
              if (ini.Get(Identifier, "0").ToString() == group_name) { screens[group_name].Add(b as IMyTextSurface); }
            } else if (b is IMyTextSurfaceProvider) {
              var bcast = b as IMyTextSurfaceProvider;
              for (int i = 0; i < bcast.SurfaceCount; ++i) {
                if (ini.Get(Identifier, i.ToString()).ToString() == group_name) { screens[group_name].Add(bcast.GetSurface(i)); }
              }
            }
          }
        }
      }

      public bool Add(string group_name, IMyTextSurface block) {
        if (!screens.ContainsKey(group_name)) { screens[group_name] = new TextSurfaceGroup(); }
        return screens[group_name].Add(block);
      }

      public bool Add(string group_name, List<IMyTextSurface> list) {
        bool only_uniques = true;
        if (!screens.ContainsKey(group_name)) { screens[group_name] = new TextSurfaceGroup(); }
        foreach (var surf in list) { only_uniques &= screens[group_name].Add(surf); }
        return only_uniques;
      }

      public bool Remove<T>(string group_name = null, Func<T, bool> filter = null)
        where T : IMyTerminalBlock {
        if (screens.ContainsKey(group_name)) {
          //TODO
          return true;
        } else { return false; }
      }

      public void Cleanup() {
        foreach (var g in screens.Values) { g.RemoveAll(x => x == null); }
      }

      static private string ListToString(string separator = " ", params object[] things) {
        if (things.Length == 1 && things[0] is string) { return things[0] as string; } else {
          string s = "";
          foreach (var p in things) {
            if (p is string) { s += p + separator; } else if (p is IEnumerable) {
              foreach (var x in p as IList) { s += x.ToString() + separator; }
            } else { s += p.ToString() + separator; }
          }
          return s;
        }
      }

      public bool WriteText(string group_name, bool append = false, bool EchoOnFail = false, string separator = " ", params object[] things) {
        var exists = screens.ContainsKey(group_name) && screens[group_name].Any();
        if (!exists && !EchoOnFail) { return false; }

        string text = ListToString(separator, things);
        if (exists) { screens[group_name].WriteText(text, append); } else if (EchoOnFail) { Program.Echo(text); }
        return true;
      }

      public bool WriteText(string[] group_names, bool append = false, bool EchoOnFail = false, string separator = " ", params object[] things) {
        var success = false;
        foreach (var group_name in group_names) { success |= WriteText(group_name: group_name, append: append, EchoOnFail: EchoOnFail, separator: separator, things: things); }

        return success;
      }

      public void Configure(string group_name, ContentType contentType, string font, bool clear = false) {
        if (screens.ContainsKey(group_name)) {
          screens[group_name].ContentType = contentType;
          screens[group_name].Font = font;
          if (clear) { screens[group_name].Text = ""; }
        }
      }

      public void ConfigureAll(ContentType contentType, string font, bool clear = false) {
        foreach (var group in screens.Values) {
          group.ContentType = contentType;
          group.Font = font;
          if (clear) { group.Text = ""; }
        }
      }
    }

    public class TextSurfaceGroup : AbstractCachingGroup<IMyTextSurface> {
      private ContentType _ContentType;
      private string _Font;
      private float _FontSize;
      private Color _FontColor;
      private TextAlignment _Alignment;
      private float _TextPadding;
      private string _Text;

      public TextSurfaceGroup(List<IMyTextSurface> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public TextSurfaceGroup(HashSet<IMyTextSurface> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public TextSurfaceGroup() : base() { }

      public ContentType ContentType {
        get { return _ContentType; }
        set {
          if (_ContentType != value) {
            _ContentType = value;
            foreach (var x in L) { if (x != null) { x.ContentType = value; } }
          }
        }
      }

      public string Font {
        get { return _Font; }
        set {
          if (_Font != value) {
            _Font = value;
            foreach (var x in L) { if (x != null) { x.Font = value; } }
          }
        }
      }

      public float FontSize {
        get { return _FontSize; }
        set {
          if (_FontSize != value) {
            _FontSize = value;
            foreach (var x in L) { if (x != null) { x.FontSize = _FontSize; } }
          }
        }
      }

      public Color FontColor {
        get { return _FontColor; }
        set {
          if (_FontColor != value) {
            _FontColor = value;
            foreach (var x in L) { if (x != null) { x.FontColor = value; } }
          }
        }
      }

      public TextAlignment Alignment {
        get { return _Alignment; }
        set {
          if (_Alignment != value) {
            _Alignment = value;
            foreach (var x in L) { if (x != null) { x.Alignment = value; } }
          }
        }
      }

      public float TextPadding {
        get { return _TextPadding; }
        set {
          if (_TextPadding != value) {
            _TextPadding = value;
            foreach (var x in L) { if (x != null) { x.TextPadding = value; } }
          }
        }
      }

      public string Text {
        get { return _Text; }
        set {
          if (_Text != value) {
            _Text = value;
            foreach (var x in L) { if (x != null) { x.WriteText(value); } }
          }
        }
      }

      public void WriteText(string text, bool append = false) {
        foreach (var x in L) { if (x != null) { x.WriteText(text, append); } }
        if (append) { _Text += text; } else { _Text = text; }
      }
    }

    public class ThrusterGroup : FunctionalBlockGroup<IMyThrust> {
      private double _Override = 0;
      private double _Thrust_Max_Abs = 0;
      private double _Thrust_Max_Eff = 0;
      private double _Thrust_Current = 0;

      public ThrusterGroup(List<IMyThrust> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public ThrusterGroup(HashSet<IMyThrust> _L, int _CachedTicks = TickSet)
        : base(_L, _CachedTicks) { }
      public ThrusterGroup() : base() { }

      public double Override {
        get { return _Override; }
        set {
          if (_Override != value) {
            _Override = value;
            float or = (float)value;
            foreach (var t in L) { if (t != null) { t.ThrustOverridePercentage = or; } }
          }
        }
      }

      public double Thrust_Max_Abs {
        get {
          if (++TickCounter > CachedTicks) { Update(); }
          return _Thrust_Max_Abs;
        }
        private set { _Thrust_Max_Abs = value; }
      }

      public double Thrust_Max_Eff {
        get {
          if (++TickCounter > CachedTicks) { Update(); }
          return _Thrust_Max_Eff;
        }
        private set { _Thrust_Max_Eff = value; }
      }

      public double Thrust_Current {
        get {
          if (++TickCounter > CachedTicks) { Update(); }
          return _Thrust_Current;
        }
        private set { _Thrust_Current = value; }
      }

      public double Ratio { get; private set; } = 0;

      public override void Update() {
        TickCounter = 0;
        _Thrust_Max_Abs = 0;
        _Thrust_Max_Eff = 0;
        _Thrust_Current = 0;

        foreach (IMyThrust t in L) {
          if (t == null) continue;
          _Thrust_Max_Abs += t.MaxThrust;
          _Thrust_Max_Eff += t.MaxEffectiveThrust;
          _Thrust_Current += t.CurrentThrust;
        }
        Ratio = _Thrust_Max_Eff / _Thrust_Max_Abs;
      }

      public void Off() {
        _Override = 0;
        _Enabled = false;
        foreach (var t in L) { t.Enabled = false; t.ThrustOverridePercentage = 0; }
      }
    }

    #region PreludeFooter
  }
}
#endregion
