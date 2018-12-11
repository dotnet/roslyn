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
using System.Security.AccessControl;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        // Size of the buffers to use: 64K
        private const int PipeBufferSize = 0x10000;

        private readonly ICompilerServerHost _compilerServerHost;
        private readonly string _pipeName;
        private int _loggingIdentifier;

        internal NamedPipeClientConnectionHost(ICompilerServerHost compilerServerHost, string pipeName)
        {
            _compilerServerHost = compilerServerHost;
            _pipeName = pipeName;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var pipeStream = await CreateListenTaskCore(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), pipeStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTaskCore(CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            NamedPipeServerStream pipeStream = ConstructPipe(_pipeName);

            CompilerServerLogger.Log("Waiting for new connection");
            await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            CompilerServerLogger.Log("Pipe connection detected.");

            if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
            {
                CompilerServerLogger.Log("Memory available - accepting connection");
                return pipeStream;
            }

            pipeStream.Close();
            throw new Exception("Insufficient resources to process new connection.");
        }

        /// <summary>
        /// Create an instance of the pipe. This might be the first instance, or a subsequent instance.
        /// There always needs to be an instance of the pipe created to listen for a new client connection.
        /// </summary>
        /// <returns>The pipe instance or throws an exception.</returns>
        private NamedPipeServerStream ConstructPipe(string pipeName)
        {
            CompilerServerLogger.Log("Constructing pipe '{0}'.", pipeName);

#if NET472
            PipeSecurity security;
            PipeOptions pipeOptions = PipeOptions.Asynchronous | PipeOptions.WriteThrough;

            if (!PlatformInformation.IsRunningOnMono)
            {
                security = new PipeSecurity();
                SecurityIdentifier identifier = WindowsIdentity.GetCurrent().Owner;

                // Restrict access to just this account.  
                PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
                security.AddAccessRule(rule);
                security.SetOwner(identifier);
            }
            else
            {
                // Pipe security and additional access rights constructor arguments
                //  are not supported by Mono 
                // https://github.com/dotnet/roslyn/pull/30810
                // https://github.com/mono/mono/issues/11406
                security = null;
                // This enum value is implemented by Mono to restrict pipe access to
                //  the current user
                const int CurrentUserOnly = unchecked((int)0x20000000);
                pipeOptions |= (PipeOptions)CurrentUserOnly;
            }

            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                pipeOptions,
                PipeBufferSize, // Default input buffer
                PipeBufferSize, // Default output buffer
                security,
                HandleInheritability.None);
#else
            // The overload of NamedPipeServerStream with the PipeAccessRule
            // parameter was removed in netstandard. However, the default
            // constructor does not provide WRITE_DAC, so attempting to use
            // SetAccessControl will always fail. So, completely ignore ACLs on
            // netcore, and trust that our `ClientAndOurIdentitiesMatch`
            // verification will catch any invalid connections.
            // Issue to add WRITE_DAC support:
            // https://github.com/dotnet/corefx/issues/24040
            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                PipeBufferSize, // Default input buffer
                PipeBufferSize);// Default output buffer
#endif

            CompilerServerLogger.Log("Successfully constructed pipe '{0}'.", pipeName);

            return pipeStream;
        }
    }

    internal sealed class NamedPipeClientConnection : ClientConnection
    {
        private readonly NamedPipeServerStream _pipeStream;

        internal NamedPipeClientConnection(ICompilerServerHost compilerServerHost, string loggingIdentifier, NamedPipeServerStream pipeStream)
            : base(compilerServerHost, loggingIdentifier, pipeStream)
        {
            _pipeStream = pipeStream;
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            return BuildServerConnection.CreateMonitorDisconnectTask(_pipeStream, LoggingIdentifier, cancellationToken);
        }

        protected override void ValidateBuildRequest(BuildRequest request)
        {
            // Now that we've read data from the stream we can validate the identity.
            if (!ClientAndOurIdentitiesMatch(_pipeStream))
            {
                throw new Exception("Client identity does not match server identity.");
            }
        }

        /// <summary>
        /// Does the client of "pipeStream" have the same identity and elevation as we do?
        /// </summary>
        private static bool ClientAndOurIdentitiesMatch(NamedPipeServerStream pipeStream)
        {
            if (PlatformInformation.IsWindows)
            {
                var serverIdentity = GetIdentity(impersonating: false);

                (string name, bool admin) clientIdentity = default;
                pipeStream.RunAsClient(() => { clientIdentity = GetIdentity(impersonating: true); });

                CompilerServerLogger.Log($"Server identity = '{serverIdentity.name}', server elevation='{serverIdentity.admin}'.");
                CompilerServerLogger.Log($"Client identity = '{clientIdentity.name}', client elevation='{serverIdentity.admin}'.");

                return
                    StringComparer.OrdinalIgnoreCase.Equals(serverIdentity.name, clientIdentity.name) &&
                    serverIdentity.admin == clientIdentity.admin;
            }
            else
            {
                return BuildServerConnection.CheckIdentityUnix(pipeStream);
            }
        }

        /// <summary>
        /// Return the current user name and whether the current user is in the administrator role.
        /// </summary>
        private static (string name, bool admin) GetIdentity(bool impersonating)
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent(impersonating);
            WindowsPrincipal currentPrincipal = new WindowsPrincipal(currentIdentity);
            var elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            return (currentIdentity.Name, elevatedToAdmin);
        }

        public override void Close()
        {
            CompilerServerLogger.Log($"Pipe {LoggingIdentifier}: Closing.");
            try
            {
                _pipeStream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format($"Pipe {LoggingIdentifier}: Error closing pipe.");
                CompilerServerLogger.LogException(e, msg);
            }
        }
    }
}
