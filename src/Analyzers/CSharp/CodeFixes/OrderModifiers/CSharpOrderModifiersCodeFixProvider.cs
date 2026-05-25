// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.OrderModifiers), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpOrderModifiersCodeFixProvider()
    : AbstractOrderModifiersCodeFixProvider(CSharpSyntaxFacts.Instance, CSharpOrderModifiersHelper.Instance)
{
    private const string CS8652 = nameof(CS8652); // The feature 'relaxed modifier ordering' is currently in Preview and unsupported. To use Preview features, use the 'preview' language version.

    protected override CodeStyleOption2<string> GetCodeStyleOption(AnalyzerOptionsProvider options)
        => ((CSharpAnalyzerOptionsProvider)options).PreferredModifierOrder;

    protected override ImmutableArray<string> FixableCompilerErrorIds { get; } = [CS8652];
}
