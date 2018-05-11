// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportWorkspaceService(typeof(ICodeStyleConfigureService)), Shared]
    internal class CSharpCodeStyleConfigureService : ICodeStyleConfigureService
    {
        private CSharpPackage _package;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeStyleConfigureService(SVsServiceProvider serviceProvider)
        {
            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            ErrorHandler.ThrowOnFailure(shell.LoadPackage(Guids.CSharpPackageId, out var package));
            _package = (CSharpPackage)package;
        }
        public void ShowFormattingOptionPage()
        {
            _package.ShowOptionPage(typeof(FormattingOptionPage));
        }
    }
}
