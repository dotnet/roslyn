// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class UnixDomainSocket : IDisposable
    {
        private readonly Socket _serverSocket;

        private UnixDomainSocket(Socket serverSocket)
        {
            _serverSocket = serverSocket;
        }

        public void Dispose()
        {
            _serverSocket.Dispose();
        }

        // Internal type from corefx:src/System.IO.Pipes/src/System/Net/Sockets/UnixDomainSocketEndPoint.cs
        private static EndPoint CreateUnixDomainSocketEndPoint(string path)
        {
            var type = Assembly.Load(new AssemblyName("System.IO.Pipes"))
                .GetType("System.Net.Sockets.UnixDomainSocketEndPoint");
            return (EndPoint)Activator.CreateInstance(type, path);
        }

        // Copied from corefx:src/Common/src/Interop/Unix/System.Native/Interop.GetPeerID.cs
        [DllImport("System.Native", EntryPoint = "SystemNative_GetPeerID", SetLastError = true)]
        private static extern int GetPeerID(IntPtr socket, out uint euid);

        private static uint GetPeerID(Socket socket)
        {
#if NETSTANDARD1_3
            var handle = (IntPtr)socket
                .GetType()
                .GetTypeInfo()
                .GetDeclaredProperty("Handle")
                .GetMethod
                .Invoke(socket, parameters: null);
#else
            var handle = socket.Handle;
#endif
            var result = GetPeerID(handle, out var euid);
            if (result != 0)
            {
                throw new Exception($"getsockopt(SO_PEERCRED) or getpeereid failed ({result})");
            }
            return euid;
        }

        // Copied from corefx:src/Common/src/Interop/Unix/System.Native/Interop.GetEUid.cs
        [DllImport("System.Native", EntryPoint = "SystemNative_GetEUid")]
        internal static extern uint GetEUid();

        private static string NameToPath(string pipeName)
        {
            return Path.Combine(Path.GetTempPath(), pipeName);
        }

        private static bool Check(Socket connection)
        {
            // TODO (SystemNative_GetPeerID seems to be missing???)
            return true;
            // var myId = GetEUid();
            // var theirId = GetPeerID(connection);
            // return myId == theirId;
        }

        public static UnixDomainSocket CreateServer(string pipeName)
        {
            var path = NameToPath(pipeName);
            File.Delete(path);
            var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            server.Bind(CreateUnixDomainSocketEndPoint(path));
            server.Listen(2);
            return new UnixDomainSocket(server);
        }

        public async Task<NetworkStream> WaitOne()
        {
            while (true)
            {
                var acceptedSocket = await SocketHelper.AcceptAsync(_serverSocket).ConfigureAwait(false);
                if (!Check(acceptedSocket))
                {
                    acceptedSocket.Dispose();
                    continue;
                }
                var stream = new NetworkStream(acceptedSocket, true);
                return stream;
            }
        }

        public static Stream CreateClient(string pipeName)
        {
            var path = NameToPath(pipeName);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                socket.Connect(CreateUnixDomainSocketEndPoint(path));
            }
            catch
            {
                return null;
            }
            if (!Check(socket))
            {
                return null;
            }
            var stream = new NetworkStream(socket);
            return stream;
        }
    }

    internal static class SocketHelper
    {
        // Copied from corefx:src/System.Net.Sockets/src/System/Net/Sockets/SocketTaskExtensions.netfx.cs
        internal static Task<Socket> AcceptAsync(Socket socket)
        {
#if NET46
            return Task<Socket>.Factory.FromAsync(
                (callback, state) => ((Socket)state).BeginAccept(callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndAccept(asyncResult),
                state: socket);
#else
            return socket.AcceptAsync();
#endif
        }

        // Copied from corefx:src/System.Net.Sockets/src/System/Net/Sockets/SocketTaskExtensions.netfx.cs
        internal static Task<int> ReceiveAsync(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
#if NET46
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) => ((Socket)state).BeginReceive(
                                                              targetBuffer.Array,
                                                              targetBuffer.Offset,
                                                              targetBuffer.Count,
                                                              flags,
                                                              callback,
                                                              state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndReceive(asyncResult),
                buffer,
                socketFlags,
                state: socket);
#else
            return socket.ReceiveAsync(buffer, socketFlags);
#endif
        }
    }
}
