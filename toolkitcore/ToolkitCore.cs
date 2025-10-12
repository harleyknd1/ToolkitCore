using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace ToolkitCore
{
    public class ToolkitCore : Mod
    {
        public static ToolkitCoreSettings settings;

        public ToolkitCore(ModContentPack content)
          : base(content)
        {
            ToolkitCore.settings = GetSettings<ToolkitCoreSettings>();
            AuthTokenScribe.LoadAuthToken();
        }

        /// <inheritdoc />
        public override void WriteSettings()
        {
            base.WriteSettings();
            
            if (GlobalResources.ScopeRegistry.CurrentToken != null) AuthTokenScribe.SaveAuthToken();
        }

        public override string SettingsCategory() => nameof(ToolkitCore);

        public override void DoSettingsWindowContents(Rect inRect) =>
            ToolkitCore.settings.DoWindowContents(inRect);
    }

    [UsedImplicitly]
    [StaticConstructorOnStartup]
    internal sealed class Startup
    {
        static Startup()
        {
            Task.Run(async () => await InitializeAsync());
        }
        
        private static async Task InitializeAsync()
        {
            await AuthTokenValidator.ValidateTokenAsync();
            
            if (ToolkitCore.settings != null && ToolkitCore.settings.canConnectOnStartup() && !string.IsNullOrEmpty(ToolkitCoreSettings.bot_username) && !string.IsNullOrEmpty(ToolkitCoreSettings.oauth_token))
                TwitchWrapper.StartAsync();
            else
                Log.Message("Not starting the Twitch client, credentials not set");
        }
    }
}
