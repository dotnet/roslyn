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
                    try
                    {
                        return OpenFileStream();
                    }
                    catch (IOException e)
                    {
                        // Other process is reading the file preventing us to write to it.
                        // We attempt to rename and delete the file in case the reader opened it with FileShare.Delete flag that
                        // allows the file to be deleted by other processes.
                        //
                        // Note that if the file is marked "readonly" or the current user doesn't have sufficient privileges
                        // the exception thrown is UnauthorizedAccessException, not IOException, so we won't attempt to delete the file.

                        try
                        {
                            const int eWin32SharingViolation = unchecked((int)0x80070020);

                            if (PathUtilities.IsUnixLikePlatform)
                            {
                                // Unix & Mac are simple: just delete the file in the directory. 
                                // The memory mapped content remains available for the reader.
                                PortableShim.File.Delete(_filePath);
                            }
                            else if (e.HResult == eWin32SharingViolation)
                            {
                                // On Windows File.Delete only marks the file for deletion, but doens't remove it from the directory.
                                var newFilePath = Path.Combine(Path.GetDirectoryName(_filePath), Guid.NewGuid().ToString() + "_" + Path.GetFileName(_filePath));

                                // Try to rename the existing file. This fails unless the file is open with FileShare.Delete.
                                PortableShim.File.Move(_filePath, newFilePath);

                                // hide the renamed file:
                                PortableShim.File.SetAttributes(newFilePath, PortableShim.FileAttributes.Hidden);

                                // Mark the renamed file for deletion, so that it's deleted as soon as the current reader is finished reading it
                                PortableShim.File.Delete(newFilePath);
                            }
                        }
                        catch
                        {
                            // report the original exception
                            ReportOpenFileDiagnostic(diagnostics, e);
                            return null;
                        }

                        return OpenFileStream();
                    }
                }
                catch (Exception e)
                {
                    ReportOpenFileDiagnostic(diagnostics, e);
                    return null;
                }
            }

            private Stream OpenFileStream()
            {
                return _streamToDispose = _compiler.FileOpen(_filePath, PortableShim.FileMode.Create, PortableShim.FileAccess.ReadWrite, PortableShim.FileShare.None);
            }

            private void ReportOpenFileDiagnostic(DiagnosticBag diagnostics, Exception e)
            {
                var messageProvider = _compiler.MessageProvider;
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_CantOpenFileWrite, Location.None, _filePath, e.Message));
            }
        }
    }
}