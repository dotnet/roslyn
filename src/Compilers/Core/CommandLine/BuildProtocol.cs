// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CommandLine.BuildProtocolConstants;
using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;

// This file describes data structures about the protocol from client program to server that is 
// used. The basic protocol is this.
//
// After the server pipe is connected, it forks off a thread to handle the connection, and creates
// a new instance of the pipe to listen for new clients. When it gets a request, it validates
// the security and elevation level of the client. If that fails, it disconnects the client. Otherwise,
// it handles the request, sends a response (described by Response class) back to the client, then
// disconnects the pipe and ends the thread.

namespace Microsoft.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Represents a request from the client. A request is as follows.
    /// 
    ///  Field Name         Type                Size (bytes)
    /// ----------------------------------------------------
    ///  Length             Integer             4
    ///  RequestId          Guid                16
    ///  Language           RequestLanguage     4
    ///  CompilerHash       String              Variable
    ///  Argument Count     UInteger            4
    ///  Arguments          Argument[]          Variable
    /// 
    /// See <see cref="Argument"/> for the format of an
    /// Argument.
    /// 
    /// </summary>
    internal sealed class BuildRequest
    {
        /// <summary>
        /// The maximum size of a request supported by the compiler server.
        /// </summary>
        /// <remarks>
        /// Currently this limit is 5MB.
        /// </remarks>
        private const int MaximumRequestSize = 0x500000;

        public readonly Guid RequestId;
        public readonly RequestLanguage Language;
        public readonly ReadOnlyCollection<Argument> Arguments;
        public readonly string CompilerHash;

        public BuildRequest(RequestLanguage language,
                            string compilerHash,
                            IEnumerable<Argument> arguments,
                            Guid? requestId = null)
        {
            RequestId = requestId ?? Guid.Empty;
            Language = language;
            Arguments = new ReadOnlyCollection<Argument>(arguments.ToList());
            CompilerHash = compilerHash;

            Debug.Assert(!string.IsNullOrWhiteSpace(CompilerHash), "A hash value is required to communicate with the server");
        }

        public static BuildRequest Create(RequestLanguage language,
                                          IList<string> args,
                                          string workingDirectory,
                                          string? tempDirectory,
                                          string compilerHash,
                                          Guid? requestId = null,
                                          string? keepAlive = null,
                                          string? libDirectory = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(compilerHash), "CompilerHash is required to send request to the build server");

            var requestLength = args.Count + 1 + (libDirectory == null ? 0 : 1);
            var requestArgs = new List<Argument>(requestLength);

            requestArgs.Add(new Argument(ArgumentId.CurrentDirectory, 0, workingDirectory));
            requestArgs.Add(new Argument(ArgumentId.TempDirectory, 0, tempDirectory));

            if (keepAlive != null)
            {
                requestArgs.Add(new Argument(ArgumentId.KeepAlive, 0, keepAlive));
            }

            if (libDirectory != null)
            {
                requestArgs.Add(new Argument(ArgumentId.LibEnvVariable, 0, libDirectory));
            }

            for (int i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                requestArgs.Add(new Argument(ArgumentId.CommandLineArgument, i, arg));
            }

            return new BuildRequest(language, compilerHash, requestArgs, requestId);
        }

        public static BuildRequest CreateShutdown()
        {
            var requestArgs = new[] { new Argument(ArgumentId.Shutdown, argumentIndex: 0, value: "") };
            return new BuildRequest(RequestLanguage.CSharpCompile, GetCommitHash() ?? "", requestArgs);
        }

        /// <summary>
        /// Read a Request from the given stream.
        /// 
        /// The total request size must be less than <see cref="MaximumRequestSize"/>.
        /// </summary>
        /// <returns>null if the Request was too large, the Request otherwise.</returns>
        public static async Task<BuildRequest> ReadAsync(Stream inStream, CancellationToken cancellationToken)
        {
            // Read the length of the request
            var lengthBuffer = new byte[4];
            await ReadAllAsync(inStream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToInt32(lengthBuffer, 0);

            // Back out if the request is too large
            if (length > MaximumRequestSize)
            {
                throw new ArgumentException($"Request is over {MaximumRequestSize >> 20}MB in length");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Read the full request
            var requestBuffer = new byte[length];
            await ReadAllAsync(inStream, requestBuffer, length, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Parse the request into the Request data structure.
            using var reader = new BinaryReader(new MemoryStream(requestBuffer), Encoding.Unicode);
            var requestId = readGuid(reader);
            var language = (RequestLanguage)reader.ReadUInt32();
            var compilerHash = reader.ReadString();
            uint argumentCount = reader.ReadUInt32();
            var argumentsBuilder = new List<Argument>((int)argumentCount);

            for (int i = 0; i < argumentCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                argumentsBuilder.Add(BuildRequest.Argument.ReadFromBinaryReader(reader));
            }

            return new BuildRequest(language,
                                    compilerHash,
                                    argumentsBuilder,
                                    requestId);

            static Guid readGuid(BinaryReader reader)
            {
                const int size = 16;
                var bytes = new byte[size];
                if (size != reader.Read(bytes, 0, size))
                {
                    throw new InvalidOperationException();
                }

                return new Guid(bytes);
            }
        }

        /// <summary>
        /// Write a Request to the stream.
        /// </summary>
        public async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream, Encoding.Unicode);
            writer.Write(RequestId.ToByteArray());
            writer.Write((uint)Language);
            writer.Write(CompilerHash);
            writer.Write(Arguments.Count);
            foreach (Argument arg in Arguments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                arg.WriteToBinaryWriter(writer);
            }
            writer.Flush();

            cancellationToken.ThrowIfCancellationRequested();

            // Write the length of the request
            int length = checked((int)memoryStream.Length);

            // Back out if the request is too large
            if (memoryStream.Length > MaximumRequestSize)
            {
                throw new ArgumentOutOfRangeException($"Request is over {MaximumRequestSize >> 20}MB in length");
            }

            await outStream.WriteAsync(BitConverter.GetBytes(length), 0, 4,
                                       cancellationToken).ConfigureAwait(false);

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// A command line argument to the compilation. 
        /// An argument is formatted as follows:
        /// 
        ///  Field Name         Type            Size (bytes)
        /// --------------------------------------------------
        ///  ID                 UInteger        4
        ///  Index              UInteger        4
        ///  Value              String          Variable
        /// 
        /// Strings are encoded via a length prefix as a signed
        /// 32-bit integer, followed by an array of characters.
        /// </summary>
        public readonly struct Argument
        {
            public readonly ArgumentId ArgumentId;
            public readonly int ArgumentIndex;
            public readonly string? Value;

            public Argument(ArgumentId argumentId,
                            int argumentIndex,
                            string? value)
            {
                ArgumentId = argumentId;
                ArgumentIndex = argumentIndex;
                Value = value;
            }

            public static Argument ReadFromBinaryReader(BinaryReader reader)
            {
                var argId = (ArgumentId)reader.ReadInt32();
                var argIndex = reader.ReadInt32();
                string? value = ReadLengthPrefixedString(reader);
                return new Argument(argId, argIndex, value);
            }

            public void WriteToBinaryWriter(BinaryWriter writer)
            {
                writer.Write((int)ArgumentId);
                writer.Write(ArgumentIndex);
                WriteLengthPrefixedString(writer, Value);
            }
        }
    }

    /// <summary>
    /// Base class for all possible responses to a request.
    /// The ResponseType enum should list all possible response types
    /// and ReadResponse creates the appropriate response subclass based
    /// on the response type sent by the client.
    /// The format of a response is:
    ///
    /// Field Name       Field Type          Size (bytes)
    /// -------------------------------------------------
    /// responseLength   int (positive)      4  
    /// responseType     enum ResponseType   4
    /// responseBody     Response subclass   variable
    /// </summary>
    internal abstract class BuildResponse
    {
        public enum ResponseType
        {
            // The client and server are using incompatible protocol versions.
            MismatchedVersion,

            // The build request completed on the server and the results are contained
            // in the message. 
            Completed,

            // The build request could not be run on the server due because it created
            // an unresolvable inconsistency with analyzers.  
            AnalyzerInconsistency,

            // The shutdown request completed and the server process information is 
            // contained in the message. 
            Shutdown,

            // The request was rejected by the server.  
            Rejected,

            // The server hash did not match the one supplied by the client
            IncorrectHash,

            // Cannot connect to the server
            CannotConnect,
        }

        public abstract ResponseType Type { get; }

        public async Task WriteAsync(Stream outStream,
                               CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
            {
                writer.Write((int)Type);

                AddResponseBody(writer);
                writer.Flush();

                cancellationToken.ThrowIfCancellationRequested();

                // Send the response to the client

                // Write the length of the response
                int length = checked((int)memoryStream.Length);

                // There is no way to know the number of bytes written to
                // the pipe stream. We just have to assume all of them are written.
                await outStream.WriteAsync(BitConverter.GetBytes(length),
                                           0,
                                           4,
                                           cancellationToken).ConfigureAwait(false);

                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract void AddResponseBody(BinaryWriter writer);

        /// <summary>
        /// May throw exceptions if there are pipe problems.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<BuildResponse> ReadAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Read the response length
            var lengthBuffer = new byte[4];
            await ReadAllAsync(stream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToUInt32(lengthBuffer, 0);

            // Read the response
            var responseBuffer = new byte[length];
            await ReadAllAsync(stream,
                               responseBuffer,
                               responseBuffer.Length,
                               cancellationToken).ConfigureAwait(false);

            using (var reader = new BinaryReader(new MemoryStream(responseBuffer), Encoding.Unicode))
            {
                var responseType = (ResponseType)reader.ReadInt32();

                switch (responseType)
                {
                    case ResponseType.Completed:
                        return CompletedBuildResponse.Create(reader);
                    case ResponseType.MismatchedVersion:
                        return new MismatchedVersionBuildResponse();
                    case ResponseType.IncorrectHash:
                        return new IncorrectHashBuildResponse();
                    case ResponseType.AnalyzerInconsistency:
                        return AnalyzerInconsistencyBuildResponse.Create(reader);
                    case ResponseType.Shutdown:
                        return ShutdownBuildResponse.Create(reader);
                    case ResponseType.Rejected:
                        return RejectedBuildResponse.Create(reader);

                    // Intentional fall through
                    case ResponseType.CannotConnect:
                    default:
                        throw new InvalidOperationException("Received invalid response type from server.");
                }
            }
        }
    }

    /// <summary>
    /// Represents a Response from the server. A response is as follows.
    /// 
    ///  Field Name         Type            Size (bytes)
    /// --------------------------------------------------
    ///  Length             UInteger        4
    ///  ReturnCode         Integer         4
    ///  Output             String          Variable
    /// 
    /// Strings are encoded via a character count prefix as a 
    /// 32-bit integer, followed by an array of characters.
    /// 
    /// </summary>
    internal sealed class CompletedBuildResponse : BuildResponse
    {
        public readonly int ReturnCode;
        public readonly bool Utf8Output;
        public readonly string Output;

        public CompletedBuildResponse(int returnCode,
                                      bool utf8output,
                                      string? output)
        {
            ReturnCode = returnCode;
            Utf8Output = utf8output;
            Output = output ?? string.Empty;
        }

        public override ResponseType Type => ResponseType.Completed;

        public static CompletedBuildResponse Create(BinaryReader reader)
        {
            var returnCode = reader.ReadInt32();
            var utf8Output = reader.ReadBoolean();
            var output = ReadLengthPrefixedString(reader);
            return new CompletedBuildResponse(returnCode, utf8Output, output);
        }

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ReturnCode);
            writer.Write(Utf8Output);
            WriteLengthPrefixedString(writer, Output);
        }
    }

    internal sealed class ShutdownBuildResponse : BuildResponse
    {
        public readonly int ServerProcessId;

        public ShutdownBuildResponse(int serverProcessId)
        {
            ServerProcessId = serverProcessId;
        }

        public override ResponseType Type => ResponseType.Shutdown;

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ServerProcessId);
        }

        public static ShutdownBuildResponse Create(BinaryReader reader)
        {
            var serverProcessId = reader.ReadInt32();
            return new ShutdownBuildResponse(serverProcessId);
        }
    }

    file sealed class MismatchedVersionBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.MismatchedVersion;

        /// <summary>
        /// MismatchedVersion has no body.
        /// </summary>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    internal sealed class IncorrectHashBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.IncorrectHash;

        /// <summary>
        /// IncorrectHash has no body.
        /// </summary>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    internal sealed class AnalyzerInconsistencyBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.AnalyzerInconsistency;

        public ReadOnlyCollection<string> ErrorMessages { get; }

        public AnalyzerInconsistencyBuildResponse(ReadOnlyCollection<string> errorMessages)
        {
            ErrorMessages = errorMessages;
        }

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ErrorMessages.Count);
            foreach (var message in ErrorMessages)
            {
                WriteLengthPrefixedString(writer, message);
            }
        }

        public static AnalyzerInconsistencyBuildResponse Create(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var list = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(ReadLengthPrefixedString(reader) ?? "");
            }

            return new AnalyzerInconsistencyBuildResponse(new ReadOnlyCollection<string>(list));
        }
    }

    /// <summary>
    /// The <see cref="BuildRequest"/> was rejected by the server.
    /// </summary>
    internal sealed class RejectedBuildResponse : BuildResponse
    {
        public string Reason;

        public override ResponseType Type => ResponseType.Rejected;

        public RejectedBuildResponse(string reason)
        {
            Reason = reason;
        }

        protected override void AddResponseBody(BinaryWriter writer)
        {
            WriteLengthPrefixedString(writer, Reason);
        }

        public static RejectedBuildResponse Create(BinaryReader reader)
        {
            var reason = ReadLengthPrefixedString(reader);
            Debug.Assert(reason is object);
            return new RejectedBuildResponse(reason);
        }
    }

    /// <summary>
    /// Used when the client cannot connect to the server.
    /// </summary>
    internal sealed class CannotConnectResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.CannotConnect;

        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    // The id numbers below are just random. It's useful to use id numbers
    // that won't occur accidentally for debugging.
    internal enum RequestLanguage
    {
        CSharpCompile = 0x44532521,
        VisualBasicCompile = 0x44532522,
    }

    /// <summary>
    /// Constants about the protocol.
    /// </summary>
    internal static class BuildProtocolConstants
    {
        // Arguments for CSharp and VB Compiler
        public enum ArgumentId
        {
            // The current directory of the client
            CurrentDirectory = 0x51147221,

            // A comment line argument. The argument index indicates which one (0 .. N)
            CommandLineArgument,

            // The "LIB" environment variable of the client
            LibEnvVariable,

            // Request a longer keep alive time for the server
            KeepAlive,

            // Request a server shutdown from the client
            Shutdown,

            // The directory to use for temporary operations.
            TempDirectory,
        }

        /// <summary>
        /// Read a string from the Reader where the string is encoded
        /// as a length prefix (signed 32-bit integer) followed by
        /// a sequence of characters.
        /// </summary>
        public static string? ReadLengthPrefixedString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 0)
            {
                return null;
            }

            return new String(reader.ReadChars(length));
        }

        /// <summary>
        /// Write a string to the Writer where the string is encoded
        /// as a length prefix (signed 32-bit integer) follows by
        /// a sequence of characters.
        /// </summary>
        public static void WriteLengthPrefixedString(BinaryWriter writer, string? value)
        {
            if (value is object)
            {
                writer.Write(value.Length);
                writer.Write(value.ToCharArray());
            }
            else
            {
                writer.Write(-1);
            }
        }

        /// <summary>
        /// Reads the value of <see cref="CommitHashAttribute.Hash"/> of the assembly <see cref="BuildRequest"/> is defined in
        /// </summary>
        /// <returns>The hash value of the current assembly or an empty string</returns>
        public static string? GetCommitHash()
        {
            var hashAttributes = typeof(BuildRequest).Assembly.GetCustomAttributes<CommitHashAttribute>();
            var hashAttributeCount = hashAttributes.Count();
            if (hashAttributeCount != 1)
            {
                return null;
            }
            return hashAttributes.Single().Hash;
        }

        /// <summary>
        /// This task does not complete until we are completely done reading.
        /// </summary>
        internal static async Task ReadAllAsync(
            Stream stream,
            byte[] buffer,
            int count,
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            do
            {
                int bytesRead = await stream.ReadAsync(buffer,
                                                       totalBytesRead,
                                                       count - totalBytesRead,
                                                       cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("Reached end of stream before end of read.");
                }
                totalBytesRead += bytesRead;
            } while (totalBytesRead < count);
        }
    }
}
