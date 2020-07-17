// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IActiveStatementSpanTracker
    {
        bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span);
    }
}
