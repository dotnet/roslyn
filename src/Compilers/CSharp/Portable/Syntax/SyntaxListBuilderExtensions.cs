// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxListBuilderExtensions
    {
        public static SyntaxTokenList ToTokenList(this SyntaxListBuilder builder)
        {
            if (builder == null || builder.Count == 0)
            {
                return default(SyntaxTokenList);
            }

            return new SyntaxTokenList(null, builder.ToListNode(), 0, 0);
        }
    }
}
