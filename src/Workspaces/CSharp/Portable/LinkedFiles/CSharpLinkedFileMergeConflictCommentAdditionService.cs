// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ILinkedFileMergeConflictCommentAdditionService), LanguageNames.CSharp), Shared]
internal sealed class CSharpLinkedFileMergeConflictCommentAdditionService : AbstractLinkedFileMergeConflictCommentAdditionService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpLinkedFileMergeConflictCommentAdditionService()
    {
    }

    internal override string GetConflictCommentText(string header, string beforeString, string afterString)
    {
        if (beforeString == null && afterString == null)
        {
            // Whitespace only
            return null;
        }
        else if (beforeString == null)
        {
            // New code
            return string.Format(@"
/* {0}
{1}
{2}
*/
",
            header,
            WorkspacesResources.Added_colon,
            afterString);
        }
        else if (afterString == null)
        {
            // Removed code
            return string.Format(@"
/* {0}
{1}
{2}
*/
",
            header,
            WorkspacesResources.Removed_colon,
            beforeString);
        }
        else
        {
            // Changed code
            return string.Format(@"
/* {0}
{1}
{2}
{3}
{4}
*/
",
            header,
            WorkspacesResources.Before_colon,
            beforeString,
            WorkspacesResources.After_colon,
            afterString);
        }
    }
}
