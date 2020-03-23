// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MoveMembers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveMembers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.MoveMembers;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ExtractInterface
{
    internal class ExtractInterfaceTestState : IDisposable
    {
        private TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document ExtractFromDocument { get; }
        public AbstractMoveMembersService MoveMembersService { get; }
        public Solution OriginalSolution { get; }
        public string ErrorMessage { get; private set; }
        public NotificationSeverity ErrorSeverity { get; private set; }

        public static ExtractInterfaceTestState Create(string markup, string languageName, CompilationOptions compilationOptions)
        {
            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            var workspace = languageName == LanguageNames.CSharp
                ? TestWorkspace.CreateCSharp(markup, exportProvider: exportProvider, compilationOptions: compilationOptions as CSharpCompilationOptions)
                : TestWorkspace.CreateVisualBasic(markup, exportProvider: exportProvider, compilationOptions: compilationOptions);
            return new ExtractInterfaceTestState(workspace);
        }

        public ExtractInterfaceTestState(TestWorkspace workspace)
        {
            Workspace = workspace;

            OriginalSolution = Workspace.CurrentSolution;
            _testDocument = Workspace.Documents.SingleOrDefault(d => d.CursorPosition.HasValue);

            if (_testDocument == null)
            {
                throw new ArgumentException("markup does not contain a cursor position", nameof(workspace));
            }

            ExtractFromDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);
            MoveMembersService = ExtractFromDocument.GetRequiredLanguageService<AbstractMoveMembersService>();
        }

        public static readonly IExportProviderFactory ExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic
                    .WithPart(typeof(TestMoveMembersOptionService))
                    .WithPart(typeof(CSharpMoveMembersService))
                    .WithPart(typeof(VisualBasicMoveMembersService))
                    .WithPart(typeof(TestMoveMembersOptionService)));

        public TestMoveMembersOptionService TestMoveMembersOptionsService
        {
            get
            {
                return (TestMoveMembersOptionService)ExtractFromDocument.Project.Solution.Workspace.Services.GetRequiredService<IMoveMembersOptionService>();
            }
        }

        public Task<MoveMembersAnalysisResult> GetTypeAnalysisResultAsync(TypeDiscoveryRule typeDiscoveryRule)
        {
            return MoveMembersService.AnalyzeAsync(
                ExtractFromDocument,
                new TextSpan(_testDocument.CursorPosition.Value, 0),
                CancellationToken.None);
        }

        public async Task<MoveMembersResult> ExtractViaCommandAsync()
        {
            var analysis = await MoveMembersService.AnalyzeAsync(
                ExtractFromDocument,
                new TextSpan(_testDocument.CursorPosition.Value, 0),
                CancellationToken.None);

            if (analysis == null)
            {
                return new MoveMembersResult("Null analysis");
            }

            return await MoveMembersService.MoveMembersAsync(
                ExtractFromDocument,
                this.TestMoveMembersOptionsService.GetMoveMembersOptions(ExtractFromDocument, analysis, MoveMembersEntryPoint.ExtractInterface),
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
