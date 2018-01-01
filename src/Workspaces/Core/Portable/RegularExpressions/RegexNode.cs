// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal abstract class RegexNode
    {
        public readonly RegexKind Kind;

        protected RegexNode(RegexKind kind)
        {
            Debug.Assert(kind != RegexKind.None);
            Kind = kind;
        }

        public abstract int ChildCount { get; }
        public abstract RegexNodeOrToken ChildAt(int index);

        public abstract void Accept(IRegexNodeVisitor visitor);
    }
}
