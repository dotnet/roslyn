// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal interface IDescriptionService
{
    Task<IEnumerable<TaggedText>> GetDescriptionAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);

    LSP.MarkupContent GetMarkupContent(ImmutableArray<TaggedText> tags, string language, bool featureSupportsMarkdown);
}
