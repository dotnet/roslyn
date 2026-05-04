// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class CSharpCommandLineArgumentReader : CommandLineArgumentReader
{
    private CSharpCommandLineArgumentReader(MSB.Execution.ProjectInstance project)
        : base(project)
    {
    }

    public static ImmutableArray<string> Read(MSB.Execution.ProjectInstance project)
    {
        return new CSharpCommandLineArgumentReader(project).Read();
    }

    protected override void ReadCore()
    {
        ReadAdditionalFiles();
        ReadAnalyzers();
        ReadCodePage();
        ReadDebugInfo();
        ReadDelaySign();
        ReadErrorReport();
        ReadFeatures();
        ReadImports();
        ReadPlatform();
        ReadReferences();
        ReadSigning();

        AddIfNotNullOrWhiteSpace("appconfig", Project.ReadPropertyString(PropertyNames.AppConfigForCompiler));
        AddIfNotNullOrWhiteSpace("baseaddress", Project.ReadPropertyString(PropertyNames.BaseAddress));
        AddIfTrue("checked", Project.ReadPropertyBool(PropertyNames.CheckForOverflowUnderflow));
        AddIfNotNullOrWhiteSpace("define", Project.ReadPropertyString(PropertyNames.DefineConstants));
        AddIfNotNullOrWhiteSpace("filealign", Project.ReadPropertyString(PropertyNames.FileAlignment));
        AddIfNotNullOrWhiteSpace("doc", Project.ReadItemsAsString(ItemNames.DocFileItem));
        AddIfTrue("fullpaths", Project.ReadPropertyBool(PropertyNames.GenerateFullPaths));
        AddIfTrue("highentropyva", Project.ReadPropertyBool(PropertyNames.HighEntropyVA));
        AddIfNotNullOrWhiteSpace("langversion", Project.ReadPropertyString(PropertyNames.LangVersion));
        AddIfNotNullOrWhiteSpace("main", Project.ReadPropertyString(PropertyNames.StartupObject));
        AddIfNotNullOrWhiteSpace("moduleassemblyname", Project.ReadPropertyString(PropertyNames.ModuleAssemblyName));
        AddIfTrue("nostdlib", Project.ReadPropertyBool(PropertyNames.NoCompilerStandardLib));
        AddIfNotNullOrWhiteSpace("nowarn", Project.ReadPropertyString(PropertyNames.NoWarn));
        AddIfTrue("optimize", Project.ReadPropertyBool(PropertyNames.Optimize));
        AddIfNotNullOrWhiteSpace("out", Project.ReadItemsAsString(PropertyNames.IntermediateAssembly));
        AddIfNotNullOrWhiteSpace("pdb", Project.ReadPropertyString(PropertyNames.PdbFile));
        AddIfNotNullOrWhiteSpace("ruleset", Project.ReadPropertyString(PropertyNames.ResolvedCodeAnalysisRuleSet));
        AddIfNotNullOrWhiteSpace("subsystemversion", Project.ReadPropertyString(PropertyNames.SubsystemVersion));
        AddIfNotNullOrWhiteSpace("target", Project.ReadPropertyString(PropertyNames.OutputType));
        AddIfTrue("unsafe", Project.ReadPropertyBool(PropertyNames.AllowUnsafeBlocks));
        Add("warn", Project.ReadPropertyInt(PropertyNames.WarningLevel));
        AddIfTrue("warnaserror", Project.ReadPropertyBool(PropertyNames.TreatWarningsAsErrors));
        AddIfNotNullOrWhiteSpace("warnaserror+", Project.ReadPropertyString(PropertyNames.WarningsAsErrors));
        AddIfNotNullOrWhiteSpace("warnaserror-", Project.ReadPropertyString(PropertyNames.WarningsNotAsErrors));
    }
}
