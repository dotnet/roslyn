// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class GlobalOptions_OutOfProc : OutOfProcComponent
    {
        private readonly GlobalOptions_InProc _inProc;

        public GlobalOptions_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<GlobalOptions_InProc>(visualStudioInstance);
        }

        public bool IsPrettyListingOn(string languageName)
            => _inProc.IsPrettyListingOn(languageName);

        public void SetPrettyListing(string languageName, bool value)
            => _inProc.SetPrettyListing(languageName, value);

        public void ResetOptions()
            => _inProc.ResetOptions();

        public void SetImportCompletionOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, value);
            SetGlobalOption(WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, value);
        }

        public void SetEnableDecompilationOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.MetadataAsSourceOptions_NavigateToDecompiledSources, language: null, value);
        }

        public void SetArgumentCompletionSnippetsOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets, LanguageNames.CSharp, value);
            SetGlobalOption(WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, value);
        }

        public void SetTriggerCompletionInArgumentLists(bool value)
            => SetGlobalOption(WellKnownGlobalOption.CompletionOptions_TriggerInArgumentLists, LanguageNames.CSharp, value);

        public void SetGlobalOption(WellKnownGlobalOption option, string? language, object? value)
            => _inProc.SetGlobalOption(option, language, value);
    }
}
