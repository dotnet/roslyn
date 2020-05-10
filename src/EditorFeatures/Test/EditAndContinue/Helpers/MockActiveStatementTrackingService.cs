// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 
#pragma warning disable CS0067 // unused event

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockActiveStatementTrackingService : IActiveStatementTrackingService
    {
        public event Action? TrackingSpansChanged;

        public void StartTracking()
        {
        }

        public void EndTracking()
        {
        }

        public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
            => Array.Empty<ActiveStatementTextSpan>();

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            span = default;
            return false;
        }

        public void UpdateActiveStatementSpans(SourceText source, IEnumerable<(ActiveStatementId, ActiveStatementTextSpan)> spans)
        {
        }
    }
}
