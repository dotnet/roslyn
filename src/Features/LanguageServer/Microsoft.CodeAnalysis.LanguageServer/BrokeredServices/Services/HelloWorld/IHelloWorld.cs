// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ServiceHub.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;

/// <summary>
/// A simple interface used for sanity checking that the brokered service bridge works.
/// There is an implementation of the same service on the green side that we can talk to.
/// </summary>
internal interface IHelloWorld
{
    [JsonRpcMethod("sayHello")]
    Task<string> SayHelloAsync(string name, CancellationToken cancellationToken);

    [JsonRpcMethod("callMe")]
    Task<string> CallMeAsync(ServiceMoniker serviceMoniker, CancellationToken cancellationToken);
}
