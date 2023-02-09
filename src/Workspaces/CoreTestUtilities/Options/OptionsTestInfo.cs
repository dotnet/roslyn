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

internal readonly record struct OptionsTestInfo(IOption2 Option, List<(string namespaceName, string qualifiedName, bool isPublic, IOption2 option)> Accessors)
{
    public static Dictionary<string, OptionsTestInfo> CollectOptions(string directory)
    {
        var result = new Dictionary<string, OptionsTestInfo>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if ((fileName.StartsWith("Microsoft.CodeAnalysis") || fileName.StartsWith("Microsoft.VisualStudio.LanguageServices")) &&
                !fileName.Contains("Test"))
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

                foreach (var type in types)
                {
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (field.Name.Contains("SpacingAfterMethodDeclarationName"))
                        {
                        }

                        if (typeof(IOption2).IsAssignableFrom(field.FieldType))
                        {
                            Assert.False(type.IsGenericType, "Option should not be defined in a generic type");

                            var option = (IOption2)field.GetValue(null)!;
                            Assert.NotNull(option);

                            var isBackingField = field.Name.EndsWith("k__BackingField");
                            var unmangledName = isBackingField ? field.Name[(field.Name.IndexOf('<') + 1)..field.Name.IndexOf('>')] : field.Name;
                            var accessor = type.FullName + "." + unmangledName;
                            var isPublic = type.IsPublic && (isBackingField ? type.GetProperty(unmangledName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetMethod!.IsPublic : field.IsPublic);
                            var accessorNamespace = type.Namespace;
                            Assert.NotNull(accessorNamespace);

                            var configName = option.Definition.ConfigName;
                            if (result.TryGetValue(configName, out var optionInfo))
                            {
                                optionInfo.Accessors.Add((accessorNamespace!, accessor, isPublic, option));
                            }
                            else
                            {
                                optionInfo = new OptionsTestInfo(option, new List<(string, string, bool, IOption2)> { (accessorNamespace!, accessor, isPublic, option) });
                            }

                            result[configName] = optionInfo;
                        }
                    }
                }
            }
        }

        return result;
    }
}
