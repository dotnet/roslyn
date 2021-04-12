// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide a way for <see cref="IEventListener"/> to mark it as stoppable
    /// 
    /// for example, if the service <see cref="IEventListener"/> is used for is a disposable
    /// service, the service can call Stop when the service go away
    /// </summary>
    internal interface IEventListenerStoppable
    {
        void StopListening(Workspace workspace);
    }
}
