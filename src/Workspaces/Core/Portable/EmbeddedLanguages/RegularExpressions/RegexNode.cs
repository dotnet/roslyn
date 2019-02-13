// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    internal abstract class RegexNode : EmbeddedSyntaxNode<RegexKind, RegexNode>
    {
        protected RegexNode(RegexKind kind) : base(kind)
        {
        }

        public abstract void Accept(IRegexNodeVisitor visitor);
    }
}
