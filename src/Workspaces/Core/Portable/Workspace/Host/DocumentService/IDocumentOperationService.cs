﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// TODO: Merge into <see cref="DocumentPropertiesService"/>.
    /// Used by Razor via IVT.
    /// </summary>
    internal interface IDocumentOperationService : IDocumentService
    {
        /// <summary>
        /// document version of <see cref="Workspace.CanApplyChange(ApplyChangesKind)"/>
        /// </summary>
        bool CanApplyChange { get; }

        /// <summary>
        /// indicates whether this document supports diagnostics or not
        /// </summary>
        bool SupportDiagnostics { get; }
    }
}
