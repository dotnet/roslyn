﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementExplicitlyCodeRefactoringProvider :
        AbstractChangeImplementionCodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpImplementExplicitlyCodeRefactoringProvider()
        {
        }

        protected override string Implement_0 => FeaturesResources.Implement_0_explicitly;
        protected override string Implement_all_interfaces => FeaturesResources.Implement_all_interfaces_explicitly;
        protected override string Implement => FeaturesResources.Implement_explicitly;

        // If we already have an explicit name, we can't change this to be explicit.
        protected override bool CheckExplicitNameAllowsConversion(ExplicitInterfaceSpecifierSyntax? explicitName)
            => explicitName == null;

        // If we don't implement any interface members we can't convert this to be explicit.
        protected override bool CheckMemberCanBeConverted(ISymbol member)
            => member.ImplicitInterfaceImplementations().Length > 0;

        protected override async Task UpdateReferencesAsync(
            Project project, SolutionEditor solutionEditor,
            ISymbol implMember, INamedTypeSymbol interfaceType, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            // We don't need to cascade in this search, we're only explicitly looking for direct
            // calls to our instance member (and not anyone else already calling through the
            // interface already).
            //
            // This can save a lot of extra time spent finding callers, especially for methods with
            // high fan-out (like IDisposable.Dispose()).
            var findRefsOptions = FindReferencesSearchOptions.Default.WithCascade(false);
            var references = await SymbolFinder.FindReferencesAsync(
                implMember, solution, findRefsOptions, cancellationToken).ConfigureAwait(false);

            var implReferences = references.FirstOrDefault();
            if (implReferences == null)
                return;

            var referenceByDocument = implReferences.Locations.GroupBy(loc => loc.Document);

            foreach (var group in referenceByDocument)
            {
                var document = group.Key;
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = await solutionEditor.GetDocumentEditorAsync(
                    document.Id, cancellationToken).ConfigureAwait(false);

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

        private static void UpdateLocation(
            SemanticModel semanticModel, INamedTypeSymbol interfaceType,
            SyntaxEditor editor, ISyntaxFactsService syntaxFacts,
            Location location, CancellationToken cancellationToken)
        {
            var identifierName = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (identifierName == null || !syntaxFacts.IsIdentifierName(identifierName))
                return;

            var node = syntaxFacts.IsNameOfSimpleMemberAccessExpression(identifierName) || syntaxFacts.IsNameOfMemberBindingExpression(identifierName)
                ? identifierName.Parent
                : identifierName;

            RoslynDebug.Assert(node is object);
            if (syntaxFacts.IsInvocationExpression(node.Parent))
                node = node.Parent;

            var operation = semanticModel.GetOperation(node, cancellationToken);
            var instance = operation switch
            {
                IMemberReferenceOperation memberReference => memberReference.Instance,
                IInvocationOperation invocation => invocation.Instance,
                _ => null,
            };

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
