// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptDiagnosticsUpdatedArgsWrapper
    {
        internal readonly DiagnosticsUpdatedArgs UnderlyingObject;

        public VSTypeScriptDiagnosticsUpdatedArgsWrapper(DiagnosticsUpdatedArgs underlyingObject)
            => UnderlyingObject = underlyingObject;

        public Solution? Solution
            => UnderlyingObject.Solution;

        public DocumentId? DocumentId
            => UnderlyingObject.DocumentId;
    }
}
