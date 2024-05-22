// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Highlighting;

internal interface IHighlighter
{
    void AddHighlights(SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken);
}
