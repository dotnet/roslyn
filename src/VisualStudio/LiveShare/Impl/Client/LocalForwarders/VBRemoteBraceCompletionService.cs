// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using System.Composition;
using System;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportLanguageServiceFactory(typeof(IEditorBraceCompletionSessionFactory), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspBraceCompletionServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VBLspBraceCompletionServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => languageServices.GetOriginalLanguageService<IEditorBraceCompletionSessionFactory>();
    }
}
