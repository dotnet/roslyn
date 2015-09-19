// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides the Visual Basic <see cref="CodeDomProvider"/> for use by designers and code generators.
    /// </summary>
    [ExportVsProfferedProjectService(typeof(CodeDomProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicCodeDomProvider : VBCodeProvider
    {
        [ImportingConstructor]
        public VisualBasicCodeDomProvider()
        {
        }

        [Import]
        public UnconfiguredProject UnconfiguredProject // Put ourselves in the UnconfiguredProject scope
        {
            get;
            set;
        }
    }
}
