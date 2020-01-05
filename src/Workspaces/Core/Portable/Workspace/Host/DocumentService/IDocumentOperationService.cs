// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
