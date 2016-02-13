// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// This factory is called to create the view service that will manage line separators.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class LineSeparatorAdornmentManagerProvider : IWpfTextViewCreationListener
    {
        internal const string AdornmentLayerName = "RoslynLineSeparator";

        private readonly IViewTagAggregatorFactoryService _tagAggregatorFactoryService;
        private readonly IAsynchronousOperationListener _asyncListener;

        [Export]
        [Name(AdornmentLayerName)]
        [ContentType(ContentTypeNames.RoslynContentType)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Squiggle)]
#pragma warning disable 0169
        private readonly AdornmentLayerDefinition _lineSeparatorLayer;
#pragma warning restore 0169

        [ImportingConstructor]
        public LineSeparatorAdornmentManagerProvider(
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _tagAggregatorFactoryService = tagAggregatorFactoryService;
            _asyncListener = new AggregateAsynchronousOperationListener(
                asyncListeners,
                FeatureAttribute.LineSeparators);
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!textView.TextBuffer.GetOption(EditorComponentOnOffOptions.Adornment))
            {
                return;
            }

            // the manager keeps itself alive by listening to text view events.
            AdornmentManager<LineSeparatorTag>.Create(textView, _tagAggregatorFactoryService, _asyncListener, AdornmentLayerName);
        }
    }
}
