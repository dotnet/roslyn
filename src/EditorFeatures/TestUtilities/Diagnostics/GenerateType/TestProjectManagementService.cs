// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestProjectManagementService()
        {
        }

        public IList<string> GetFolders(ProjectId projectId, Workspace workspace)
            => null;

        public string GetDefaultNamespace(Project project, Workspace workspace)
            => _defaultNamespace;

        public void SetDefaultNamespace(string defaultNamespace)
            => _defaultNamespace = defaultNamespace;
    }
}
