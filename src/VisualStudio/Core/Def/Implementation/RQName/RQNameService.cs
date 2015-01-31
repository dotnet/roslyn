// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.RQName
{
    internal static class RQNameService
    {
        public static bool TryBuild(ISymbol symbol, out string rqname)
        {
            var node = RQNodeBuilder.Build(symbol);
            rqname = (node != null) ? ParenthesesTreeWriter.ToParenthesesFormat(node.ToSimpleTree()) : null;

            return node != null;
        }

        public static bool TryBuildForPublicAPIs(ISymbol symbol, out string rqname)
        {
            var node = RQNodeBuilder.Build(symbol, buildForPublicAPIs: true);
            rqname = (node != null) ? ParenthesesTreeWriter.ToParenthesesFormat(node.ToSimpleTree()) : null;

            return node != null;
        }
    }
}
