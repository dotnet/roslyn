// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ReplaceDiscardDeclarationsWithAssignments;

internal interface IReplaceDiscardDeclarationsWithAssignmentsService : ILanguageService
{
    /// <summary>
    /// Returns an updated <paramref name="memberDeclaration"/> with all the
    /// local declarations named '_' replaced with simple assignments to discard.
    /// For example,
    ///  1. <code>int _ = M();</code> is replaced with <code>_ = M();</code>
    ///  2. <code>int x = 1, _ = M(), y = 2;</code> is replaced with following statements:
    ///  <code>
    ///          int x = 1;
    ///          _ = M();
    ///          int y = 2;
    ///  </code>
    /// This is normally done in context of a code transformation that generates new discard assignment(s),
    /// such as <code>_ = M();</code>, and wants to prevent compiler errors where the containing method already
    /// has a discard variable declaration, say <code>var _ = M2();</code> at some line after the one
    /// where the code transformation wants to generate new discard assignment(s), which would be a compiler error.
    /// This method replaces such discard variable declarations with discard assignments.
    /// </summary>
    Task<SyntaxNode> ReplaceAsync(Document document, SyntaxNode memberDeclaration, CancellationToken cancellationToken);
}
