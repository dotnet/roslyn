// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;                
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.SyncNamespace
{
    public abstract class CSharpSyncNamespaceTestsBase : AbstractCodeActionTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpSyncNamespaceCodeRefactoringProvider();

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            return TestWorkspace.IsWorkspaceElement(initialMarkup)
                ? TestWorkspace.Create(initialMarkup)
                : TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);
        }

        protected string RootNamespace { get; set; } = string.Empty; 

        protected override async Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var testprojectManagementService = workspace.Services.GetService<IProjectManagementService>() as TestProjectManagementService;
            testprojectManagementService.SetDefaultNamespace(RootNamespace);
            return await base.GetCodeActionsWorkerAsync(workspace, parameters).ConfigureAwait(false);
        }      
    }
}
