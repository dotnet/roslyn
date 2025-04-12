// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;

internal sealed class SignatureHelpClassifier : IClassifier
{
    private readonly ITextBuffer _subjectBuffer;
    private readonly ClassificationTypeMap _typeMap;

#pragma warning disable 67
    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

    public SignatureHelpClassifier(ITextBuffer subjectBuffer, ClassificationTypeMap typeMap)
    {
        _subjectBuffer = subjectBuffer;
        _typeMap = typeMap;
    }

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        if (_subjectBuffer.Properties.TryGetProperty(typeof(ISignatureHelpSession), out ISignatureHelpSession session) &&
            session.SelectedSignature is Signature)
        {
            var signature = (Signature)session.SelectedSignature;
            if (!_subjectBuffer.Properties.TryGetProperty("UsePrettyPrintedContent", out bool usePrettyPrintedContent))
            {
                usePrettyPrintedContent = false;
            }

            var content = usePrettyPrintedContent
                ? signature.PrettyPrintedContent
                : signature.Content;

            var displayParts = usePrettyPrintedContent
                ? signature.PrettyPrintedDisplayParts
                : signature.DisplayParts;

            if (content == _subjectBuffer.CurrentSnapshot.GetText())
            {
                return displayParts.ToClassificationSpans(span.Snapshot, _typeMap);
            }
        }

        return [];
    }
}
