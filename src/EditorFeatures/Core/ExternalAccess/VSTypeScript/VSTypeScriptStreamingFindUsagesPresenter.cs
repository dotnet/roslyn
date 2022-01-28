// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Export(typeof(IVSTypeScriptStreamingFindUsagesPresenter)), Shared]
    internal sealed class VSTypeScriptStreamingFindUsagesPresenter : IVSTypeScriptStreamingFindUsagesPresenter
    {
        private readonly IStreamingFindUsagesPresenter _underlyingObject;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptStreamingFindUsagesPresenter(IStreamingFindUsagesPresenter underlyingObject)
            => _underlyingObject = underlyingObject;

        public (VSTypeScriptFindUsagesContext context, CancellationToken cancellationToken) StartSearch(
            string title, bool supportsReferences)
        {
            var (context, cancellationToken) = _underlyingObject.StartSearch(title, supportsReferences);
            return (new(context), cancellationToken);
        }

        public (VSTypeScriptFindUsagesContext context, CancellationToken cancellationToken) StartSearchWithCustomColumns(
            string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
        {
            var (context, cancellationToken) = _underlyingObject.StartSearchWithCustomColumns(title, supportsReferences, includeContainingTypeAndMemberColumns, includeKindColumn);
            return (new(context), cancellationToken);
        }

        public void ClearAll()
            => _underlyingObject.ClearAll();
    }
}
