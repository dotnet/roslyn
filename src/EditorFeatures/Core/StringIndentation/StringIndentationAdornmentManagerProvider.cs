// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.StringIndentation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation;

/// <summary>
/// This factory is called to create the view service that will manage the indentation line for raw strings.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class StringIndentationAdornmentManagerProvider :
    AbstractAdornmentManagerProvider<StringIndentationTag>
{
    private const string LayerName = "RoslynStringIndentation";

    [Export]
    [Name(LayerName)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Squiggle)]
#pragma warning disable 0169
#pragma warning disable IDE0051 // Remove unused private members
    private readonly AdornmentLayerDefinition? _stringIndentationLayer;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore 0169

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public StringIndentationAdornmentManagerProvider(
        IThreadingContext threadingContext,
        IViewTagAggregatorFactoryService tagAggregatorFactoryService,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(threadingContext, tagAggregatorFactoryService, globalOptions, listenerProvider)
    {
    }

    protected override string FeatureAttributeName => FeatureAttribute.StringIndentation;
    protected override string AdornmentLayerName => LayerName;

    protected override void CreateAdornmentManager(IWpfTextView textView)
    {
        // the manager keeps itself alive by listening to text view events.
        _ = new StringIndentationAdornmentManager(ThreadingContext, textView, TagAggregatorFactoryService, AsyncListener, AdornmentLayerName);
    }
}
