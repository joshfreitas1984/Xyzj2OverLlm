﻿using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Contracts;
using EnglishPatch.Support;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnglishPatch.DynamicStrings;

/// <summary>
/// Used to replace hardcoded strings in IL
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.DynamicStringDumperPlugin", "DynamicStringDumperPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class StringDumperPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        // Disable Dumper for non devs
        DumpFiles(@"G:/Xyzj2OverLlm/Files/Raw/DynamicStrings/dynamicStrings.txt");
    }

    public void DumpFiles(string outputPath)
    {
        Logger.LogError("Dumping dynamic strings...");

        try
        {
            //!!! Manually copy YamlDotNet.dll to the ManagedDlls folder
            var serializer = Yaml.CreateSerializer();
            string gamePath = Paths.ManagedPath;
            string assemblyPath = Path.Combine(gamePath, "Assembly-CSharp.dll");

            var contracts = new List<DynamicStringContract>();
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            int count = 0;

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    ProcessType(type, contracts);
                    count++;
                }
            }

            //YAML is too hard to handle when splitting
            //File.WriteAllText(outputPath, serializer.Serialize(contracts));

            var lines = new List<string>();
            foreach (var contract in contracts)
            {
                var parameters = CleanForCsv($"[{string.Join(",", contract.Parameters)}]");
                lines.Add($"{CleanForCsv(contract.Type)},{CleanForCsv(contract.Method)},{CleanForCsv(contract.ILOffset.ToString())},{CleanForCsv(contract.Raw)},{parameters}");
            }

            File.WriteAllLines(outputPath, lines);
            Logger.LogWarning($"Dumped to: {outputPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error dumping: {ex.Message}");
        }
    }

    public string CleanForCsv(string input)
    {
        return input.Replace(",", "，")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private void ProcessType(TypeDefinition type, List<DynamicStringContract> stringReferences, int recursionLevel = 0)
    {
        // Process nested types
        foreach (var nestedType in type.NestedTypes)
            if (!stringReferences.Any(r => r.Type == nestedType.ToString()) && recursionLevel < 15) //15 should be fine
                ProcessType(nestedType, stringReferences, recursionLevel++);

        // Process methods
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            foreach (var instruction in method.Body.Instructions)
            {
                string operandValue = string.Empty;

                if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string stringValue)
                {
                    operandValue = stringValue;
                }
                else if (instruction.OpCode == OpCodes.Ldc_I4
                    && instruction.Operand is int intValue
                    && intValue >= char.MinValue
                    && intValue <= char.MaxValue
                    && IsLikelyCharParameter(instruction))
                {
                    operandValue = $"INVALIDCHAR: {(char)intValue}";
                }

                // Look for string load operations
                if (!string.IsNullOrWhiteSpace(operandValue) && Regex.IsMatch(operandValue, MainPlugin.ChineseCharPattern))
                {
                    // Add to our list
                    stringReferences.Add(new DynamicStringContract
                    {
                        Type = type.FullName,
                        Method = method.Name,
                        Raw = operandValue,
                        ILOffset = instruction.Offset,
                        Parameters = method.Parameters.Select(p => p.ParameterType.FullName).ToArray(),
                    });
                }
            }
        }
    }

    public static bool IsLikelyCharParameter(Mono.Cecil.Cil.Instruction currentInstruction)
    {
        // Get the next instruction after loading the potential character value
        var nextInstruction = currentInstruction.Next;

        // Check if there's no next instruction
        if (nextInstruction == null)
            return false;

        // Case 1: Direct call to a method that takes a char parameter
        if (nextInstruction.OpCode == OpCodes.Call || nextInstruction.OpCode == OpCodes.Callvirt)
        {
            MethodReference calledMethod = nextInstruction.Operand as MethodReference;
            if (calledMethod != null && calledMethod.Parameters.Count > 0)
            {
                // Check if the first parameter is a char
                // (or if any parameter is a char, depending on your needs)
                foreach (ParameterDefinition param in calledMethod.Parameters)
                {
                    if (param.ParameterType.FullName == "System.Char")
                        return true;
                }
            }
        }

        // Case 2: Character being boxed (common for Split and other methods)
        if (nextInstruction.OpCode == OpCodes.Box &&
            nextInstruction.Operand is TypeReference typeRef &&
            typeRef.FullName == "System.Char")
        {
            return true;
        }

        // Case 3: Looking at a string Split method specifically
        // This might require more context - looking ahead several instructions
        if (IsPartOfStringSplit(currentInstruction))
        {
            return true;
        }

        return false;
    }

    public static bool IsPartOfStringSplit(Mono.Cecil.Cil.Instruction loadInstruction)
    {
        // Look ahead several instructions for a pattern that matches String.Split
        var current = loadInstruction;

        // Skip ahead a few instructions looking for the call
        for (int i = 0; i < 5 && current.Next != null; i++)
        {
            current = current.Next;

            if ((current.OpCode == OpCodes.Call || current.OpCode == OpCodes.Callvirt) &&
                current.Operand is MethodReference methodRef)
            {
                // Check if the method is String.Split
                if (methodRef.DeclaringType.FullName == "System.String" &&
                    methodRef.Name == "Split")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
