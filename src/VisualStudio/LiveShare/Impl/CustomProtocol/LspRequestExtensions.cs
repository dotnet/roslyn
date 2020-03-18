﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
