// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

/// <summary>
/// Represents a service that can be imported via MEF to provide descriptions for a symbol.
/// </summary>
internal interface IDescriptionService
{
    /// <summary>
    /// Gets the description for the given symbol.
    /// </summary>
    Task<IEnumerable<TaggedText>> GetDescriptionAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Converts the given <paramref name="tags"/> to <see cref="LSP.MarkupContent"/>.
    /// </summary>
    LSP.MarkupContent GetMarkupContent(ImmutableArray<TaggedText> tags, string language, bool featureSupportsMarkdown);
}
