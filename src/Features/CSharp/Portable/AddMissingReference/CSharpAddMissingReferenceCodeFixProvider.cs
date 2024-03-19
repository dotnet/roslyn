// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddMissingReference;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CSharp.AddMissingReference;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddMissingReference), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.SimplifyNames)]
internal class CSharpAddMissingReferenceCodeFixProvider : AbstractAddMissingReferenceCodeFixProvider
{
    private const string CS0012 = nameof(CS0012); // The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'ProjectA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [CS0012];

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpAddMissingReferenceCodeFixProvider()
    {
    }

    /// <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
    internal CSharpAddMissingReferenceCodeFixProvider(
        IPackageInstallerService installerService,
        ISymbolSearchService symbolSearchService)
        : base(installerService, symbolSearchService)
    {
    }
}
