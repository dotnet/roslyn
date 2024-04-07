// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FullyQualify), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
internal sealed class CSharpFullyQualifyCodeFixProvider : AbstractFullyQualifyCodeFixProvider
{
    /// <summary>
    /// name does not exist in context
    /// </summary>
    private const string CS0103 = nameof(CS0103);

    /// <summary>
    /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
    /// </summary>
    private const string CS0104 = nameof(CS0104);

    /// <summary>
    /// type or namespace could not be found
    /// </summary>
    private const string CS0246 = nameof(CS0246);

    /// <summary>
    /// wrong number of type args
    /// </summary>
    private const string CS0305 = nameof(CS0305);

    /// <summary>
    /// The non-generic type 'A' cannot be used with type arguments
    /// </summary>
    private const string CS0308 = nameof(CS0308);

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpFullyQualifyCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [CS0103, CS0104, CS0246, CS0305, CS0308, IDEDiagnosticIds.UnboundIdentifierId];
}
