// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Tagger
{
    /// <remarks>
    /// Adapted from Microsoft.CodeAnalysis.Editor.Implementation.Classification.SyntacticClassificationTaggerProvider.
    /// The provider hands out taggers.  Two taggers for the same buffer are backed by the same tag computer to avoid
    /// duplicating work.  This is important because tagger consumers may Dispose them and we only want to clean up
    /// the corresponding computer when the last tagger for the buffer is disposed.
    /// </remarks>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed partial class SyntacticClassificationTaggerProvider : ITaggerProvider
    {
        private readonly ClassificationTypeMap _typeMap;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;

        private readonly ConditionalWeakTable<ITextBuffer, TagComputer> tagComputers = new ConditionalWeakTable<ITextBuffer, TagComputer>();

        [ImportingConstructor]
        public SyntacticClassificationTaggerProvider(
            ClassificationTypeMap typeMap,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _typeMap = typeMap;
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.Classification);
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (!tagComputers.TryGetValue(buffer, out var tagComputer))
            {
                tagComputer = new TagComputer(buffer, _typeMap, _listener, this);
                tagComputers.Add(buffer, tagComputer);
            }

            tagComputer.IncrementReferenceCount();

            var tagger = new Tagger(tagComputer);

            if (tagger is ITagger<T> typedTagger)
            {
                return typedTagger;
            }

            // Oops, we can't actually return this tagger, so just clean up
            // (This seems like it should be impossible in practice, but Roslyn
            // was hardened against it so we are as well.)
            tagger.Dispose();
            return null;
        }

        private void DisconnectTagComputer(ITextBuffer buffer)
        {
            tagComputers.Remove(buffer);
        }
    }
}
