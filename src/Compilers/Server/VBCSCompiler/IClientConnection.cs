// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Abstraction over the connection to the client process.   This hides underlying connection
    /// to facilitate better testing. 
    /// </summary>
    internal interface IClientConnection
    {
        /// <summary>
        /// A value which can be used to identify this connection for logging purposes only.  It has 
        /// no guarantee of uniqueness.  
        /// </summary>
        string LoggingIdentifier { get; }

        /// <summary>
        /// Server the connection and return the result.
        /// </summary>
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        Task<ConnectionData> HandleConnection(bool allowCompilationRequests, CancellationToken cancellationToken);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        /// <summary>
        /// Close the underlying client connection.
        /// </summary>
        void Close();
    }
}
