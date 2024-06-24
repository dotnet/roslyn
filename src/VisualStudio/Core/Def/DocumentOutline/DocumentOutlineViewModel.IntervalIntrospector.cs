// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

internal sealed partial class DocumentOutlineViewModel
{
    /// <summary>
    /// Helper for <see cref="DocumentOutlineViewState.ViewModelItemsTree"/>.  Allows us to lookup a set of
    /// view-models that intersect the care efficiently.
    /// </summary>
    private readonly struct IntervalIntrospector : IIntervalIntrospector<DocumentSymbolDataViewModel>
    {
        public TextSpan GetSpan(DocumentSymbolDataViewModel value)
            => value.Data.RangeSpan.Span.ToTextSpan();
    }
}
