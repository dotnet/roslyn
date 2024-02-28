// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

/// <summary>
/// Contains changes that can be either applied to different targets such as a buffer or a tree
/// or examined to be used in other places such as quick fix.
/// </summary>
internal interface IFormattingResult
{
    IList<TextChange> GetTextChanges(CancellationToken cancellationToken);
    SyntaxNode GetFormattedRoot(CancellationToken cancellationToken);
}
