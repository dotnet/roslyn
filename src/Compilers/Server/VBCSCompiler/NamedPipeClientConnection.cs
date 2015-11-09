// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnection : IClientConnection
    {
        private readonly NamedPipeServerStream _pipeStream;

        // This is a value used for logging only, do not depend on this value
        private readonly string _loggingIdentifier;
        private static int s_lastLoggingIdentifier;

        internal NamedPipeClientConnection(NamedPipeServerStream pipeStream)
        {
            _pipeStream = pipeStream;
            _loggingIdentifier = Interlocked.Increment(ref s_lastLoggingIdentifier).ToString();
        }

        public string LoggingIdentifier
        {
            get { return _loggingIdentifier; }
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        private async Task<bool> CreateMonitorDisconnectTaskCore(CancellationToken cancellationToken)
        {
            var buffer = SpecializedCollections.EmptyBytes;

            while (!cancellationToken.IsCancellationRequested && _pipeStream.IsConnected)
            {
                // Wait a second before trying again
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                try
                {
                    CompilerServerLogger.Log("Pipe {0}: Before poking pipe.", _loggingIdentifier);
                    await _pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                    CompilerServerLogger.Log("Pipe {0}: After poking pipe.", _loggingIdentifier);
                }
                catch (Exception e)
                {
                    // It is okay for this call to fail.  Errors will be reflected in the 
                    // IsConnected property which will be read on the next iteration of the 
                    // loop
                    var msg = string.Format("Pipe {0}: Error poking pipe.", _loggingIdentifier);
                    CompilerServerLogger.LogException(e, msg);
                }
            }

            return !_pipeStream.IsConnected;
        }

        /// <summary>
        /// Does the client of "pipeStream" have the same identity and elevation as we do?
        /// </summary>
        private bool ClientAndOurIdentitiesMatch()
        {
            var serverIdentity = GetIdentity(impersonating: false);

            Tuple<string, bool> clientIdentity = null;
            _pipeStream.RunAsClient(() => { clientIdentity = GetIdentity(impersonating: true); });

            CompilerServerLogger.Log(
                "Pipe {0}: Server identity = '{1}', server elevation='{2}'.",
                _loggingIdentifier,
                serverIdentity.Item1,
                serverIdentity.Item2.ToString());
            CompilerServerLogger.Log(
                "Pipe {0}: Client identity = '{1}', client elevation='{2}'.",
                _loggingIdentifier,
                clientIdentity.Item1,
                clientIdentity.Item2.ToString());

            return
                StringComparer.OrdinalIgnoreCase.Equals(serverIdentity.Item1, clientIdentity.Item1) &&
                serverIdentity.Item2 == clientIdentity.Item2;
        }

        /// <summary>
        /// Return the current user name and whether the current user is in the administrator role.
        /// </summary>
        private static Tuple<string, bool> GetIdentity(bool impersonating)
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent(impersonating);
            WindowsPrincipal currentPrincipal = new WindowsPrincipal(currentIdentity);
            var elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            return Tuple.Create(currentIdentity.Name, elevatedToAdmin);
        }

        public void Close()
        {
            CompilerServerLogger.Log("Pipe {0}: Closing.", _loggingIdentifier);
            try
            {
                _pipeStream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format("Pipe {0}: Error closing pipe.", _loggingIdentifier);
                CompilerServerLogger.LogException(e, msg);
            }
        }

        public Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            return CreateMonitorDisconnectTaskCore(cancellationToken);
        }

        public async Task<BuildRequest> ReadBuildRequest(CancellationToken cancellationToken)
        {
            var buildRequest = await BuildRequest.ReadAsync(_pipeStream, cancellationToken).ConfigureAwait(false);
            if (!ClientAndOurIdentitiesMatch())
            {
                throw new Exception("Client identity does not match server identity.");
            }

            return buildRequest;
        }

        public Task WriteBuildResponse(BuildResponse response, CancellationToken cancellationToken)
        {
            return response.WriteAsync(_pipeStream, cancellationToken);
        }
    }
}
