// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.CSharp.Wrapping.ChainedExpression;
using Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.Wrapping), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpWrappingCodeRefactoringProvider() : AbstractWrappingCodeRefactoringProvider(s_wrappers)
{
    private static readonly ImmutableArray<ISyntaxWrapper> s_wrappers =
        ImmutableArray.Create<ISyntaxWrapper>(
            new CSharpArgumentWrapper(),
            new CSharpParameterWrapper(),
            new CSharpBinaryExpressionWrapper(),
            new CSharpChainedExpressionWrapper(),
            new CSharpInitializerExpressionWrapper(),
            new CSharpCollectionExpressionWrapper());

    protected override SyntaxWrappingOptions GetWrappingOptions(IOptionsReader options, CodeActionOptions ideOptions)
        => options.GetCSharpSyntaxWrappingOptions(ideOptions);
}
