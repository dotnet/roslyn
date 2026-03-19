// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <summary>
/// Code style analyzer that reports at least one 'unnecessary' code diagnostic.
/// </summary>
internal abstract class AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    /// <summary>
    /// Constructor for an unnecessary code style analyzer with a single diagnostic descriptor and
    /// unique <see cref="IPerLanguageValuedOption"/> code style option.
    /// </summary>
    /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
    /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
    /// <param name="option">
    /// Code style option that can be used to configure the given <paramref name="diagnosticId"/>.
    /// <see langword="null"/>, if there is no such unique option.
    /// </param>
    /// <param name="title">Title for the diagnostic descriptor</param>
    /// <param name="messageFormat">
    /// Message for the diagnostic descriptor.
    /// <see langword="null"/> if the message is identical to the title.
    /// </param>
    /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
    protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
        string diagnosticId,
        EnforceOnBuild enforceOnBuild,
        IOption2? option,
        LocalizableString title,
        LocalizableString? messageFormat = null,
        bool configurable = true)
        : base(diagnosticId, enforceOnBuild, option, title, messageFormat, isUnnecessary: true, configurable)
    {
    }

    /// <summary>
    /// Constructor for an unnecessary code style analyzer with a single diagnostic descriptor and
    /// two or more <see cref="IPerLanguageValuedOption"/> code style options.
    /// </summary>
    /// <param name="diagnosticId">Diagnostic ID reported by this analyzer</param>
    /// <param name="enforceOnBuild">Build enforcement recommendation for this analyzer</param>
    /// <param name="options">
    /// Set of two or more per-language options that can be used to configure the diagnostic severity of the given diagnosticId.
    /// </param>
    /// <param name="title">Title for the diagnostic descriptor</param>
    /// <param name="messageFormat">
    /// Message for the diagnostic descriptor.
    /// Null if the message is identical to the title.
    /// </param>
    /// <param name="configurable">Flag indicating if the reported diagnostics are configurable by the end users</param>
    protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
        string diagnosticId,
        EnforceOnBuild enforceOnBuild,
        ImmutableHashSet<IOption2> options,
        LocalizableString title,
        LocalizableString? messageFormat = null,
        bool configurable = true)
        : base(diagnosticId, enforceOnBuild, options, title, messageFormat, isUnnecessary: true, configurable)
    {
    }

    /// <summary>
    /// Constructor for an unnecessary code style analyzer with multiple descriptors.
    /// </summary>
    /// <param name="descriptors">Descriptors supported by this analyzer</param>
    protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> descriptors)
        : base(descriptors)
    {
    }

    /// <summary>
    /// Constructor for a code style analyzer with a multiple diagnostic descriptors with a code style options that can be used to configure each descriptor.
    /// </summary>
    protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableArray<(DiagnosticDescriptor Descriptor, IOption2 Option)> supportedDiagnosticsWithOptions)
        : base(supportedDiagnosticsWithOptions)
    {
    }

    /// <summary>
    /// Constructor for a code style analyzer with multiple diagnostic descriptors with zero or more code style options that can be used to configure each descriptor.
    /// </summary>
    protected AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(ImmutableArray<(DiagnosticDescriptor Descriptor, ImmutableHashSet<IOption2> Options)> supportedDiagnosticsWithOptions)
        : base(supportedDiagnosticsWithOptions)
    {
    }
}
