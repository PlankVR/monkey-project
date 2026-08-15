#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using UnityEngine;

[InitializeOnLoad]
public static class ChimpyClimbingEditorChecker
{
    static ChimpyClimbingEditorChecker()
    {
        CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
    }

    private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
    {
        // Perform the check only after all scripts compile successfully
        if (IsCompilationSuccessful(messages))
        {
            CheckAndRemoveChimpyClimbing();
        }
    }

    private static bool IsCompilationSuccessful(CompilerMessage[] messages)
    {
        foreach (var message in messages)
        {
            if (message.type == CompilerMessageType.Error)
            {
                return false;
            }
        }
        return true;
    }

    private static void CheckAndRemoveChimpyClimbing()
    {
        string assetsPath = Application.dataPath;
        string filePath = Path.Combine(assetsPath, "ChimpyClimbing.cs");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.LogError("This script cannot be used in SCL due to changes done by using the RopeClimbing.cs script.");
            AssetDatabase.Refresh(); // Refresh the editor to reflect changes
        }
    }
}
#endif
