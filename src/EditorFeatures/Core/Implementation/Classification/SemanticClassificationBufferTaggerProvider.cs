// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    /// <summary>
    /// This is the tagger we use for buffer classification scenarios.  It is only used for 
    /// IAccurateTagger scenarios.  Namely: Copy/Paste and Printing.  We use an 'Accurate' buffer
    /// tagger since these features need to get classification tags for the entire file.
    /// 
    /// i.e. if you're printing, you want semantic classification even for code that's not in view.
    /// The same applies to copy/pasting.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class SemanticClassificationBufferTaggerProvider : ForegroundThreadAffinitizedObject, ITaggerProvider
    {
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IForegroundNotificationService _notificationService;
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public SemanticClassificationBufferTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            ClassificationTypeMap typeMap,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext)
        {
            _notificationService = notificationService;
            _semanticChangeNotificationService = semanticChangeNotificationService;
            _typeMap = typeMap;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.Classification);
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            this.AssertIsForeground();
            return new Tagger(this, buffer) as IAccurateTagger<T>;
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            return CreateTagger<T>(buffer);
        }
    }
}
