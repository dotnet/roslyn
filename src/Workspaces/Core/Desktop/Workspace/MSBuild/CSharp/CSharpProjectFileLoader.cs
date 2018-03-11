// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpProjectFileLoader : ProjectFileLoader
    {
        public CSharpProjectFileLoader()
        {
        }

        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        protected override ProjectFile CreateProjectFile(LoadedProjectInfo info)
        {
            return new CSharpProjectFile(this, info.Project, info.ErrorMessage);
        }
    }
}
