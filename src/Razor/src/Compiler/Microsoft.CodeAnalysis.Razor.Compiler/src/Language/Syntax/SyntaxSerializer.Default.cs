// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxSerializer
{
    public sealed class Default : SyntaxSerializer
    {
        private Default(StringBuilder builder)
            : base(builder)
        {
        }

        internal static string Serialize(RazorSyntaxNode node)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            var serializer = new Default(builder);
            serializer.Visit(node);

            return builder.ToString();
        }

        internal static string Serialize(SyntaxToken token)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);
            var serializer = new Default(builder);
            serializer.VisitToken(token);

            return builder.ToString();
        }
    }
}
