using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Adds the given define symbols to PlayerSettings define symbols.
/// Just add your own define symbols to the Symbols property at the below.
/// </summary>
[InitializeOnLoad]
public class CiDyScriptDefine : Editor
{
    public static readonly string[] Symbols = new string[] {
        "CiDy",
    };

    /// <summary>
    /// Add define symbols as soon as Unity gets done compiling.
    /// </summary>
    static CiDyScriptDefine()
    {
        // 1. Get the current active BuildTarget using the modern API
        // NamedBuildTarget is the replacement for BuildTargetGroup
        NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        // 2. Use the modern GetScriptingDefineSymbols
        string definesString = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

        List<string> allDefines = definesString.Split(';').ToList();
        allDefines.AddRange(Symbols.Except(allDefines));

        // 3. Use the modern SetScriptingDefineSymbols
        PlayerSettings.SetScriptingDefineSymbols(
            namedTarget,
            string.Join(";", allDefines.ToArray()));
    }
}