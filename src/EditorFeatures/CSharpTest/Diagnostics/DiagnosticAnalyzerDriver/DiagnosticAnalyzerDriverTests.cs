﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UserDiagnosticProviderEngine
{
    public class DiagnosticAnalyzerDriverTests
    {
        [WpfFact]
        public void DiagnosticAnalyzerDriverAllInOne()
        {
            var source = TestResource.AllInOneCSharpCode;

            // AllInOneCSharpCode has no properties with initializers or named types with primary constructors.
            var symbolKindsWithNoCodeBlocks = new HashSet<SymbolKind>();
            symbolKindsWithNoCodeBlocks.Add(SymbolKind.Property);
            symbolKindsWithNoCodeBlocks.Add(SymbolKind.NamedType);

            var analyzer = new CSharpTrackingDiagnosticAnalyzer();
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source, TestOptions.Regular))
            {
                var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
                AccessSupportedDiagnostics(analyzer);
                DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, document, new Text.TextSpan(0, document.GetTextAsync().Result.Length));
                analyzer.VerifyAllAnalyzerMembersWereCalled();
                analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds();
                analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds();
                analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(symbolKindsWithNoCodeBlocks, true);
            }
        }

        [WpfFact, WorkItem(908658)]
        public void DiagnosticAnalyzerDriverVsAnalyzerDriverOnCodeBlock()
        {
            var methodNames = new string[] { "Initialize", "AnalyzeCodeBlock" };
            var source = @"
[System.Obsolete]
class C
{
    int P { get; set; }
    delegate void A();
    delegate string F();
}
";

            var ideEngineAnalyzer = new CSharpTrackingDiagnosticAnalyzer();
            using (var ideEngineWorkspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source))
            {
                var ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single();
                DiagnosticProviderTestUtilities.GetAllDiagnostics(ideEngineAnalyzer, ideEngineDocument, new Text.TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));
                foreach (var method in methodNames)
                {
                    Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && e.ReturnsVoid));
                    Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && !e.ReturnsVoid));
                    Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.NamedType));
                    Assert.False(ideEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.Property));
                }
            }

            var compilerEngineAnalyzer = new CSharpTrackingDiagnosticAnalyzer();
            using (var compilerEngineWorkspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source))
            {
                var compilerEngineCompilation = (CSharpCompilation)compilerEngineWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync().Result;
                compilerEngineCompilation.GetAnalyzerDiagnostics(new[] { compilerEngineAnalyzer });
                foreach (var method in methodNames)
                {
                    Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && e.ReturnsVoid));
                    Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.MethodKind == MethodKind.DelegateInvoke && !e.ReturnsVoid));
                    Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.NamedType));
                    Assert.False(compilerEngineAnalyzer.CallLog.Any(e => e.CallerName == method && e.SymbolKind == SymbolKind.Property));
                }
            }
        }

        [WpfFact]
        [WorkItem(759)]
        public void DiagnosticAnalyzerDriverIsSafeAgainstAnalyzerExceptions()
        {
            var source = TestResource.AllInOneCSharpCode;
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source, TestOptions.Regular))
            {
                var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
                ThrowingDiagnosticAnalyzer<SyntaxKind>.VerifyAnalyzerEngineIsSafeAgainstExceptions(analyzer =>
                    DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, document, new Text.TextSpan(0, document.GetTextAsync().Result.Length), logAnalyzerExceptionAsDiagnostics: true));
            }
        }

        [WorkItem(908621)]
        [WpfFact]
        public void DiagnosticServiceIsSafeAgainstAnalyzerExceptions_1()
        {
            var analyzer = new ThrowingDiagnosticAnalyzer<SyntaxKind>();
            analyzer.ThrowOn(typeof(DiagnosticAnalyzer).GetProperties().Single().Name);
            AccessSupportedDiagnostics(analyzer);
        }

        [WorkItem(908621)]
        [WpfFact]
        public void DiagnosticServiceIsSafeAgainstAnalyzerExceptions_2()
        {
            var analyzer = new ThrowingDoNotCatchDiagnosticAnalyzer<SyntaxKind>();
            analyzer.ThrowOn(typeof(DiagnosticAnalyzer).GetProperties().Single().Name);
            var exceptions = new List<Exception>();
            try
            {
                AccessSupportedDiagnostics(analyzer);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            Assert.True(exceptions.Count == 0);
        }

        [WpfFact]
        public void AnalyzerOptionsArePassedToAllAnalyzers()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(TestResource.AllInOneCSharpCode, TestOptions.Regular))
            {
                var currentProject = workspace.CurrentSolution.Projects.Single();

                var additionalDocId = DocumentId.CreateNewId(currentProject.Id);
                var newSln = workspace.CurrentSolution.AddAdditionalDocument(additionalDocId, "add.config", SourceText.From("random text"));
                currentProject = newSln.Projects.Single();
                var additionalDocument = currentProject.GetAdditionalDocument(additionalDocId);
                AdditionalText additionalStream = new AdditionalTextDocument(additionalDocument.GetDocumentState());
                AnalyzerOptions options = new AnalyzerOptions(ImmutableArray.Create(additionalStream));
                var analyzer = new OptionsDiagnosticAnalyzer<SyntaxKind>(expectedOptions: options);

                var sourceDocument = currentProject.Documents.Single();
                DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, sourceDocument, new Text.TextSpan(0, sourceDocument.GetTextAsync().Result.Length));
                analyzer.VerifyAnalyzerOptions();
            }
        }

        private void AccessSupportedDiagnostics(DiagnosticAnalyzer analyzer)
        {
            var diagnosticService = new TestDiagnosticAnalyzerService(LanguageNames.CSharp, analyzer);
            diagnosticService.GetDiagnosticDescriptors(projectOpt: null);
        }

        private class ThrowingDoNotCatchDiagnosticAnalyzer<TLanguageKindEnum> : ThrowingDiagnosticAnalyzer<TLanguageKindEnum>, IBuiltInAnalyzer where TLanguageKindEnum : struct
        {
            public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            {
                return DiagnosticAnalyzerCategory.SyntaxAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis | DiagnosticAnalyzerCategory.ProjectAnalysis;
            }
        }

        [WpfFact]
        public void AnalyzerCreatedAtCompilationLevelNeedNotBeCompilationAnalyzer()
        {
            var source = @"x";

            var analyzer = new CompilationAnalyzerWithSyntaxTreeAnalyzer();
            using (var ideEngineWorkspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source))
            {
                var ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, ideEngineDocument, new Text.TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));

                var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == "SyntaxDiagnostic");

                Assert.Equal(1, diagnosticsFromAnalyzer.Count());
            }
        }

        private class CompilationAnalyzerWithSyntaxTreeAnalyzer : DiagnosticAnalyzer
        {
            private const string ID = "SyntaxDiagnostic";

            private static readonly DiagnosticDescriptor s_syntaxDiagnosticDescriptor =
                new DiagnosticDescriptor(ID, title: "Syntax", messageFormat: "Syntax", category: "Test", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(s_syntaxDiagnosticDescriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
            }

            public void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(new SyntaxTreeAnalyzer().AnalyzeSyntaxTree);
            }

            private class SyntaxTreeAnalyzer
            {
                public void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_syntaxDiagnosticDescriptor, context.Tree.GetRoot().GetFirstToken().GetLocation()));
                }
            }
        }

        [WpfFact]
        public void CodeBlockAnalyzersOnlyAnalyzeExecutableCode()
        {
            var source = @"
using System;
class C
{
    void F(int x = 0)
    {
        Console.WriteLine(0);
    }
}
";

            var analyzer = new CodeBlockAnalyzerFactory();
            using (var ideEngineWorkspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source))
            {
                var ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = DiagnosticProviderTestUtilities.GetAllDiagnostics(analyzer, ideEngineDocument, new Text.TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length));
                var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == CodeBlockAnalyzerFactory.Descriptor.Id);
                Assert.Equal(2, diagnosticsFromAnalyzer.Count());
            }

            source = @"
