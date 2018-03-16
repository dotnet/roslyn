// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal abstract class RegexNode : EmbeddedSyntaxNode<RegexNode>
    {
        protected RegexNode(RegexKind kind) : base((int)kind)
        {
        }

        public RegexKind Kind() => (RegexKind)RawKind;

        public abstract void Accept(IRegexNodeVisitor visitor);
    }
}
