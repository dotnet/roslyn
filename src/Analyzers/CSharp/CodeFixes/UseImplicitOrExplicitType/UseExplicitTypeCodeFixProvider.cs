// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
internal class UseExplicitTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public UseExplicitTypeCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseExplicitTypeDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_explicit_type_instead_of_var, nameof(CSharpAnalyzersResources.Use_explicit_type_instead_of_var));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            await HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static async Task HandleDeclarationAsync(
        Document document, SyntaxEditor editor,
        SyntaxNode node, CancellationToken cancellationToken)
    {
        var declarationContext = node.Parent;

        if (declarationContext is RefTypeSyntax)
        {
            declarationContext = declarationContext.Parent;
        }

        if (declarationContext is VariableDeclarationSyntax varDecl)
        {
            await HandleVariableDeclarationAsync(document, editor, varDecl, cancellationToken).ConfigureAwait(false);
        }
        else if (declarationContext is ForEachStatementSyntax forEach)
        {
            await HandleForEachStatementAsync(document, editor, forEach, cancellationToken).ConfigureAwait(false);
        }
        else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
        {
            await HandleDeclarationExpressionAsync(document, editor, declarationExpression, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(declarationContext?.Kind());
        }
    }

    private static async Task HandleDeclarationExpressionAsync(Document document, SyntaxEditor editor, DeclarationExpressionSyntax declarationExpression, CancellationToken cancellationToken)
    {
        var typeSyntax = declarationExpression.Type;
        typeSyntax = typeSyntax.StripRefIfNeeded();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (declarationExpression.Designation is ParenthesizedVariableDesignationSyntax variableDesignation)
        {
            RoslynDebug.AssertNotNull(typeSyntax.Parent);

            var tupleTypeSymbol = GetConvertedType(semanticModel, typeSyntax.Parent, cancellationToken);

            var leadingTrivia = declarationExpression.GetLeadingTrivia()
                .Concat(variableDesignation.GetAllPrecedingTriviaToPreviousToken().Where(t => !t.IsWhitespace()).Select(t => t.WithoutAnnotations(SyntaxAnnotation.ElasticAnnotation)));

            var tupleDeclaration = GenerateTupleDeclaration(tupleTypeSymbol, variableDesignation).WithLeadingTrivia(leadingTrivia);

            editor.ReplaceNode(declarationExpression, tupleDeclaration);
        }
        else
        {
            var typeSymbol = GetConvertedType(semanticModel, typeSyntax, cancellationToken);
            editor.ReplaceNode(typeSyntax, GenerateTypeDeclaration(typeSyntax, typeSymbol));
        }
    }

    private static Task HandleForEachStatementAsync(Document document, SyntaxEditor editor, ForEachStatementSyntax forEach, CancellationToken cancellationToken)
        => UpdateTypeSyntaxAsync(
            document,
            editor,
            forEach.Type,
            forEach.Identifier.GetRequiredParent(),
            cancellationToken);

    private static Task HandleVariableDeclarationAsync(Document document, SyntaxEditor editor, VariableDeclarationSyntax varDecl, CancellationToken cancellationToken)
        => UpdateTypeSyntaxAsync(
            document,
            editor,
            varDecl.Type,
            // Since we're only dealing with variable declaration using var, we know
            // that implicitly typed variables cannot have multiple declarators in
            // a single declaration (CS0819). Only one variable should be present
            varDecl.Variables.Single().Identifier.Parent!,
            cancellationToken);

    private static async Task UpdateTypeSyntaxAsync(Document document, SyntaxEditor editor, TypeSyntax typeSyntax, SyntaxNode declarationSyntax, CancellationToken cancellationToken)
    {
        typeSyntax = typeSyntax.StripRefIfNeeded();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var typeSymbol = GetConvertedType(semanticModel, typeSyntax, cancellationToken);

        typeSymbol = AdjustNullabilityOfTypeSymbol(
            typeSymbol,
            semanticModel,
            declarationSyntax,
            cancellationToken);

        editor.ReplaceNode(typeSyntax, GenerateTypeDeclaration(typeSyntax, typeSymbol));
    }

    private static ITypeSymbol AdjustNullabilityOfTypeSymbol(
        ITypeSymbol typeSymbol,
        SemanticModel semanticModel,
        SyntaxNode declarationSyntax,
        CancellationToken cancellationToken)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // It's possible that the var shouldn't be annotated nullable, check assignments to the variable and 
            // determine if it needs to be null
            var isPossiblyAssignedNull = NullableHelpers.IsDeclaredSymbolAssignedPossiblyNullValue(semanticModel, declarationSyntax, cancellationToken);
            if (!isPossiblyAssignedNull)
            {
                // If the symbol is never assigned null we can update the type symbol to also be non-null
                return typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }
        }

        return typeSymbol;
    }

    private static ExpressionSyntax GenerateTupleDeclaration(ITypeSymbol typeSymbol, ParenthesizedVariableDesignationSyntax parensDesignation)
    {
        Debug.Assert(typeSymbol.IsTupleType);
        var elements = ((INamedTypeSymbol)typeSymbol).TupleElements;
        Debug.Assert(elements.Length == parensDesignation.Variables.Count);

        using var _ = ArrayBuilder<ArgumentSyntax>.GetInstance(elements.Length, out var builder);
        for (var i = 0; i < elements.Length; i++)
        {
            var designation = parensDesignation.Variables[i];
            var type = elements[i].Type;
            ExpressionSyntax newDeclaration;
            switch (designation.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                case SyntaxKind.DiscardDesignation:
                    var typeName = type.GenerateTypeSyntax(allowVar: false);
                    newDeclaration = DeclarationExpression(typeName, designation);
                    break;
                case SyntaxKind.ParenthesizedVariableDesignation:
                    newDeclaration = GenerateTupleDeclaration(type, (ParenthesizedVariableDesignationSyntax)designation);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(designation.Kind());
            }

            newDeclaration = newDeclaration
                .WithLeadingTrivia(designation.GetAllPrecedingTriviaToPreviousToken())
                .WithTrailingTrivia(designation.GetTrailingTrivia());

            builder.Add(Argument(newDeclaration));
        }

        var separatorBuilder = ArrayBuilder<SyntaxToken>.GetInstance(builder.Count - 1, Token(leading: default, SyntaxKind.CommaToken, trailing: default));

        return TupleExpression(
            OpenParenToken.WithTrailingTrivia(),
            SeparatedList(builder, separatorBuilder),
            CloseParenToken)
            .WithTrailingTrivia(parensDesignation.GetTrailingTrivia());
    }

    private static SyntaxNode GenerateTypeDeclaration(TypeSyntax typeSyntax, ITypeSymbol newTypeSymbol)
    {
        // We're going to be passed through the simplifier.  Tell it to not just convert this back to var (as
        // that would defeat the purpose of this refactoring entirely).
        var newTypeSyntax = newTypeSymbol
                     .GenerateTypeSyntax(allowVar: false)
                     .WithTriviaFrom(typeSyntax);

        return newTypeSyntax;
    }

    private static ITypeSymbol GetConvertedType(SemanticModel semanticModel, SyntaxNode typeSyntax, CancellationToken cancellationToken)
    {
        var typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType;
        if (typeSymbol is null)
        {
            throw ExceptionUtilities.UnexpectedValue(typeSymbol);
        }

        return typeSymbol;
    }
}
