// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType
{
    internal sealed class GenerateTypeTestState
    {
        public static List<string> FixIds = new List<string>(new[] { "CS0246", "CS0234", "CS0103", "BC30002", "BC30451", "BC30456" });
        private readonly EditorTestHostDocument _testDocument;
        public EditorTestWorkspace Workspace { get; }
        public Document InvocationDocument { get; }
        public Document ExistingDocument { get; }
        public Project ProjectToBeModified { get; }
        public Project TriggeredProject { get; }
        public string TypeName { get; }

        public GenerateTypeTestState(
            EditorTestWorkspace workspace,
            string projectToBeModified,
            string typeName,
            string existingFileName)
        {
            Workspace = workspace;
            _testDocument = Workspace.Documents.SingleOrDefault(d => d.CursorPosition.HasValue);
            Contract.ThrowIfNull(_testDocument, "markup does not contain a cursor position");

            TriggeredProject = Workspace.CurrentSolution.GetProject(_testDocument.Project.Id);

            if (projectToBeModified == null)
            {
                // Select the project from which the Codefix was triggered
                ProjectToBeModified = Workspace.CurrentSolution.GetProject(_testDocument.Project.Id);
            }
            else
            {
                ProjectToBeModified = Workspace.CurrentSolution.Projects.FirstOrDefault(proj => proj.Name.Equals(projectToBeModified));
                Contract.ThrowIfNull(ProjectToBeModified, "Project with the given name does not exist");
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
                return (TestGenerateTypeOptionsService)InvocationDocument.Project.Solution.Services.GetRequiredService<IGenerateTypeOptionsService>();
            }
        }

        public TestProjectManagementService TestProjectManagementService
        {
            get
            {
                return (TestProjectManagementService)InvocationDocument.Project.Solution.Services.GetService<IProjectManagementService>();
            }
        }
    }
}
