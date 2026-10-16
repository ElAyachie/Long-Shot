using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the browser version of the game that the portfolio website hosts.
// From the editor: Long Shot > Build WebGL. From the command line:
//   Unity.exe -projectPath <project> -buildTarget WebGL -executeMethod WebGLBuild.BuildFromCommandLine -quit -out <folder>
public static class WebGLBuild
{
    const string DefaultOut = "Builds/WebGL";

    [MenuItem("Long Shot/Build WebGL")]
    public static void BuildFromMenu()
    {
        Build(DefaultOut);
    }

    public static void BuildFromCommandLine()
    {
        string outDir = DefaultOut;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-out") outDir = args[i + 1];
        var result = Build(outDir);
        EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
    }

    static BuildResult Build(string outDir)
    {
        PlayerSettings.WebGL.template = "PROJECT:LongShot";
        // Gzip with the decompression fallback works on any static host, no special headers needed.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.defaultWebScreenWidth = 405;
        PlayerSettings.defaultWebScreenHeight = 832;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("WebGL build " + report.summary.result + ": " + (report.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB -> " + outDir);
        return report.summary.result;
    }
}
