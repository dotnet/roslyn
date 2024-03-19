// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

/// <summary>
/// GlyphFactory provides the InheritanceMargin shows in IndicatorMargin. (Margin shared with breakpoint)
/// </summary>
internal sealed class InheritanceGlyphFactory : IGlyphFactory
{
    private readonly Workspace _workspace;
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
    private readonly ClassificationTypeMap _classificationTypeMap;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly IUIThreadOperationExecutor _operationExecutor;
    private readonly IWpfTextView _textView;
    private readonly IAsynchronousOperationListener _listener;
    private readonly IGlobalOptionService _globalOptions;

    public InheritanceGlyphFactory(
        Workspace workspace,
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
        ClassificationTypeMap classificationTypeMap,
        IClassificationFormatMap classificationFormatMap,
        IUIThreadOperationExecutor operationExecutor,
        IWpfTextView textView,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListener listener)
    {
        _workspace = workspace;
        _threadingContext = threadingContext;
        _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
        _classificationTypeMap = classificationTypeMap;
        _classificationFormatMap = classificationFormatMap;
        _operationExecutor = operationExecutor;
        _textView = textView;
        _globalOptions = globalOptions;
        _listener = listener;
    }

    public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
    {
        if (tag is not InheritanceMarginTag inheritanceMarginTag)
        {
            return null;
        }

        // The life cycle of the glyphs in Indicator Margin is controlled by the editor,
        // so in order to get the glyphs removed when FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin is off,
        // we need
        // 1. Generate tags when this option changes.
        // 2. Always return null here to force the editor to remove the glyphs.
        if (!_globalOptions.GetOption(InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin))
        {
            return null;
        }

        if (_textView.TextBuffer.GetWorkspace() == null)
        {
            return null;
        }

        var membersOnLine = inheritanceMarginTag.MembersOnLine;
        Contract.ThrowIfTrue(membersOnLine.IsEmpty);
        return new InheritanceMarginGlyph(
            _workspace,
            _threadingContext,
            _streamingFindUsagesPresenter,
            _classificationTypeMap,
            _classificationFormatMap,
            _operationExecutor,
            inheritanceMarginTag,
            _textView,
            _listener);
    }
}
