﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using BepInEx.Logging;
using UnityEngine.UI;
using TerminalApi;
using TerminalApi.Classes;
using static TerminalApi.TerminalApi;
using System;

namespace TerminalCommander.Patches
{
    /// <summary>
    /// Patches in game terminal. Client side execution
    /// </summary>
    [HarmonyPatch(typeof(Terminal))]

    internal class TerminalHotkeys
    {
        private static string turretName = "turretscript";
        private static string landmineName = "landmine";
        private static string doorName = "bigdoor";
        private static bool openDoors = true; //Used to close or open all doors

        private static ManualLogSource logSource; // Log source field
        private static Commander commanderSource;
        // Method to set the log source
        public static void SetSource(Commander source)
        {
            commanderSource = source;
            logSource = commanderSource.log;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void HotKeyPatch(ref bool ___terminalInUse, ref TMP_InputField ___screenText)
        {
            try
            {
                if (___terminalInUse && BepInEx.UnityInput.Current.GetKey(KeyCode.LeftControl) || BepInEx.UnityInput.Current.GetKey(KeyCode.RightControl))
                {
                    Terminal t = FindActiveObject<Terminal>();
                    //RoundManager r = FindActiveObject<RoundManager>();

                    if (t == null)
                    {
                        logSource.LogInfo($"{Commander.modName} ERROR: Terminal could not be found.");
                    }
                    //Switch Hot Key
                    //Executes a monitor switch
                    if (BepInEx.UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.S))
                    {
                        SwitchPlayer(t);
                    }
                    //Transmit Hot Key
                    //Executes transmit text
                    else if (BepInEx.UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.T))
                    {
                        Transmission(t, ___screenText);
                    }
                    //Doors Hot Key
                    //Open / Close all doors
                    else if (BepInEx.UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.D))
                    {
                       OperateBigDoors(t);
                    }
                    //Jamming Hot Key
                    //Disable all turrets and mines
                    else if (BepInEx.UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.J))
                    {
                        JamTurrentMines(t);
                    }
                    //View Monitor Hot Key
                    //Quickly turn on and off monitor
                    else if (BepInEx.UnityInput.Current.GetKeyDown(UnityEngine.KeyCode.M))
                    {
                        ViewMonitor(t);             
                    }
                }
            }
            catch(Exception ex)
            {
                logSource.LogInfo($"{Commander.modName} ERROR: {ex.Message}");
            }
        }

        static void SwitchPlayer(Terminal t)
        {

            //TerminalNode tn = t.terminalNodes.specialNodes[20];
            //StartOfRound.Instance.mapScreen.SwitchRadarTargetForward(callRPC: true);
            //t.LoadNewNode(tn);

            string cmd = "switch";

            t.screenText.text += cmd;
            t.textAdded = cmd.Length;

            t.OnSubmit();
        }
        static void Transmission(Terminal t, TMP_InputField ___screenText)
        {
            ___screenText.text += "transmit ";
        }
        static void OperateBigDoors(Terminal t)
        {
            TerminalAccessibleObject[] taos = (from x in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>()
                                               select x).ToArray();

            List<string> items = new List<string>();

            foreach (var tao in taos.Where(x => x.name.ToLower().Contains(doorName)))
            {
                if (!tao.isBigDoor) { return; } //Not a big door
                items.Add(tao.objectCode);
                tao.SetDoorLocalClient(openDoors);
            }

            if (openDoors)
            {
                SetTerminalText(t, "Opening all doors\n\n");
                openDoors = false;
            }
            else
            {
                SetTerminalText(t, "Closing all doors\n\n");
                openDoors = true;
            }
            t.terminalAudio.PlayOneShot(t.codeBroadcastSFX, 1f);
            t.codeBroadcastAnimator.SetTrigger("display");
            logSource.LogInfo($"{Commander.modName} TerminalAccessibleObjects Called: Count{taos.Count()} - ({string.Join(", ", items)})");
        }
        static void SetTerminalText(Terminal t, string s)
        {
            TerminalNode tn = new TerminalNode();
            tn.clearPreviousText = true;
            tn.acceptAnything = false;
            tn.displayText = s;
            t.LoadNewNode(tn);

            t.screenText.ActivateInputField();
            ((Selectable)t.screenText).Select();
        }          
        static void JamTurrentMines(Terminal t)
        {
            TerminalAccessibleObject[] taos = (from x in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>()
                                               select x).ToArray();

            List<string> items = new List<string>();

            foreach (var tao in taos.Where(x => x.name.ToLower() == turretName || x.name.ToLower() == landmineName))
            {
                items.Add(tao.objectCode);
                tao.CallFunctionFromTerminal();

            }

            SetTerminalText(t, "Jamming turrets and land mines\n\n");

            t.terminalAudio.PlayOneShot(t.codeBroadcastSFX, 1f);
            t.codeBroadcastAnimator.SetTrigger("display");

            logSource.LogInfo($"{Commander.modName} TerminalAccessibleObjects Called: Count{taos.Count()} - ({string.Join(", ", items)})");
        }
        static void ViewMonitor(Terminal t)
        {
            string cmd = "view monitor";

            t.screenText.text += cmd;
            t.textAdded = cmd.Length;

            t.OnSubmit();
        }
       
        static T FindActiveObject<T>() where T : UnityEngine.Object
        {
            T[] unityObjects = UnityEngine.Object.FindObjectsOfType<T>();

            if (unityObjects.Length > 0)
            {
                // For simplicity, just return the first found.
                //May need to expand later if first object is not desired object.
                return unityObjects[0];
            }

            return null;
        }

    }
}
