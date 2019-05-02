// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType
{
#pragma warning disable RS0032 // Test exports should not be discoverable
    [ExportWorkspaceService(typeof(IProjectManagementService), ServiceLayer.Default), Shared]
#pragma warning restore RS0032 // Test exports should not be discoverable
    internal class TestProjectManagementService : IProjectManagementService
    {
        private string _defaultNamespace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
