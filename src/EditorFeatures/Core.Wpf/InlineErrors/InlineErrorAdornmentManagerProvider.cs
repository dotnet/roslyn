// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.InlineErrors;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class InlineErrorAdornmentManagerProvider : AbstractAdornmentManagerProvider<InlineErrorTag>
    {
        private const string LayerName = "RoslynInlineErrors";
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IClassificationTypeRegistryService _classificationTypeRegistryService;

        [Export]
        [Name(LayerName)]
        [ContentType(ContentTypeNames.RoslynContentType)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Squiggle)]
        internal readonly AdornmentLayerDefinition InlineErrorLayer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public InlineErrorAdornmentManagerProvider(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            IThreadingContext threadingContext,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService
            ) : base(threadingContext, tagAggregatorFactoryService, listenerProvider)
        {
            _classificationFormatMapService = classificationFormatMapService;
            _classificationTypeRegistryService = classificationTypeRegistryService;
        }

        protected override string FeatureAttributeName => FeatureAttribute.InlineErrors;

        protected override string AdornmentLayerName => LayerName;

        protected override void CreateAdornmentManager(IWpfTextView textView)
        {
            // the manager keeps itself alive by listening to text view events.
            _ = new InlineErrorAdornmentManager(ThreadingContext, textView, TagAggregatorFactoryService, AsyncListener, AdornmentLayerName, _classificationFormatMapService, _classificationTypeRegistryService);
        }
    }
}
