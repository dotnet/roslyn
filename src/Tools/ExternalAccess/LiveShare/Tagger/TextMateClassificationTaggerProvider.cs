// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Tagger
{
    // We implement both ITaggerProvider and IViewTaggerProvider.  The former is so that
    // we can classify buffers not associated with views (VS actually uses these in cases like the
    // scroll bar "scroll map" feature.  The latter is so that we are told about text views, and
    // thus can register their format maps with the FormatMappingChangedWatcher.
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TextMateClassificationTaggerProvider : ITaggerProvider, IViewTaggerProvider
    {
        private readonly ICommonEditorAssetServiceFactory _assetServiceFactory;

        [ImportingConstructor]
        public TextMateClassificationTaggerProvider(ICommonEditorAssetServiceFactory assetServiceFactory)
        {
            _assetServiceFactory = assetServiceFactory;
        }

        public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag
        {
            return CreateTagger<T>(buffer);
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            buffer.Properties.GetOrCreateSingletonProperty("defaultFileExtensionForGrammar", GetDefaultFileExtension);

            var factory = _assetServiceFactory.GetOrCreate(buffer);
            var taggerProvider = factory.FindAsset<ITaggerProvider>((metadata) => metadata.TagTypes.Any(tagType => typeof(T).IsAssignableFrom(tagType)));
            var tagger = taggerProvider?.CreateTagger<T>(buffer);

            return tagger;

            string GetDefaultFileExtension()
            {
                if (!TryGetFilePathFromTextDocument(buffer, out var filePath))
                {
                    // nothing we can do without filepath
                    return null;
                }

                return Path.GetExtension(filePath);
            }

            static bool TryGetFilePathFromTextDocument(ITextBuffer buffer, out string filePath)
            {
                if (buffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument))
                {
                    filePath = textDocument.FilePath;
                    return true;
                }

                filePath = null;
                return false;
            }
        }
    }
}
