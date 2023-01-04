// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

internal sealed class OptionsTestInfo
{
    public readonly HashSet<string> Assemblies = new();
    public readonly Dictionary<string, (IOption2 option, string accessor, bool isPublic)> EditorConfig = new();
    public readonly Dictionary<string, (IOption2 option, string? language, string accessor, string storage)> GlobalOptions = new();

    public override string ToString()
    {
        var lines = new List<string>();

        lines.Add("Assemblies:");
        lines.AddRange(Assemblies.OrderBy(e => e));
        lines.Add("");

        lines.Add("");
        lines.Add("    private static readonly Dictionary<string, VisualStudioOptionStorage> s_storages = new()");
        lines.Add("    {");
        lines.AddRange(GlobalOptions.OrderBy(e => e.Key).Select(e => $"        {{\"{e.Key}\", {e.Value.storage}}},"));
        lines.Add("    };");

        lines.Add("");
        lines.AddRange(EditorConfig.OrderBy(e => (e.Value.isPublic, e.Key)).Select(e => $"\"{e.Key}\", {e.Value.accessor}{(e.Value.isPublic ? " [public]" : "")}"));

        lines.Add("");
        lines.Add("    private static readonly Dictionary<string, string> s_legacyNameMap = new()");
        lines.Add("    {");
        lines.AddRange(from e in EditorConfig
                       where e.Value.isPublic
                       select $"        {{\"{e.Value.option.Name}\", \"{e.Key}\"}},");
        lines.Add("    };");

        return string.Join(Environment.NewLine, lines);
    }

    public static OptionsTestInfo CollectOptions(string directory)
    {
        var result = new OptionsTestInfo();
        foreach (var file in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.StartsWith("Microsoft.CodeAnalysis") || fileName.StartsWith("Microsoft.VisualStudio.LanguageServices"))
            {
                Type[] types;
                try
                {
                    types = Assembly.Load(fileName).GetTypes();
                }
                catch
                {
                    continue;
                }

                var language = file.Contains("CSharp") ? "CSharp" : file.Contains("VisualBasic") ? "VisualBasic" : null;

                foreach (var type in types)
                {
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (typeof(IOption2).IsAssignableFrom(field.FieldType))
                        {
                            Assert.False(type.IsGenericType, "Option should not be defined in a generic type");

                            result.Assemblies.Add(fileName);

                            var option = (IOption2)field.GetValue(null)!;
                            Assert.NotNull(option);

                            var isBackingField = field.Name.EndsWith("k__BackingField");
                            var unmangledName = isBackingField ? field.Name[(field.Name.IndexOf('<') + 1)..field.Name.IndexOf('>')] : field.Name;
                            var accessor = type.FullName + "." + unmangledName;
                            var isPublic = type.IsPublic && (isBackingField ? type.GetProperty(unmangledName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetMethod!.IsPublic : field.IsPublic);

                            var configName = option.OptionDefinition.ConfigName;

                            static string? TryGetStorage(OptionStorageLocation location)
                                => location switch
                                {
                                    ClientSettingsStorageLocation vsSettingStorage =>
                                        (vsSettingStorage.KeyName != null)
                                        ? $"new RoamingProfileStorage(\"{vsSettingStorage.KeyName}\")"
                                        : $"new RoamingProfileStorage(\"{vsSettingStorage.GetKeyNameForLanguage("%LANGUAGE%")}\", \"{vsSettingStorage.GetKeyNameForLanguage(LanguageNames.VisualBasic)}\")",
                                    FeatureFlagStorageLocation featureFlagStorage =>
                                        $"new FeatureFlagStorage(@\"{featureFlagStorage.Name}\")",
                                    LocalUserProfileStorageLocation userProfileStorage =>
                                        $"new LocalUserProfileStorage(@\"{Path.GetDirectoryName(userProfileStorage.KeyName)}\", \"{Path.GetFileName(userProfileStorage.KeyName)}\")",
                                    _ => null
                                };

                            var newStorages = (from l in option.StorageLocations let s = TryGetStorage(l) where s != null select s).ToArray();

                            var newStorage = (newStorages.Length > 1)
                                ? $"new CompositeStorage({string.Join(", ", newStorages)})"
                                : newStorages.SingleOrDefault();

                            if (newStorage != null)
                            {
                                if (result.GlobalOptions.TryGetValue(configName, out var existing))
                                {
                                    Assert.Equal(existing.storage, newStorage);
                                }
                                else
                                {
                                    result.GlobalOptions.Add(configName, (option, language, accessor, newStorage));
                                }
                            }

                            var ecStorage = option.StorageLocations.OfType<IEditorConfigStorageLocation2>().SingleOrDefault();
                            if (ecStorage != null)
                            {
                                if (result.EditorConfig.TryGetValue(configName, out var existing))
                                {
                                    isPublic |= existing.isPublic;
                                    Assert.Equal(existing.option.Name, option.Name);
                                }

                                result.EditorConfig[configName] = (option, accessor, isPublic);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}
