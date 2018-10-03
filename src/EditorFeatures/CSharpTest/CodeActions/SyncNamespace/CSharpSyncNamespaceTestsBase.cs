// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;                
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ProjectManagement;
using Roslyn.Utilities;

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

        protected string ProjectRootPath
            => PathUtilities.IsUnixLikePlatform 
            ? @"/ProjectA/" 
            : @"C:\ProjectA\";

        protected string ProjectFilePath
            => PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, "ProjectA.csproj");

        protected (string folder, string filePath) CreateDocumentFilePath(string[] folder, string fileName = "DocumentA.cs")
        {
            if (folder == null || folder.Length == 0)
            {
                return (string.Empty, PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, fileName));
            }
            else
            {
                var folderPath = string.Join(PathUtilities.DirectorySeparatorStr, folder);
                var relativePath = PathUtilities.CombinePossiblyRelativeAndRelativePaths(folderPath, fileName);
                return (folderPath, PathUtilities.CombineAbsoluteAndRelativePaths(ProjectRootPath, relativePath));
            }
        }        
    }
}
