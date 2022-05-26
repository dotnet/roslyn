// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
            => string.Format("#r \"{0}\"", referenceName);

        protected override string CreateImport(string namespaceName)
            => string.Format("using {0};", namespaceName);
    }
}