using System;
class C
{
    void F(int x = 0, int y = 1, int z = 2)
    {
        Console.WriteLine(0);
    }
}
";

            using (var compilerEngineWorkspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(source))
            {
                var compilerEngineCompilation = (CSharpCompilation)compilerEngineWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync().Result;
                var diagnostics = compilerEngineCompilation.GetAnalyzerDiagnostics(new[] { analyzer });
                var diagnosticsFromAnalyzer = diagnostics.Where(d => d.Id == CodeBlockAnalyzerFactory.Descriptor.Id);
                Assert.Equal(4, diagnosticsFromAnalyzer.Count());
            }
        }

        private class CodeBlockAnalyzerFactory : DiagnosticAnalyzer
        {
            public static DiagnosticDescriptor Descriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Descriptor);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockStartAction<SyntaxKind>(CreateAnalyzerWithinCodeBlock);
            }

            public void CreateAnalyzerWithinCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> context)
            {
                var blockAnalyzer = new CodeBlockAnalyzer();
                context.RegisterCodeBlockEndAction(blockAnalyzer.AnalyzeCodeBlock);
                context.RegisterSyntaxNodeAction(blockAnalyzer.AnalyzeNode, blockAnalyzer.SyntaxKindsOfInterest.ToArray());
            }

            private class CodeBlockAnalyzer
            {
                public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
                {
                    get
                    {
                        return ImmutableArray.Create(SyntaxKind.MethodDeclaration, SyntaxKind.ExpressionStatement, SyntaxKind.EqualsValueClause);
                    }
                }

                public void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
                {
                }

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    // Ensure only executable nodes are analyzed.
                    Assert.NotEqual(SyntaxKind.MethodDeclaration, context.Node.Kind());
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}
