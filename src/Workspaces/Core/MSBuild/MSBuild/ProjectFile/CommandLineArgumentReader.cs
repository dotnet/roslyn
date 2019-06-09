// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class CommandLineArgumentReader
    {
        protected readonly MSB.Execution.ProjectInstance Project;
        private readonly ImmutableArray<string>.Builder _builder;

        protected CommandLineArgumentReader(MSB.Execution.ProjectInstance project)
        {
            Project = project;
            _builder = ImmutableArray.CreateBuilder<string>();
        }

        protected abstract void ReadCore();

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Contains(char.IsWhiteSpace))
            {
                throw new ArgumentException($"Parameter cannot be null, empty, or contain whitespace.", nameof(name));
            }
        }

        protected void Add(string name)
        {
            ValidateName(name);

            _builder.Add($"/{name}");
        }

        protected void Add(string name, string value)
        {
            ValidateName(name);

            if (string.IsNullOrEmpty(value) || value.Contains(char.IsWhiteSpace))
            {
                _builder.Add($"/{name}:\"{value}\"");
            }
            else
            {
                _builder.Add($"/{name}:{value}");
            }
        }

        protected void Add(string name, int value)
        {
            Add(name, value.ToString());
        }

        protected void AddIfNotNullOrWhiteSpace(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Add(name, value);
            }
        }

        protected void AddIfTrue(string name, bool condition)
        {
            if (condition)
            {
                Add(name);
            }
        }

        protected void AddIfTrue(string name, string value, bool condition)
        {
            if (condition)
            {
                Add(name, value);
            }
        }

        protected void AddIfFalse(string name, bool condition)
        {
            if (!condition)
            {
                Add(name);
            }
        }

        protected void AddWithPlus(string name)
        {
            Add($"{name}+");
        }

        protected void AddWithMinus(string name)
        {
            Add($"{name}-");
        }

        protected void AddWithPlusOrMinus(string name, bool condition)
        {
            if (condition)
            {
                AddWithPlus(name);
            }
            else
            {
                AddWithMinus(name);
            }
        }

        protected string GetDocumentFilePath(MSB.Framework.ITaskItem documentItem)
        {
            return GetAbsolutePath(documentItem.ItemSpec);
        }

        protected string GetAbsolutePath(string path)
        {
            var baseDirectory = PathUtilities.GetDirectoryName(Project.FullPath);
            var absolutePath = FileUtilities.ResolveRelativePath(path, baseDirectory) ?? path;
            return FileUtilities.TryNormalizeAbsolutePath(absolutePath) ?? absolutePath;
        }

        protected void ReadAdditionalFiles()
        {
            var additionalFiles = Project.GetAdditionalFiles();
            if (additionalFiles != null)
            {
                foreach (var additionalFile in additionalFiles)
                {
                    Add("additionalfile", GetDocumentFilePath(additionalFile));
                }
            }
        }

        protected void ReadAnalyzers()
        {
            var analyzers = Project.GetAnalyzers();
            if (analyzers != null)
            {
                foreach (var analyzer in analyzers)
                {
                    Add("analyzer", GetDocumentFilePath(analyzer));
                }
            }
        }

        protected void ReadCodePage()
        {
            var codePage = Project.ReadPropertyInt(PropertyNames.CodePage);
            AddIfTrue("codepage", codePage.ToString(), codePage != 0);
        }

        private static readonly ImmutableDictionary<string, string> s_debugTypeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "none", "none" },
            { "pdbonly", "pdbonly" },
            { "full", "full" },
            { "portable", "portable" },
            { "embedded", "embedded" }
        }.ToImmutableDictionary();

        protected void ReadDebugInfo()
        {
            var emitDebugInfo = Project.ReadPropertyBool(PropertyNames.DebugSymbols);
            if (emitDebugInfo)
            {
                var debugType = Project.ReadPropertyString(PropertyNames.DebugType);
                if (debugType != null && s_debugTypeValues.TryGetValue(debugType, out var value))
                {
                    Add("debug", value);
                }
            }
        }

        protected void ReadDelaySign()
        {
            var delaySign = Project.ReadPropertyString(PropertyNames.DelaySign);
            if (!string.IsNullOrWhiteSpace(delaySign))
            {
                AddWithPlusOrMinus("delaysign", Conversions.ToBool(delaySign));
            }
        }

        protected void ReadErrorReport()
        {
            var errorReport = Project.ReadPropertyString(PropertyNames.ErrorReport);
            if (!string.IsNullOrWhiteSpace(errorReport))
            {
                Add("errorreport", errorReport.ToLower());
            }
        }

        protected void ReadFeatures()
        {
            var features = Project.ReadPropertyString(PropertyNames.Features);
            if (!string.IsNullOrWhiteSpace(features))
            {
                foreach (var feature in CompilerOptionParseUtilities.ParseFeatureFromMSBuild(features))
                {
                    Add("features", feature);
                }
            }
        }

        protected void ReadImports()
        {
            var imports = Project.GetTaskItems(ItemNames.Import);
            if (imports != null)
            {
                AddIfNotNullOrWhiteSpace("imports", string.Join(",", imports.Select(item => item.ItemSpec.Trim())));
            }
        }

        protected void ReadPlatform()
        {
            var platform = Project.ReadPropertyString(PropertyNames.PlatformTarget);
            var prefer32bit = Project.ReadPropertyBool(PropertyNames.Prefer32Bit);

            if (prefer32bit && (string.IsNullOrWhiteSpace(platform) || string.Equals("anycpu", platform, StringComparison.OrdinalIgnoreCase)))
            {
                platform = "anycpu32bitpreferred";
            }

            AddIfNotNullOrWhiteSpace("platform", platform);
        }

        protected void ReadReferences()
        {
            var references = Project.GetMetadataReferences();
            if (references != null)
            {
                foreach (var reference in references)
                {
                    if (reference.ReferenceOutputAssemblyIsTrue())
                    {
                        var filePath = GetDocumentFilePath(reference);

                        var aliases = reference.GetAliases();
                        if (aliases.IsDefaultOrEmpty)
                        {
                            Add("reference", filePath);
                        }
                        else
                        {
                            foreach (var alias in aliases)
                            {
                                if (string.Equals(alias, "global", StringComparison.OrdinalIgnoreCase))
                                {
                                    Add("reference", filePath);
                                }
                                else
                                {
                                    Add("reference", $"{alias}=\"{filePath}\"");
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void ReadSigning()
        {
            var signAssembly = Project.ReadPropertyBool(PropertyNames.SignAssembly);
            if (signAssembly)
            {
                var keyFile = Project.ReadPropertyString(PropertyNames.KeyOriginatorFile);
                if (!string.IsNullOrWhiteSpace(keyFile))
                {
                    Add("keyFile", keyFile);
                }

                var keyContainer = Project.ReadPropertyString(PropertyNames.KeyContainerName);
                if (!string.IsNullOrWhiteSpace(keyContainer))
                {
                    Add("keycontainer", keyContainer);
                }
            }
        }

        protected ImmutableArray<string> Read()
        {
            ReadCore();
            return _builder.ToImmutable();
        }
    }
}
