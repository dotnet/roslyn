// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementExplicitlyCodeRefactoringProvider :
        AbstractChangeImplementionCodeRefactoringProvider
    {
        protected override string Implement_0 => FeaturesResources.Implement_0_explicitly;
        protected override string Implement_all_interfaces => FeaturesResources.Implement_all_interfaces_explicitly;
        protected override string Implement => FeaturesResources.Implement_explicitly;

        // If we already have an explicit name, we can't change this to be explicit.
        protected override bool CheckExplicitName(ExplicitInterfaceSpecifierSyntax? explicitName)
            => explicitName == null;

        // If we don't implement any interface members we can't convert this to be explicit.
        protected override bool CheckMember(ISymbol member)
            => member.ExplicitOrImplicitInterfaceImplementations().Length > 0;

        protected override async Task UpdateReferencesAsync(
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

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = await GetEditor(documentToEditor, document, cancellationToken).ConfigureAwait(false);

                foreach (var refLocation in group)
                {
                    if (refLocation.IsImplicit)
                        continue;

                    var location = refLocation.Location;
                    if (!location.IsInSource)
                        continue;

                    UpdateLocation(
                        semanticModel, interfaceType, editor,
                        syntaxFacts, location, cancellationToken);
                }
            }
        }

        private void UpdateLocation(
            SemanticModel semanticModel, INamedTypeSymbol interfaceType,
            SyntaxEditor editor, ISyntaxFactsService syntaxFacts,
            Location location, CancellationToken cancellationToken)
        {
            var identifierName = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (identifierName == null || !syntaxFacts.IsIdentifierName(identifierName))
                return;

            var node = syntaxFacts.IsNameOfMemberAccessExpression(identifierName) || syntaxFacts.IsMemberBindingExpression(identifierName.Parent)
                ? identifierName.Parent
                : identifierName;

            if (syntaxFacts.IsInvocationExpression(node.Parent))
                node = node.Parent;

            var operation = semanticModel.GetOperation(node);
            if (operation == null || operation.Kind == OperationKind.None)
                return;

            var instance =
                operation is IMemberReferenceOperation memberReference ? memberReference.Instance :
                operation is IInvocationOperation invocation ? invocation.Instance : null;

            if (instance == null)
                return;

            if (instance.IsImplicit)
            {
                if (instance is IInstanceReferenceOperation instanceReference &&
                    instanceReference.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
                {
                    return;
                }

                // Accessing the member not off of <dot>.  i.e just plain `Goo()`.  Replace with
                // ((IGoo)this).Goo();
                var generator = editor.Generator;
                editor.ReplaceNode(
                    identifierName,
                    generator.MemberAccessExpression(
                        generator.AddParentheses(generator.CastExpression(interfaceType, generator.ThisExpression())),
                        identifierName.WithoutTrivia()).WithTriviaFrom(identifierName));
            }
            else
            {
                // Accessing the member like `x.Goo()`.  Replace with `((IGoo)x).Goo()`
                editor.ReplaceNode(
                    instance.Syntax, (current, g) =>
                        g.AddParentheses(
                            g.CastExpression(interfaceType, current.WithoutTrivia())).WithTriviaFrom(current));
            }
        }

        protected override SyntaxNode ChangeImplementation(SyntaxGenerator generator, SyntaxNode decl, ISymbol interfaceMember)
            => generator.WithExplicitInterfaceImplementations(decl, ImmutableArray.Create(interfaceMember));
    }
}
