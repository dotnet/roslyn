// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents project properties and items that impact parse options, compilation options and source file context interpretation.
/// 
/// Project settings fall into following categories:
/// 1) Change requires restart. Error is reported.
///    E.g. language version
///    
/// 2) Change has no-effect until restart. Warning is reported if tracked by <see cref="Project"/>.
///    E.g. output type, platform, emit options
///    Not all project settings are tracked by Roslyn. Only those that are are reported.
///    If we want to enable auto-restart when a setting is changed we need to track it in <see cref="Project"/>.
/// 
/// 5) Change has no effect on emitted IL/metadata, the effect is not observable to the application. Not reported.
///    E.g. analyzer settings, allow unsafe, nullable, code page, delay sign, no warn, xml doc file, embedded sources, etc.
///    
/// The enum only includes settings from categories [1] and [2].
/// The names of the enum members should match the corresponding msbuild property names.
/// </summary>
internal enum ProjectSettingKind
{
    /// <summary>
    /// Error to avoid confusion and inconsistencies.
    /// If changed we would interpret changed syntax trees using one language version while unchanged would use another language version.
    /// We would also compile changed members using one version while unchanged would use another.
    /// </summary>
    LangVersion = 0,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    Features = 1,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    DefineConstants = 2,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    CheckForOverflowUnderflow = 3,

    /// <summary>
    /// Warning since output type only affects entry point.
    /// </summary>
    OutputType = 4,

    /// <summary>
    /// Warning.
    /// </summary>
    StartupObject = 5,

    /// <summary>
    /// Error, need to preserve the module name.
    /// </summary>
    ModuleAssemblyName = 9,

    /// <summary>
    /// Error, need to preserve the assembly name.
    /// </summary>
    AssemblyName = 10,

    /// <summary>
    /// Warning, can't be changed without restarting the application but not blocking.
    /// </summary>
    Platform = 11,

    /// <summary>
    /// Must be <see cref="OptimizationLevel.Debug"/>.
    /// </summary>
    OptimizationLevel = 12,

    // VB specific settings

    /// <summary>
    /// Error. Can't change namespace of existing types.
    /// </summary>
    RootNamespace = 50,

    OptionStrict = 51,
    OptionInfer = 52,
    OptionExplicit = 53,
    OptionCompare = 54,
}

internal static class ProjectSettingKindExtensions
{
    public static bool IsWarning(this ProjectSettingKind kind)
        => kind is
           ProjectSettingKind.OutputType or
           ProjectSettingKind.StartupObject or
           ProjectSettingKind.Platform;
}
