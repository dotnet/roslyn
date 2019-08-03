// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    /// <summary>
    /// Provide information related to VS/Roslyn to CodeLens OOP process
    /// </summary>
    internal interface ICodeLensContext
    {
        /// <summary>
        /// Get roslyn remote host's host group ID that is required for code lens OOP to connect roslyn remote host
        /// </summary>
        Task<string> GetHostGroupIdAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get [documentId.ProjectId.Id, documentId.Id] from given project guid and filePath
        /// 
        /// we can only use types code lens OOP supports by default. otherwise, we need to define DTO types
        /// just to marshal between VS and Code lens OOP. 
        /// </summary>
        List<Guid> GetDocumentId(Guid projectGuid, string filePath, CancellationToken cancellationToken);

        /// <summary>
        /// Get reference count of the given descriptor
        /// </summary>
        Task<ReferenceCount> GetReferenceCountAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);

        /// <summary>
        /// get reference location descriptor of the given descriptor
        /// </summary>
        Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);

        /// <summary>
        /// Given a document and syntax node, returns a collection of locations of methods that refer to the located node.
        /// </summary>
        Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);
    }
}
