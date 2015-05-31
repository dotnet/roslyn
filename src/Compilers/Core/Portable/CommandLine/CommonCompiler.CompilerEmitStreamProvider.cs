// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

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
            private readonly string _filePath;
            private Stream _streamToDispose;

            internal CompilerEmitStreamProvider(CommonCompiler compiler, string filePath)
            {
                _compiler = compiler;
                _filePath = filePath;
            }

            public void Dispose()
            {
                _streamToDispose?.Dispose();
            }

            public override Stream Stream => null;

            public override Stream CreateStream(DiagnosticBag diagnostics)
            {
                Debug.Assert(_streamToDispose == null);

                try
                {
                    return _streamToDispose = _compiler.FileOpen(_filePath, PortableShim.FileMode.Create, PortableShim.FileAccess.ReadWrite, PortableShim.FileShare.None);
                }
                catch (Exception e)
                {
                    var messageProvider = _compiler.MessageProvider;
                    diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_CantOpenFileWrite, Location.None, _filePath, e.Message));
                    return null;
                }
            }
        }
    }
}