﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        Task<ConnectionData> HandleConnection(bool allowCompilationRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Close the underlying client connection.
        /// </summary>
        void Close();
    }
}
