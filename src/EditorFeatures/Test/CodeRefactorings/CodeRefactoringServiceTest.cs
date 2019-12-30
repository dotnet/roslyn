// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService
{
    [UseExportProvider]
    public class CodeRefactoringServiceTest
    {
        [Fact]
        public async Task TestExceptionInComputeRefactorings()
        {
            await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInCodeActions>();
        }

        [Fact]
        public async Task TestExceptionInComputeRefactoringsAsync()
        {
            await VerifyRefactoringDisabledAsync<ErrorCases.ExceptionInComputeRefactoringsAsync>();
        }

        private async Task VerifyRefactoringDisabledAsync<T>()
            where T : CodeRefactoringProvider
        {
            var exportProvider = ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(T))).CreateExportProvider();
            using var workspace = TestWorkspace.CreateCSharp(@"class Program {}", exportProvider: exportProvider);
            var refactoringService = workspace.GetService<ICodeRefactoringService>();
            var codeRefactoring = exportProvider.GetExportedValues<CodeRefactoringProvider>().OfType<T>().Single();

            var project = workspace.CurrentSolution.Projects.Single();
            var document = project.Documents.Single();
            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
            var result = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(codeRefactoring));
            Assert.False(extensionManager.IsIgnored(codeRefactoring));
        }
    }
}
