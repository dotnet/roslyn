// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.CodeLensOOP
{
    /// <summary>
    /// new code lens API that runs on codelen's OOP assumes it gets result from roslyn OOP's primary workspace.
    /// 
    /// for now, we keeps both new and old APIs, but before v.Next we will decide wether we will go this way or move back to old API
    /// but add new ability in new code lens API where it can point to any OOP as code lens provider OOP host rather than forcing
    /// people to use codelens OOP which might not have all information.
    /// </summary>
    internal interface IRemoteCodeLensReferencesFromPrimaryWorkspaceService
    {
        Task<ReferenceCount> GetReferenceCountAsync(Guid projectIdGuid, string filePath, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken);
        Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Guid projectIdGuid, string filePath, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// due to limitation on current code lens API, it doesn't allow anything to push/ask code lens to refresh. this is a workaround to make that work for now
        /// </summary>
        void SetCodeLensReferenceCallback(Guid projectIdGuid, string filePath, CancellationToken cancellationToken);
    }
}
