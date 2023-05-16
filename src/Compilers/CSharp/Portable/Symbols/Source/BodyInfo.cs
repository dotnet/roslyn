// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal readonly struct BodyInfo
    {
        public static readonly BodyInfo NoBody = default;
        public static readonly BodyInfo NonBlockNonExpressionBodied = new BodyInfo(hasBlockBody: false, hasExpressionBody: false, hasNonBlockNonExpressionBody: true);
        public static readonly BodyInfo BlockBodied = new BodyInfo(hasBlockBody: false, hasExpressionBody: true, hasNonBlockNonExpressionBody: false);
        public static readonly BodyInfo ExpressionBodied = new BodyInfo(hasBlockBody: false, hasExpressionBody: true, hasNonBlockNonExpressionBody: false);

        /// <summary>
        /// Symbol explicitly had a <see cref="BlockSyntax"/> body.
        /// </summary>
        public readonly bool HasBlockBody;

        /// <summary>
        /// Symbol explicitly had a <see cref="ArrowExpressionClauseSyntax"/> body and did <em>not</em> also have a
        /// <see cref="BlockSyntax"/> body.
        /// </summary>
        public readonly bool HasExpressionBody;

        /// <summary>
        /// Symbol had some syntax that should be considered its 'body'.  Intuitively, this is generally set for 
        /// symbols that still execute code, but don't explicitly have block or expression body (like a top level
        /// entrypoint).  This is generally <em>not</em> set if a symbol is abstract/extern, as those modifiers
        /// indicate that they will not execute c# code directly themselves.
        /// </summary>
        private readonly bool _hasNonBlockNonExpressionBody;

        /// <summary>
        /// Symbol had a body of some sort.  Either a <see cref="BlockSyntax"/> or <see cref="ArrowExpressionClauseSyntax"/>
        /// body, or something else that was considered to be the body.  For example, a "top level entrypoint" is considered
        /// to have a body, since it had top level statements which constitute its body.
        /// </summary>
        public bool HasAnyBody => HasBlockBody || HasExpressionBody || _hasNonBlockNonExpressionBody;

#nullable enable
        public static BodyInfo Create(BlockSyntax? block, ArrowExpressionClauseSyntax? arrowExpressionClause)
            => new BodyInfo(hasBlockBody: block != null, hasExpressionBody: block == null && arrowExpressionClause != null, hasNonBlockNonExpressionBody: false);
#nullable disable

        private BodyInfo(bool hasBlockBody, bool hasExpressionBody, bool hasNonBlockNonExpressionBody)
        {
            HasBlockBody = hasBlockBody;
            HasExpressionBody = hasExpressionBody;
            _hasNonBlockNonExpressionBody = hasNonBlockNonExpressionBody;
        }
    }
}
