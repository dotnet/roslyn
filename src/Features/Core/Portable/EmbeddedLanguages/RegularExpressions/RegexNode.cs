// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;

internal abstract class RegexNode : EmbeddedSyntaxNode<RegexKind, RegexNode>
{
    protected RegexNode(RegexKind kind) : base(kind)
    {
    }

    public abstract void Accept(IRegexNodeVisitor visitor);
}
