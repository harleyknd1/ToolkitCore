using System;
using System.Threading;
using System.Threading.Tasks;
using ToolkitCore.Utilities;
using ToolkitCore.Windows;
using UnityEngine;
using Verse;

namespace ToolkitCore
{
    public class ToolkitCoreSettings : ModSettings
    {
        public static string client_id = "9wyhma4y3tmx1haf19owy0u8jh7paj";
        public static string channel_username = "";
        public static string bot_username = "";

        [Obsolete("Use ScopeRegistry.CurrentToken.AccessToken instead.")]
        public static string oauth_token = "";

        public static bool connectOnGameStartup = false;
        public static bool allowWhispers = true;
        public static bool forceWhispers = false;
        public static bool sendMessageToChatOnStartup = true;
        private bool showOauth;
        private static readonly float verticalHeight = 32f;
        private static readonly float verticalSpacing = 40f;

        public void DoWindowContents(Rect inRect)
        {
            if (Widgets.ButtonText(new Rect((inRect).width - 120f, verticalSpacing, 90f, verticalHeight), "Help", true, true, true))
            {
                Application.OpenURL("https://github.com/hodldeeznuts/ToolkitCore/wiki/Twitch-Chat-Connection");
            }
            Rect val = new Rect(0f, verticalSpacing, inRect.width / 2f, 64f);
            Widgets.Label(val, TCText.BigText("Channel Details"));
            float num = val.y + verticalSpacing * 2f;
            Rect val2 = new Rect(0f, num, 200f, verticalHeight);
            Widgets.Label(val2, "Channel:");
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "Bot Username:");
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "OAuth Token:");
            Rect val3 = new Rect(200f, num, 200f, verticalHeight);
            channel_username = Widgets.TextField(val3, channel_username);
            val3.y = val3.y + verticalSpacing;
            bot_username = Widgets.TextField(val3, bot_username);
            if (channel_username != "" && Widgets.ButtonText(new Rect(((val3)).x + ((val3)).width + 10f, ((val3)).y, 210f, verticalHeight), "Same as Channel", true, true, true))
            {
                bot_username = channel_username;
            }
            val3.y = val3.y + verticalSpacing;
            Rect val4 = new Rect(((val3)).x + ((val3)).width + 10f, ((val3)).y, 60f, verticalHeight);
            if (showOauth)
            {
                oauth_token = Widgets.TextField(val3, oauth_token);
                if (Widgets.ButtonText(val4, "Hide", true, true, true))
                {
                    showOauth = !showOauth;
                }
            }
            else
            {
                Widgets.Label(val3, new string('*', Math.Min(oauth_token.Length, 16)));
                if (Widgets.ButtonText(val4, "Show", true, true, true))
                {
                    showOauth = !showOauth;
                }
            }
            if (Widgets.ButtonText(new Rect(val4.x + val4.width + 10f, val3.y, 140f, verticalHeight), "New OAuth Token", true, true, true))
            {
                var window = Window_DeviceCodeFlow.CreateInstance();
                Find.WindowStack.Add(window);
                Task.Run(async () => await GlobalResources.ScopeRegistry.InitialAuthenticateAsync(window.CancellationTokenSource.Token));
            }
            val3.y = val3.y + verticalSpacing;
            if (Widgets.ButtonText(val3, "Paste from Clipboard", true, true, true))
            {
                oauth_token = GUIUtility.systemCopyBuffer;
            }
            Rect val5 = new Rect(0f, val3.y + verticalSpacing * 2f, inRect.width / 2f, 64f);
            Widgets.Label(val5, TCText.BigText("Connection"));
            num = val5.y + verticalSpacing * 2f;
            val2.y = (num);
            val3.y = num;
            Widgets.Label(val2, "Status:");
            Rect val6 = new Rect(((val3)).x + ((val3)).width + 2f, ((val3)).y, ((val3)).width, verticalHeight);
            if (TwitchWrapper.Client != null && TwitchWrapper.Client.IsConnected)
            {
                Widgets.Label(val3, TCText.ColoredText("Connected", Color.green));
                if (Widgets.ButtonText(val6, "Disconnect", true, true, true))
                {
                    TwitchWrapper.Client.Disconnect();
                }
            }
            else
            {
                Widgets.Label(val3, TCText.ColoredText("Not Connected", Color.red));
                if (Widgets.ButtonText(val6, "Connect", true, true, true))
                {
                    TwitchWrapper.StartAsync();
                }
            }
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "Auto Connect on Startup:");
            val3.y = val2.y;
            Widgets.Checkbox(((val3)).position, ref connectOnGameStartup, 24f, false, false, null, null);
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "Allow Viewers to Whisper:");
            val3.y = val2.y;
            Widgets.Checkbox(((val3)).position, ref allowWhispers, 24f, false, false, null, null);
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "Force Viewers to Whisper:");
            val3.y = val2.y;
            Widgets.Checkbox(((val3)).position, ref forceWhispers, 24f, false, false, null,null);
            val2.y = val2.y + verticalSpacing;
            Widgets.Label(val2, "Send Connection Message:");
            val3.y = val2.y;
            Widgets.Checkbox(((val3)).position, ref sendMessageToChatOnStartup, 24f);

        }

        public override void ExposeData()
        {
            Scribe_Values.Look<string>(ref ToolkitCoreSettings.channel_username, "channel_username", "", false);
            Scribe_Values.Look<string>(ref ToolkitCoreSettings.bot_username, "bot_username", "", false);
            Scribe_Values.Look<string>(ref ToolkitCoreSettings.oauth_token, "oauth_token", "", false);
            Scribe_Values.Look<bool>(ref ToolkitCoreSettings.connectOnGameStartup, "connectOnGameStartup", false, false);
            Scribe_Values.Look<bool>(ref ToolkitCoreSettings.allowWhispers, "allowWhispers", true, false);
            Scribe_Values.Look<bool>(ref ToolkitCoreSettings.sendMessageToChatOnStartup, "sendMessageToChatOnStartup", true, false);
            Scribe_Values.Look<bool>(ref ToolkitCoreSettings.forceWhispers, "forceWhispers", false, false);
            Scribe_Values.Look<string>(ref ToolkitCoreSettings.client_id, "clientId", "9wyhma4y3tmx1haf19owy0u8jh7paj");
        }

        public bool canConnectOnStartup()
        {
            ExposeData();
            return (connectOnGameStartup && !string.IsNullOrEmpty(bot_username) && !string.IsNullOrEmpty(oauth_token));
        }
    }
}
