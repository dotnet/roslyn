// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Abstraction over the connection to the client process.   This hides underlying connection
    /// to facilitate better testing. 
    /// </summary>
    internal interface IClientConnection : IDisposable
    {
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
        /// <summary>
        /// True when the host is listening for new connections (after <see cref="BeginListening"/> is
        /// called but before <see cref="EndListening"/> is called).
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Start listening for new connections
        /// </summary>
        void BeginListening();

        /// <summary>
        /// Returns a <see cref="Task"/> that completes when a new <see cref="IClientConnection"/> is 
        /// received. If this is called after <see cref="EndListening"/> is called then an exception
        /// will be thrown.
        /// </summary>
        Task<IClientConnection> GetNextClientConnectionAsync();

        /// <summary>
        /// Stop accepting new connections. It will also ensure that the last return from 
        /// <see cref="GetNextClientConnectionAsync"/> is either already in a completed state, or has scheduled an
        /// operation which will transition the task to a completed state.
        /// </summary>
        void EndListening();
    }
}
