// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
