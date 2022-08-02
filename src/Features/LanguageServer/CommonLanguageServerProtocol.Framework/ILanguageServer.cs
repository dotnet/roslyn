// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework;

// TODO: Seems like we might not need it? I thought it would be useful for creating your own LanguageServer but if we provide useful virtuals that shouldn't happen.
public interface ILanguageServer : IAsyncDisposable
{
}
