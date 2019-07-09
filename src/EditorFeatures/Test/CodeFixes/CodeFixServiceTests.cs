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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes
{
    [UseExportProvider]
    public class CodeFixServiceTests
    {
        [Fact]
        public async Task TestGetFirstDiagnosticWithFixAsync()
        {
            var diagnosticService = new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());

            var fixers = CreateFixers();
            var code = @"
    a
";
            using var workspace = TestWorkspace.CreateCSharp(code);

            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => workspace.Services.GetService<IErrorLoggerService>()));
            var fixService = new CodeFixService(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                diagnosticService, logger, fixers, SpecializedCollections.EmptyEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>());

            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)diagnosticService;

            // register diagnostic engine to solution crawler
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);

            var reference = new MockAnalyzerReference();
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            var document = project.Documents.Single();
            var unused = await fixService.GetMostSevereFixableDiagnosticAsync(document, TextSpan.FromBounds(0, 0), cancellationToken: CancellationToken.None);

            var fixer1 = fixers.Single().Value as MockFixer;
            var fixer2 = reference.Fixer as MockFixer;

            // check to make sure both of them are called.
            Assert.True(fixer1.Called);
            Assert.True(fixer2.Called);
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethod()
        {
            await GetFirstDiagnosticWithFixAsync(new ErrorCases.ExceptionInRegisterMethod());
            await GetAddedFixesAsync(new ErrorCases.ExceptionInRegisterMethod());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInRegisterMethodAsync()
        {
            await GetFirstDiagnosticWithFixAsync(new ErrorCases.ExceptionInRegisterMethodAsync());
            await GetAddedFixesAsync(new ErrorCases.ExceptionInRegisterMethodAsync());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds()
        {
            await GetDefaultFixesAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());
            await GetAddedFixesAsync(new ErrorCases.ExceptionInFixableDiagnosticIds());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21533")]
        public async Task TestGetCodeFixWithExceptionInFixableDiagnosticIds2()
        {
            await GetDefaultFixesAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());
            await GetAddedFixesAsync(new ErrorCases.ExceptionInFixableDiagnosticIds2());
        }

        [Fact]
        public async Task TestGetCodeFixWithExceptionInGetFixAllProvider()
        {
            await GetAddedFixesAsync(new ErrorCases.ExceptionInGetFixAllProvider());
        }

        private async Task GetDefaultFixesAsync(CodeFixProvider codefix)
        {
            var tuple = ServiceSetup(codefix);
            using var workspace = tuple.Item1;

            GetDocumentAndExtensionManager(tuple.Item2, workspace, out var document, out var extensionManager);
            var fixes = await tuple.Item3.GetFixesAsync(document, TextSpan.FromBounds(0, 0), includeConfigurationFixes: true, cancellationToken: CancellationToken.None);
            Assert.True(((TestErrorLogger)tuple.Item4).Messages.Count == 1);
            Assert.True(((TestErrorLogger)tuple.Item4).Messages.TryGetValue(codefix.GetType().Name, out var message));
        }

        private async Task GetAddedFixesAsync(CodeFixProvider codefix)
        {
            var tuple = ServiceSetup(codefix);

            using var workspace = tuple.Item1;

            GetDocumentAndExtensionManager(tuple.Item2, workspace, out var document, out var extensionManager);
            var incrementalAnalyzer = (IIncrementalAnalyzerProvider)tuple.Item2;
            var analyzer = incrementalAnalyzer.CreateIncrementalAnalyzer(workspace);
            var reference = new MockAnalyzerReference(codefix);
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            document = project.Documents.Single();
            var fixes = await tuple.Item3.GetFixesAsync(document, TextSpan.FromBounds(0, 0), includeConfigurationFixes: true, cancellationToken: CancellationToken.None);

            Assert.True(extensionManager.IsDisabled(codefix));
            Assert.False(extensionManager.IsIgnored(codefix));
        }

        private async Task GetFirstDiagnosticWithFixAsync(CodeFixProvider codefix)
        {
            var tuple = ServiceSetup(codefix);
            using var workspace = tuple.Item1;
            GetDocumentAndExtensionManager(tuple.Item2, workspace, out var document, out var extensionManager);
            var unused = await tuple.Item3.GetMostSevereFixableDiagnosticAsync(document, TextSpan.FromBounds(0, 0), cancellationToken: CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(codefix));
            Assert.False(extensionManager.IsIgnored(codefix));
        }

        private static Tuple<TestWorkspace, TestDiagnosticAnalyzerService, CodeFixService, IErrorLoggerService> ServiceSetup(CodeFixProvider codefix)
        {
            var diagnosticService = new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            var fixers = SpecializedCollections.SingletonEnumerable(
                new Lazy<CodeFixProvider, CodeChangeProviderMetadata>(
                () => codefix,
                new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp)));
            var code = @"class Program { }";
            var workspace = TestWorkspace.CreateCSharp(code);
            var logger = SpecializedCollections.SingletonEnumerable(new Lazy<IErrorLoggerService>(() => new TestErrorLogger()));
            var errorLogger = logger.First().Value;
            var fixService = new CodeFixService(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                diagnosticService, logger, fixers, SpecializedCollections.EmptyEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>());
            return Tuple.Create(workspace, diagnosticService, fixService, errorLogger);
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
                return Task.CompletedTask;
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

            private static string ToLogFormat(Exception exception)
            {
                return exception.Message + Environment.NewLine + exception.StackTrace;
            }
        }
    }
}
