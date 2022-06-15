// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommonLanguageServerProtocol.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILanguageServerFactory
    {
        public ILanguageServer Create(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IRoslynLspLogger logger);
    }
}
