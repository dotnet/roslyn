// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A service that let people to register new IDiagnosticUpdateSource
    /// </summary>
    internal interface IDiagnosticUpdateSourceRegistrationService
    {
        /// <summary>
        /// Register new IDiagnosticUpdateSource
        /// 
        /// Currently, it doesn't support unregister since our event is asynchronous and unregistering source that deal with asynchronous event is not straight forward.
        /// </summary>
        void Register(IDiagnosticUpdateSource source);
    }
}
