// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TagType(typeof(IClassificationTag))]
    internal partial class SyntacticClassificationTaggerProvider : ITaggerProvider
    {
        private readonly IForegroundNotificationService _notificationService;
        private readonly IViewSupportsClassificationService _viewSupportsClassificationServiceOpt;
        private readonly ITextBufferAssociatedViewService _associatedViewService;
        private readonly IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> _editorClassificationLanguageServices;
        private readonly IEnumerable<Lazy<ILanguageService, ContentTypeLanguageMetadata>> _contentTypesToLanguageNames;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;
        private readonly ClassificationTypeMap _typeMap;

        private readonly ConditionalWeakTable<ITextBuffer, TagComputer> _tagComputers = new ConditionalWeakTable<ITextBuffer, TagComputer>();

        [ImportingConstructor]
        public SyntacticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ClassificationTypeMap typeMap,
            [Import(AllowDefault = true)] IViewSupportsClassificationService viewSupportsClassificationServiceOpt,
            ITextBufferAssociatedViewService associatedViewService,
            [ImportMany] IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> allLanguageServices,
            [ImportMany] IEnumerable<Lazy<ILanguageService, ContentTypeLanguageMetadata>> contentTypes,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _notificationService = notificationService;
            _typeMap = typeMap;
            _viewSupportsClassificationServiceOpt = viewSupportsClassificationServiceOpt;
            _associatedViewService = associatedViewService;
            _editorClassificationLanguageServices = allLanguageServices.Where(s => s.Metadata.ServiceType == typeof(IEditorClassificationService).AssemblyQualifiedName);
            _contentTypesToLanguageNames = contentTypes.Where(x => x.Metadata.DefaultContentType != null);
            _asyncListeners = asyncListeners;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (!buffer.GetOption(InternalFeatureOnOffOptions.SyntacticColorizer))
            {
                return null;
            }

            TagComputer tagComputer;
            if (!_tagComputers.TryGetValue(buffer, out tagComputer))
            {
                var asyncListener = new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.Classification);

                var languageName = _contentTypesToLanguageNames.FirstOrDefault(x => buffer.ContentType.MatchesAny(x.Metadata.DefaultContentType))?.Metadata.Language;
                var editorClassificationService = _editorClassificationLanguageServices.FirstOrDefault(x => x.Metadata.Language == languageName).Value as IEditorClassificationService;

                if (editorClassificationService == null)
                {
                    return null;
                }

                tagComputer = new TagComputer(
                    buffer,
                    _notificationService,
                    asyncListener,
                    _typeMap,
                    this,
                    _viewSupportsClassificationServiceOpt,
                    _associatedViewService, 
                    editorClassificationService,
                    languageName);

                _tagComputers.Add(buffer, tagComputer);
            }

            tagComputer.IncrementReferenceCount();

            var tagger = new Tagger(tagComputer);
            var typedTagger = tagger as ITagger<T>;

            if (typedTagger == null)
            {
                // Oops, we can't actually return this tagger, so just clean up
                tagger.Dispose();
                return null;
            }
            else
            {
                return typedTagger;
            }
        }

        private void DisconnectTagComputer(ITextBuffer buffer)
        {
            _tagComputers.Remove(buffer);
        }
    }
}
