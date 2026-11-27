// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestFixes.CSharpCodeFixTest<
    Microsoft.CodeAnalysis.Testing.NonLocalDiagnosticsTests.HighlightVariableFromInitializerAnalyzer,
    Microsoft.CodeAnalysis.Testing.NonLocalDiagnosticsTests.RemoveInitializerCodeFix>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class NonLocalDiagnosticsTests
    {
        [Fact]
        public async Task NonLocalDiagnosticFailsValidation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await new CSharpTest
                    {
                        TestCode = @"class [|TestClass|]
{
    private int Value = 3;
}",
                        FixedCode = @"class TestClass
{
    private int Value;
}",
                    }.RunAsync();
                });

            Assert.Equal(
                @"Code fix is attempting to provide a fix for a non-local analyzer diagnostic",
                exception.Message);
        }

        [Fact]
        public async Task NonLocalDiagnosticFixedWhenValidationDisabled()
        {
            await new CSharpTest
            {
                CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck,
                TestCode = @"class [|TestClass|]
{
    private int Value = 3;
}",
                FixedCode = @"class TestClass
{
    private int Value;
}",
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class HighlightVariableFromInitializerAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor("RemoveVariable", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxNodeAction(HandleEqualsValueClause, SyntaxKind.EqualsValueClause);
            }

            private void HandleEqualsValueClause(SyntaxNodeAnalysisContext context)
            {
                var equalsValueClause = (EqualsValueClauseSyntax)context.Node;
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, equalsValueClause.FirstAncestorOrSelf<ClassDeclarationSyntax>()!.Identifier.GetLocation()));
            }
        }

        internal class RemoveInitializerCodeFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HighlightVariableFromInitializerAnalyzer.Descriptor.Id);

            public override FixAllProvider? GetFixAllProvider()
                => null;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Remove initializer",
                            async cancellationToken =>
                            {
                                var syntaxRoot = await context.Document.GetSyntaxRootAsync(cancellationToken);
                                var classDeclaration = (ClassDeclarationSyntax)syntaxRoot!.FindToken(diagnostic.Location.SourceSpan.Start).Parent!;
                                var declarator = ((FieldDeclarationSyntax)classDeclaration.Members[0]).Declaration.Variables[0];
                                return context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(declarator, declarator.WithInitializer(null)).WithAdditionalAnnotations(Formatter.Annotation));
                            },
                            "Remove initializer"),
                        diagnostic);
                }

                return Task.CompletedTask;
            }
        }
    }
}
