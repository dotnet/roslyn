// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class AbstractRunIterations<TMethodDeclarationSyntax> : CodeRefactoringProvider
        where TMethodDeclarationSyntax : SyntaxNode
    {
        private protected abstract IRefactoringHelpers RefactoringHelpers { get; }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var method = await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>(RefactoringHelpers).ConfigureAwait(false);
            if (method is null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            RoslynDebug.Assert(semanticModel is not null);

            if (!semanticModel.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.XunitFactAttribute, out var factAttribute)
                || !semanticModel.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.XunitSdkDataAttribute, out var dataAttribute)
                || !semanticModel.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.XunitCombinatorialDataAttribute, out var combinatorialDataAttribute))
            {
                return;
            }

            var knownTestAttributes = new ConcurrentDictionary<INamedTypeSymbol, bool>();
            var methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(method, context.CancellationToken)!;
            if (!methodSymbol.IsBenchmarkOrXUnitTestMethod(knownTestAttributes, benchmarkAttribute: null, factAttribute))
                return;

            foreach (var parameter in methodSymbol.Parameters)
            {
                // This is already a test with iterations
                if (parameter.Name == "iteration")
                    return;
            }

            // When true, this method is a [Fact] (or related) test which requires conversion to [Theory] with
            // application of [CombinatorialData] as part of the refactoring. Otherwise, this test is already a [Theory]
            // and only needs an additional parameter added.
            bool convertToTheory = true;

            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (!attribute.AttributeClass.DerivesFrom(dataAttribute))
                    continue;

                if (!attribute.AttributeClass.DerivesFrom(combinatorialDataAttribute))
                {
                    // The test is already a theory, but doesn't use [CombinatorialData]. It's not known how this test
                    // can be automatically converted to run iterations.
                    return;
                }

                convertToTheory = false;
                break;
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.Run_iterations,
                    cancellationToken => AbstractRunIterations<TMethodDeclarationSyntax>.RunIterationsAsync(context.Document, method, convertToTheory, cancellationToken),
                    equivalenceKey: nameof(RoslynDiagnosticsAnalyzersResources.Run_iterations)));
        }

        private static async Task<Document> RunIterationsAsync(Document document, TMethodDeclarationSyntax method, bool convertToTheory, CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            SyntaxNode updatedMethod = method;

            if (convertToTheory)
            {
                foreach (var attribute in syntaxGenerator.GetAttributes(method))
                {
                    var name = syntaxGenerator.GetName(attribute);
                    if (name.EndsWith("Fact", StringComparison.Ordinal))
                    {
                        updatedMethod = updatedMethod.ReplaceNode(
                            attribute,
                            ReplaceName(syntaxGenerator, attribute, name[0..^4] + "Theory"));
                        break;
                    }
                    else if (name.EndsWith("FactAttribute", StringComparison.Ordinal))
                    {
                        updatedMethod = updatedMethod.ReplaceNode(
                            attribute,
                            ReplaceName(syntaxGenerator, attribute, name[0..^"FactAttribute".Length] + "TheoryAttribute"));
                        break;
                    }
                }

                updatedMethod = syntaxGenerator.AddAttributes(updatedMethod, syntaxGenerator.Attribute(WellKnownTypeNames.XunitCombinatorialDataAttribute).WithAddImportsAnnotation());
            }

            updatedMethod = syntaxGenerator.AddParameters(
                updatedMethod,
                new[]
                {
                    syntaxGenerator.AddAttributes(
                        syntaxGenerator.ParameterDeclaration(
                            "iteration",
                            syntaxGenerator.TypeExpression(SpecialType.System_Int32)),
                        syntaxGenerator.Attribute(
                            WellKnownTypeNames.XunitCombinatorialRangeAttribute,
                            syntaxGenerator.LiteralExpression(0),
                            syntaxGenerator.LiteralExpression(10))),
                });

            // For C# test projects, add a discard assignment to avoid xunit warnings about unused theory parameters
            if (document.Project.Language == LanguageNames.CSharp)
            {
                var assignment = syntaxGenerator.AssignmentStatement(syntaxGenerator.IdentifierName("_"), syntaxGenerator.IdentifierName("iteration"));
                var statements = syntaxGenerator.GetStatements(updatedMethod);
                updatedMethod = syntaxGenerator.WithStatements(updatedMethod, new[] { assignment }.Concat(statements));
            }

            var root = await method.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(root.ReplaceNode(method, updatedMethod));
        }

        private static SyntaxNode ReplaceName(SyntaxGenerator syntaxGenerator, SyntaxNode node, string name)
        {
            var newNode = syntaxGenerator.WithName(node, name);
            if (newNode.RawKind != node.RawKind
                && newNode.ChildNodes().FirstOrDefault()?.RawKind == node.RawKind)
            {
                // The call to WithName may have converted AttributeSyntax to AttributeListSyntax; we only want the
                // AttributeSyntax portion.
                newNode = newNode.ChildNodes().First();
            }

            return newNode;
        }
    }
}
