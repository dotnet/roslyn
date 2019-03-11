// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide various operations for this document
    /// 
    /// I followed name from EditorOperation for now. 
    /// </summary>
    internal interface IDocumentOperationService : IDocumentService
    {
        /// <summary>
        /// document version of <see cref="Workspace.CanApplyChange(ApplyChangesKind)"/>
        /// </summary>
        bool CanApplyChange { get; }

        /// <summary>
        /// This property is unused and has no effect. The definition is retained until IVT users migrate to the adapter
        /// assemblies.
        /// </summary>
        [Obsolete("This property is unused and has no effect.")]
        bool SupportDiagnostics { get; }
    }
}
