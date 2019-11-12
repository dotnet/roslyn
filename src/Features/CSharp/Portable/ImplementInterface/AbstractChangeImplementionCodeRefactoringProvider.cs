// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    internal abstract class AbstractChangeImplementionCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly SymbolDisplayFormat NameAndTypeParametersFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected abstract string Implement_0 { get; }
        protected abstract string Implement_all_interfaces { get; }
        protected abstract string Implement { get; }

        protected abstract bool CheckExplicitName(ExplicitInterfaceSpecifierSyntax? explicitName);
        protected abstract bool CheckMember(ISymbol member);
        protected abstract SyntaxNode ChangeImplementation(SyntaxGenerator generator, SyntaxNode currentDecl, ISymbol interfaceMember);
        protected abstract Task UpdateReferencesAsync(Project project, Dictionary<Document, SyntaxEditor> documentToEditor, ISymbol implMember, INamedTypeSymbol containingType, CancellationToken cancellationToken);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (container, explicitName, name) = await GetContainerAsync(context).ConfigureAwait(false);
            if (container == null)
                return;

            if (!CheckExplicitName(explicitName))
                return;

            var (document, _, cancellationToken) = context;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var member = semanticModel.GetDeclaredSymbol(container, cancellationToken) ??
                throw new InvalidOperationException();

            if (!CheckMember(member))
                return;

            var project = document.Project;

            var directlyImplementedMembers = new OrderedMultiDictionary<ISymbol, ISymbol>();
            directlyImplementedMembers.AddRange(member, member.ExplicitOrImplicitInterfaceImplementations());

            var codeAction = new MyCodeAction(
                string.Format(Implement_0, member.ExplicitOrImplicitInterfaceImplementations().First().Name),
                c => ChangeImplementationAsync(project, directlyImplementedMembers, c));

            var containingType = member.ContainingType;
            var interfaceTypes = directlyImplementedMembers.SelectMany(kvp => kvp.Value).Select(
                s => s.ContainingType).Distinct().ToImmutableArray();

            var implementedMembersFromSameInterfaces = GetImplementedMembers(containingType, interfaceTypes);
            var implementedMembersFromAllInterfaces = GetImplementedMembers(containingType, containingType.AllInterfaces);

            var offerForSameInterface = TotalCount(implementedMembersFromSameInterfaces) > TotalCount(directlyImplementedMembers);
            var offerForAllInterfaces = TotalCount(implementedMembersFromAllInterfaces) > TotalCount(implementedMembersFromSameInterfaces);

            // If there's only one member in the interface we implement, and there are no other
            // interfaces, then just offer to switch the implementation for this single member
            if (!offerForSameInterface && !offerForAllInterfaces)
            {
                context.RegisterRefactoring(codeAction);
                return;
            }

            // Otherwise, create a top level action to change the implementation, and offer this
            // action, along with either/both of the other two.

            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            nestedActions.Add(codeAction);

            if (offerForSameInterface)
            {
                var interfaceNames = interfaceTypes.Select(i => i.ToDisplayString(NameAndTypeParametersFormat));
                nestedActions.Add(new MyCodeAction(
                    string.Format(Implement_0, string.Join(", ", interfaceNames)),
                    c => ChangeImplementationAsync(project, implementedMembersFromSameInterfaces, c)));
            }

            if (offerForAllInterfaces)
            {
                nestedActions.Add(new MyCodeAction(
                    Implement_all_interfaces,
                    c => ChangeImplementationAsync(project, implementedMembersFromAllInterfaces, c)));
            }

            context.RegisterRefactoring(new CodeAction.CodeActionWithNestedActions(
                Implement, nestedActions.ToImmutableAndFree(), isInlinable: true));
        }

        private static async Task<(SyntaxNode, ExplicitInterfaceSpecifierSyntax?, SyntaxToken)> GetContainerAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            // Move back if the user is at: X.Goo$$(
            if (span.IsEmpty && token.Kind() == SyntaxKind.OpenParenToken)
                token = token.GetPreviousToken();

            // Offer the feature if the user is anywhere between the start of the explicit-impl of
            // the member (if we have one) and the end if the identifier of the member.
            var (container, explicitName, identifier) = GetContainer(token);
            var applicableSpan = explicitName == null
                ? identifier.FullSpan
                : TextSpan.FromBounds(explicitName.FullSpan.Start, identifier.FullSpan.End);

            if (!applicableSpan.Contains(span))
                return default;

            return (container, explicitName, identifier);
        }

        private static (SyntaxNode, ExplicitInterfaceSpecifierSyntax?, SyntaxToken) GetContainer(SyntaxToken token)
        {
            for (var node = token.Parent; node != null; node = node.Parent)
            {
                var result = node switch
                {
                    MethodDeclarationSyntax member => (member, member.ExplicitInterfaceSpecifier, member.Identifier),
                    PropertyDeclarationSyntax member => (member, member.ExplicitInterfaceSpecifier, member.Identifier),
                    EventDeclarationSyntax member => (member, member.ExplicitInterfaceSpecifier, member.Identifier),
                    _ => default((SyntaxNode member, ExplicitInterfaceSpecifierSyntax?, SyntaxToken)),
                };

                if (result.member != null)
                    return result;
            }

            return default;
        }

        private int TotalCount(OrderedMultiDictionary<ISymbol, ISymbol> dictionary)
        {
            var result = 0;
            foreach (var (key, values) in dictionary)
            {
                result += values.Count;
            }
            return result;
        }

        /// <summary>
        /// Returns a mapping from members in our containing types to all the interface members (of
        /// the sort we care about) that it implements.
        /// </summary>
        private OrderedMultiDictionary<ISymbol, ISymbol> GetImplementedMembers(
            INamedTypeSymbol containingType, ImmutableArray<INamedTypeSymbol> interfaceTypes)
        {
            var result = new OrderedMultiDictionary<ISymbol, ISymbol>();
            foreach (var interfaceType in interfaceTypes)
            {
                foreach (var interfaceMember in interfaceType.GetMembers())
                {
                    var impl = containingType.FindImplementationForInterfaceMember(interfaceMember);
                    if (impl != null &&
                        containingType.Equals(impl.ContainingType) &&
                        CheckMember(impl) &&
                        !impl.IsAccessor())
                    {
                        result.Add(impl, interfaceMember);
                    }
                }
            }

            return result;
        }

        private async Task<Solution> ChangeImplementationAsync(
            Project project, OrderedMultiDictionary<ISymbol, ISymbol> implMemberToInterfaceMembers,
            CancellationToken cancellationToken)
        {
            // First, we have to go through and find all the references to these interface
            // implementation members.  We may have to update them to preserve semantics.  i.e. a
            // call to goo.Bar() will be updated to `((IGoo)goo).Bar()` if we're switching to
            // explicit implementation.
            var documentToEditor = new Dictionary<Document, SyntaxEditor>();
            foreach (var (implMember, interfaceMembers) in implMemberToInterfaceMembers)
            {
                await UpdateReferencesAsync(
                    project, documentToEditor, implMember,
                    interfaceMembers.First().ContainingType,
                    cancellationToken).ConfigureAwait(false);
            }

            var solution = project.Solution;

            // Now, bucket all the implemented members by which document they appear in.
            // That way, we can update all the members in a specific document in bulk.
            var documentToImplDeclarations = new OrderedMultiDictionary<Document, (SyntaxNode, SetWithInsertionOrder<ISymbol>)>();
            foreach (var (implMember, interfaceMembers) in implMemberToInterfaceMembers)
            {
                foreach (var syntaxRef in implMember.DeclaringSyntaxReferences)
                {
                    var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                    if (doc != null)
                    {
                        var decl = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                        if (decl != null)
                        {
                            documentToImplDeclarations.Add(doc, (decl, interfaceMembers));
                        }
                    }
                }
            }

            var currentSolution = solution;
            foreach (var (document, declsAndSymbol) in documentToImplDeclarations)
            {
                var editor = await GetEditor(documentToEditor, document, cancellationToken).ConfigureAwait(false);

                foreach (var (decl, symbols) in declsAndSymbol)
                {
                    editor.ReplaceNode(decl, (currentDecl, g) =>
                        symbols.Select(s => ChangeImplementation(g, currentDecl, s)));
                }

                currentSolution = currentSolution.WithDocumentSyntaxRoot(
                    document.Id, editor.GetChangedRoot());
            }

            return currentSolution;
        }

        protected static async Task<SyntaxEditor> GetEditor(
            Dictionary<Document, SyntaxEditor> documentToEditor, Document document, CancellationToken cancellationToken)
        {
            if (!documentToEditor.TryGetValue(document, out var editor))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
                documentToEditor.Add(document, editor);
            }

            return editor;
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
