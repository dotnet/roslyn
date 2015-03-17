// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// This implementation of <see cref="Compilation.EmitStreamProvider"/> will delay the creation
        /// of the PE / PDB file until the compiler determines the compilation has succeeded.  This prevents
        /// the compiler from deleting output from the previous compilation when a new compilation 
        /// fails.
        /// </summary>
        private sealed class CompilerEmitStreamProvider : Compilation.EmitStreamProvider, IDisposable
        {
            private readonly CommonCompiler _compiler;
            private readonly TouchedFileLogger _touchedFileLogger;
            private readonly string _filePath;
            private Stream _stream;

            internal CompilerEmitStreamProvider(CommonCompiler compiler, TouchedFileLogger touchedFileLogger, string filePath)
            {
                _compiler = compiler;
                _touchedFileLogger = touchedFileLogger;
                _filePath = filePath;
            }

            public void Dispose()
            {
                _stream?.Dispose();
                _stream = null;
            }

            public override Stream GetStream(DiagnosticBag diagnostics)
            {
                if (_stream == null)
                {
                    _stream = OpenFile(_filePath, diagnostics);
                }

                return _stream;
            }

            private Stream OpenFile(string filePath, DiagnosticBag diagnostics)
            {
                try
                {
                    Stream stream = _compiler.FileOpen(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                    if (_touchedFileLogger != null)
                    {
                        _touchedFileLogger.AddWritten(filePath);
                    }

                    return stream;
                }
                catch (Exception e)
                {
                    var messageProvider = _compiler.MessageProvider;
                    diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_CantOpenFileWrite, Location.None, filePath, e.Message));
                    return null;
                }
            }
        }
    }
}