// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: loc kvp

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor;

using static CSharpUsePrimaryConstructorDiagnosticAnalyzer;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePrimaryConstructor), Shared]
internal partial class CSharpUsePrimaryConstructorCodeFixProvider : CodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUsePrimaryConstructorCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId);

    public override FixAllProvider? GetFixAllProvider()
#if CODE_STYLE
        => WellKnownFixAllProviders.BatchFixer;
#else
        => new CSharpUsePrimaryConstructorFixAllProvider();
#endif

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Location.FindNode(cancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
                continue;

            var properties = diagnostic.Properties;
            var additionalNodes = diagnostic.AdditionalLocations;

            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpAnalyzersResources.Use_primary_constructor,
                    cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: false, cancellationToken),
                    nameof(CSharpAnalyzersResources.Use_primary_constructor)),
                diagnostic);

            if (diagnostic.Properties.Count > 0)
            {
                var resource =
                    diagnostic.Properties.ContainsKey(AllFieldsName) ? CSharpCodeFixesResources.Use_primary_constructor_and_remove_fields :
                    diagnostic.Properties.ContainsKey(AllPropertiesName) ? CSharpCodeFixesResources.Use_primary_constructor_and_remove_properties :
                    CSharpCodeFixesResources.Use_primary_constructor_and_remove_members;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        resource,
                        cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: true, cancellationToken),
                        nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_members)),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Solution> UsePrimaryConstructorAsync(
        Document document,
        ConstructorDeclarationSyntax constructorDeclaration,
        ImmutableDictionary<string, string?> properties,
        bool removeMembers,
        CancellationToken cancellationToken)
    {
        var solutionEditor = new SolutionEditor(document.Project.Solution);

        await UsePrimaryConstructorAsync(
            solutionEditor, document, constructorDeclaration, properties, removeMembers, cancellationToken).ConfigureAwait(false);

        return solutionEditor.GetChangedSolution();
    }

    private static async Task UsePrimaryConstructorAsync(
        SolutionEditor solutionEditor,
        Document document,
        ConstructorDeclarationSyntax constructorDeclaration,
        ImmutableDictionary<string, string?> properties,
        bool removeMembers,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var typeDeclaration = (TypeDeclarationSyntax)constructorDeclaration.GetRequiredParent();

        var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);
        var constructor = semanticModel.GetRequiredDeclaredSymbol(constructorDeclaration, cancellationToken);

        using var _1 = PooledDictionary<ISymbol, SyntaxNode>.GetInstance(out var removedMembers);

        // If we're removing members, first go through and update all references to that member to use the parameter name.
        var typeDeclarationNodes = namedType.DeclaringSyntaxReferences.Select(r => (TypeDeclarationSyntax)r.GetSyntax(cancellationToken));
        var namedTypeDocuments = typeDeclarationNodes.Select(r => solution.GetRequiredDocument(r.SyntaxTree)).ToImmutableHashSet();
        await RemoveMembersAsync().ConfigureAwait(false);

        // If the constructor has a base-initializer, then go find the base-type in the inheritance list for the
        // typedecl and move it there.
        await MoveBaseConstructorArgumentsAsync().ConfigureAwait(false);

        // Then take all the assignments in the constructor, and place them directly on the field/property initializers.
        if (constructorDeclaration.ExpressionBody is not null)
        {
            // Validated by analyzer.
            await ProcessAssignmentAsync((AssignmentExpressionSyntax)constructorDeclaration.ExpressionBody.Expression, expressionStatement: null).ConfigureAwait(false);
        }
        else
        {
            Contract.ThrowIfNull(constructorDeclaration.Body);
            foreach (var statement in constructorDeclaration.Body.Statements)
            {
                // Validated by analyzer.
                var expressionStatement = (ExpressionStatementSyntax)statement;
                await ProcessAssignmentAsync((AssignmentExpressionSyntax)expressionStatement.Expression, expressionStatement).ConfigureAwait(false);
            }
        }

        // Then remove the constructor itself.
        var constructorDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
        constructorDocumentEditor.RemoveNode(constructorDeclaration);

        var finalTrivia = CreateFinalTypeDeclarationLeadingTrivia(
            typeDeclaration, constructorDeclaration, constructor, properties, removedMembers);

        // Finally move the constructors parameter list to the type declaration.
        constructorDocumentEditor.ReplaceNode(
            typeDeclaration,
            (current, generator) =>
            {
                var currentTypeDeclaration = (TypeDeclarationSyntax)current;

                // Move the whitespace that is current after the name (or type args) to after the parameter list.

                var typeParameterList = currentTypeDeclaration.TypeParameterList;
                var triviaAfterName = typeParameterList != null
                    ? typeParameterList.GetTrailingTrivia()
                    : currentTypeDeclaration.Identifier.GetAllTrailingTrivia();

                return currentTypeDeclaration
                    .WithLeadingTrivia(finalTrivia)
                    .WithIdentifier(typeParameterList != null ? currentTypeDeclaration.Identifier : currentTypeDeclaration.Identifier.WithoutTrailingTrivia())
                    .WithTypeParameterList(typeParameterList?.WithoutTrailingTrivia())
                    .WithParameterList(constructorDeclaration.ParameterList
                        .WithoutLeadingTrivia()
                        .WithTrailingTrivia(triviaAfterName)
                        .WithAdditionalAnnotations(Formatter.Annotation));
            });

        return;

        async ValueTask MoveBaseConstructorArgumentsAsync()
        {
            if (constructorDeclaration.Initializer is null)
                return;

            foreach (var current in typeDeclarationNodes)
            {
                // only need to check the first type in the list, the rest must be interfaces.
                if (current.BaseList is not { Types: [SimpleBaseTypeSyntax baseType, ..] })
                    continue;

                if (semanticModel.GetSymbolInfo(baseType.Type, cancellationToken).GetAnySymbol() is not INamedTypeSymbol { TypeKind: TypeKind.Class })
                    continue;

                var document = solution.GetRequiredDocument(baseType.SyntaxTree);
                var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

                documentEditor.ReplaceNode(
                    baseType,
                    PrimaryConstructorBaseType(baseType.Type.WithoutTrailingTrivia(), constructorDeclaration.Initializer.ArgumentList.WithoutLeadingTrivia())
                        .WithTrailingTrivia(baseType.GetTrailingTrivia()));
                return;
            }
        }

        async ValueTask ProcessAssignmentAsync(AssignmentExpressionSyntax assignmentExpression, ExpressionStatementSyntax? expressionStatement)
        {
            var member = semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).GetAnySymbol()?.OriginalDefinition;

            // Validated by analyzer.
            Contract.ThrowIfFalse(member is IFieldSymbol or IPropertySymbol);

            // no point updating the member if it's going to be removed.
            if (removedMembers.ContainsKey(member))
                return;

            var declaration = member.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var declarationDocument = solution.GetRequiredDocument(declaration.SyntaxTree);
            var declarationDocumentEditor = await solutionEditor.GetDocumentEditorAsync(declarationDocument.Id, cancellationToken).ConfigureAwait(false);

            declarationDocumentEditor.ReplaceNode(
                declaration,
                UpdateDeclaration(declaration, assignmentExpression, expressionStatement).WithAdditionalAnnotations(Formatter.Annotation));
        }

        SyntaxNode UpdateDeclaration(SyntaxNode declaration, AssignmentExpressionSyntax assignmentExpression, ExpressionStatementSyntax? expressionStatement)
        {
            var newLeadingTrivia = assignmentExpression.Left.GetTrailingTrivia();
            var initializer = EqualsValueClause(assignmentExpression.OperatorToken, assignmentExpression.Right);
            if (declaration is VariableDeclaratorSyntax declarator)
            {
                return declarator
                    .WithIdentifier(declarator.Identifier.WithTrailingTrivia(newLeadingTrivia))
                    .WithInitializer(initializer);
            }
            else if (declaration is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration
                    .WithoutTrailingTrivia()
                    .WithInitializer(initializer.WithLeadingTrivia(newLeadingTrivia))
                    .WithSemicolonToken(
                        // Use existing semicolon if we have it.  Otherwise create a fresh one and place existing
                        // trailing trivia after it.
                        expressionStatement?.SemicolonToken
                        ?? Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia()));
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        async ValueTask RemoveMembersAsync()
        {
            if (!removeMembers)
                return;

            // Go through each pair of member/parameterName.  Update all references to member to now refer to
            // parameterName. This is safe as the analyzer ensured that all existing locations would safely be able
            // to do this.  Then once those are all done, actually remove the members.
            foreach (var (memberName, parameterName) in properties)
            {
                Contract.ThrowIfNull(parameterName);

                var (member, nodeToRemove) = GetMemberToRemove(memberName);
                if (member is null)
                    continue;

                removedMembers[member] = nodeToRemove;
                await ReplaceReferencesToMemberWithParameterAsync(member, parameterName).ConfigureAwait(false);
            }

            foreach (var group in removedMembers.Values.GroupBy(n => n.SyntaxTree))
            {
                var syntaxTree = group.Key;
                var memberDocument = solution.GetRequiredDocument(syntaxTree);
                var documentEditor = await solutionEditor.GetDocumentEditorAsync(memberDocument.Id, cancellationToken).ConfigureAwait(false);

                foreach (var memberToRemove in group)
                    documentEditor.RemoveNode(memberToRemove);
            }
        }

        (ISymbol? member, SyntaxNode nodeToRemove) GetMemberToRemove(string memberName)
        {
            foreach (var member in namedType.GetMembers(memberName))
            {
                if (IsViableMemberToAssignTo(namedType, member, out var nodeToRemove, cancellationToken))
                    return (member, nodeToRemove);
            }

            return default;
        }

        async ValueTask ReplaceReferencesToMemberWithParameterAsync(ISymbol member, string parameterName)
        {
            var parameterNameNode = IdentifierName(parameterName);

            // find all the references to member within this project.  We can immediately filter down just to the
            // documents containing our named type.
            var references = await SymbolFinder.FindReferencesAsync(
                member, solution, namedTypeDocuments, cancellationToken).ConfigureAwait(false);
            foreach (var reference in references)
            {
                foreach (var group in reference.Locations.GroupBy(loc => loc.Document))
                {
                    var document = group.Key;
                    var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

                    foreach (var location in group)
                    {
                        if (location.IsImplicit)
                            continue;

                        var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken) as IdentifierNameSyntax;
                        if (node is null)
                            continue;

                        var nodeToReplace = node.IsRightSideOfDot() ? node.GetRequiredParent() : node;
                        documentEditor.ReplaceNode(
                            nodeToReplace,
                            parameterNameNode.WithTriviaFrom(nodeToReplace));
                    }
                }
            }
        }
    }
}
