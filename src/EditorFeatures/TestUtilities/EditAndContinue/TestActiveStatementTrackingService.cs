// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class TestActiveStatementTrackingService : IActiveStatementTrackingService
    {
        public readonly TextSpan?[] TrackingSpans;
        private readonly DocumentId _documentId;

        public TestActiveStatementTrackingService(DocumentId documentId, TextSpan?[] trackingSpans)
        {
            this.TrackingSpans = trackingSpans;
            _documentId = documentId;
        }

        public bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span)
        {
            Assert.Equal(id.DocumentId, _documentId);

            var spanOpt = TrackingSpans[id.Ordinal];
            if (spanOpt != null)
            {
                span = spanOpt.Value;
                return true;
            }

            span = default(TextSpan);
            return false;
        }

        public void UpdateActiveStatementSpans(SourceText source, IEnumerable<KeyValuePair<ActiveStatementId, TextSpan>> spans)
        {
            foreach (var span in spans)
            {
                TrackingSpans[span.Key.Ordinal] = span.Value.Length > 0 ? span.Value : (TextSpan?)null;
            }
        }

        #region Not Implemented

#pragma warning disable 67 // unused
        public event Action<bool> TrackingSpansChanged;
#pragma warning restore 67

        public void StartTracking(EditSession editSession)
        {
            throw new NotImplementedException();
        }

        public void EndTracking()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
