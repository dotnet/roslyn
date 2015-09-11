// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IActiveStatementTrackingService : IWorkspaceService
    {
        void StartTracking(EditSession editSession);
        void EndTracking();

        bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span);
        IEnumerable<ActiveStatementTextSpan> GetSpans(SourceText source);

        /// <summary>
        /// Triggered when tracking spans have changed.
        /// </summary>
        /// <remarks>
        /// The argument is true if the leaf active statement may have changed. 
        /// It might be true even if it didn't, but it's not false if it does.
        /// </remarks>
        event Action<bool> TrackingSpansChanged;

        /// <summary>
        /// Replaces the existing tracking spans with specified active statement spans.
        /// </summary>
        void UpdateActiveStatementSpans(SourceText source, IEnumerable<KeyValuePair<ActiveStatementId, TextSpan>> spans);
    }
}
