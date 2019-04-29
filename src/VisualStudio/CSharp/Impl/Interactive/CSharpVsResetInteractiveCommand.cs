// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Services.Interactive;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [ExportInteractive(typeof(IResetInteractiveCommand), ContentTypeNames.CSharpContentType)]
    internal sealed class CSharpVsResetInteractiveCommand
        : AbstractResetInteractiveCommand
    {
        [ImportingConstructor]
        public CSharpVsResetInteractiveCommand(
            VisualStudioWorkspace workspace,
            CSharpVsInteractiveWindowProvider interactiveWindowProvider,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(workspace, interactiveWindowProvider, serviceProvider)
        {
        }

        protected override string LanguageName
        {
            get { return "C#"; }
        }

        protected override string CreateReference(string referenceName)
        {
            return string.Format("#r \"{0}\"", referenceName);
        }

        protected override string CreateImport(string namespaceName)
        {
            return string.Format("using {0};", namespaceName);
        }
    }
}
