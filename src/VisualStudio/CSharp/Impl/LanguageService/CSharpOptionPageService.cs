// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportWorkspaceService(typeof(IOptionPageService)), Shared]
    internal class CSharpOptionPageService : ForegroundThreadAffinitizedObject, IOptionPageService
    {
        private readonly CSharpPackage _package;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpOptionPageService(SVsServiceProvider serviceProvider)
        {
            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            ErrorHandler.ThrowOnFailure(shell.LoadPackage(Guids.CSharpPackageId, out var package));
            _package = (CSharpPackage)package;
        }

        public void ShowFormattingOptionPage()
        {
            AssertIsForeground();

            _package.ShowOptionPage(typeof(FormattingOptionPage));
        }
    }
}
