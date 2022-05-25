// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class CodeStyleSeparatedSyntaxListExtensions
    {
        public static SeparatedSyntaxList<TDerived> CastDown<TDerived>(this SeparatedSyntaxList<SyntaxNode> list)
            where TDerived : SyntaxNode
        {
#pragma warning disable CS0612 // Type or member is obsolete
            return CastDownHelper<TDerived>(list);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        [Obsolete]
        private static SeparatedSyntaxList<TDerived> CastDownHelper<TDerived>(SeparatedSyntaxList<SyntaxNode> list)
            where TDerived : SyntaxNode
        {
            return list;
        }
    }
}
