// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable CS8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

/// <summary>
/// Context passed to a source transformer when <see cref="ISourceTransformerWithServices.InitializeServices"/> is called.
/// </summary>
// ReSharper disable once ClassCannotBeInstantiated
public sealed class InitializeServicesContext
{
#if !METALAMA_COMPILER_INTERFACE
    private readonly DiagnosticBag _diagnostics;

    internal InitializeServicesContext(
        Compilation compilation,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        InitializeServicesOptions options,
        DiagnosticBag diagnostics)
    {
        Compilation = compilation;
        Options = options;
        AnalyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
        _diagnostics = diagnostics;
    }
#else
    private InitializeServicesContext()
    {
    }
#endif

    /// <summary>
    /// Gets the <see cref="Compilation"/>. 
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Gets options of the current <see cref="InitializeServicesContext"/>.
    /// </summary>
    public InitializeServicesOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="AnalyzerConfigOptionsProvider"/>, which allows to access <c>.editorconfig</c> options.
    /// </summary>
    public AnalyzerConfigOptionsProvider AnalyzerConfigOptionsProvider { get; }

    /// <summary>
    /// Adds a <see cref="Diagnostic"/> to the user's compilation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
    /// <remarks>
    /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
    /// </remarks>
    public void ReportDiagnostic(Diagnostic diagnostic)
    {
#if !METALAMA_COMPILER_INTERFACE
        _diagnostics.Add(diagnostic);
#endif
    }
}

/// <summary>
/// Options of a <see cref="ISourceTransformer"/>, exposed on <see cref="InitializeServicesContext.Options"/>.
/// </summary>
public sealed class InitializeServicesOptions
{
    public bool IsLongRunningProcess { get; }

    internal InitializeServicesOptions(bool isLongRunningProcess)
    {
        IsLongRunningProcess = isLongRunningProcess;
    }
}
