// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CaseCorrection;
using Microsoft.CodeAnalysis.CSharp.GenerateType;
using Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.Editor.Implementation.CodeActions;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.CaseCorrection;
using Microsoft.CodeAnalysis.VisualBasic.GenerateType;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType
{
    internal sealed class GenerateTypeTestState : IDisposable
    {
        public static List<string> FixIds = new List<string>(new[] { "CS0246", "CS0234", "CS0103", "BC30002", "BC30451", "BC30456" });
        private TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document InvocationDocument { get; }
        public Document ExistingDocument { get; }
        public Project ProjectToBeModified { get; }
        public Project TriggeredProject { get; }
        public string TypeName { get; }

        public static async Task<GenerateTypeTestState> CreateAsync(
            string initial,
            bool isLine,
            string projectToBeModified,
            string typeName,
            string existingFileName,
            string languageName)
        {
            var workspace = languageName == LanguageNames.CSharp
                  ? isLine ? await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(initial, exportProvider: s_exportProvider) : await TestWorkspaceFactory.CreateWorkspaceAsync(initial, exportProvider: s_exportProvider)
                  : isLine ? await TestWorkspaceFactory.CreateVisualBasicWorkspaceAsync(initial, exportProvider: s_exportProvider) : await TestWorkspaceFactory.CreateWorkspaceAsync(initial, exportProvider: s_exportProvider);

            return new GenerateTypeTestState(projectToBeModified, typeName, existingFileName, workspace);
        }

        private GenerateTypeTestState(string projectToBeModified, string typeName, string existingFileName, TestWorkspace testWorkspace)
        {
            Workspace = testWorkspace;
            _testDocument = Workspace.Documents.SingleOrDefault(d => d.CursorPosition.HasValue);

            if (_testDocument == null)
            {
                throw new ArgumentException("markup does not contain a cursor position", "workspace");
            }

            TriggeredProject = Workspace.CurrentSolution.GetProject(_testDocument.Project.Id);

            if (projectToBeModified == null)
            {
                // Select the project from which the Codefix was triggered
                ProjectToBeModified = Workspace.CurrentSolution.GetProject(_testDocument.Project.Id);
            }
            else
            {
                ProjectToBeModified = Workspace.CurrentSolution.Projects.FirstOrDefault(proj => proj.Name.Equals(projectToBeModified));
                if (ProjectToBeModified == null)
                {
                    throw new ArgumentException("Project with the given name does not exist", "workspace");
                }
            }

            InvocationDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);
            if (projectToBeModified == null && existingFileName == null)
            {
                ExistingDocument = InvocationDocument;
            }
            else if (existingFileName != null)
            {
                ExistingDocument = ProjectToBeModified.Documents.FirstOrDefault(doc => doc.Name.Equals(existingFileName));
            }

            TypeName = typeName;
        }

        public TestGenerateTypeOptionsService TestGenerateTypeOptionsService
        {
            get
            {
                return (TestGenerateTypeOptionsService)InvocationDocument.Project.Solution.Workspace.Services.GetService<IGenerateTypeOptionsService>();
            }
        }

        public TestProjectManagementService TestProjectManagementService
        {
            get
            {
                return (TestProjectManagementService)InvocationDocument.Project.Solution.Workspace.Services.GetService<IProjectManagementService>();
            }
        }

        private static readonly ExportProvider s_exportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                typeof(TestGenerateTypeOptionsService),
                typeof(TestProjectManagementService),
                typeof(CSharpGenerateTypeService),
                typeof(VisualBasicGenerateTypeService),
                typeof(CSharpCaseCorrectionService),
                typeof(VisualBasicCaseCorrectionServiceFactory),
                typeof(CSharpTypeInferenceService),
                typeof(VisualBasicTypeInferenceService),
                typeof(CodeActionEditHandlerService),
                typeof(PreviewFactoryService),
                typeof(InlineRenameService),
                typeof(TextBufferAssociatedViewService),
                typeof(IProjectionBufferFactoryServiceExtensions)));

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.Dispose();
            }
        }
    }
}
