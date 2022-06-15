// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorLanguageServerTargetWrapper : IRazorLanguageServerTarget
    {
        private readonly ILanguageServer _languageServer;

        public RazorLanguageServerTargetWrapper(ILanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public ValueTask DisposeAsync() => _languageServer.DisposeAsync();
    }
}
