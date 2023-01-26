// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens;

internal interface ICodeLensMemberFinder : ILanguageService
{
    /// <summary>
    /// Returns members in the document that are valid code lens locations.
    /// </summary>
    Task<ImmutableArray<CodeLensMember>> GetCodeLensMembersAsync(Document document, CancellationToken cancellationToken);
}

/// <summary>
/// Holds the node (for later reference count computation) and the span associated with the code lens element.
/// </summary>
internal record struct CodeLensMember(SyntaxNode Node, TextSpan Span);
