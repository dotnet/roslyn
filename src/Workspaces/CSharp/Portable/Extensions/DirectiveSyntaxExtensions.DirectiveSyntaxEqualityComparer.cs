// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class DirectiveSyntaxExtensions
    {
        private class DirectiveSyntaxEqualityComparer : IEqualityComparer<DirectiveTriviaSyntax>
        {
            public static readonly DirectiveSyntaxEqualityComparer Instance = new DirectiveSyntaxEqualityComparer();

            private DirectiveSyntaxEqualityComparer()
            {
            }

            public bool Equals(DirectiveTriviaSyntax x, DirectiveTriviaSyntax y)
            {
                return x.SpanStart == y.SpanStart;
            }

            public int GetHashCode(DirectiveTriviaSyntax obj)
            {
                return obj.SpanStart;
            }
        }
    }
}
