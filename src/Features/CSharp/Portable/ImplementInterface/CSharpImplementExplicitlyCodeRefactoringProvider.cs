// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    using static Helpers;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementExplicitlyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var (container, explicitName, name) = await GetContainerAsync(context).ConfigureAwait(false);

            // Make sure we have a member and that it's not already an explicit impl.
            if (container == null || explicitName != null)
                return;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var member = semanticModel.GetDeclaredSymbol(container, cancellationToken) ??
                throw new InvalidOperationException();

            // If this member doesn't implement anything implicitly, then we don't need to do anything
            // with it.
            if (member.ExplicitOrImplicitInterfaceImplementations().Length == 0)
                return;

            var project = document.Project;

            var directlyImplementedMembers = new MultiDictionary<ISymbol, ISymbol>();
            directlyImplementedMembers.AddRange(member, member.ExplicitOrImplicitInterfaceImplementations());

            var codeAction = new MyCodeAction(
                string.Format(FeaturesResources.Implement_0_explicitly, member.Name),
                c => ImplementExplicitlyAsync(project, directlyImplementedMembers, c));

            var containingType = member.ContainingType;
            var interfaceTypes = directlyImplementedMembers.Values.SelectMany(
                c => c.Select(d => d.ContainingType)).Distinct().ToImmutableArray();

            var implementedMembersFromSameInterfaces = GetImplicitlyImplementedMembers(containingType, interfaceTypes);
            var implementedMembersFromAllInterfaces = GetImplicitlyImplementedMembers(containingType, containingType.AllInterfaces);

            var offerForSameInterface = TotalCount(implementedMembersFromSameInterfaces) > TotalCount(directlyImplementedMembers);
            var offerForAllInterfaces = TotalCount(implementedMembersFromAllInterfaces) > TotalCount(implementedMembersFromSameInterfaces);

            if (!offerForSameInterface && !offerForAllInterfaces)
            {
                context.RegisterRefactoring(codeAction);
                return;
            }

            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            nestedActions.Add(codeAction);

            if (offerForSameInterface)
            {
                var interfaceNames = interfaceTypes.Select(i => i.ToDisplayString(NameAndTypeParametersFormat));
                nestedActions.Add(new MyCodeAction(
                    string.Format(FeaturesResources.Implement_0_explicitly, string.Join(", ", interfaceNames)),
                    c => ImplementExplicitlyAsync(project, implementedMembersFromSameInterfaces, c)));
            }

            if (offerForAllInterfaces)
            {
                nestedActions.Add(new MyCodeAction(
                    FeaturesResources.Implement_all_interfaces_explicitly,
                    c => ImplementExplicitlyAsync(project, implementedMembersFromAllInterfaces, c)));
            }

            context.RegisterRefactoring(new CodeAction.CodeActionWithNestedActions(
                FeaturesResources.Implement_implicitly, nestedActions.ToImmutableAndFree(), isInlinable: true));
        }

        private int TotalCount(MultiDictionary<ISymbol, ISymbol> dictionary)
        {
            var result = 0;
            foreach (var (key, values) in dictionary)
            {
                result += values.Count;
            }
            return result;
        }

        private MultiDictionary<ISymbol, ISymbol> GetImplicitlyImplementedMembers(
            INamedTypeSymbol containingType, ImmutableArray<INamedTypeSymbol> interfaceTypes)
        {
            var result = new MultiDictionary<ISymbol, ISymbol>();
            foreach (var interfaceType in interfaceTypes)
            {
                foreach (var interfaceMember in interfaceType.GetMembers())
                {
                    var impl = containingType.FindImplementationForInterfaceMember(interfaceMember);
                    if (impl != null &&
                        containingType.Equals(impl.ContainingType) &&
                        impl.ExplicitInterfaceImplementations().Length == 0)
                    {
                        result.Add(impl, interfaceMember);
                    }
                }
            }

            return result;
        }

        private async Task<Solution> ImplementExplicitlyAsync(
            Project project, MultiDictionary<ISymbol, ISymbol> implMemberToInterfaceMembers,
            CancellationToken cancellationToken)
        {
            // First, we have to go through and find all the references to these interface
            // implementation members.  We'll have to update all callers to call through the
            // interface instead to preserve semantics.  i.e. a call to goo.Bar() will be 
            // updated to `((IGoo)goo).Bar()`.  We'll also add a simplification annotation
            // here so that the cast can go away if not necessary.
            var documentToEditor = new Dictionary<Document, SyntaxEditor>();
            foreach (var (implMember, interfaceMembers) in implMemberToInterfaceMembers)
            {
                await UpdateReferencesAsync(
                    project, documentToEditor, implMember,
                    interfaceMembers.First().ContainingType,
                    cancellationToken).ConfigureAwait(false);
            }

            var solution = project.Solution;

            // Not, bucket all the implemented members by which document they appear in.
            // That way, we can update all the members in a specific document in bulk.
            var documentToImplDeclarations = new MultiDictionary<Document, (SyntaxNode, ISet<ISymbol>)>();
            foreach (var (implMember, interfaceMembers) in implMemberToInterfaceMembers)
            {
                foreach (var syntaxRef in implMember.DeclaringSyntaxReferences)
                {
                    var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                    if (doc != null)
                    {
                        var decl = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                        if (decl != null)
                            documentToImplDeclarations.Add(doc, (decl, interfaceMembers.ToSet()));
                    }
                }
            }

            var currentSolution = solution;
            foreach (var (document, declsAndSymbol) in documentToImplDeclarations)
            {
                var editor = await GetEditor(documentToEditor, document, cancellationToken).ConfigureAwait(false);

                foreach (var (decl, symbols) in declsAndSymbol)
                {
                    if (symbols.Count == 1)
                    {
                        // Make sure we pass in the current value of the decl as it may have had 
                        // edits made inside it as we updated references.
                        editor.ReplaceNode(decl, (currentDecl, _) =>
                            ImplementExplicitly(editor.Generator, currentDecl, symbols.First()));
                    }
                    else
                    {
                        // member implemented multiple interface members.  Break them apart into
                        // copies and have each copy implement the new interface type.

                        // We have to see if we can find the member in the current syntax editor
                        // though in case it has been modified while we were updating references.
                        var latest = editor.GetChangedRoot().GetCurrentNode(decl) ?? decl;

                        foreach (var symbol in symbols)
                        {
                            editor.InsertAfter(decl, ImplementExplicitly(editor.Generator, latest, symbol));
                        }

                        // Then, remove the original decl
                        editor.RemoveNode(decl);
                    }
                }

                currentSolution = currentSolution.WithDocumentSyntaxRoot(
                    document.Id, editor.GetChangedRoot());
            }

            return currentSolution;
        }

        private static async Task<SyntaxEditor> GetEditor(
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

        private async Task UpdateReferencesAsync(
            Project project, Dictionary<Document, SyntaxEditor> documentToEditor,
            ISymbol implMember, INamedTypeSymbol interfaceType, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var references = await SymbolFinder.FindReferencesAsync(
                new SymbolAndProjectId(implMember, project.Id),
                solution, cancellationToken).ConfigureAwait(false);

            var implReferences = references.FirstOrDefault(r => implMember.Equals(r.Definition));
            if (implReferences == null)
                return;

            var referenceByDocument = implReferences.Locations.GroupBy(loc => loc.Document);

            foreach (var group in referenceByDocument)
            {
                var document = group.Key;
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = await GetEditor(documentToEditor, document, cancellationToken).ConfigureAwait(false);

                foreach (var refLocation in group)
                {
                    if (refLocation.IsImplicit)
                        continue;

                    var location = refLocation.Location;
                    if (!location.IsInSource)
                        continue;

                    UpdateLocation(interfaceType, editor, syntaxFacts, location, cancellationToken);
                }
            }
        }

        private void UpdateLocation(
            INamedTypeSymbol interfaceType, SyntaxEditor editor, ISyntaxFactsService syntaxFacts,
            Location location, CancellationToken cancellationToken)
        {
            var identifierName = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (identifierName == null || !syntaxFacts.IsIdentifierName(identifierName))
                return;

            var parent = identifierName.Parent;
            if (syntaxFacts.IsAnyMemberAccessExpression(parent))
            {
                // We have something like `expr.Goo` replace it with `((IGoo)expr).Goo`
                var expr = syntaxFacts.GetExpressionOfMemberAccessExpression(parent);
                editor.ReplaceNode(expr, (current, g) => AddCast(interfaceType, current, g));
                return;
            }

            if (syntaxFacts.IsMemberBindingExpression(parent))
            {
                // We have something like `expr?.Goo` replace it with `((IGoo)expr)?.Goo`
                var expr = syntaxFacts.GetTargetOfMemberBinding(parent);
                editor.ReplaceNode(expr, (current, g) => AddCast(interfaceType, current, g));
                return;
            }

            // Accessing the member not off of <dot>.  i.e just plain `Goo()`.  Replace with
            // ((IGoo)this).Goo();
            var generator = editor.Generator;
            editor.ReplaceNode(
                identifierName,
                generator.MemberAccessExpression(
                    generator.AddParentheses(
                        generator.CastExpression(
                            interfaceType,
                            generator.ThisExpression())),
                    identifierName.WithoutTrivia()).WithTriviaFrom(identifierName));
        }

        private static SyntaxNode AddCast(INamedTypeSymbol interfaceType, SyntaxNode current, SyntaxGenerator g)
        {
            return g.AddParentheses(
                                        g.CastExpression(interfaceType, current.WithoutTrivia())).WithTriviaFrom(current);
        }

        private SyntaxNode ImplementExplicitly(SyntaxGenerator generator, SyntaxNode decl, ISymbol interfaceMember)
            => generator.WithExplicitInterfaceImplementations(decl, ImmutableArray.Create(interfaceMember));

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
