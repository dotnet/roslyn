// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService
{
    public class CodeRefactoringServiceTest
    {
        [Fact]
        public async Task TestExceptionInComputeRefactorings()
        {
            await VerifyRefactoringDisabledAsync(new ErrorCases.ExceptionInCodeActions());
        }

        [Fact]
        public async Task TestExceptionInComputeRefactoringsAsync()
        {
            await VerifyRefactoringDisabledAsync(new ErrorCases.ExceptionInComputeRefactoringsAsync());
        }

        public async Task VerifyRefactoringDisabledAsync(CodeRefactoringProvider codeRefactoring)
        {
            var refactoringService = new CodeRefactorings.CodeRefactoringService(GetMetadata(codeRefactoring));
            using (var workspace = await TestWorkspaceFactory.CreateCSharpAsync(@"class Program {}"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                var document = project.Documents.Single();
                var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
                var result = await refactoringService.GetRefactoringsAsync(document, TextSpan.FromBounds(0, 0), CancellationToken.None);
                Assert.True(extensionManager.IsDisabled(codeRefactoring));
                Assert.False(extensionManager.IsIgnored(codeRefactoring));
            }
        }

        private static IEnumerable<Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>> GetMetadata(params CodeRefactoringProvider[] providers)
        {
            foreach (var provider in providers)
            {
                var providerCopy = provider;
                yield return new Lazy<CodeRefactoringProvider, CodeChangeProviderMetadata>(() => providerCopy, new CodeChangeProviderMetadata("Test", languages: LanguageNames.CSharp));
            }
        }
    }
}
