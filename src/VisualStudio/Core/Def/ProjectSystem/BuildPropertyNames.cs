// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem;

/// <summary>
/// A class that provides constants for common MSBuild property names.
/// </summary>
internal static class BuildPropertyNames
{
    // Properties received whenever a project is updated.
    //
    // Supported properties can be found in dotnet/project-system repo
    // https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Rules/LanguageService.xaml.

    public const string RootNamespace = nameof(RootNamespace);
    public const string MaxSupportedLangVersion = nameof(MaxSupportedLangVersion);
    public const string RunAnalyzers = nameof(RunAnalyzers);
    public const string RunAnalyzersDuringLiveAnalysis = nameof(RunAnalyzersDuringLiveAnalysis);
    public const string TemporaryDependencyNodeTargetIdentifier = nameof(TemporaryDependencyNodeTargetIdentifier);
    public const string TargetRefPath = nameof(TargetRefPath);

    // Properties requested at project creation time.

    public const string MSBuildProjectFullPath = nameof(MSBuildProjectFullPath);
    public const string TargetPath = nameof(TargetPath);
    public const string AssemblyName = nameof(AssemblyName);
    public const string CommandLineArgsForDesignTimeEvaluation = nameof(CommandLineArgsForDesignTimeEvaluation);
    public const string IntermediateAssembly = nameof(IntermediateAssembly);

    public static readonly ImmutableArray<string> InitialEvaluationPropertyNames = [MSBuildProjectFullPath, TargetPath, AssemblyName, CommandLineArgsForDesignTimeEvaluation];

    public static readonly ImmutableArray<string> InitialEvaluationItemNames = [IntermediateAssembly];
}
