// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface IRequestHandlerProviderMetadata
    {
        /// <summary>
        /// Name of the language for LSP method to handle (optional).
        /// </summary>
        string? LanguageName { get; }
    }
}
