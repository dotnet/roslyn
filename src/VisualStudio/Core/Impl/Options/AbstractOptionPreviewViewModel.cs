// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
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
        private IComponentModel _componentModel;
        private IWpfTextViewHost _textViewHost;

        private IContentType _contentType;
        private IEditorOptionsFactoryService _editorOptions;
        private ITextEditorFactoryService _textEditorFactoryService;
        private ITextBufferFactoryService _textBufferFactoryService;
        private IProjectionBufferFactoryService _projectionBufferFactory;
        private IContentTypeRegistryService _contentTypeRegistryService;

        public List<object> Items { get; set; }

        public OptionSet Options { get; private set; }

        protected AbstractOptionPreviewViewModel(OptionSet options, IServiceProvider serviceProvider, string language)
        {
            this.Options = options;
            this.Items = new List<object>();

            _componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

            _contentTypeRegistryService = _componentModel.GetService<IContentTypeRegistryService>();
            _textBufferFactoryService = _componentModel.GetService<ITextBufferFactoryService>();
            _textEditorFactoryService = _componentModel.GetService<ITextEditorFactoryService>();
            _projectionBufferFactory = _componentModel.GetService<IProjectionBufferFactoryService>();
            _editorOptions = _componentModel.GetService<IEditorOptionsFactoryService>();
            this.Language = language;

            _contentType = _contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
        }

        internal OptionSet ApplyChangedOptions(OptionSet optionSet)
        {
            foreach (var optionKey in this.Options.GetAccessedOptions())
            {
                if (ShouldPersistOption(optionKey))
                {
                    optionSet = optionSet.WithChangedOption(optionKey, this.Options.GetOption(optionKey));
                }
            }

            return optionSet;
        }

        public void SetOptionAndUpdatePreview<T>(T value, IOption option, string preview)
        {
            if (option is PerLanguageOption<T>)
            {
                Options = Options.WithChangedOption((PerLanguageOption<T>)option, Language, value);
            }
            else
            {
                Options = Options.WithChangedOption((Option<T>)option, value);
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
            const string start = "//[";
            const string end = "//]";

            var service = MefV1HostServices.Create(_componentModel.DefaultExportProvider);
            var workspace = new PreviewWorkspace(service);
            var fileName = string.Format("project.{0}", Language == "C#" ? "csproj" : "vbproj");
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
            var formatted = Formatter.FormatAsync(document, this.Options).WaitAndGetResult(CancellationToken.None);

            var textBuffer = _textBufferFactoryService.CreateTextBuffer(formatted.GetTextAsync().Result.ToString(), _contentType);

            var container = textBuffer.AsTextContainer();
            var documentBackedByTextBuffer = document.WithText(container.CurrentText);

            var bufferText = textBuffer.CurrentSnapshot.GetText().ToString();
            var startIndex = bufferText.IndexOf(start, StringComparison.Ordinal);
            var endIndex = bufferText.IndexOf(end, StringComparison.Ordinal);
            var startLine = textBuffer.CurrentSnapshot.GetLineNumberFromPosition(startIndex) + 1;
            var endLine = textBuffer.CurrentSnapshot.GetLineNumberFromPosition(endIndex);

            var projection = _projectionBufferFactory.CreateProjectionBufferWithoutIndentation(_contentTypeRegistryService,
                _editorOptions.CreateOptions(),
                textBuffer.CurrentSnapshot,
                "",
                LineSpan.FromBounds(startLine, endLine));

            var textView = _textEditorFactoryService.CreateTextView(projection,
              _textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable));

            this.TextViewHost = _textEditorFactoryService.CreateTextViewHost(textView, setFocus: false);

            workspace.TryApplyChanges(documentBackedByTextBuffer.Project.Solution);
            workspace.OpenDocument(document.Id);

            this.TextViewHost.Closed += (s, a) =>
            {
                workspace.Dispose();
                workspace = null;
            };
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

        internal abstract bool ShouldPersistOption(OptionKey optionKey);
    }
}
