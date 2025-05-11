// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Base data type for all document based resolve handlers that stores the <see cref="TextDocumentIdentifier"/> for the resolve request.
/// </summary>
/// <param name="TextDocument">the text document associated with the request to resolve.</param>
internal record DocumentResolveData(TextDocumentIdentifier TextDocument);
