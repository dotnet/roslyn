// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.QuickInfo;

internal class XamlQuickInfo(TextSpan span, IEnumerable<TaggedText> description, ISymbol? symbol)
{
    public TextSpan Span { get; } = span;
    public IEnumerable<TaggedText> Description { get; } = description;
    public ISymbol? Symbol { get; } = symbol;
}
