﻿using UnityEngine;
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
            Init();
        }

        public void Init()
        {
            if(settings != null && settings.canConnectOnStartup())
            {
                TwitchWrapper.StartAsync();
            }
        }

        public override string SettingsCategory() => nameof(ToolkitCore);

        public override void DoSettingsWindowContents(Rect inRect) =>
            ToolkitCore.settings.DoWindowContents(inRect);
    }
}
