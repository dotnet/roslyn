// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal class MoveMembersResult
    {
        public Solution? Solution { get; }
        public DocumentId? NavigationDocumentId { get; }
        public string? FailureMessage { get; }
        public bool Success { get; }

        public MoveMembersResult(string failureMessage)
        {
            FailureMessage = failureMessage;
            Success = false;
        }

        public MoveMembersResult(Solution solution, DocumentId navigationDocumentId)
        {
            Success = true;
            Solution = solution;
            NavigationDocumentId = navigationDocumentId;
        }
    }
}
