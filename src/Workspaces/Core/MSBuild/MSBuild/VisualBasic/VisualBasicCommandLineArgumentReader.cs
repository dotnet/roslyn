// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicCommandLineArgumentReader : CommandLineArgumentReader
    {
        public VisualBasicCommandLineArgumentReader(MSB.Execution.ProjectInstance project)
            : base(project)
        {
        }

        public static ImmutableArray<string> Read(MSB.Execution.ProjectInstance project)
        {
            return new VisualBasicCommandLineArgumentReader(project).Read();
        }

        protected override void ReadCore()
        {
            ReadAdditionalFiles();
            ReadAnalyzers();
            ReadCodePage();
            ReadDebugInfo();
            ReadDelaySign();
            ReadDoc();
            ReadErrorReport();
            ReadFeatures();
            ReadImports();
            ReadOptions();
            ReadPlatform();
            ReadReferences();
            ReadSigning();
            ReadVbRuntime();

            AddIfNotNullOrWhiteSpace("baseaddress", Project.ReadPropertyString(PropertyNames.BaseAddress));
            AddIfNotNullOrWhiteSpace("define", Project.ReadPropertyString(PropertyNames.FinalDefineConstants));
            AddIfNotNullOrWhiteSpace("filealign", Project.ReadPropertyString(PropertyNames.FileAlignment));
            AddIfTrue("highentropyva", Project.ReadPropertyBool(PropertyNames.HighEntropyVA));
            AddIfNotNullOrWhiteSpace("langversion", Project.ReadPropertyString(PropertyNames.LangVersion));
            AddIfNotNullOrWhiteSpace("main", Project.ReadPropertyString(PropertyNames.StartupObject));
            AddIfNotNullOrWhiteSpace("moduleassemblyname", Project.ReadPropertyString(PropertyNames.ModuleAssemblyName));
            AddIfTrue("netcf", Project.ReadPropertyBool(PropertyNames.TargetCompactFramework));
            AddIfTrue("nostdlib", Project.ReadPropertyBool(PropertyNames.NoCompilerStandardLib));
            AddIfNotNullOrWhiteSpace("nowarn", Project.ReadPropertyString(PropertyNames.NoWarn));
            AddIfTrue("nowarn", Project.ReadPropertyBool(PropertyNames._NoWarnings));
            AddIfTrue("optimize", Project.ReadPropertyBool(PropertyNames.Optimize));
            AddIfNotNullOrWhiteSpace("out", Project.ReadItemsAsString(PropertyNames.IntermediateAssembly));
            AddIfTrue("removeintchecks", Project.ReadPropertyBool(PropertyNames.RemoveIntegerChecks));
            AddIfNotNullOrWhiteSpace("rootnamespace", Project.ReadPropertyString(PropertyNames.RootNamespace));
            AddIfNotNullOrWhiteSpace("ruleset", Project.ReadPropertyString(PropertyNames.ResolvedCodeAnalysisRuleSet));
            AddIfNotNullOrWhiteSpace("sdkpath", Project.ReadPropertyString(PropertyNames.FrameworkPathOverride));
            AddIfNotNullOrWhiteSpace("subsystemversion", Project.ReadPropertyString(PropertyNames.SubsystemVersion));
            AddIfNotNullOrWhiteSpace("target", Project.ReadPropertyString(PropertyNames.OutputType));
            AddIfTrue("warnaserror", Project.ReadPropertyBool(PropertyNames.TreatWarningsAsErrors));
            AddIfNotNullOrWhiteSpace("warnaserror+", Project.ReadPropertyString(PropertyNames.WarningsAsErrors));
            AddIfNotNullOrWhiteSpace("warnaserror-", Project.ReadPropertyString(PropertyNames.WarningsNotAsErrors));
        }

        private void ReadDoc()
        {
            var documentationFile = Project.ReadPropertyString(PropertyNames.DocFileItem);
            var generateDocumentation = Project.ReadPropertyBool(PropertyNames.GenerateDocumentation);

            var hasDocumentationFile = !RoslynString.IsNullOrWhiteSpace(documentationFile);

            if (hasDocumentationFile || generateDocumentation)
            {
                if (!RoslynString.IsNullOrWhiteSpace(documentationFile))
                {
                    Add("doc", documentationFile);
                }
                else
                {
                    Add("doc");
                }
            }
        }

        private void ReadOptions()
        {
            var optionCompare = Project.ReadPropertyString(PropertyNames.OptionCompare);
            if (string.Equals("binary", optionCompare, StringComparison.OrdinalIgnoreCase))
            {
                Add("optioncompare", "binary");
            }
            else if (string.Equals("text", optionCompare, StringComparison.OrdinalIgnoreCase))
            {
                Add("optioncompare", "text");
            }

            // default is on/true
            AddIfFalse("optionexplicit-", Project.ReadPropertyBool(PropertyNames.OptionExplicit));

            AddIfTrue("optioninfer", Project.ReadPropertyBool(PropertyNames.OptionInfer));
            AddWithPlusOrMinus("optionstrict", Project.ReadPropertyBool(PropertyNames.OptionStrict));
            AddIfNotNullOrWhiteSpace("optionstrict", Project.ReadPropertyString(PropertyNames.OptionStrictType));
        }

        private void ReadVbRuntime()
        {
            var vbRuntime = Project.ReadPropertyString(PropertyNames.VbRuntime);
            if (!RoslynString.IsNullOrWhiteSpace(vbRuntime))
            {
                if (string.Equals("default", vbRuntime, StringComparison.OrdinalIgnoreCase))
                {
                    Add("vbruntime+");
                }
                else if (string.Equals("embed", vbRuntime, StringComparison.OrdinalIgnoreCase))
                {
                    Add("vbruntime*");
                }
                else if (string.Equals("none", vbRuntime, StringComparison.OrdinalIgnoreCase))
                {
                    Add("vbruntime-");
                }
                else
                {
                    Add("vbruntime", vbRuntime);
                }
            }
        }
    }
}
