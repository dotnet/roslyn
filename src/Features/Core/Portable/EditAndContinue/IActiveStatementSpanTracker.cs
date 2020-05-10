// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IActiveStatementSpanTracker : IWorkspaceService
    {
        bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span);

        /// <summary>
        /// Replaces the existing tracking spans with specified active statement spans.
        /// </summary>
        void UpdateActiveStatementSpans(SourceText source, IEnumerable<(ActiveStatementId, ActiveStatementTextSpan)> spans);
    }
}
