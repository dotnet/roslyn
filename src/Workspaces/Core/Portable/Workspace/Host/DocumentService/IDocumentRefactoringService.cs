// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    public interface IDocumentRefactoringService : IWorkspaceService
    {
        /// <summary>
        /// Updates the contents of a document after some document info change, such as file name or path. May
        /// prompt a user for confirmation on the changes.
        /// </summary>
        /// <returns>
        /// Solution with the updated <paramref name="current"/> document
        /// </returns> 
        Task<Solution> UpdateAfterInfoChangeAsync(Document current, Document previous, CancellationToken cancellation = default);
    }
}
