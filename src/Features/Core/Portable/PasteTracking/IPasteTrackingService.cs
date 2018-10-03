// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    internal interface IPasteTrackingService
    {
        bool TryGetPasteTrackingInformation(Document document, out PasteTrackingInformation trackingInformation);

        void RegisterPastedTextSpan(Document document, TextSpan textSpan);

        bool ClearPasteTrackingInformation(Document document);
    }
}
