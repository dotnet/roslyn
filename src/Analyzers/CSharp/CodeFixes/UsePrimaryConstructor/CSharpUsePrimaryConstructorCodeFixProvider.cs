// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: loc kvp

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor
{
    using static SyntaxFactory;
    using static CSharpUsePrimaryConstructorDiagnosticAnalyzer;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePrimaryConstructor), Shared]
    internal class CSharpUsePrimaryConstructorCodeFixProvider : CodeFixProvider
    {
        private const string s_summaryTagName = "summary";
        private const string s_remarksTagName = "remarks";
        private const string s_paramTagName = "param";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUsePrimaryConstructorCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId);

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
                    var (resource, equivalenceKey) =
                        diagnostic.Properties.ContainsKey(AllFieldsName) ? (CSharpCodeFixesResources.Use_primary_constructor_and_remove_fields, nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_fields)) :
                        diagnostic.Properties.ContainsKey(AllPropertiesName) ? (CSharpCodeFixesResources.Use_primary_constructor_and_remove_properties, nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_properties)) :
                        (CSharpCodeFixesResources.Use_primary_constructor_and_remove_members, nameof(CSharpCodeFixesResources.Use_primary_constructor_and_remove_members));

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            resource,
                            cancellationToken => UsePrimaryConstructorAsync(document, constructorDeclaration, properties, removeMembers: true, cancellationToken),
                            equivalenceKey),
                        diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static DocumentationCommentTriviaSyntax? GetDocCommentStructure(SyntaxNode node)
            => (DocumentationCommentTriviaSyntax?)GetDocComment(node).GetStructure();

        private static SyntaxTrivia GetDocComment(SyntaxNode node)
            => GetDocComment(node.GetLeadingTrivia());

        private static SyntaxTrivia GetDocComment(SyntaxTriviaList trivia)
            => trivia.LastOrDefault(t => t.IsSingleLineDocComment());

        private static async Task<Solution> UsePrimaryConstructorAsync(
            Document document,
            ConstructorDeclarationSyntax constructorDeclaration,
            ImmutableDictionary<string, string?> properties,
            bool removeMembers,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclaration = (TypeDeclarationSyntax)constructorDeclaration.GetRequiredParent();

            var namedType = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);
            var constructor = semanticModel.GetRequiredDeclaredSymbol(constructorDeclaration, cancellationToken);

            var solution = document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);

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

            var finalTrivia = CreateFinalTypeDeclarationLeadingTrivia();

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

            // TODO: reconcile doc comments.
            // 1. If we are not removing members and the constructor had parameter doc comments, we likely want to move
            //    those to the type declaration.
            // 2. if we are removing members and the members had doc comments:
            //      2a. if the constructor had parameter doc comments, choose which to win (probably parameter)
            //      2b. if the constructor did not have parameter doc comments, take the member doc comments and convert
            //          to parameter comments.

            return solutionEditor.GetChangedSolution();

            SyntaxTriviaList CreateFinalTypeDeclarationLeadingTrivia()
            {
                var typeDeclarationLeadingTrivia = MergeTypeDeclarationAndConstructorDocComments();

                // now, if we're removing any members, and they had doc comments, and we don't already have doc comments
                // for that parameter in our final doc comment, then move them to there, converting from `<summary>` doc comments to
                // `<param name="x">` doc comments.

                // Keep the <param> tags ordered by the order they are in the constructor parameters.
                var orderedKVPs = properties.OrderBy(kvp => constructor.Parameters.FirstOrDefault(p => p.Name == kvp.Value)?.Ordinal);

                using var _1 = ArrayBuilder<(string parameterName, DocumentationCommentTriviaSyntax docComment)>.GetInstance(out var docCommentsToMove);

                foreach (var (memberName, parameterName) in orderedKVPs)
                {
                    var (removedMember, memberDeclaration) = removedMembers.FirstOrDefault(kvp => kvp.Key.Name == memberName);
                    if (removedMember is null)
                        continue;

                    var removedMemberDocComment = GetDocCommentStructure(
                        memberDeclaration is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } ? field : memberDeclaration);
                    if (removedMemberDocComment != null)
                        docCommentsToMove.Add((parameterName, removedMemberDocComment)!);
                }

                var existingTypeDeclarationDocComment = GetDocComment(typeDeclarationLeadingTrivia);

                // Simple case, no doc comments on either
                if (typeDeclarationLeadingTrivia == default && docCommentsToMove.Count == 0)
                    return typeDeclarationLeadingTrivia;

                if (existingTypeDeclarationDocComment == default)
                {
                    // type doesn't have doc comment, create a fresh one from all the doc comments removed.
                    using var _2 = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var allContent);
                    foreach (var (parameterName, commentToMove) in docCommentsToMove)
                        allContent.AddRange(ConvertSummaryToParam(commentToMove.Content, parameterName));

                    var insertionIndex = typeDeclarationLeadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)]
                        ? typeDeclarationLeadingTrivia.Count - 1
                        : typeDeclarationLeadingTrivia.Count;

                    var newDocComment = Trivia(DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, List(allContent)));
                    return typeDeclarationLeadingTrivia.Insert(
                        insertionIndex,
                        newDocComment.WithAdditionalAnnotations(Formatter.Annotation));
                }

                return typeDeclarationLeadingTrivia;
            }

            static IEnumerable<XmlNodeSyntax> ConvertSummaryToParam(IEnumerable<XmlNodeSyntax> content, string parameterName)
            {
                foreach (var node in content)
                {
                    yield return IsXmlElement(node, s_summaryTagName, out var xmlElement)
                        ? WithNameAttribute(ConvertXmlElementName(xmlElement, s_paramTagName), parameterName)
                        : node;
                }
            }

            static XmlElementSyntax WithNameAttribute(XmlElementSyntax element, string parameterName)
                => element.ReplaceNode(element.StartTag, element.StartTag.AddAttributes(XmlNameAttribute(parameterName)));

            SyntaxTriviaList MergeTypeDeclarationAndConstructorDocComments()
            {
                var typeDeclarationLeadingTrivia = typeDeclaration.GetLeadingTrivia();

                // TODO: add support for `/** */` style doc comments if customer demand is there.
                var existingTypeDeclarationDocComment = GetDocComment(typeDeclarationLeadingTrivia);
                var existingConstructorDocComment = GetDocComment(constructorDeclaration);

                // Simple case, no doc comments on either
                if (existingTypeDeclarationDocComment == default && existingConstructorDocComment == default)
                    return typeDeclarationLeadingTrivia;

                if (existingTypeDeclarationDocComment == default)
                {
                    // type doesn't have doc comment, but constructor does.  Move constructor doc comment to type decl.
                    // note: the doc comment always ends with a newline.  so we want to place the new one before the
                    // final leading spaces of the type decl trivia.
                    var insertionIndex = typeDeclarationLeadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)]
                        ? typeDeclarationLeadingTrivia.Count - 1
                        : typeDeclarationLeadingTrivia.Count;

                    return typeDeclarationLeadingTrivia.Insert(
                        insertionIndex,
                        existingConstructorDocComment.WithAdditionalAnnotations(Formatter.Annotation));
                }

                if (existingConstructorDocComment != default)
                {
                    // Both the type and the constructor have doc comments.  Want to move the constructor parameter
                    // pieces into the type decl doc comment.
                    return typeDeclarationLeadingTrivia.Replace(
                        existingTypeDeclarationDocComment,
                        MergeDocComments(existingTypeDeclarationDocComment, existingConstructorDocComment).WithAdditionalAnnotations(Formatter.Annotation));
                }

                return typeDeclarationLeadingTrivia;
            }

            static SyntaxTrivia MergeDocComments(SyntaxTrivia typeDeclarationDocComment, SyntaxTrivia constructorDocComment)
            {
                var typeStructure = (DocumentationCommentTriviaSyntax)typeDeclarationDocComment.GetStructure()!;
                var constructorStructure = (DocumentationCommentTriviaSyntax)constructorDocComment.GetStructure()!;

                using var _ = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var content);

                // Add all the type decl comments first.
                content.AddRange(typeStructure.Content);

                // then add the constructor comments.  If the type decl already had a summary tag then convert the
                // constructor's summary tag to a 'remarks' tag to keep around the info while not stomping on the
                // existing summary.
                var constructorContents = typeStructure.Content.Any(n => n is XmlElementSyntax { StartTag.Name.LocalName.ValueText: s_summaryTagName })
                    ? ConvertSummaryToRemarks(constructorStructure.Content)
                    : constructorStructure.Content;

                content.AddRange(constructorContents);

                return Trivia(DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    List(content),
                    typeStructure.EndOfComment));
            }

            static IEnumerable<XmlNodeSyntax> ConvertSummaryToRemarks(IEnumerable<XmlNodeSyntax> nodes)
            {
                foreach (var node in nodes)
                {
                    yield return IsXmlElement(node, s_summaryTagName, out var xmlElement)
                        ? ConvertXmlElementName(xmlElement, s_remarksTagName)
                        : node;
                }
            }

            static bool IsXmlElement(XmlNodeSyntax node, string name, [NotNullWhen(true)] out XmlElementSyntax? element)
            {
                element = node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: var elementName } xmlElement && elementName == name
                    ? xmlElement
                    : null;
                return element != null;
            }

            static XmlElementSyntax ConvertXmlElementName(XmlElementSyntax xmlElement, string name)
            {
                return xmlElement.ReplaceTokens(
                    new[] { xmlElement.StartTag.Name.LocalName, xmlElement.EndTag.Name.LocalName },
                    (token, _) => Identifier(name).WithTriviaFrom(token));
            }

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

                Contract.ThrowIfTrue(properties.IsEmpty);

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
}
