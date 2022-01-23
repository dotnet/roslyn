// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    internal interface IPasteTrackingService
    {
        bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan);
    }
}
