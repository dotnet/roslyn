// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol
{
    public static class LspRequestExtensions
    {
        public static LS.LspRequest<TIn, TOut> ToLSRequest<TIn, TOut>(this LSP.LspRequest<TIn, TOut> lspRequest)
        {
            return new LS.LspRequest<TIn, TOut>(lspRequest.Name);
        }

    }
}
