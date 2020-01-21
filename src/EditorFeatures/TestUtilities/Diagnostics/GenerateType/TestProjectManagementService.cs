// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType
{
    [ExportWorkspaceService(typeof(IProjectManagementService), ServiceLayer.Default), Shared]
    internal class TestProjectManagementService : IProjectManagementService
    {
        private string _defaultNamespace;

        [ImportingConstructor]
        public TestProjectManagementService()
        {
        }

        public IList<string> GetFolders(ProjectId projectId, Workspace workspace)
        {
            return null;
        }

        public string GetDefaultNamespace(Project project, Workspace workspace)
        {
            return _defaultNamespace;
        }

        public void SetDefaultNamespace(string defaultNamespace)
        {
            _defaultNamespace = defaultNamespace;
        }
    }
}
