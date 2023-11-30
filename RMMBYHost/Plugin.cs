using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Microsoft.Win32;
using System.Windows.Input;
using UnityEngine;
using MelonLoader.Utils;
using UnityEngine.SceneManagement;
using IdolShowdown.UI;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Harmony;

namespace RMMBYHost
{
    public class Plugin : MelonMod
    {
        // Set Game Name So RMMBY Displays The Correct Game
        private string GameName = "Idol Showdown";

        // Mods Can Subscribe To This Event To Know When RMMBY Has Updated Enabled Mods
        public static event Action onGetEnabledChanged;

        // True When RMMBYHost Opens RMMBY And RMMBY Is Still Open
        private bool useServer;
        public void RMMBYListener()
        {
            TcpListener serverSocket = null;
            try
            {
                // Open A Server That Listens To Port 1290 Only On The Local Machine
                serverSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), 1290);
                serverSocket.Start();
                MelonLogger.Msg("Started Server");

                // Byte Array For Incoming Messages
                byte[] bytes = new byte[1024];
                // Byte Array Converted To Plain Text
                string data = null;

                useServer = true;

                // Run While Server Is Active
                while (useServer)
                {
                    MelonLogger.Msg("Waiting For Connection");

                    // Accept The Incoming Client
                    TcpClient client = serverSocket.AcceptTcpClient();
                    MelonLogger.Msg("Client Connected");

                    // Empty Incoming Text String
                    data = null;

                    NetworkStream stream = client.GetStream();

                    int i;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Convert Incoming Messages To A String
                        data = Encoding.ASCII.GetString(bytes, 0, i);
                        MelonLogger.Msg(string.Format("Received: {0}", data));

                        // "End Connection No Restart" Will Tell RMMBYHost To:
                        // End The Server
                        // Send An Event For Other Mods
                        // Return The Game To Its Previous State
                        if (data == "End Connection No Restart")
                        {
                            onGetEnabledChanged();
                            EndConnection(stream, client, serverSocket);
                            break;
                        }
                        // "End Connection Do Restart" Will Tell RMMBYHost To:
                        // End The Server
                        // Exit The Game
                        else if (data == "End Connection Do Restart")
                        {
                            EndConnection(stream, client, serverSocket);
                            restart();
                            return;
                        }

                        // Return A Message To RMMBY
                        data = data.ToUpper();
                        byte[] msg = Encoding.ASCII.GetBytes(data);

                        stream.Write(msg, 0, msg.Length);
                        MelonLogger.Msg(string.Format("Sent: {0}", data));
                    }
                }
            }
            catch (SocketException e)
            {
                MelonLogger.Msg(string.Format("SocketException: {0}", e));
            }
            finally
            {
                // End The Server
                serverSocket.Server.Close(0);
                serverSocket.Stop();
            }

            MelonLogger.Msg("Close Server");
        }

        // Cleanup The Client And Stream
        // Toggle useServer To End The Server
        public void EndConnection(NetworkStream stream, TcpClient client, TcpListener server)
        {
            client.Close();
            client.Dispose();
            stream.Flush();
            stream.Close();
            useServer = false;
        }

        // Close The Game
        public void restart()
        {
            //restartApplication = true;
            Application.Quit();
        }

        // Use The Registry To Find The Location Of RMMBY's .exe
        // Call This To Open RMMBY In Exclusive Mode
        public void CallModManager()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(string.Format("SOFTWARE\\Classes\\{0}\\shell\\open\\command", "rmmby")))
            {
                if (key != null)
                {
                    System.Object o = key.GetValue("");

                    if (o != null)
                    {
                        // Create A Process For RMMBY
                        Process p = new Process();
                        p.StartInfo.FileName = o.ToString();
                        // Send An Argument To Tell RMMBY To Enter Exclusive Mode For This Game
                        p.StartInfo.Arguments = string.Format("\"rmmby://GameMenu/{0}\"", GameName);
                        p.StartInfo.UseShellExecute = false;
                        p.Start();
                    }
                }
            }

            // Open The Server
            RMMBYListener();
        }


        // Idol Showdown Specific Code
        // Adds A Button To The Main Menu To Open RMMBY
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            if (sceneName == "MainMenu") CreateMenuModButton();
        }

        public void CreateMenuModButton()
        {
            GameObject canvas = SceneManager.GetSceneAt(1).GetRootGameObjects()[1];

            GameObject modButton = canvas.transform.Find("Buttons/MainButtons/Bottom Black Bar Constraint/ButtonGrid/MainMenuMiniButton_discord").gameObject;
            modButton.SetActive(true);

            modButton.AddComponent<ModButtonSetup>();
        }

        private Navigation nav;
        private ISUIElementsHandler eh;

        public void FinshMenuModButton(GameObject modMenuButton)
        {
            GameObject canvas = SceneManager.GetSceneAt(1).GetRootGameObjects()[1];
            eh = canvas.GetComponent<ISUIElementsHandler>();

            Type t = eh.GetType();
            FieldInfo field = t.GetField("uiElements", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty);

            List<ISUIElementsHandler.UIElement> uiElements = (List<ISUIElementsHandler.UIElement>)field.GetValue(eh);

            nav = uiElements[6].NavigationOriginal;

            nav.selectOnLeft = modMenuButton.GetComponent<Button>();

            ISUIElementsHandler.UIElement ele = uiElements[6];
            ele.NavigationOriginal = nav;
            uiElements[6] = ele;

            t = eh.GetType();
            field = t.GetField("uiElements", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty);

            field.SetValue(eh, uiElements);

            eh.BuildNavMaps();

            UnityAction action = new UnityAction(CallModManager);

            modMenuButton.GetComponent<EventTrigger>().triggers[2].callback = new EventTrigger.TriggerEvent();

            modMenuButton.GetComponent<EventTrigger>().triggers[2].callback.AddListener(OnSubmit);
        }

        public MethodInfo CMMInfo()
        {
            return HarmonyLib.SymbolExtensions.GetMethodInfo(() => CallModManager());
        }


        
        public virtual void OnSubmit(BaseEventData data)
        {
            CallModManager();
        }
    }
}
