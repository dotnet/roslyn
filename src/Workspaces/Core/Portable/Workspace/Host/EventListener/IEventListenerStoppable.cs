// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
