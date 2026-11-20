// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Used to log information from within the compiler server
    /// </summary>
    /// <remarks>
    /// Implementations of this interface must assume they are used on multiple threads without any form
    /// of synchronization.
    /// </remarks>
    internal interface ICompilerServerLogger
    {
        bool IsLogging { get; }
        void Log(string message);
    }

    internal static class CompilerServerLoggerExtensions
    {
        internal static void Log(this ICompilerServerLogger logger, string format, params object?[] arguments)
        {
            if (logger.IsLogging)
            {
                logger.Log(string.Format(format, arguments));
            }
        }

        internal static void LogError(this ICompilerServerLogger logger, string message)
        {
            if (logger.IsLogging)
            {
                logger.Log($"Error: {message}");
            }
        }

        internal static void LogError(this ICompilerServerLogger logger, string format, params object?[] arguments)
        {
            if (logger.IsLogging)
            {
                logger.Log($"Error: {format}", arguments);
            }
        }

        /// <summary>
        /// Log an exception. Also logs information about inner exceptions.
        /// </summary>
        internal static void LogException(this ICompilerServerLogger logger, Exception exception, string reason)
        {
            if (!logger.IsLogging)
            {
                return;
            }

            var builder = new StringBuilder();
            AppendException(exception);

            int innerExceptionLevel = 0;
            Exception? e = exception.InnerException;
            while (e != null)
            {
                builder.Append($"Inner exception[{innerExceptionLevel}]  ");
                AppendException(e);
                e = e.InnerException;
                innerExceptionLevel += 1;
            }

            logger.Log(builder.ToString());

            void AppendException(Exception exception)
            {
                builder.AppendLine($"Exception: '{exception.GetType().Name}' '{exception.Message}' occurred during '{reason}'");
                builder.AppendLine("Stack trace:");
                builder.AppendLine(exception.StackTrace);
            }
        }
    }

    /// <summary>
    /// Class for logging information about what happens in the server and client parts of the 
    /// Roslyn command line compiler and build tasks. Useful for debugging what is going on.
    /// </summary>
    /// <remarks>
    /// To use the logging, set the environment variable RoslynCommandLineLogFile to the name
    /// of a file to log to. This file is logged to by both client and server components.
    /// </remarks>
    internal sealed class CompilerServerLogger : ICompilerServerLogger, IDisposable
    {
        // Environment variable, if set, to enable logging and set the file to log to.
        internal const string EnvironmentVariableName = "RoslynCommandLineLogFile";

        private Stream? _loggingStream;
        private readonly string _identifier;

        public bool IsLogging => _loggingStream is object;

        public CompilerServerLogger(string identifier, IBuildEnvironment buildEnvironment)
            : this(identifier, GetLoggingFilePath(buildEnvironment))
        {
        }

        /// <summary>
        /// Initializes logging using the supplied environment variable lookup and path absolutization.
        /// </summary>
        public CompilerServerLogger(string identifier, string? loggingFilePath)
        {
            Debug.Assert(loggingFilePath is null || Path.IsPathFullyQualified(loggingFilePath));
            _identifier = identifier;
            try
            {
                if (loggingFilePath is not null)
                {
                    // Open allowing sharing. We allow multiple processes to log to the same file, so we use share mode to allow that.
#pragma warning disable RS0030 // getFullPath guarantees a full path
                    _loggingStream = new FileStream(loggingFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
#pragma warning restore RS0030
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
        }

        public void Dispose()
        {
            _loggingStream?.Dispose();
            _loggingStream = null;
        }

        public void Log(string message)
        {
            if (_loggingStream is object)
            {
                var threadId = Environment.CurrentManagedThreadId;
                var prefix = $"[{DateTimeOffset.Now:o}] ID={_identifier} TID={threadId}: ";
                string output = prefix + message + Environment.NewLine;
                byte[] bytes = Encoding.UTF8.GetBytes(output);

                // Because multiple processes might be logging to the same file, we always seek to the end,
                // write, and flush.
                _loggingStream.Seek(0, SeekOrigin.End);
                _loggingStream.Write(bytes, 0, bytes.Length);
                _loggingStream.Flush();
            }
        }

        /// <summary>
        /// Get the fully qualified file path for the log file to use for logging. Returns null if logging is not enabled.
        /// </summary>
        private static string? GetLoggingFilePath(IBuildEnvironment buildEnvironment)
        {
            try
            {
                var loggingFilePath = buildEnvironment.GetEnvironmentVariable(EnvironmentVariableName);
                if (string.IsNullOrEmpty(loggingFilePath))
                {
                    return null;
                }

                loggingFilePath = buildEnvironment.GetFullyQualifiedPath(loggingFilePath);

                // If the environment variable contains the path of a currently existing directory,
                // then use a process-specific name for the log file and put it in that directory.
                // Otherwise, assume that the environment variable specifies the name of the log file.
#pragma warning disable RS0030 // getFullPath guarantees a full path
                if (Directory.Exists(loggingFilePath))
#pragma warning restore RS0030
                {
                    var processId = Process.GetCurrentProcess().Id;
                    loggingFilePath = Path.Combine(loggingFilePath, $"server.{processId}.log");
                }

                return loggingFilePath;
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
                return null;
            }
        }
    }

    internal sealed class EmptyCompilerServerLogger : ICompilerServerLogger
    {
        public static EmptyCompilerServerLogger Instance { get; } = new EmptyCompilerServerLogger();

        public bool IsLogging => false;

        private EmptyCompilerServerLogger()
        {
        }

        public void Log(string message)
        {
        }
    }
}
