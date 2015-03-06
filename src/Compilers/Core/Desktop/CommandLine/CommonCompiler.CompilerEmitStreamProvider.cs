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
        /// of the PE + PDB file until the compiler determines the compilation has succeeded.  This prevents
        /// the compiler from deleting output from the previous compilation when a new compilation 
        /// fails.
        /// </summary>
        private sealed class CompilerEmitStreamProvider : Compilation.EmitStreamProvider, IDisposable
        {
            private readonly CommonCompiler _compiler;
            private readonly TouchedFileLogger _touchedFileLogger;
            private readonly string _peFilePath;
            private readonly string _pdbFilePath;
            private Stream _peStream;
            private Stream _pdbStream;

            internal CompilerEmitStreamProvider(CommonCompiler compiler, TouchedFileLogger touchedFileLogger, string peFilePath, string pdbFilePath)
            {
                _compiler = compiler;
                _touchedFileLogger = touchedFileLogger;
                _peFilePath = peFilePath;
                _pdbFilePath = pdbFilePath;
            }

            public void Dispose()
            {
                _peStream?.Dispose();
                _peStream = null;
                _pdbStream?.Dispose();
                _pdbStream = null;
            }

            public override bool HasPdbStream
            {
                get { return _pdbFilePath != null; }
            }

            public override Stream GetPeStream(DiagnosticBag diagnostics)
            {
                if (_peStream == null)
                {
                    _peStream = OpenFile(_peFilePath, diagnostics);
                }

                return _peStream;
            }

            public override Stream GetPdbStream(DiagnosticBag diagnostics)
            {
                Debug.Assert(HasPdbStream);

                if (_pdbStream == null)
                {
                    _pdbStream = OpenFile(_pdbFilePath, diagnostics);
                }

                return _pdbStream;
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

        private sealed class SimpleEmitStreamProvider : Compilation.EmitStreamProvider
        {
            private readonly Stream _peStream;
            private readonly Stream _pdbStream;

            internal SimpleEmitStreamProvider(Stream peStream, Stream pdbStream = null)
            {
                Debug.Assert(peStream.CanWrite);
                Debug.Assert(pdbStream == null || pdbStream.CanWrite);
                _peStream = peStream;
                _pdbStream = pdbStream;
            }

            public override bool HasPdbStream
            {
                get { return _pdbStream != null; }
            }

            public override Stream GetPeStream(DiagnosticBag diagnostics)
            {
                return _peStream;
            }

            public override Stream GetPdbStream(DiagnosticBag diagnostics)
            {
                Debug.Assert(HasPdbStream);
                return _pdbStream;
            }
        }
    }
}