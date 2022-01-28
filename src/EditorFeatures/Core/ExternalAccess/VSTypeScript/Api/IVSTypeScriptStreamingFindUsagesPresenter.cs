// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptStreamingFindUsagesPresenter
    {
        (VSTypeScriptFindUsagesContext context, CancellationToken cancellationToken) StartSearch(
            string title, bool supportsReferences);

        (VSTypeScriptFindUsagesContext context, CancellationToken cancellationToken) StartSearchWithCustomColumns(
            string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn);

        void ClearAll();
    }
}
