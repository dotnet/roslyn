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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementImplicitlyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            // Move back if the user is at: X.Goo$$(
            if (span.IsEmpty && token.Kind() == SyntaxKind.OpenParenToken)
                token = token.GetPreviousToken();

            var (container, explicitName, name) = GetContainer(token);
            if (explicitName == null)
                return;

            var applicableSpan = TextSpan.FromBounds(explicitName.FullSpan.Start, name.FullSpan.End);
            if (!applicableSpan.Contains(span))
                return;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var member = semanticModel.GetDeclaredSymbol(container, cancellationToken) ??
                throw new InvalidOperationException();

            var explicitImpls = member.ExplicitInterfaceImplementations();
            if (explicitImpls.Length != 1)
                return;

            var explicitImpl = explicitImpls[0];
            var interfaceType = explicitImpl.ContainingType;
            var interfaceName = interfaceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var solution = document.Project.Solution;

            var codeAction = new MyCodeAction(
                string.Format(FeaturesResources.Implement_0_implicitly, explicitImpl.Name),
                c => ImplementImplicitlyAsync(solution, new HashSet<ISymbol> { member }, c));

            var containingType = member.ContainingType;
            var explicitlyImplementedMembers = GetExplicitlyImplementedMembers(containingType, interfaceType).ToSet();

            // There was only one member in the interface that we implement.  Only need to show
            // the single action.
            if (explicitlyImplementedMembers.Count <= 1)
            {
                context.RegisterRefactoring(codeAction);
                return;
            }

            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            nestedActions.Add(codeAction);
            if (explicitlyImplementedMembers.Count > 1)
                nestedActions.Add(new MyCodeAction(
                    string.Format(FeaturesResources.Implement_0_implicitly, interfaceName),
                    c => ImplementImplicitlyAsync(solution, explicitlyImplementedMembers, c)));

            var allExplicitlyImplementedMembers = containingType.AllInterfaces.SelectMany(
                i => GetExplicitlyImplementedMembers(containingType, i)).ToSet();

            if (allExplicitlyImplementedMembers.Count > explicitlyImplementedMembers.Count)
                nestedActions.Add(new MyCodeAction(
                    FeaturesResources.Implement_all_interfaces_implicitly,
                    c => ImplementImplicitlyAsync(solution, allExplicitlyImplementedMembers, c)));

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

        private (SyntaxNode, ExplicitInterfaceSpecifierSyntax?, SyntaxToken) GetContainer(SyntaxToken token)
        {
            for (var node = token.Parent; node != null; node = node.Parent)
            {
                switch (node)
                {
                    case MethodDeclarationSyntax method:
                        return (method, method.ExplicitInterfaceSpecifier, method.Identifier);
                    case PropertyDeclarationSyntax property:
                        return (property, property.ExplicitInterfaceSpecifier, property.Identifier);
                    case EventDeclarationSyntax ev:
                        return (ev, ev.ExplicitInterfaceSpecifier, ev.Identifier);
                }
            }

            return default;
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
