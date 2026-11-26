// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageService;

/// <summary>
/// Optional language service for identifying the members selected by a user.
/// </summary>
internal interface ISelectedMembersService : ILanguageService
{
    /// <summary>
    /// Returns the selected members, or an empty array if the selection cannot be determined.
    /// </summary>
    Task<ImmutableArray<SyntaxNode>> GetSelectedMembersAsync(
        SyntaxTree tree, TextSpan span, bool allowPartialSelection, CancellationToken cancellationToken);
}
