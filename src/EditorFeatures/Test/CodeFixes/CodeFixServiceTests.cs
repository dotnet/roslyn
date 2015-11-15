// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes
{
    public class CodeFixServiceTests
    {
        [WpfFact]
        public void TestGetFirstDiagnosticWithFixAsync()
        {
            var diagnosticService = new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());

            var fixers = CreateFixers();
            var code = @"
    a
";
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
            {
                var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetService<IErrorLoggerService>()));
                var fixService = new CodeFixService(
                    diagnosticService, logger, fixers, SpecializedCollections.EmptyEnumerable<Lazy<ISuppressionFixProvider, CodeChangeProviderMetadata>>());

                var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

                // register diagnostic engine to solution crawler
                var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

                var reference = new MockAnalyzerReference();
                var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
                var document = project.Documents.Single();
                var unused = fixService.GetFirstDiagnosticWithFixAsync(document, TextSpan.FromBounds(0, 0), considerSuppressionFixes: false, cancellationToken: CancellationToken.None).Result;

                var fixer1 = fixers.Single().Value as MockFixer;
                var fixer2 = reference.Fixer as MockFixer;

                // check to make sure both of them are called.
                Assert.True(fixer1.Called);
                Assert.True(fixer2.Called);
            }
        }

        [WpfFact]
        public void TestGetCodeFixWithExceptionInRegisterMethod()
        {
            GetFirstDiagnosticWithFix(new ErrorCases.ExceptionInRegisterMethod());
            GetAddedFixes(new ErrorCases.ExceptionInRegisterMethod());
        }

        [WpfFact]
        public void TestGetCodeFixWithExceptionInRegisterMethodAsync()
        {
            GetFirstDiagnosticWithFix(new ErrorCases.ExceptionInRegisterMethodAsync());
            GetAddedFixes(new ErrorCases.ExceptionInRegisterMethodAsync());
        }

        [WpfFact]
        public void TestGetCodeFixWithExceptionInFixableDiagnosticIds()
        {
            GetDefaultFixes(new ErrorCases.ExceptionInFixableDiagnosticIds());
            GetAddedFixes(new ErrorCases.ExceptionInFixableDiagnosticIds());
        }

        [WpfFact]
        public void TestGetCodeFixWithExceptionInFixableDiagnosticIds2()
        {
            GetDefaultFixes(new ErrorCases.ExceptionInFixableDiagnosticIds2());
            GetAddedFixes(new ErrorCases.ExceptionInFixableDiagnosticIds2());
        }

        [WpfFact]
        public void TestGetCodeFixWithExceptionInGetFixAllProvider()
        {
            GetAddedFixes(new ErrorCases.ExceptionInGetFixAllProvider());
        }

        public void GetDefaultFixes(CodeFixProvider codefix)
        {
            TestDiagnosticAnalyzerService diagnosticService;
            CodeFixService fixService;
            IErrorLoggerService errorLogger;
            using (var workspace = ServiceSetup(codefix, out diagnosticService, out fixService, out errorLogger))
            {
                Document document;
                EditorLayerExtensionManager.ExtensionManager extensionManager;
                GetDocumentAndExtensionManager(diagnosticService, workspace, out document, out extensionManager);
                var fixes = fixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), includeSuppressionFixes: true, cancellationToken: CancellationToken.None).Result;
                Assert.True(((TestErrorLogger)errorLogger).Messages.Count == 1);
                string message;
                Assert.True(((TestErrorLogger)errorLogger).Messages.TryGetValue(codefix.GetType().Name, out message));
            }
        }

        public void GetAddedFixes(CodeFixProvider codefix)
        {
            TestDiagnosticAnalyzerService diagnosticService;
            CodeFixService fixService;
            IErrorLoggerService errorLogger;
            using (var workspace = ServiceSetup(codefix, out diagnosticService, out fixService, out errorLogger))
            {
                Document document;
                EditorLayerExtensionManager.ExtensionManager extensionManager;
                GetDocumentAndExtensionManager(diagnosticService, workspace, out document, out extensionManager);
                var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;
                var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);
                var reference = new MockAnalyzerReference(codefix);
                var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
                document = project.Documents.Single();
                var fixes = fixService.GetFixesAsync(document, TextSpan.FromBounds(0, 0), includeSuppressionFixes: true, cancellationToken: CancellationToken.None).Result;

                Assert.True(extensionManager.IsDisabled(codefix));
                Assert.False(extensionManager.IsIgnored(codefix));
            }
        }

        public void GetFirstDiagnosticWithFix(CodeFixProvider codefix)
        {
            TestDiagnosticAnalyzerService diagnosticService;
            CodeFixService fixService;
            IErrorLoggerService errorLogger;
            using (var workspace = ServiceSetup(codefix, out diagnosticService, out fixService, out errorLogger))
            {
                Document document;
                EditorLayerExtensionManager.ExtensionManager extensionManager;
                GetDocumentAndExtensionManager(diagnosticService, workspace, out document, out extensionManager);
                var unused = fixService.GetFirstDiagnosticWithFixAsync(document, TextSpan.FromBounds(0, 0), considerSuppressionFixes: false, cancellationToken: CancellationToken.None).Result;
                Assert.True(extensionManager.IsDisabled(codefix));
                Assert.False(extensionManager.IsIgnored(codefix));
            }
        }

        private static TestWorkspace ServiceSetup(CodeFixProvider codefix, out TestDiagnosticAnalyzerService diagnosticService, out CodeFixService fixService, out IErrorLoggerService errorLogger)
        {
            diagnosticService = new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            var fixers = SpecializedCollections.SingletonEnumerable(
                new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(
                () => codefix,
                new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));
            var code = @"class Program { }";
            var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code);
            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => new TestErrorLogger()));
            errorLogger = logger.First().Value;
            fixService = new CodeFixService(
                    diagnosticService, logger, fixers, SpecializedCollections.EmptyEnumerable<Lazy<ISuppressionFixProvider, CodeChangeProviderMetadata>>());
            return workspace;
        }

        private static void GetDocumentAndExtensionManager(TestDiagnosticAnalyzerService diagnosticService, TestWorkspace workspace, out Document document, out EditorLayerExtensionManager.ExtensionManager extensionManager)
        {
            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

            // register diagnostic engine to solution crawler
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

            var reference = new MockAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            document = project.Documents.Single();
            extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
        }

        private IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> CreateFixers()
        {
            return SpecializedCollections.SingletonEnumerable(
                new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(() => new MockFixer(), new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));
        }

        internal class MockFixer : CodeFixProvider
        {
            public const string Id = "MyDiagnostic";
            public bool Called = false;

            public sealed override ImmutableArray<string> FixableDiagnosticIds
            {
                get { return ImmutableArray.Create(Id); }
            }

            public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                Called = true;
                return SpecializedTasks.EmptyTask;
            }
        }

        private class MockAnalyzerReference : AnalyzerReference, ICodeFixProviderFactory
        {
            public readonly CodeFixProvider Fixer;
            public readonly MockDiagnosticAnalyzer Analyzer = new MockDiagnosticAnalyzer();

            public MockAnalyzerReference()
            {
                Fixer = new MockFixer();
            }

            public MockAnalyzerReference(CodeFixProvider codeFix)
            {
                Fixer = codeFix;
            }

            public override string Display
            {
                get
                {
                    return "MockAnalyzerReference";
                }
            }

            public override string FullPath
            {
                get
                {
                    return string.Empty;
                }
            }

            public override object Id
            {
                get
                {
                    return "MockAnalyzerReference";
                }
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            {
                return ImmutableArray.Create<DiagnosticAnalyzer>(Analyzer);
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            {
                return ImmutableArray<DiagnosticAnalyzer>.Empty;
            }

            public ImmutableArray<CodeFixProvider> GetFixers()
            {
                return ImmutableArray.Create<CodeFixProvider>(Fixer);
            }

            public class MockDiagnosticAnalyzer : DiagnosticAnalyzer
            {
                private DiagnosticDescriptor _descriptor = new DiagnosticDescriptor(MockFixer.Id, "MockDiagnostic", "MockDiagnostic", "InternalCategory", DiagnosticSeverity.Warning, isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(_descriptor);
                    }
                }

                public override void Initialize(AnalysisContext context)
                {
                    context.RegisterSyntaxTreeAction(c =>
                    {
                        c.ReportDiagnostic(Diagnostic.Create(_descriptor, c.Tree.GetLocation(TextSpan.FromBounds(0, 0))));
                    });
                }
            }
        }

        internal class TestErrorLogger : IErrorLoggerService
        {
            public Dictionary<string, string> Messages = new Dictionary<string, string>();

            public void LogException(object source, Exception exception)
            {
                Messages.Add(source.GetType().Name, ToLogFormat(exception));
            }

            public bool TryLogException(object source, Exception exception)
            {
                try
                {
                    Messages.Add(source.GetType().Name, ToLogFormat(exception));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private static string ToLogFormat(Exception exception)
            {
                return exception.Message + Environment.NewLine + exception.StackTrace;
            }
        }
    }
}
