// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(IProjectFileLoader), LanguageNames.CSharp)]
    internal partial class CSharpProjectFileLoader : ProjectFileLoader
    {
        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        private static readonly Guid projectTypeGuid = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");

        public override bool IsProjectTypeGuid(Guid guid)
        {
            return guid == projectTypeGuid;
        }

        public override bool IsProjectFileExtension(string fileExtension)
        {
            return string.Equals("csproj", fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        protected override ProjectFile CreateProjectFile(MSB.Evaluation.Project loadedProject)
        {
            return new CSharpProjectFile(this, loadedProject);
        }
    }
}