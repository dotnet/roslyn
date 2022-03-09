// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// An <see cref="IOptionPersister"/> that syncs core language settings against the settings that exist for all languages
    /// in Visual Studio and whose backing store is provided by the shell. This includes things like default tab size, tabs vs. spaces, etc.
    /// 
    /// TODO: replace with free-threaded impl: https://github.com/dotnet/roslyn/issues/56815
    /// </summary>
    internal sealed class LanguageSettingsPersister : ForegroundThreadAffinitizedObject, IVsTextManagerEvents4, IOptionPersister
    {
        private readonly IVsTextManager4 _textManager;
        private readonly IGlobalOptionService _globalOptions;

#pragma warning disable IDE0052 // Remove unread private members - https://github.com/dotnet/roslyn/issues/46167
        private readonly ComEventSink _textManagerEvents2Sink;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// The mapping between language names and Visual Studio language service GUIDs.
        /// </summary>
        /// <remarks>
        /// This is a map between string and <see cref="Tuple{Guid}"/> rather than just to <see cref="Guid"/>
        /// to avoid a bunch of JIT during startup. Generics of value types like <see cref="Guid"/> will have to JIT
        /// but the ngen image will exist for the basic map between two reference types, since those are reused.</remarks>
        private readonly IBidirectionalMap<string, Tuple<Guid>> _languageMap;

        /// <remarks>
        /// We make sure this code is from the UI by asking for all <see cref="IOptionPersister"/> in <see cref="RoslynPackage.InitializeAsync"/>
        /// </remarks>
        public LanguageSettingsPersister(
            IThreadingContext threadingContext,
            IVsTextManager4 textManager,
            IGlobalOptionService globalOptions)
            : base(threadingContext, assertIsForeground: true)
        {
            _textManager = textManager;
            _globalOptions = globalOptions;

            var languageMap = BidirectionalMap<string, Tuple<Guid>>.Empty;

            InitializeSettingsForLanguage(LanguageNames.CSharp, Guids.CSharpLanguageServiceId);
            InitializeSettingsForLanguage(LanguageNames.VisualBasic, Guids.VisualBasicLanguageServiceId);
            InitializeSettingsForLanguage(InternalLanguageNames.TypeScript, new Guid("4a0dddb5-7a95-4fbf-97cc-616d07737a77"));
            InitializeSettingsForLanguage("F#", new Guid("BC6DD5A5-D4D6-4dab-A00D-A51242DBAF1B"));
            InitializeSettingsForLanguage("Xaml", new Guid("CD53C9A1-6BC2-412B-BE36-CC715ED8DD41"));

            void InitializeSettingsForLanguage(string languageName, Guid languageGuid)
            {
                var languagePreferences = new LANGPREFERENCES3[1];
                languagePreferences[0].guidLang = languageGuid;

                // The function can potentially fail if that language service isn't installed
                if (ErrorHandler.Succeeded(_textManager.GetUserPreferences4(pViewPrefs: null, pLangPrefs: languagePreferences, pColorPrefs: null)))
                {
                    RefreshLanguageSettings(languagePreferences, languageName);
                    languageMap = languageMap.Add(languageName, Tuple.Create(languageGuid));
                }
                else
                {
                    FatalError.ReportWithDumpAndCatch(new InvalidOperationException("GetUserPreferences4 failed"), ErrorSeverity.Diagnostic);
                }
            }

            _languageMap = languageMap;
            _textManagerEvents2Sink = ComEventSink.Advise<IVsTextManagerEvents4>(_textManager, this);
        }

        private readonly IOption[] _supportedOptions = new IOption[]
        {
            FormattingOptions.UseTabs,
            FormattingOptions.TabSize,
            FormattingOptions.SmartIndent,
            FormattingOptions.IndentationSize,
            CompletionOptionsStorage.HideAdvancedMembers,
            CompletionOptionsStorage.TriggerOnTyping,
            SignatureHelpViewOptions.ShowSignatureHelp,
            NavigationBarViewOptions.ShowNavigationBar
        };

        int IVsTextManagerEvents4.OnUserPreferencesChanged4(
            VIEWPREFERENCES3[] viewPrefs,
            LANGPREFERENCES3[] langPrefs,
            FONTCOLORPREFERENCES2[] colorPrefs)
        {
            if (langPrefs != null)
            {
                RefreshLanguageSettings(langPrefs);
            }

            return VSConstants.S_OK;
        }

        private void RefreshLanguageSettings(LANGPREFERENCES3[] langPrefs)
        {
            this.AssertIsForeground();
            if (_languageMap.TryGetKey(Tuple.Create(langPrefs[0].guidLang), out var languageName))
            {
                RefreshLanguageSettings(langPrefs, languageName);
            }
        }

        private void RefreshLanguageSettings(LANGPREFERENCES3[] langPrefs, string languageName)
        {
            this.AssertIsForeground();

            foreach (var option in _supportedOptions)
            {
                var keyWithLanguage = new OptionKey(option, languageName);
                var newValue = GetValueForOption(option, langPrefs[0]);

                _globalOptions.RefreshOption(keyWithLanguage, newValue);
            }
        }

        private static object GetValueForOption(IOption option, LANGPREFERENCES3 languagePreference)
        {
            if (option == FormattingOptions.UseTabs)
            {
                return languagePreference.fInsertTabs != 0;
            }
            else if (option == FormattingOptions.TabSize)
            {
                return Convert.ToInt32(languagePreference.uTabSize);
            }
            else if (option == FormattingOptions.IndentationSize)
            {
                return Convert.ToInt32(languagePreference.uIndentSize);
            }
            else if (option == FormattingOptions.SmartIndent)
            {
                switch (languagePreference.IndentStyle)
                {
                    case vsIndentStyle.vsIndentStyleNone:
                        return FormattingOptions.IndentStyle.None;
                    case vsIndentStyle.vsIndentStyleDefault:
                        return FormattingOptions.IndentStyle.Block;
                    default:
                        return FormattingOptions.IndentStyle.Smart;
                }
            }
            else if (option == CompletionOptionsStorage.HideAdvancedMembers)
            {
                return languagePreference.fHideAdvancedAutoListMembers != 0;
            }
            else if (option == CompletionOptionsStorage.TriggerOnTyping)
            {
                return languagePreference.fAutoListMembers != 0;
            }
            else if (option == SignatureHelpViewOptions.ShowSignatureHelp)
            {
                return languagePreference.fAutoListParams != 0;
            }
            else if (option == NavigationBarViewOptions.ShowNavigationBar)
            {
                return languagePreference.fDropdownBar != 0;
            }
            else
            {
                throw new ArgumentException("Unexpected option.", nameof(option));
            }
        }

        private static void SetValueForOption(IOption option, ref LANGPREFERENCES3 languagePreference, object value)
        {
            if (option == FormattingOptions.UseTabs)
            {
                languagePreference.fInsertTabs = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == FormattingOptions.TabSize)
            {
                languagePreference.uTabSize = Convert.ToUInt32(value);
            }
            else if (option == FormattingOptions.IndentationSize)
            {
                languagePreference.uIndentSize = Convert.ToUInt32(value);
            }
            else if (option == FormattingOptions.SmartIndent)
            {
                switch ((FormattingOptions.IndentStyle)value)
                {
                    case FormattingOptions.IndentStyle.None:
                        languagePreference.IndentStyle = vsIndentStyle.vsIndentStyleNone;
                        break;
                    case FormattingOptions.IndentStyle.Block:
                        languagePreference.IndentStyle = vsIndentStyle.vsIndentStyleDefault;
                        break;
                    default:
                        languagePreference.IndentStyle = vsIndentStyle.vsIndentStyleSmart;
                        break;
                }
            }
            else if (option == CompletionOptionsStorage.HideAdvancedMembers)
            {
                languagePreference.fHideAdvancedAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == CompletionOptionsStorage.TriggerOnTyping)
            {
                languagePreference.fAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == SignatureHelpViewOptions.ShowSignatureHelp)
            {
                languagePreference.fAutoListParams = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == NavigationBarViewOptions.ShowNavigationBar)
            {
                languagePreference.fDropdownBar = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else
            {
                throw new ArgumentException("Unexpected option.", nameof(option));
            }
        }

        public bool TryFetch(OptionKey optionKey, out object value)
        {
            // This particular serializer is a bit strange, since we have to initially read things out on the UI thread.
            // Therefore, we refresh the values in the constructor, meaning that this should never get called for our values.

            if (_supportedOptions.Contains(optionKey.Option) && _languageMap.ContainsKey(optionKey.Language))
            {
                FatalError.ReportWithDumpAndCatch(new InvalidOperationException("Unexpected call to " + nameof(LanguageSettingsPersister) + "." + nameof(TryFetch)), ErrorSeverity.Diagnostic);
            }

            value = null;
            return false;
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            if (!_supportedOptions.Contains(optionKey.Option))
            {
                return false;
            }

            if (!_languageMap.TryGetValue(optionKey.Language, out var languageServiceGuid))
            {
                return false;
            }

            var languagePreferences = new LANGPREFERENCES3[1];
            languagePreferences[0].guidLang = languageServiceGuid.Item1;
            Marshal.ThrowExceptionForHR(_textManager.GetUserPreferences4(null, languagePreferences, null));

            SetValueForOption(optionKey.Option, ref languagePreferences[0], value);
            _ = SetUserPreferencesMaybeAsync(languagePreferences);

            // Even if we didn't call back, say we completed the persist
            return true;
        }

        private async Task SetUserPreferencesMaybeAsync(LANGPREFERENCES3[] languagePreferences)
        {
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
            Marshal.ThrowExceptionForHR(_textManager.SetUserPreferences4(pViewPrefs: null, pLangPrefs: languagePreferences, pColorPrefs: null));
        }
    }
}
