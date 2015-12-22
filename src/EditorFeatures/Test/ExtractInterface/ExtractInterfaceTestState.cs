// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractInterface;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.ExtractInterface;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
{
    internal class ExtractInterfaceTestState : IDisposable
    {
        private TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document ExtractFromDocument { get; }
        public AbstractExtractInterfaceService ExtractInterfaceService { get; }
        public Solution OriginalSolution { get; }
        public string ErrorMessage { get; private set; }
        public NotificationSeverity ErrorSeverity { get; private set; }

        public static async Task<ExtractInterfaceTestState> CreateAsync(string markup, string languageName, CompilationOptions compilationOptions)
        {
            var workspace = languageName == LanguageNames.CSharp
                ? await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(markup, exportProvider: ExportProvider, compilationOptions: compilationOptions as CSharpCompilationOptions)
                : await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(markup, exportProvider: ExportProvider, compilationOptions: compilationOptions);
            return new ExtractInterfaceTestState(workspace);
        }

        public ExtractInterfaceTestState(TestWorkspace workspace)
        {
            Workspace = workspace;

            OriginalSolution = Workspace.CurrentSolution;
            _testDocument = Workspace.Documents.SingleOrDefault(d => d.CursorPosition.HasValue);

            if (_testDocument == null)
            {
                throw new ArgumentException("markup does not contain a cursor position", "workspace");
            }

            ExtractFromDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);
            ExtractInterfaceService = ExtractFromDocument.GetLanguageService<AbstractExtractInterfaceService>();
        }

        public static readonly ExportProvider ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                .WithPart(typeof(TestExtractInterfaceOptionsService))
                .WithPart(typeof(CSharpExtractInterfaceService))
                .WithPart(typeof(VisualBasicExtractInterfaceService)));

        public TestExtractInterfaceOptionsService TestExtractInterfaceOptionsService
        {
            get
            {
                return (TestExtractInterfaceOptionsService)ExtractFromDocument.Project.Solution.Workspace.Services.GetService<IExtractInterfaceOptionsService>();
            }
        }

        public Task<ExtractInterfaceTypeAnalysisResult> GetTypeAnalysisResultAsync(TypeDiscoveryRule typeDiscoveryRule)
        {
            return ExtractInterfaceService.AnalyzeTypeAtPositionAsync(
                ExtractFromDocument,
                _testDocument.CursorPosition.Value,
                typeDiscoveryRule,
                CancellationToken.None);
        }

        public ExtractInterfaceResult ExtractViaCommand()
        {
            return ExtractInterfaceService.ExtractInterface(
                ExtractFromDocument,
                _testDocument.CursorPosition.Value,
                (errorMessage, severity) =>
                {
                    this.ErrorMessage = errorMessage;
                    this.ErrorSeverity = severity;
                },
                CancellationToken.None);
        }

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.Dispose();
            }
        }
    }
}
