// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        protected void Add(string name)
        {
            _builder.Add($"/{name}");
        }

        protected void Add(string name, string value)
        {
            _builder.Add($"/{name}:{value}");
        }

        protected void Add(string name, int value)
        {
            _builder.Add($"/{name}:{value}");
        }

        protected void AddIfNotNullOrWhiteSpace(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _builder.Add($"/{name}:{value}");
            }
        }

        protected void AddIfTrue(string name, bool condition)
        {
            if (condition)
            {
                _builder.Add($"/{name}");
            }
        }

        protected void AddIfTrue(string name, string value, bool condition)
        {
            if (condition)
            {
                _builder.Add($"/{name}:{value}");
            }
        }

        protected void AddIfFalse(string name, bool condition)
        {
            if (!condition)
            {
                _builder.Add($"/{name}");
            }
        }

        protected void AddQuoted(string name, string value)
        {
            _builder.Add($"/{name}:\"{value}\"");
        }

        protected void AddWithPlusOrMinus(string name, bool condition)
        {
            if (condition)
            {
                Add($"/{name}+");
            }
            else
            {
                Add($"/{name}-");
            }
        }

        protected string GetDocumentFilePath(MSB.Framework.ITaskItem documentItem)
        {
            return GetAbsolutePath(documentItem.ItemSpec);
        }

        protected string GetAbsolutePath(string path)
        {
            var directoryPath = PathUtilities.GetDirectoryName(Project.FullPath);
            return Path.GetFullPath(FileUtilities.ResolveRelativePath(path, directoryPath) ?? path);
        }

        protected void ReadAdditionalFiles()
        {
            var additionalFiles = Project.GetAdditionalFiles();
            if (additionalFiles != null)
            {
                foreach (var additionalFile in additionalFiles)
                {
                    AddQuoted("additionalfile", GetDocumentFilePath(additionalFile));
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

        protected void ReadDebugInfo()
        {
            var emitDebugInfo = Project.ReadPropertyBool(PropertyNames.DebugSymbols);
            if (emitDebugInfo)
            {
                var debugType = Project.ReadPropertyString(PropertyNames.DebugType);

                if (string.Equals(debugType, "none", StringComparison.OrdinalIgnoreCase))
                {
                    Add("debug");
                }
                else if (string.Equals(debugType, "pdbonly", StringComparison.OrdinalIgnoreCase))
                {
                    Add("debug", "pdbonly");
                }
                else if (string.Equals(debugType, "full", StringComparison.OrdinalIgnoreCase))
                {
                    Add("debug", "full");
                }
                else if (string.Equals(debugType, "portable", StringComparison.OrdinalIgnoreCase))
                {
                    Add("debug", "portable");
                }
                else if (string.Equals(debugType, "embedded", StringComparison.OrdinalIgnoreCase))
                {
                    Add("debug", "embedded");
                }
            }
        }

        protected void ReadDelaySign()
        {
            var delaySignProperty = Project.GetProperty(PropertyNames.DelaySign);
            if (delaySignProperty != null && !string.IsNullOrWhiteSpace(delaySignProperty.EvaluatedValue))
            {
                AddWithPlusOrMinus("delaysign", Project.ReadPropertyBool(PropertyNames.DelaySign));
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
                    if (!reference.HasReferenceOutputAssemblyMetadataEqualToTrue())
                    {
                        var filePath = GetDocumentFilePath(reference);

                        var aliases = reference.GetAliases();
                        if (aliases.IsDefaultOrEmpty)
                        {
                            AddQuoted("reference", filePath);
                        }
                        else
                        {
                            foreach (var alias in aliases)
                            {
                                Add("reference", $"{alias}=\"{filePath}\"");
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
                    AddQuoted("keyFile", keyFile);
                }

                var keyContainer = Project.ReadPropertyString(PropertyNames.KeyContainerName);
                if (!string.IsNullOrWhiteSpace(keyContainer))
                {
                    AddQuoted("keycontainer", keyContainer);
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
