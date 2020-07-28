// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

#nullable enable

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Abstraction over the connection to the client process.   This hides underlying connection
    /// to facilitate better testing. 
    /// </summary>
    internal interface IClientConnection : IDisposable
    {
        /// <summary>
        /// A value which can be used to identify this connection for logging purposes only.  It has 
        /// no guarantee of uniqueness.  
        /// </summary>
        string LoggingIdentifier { get; }

        /// <summary>
        /// This task resolves if the client disconnects from the server.
        /// </summary>
        Task DisconnectTask { get; }

        /// <summary>
        /// Read a <see cref="BuildRequest" /> from the client
        /// </summary>
        Task<BuildRequest> ReadBuildRequestAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Write a <see cref="BuildResponse" /> to the client
        /// </summary>
        Task WriteBuildResponseAsync(BuildResponse response, CancellationToken cancellationToken);
    }

    internal interface IClientConnectionHost
    {
        Task<IClientConnection> ListenAsync(CancellationToken cancellationToken);
    }

}
