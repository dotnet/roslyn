// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal readonly struct ConflictLocationInfo
    {
        // The span of the Node that needs to be complexified 
        public readonly TextSpan ComplexifiedSpan;
        public readonly DocumentId DocumentId;

        // The identifier span that needs to be checked for conflict
        public readonly TextSpan OriginalIdentifierSpan;

        public ConflictLocationInfo(RelatedLocation location)
        {
            Debug.Assert(location.ComplexifiedTargetSpan.Contains(location.ConflictCheckSpan) || location.Type == RelatedLocationType.UnresolvableConflict);
            this.ComplexifiedSpan = location.ComplexifiedTargetSpan;
            this.DocumentId = location.DocumentId;
            this.OriginalIdentifierSpan = location.ConflictCheckSpan;
        }
    }
}
