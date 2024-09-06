// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal sealed class TrackingSpanIntrospector(ITextSnapshot snapshot) : IIntervalIntrospector<ITrackingSpan>
{
    public TextSpan GetSpan(ITrackingSpan value)
        => value.GetSpan(snapshot).Span.ToTextSpan();
}
