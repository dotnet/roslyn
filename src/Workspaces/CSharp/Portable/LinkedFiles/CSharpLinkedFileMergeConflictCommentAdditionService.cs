// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ILinkedFileMergeConflictCommentAdditionService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpLinkedFileMergeConflictCommentAdditionService()
    : AbstractLinkedFileMergeConflictCommentAdditionService
{
    protected override string? GetLanguageSpecificConflictCommentText(string header, string? beforeString, string? afterString)
    {
        if (beforeString == null)
        {
            // New code
            return $"""

                /* {header}
                {WorkspacesResources.Added_colon}
                {afterString}
                */

                """;
        }
        else if (afterString == null)
        {
            // Removed code
            return $"""

                /* {header}
                {WorkspacesResources.Removed_colon}
                {beforeString}
                */

                """;
        }
        else
        {
            return null;
        }
    }
}
