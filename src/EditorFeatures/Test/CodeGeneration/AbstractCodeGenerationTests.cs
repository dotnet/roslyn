// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;
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
                .AddProject(projectId, languageName, $"{languageName}.dll", languageName).GetProject(projectId);

            var normalizedSyntax = syntaxNode.NormalizeWhitespace().ToFullString();
            var document = project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                .AddDocument("Fake Document", SourceText.From(normalizedSyntax));

            var annotatedDocument = document.WithSyntaxRoot(
                    document.GetSyntaxRootAsync().Result.WithAdditionalAnnotations(Simplification.Simplifier.Annotation));

            var annotatedRootNode = annotatedDocument.GetSyntaxRootAsync().Result;

            var simplifiedDocument = Simplification.Simplifier.ReduceAsync(annotatedDocument).Result;

            var rootNode = simplifiedDocument.GetSyntaxRootAsync().Result;

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

        internal void Test(
            Func<SyntaxGenerator, SyntaxNode> nodeCreator,
            string cs, string csSimple,
            string vb, string vbSimple)
        {
            Assert.True(cs != null || csSimple != null || vb != null || vbSimple != null,
                $"At least one of {nameof(cs)}, {nameof(csSimple)}, {nameof(vb)}, {nameof(vbSimple)} must be provided");

            var hostServices = VisualStudioMefHostServices.Create(TestExportProvider.ExportProviderWithCSharpAndVisualBasic);
            var workspace = new AdhocWorkspace(hostServices);

            if (cs != null || csSimple != null)
            {
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<SyntaxGenerator>();

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
                var codeDefFactory = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService<SyntaxGenerator>();

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
