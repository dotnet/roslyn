// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly record struct VSTypeScriptIndentationOptions(
    bool UseSpaces,
    int TabSize,
    int IndentSize);

internal interface IVSTypeScriptFormattingServiceImplementation
{
    Task<Document> FormatAsync(Document document, IEnumerable<TextSpan>? spans, VSTypeScriptIndentationOptions options, CancellationToken cancellationToken);
}
