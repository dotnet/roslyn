// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal sealed class InteractiveSmartIndenter : ISmartIndent
    {
        private readonly IContentType _contentType;
        private readonly ITextView _view;
        private readonly ISmartIndent _indenter;

        internal static InteractiveSmartIndenter Create(
            IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> smartIndenterProviders,
            IContentType contentType,
            ITextView view)
        {
            var provider = GetProvider(smartIndenterProviders, contentType);
            return (provider == null) ? null : new InteractiveSmartIndenter(contentType, view, provider.Item2.Value);
        }

        private InteractiveSmartIndenter(IContentType contentType, ITextView view, ISmartIndentProvider provider)
        {
            _contentType = contentType;
            _view = view;
            _indenter = provider.CreateSmartIndent(view);
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            // get point at the subject buffer
            var mappingPoint = _view.BufferGraph.CreateMappingPoint(line.Start, PointTrackingMode.Negative);

            // TODO (https://github.com/dotnet/roslyn/issues/5281): Remove try-catch.
            SnapshotPoint? point = null;
            try
            {
                point = mappingPoint.GetInsertionPoint(b => b.ContentType.IsOfType(_contentType.TypeName));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Suppress this to work around DevDiv #144964.
                // Note: Other callers might be affected, but this is the narrowest workaround for the observed problems.
                // A fix is already being reviewed, so a broader change is not required.
                return null;
            }

            if (!point.HasValue)
            {
                return null;
            }

            // Currently, interactive smart indenter returns indentation based
            // solely on subject buffer's information and doesn't consider spaces
            // in interactive window itself. Note: This means the ITextBuffer passed
            // to ISmartIndent.GetDesiredIndentation is not this.view.TextBuffer.
            return _indenter.GetDesiredIndentation(point.Value.GetContainingLine());
        }

        public void Dispose()
        {
            _indenter.Dispose();
        }

        // Returns the provider that supports the most derived content type.
        // If there are two providers that support the same content type, or
        // two providers that support different content types that do not have
        // inheritance relationship, we simply return the first we encounter.
        private static Tuple<IContentType, Lazy<ISmartIndentProvider, ContentTypeMetadata>> GetProvider(
            IEnumerable<Lazy<ISmartIndentProvider, ContentTypeMetadata>> smartIndenterProviders,
            IContentType contentType)
        {
            // If there are two providers that both support the
            // same content type, we simply choose the first.
            var provider = smartIndenterProviders.FirstOrDefault(p => p.Metadata.ContentTypes.Contains(contentType.TypeName));
            if (provider != null)
            {
                return Tuple.Create(contentType, provider);
            }

            Tuple<IContentType, Lazy<ISmartIndentProvider, ContentTypeMetadata>> bestPair = null;
            foreach (var baseType in contentType.BaseTypes)
            {
                var pair = GetProvider(smartIndenterProviders, baseType);
                if ((pair != null) && ((bestPair == null) || IsBaseContentType(pair.Item1, bestPair.Item1)))
                {
                    bestPair = pair;
                }
            }

            return bestPair;
        }

        // Returns true if the second content type is a base type of the first.
        private static bool IsBaseContentType(IContentType type, IContentType potentialBase)
        {
            return type.BaseTypes.Any(b => b.IsOfType(potentialBase.TypeName) || IsBaseContentType(b, potentialBase));
        }
    }
}
