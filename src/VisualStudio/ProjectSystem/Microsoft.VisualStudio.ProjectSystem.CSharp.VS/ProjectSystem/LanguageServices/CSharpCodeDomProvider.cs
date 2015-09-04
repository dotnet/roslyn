// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides the C# <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    internal class CSharpCodeDomProvider
    {
        [ImportingConstructor]
        public CSharpCodeDomProvider()
        {
        }

        [Import]
        protected UnconfiguredProject UnconfiguredProject
        {
            get;
            private set;
        }

        [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
        [AppliesTo(ProjectCapability.CSharp)]
        private CodeDomProvider CodeDomProviderService
        {
            get { return CodeDomProvider.CreateProvider("CSharp"); }
        }
    }
}
