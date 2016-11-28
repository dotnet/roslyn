// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    /// <summary>
    /// This factory is called to create the view service that will manage suggestion adornments.
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class SuggestionAdornmentManagerProvider :
        AbstractAdornmentManagerProvider<SuggestionTag>
    {
        private const string LayerName = "RoslynSuggestions";

        [Export]
        [Name(LayerName)]
        [ContentType(ContentTypeNames.RoslynContentType)]
        [Order(After = PredefinedAdornmentLayers.Selection)]
        [Order(After = PredefinedAdornmentLayers.Squiggle)]
#pragma warning disable 0169
        private readonly AdornmentLayerDefinition _lineSeparatorLayer;
#pragma warning restore 0169

        [ImportingConstructor]
        public SuggestionAdornmentManagerProvider(
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(tagAggregatorFactoryService, asyncListeners)
        {
        }

        protected override string FeatureAttributeName => FeatureAttribute.ErrorSquiggles;
        protected override string AdornmentLayerName => LayerName;
    }
}