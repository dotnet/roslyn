// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    using static Helpers;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementImplicitlyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var (container, explicitName, name) = await GetContainerAsync(context).ConfigureAwait(false);
            if (explicitName == null)
                return;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var member = semanticModel.GetDeclaredSymbol(container, cancellationToken) ??
                throw new InvalidOperationException();

            if (member.ExplicitInterfaceImplementations().Length != 1)
                return;

            var solution = document.Project.Solution;

            var interfaceMember = member.ExplicitInterfaceImplementations().Single();
            var directImplementions = new HashSet<ISymbol> { member };
            var codeAction = new MyCodeAction(
                string.Format(FeaturesResources.Implement_0_implicitly, interfaceMember.Name),
                c => ImplementImplicitlyAsync(solution, directImplementions, c));

            var containingType = member.ContainingType;
            var interfaceType = member.ExplicitInterfaceImplementations().Single().ContainingType;

            var implementationsFromSameInterface = GetExplicitlyImplementedMembers(containingType, interfaceType).ToSet();
            var implementationsFromAllInterfaces = containingType.AllInterfaces.SelectMany(
                i => GetExplicitlyImplementedMembers(containingType, i)).ToSet();

            var offerForSameInterface = implementationsFromSameInterface.Count > directImplementions.Count;
            var offerForAllInterfaces = implementationsFromAllInterfaces.Count > implementationsFromSameInterface.Count;

            // There was only one member in the interface that we implement.  Only need to show
            // the single action.
            if (!offerForSameInterface && !offerForAllInterfaces)
            {
                context.RegisterRefactoring(codeAction);
                return;
            }

            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            nestedActions.Add(codeAction);

            if (offerForSameInterface)
            {
                nestedActions.Add(new MyCodeAction(
                    string.Format(
                        FeaturesResources.Implement_0_implicitly,
                        interfaceType.ToDisplayString(NameAndTypeParametersFormat)),
                    c => ImplementImplicitlyAsync(solution, implementationsFromSameInterface, c)));
            }

            if (offerForAllInterfaces)
            {
                nestedActions.Add(new MyCodeAction(
                    FeaturesResources.Implement_all_interfaces_implicitly,
                    c => ImplementImplicitlyAsync(solution, implementationsFromAllInterfaces, c)));
            }

            context.RegisterRefactoring(new CodeAction.CodeActionWithNestedActions(
                FeaturesResources.Implement_implicitly, nestedActions.ToImmutableAndFree(), isInlinable: true));
        }

        private IEnumerable<ISymbol> GetExplicitlyImplementedMembers(INamedTypeSymbol containingType, INamedTypeSymbol interfaceType)
            => from interfaceMember in interfaceType.GetMembers()
               let impl = containingType.FindImplementationForInterfaceMember(interfaceMember)
               where impl != null
               where containingType.Equals(impl.ContainingType)
               where impl.ExplicitInterfaceImplementations().Length > 0
               select impl;

        private async Task<Solution> ImplementImplicitlyAsync(
            Solution solution, ISet<ISymbol> implementations, CancellationToken cancellationToken)
        {
            // First, bucket all the implemented members by which document they appear in.
            // That way, we can update all the members in a specific document in bulk.
            var documentToImplDeclarations = new MultiDictionary<Document, SyntaxNode>();
            foreach (var impl in implementations)
            {
                foreach (var syntaxRef in impl.DeclaringSyntaxReferences)
                {
                    var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                    if (doc != null)
                    {
                        var decl = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                        if (decl != null)
                            documentToImplDeclarations.Add(doc, decl);
                    }
                }
            }

            var currentSolution = solution;
            foreach (var (document, decls) in documentToImplDeclarations)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(root, solution.Workspace);

                foreach (var decl in decls)
                {
                    var updatedDecl = ImplementImplicitly(editor.Generator, decl);
                    if (updatedDecl != null)
                        editor.ReplaceNode(decl, updatedDecl);
                }

                currentSolution = currentSolution.WithDocumentSyntaxRoot(
                    document.Id, editor.GetChangedRoot());
            }

            return currentSolution;
        }

        private SyntaxNode ImplementImplicitly(SyntaxGenerator generator, SyntaxNode decl)
            => generator.WithAccessibility(WithoutExplicitImpl(decl), Accessibility.Public);

        private SyntaxNode? WithoutExplicitImpl(SyntaxNode decl)
            => decl switch
            {
                MethodDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                PropertyDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                EventDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                _ => null,
            };

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
