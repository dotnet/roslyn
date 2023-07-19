// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    [UseExportProvider]
    public abstract class AbstractCodeGenerationTests
    {
        private static SyntaxNode Simplify(
            AdhocWorkspace workspace,
            SyntaxNode syntaxNode,
            string languageName)
        {
            var projectId = ProjectId.CreateNewId();

            var project = workspace.CurrentSolution
                .AddProject(projectId, languageName, $"{languageName}.dll", languageName).GetRequiredProject(projectId);

            var normalizedSyntax = syntaxNode.NormalizeWhitespace().ToFullString();
            var document = project.AddMetadataReference(TestMetadata.Net451.mscorlib)
                .AddDocument("Fake Document", SourceText.From(normalizedSyntax));

            var root = document.GetRequiredSyntaxRootAsync(default).AsTask().Result;
            var annotatedDocument = document.WithSyntaxRoot(
                    root.WithAdditionalAnnotations(Simplifier.Annotation));

            var options = document.Project.Services.GetRequiredService<ISimplificationService>().DefaultOptions;
            var simplifiedDocument = Simplifier.ReduceAsync(annotatedDocument, options, CancellationToken.None).Result;

            var rootNode = simplifiedDocument.GetRequiredSyntaxRootAsync(default).AsTask().Result;

            return rootNode;
        }

        private static SyntaxNode WrapExpressionInBoilerplate(SyntaxNode expression, SyntaxGenerator codeDefFactory)
        {
            return codeDefFactory.CompilationUnit(
                codeDefFactory.NamespaceImportDeclaration(codeDefFactory.IdentifierName("System")),
                codeDefFactory.ClassDeclaration(
                    "C",
                    members: new[]
                    {
                        codeDefFactory.MethodDeclaration(
                            "Dummy",
                            returnType: null,
                            statements: new[]
                            {
                                codeDefFactory.LocalDeclarationStatement("test", expression)
                            })
                    })
                );
        }

        internal static void Test(
            Func<SyntaxGenerator, SyntaxNode> nodeCreator,
            string cs, string csSimple,
            string vb, string vbSimple)
        {
            Assert.True(cs != null || csSimple != null || vb != null || vbSimple != null,
                $"At least one of {nameof(cs)}, {nameof(csSimple)}, {nameof(vb)}, {nameof(vbSimple)} must be provided");

            using var workspace = new AdhocWorkspace();

            if (cs != null || csSimple != null)
            {
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<SyntaxGenerator>();

                var node = nodeCreator(codeDefFactory);
                node = node.NormalizeWhitespace();

                if (cs != null)
                {
                    TokenUtilities.AssertTokensEqual(cs, node.ToFullString(), LanguageNames.CSharp);
                }

                if (csSimple != null)
                {
                    var simplifiedRootNode = Simplify(workspace, WrapExpressionInBoilerplate(node, codeDefFactory), LanguageNames.CSharp);
                    var expression = simplifiedRootNode.DescendantNodes().OfType<EqualsValueClauseSyntax>().First().Value;

                    TokenUtilities.AssertTokensEqual(csSimple, expression.NormalizeWhitespace().ToFullString(), LanguageNames.CSharp);
                }
            }

            if (vb != null || vbSimple != null)
            {
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetRequiredService<SyntaxGenerator>();

                var node = nodeCreator(codeDefFactory);
                node = node.NormalizeWhitespace();

                if (vb != null)
                {
                    TokenUtilities.AssertTokensEqual(vb, node.ToFullString(), LanguageNames.VisualBasic);
                }

                if (vbSimple != null)
                {
                    var simplifiedRootNode = Simplify(workspace, WrapExpressionInBoilerplate(node, codeDefFactory), LanguageNames.VisualBasic);
                    var expression = simplifiedRootNode.DescendantNodes().OfType<EqualsValueSyntax>().First().Value;

                    TokenUtilities.AssertTokensEqual(vbSimple, expression.NormalizeWhitespace().ToFullString(), LanguageNames.VisualBasic);
                }
            }
        }

        protected static ITypeSymbol CreateClass(string name)
        {
            return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default, accessibility: default, modifiers: default, TypeKind.Class, name);
        }
    }
}
