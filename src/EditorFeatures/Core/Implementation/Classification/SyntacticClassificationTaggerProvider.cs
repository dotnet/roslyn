// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TagType(typeof(IClassificationTag))]
    internal partial class SyntacticClassificationTaggerProvider : ITaggerProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListener _listener;
        private readonly ClassificationTypeMap _typeMap;

        private readonly ConditionalWeakTable<ITextBuffer, TagComputer> _tagComputers = new ConditionalWeakTable<ITextBuffer, TagComputer>();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SyntacticClassificationTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _notificationService = notificationService;
            _typeMap = typeMap;
            _listener = listenerProvider.GetListener(FeatureAttribute.Classification);
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (!buffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.SyntacticColorizer))
            {
                return null;
            }

            if (!_tagComputers.TryGetValue(buffer, out var tagComputer))
            {
                tagComputer = new TagComputer(buffer, _notificationService, _listener, _typeMap, this);
                _tagComputers.Add(buffer, tagComputer);
            }

            tagComputer.IncrementReferenceCount();

            var tagger = new Tagger(tagComputer);

            if (!(tagger is ITagger<T> typedTagger))
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
            => _tagComputers.Remove(buffer);
    }
}
