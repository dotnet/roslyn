// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Internal.Performance;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    public static class ProjectSystemCodeMarkers
    {
        public static bool CodeMarker(int nTimerID)
            => CodeMarkers.Instance.CodeMarker(nTimerID);
    }
}
