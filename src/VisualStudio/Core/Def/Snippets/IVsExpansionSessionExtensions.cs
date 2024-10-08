// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal static class IVsExpansionSessionExtensions
{
    public static bool TryGetHeaderNode(this IVsExpansionSession expansionSession, string name, [NotNullWhen(true)] out IXMLDOMNode? node)
    {
        var query = name is null ? null : $@"node()[local-name()=""{name}""]";

        IXMLDOMNode? localNode = null;
        if (!ErrorHandler.Succeeded(ErrorHandler.CallWithCOMConvention(() => expansionSession.GetHeaderNode(query, out localNode))))
        {
            node = null;
            return false;
        }

        node = localNode;
        return node is not null;
    }
}
