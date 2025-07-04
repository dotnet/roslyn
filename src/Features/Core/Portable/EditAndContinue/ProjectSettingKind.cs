// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents project properties and items that impact parse options, compilation options, EnC emit options and source file context interpretation.
/// 
/// Projects settings fall into following categories:
/// 1) Change requires restart (error is reported)
///    E.g. language version
/// 2) Change has no-effect until restart (warning is reported)
///    E.g. output type
/// 3) Change has no effect on emitted IL/metadata, the effect is not observable to the application, or the effect manifests and document content change
///    E.g. analyzer settings, allow unsafe, nullable, checksum algorithm, code page, delay sign, no warn, xml doc file, embedded sources, file alignment, etc.
///    
/// The enum only includes settings from categories [1] and [2].
/// </summary>
internal enum RudeProjectEditKind : ushort
{
    None = 0,
    Unknown = 1,

    /// <summary>
    /// Error to avoid confusion and inconsistencies.
    /// If changed we would interpret changed syntax trees using one language version while unchanged would use another language version.
    /// We would also compile changed members using one version while unchanged would use another.
    /// </summary>
    LangVersion = 2,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    Features = 3,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    DefineConstants = 4,

    /// <summary>
    /// Error for the same reasons as <see cref="LangVersion"/>.
    /// </summary>
    CheckForOverflowUnderflow = 6,

    /// <summary>
    /// Warning since output type only affects entry point.
    /// </summary>
    OutputType = 7,

    /// <summary>
    /// Warning.
    /// </summary>
    StartupObject = 8,

    /// <summary>
    /// Error, to avoid complexity of transitions between Portable and Windows PDB (the latter being obsolete).
    /// </summary>
    DebugType = 9,

    /// <summary>
    /// Error. Debug symbols are required for EnC.
    /// </summary>
    DebugSymbols = 10,

    /// <summary>
    /// Error, need to preserve the module name.
    /// </summary>
    ModuleAssemblyName = 11,

    /// <summary>
    /// Error, need to preserve the assembly identity.
    /// </summary>
    OutputAssembly = 12,

    /// <summary>
    /// Error, need to preserve PDB path.
    /// </summary>
    PdbFile = 13,
}

internal static class ProjectSettingKindExtensions
{
    public static string GetDisplayName(this RudeProjectEditKind kind)
        => Enum.GetName(typeof(RudeProjectEditKind), kind) ?? throw ExceptionUtilities.UnexpectedValue(kind);
}
