// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal struct RegexNodeOrToken
    {
        public readonly RegexNode Node;
        public readonly RegexToken Token;

        private RegexNodeOrToken(RegexNode node) : this()
        {
            Debug.Assert(node != null);
            Node = node;
        }

        private RegexNodeOrToken(RegexToken token) : this()
        {
            Debug.Assert(token.Kind != RegexKind.None);
            Token = token;
        }

        public bool IsNode => Node != null;

        public static implicit operator RegexNodeOrToken(RegexNode node)
            => new RegexNodeOrToken(node);

        public static implicit operator RegexNodeOrToken(RegexToken token)
            => new RegexNodeOrToken(token);
    }
}
