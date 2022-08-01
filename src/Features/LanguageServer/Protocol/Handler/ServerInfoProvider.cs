// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class ServerInfoProvider
    {
        public ServerInfoProvider(string serverKind, ImmutableArray<string> supportedLanguages)
        {
            ServerKind = serverKind;
            SupportedLanguages = supportedLanguages;
        }

        public readonly string ServerKind;
        public readonly ImmutableArray<string> SupportedLanguages;
    }
}
