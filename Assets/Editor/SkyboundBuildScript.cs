#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkyboundRush.Editor
{
    public static class SkyboundBuildScript
    {
        [MenuItem("Skybound Rush/Build Standalone Player")]
        public static void BuildStandalonePlayer()
        {
            Debug.Log("[SkyboundBuildScript] Starting standalone build for Skybound Rush...");

            string[] scenes = { "Assets/Scenes/MainGame.unity" };
            string buildPath = "Build/SkyboundRush.exe";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BUILD_SUCCEEDED] Build completed successfully: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s at {buildPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BUILD_FAILED] Build failed with result: {summary.result}, errors: {summary.totalErrors}");
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
