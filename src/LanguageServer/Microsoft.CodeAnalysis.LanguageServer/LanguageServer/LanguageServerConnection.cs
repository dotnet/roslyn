// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A single transport connection between an editor (or thin client) and a language server: a pair of
/// input/output streams plus an optional resource to dispose when the server for this connection exits.
/// </summary>
internal readonly record struct LanguageServerConnection(Stream InputStream, Stream OutputStream, IDisposable? Resource = null);
