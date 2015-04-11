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
            private static Stream s_uninitialized = Stream.Null;

            private readonly CommonCompiler _compiler;
            private readonly string _filePath;

            private Stream _lazyStream;

            internal CompilerEmitStreamProvider(CommonCompiler compiler, string filePath)
            {
                _compiler = compiler;
                _filePath = filePath;
                _lazyStream = s_uninitialized;
            }

            public void Dispose()
            {
                if (_lazyStream != s_uninitialized)
                {
                    _lazyStream?.Dispose();
                    _lazyStream = s_uninitialized;
                }
            }

            public override Stream GetStream(DiagnosticBag diagnostics)
            {
                if (_lazyStream == s_uninitialized)
                {
                    _lazyStream = OpenFile(_filePath, diagnostics);
                }

                return _lazyStream;
            }

            private Stream OpenFile(string filePath, DiagnosticBag diagnostics)
            {
                try
                {
                    return _compiler.FileOpen(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
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