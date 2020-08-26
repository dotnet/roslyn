// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        Task<ImmutableDictionary<Guid, string>> GetProjectVersionsAsync(ImmutableArray<Guid> projectGuids, CancellationToken cancellationToken);

        /// <summary>
        /// Get reference count of the given descriptor
        /// </summary>
        Task<ReferenceCount?> GetReferenceCountAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);

        /// <summary>
        /// get reference location descriptor of the given descriptor
        /// </summary>
        Task<IEnumerable<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);

        /// <summary>
        /// Given a document and syntax node, returns a collection of locations of methods that refer to the located node.
        /// </summary>
        Task<IEnumerable<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(
            CodeLensDescriptor descriptor, CodeLensDescriptorContext descriptorContext, CancellationToken cancellationToken);
    }
}
