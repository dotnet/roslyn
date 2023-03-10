// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed partial class DocumentOutlineViewModel
    {
        private readonly struct IntervalIntrospector : IIntervalIntrospector<DocumentSymbolDataViewModel>
        {
            private readonly ITextSnapshot _textSnapshot;

            public IntervalIntrospector(ITextSnapshot textSnapshot)
            {
                _textSnapshot = textSnapshot;
            }

            public int GetStart(DocumentSymbolDataViewModel value)
            {
                return value.Data.RangeSpan.Start.TranslateTo(_textSnapshot, PointTrackingMode.Positive);
            }

            public int GetLength(DocumentSymbolDataViewModel value)
            {
                return value.Data.RangeSpan.TranslateTo(_textSnapshot, SpanTrackingMode.EdgeInclusive).Length;
            }
        }
    }
}
