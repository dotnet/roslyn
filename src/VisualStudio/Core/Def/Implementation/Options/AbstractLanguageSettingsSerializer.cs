// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractLanguageSettingsSerializer : ForegroundThreadAffinitizedObject, IVsTextManagerEvents4, IOptionSerializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _languageName;

        private readonly IVsTextManager4 _textManager;
        private readonly IComEventSink _textManagerEvents2Sink;
        private LANGPREFERENCES3 _languageSetting;

        // We make sure this code is from the UI by asking for the optionservice in the initialize() in AbstractPackage`2
        public AbstractLanguageSettingsSerializer(Guid languageServiceguid, string languageName, IServiceProvider serviceProvider)
            : base(assertIsForeground: true)
        {
            _serviceProvider = serviceProvider;
            _languageName = languageName;

            _textManager = (IVsTextManager4)serviceProvider.GetService(typeof(SVsTextManager));

            var langPrefs = new LANGPREFERENCES3[1];
            langPrefs[0].guidLang = languageServiceguid;
            Marshal.ThrowExceptionForHR(_textManager.GetUserPreferences4(pViewPrefs: null, pLangPrefs: langPrefs, pColorPrefs: null));
            _languageSetting = langPrefs[0];
            _textManagerEvents2Sink = ComEventSink.Advise<IVsTextManagerEvents4>(_textManager, this);
        }

        int IVsTextManagerEvents4.OnUserPreferencesChanged4(
            VIEWPREFERENCES3[] viewPrefs,
            LANGPREFERENCES3[] langPrefs,
            FONTCOLORPREFERENCES2[] colorPrefs)
        {
            this.AssertIsForeground();

            if (langPrefs != null && langPrefs[0].guidLang == _languageSetting.guidLang)
            {
                _languageSetting = langPrefs[0];

                // We need to go and refresh each option we know about, since the option service caches them.
                // TODO: should a serializer have an event to say the backing store changed?
                RefreshOption(FormattingOptions.UseTabs);
                RefreshOption(FormattingOptions.TabSize);
                RefreshOption(FormattingOptions.SmartIndent);
                RefreshOption(FormattingOptions.IndentationSize);
                RefreshOption(CompletionOptions.HideAdvancedMembers);
                RefreshOption(CompletionOptions.TriggerOnTyping);
                RefreshOption(SignatureHelpOptions.ShowSignatureHelp);
                RefreshOption(NavigationBarOptions.ShowNavigationBar);
                RefreshOption(BraceCompletionOptions.EnableBraceCompletion);
            }

            return VSConstants.S_OK;
        }

        public virtual bool TryFetch(OptionKey optionKey, out object value)
        {
            if (optionKey.Language != _languageName)
            {
                value = null;
                return false;
            }

            if (optionKey.Option == FormattingOptions.UseTabs)
            {
                value = _languageSetting.fInsertTabs != 0;
            }
            else if (optionKey.Option == FormattingOptions.TabSize)
            {
                value = Convert.ToInt32(_languageSetting.uTabSize);
            }
            else if (optionKey.Option == FormattingOptions.IndentationSize)
            {
                value = Convert.ToInt32(_languageSetting.uIndentSize);
            }
            else if (optionKey.Option == FormattingOptions.SmartIndent)
            {
                switch (_languageSetting.IndentStyle)
                {
                    case vsIndentStyle.vsIndentStyleNone:
                        value = FormattingOptions.IndentStyle.None;
                        break;
                    case vsIndentStyle.vsIndentStyleDefault:
                        value = FormattingOptions.IndentStyle.Block;
                        break;
                    default:
                        value = FormattingOptions.IndentStyle.Smart;
                        break;
                }
            }
            else if (optionKey.Option == CompletionOptions.HideAdvancedMembers)
            {
                value = _languageSetting.fHideAdvancedAutoListMembers != 0;
            }
            else if (optionKey.Option == CompletionOptions.TriggerOnTyping)
            {
                value = _languageSetting.fAutoListMembers != 0;
            }
            else if (optionKey.Option == SignatureHelpOptions.ShowSignatureHelp)
            {
                value = _languageSetting.fAutoListParams != 0;
            }
            else if (optionKey.Option == NavigationBarOptions.ShowNavigationBar)
            {
                value = _languageSetting.fDropdownBar != 0;
            }
            else if (optionKey.Option == BraceCompletionOptions.EnableBraceCompletion)
            {
                value = _languageSetting.fBraceCompletion != 0;
            }
            else
            {
                // Unrecognized option
                value = null;
                return false;
            }

            return true;
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            if (optionKey.Language != _languageName)
            {
                return false;
            }

            var newSetting = _languageSetting;

            if (optionKey.Option == FormattingOptions.UseTabs)
            {
                newSetting.fInsertTabs = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (optionKey.Option == FormattingOptions.TabSize)
            {
                newSetting.uTabSize = Convert.ToUInt32(value);
            }
            else if (optionKey.Option == FormattingOptions.IndentationSize)
            {
                newSetting.uIndentSize = Convert.ToUInt32(value);
            }
            else if (optionKey.Option == FormattingOptions.SmartIndent)
            {
                switch ((FormattingOptions.IndentStyle)value)
                {
                    case FormattingOptions.IndentStyle.None:
                        newSetting.IndentStyle = vsIndentStyle.vsIndentStyleNone;
                        break;
                    case FormattingOptions.IndentStyle.Block:
                        newSetting.IndentStyle = vsIndentStyle.vsIndentStyleDefault;
                        break;
                    default:
                        newSetting.IndentStyle = vsIndentStyle.vsIndentStyleSmart;
                        break;
                }
            }
            else if (optionKey.Option == CompletionOptions.HideAdvancedMembers)
            {
                newSetting.fHideAdvancedAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (optionKey.Option == CompletionOptions.TriggerOnTyping)
            {
                newSetting.fAutoListMembers = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (optionKey.Option == SignatureHelpOptions.ShowSignatureHelp)
            {
                newSetting.fAutoListParams = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (optionKey.Option == NavigationBarOptions.ShowNavigationBar)
            {
                newSetting.fDropdownBar = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else if (optionKey.Option == BraceCompletionOptions.EnableBraceCompletion)
            {
                newSetting.fBraceCompletion = Convert.ToUInt32((bool)value ? 1 : 0);
            }
            else
            {
                // Unrecognized option
                return false;
            }

            if (!newSetting.Equals(_languageSetting))
            {
                _languageSetting = newSetting;

                // Something actually changed, so let's call down.
                SetUserPreferences();
            }

            // Even if we didn't call back, say we completed the persist
            return true;
        }

        private void SetUserPreferences()
        {
            var langPrefs = new LANGPREFERENCES3[1];
            langPrefs[0] = _languageSetting;

            if (IsForeground())
            {
                Marshal.ThrowExceptionForHR(_textManager.SetUserPreferences4(pViewPrefs: null, pLangPrefs: langPrefs, pColorPrefs: null));
            }
            else
            {
                Task.Factory.StartNew(this.SetUserPreferences, CancellationToken.None, TaskCreationOptions.None, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler);
            }
        }

        private void RefreshOption<T>(PerLanguageOption<T> option)
        {
            object value;
            OptionKey optionKey = new OptionKey(option, _languageName);
            if (TryFetch(optionKey, out value))
            {
                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                var visualStudioWorkspace = componentModel.GetService<VisualStudioWorkspace>();

                var optionService = visualStudioWorkspace.Services.GetService<IOptionService>();
                optionService.SetOptions(optionService.GetOptions().WithChangedOption(optionKey, value));
            }
        }
    }
}
