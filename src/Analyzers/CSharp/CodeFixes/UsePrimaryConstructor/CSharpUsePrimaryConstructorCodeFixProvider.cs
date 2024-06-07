// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: loc kvp

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
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
using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePrimaryConstructor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class CSharpUsePrimaryConstructorCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UsePrimaryConstructorDiagnosticId];

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

        // If we're removing members, first go through and update all references to that member to use the parameter name.
        var typeDeclarationNodes = namedType.DeclaringSyntaxReferences.Select(r => (TypeDeclarationSyntax)r.GetSyntax(cancellationToken));
        var namedTypeDocuments = typeDeclarationNodes.Select(r => solution.GetRequiredDocument(r.SyntaxTree)).ToImmutableHashSet();
        var removedMembers = await RemoveMembersAsync().ConfigureAwait(false);

        // If the constructor has a base-initializer, then go find the base-type in the inheritance list for the
        // typedecl and move it there.
        await MoveBaseConstructorArgumentsAsync().ConfigureAwait(false);

        // Then take all the assignments in the constructor, and place them directly on the field/property initializers.
        await ProcessConstructorAssignmentsAsync().ConfigureAwait(false);

        // Then remove the constructor itself.
        var constructorDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
        constructorDocumentEditor.RemoveNode(constructorDeclaration, GetConstructorRemovalOptions());

        // When moving the parameter list from the constructor to the type, we will no longer have nested types or
        // member constants in scope.  So rewrite references to them if that's the case.
        var updatedParameterList = GenerateFinalParameterList();

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

                var finalAttributeLists = currentTypeDeclaration.AttributeLists.AddRange(
                    constructorDeclaration.AttributeLists.Select(
                        a => a.WithTarget(AttributeTargetSpecifier(MethodKeyword)).WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation)));

                var finalTrivia = CreateFinalTypeDeclarationLeadingTrivia(
                    currentTypeDeclaration, constructorDeclaration, constructor, properties, removedMembers);

                return currentTypeDeclaration
                    .WithAttributeLists(finalAttributeLists)
                    .WithLeadingTrivia(finalTrivia)
                    .WithIdentifier(typeParameterList != null ? currentTypeDeclaration.Identifier : currentTypeDeclaration.Identifier.WithoutTrailingTrivia())
                    .WithTypeParameterList(typeParameterList?.WithoutTrailingTrivia())
                    .WithParameterList(updatedParameterList
                        .WithoutLeadingTrivia()
                        .WithTrailingTrivia(triviaAfterName)
                        .WithAdditionalAnnotations(Formatter.Annotation));
            });

        return;

        SyntaxRemoveOptions GetConstructorRemovalOptions()
        {
            // if we're removing all the members prior to the constructor, and any of those member had pragmas we are
            // keeping, then we need to keep it on the constructor as well.

            var constructorRemoveOptions = GetRemoveOptions(constructorDeclaration);
            if (constructorRemoveOptions == SyntaxGenerator.DefaultRemoveOptions)
            {
                for (var currentIndex = typeDeclaration.Members.IndexOf(constructorDeclaration) - 1; currentIndex >= 0; currentIndex--)
                {
                    var priorMember = typeDeclaration.Members[currentIndex];

                    // Hit a member we're not removing.  Just use the default options for the constructor.
                    if (!removedMembers.Any(kvp => kvp.Value.memberNode == priorMember))
                        break;

                    // Check if we had special options when removing the field/prop.  We want to apply that to the
                    // constructor as well.
                    var memberRemoveOptions = GetRemoveOptions(priorMember);
                    if (memberRemoveOptions != SyntaxGenerator.DefaultRemoveOptions)
                        return memberRemoveOptions;
                }
            }

            return constructorRemoveOptions;
        }

        ParameterListSyntax GenerateFinalParameterList()
        {
            // Note: we can use constructorDeclarationSemanticModel as we're only touching nodes within the constructor
            // declaration itself.
            var updatedParameterList = UpdateReferencesToNestedMembers(constructorDeclaration.ParameterList);

            updatedParameterList = RemoveElementIndentation(
                typeDeclaration, constructorDeclaration, updatedParameterList,
                static list => list.Parameters);

            updatedParameterList = RemoveInModifierIfMemberIsRemoved(updatedParameterList);

            return updatedParameterList;
        }

        ParameterListSyntax RemoveInModifierIfMemberIsRemoved(ParameterListSyntax parameterList)
        {
            if (!removeMembers)
                return parameterList;

            return parameterList.ReplaceNodes(
                parameterList.Parameters,
                (_, current) =>
                {
                    var inKeyword = current.Modifiers.FirstOrDefault(t => t.Kind() == SyntaxKind.InKeyword);
                    if (inKeyword == default)
                        return current;

                    // remove the 'in' modifier if we're removing the field.  Captures can't refer to an in-parameter.
                    if (!properties.Values.Any(v => v == current.Identifier.ValueText))
                        return current;

                    return current.WithModifiers(current.Modifiers.Remove(inKeyword)).WithTriviaFrom(current);
                });
        }

        ParameterListSyntax UpdateReferencesToNestedMembers(ParameterListSyntax parameterList)
        {
            return parameterList.ReplaceNodes(
                parameterList.DescendantNodes().OfType<SimpleNameSyntax>(),
                (nameSyntax, currentNameSyntax) =>
                {
                    if (nameSyntax.Parent is QualifiedNameSyntax qualifiedNameSyntax)
                    {
                        // Don't have to update if the name is already the RHS of some qualified name.
                        if (qualifiedNameSyntax.Left == nameSyntax)
                        {
                            // Qualified names occur in things like the `type` portion of the parameter
                            return TryQualify(nameSyntax, currentNameSyntax);
                        }
                    }
                    else if (nameSyntax.Parent is MemberAccessExpressionSyntax memberAccessExpression)
                    {
                        // Don't have to update if the name is already the RHS of some member access expr.
                        if (memberAccessExpression.Expression == nameSyntax)
                        {
                            // Member access expressions occur in things like the default initializer, or attribute
                            // arguments of the parameter.
                            return TryQualify(nameSyntax, currentNameSyntax);
                        }
                    }
                    else
                    {
                        // Standalone name.  Try to qualify depending on if this is a type or member context.
                        return TryQualify(nameSyntax, currentNameSyntax);
                    }

                    return currentNameSyntax;
                });

            SyntaxNode TryQualify(
                SimpleNameSyntax originalName,
                SimpleNameSyntax currentName)
            {
                var symbol = semanticModel.GetSymbolInfo(originalName, cancellationToken).GetAnySymbol();
                return symbol switch
                {
                    INamedTypeSymbol { ContainingType: { } containingType } => CreateDottedName(originalName, currentName, containingType),
                    IMethodSymbol or IPropertySymbol or IEventSymbol or IFieldSymbol =>
                        symbol is { ContainingType.OriginalDefinition: { } containingType } &&
                        namedType.Equals(containingType) ? CreateDottedName(originalName, currentName, containingType) : currentName,
                    _ => currentName,
                };
            }

            SyntaxNode CreateDottedName(
                SimpleNameSyntax originalName,
                SimpleNameSyntax currentName,
                INamedTypeSymbol containingType)
            {
                var containingTypeSyntax = containingType.GenerateNameSyntax();
                return SyntaxFacts.IsInTypeOnlyContext(originalName)
                    ? QualifiedName(containingTypeSyntax, currentName)
                    : MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, containingTypeSyntax, currentName);
            }
        }

        static TListSyntax RemoveElementIndentation<TListSyntax>(
            TypeDeclarationSyntax typeDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration,
            TListSyntax list,
            Func<TListSyntax, IEnumerable<SyntaxNode>> getElements)
            where TListSyntax : SyntaxNode
        {
            // Since we're moving parameters from the constructor to the type, attempt to dedent them if appropriate.

            var typeLeadingWhitespace = GetLeadingWhitespace(typeDeclaration);
            var constructorLeadingWhitespace = GetLeadingWhitespace(constructorDeclaration);

            if (constructorLeadingWhitespace.Length > typeLeadingWhitespace.Length &&
                constructorLeadingWhitespace.StartsWith(typeLeadingWhitespace))
            {
                var indentation = constructorLeadingWhitespace[typeLeadingWhitespace.Length..];
                return list.ReplaceNodes(
                    getElements(list),
                    (p, _) =>
                    {
                        var elementLeadingWhitespace = GetLeadingWhitespace(p);
                        if (elementLeadingWhitespace.EndsWith(indentation))
                        {
                            var leadingTrivia = p.GetLeadingTrivia();
                            return p.WithLeadingTrivia(
                                leadingTrivia.Take(leadingTrivia.Count - 1).Concat(Whitespace(elementLeadingWhitespace[..^indentation.Length])));
                        }

                        return p;
                    });
            }

            return list;
        }

        static string GetLeadingWhitespace(SyntaxNode node)
            => node.GetLeadingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia) whitespace] ? whitespace.ToString() : "";

        async ValueTask MoveBaseConstructorArgumentsAsync()
        {
            if (constructorDeclaration.Initializer is null)
                return;

            // Note: the primary constructor parameters can only be passed to the base class on the same type
            // declaration that the primary constructor is on.
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

            var argumentList = RemoveElementIndentation(
                typeDeclaration, constructorDeclaration, constructorDeclaration.Initializer.ArgumentList,
                static list => list.Arguments);

            if (typeDeclaration.BaseList is { Types: [SimpleBaseTypeSyntax baseType, ..] } &&
                semanticModel.GetSymbolInfo(baseType.Type, cancellationToken).GetAnySymbol() is INamedTypeSymbol { TypeKind: TypeKind.Class })
            {
                // Case 1: The type already explicitly lists the base type on the current type decl.  If so, move the arguments to it.
                // For example:
                //
                //      `class C : B, I` becomes `class C(int i) : B(i), I`

                documentEditor.ReplaceNode(
                    baseType,
                    PrimaryConstructorBaseType(baseType.Type.WithoutTrailingTrivia(), argumentList.WithoutLeadingTrivia())
                        .WithTrailingTrivia(baseType.GetTrailingTrivia()));
            }
            else
            {
                // Case 2: The type doesn't have the base type on this declaration.  We'll have to synthesize it and add it to the base list.
                // For example:
                //
                //      `class C : I` becomes `class C(int i) : B(i), I`
                var baseTypeSymbol = namedType.BaseType;
                if (baseTypeSymbol is null)
                    return;

                var synthesizedTypeNode = baseTypeSymbol.GenerateNameSyntax(allowVar: false);
                var baseTypeSyntax = PrimaryConstructorBaseType(synthesizedTypeNode, argumentList);

                documentEditor.ReplaceNode(
                    typeDeclaration,
                    (current, _) =>
                    {
                        var currentTypeDeclaration = (TypeDeclarationSyntax)current;
                        if (currentTypeDeclaration.BaseList is null)
                        {
                            var typeParameterList = currentTypeDeclaration.TypeParameterList;
                            var triviaAfterName = typeParameterList != null
                                ? typeParameterList.GetTrailingTrivia()
                                : currentTypeDeclaration.Identifier.GetAllTrailingTrivia();

                            return currentTypeDeclaration
                                .WithIdentifier(currentTypeDeclaration.Identifier.WithoutTrailingTrivia())
                                .WithTypeParameterList(typeParameterList?.WithoutTrailingTrivia())
                                .WithBaseList(BaseList(SeparatedList<BaseTypeSyntax>().Add(baseTypeSyntax)).WithLeadingTrivia(Space).WithTrailingTrivia(triviaAfterName));
                        }
                        else
                        {
                            return currentTypeDeclaration.WithBaseList(
                                currentTypeDeclaration.BaseList.WithTypes(currentTypeDeclaration.BaseList.Types.Insert(0, baseTypeSyntax)));
                        }
                    });
            }
        }

        async ValueTask ProcessConstructorAssignmentsAsync()
        {
            if (constructorDeclaration.ExpressionBody is not null)
            {
                // Validated by analyzer.
                await ProcessConstructorAssignmentAsync(
                    (AssignmentExpressionSyntax)constructorDeclaration.ExpressionBody.Expression, expressionStatement: null).ConfigureAwait(false);
            }
            else
            {
                Contract.ThrowIfNull(constructorDeclaration.Body);
                foreach (var statement in constructorDeclaration.Body.Statements)
                {
                    // Validated by analyzer.
                    var expressionStatement = (ExpressionStatementSyntax)statement;
                    await ProcessConstructorAssignmentAsync(
                        (AssignmentExpressionSyntax)expressionStatement.Expression, expressionStatement).ConfigureAwait(false);
                }
            }
        }

        async ValueTask ProcessConstructorAssignmentAsync(
            AssignmentExpressionSyntax assignmentExpression, ExpressionStatementSyntax? expressionStatement)
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
                        ?? SemicolonToken.WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia()));
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        async ValueTask<ImmutableDictionary<ISymbol, (MemberDeclarationSyntax memberNode, SyntaxNode nodeToRemove)>> RemoveMembersAsync()
        {
            var removedMembers = ImmutableDictionary<ISymbol, (MemberDeclarationSyntax memberNode, SyntaxNode nodeToRemove)>.Empty;
            if (removeMembers)
            {
                // Go through each pair of member/parameterName.  Update all references to member to now refer to
                // parameterName. This is safe as the analyzer ensured that all existing locations would safely be able
                // to do this.  Then once those are all done, actually remove the members.
                foreach (var (memberName, parameterName) in properties)
                {
                    Contract.ThrowIfNull(parameterName);

                    var (member, memberNode, nodeToRemove) = GetMemberToRemove(memberName);
                    if (member is null)
                        continue;

                    removedMembers = removedMembers.Add(member, (memberNode, nodeToRemove));
                    await ReplaceReferencesToMemberWithParameterAsync(
                        member, CSharpSyntaxFacts.Instance.EscapeIdentifier(parameterName)).ConfigureAwait(false);
                }

                foreach (var group in removedMembers.Values.GroupBy(n => n.memberNode.SyntaxTree))
                {
                    var syntaxTree = group.Key;
                    var memberDocument = solution.GetRequiredDocument(syntaxTree);
                    var documentEditor = await solutionEditor.GetDocumentEditorAsync(memberDocument.Id, cancellationToken).ConfigureAwait(false);

                    foreach (var (memberNode, nodeToRemove) in group)
                    {
                        // Preserve pragmas around fields as they can affect more than just the field itself (they
                        // extend to the rest of the file).
                        documentEditor.RemoveNode(nodeToRemove, GetRemoveOptions(memberNode));
                    }
                }
            }

            return removedMembers;
        }

        static SyntaxRemoveOptions GetRemoveOptions(MemberDeclarationSyntax memberDeclaration)
            => memberDeclaration.GetLeadingTrivia().Any(t => t.GetStructure()?.Kind() == SyntaxKind.PragmaWarningDirectiveTrivia)
                ? SyntaxRemoveOptions.KeepDirectives
                : SyntaxGenerator.DefaultRemoveOptions;

        (ISymbol? member, MemberDeclarationSyntax memberNode, SyntaxNode nodeToRemove) GetMemberToRemove(string memberName)
        {
            foreach (var member in namedType.GetMembers(memberName))
            {
                if (IsViableMemberToAssignTo(namedType, member, out var memberNode, out var nodeToRemove, cancellationToken))
                    return (member, memberNode, nodeToRemove);
            }

            return default;
        }

        async ValueTask ReplaceReferencesToMemberWithParameterAsync(ISymbol member, string parameterName)
        {
            var parameterNameNode = IdentifierName(ParseToken(parameterName));

            // find all the references to member within this project.  We can immediately filter down just to the
            // documents containing our named type.
            var references = await SymbolFinder.FindReferencesAsync(
                member, solution, namedTypeDocuments, cancellationToken).ConfigureAwait(false);

            using var _1 = PooledHashSet<SyntaxNode>.GetInstance(out var nodesToReplace);
            using var _2 = PooledHashSet<XmlEmptyElementSyntax>.GetInstance(out var seeTagsToReplace);
            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    if (location.IsImplicit)
                        continue;

                    if (location.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken) is not IdentifierNameSyntax identifier)
                        continue;

                    var xmlElement = identifier.AncestorsAndSelf().OfType<XmlEmptyElementSyntax>().FirstOrDefault();
                    if (xmlElement is { Name.LocalName.ValueText: "see" })
                    {
                        // reference to member in a `<see cref="name"/>` tag.  Switch to a paramref tag instead.
                        seeTagsToReplace.Add(xmlElement);
                    }
                    else if (identifier.IsRightSideOfDot())
                    {
                        if (identifier.GetRequiredParent() is ExpressionSyntax expression)
                            nodesToReplace.Add(expression);
                    }
                    else
                    {
                        nodesToReplace.Add(identifier);
                    }
                }
            }

            foreach (var group in nodesToReplace.GroupBy(n => n.SyntaxTree))
            {
                var document = solution.GetDocument(group.Key);
                if (document is null)
                    continue;

                var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

                foreach (var nodeToReplace in group)
                {
                    documentEditor.ReplaceNode(
                        nodeToReplace,
                        parameterNameNode.WithTriviaFrom(nodeToReplace));
                }
            }

            foreach (var group in seeTagsToReplace.GroupBy(n => n.SyntaxTree))
            {
                var document = solution.GetDocument(group.Key);
                if (document is null)
                    continue;

                var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

                foreach (var seeTag in group)
                {
                    var paramRefTag = seeTag
                        .ReplaceToken(seeTag.Name.LocalName, Identifier("paramref").WithTriviaFrom(seeTag.Name.LocalName))
                        .WithAttributes([XmlNameAttribute(parameterName)]);

                    documentEditor.ReplaceNode(seeTag, paramRefTag);
                }
            }
        }
    }
}
