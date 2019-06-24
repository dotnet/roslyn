// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for logging compiler diagnostics.
    /// </summary>
    internal abstract class ErrorLogger
    {
        public abstract void LogDiagnostic(Diagnostic diagnostic);
    }

    /// <summary>
    /// Used for logging all compiler diagnostics into a given <see cref="Stream"/>.
    /// This logger is responsible for closing the given stream on <see cref="Dispose"/>.
    /// It is incorrect to use the logger concurrently from multiple threads.
    /// </summary>
    internal abstract class StreamErrorLogger : ErrorLogger, IDisposable
    {
        protected JsonWriter _writer { get; } // TODO: Rename to Writer.

        public StreamErrorLogger(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            _writer = new JsonWriter(new StreamWriter(stream));
        }

        public virtual void Dispose()
        {
            _writer.Dispose();
        }
    }
}
