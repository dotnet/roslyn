// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal sealed class GlobalOptions_InProc : InProcComponent
    {
        private readonly IGlobalOptionService _globalOptions;

        public GlobalOptions_InProc()
        {
            _globalOptions = GetComponentModelService<IGlobalOptionService>();
        }

        public bool IsPrettyListingOn(string languageName)
            => _globalOptions.GetOption(FeatureOnOffOptions.PrettyListing, languageName);

        public void SetPrettyListing(string languageName, bool value)
            => InvokeOnUIThread(_ => _globalOptions.SetGlobalOption(FeatureOnOffOptions.PrettyListing, languageName, value));

        public void SetFileScopedNamespaces(bool value)
            => InvokeOnUIThread(_ => _globalOptions.SetGlobalOption(Microsoft.CodeAnalysis.CSharp.CodeStyle.CSharpCodeStyleOptions.NamespaceDeclarations,
                new CodeStyleOption2<NamespaceDeclarationPreference>(value ? NamespaceDeclarationPreference.FileScoped : NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Suggestion)));

        public void SetGlobalOption(WellKnownGlobalOption option, string? language, object? value)
            => InvokeOnUIThread(_ => _globalOptions.SetGlobalOption(option.GetKey(language), value));

        /// <summary>
        /// Reset options that are manipulated by integration tests back to their default values.
        /// </summary>
        public void ResetOptions()
        {
            SetFileScopedNamespaces(false);

            ResetOption(CompletionViewOptions.EnableArgumentCompletionSnippets);
            ResetOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
            return;

            // Local function
            void ResetOption(IOption2 option)
            {
                if (option.IsPerLanguage)
                {
                    _globalOptions.SetGlobalOption(new OptionKey2(option, LanguageNames.CSharp), option.DefaultValue);
                    _globalOptions.SetGlobalOption(new OptionKey2(option, LanguageNames.VisualBasic), option.DefaultValue);
                }
                else
                {
                    _globalOptions.SetGlobalOption(new OptionKey2(option, language: null), option.DefaultValue);
                }
            }
        }
    }
}
