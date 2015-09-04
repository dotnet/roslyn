// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides the Visual Basic <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    internal class VisualBasicCodeDomProvider
    {
        [ImportingConstructor]
        public VisualBasicCodeDomProvider()
        {
        }

        [Import]
        protected UnconfiguredProject UnconfiguredProject
        {
            get;
            private set;
        }

        [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
        [AppliesTo(ProjectCapability.VisualBasic)]
        private CodeDomProvider CodeDomProviderService
        {
            get { return CodeDomProvider.CreateProvider("VB"); }
        }
    }
}
