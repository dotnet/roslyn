// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.ProjectManagement;

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

        public static GenerateTypeTestState Create(
            string initial,
            string projectToBeModified,
            string typeName,
            string existingFileName,
            string languageName)
        {
            var workspace = TestWorkspace.IsWorkspaceElement(initial)
                ? TestWorkspace.Create(initial)
                : languageName == LanguageNames.CSharp
                  ? TestWorkspace.CreateCSharp(initial)
                  : TestWorkspace.CreateVisualBasic(initial);

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

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.Dispose();
            }
        }
    }
}
