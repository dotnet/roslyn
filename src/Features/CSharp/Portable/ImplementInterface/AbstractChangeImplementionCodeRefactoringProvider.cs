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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    using static Helpers;

    internal abstract class AbstractChangeImplementionCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract bool CheckExplicitName(ExplicitInterfaceSpecifierSyntax? explicitName);
        protected abstract bool CheckMember(ISymbol member);
        protected abstract SyntaxNode ChangeImplementation(SyntaxGenerator generator, SyntaxNode currentDecl, ISymbol interfaceMember);
        protected abstract Task UpdateReferencesAsync(Project project, Dictionary<Document, SyntaxEditor> documentToEditor, ISymbol implMember, INamedTypeSymbol containingType, CancellationToken cancellationToken);

        protected abstract string Implement_0 { get; }
        protected abstract string Implement_all_interfaces { get; }
        protected abstract string Implement { get; }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var (container, explicitName, name) = await GetContainerAsync(context).ConfigureAwait(false);
            if (container == null)
                return;

            if (!CheckExplicitName(explicitName))
                return;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var member = semanticModel.GetDeclaredSymbol(container, cancellationToken) ??
                throw new InvalidOperationException();

            if (!CheckMember(member))
                return;

            var project = document.Project;

            var directlyImplementedMembers = new MultiDictionary<ISymbol, ISymbol>();
            directlyImplementedMembers.AddRange(member, member.ExplicitOrImplicitInterfaceImplementations());

            var codeAction = new MyCodeAction(
                string.Format(Implement_0, member.ExplicitOrImplicitInterfaceImplementations().First().Name),
                c => ChangeImplementationAsync(project, directlyImplementedMembers, c));

            var containingType = member.ContainingType;
            var interfaceTypes = directlyImplementedMembers.Values.SelectMany(
                c => c.Select(d => d.ContainingType)).Distinct().ToImmutableArray();

            var implementedMembersFromSameInterfaces = GetImplementedMembers(containingType, interfaceTypes);
            var implementedMembersFromAllInterfaces = GetImplementedMembers(containingType, containingType.AllInterfaces);

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


        private int TotalCount(MultiDictionary<ISymbol, ISymbol> dictionary)
        {
            var result = 0;
            foreach (var (key, values) in dictionary)
            {
                result += values.Count;
            }
            return result;
        }

        private MultiDictionary<ISymbol, ISymbol> GetImplementedMembers(
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
            Project project, MultiDictionary<ISymbol, ISymbol> implMemberToInterfaceMembers,
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
                            ChangeImplementation(editor.Generator, currentDecl, symbols.First()));
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
                            editor.InsertAfter(decl,
                                ChangeImplementation(editor.Generator, latest, symbol));
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
