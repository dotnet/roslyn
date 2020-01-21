// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// An <see cref="IOptionPersister"/> that syncs core language settings against the settings that exist for all languages
    /// in Visual Studio and whose backing store is provided by the shell. This includes things like default tab size, tabs vs. spaces, etc.
    /// </summary>
    [Export(typeof(IOptionPersister))]
    internal sealed class LanguageSettingsPersister : ForegroundThreadAffinitizedObject, IVsTextManagerEvents4, IOptionPersister
    {
        private readonly IVsTextManager4 _textManager;
        private readonly IGlobalOptionService _optionService;

        private readonly ComEventSink _textManagerEvents2Sink;

        /// <summary>
        /// The mapping between language names and Visual Studio language service GUIDs.
        /// </summary>
        /// <remarks>
        /// This is a map between string and <see cref="Tuple{Guid}"/> rather than just to <see cref="Guid"/>
        /// to avoid a bunch of JIT during startup. Generics of value types like <see cref="Guid"/> will have to JIT
        /// but the ngen image will exist for the basic map between two reference types, since those are reused.</remarks>
        private readonly IBidirectionalMap<string, Tuple<Guid>> _languageMap;

        /// <remarks>
        /// We make sure this code is from the UI by asking for all serializers on the UI thread in <see cref="HACK_AbstractCreateServicesOnUiThread"/>.
        /// </remarks>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageSettingsPersister(
            IThreadingContext threadingContext,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IGlobalOptionService optionService)
            : base(threadingContext, assertIsForeground: true)
        {
            _textManager = (IVsTextManager4)serviceProvider.GetService(typeof(SVsTextManager));
            _optionService = optionService;

            // TODO: make this configurable
            _languageMap = BidirectionalMap<string, Tuple<Guid>>.Empty.Add(LanguageNames.CSharp, Tuple.Create(Guids.CSharpLanguageServiceId))
                                                               .Add(LanguageNames.VisualBasic, Tuple.Create(Guids.VisualBasicLanguageServiceId))
                                                               .Add("TypeScript", Tuple.Create(new Guid("4a0dddb5-7a95-4fbf-97cc-616d07737a77")))
                                                               .Add("F#", Tuple.Create(new Guid("BC6DD5A5-D4D6-4dab-A00D-A51242DBAF1B")))
                                                               .Add("Xaml", Tuple.Create(new Guid("CD53C9A1-6BC2-412B-BE36-CC715ED8DD41")));

            foreach (var languageGuid in _languageMap.Values)
            {
                var languagePreferences = new LANGPREFERENCES3[1];
                languagePreferences[0].guidLang = languageGuid.Item1;

                // The function can potentially fail if that language service isn't installed
                if (ErrorHandler.Succeeded(_textManager.GetUserPreferences4(pViewPrefs: null, pLangPrefs: languagePreferences, pColorPrefs: null)))
                {
                    RefreshLanguageSettings(languagePreferences);
                }
            }

            _textManagerEvents2Sink = ComEventSink.Advise<IVsTextManagerEvents4>(_textManager, this);
        }

        private readonly IOption[] _supportedOptions = new IOption[]
        {
            FormattingOptions.UseTabs,
            FormattingOptions.TabSize,
            FormattingOptions.SmartIndent,
            FormattingOptions.IndentationSize,
            CompletionOptions.HideAdvancedMembers,
            CompletionOptions.TriggerOnTyping,
            SignatureHelpOptions.ShowSignatureHelp,
            NavigationBarOptions.ShowNavigationBar,
            BraceCompletionOptions.Enable,
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
                foreach (var option in _supportedOptions)
                {
                    var keyWithLanguage = new OptionKey(option, languageName);
                    var newValue = GetValueForOption(option, langPrefs[0]);

                    _optionService.RefreshOption(keyWithLanguage, newValue);
                }
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
            else if (option == CompletionOptions.HideAdvancedMembers)
            {
                return languagePreference.fHideAdvancedAutoListMembers != 0;
            }
            else if (option == CompletionOptions.TriggerOnTyping)
            {
                return languagePreference.fAutoListMembers != 0;
            }
            else if (option == SignatureHelpOptions.ShowSignatureHelp)
            {
                return languagePreference.fAutoListParams != 0;
            }
            else if (option == NavigationBarOptions.ShowNavigationBar)
            {
                return languagePreference.fDropdownBar != 0;
            }
            else if (option == BraceCompletionOptions.Enable)
            {
                return languagePreference.fBraceCompletion != 0;
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
            else if (option == CompletionOptions.HideAdvancedMembers)
            {
                languagePreference.fHideAdvancedAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == CompletionOptions.TriggerOnTyping)
            {
                languagePreference.fAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == SignatureHelpOptions.ShowSignatureHelp)
            {
                languagePreference.fAutoListParams = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == NavigationBarOptions.ShowNavigationBar)
            {
                languagePreference.fDropdownBar = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (option == BraceCompletionOptions.Enable)
            {
                languagePreference.fBraceCompletion = Convert.ToUInt32((bool)value ? 1 : 0);
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

            Contract.ThrowIfTrue(_supportedOptions.Contains(optionKey.Option) && _languageMap.ContainsKey(optionKey.Language));

            value = null;
            return false;
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            if (!_supportedOptions.Contains(optionKey.Option))
            {
                value = null;
                return false;
            }

            if (!_languageMap.TryGetValue(optionKey.Language, out var languageServiceGuid))
            {
                value = null;
                return false;
            }

            var languagePreferences = new LANGPREFERENCES3[1];
            languagePreferences[0].guidLang = languageServiceGuid.Item1;
            Marshal.ThrowExceptionForHR(_textManager.GetUserPreferences4(null, languagePreferences, null));

            SetValueForOption(optionKey.Option, ref languagePreferences[0], value);
            SetUserPreferencesMaybeAsync(languagePreferences);

            // Even if we didn't call back, say we completed the persist
            return true;
        }

        private void SetUserPreferencesMaybeAsync(LANGPREFERENCES3[] languagePreferences)
        {
            if (IsForeground())
            {
                Marshal.ThrowExceptionForHR(_textManager.SetUserPreferences4(pViewPrefs: null, pLangPrefs: languagePreferences, pColorPrefs: null));
            }
            else
            {
                Task.Factory.StartNew(
                    async () =>
                    {
                        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                        this.SetUserPreferencesMaybeAsync(languagePreferences);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default).Unwrap();
            }
        }
    }
}
