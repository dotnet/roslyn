// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExportLanguageService(typeof(IOptionPageService), LanguageNames.CSharp), Shared]
    internal class CSharpOptionPageService : ForegroundThreadAffinitizedObject, IOptionPageService
    {
        private readonly CSharpPackage _package;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpOptionPageService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
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
