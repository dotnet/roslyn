// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide a way for features to lazily subscribe to a service event for particular workspace
    /// 
    /// see <see cref="WellKnownEventListeners"/> for supported services
    /// </summary>
    internal interface IEventListener
    {
    }
}
