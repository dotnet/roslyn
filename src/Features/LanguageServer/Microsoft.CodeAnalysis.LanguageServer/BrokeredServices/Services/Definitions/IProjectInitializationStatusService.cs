// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.Definitions;
/// <summary>
/// Copied from https://devdiv.visualstudio.com/DevDiv/_git/CPS?path=/src/Microsoft.VisualStudio.ProjectSystem.Server/BrokerServices/IProjectInitializationStatusService.cs
/// </summary>
internal interface IProjectInitializationStatusService
{
    [JsonRpcMethod("subscribeInitializationCompletion")]
    ValueTask<IDisposable> SubscribeInitializationCompletionAsync(IObserver<ProjectInitializationCompletionState> observer, CancellationToken cancellationToken);
}
