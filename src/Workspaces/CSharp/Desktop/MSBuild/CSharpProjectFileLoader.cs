// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpProjectFileLoader : ProjectFileLoader
    {
        private readonly HostWorkspaceServices _workspaceServices;

        public CSharpProjectFileLoader(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;
        }

        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        protected override ProjectFile CreateProjectFile(MSB.Evaluation.Project loadedProject)
        {
            return new CSharpProjectFile(this, loadedProject, _workspaceServices.GetService<IMetadataService>(), _workspaceServices.GetService<IAnalyzerService>());
        }
    }
}
