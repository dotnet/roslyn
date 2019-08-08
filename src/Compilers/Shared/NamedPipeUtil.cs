// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The compiler needs to take advantage of features on named pipes which require target framework
    /// specific APIs. This class is meant to provide a simple, universal interface on top of the 
    /// multi-targeting code that is needed here.
    /// </summary>
    internal static class NamedPipeUtil
    {
        // Size of the buffers to use: 64K
        private const int PipeBufferSize = 0x10000;

        private static string GetPipeNameOrPath(string pipeName)
        {
            if (PlatformInformation.IsUnix)
            {
                // If we're on a Unix machine then named pipes are implemented using Unix Domain Sockets.
                // Most Unix systems have a maximum path length limit for Unix Domain Sockets, with
                // Mac having a particularly short one. Mac also has a generated temp directory that
                // can be quite long, leaving very little room for the actual pipe name. Fortunately,
                // '/tmp' is mandated by POSIX to always be a valid temp directory, so we can use that
                // instead.
                return Path.Combine("/tmp", pipeName);
            }
            else
            {
                return pipeName;
            }
        }

        /// <summary>
        /// Create a client for the current user only.
        /// </summary>
        internal static NamedPipeClientStream CreateClient(string serverName, string pipeName, PipeDirection direction, PipeOptions options)
            => new NamedPipeClientStream(serverName, GetPipeNameOrPath(pipeName), direction, options | CurrentUserOption);

        /// <summary>
        /// Does the client of "pipeStream" have the same identity and elevation as we do? The <see cref="CreateClient"/> and 
        /// <see cref="CreateServer(string)" /> methods will already guarantee that the identity of the client and server are the 
        /// same. This method is attempting to validate that the elevation level is the same between both ends of the 
        /// named pipe (want to disallow low priv session sending compilation requests to an elevated one).
        /// </summary>
        internal static bool CheckClientElevationMatches(NamedPipeServerStream pipeStream)
        {
            if (PlatformInformation.IsWindows)
            {
                var serverIdentity = getIdentity(impersonating: false);

                (string name, bool admin) clientIdentity = default;
                pipeStream.RunAsClient(() => { clientIdentity = getIdentity(impersonating: true); });

                return
                    StringComparer.OrdinalIgnoreCase.Equals(serverIdentity.name, clientIdentity.name) &&
                    serverIdentity.admin == clientIdentity.admin;

                (string name, bool admin) getIdentity(bool impersonating)
                {
                    var currentIdentity = WindowsIdentity.GetCurrent(impersonating);
                    var currentPrincipal = new WindowsPrincipal(currentIdentity);
                    var elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
                    return (currentIdentity.Name, elevatedToAdmin);
                }
            }

            return true;
        }

        /// <summary>
        /// Create a server for the current user only
        /// </summary>
        internal static NamedPipeServerStream CreateServer(string pipeName)
        {
            var pipeOptions = PipeOptions.Asynchronous | PipeOptions.WriteThrough;
            return CreateServer(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                pipeOptions,
                PipeBufferSize,
                PipeBufferSize);
        }

#if NET472

        const int s_currentUserOnlyValue = unchecked((int)0x20000000);

        /// <summary>
        /// Mono supports CurrentUserOnly even though it's not exposed on the reference assemblies for net472. This 
        /// must be used because ACL security does not work.
        /// </summary>
        private static PipeOptions CurrentUserOption = PlatformInformation.IsRunningOnMono
            ? (PipeOptions)s_currentUserOnlyValue
            : PipeOptions.None;

        private static NamedPipeServerStream CreateServer(
            string pipeName,
            PipeDirection direction,
            int maxNumberOfServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions options,
            int inBufferSize,
            int outBufferSize) =>
            new NamedPipeServerStream(
                GetPipeNameOrPath(pipeName),
                direction,
                maxNumberOfServerInstances,
                transmissionMode,
                options | CurrentUserOption,
                inBufferSize,
                outBufferSize,
                CreatePipeSecurity(),
                HandleInheritability.None);

        /// <summary>
        /// Check to ensure that the named pipe server we connected to is owned by the same
        /// user.
        /// </summary>
        internal static bool CheckPipeConnectionOwnership(NamedPipeClientStream pipeStream)
        {
            if (PlatformInformation.IsWindows)
            {
                var currentIdentity = WindowsIdentity.GetCurrent();
                var currentOwner = currentIdentity.Owner;
                var remotePipeSecurity = pipeStream.GetAccessControl();
                var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));
                return currentOwner.Equals(remoteOwner);
            }

            // Client side validation isn't supported on Unix. The model relies on the server side
            // security here.
            return true;
        }

        internal static PipeSecurity CreatePipeSecurity()
        {
            if (PlatformInformation.IsRunningOnMono)
            {
                // Pipe security and additional access rights constructor arguments
                //  are not supported by Mono 
                // https://github.com/dotnet/roslyn/pull/30810
                // https://github.com/mono/mono/issues/11406
                return null;
            }

            var security = new PipeSecurity();
            SecurityIdentifier identifier = WindowsIdentity.GetCurrent().Owner;

            // Restrict access to just this account.  
            PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
            security.AddAccessRule(rule);
            security.SetOwner(identifier);
            return security;
        }

#elif NETCOREAPP2_1 || NETCOREAPP3_0

        private static PipeOptions CurrentUserOption = PipeOptions.CurrentUserOnly;

        // Validation is handled by CurrentUserOnly
        internal static bool CheckPipeConnectionOwnership(NamedPipeClientStream pipeStream) => true;

        // Validation is handled by CurrentUserOnly
        internal static PipeSecurity CreatePipeSecurity() => null;

        private static NamedPipeServerStream CreateServer(
            string pipeName,
            PipeDirection direction,
            int maxNumberOfServerInstances,
            PipeTransmissionMode transmissionMode,
            PipeOptions options,
            int inBufferSize,
            int outBufferSize) =>
            new NamedPipeServerStream(
                GetPipeNameOrPath(pipeName),
                direction,
                maxNumberOfServerInstances,
                transmissionMode,
                options | CurrentUserOption,
                inBufferSize,
                outBufferSize);

#else
#error Unsupported configuration
#endif

    }
}
