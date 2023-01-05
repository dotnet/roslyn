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

internal readonly record struct OptionsTestInfo(IOption2 Option, string? ContainingAssemblyLanguage, List<(string qualifiedName, IOption2 option)> Accessors, bool HasPublicAccessor)
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

                var language = file.Contains("CSharp") ? "CSharp" : file.Contains("VisualBasic") ? "VisualBasic" : null;

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
                            var hasPublicAccessor = type.IsPublic && (isBackingField ? type.GetProperty(unmangledName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetMethod!.IsPublic : field.IsPublic);

                            var configName = option.Definition.ConfigName;
                            if (result.TryGetValue(configName, out var optionInfo))
                            {
                                optionInfo.Accessors.Add((accessor, option));

                                if (hasPublicAccessor)
                                {
                                    optionInfo = optionInfo with { HasPublicAccessor = true };
                                }
                            }
                            else
                            {
                                optionInfo = new OptionsTestInfo(option, language, new List<(string, IOption2)> { (accessor, option) }, hasPublicAccessor);
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
