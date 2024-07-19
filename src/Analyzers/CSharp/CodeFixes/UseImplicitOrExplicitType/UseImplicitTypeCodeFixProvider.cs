// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseImplicitType), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class UseImplicitTypeCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseImplicitTypeDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.use_var_instead_of_explicit_type, nameof(CSharpAnalyzersResources.use_var_instead_of_explicit_type));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var typeSyntax = (TypeSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            ReplaceTypeWithVar(editor, typeSyntax);
        }

        return Task.CompletedTask;
    }

    internal static void ReplaceTypeWithVar(SyntaxEditor editor, TypeSyntax type)
    {
        type = type.StripRefIfNeeded();
        var implicitType = SyntaxFactory.IdentifierName("var")
                                        .WithTriviaFrom(type);

        editor.ReplaceNode(type, implicitType);
    }
}
