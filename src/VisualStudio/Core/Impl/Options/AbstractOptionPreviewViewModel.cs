﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractOptionPreviewViewModel : AbstractNotifyPropertyChanged, IDisposable
    {
        private readonly IComponentModel _componentModel;
        private IWpfTextViewHost _textViewHost;

        private readonly IContentType _contentType;
        private readonly IEditorOptionsFactoryService _editorOptions;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactory;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;

        public List<object> Items { get; set; }
        public ObservableCollection<AbstractCodeStyleOptionViewModel> CodeStyleItems { get; set; }

        public OptionStore OptionStore { get; set; }

        protected AbstractOptionPreviewViewModel(OptionStore optionStore, IServiceProvider serviceProvider, string language)
        {
            this.OptionStore = optionStore;
            this.Items = new List<object>();
            this.CodeStyleItems = new ObservableCollection<AbstractCodeStyleOptionViewModel>();

            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            _contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
            _textBufferFactoryService = _componentModel.GetService<ITextBufferFactoryService>();
            _textEditorFactoryService = _componentModel.GetService<ITextEditorFactoryService>();
            _projectionBufferFactory = _componentModel.GetService<IProjectionBufferFactoryService>();
            _editorOptions = _componentModel.GetService<IEditorOptionsFactoryService>();
            this.Language = language;

            _contentType = _contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
        }

        public void SetOptionAndUpdatePreview<T>(T value, IOption option, string preview)
        {
            if (option is Option<CodeStyleOption<T>>)
            {
                var opt = OptionStore.GetOption((Option<CodeStyleOption<T>>)option);
                opt.Value = value;
                OptionStore.SetOption((Option<CodeStyleOption<T>>)option, opt);
            }
            else if (option is PerLanguageOption<CodeStyleOption<T>>)
            {
                var opt = OptionStore.GetOption((PerLanguageOption<CodeStyleOption<T>>)option, Language);
                opt.Value = value;
                OptionStore.SetOption((PerLanguageOption<CodeStyleOption<T>>)option, Language, opt);
            }
            else if (option is Option<T>)
            {
                OptionStore.SetOption((Option<T>)option, value);
            }
            else if (option is PerLanguageOption<T>)
            {
                OptionStore.SetOption((PerLanguageOption<T>)option, Language, value);
            }
            else
            {
                throw new InvalidOperationException("Unexpected option type");
            }

            UpdateDocument(preview);
        }

        public IWpfTextViewHost TextViewHost
        {
            get
            {
                return _textViewHost;
            }

            private set
            {
                // make sure we close previous view.
                if (_textViewHost != null)
                {
                    _textViewHost.Close();
                }

                SetProperty(ref _textViewHost, value);
            }
        }

        public string Language { get; }

        public void UpdatePreview(string text)
        {
            var service = VisualStudioMefHostServices.Create(_componentModel.GetService<ExportProvider>());
            var workspace = new PreviewWorkspace(service);
            var fileName = "project." + (Language == "C#" ? "csproj" : "vbproj");
            var project = workspace.CurrentSolution.AddProject(fileName, "assembly.dll", Language);

            // use the mscorlib, system, and system.core that are loaded in the current process.
            string[] references =
                {
                    "mscorlib",
                    "System",
                    "System.Core"
                };

            var metadataService = workspace.Services.GetService<IMetadataService>();

            var referenceAssemblies = Thread.GetDomain().GetAssemblies()
                .Where(x => references.Contains(x.GetName(true).Name, StringComparer.OrdinalIgnoreCase))
                .Select(a => metadataService.GetReference(a.Location, MetadataReferenceProperties.Assembly));

            project = project.WithMetadataReferences(referenceAssemblies);

            var document = project.AddDocument("document", SourceText.From(text, Encoding.UTF8));
            var formatted = Formatter.FormatAsync(document, OptionStore.GetOptions()).WaitAndGetResult(CancellationToken.None);

            var textBuffer = _textBufferFactoryService.CreateTextBuffer(formatted.GetTextAsync().Result.ToString(), _contentType);

            var container = textBuffer.AsTextContainer();
            var documentBackedByTextBuffer = document.WithText(container.CurrentText);

            var projection = _projectionBufferFactory.CreateProjectionBufferWithoutIndentation(_contentTypeRegistryService,
                _editorOptions.CreateOptions(),
                textBuffer.CurrentSnapshot,
                separator: "",
                exposedLineSpans: GetExposedLineSpans(textBuffer.CurrentSnapshot).ToArray());

            var textView = _textEditorFactoryService.CreateTextView(projection,
              _textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Interactive));

            this.TextViewHost = _textEditorFactoryService.CreateTextViewHost(textView, setFocus: false);

            workspace.TryApplyChanges(documentBackedByTextBuffer.Project.Solution);
            workspace.OpenDocument(document.Id);

            this.TextViewHost.Closed += (s, a) =>
            {
                workspace.Dispose();
                workspace = null;
            };
        }

        private static List<LineSpan> GetExposedLineSpans(ITextSnapshot textSnapshot)
        {
            const string start = "//[";
            const string end = "//]";

            var bufferText = textSnapshot.GetText().ToString();

            var lineSpans = new List<LineSpan>();
            var lastEndIndex = 0;

            while (true)
            {
                var startIndex = bufferText.IndexOf(start, lastEndIndex, StringComparison.Ordinal);
                if (startIndex == -1)
                {
                    break;
                }

                var endIndex = bufferText.IndexOf(end, lastEndIndex, StringComparison.Ordinal);

                var startLine = textSnapshot.GetLineNumberFromPosition(startIndex) + 1;
                var endLine = textSnapshot.GetLineNumberFromPosition(endIndex);

                lineSpans.Add(LineSpan.FromBounds(startLine, endLine));
                lastEndIndex = endIndex + end.Length;
            }

            return lineSpans;
        }

        public void Dispose()
        {
            if (_textViewHost != null)
            {
                _textViewHost.Close();
                _textViewHost = null;
            }
        }

        private void UpdateDocument(string text)
        {
            UpdatePreview(text);
        }

        protected void AddParenthesesOption(
            string language, OptionStore optionStore,
            PerLanguageOption<CodeStyleOption<ParenthesesPreference>> languageOption,
            string title, string[] examples, bool defaultAddForClarity)
        {
            var preferences = new List<ParenthesesPreference>();
            var codeStylePreferences = new List<CodeStylePreference>();

            preferences.Add(ParenthesesPreference.AlwaysForClarity);
            codeStylePreferences.Add(new CodeStylePreference(ServicesVSResources.Always_for_clarity, isChecked: defaultAddForClarity));

            preferences.Add(ParenthesesPreference.NeverIfUnnecessary);
            codeStylePreferences.Add(new CodeStylePreference(
                ServicesVSResources.Never_if_unnecessary,
                isChecked: !defaultAddForClarity));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ParenthesesPreference>(
                languageOption, language, title, preferences.ToArray(),
                examples, this, optionStore, ServicesVSResources.Parentheses_preferences_colon,
                codeStylePreferences));
        }

        protected void AddUnusedParameterOption(string language, OptionStore optionStore, string title, string[] examples)
        {
            var unusedParameterPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(ServicesVSResources.Non_public_methods, isChecked: false),
                new CodeStylePreference(ServicesVSResources.All_methods, isChecked: true),
            };

            var enumValues = new[]
            {
                UnusedParametersPreference.NonPublicMethods,
                UnusedParametersPreference.AllMethods
            };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<UnusedParametersPreference>(
                CodeStyleOptions.UnusedParameters, language,
                ServicesVSResources.Avoid_unused_parameters, enumValues,
                examples, this, optionStore, title,
                unusedParameterPreferences));
        }
    }
}
