// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeRefStruct;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeRefStruct), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class MakeRefStructCodeFixProvider() : CodeFixProvider
{
    // Error CS8345: Field or auto-implemented property cannot be of certain type unless it is an instance member of a ref struct.
    private const string CS8345 = nameof(CS8345);

    public override ImmutableArray<string> FixableDiagnosticIds
        => [CS8345];

    public override FixAllProvider? GetFixAllProvider()
    {
        // The chance of needing fix-all in these cases is super low
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;
        var span = context.Span;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Could be declared in a class or even in a nested class inside a struct,
        // so find only the first parent declaration
        if (root.FindNode(span).GetAncestor<TypeDeclarationSyntax>() is not StructDeclarationSyntax structDeclaration)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var structDeclarationSymbol = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(structDeclaration, cancellationToken);

        // CS8345 could be triggered when struct is already marked with `ref` but a property is static
        if (!structDeclarationSymbol.IsRefLikeType)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpCodeFixesResources.Make_ref_struct,
                    c => FixCodeAsync(document, structDeclaration, c),
                    nameof(CSharpCodeFixesResources.Make_ref_struct)),
                context.Diagnostics);
        }
    }

    private static async Task<Document> FixCodeAsync(
        Document document,
        StructDeclarationSyntax structDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var generator = SyntaxGenerator.GetGenerator(document);

        var newStruct = generator.WithModifiers(
            structDeclaration,
            generator.GetModifiers(structDeclaration).WithIsRef(true));
        var newRoot = root.ReplaceNode(structDeclaration, newStruct);

        return document.WithSyntaxRoot(newRoot);
    }
}
