using System;
using System.IO;
using JetBrains.Annotations;
using Verse;

namespace ToolkitCore;

[PublicAPI]
[StaticConstructorOnStartup]
public static class CoreFilePaths
{
    public static readonly string RootDir = Path.Combine(GenFilePaths.SaveDataFolderPath, path2: "ToolkitCore");
    public static readonly string ConfigDir = Path.Combine(RootDir, path2: "Config");

    static CoreFilePaths()
    {
        EnsureDirectoriesExist();
    }

    private static void EnsureDirectoriesExist()
    {
        try
        {
            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(ConfigDir);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create core directories\n\n{ex}");
        }
    }
}