// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;

namespace Microsoft.DiaSymReader
{
    internal static class SymUnmanagedWriterFactory
    {
        /// <summary>
        /// Creates a Windows PDB writer.
        /// </summary>
        /// <param name="metadataProvider"><see cref="ISymWriterMetadataProvider"/> implementation.</param>
        /// <param name="options">Options.</param>
        /// <remarks>
        /// Tries to load the implementation of the PDB writer from Microsoft.DiaSymReader.Native.{platform}.dll library first.
        /// It searches for this library in the directory Microsoft.DiaSymReader.dll is loaded from, 
        /// the application directory, the %WinDir%\System32 directory, and user directories in the DLL search path, in this order.
        /// If not found in the above locations and <see cref="SymUnmanagedWriterCreationOptions.UseAlternativeLoadPath"/> option is specified
        /// the directory specified by MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH environment variable is also searched.
        /// If the Microsoft.DiaSymReader.Native.{platform}.dll library can't be found and <see cref="SymUnmanagedWriterCreationOptions.UseComRegistry"/> 
        /// option is specified checks if the PDB reader is available from a globally registered COM object. This COM object is provided 
        /// by .NET Framework and has limited functionality (features like determinism and source link are not supported).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="metadataProvider"/>is null.</exception>
        /// <exception cref="DllNotFoundException">The SymWriter implementation is not available or failed to load.</exception>
        /// <exception cref="SymUnmanagedWriterException">Error creating the PDB writer. See inner exception for root cause.</exception>
        public static SymUnmanagedWriter CreateWriter(
            ISymWriterMetadataProvider metadataProvider,
            SymUnmanagedWriterCreationOptions options = SymUnmanagedWriterCreationOptions.Default)
        {
            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            var symWriter = SymUnmanagedFactory.CreateObject(
                createReader: false,
                useAlternativeLoadPath: (options & SymUnmanagedWriterCreationOptions.UseAlternativeLoadPath) != 0,
                useComRegistry: (options & SymUnmanagedWriterCreationOptions.UseComRegistry) != 0,
                moduleName: out var implModuleName,
                loadException: out var loadException);

            if (symWriter == null)
            {
                Debug.Assert(loadException != null);

                if (loadException is DllNotFoundException)
                {
                    throw loadException;
                }

                throw new DllNotFoundException(loadException.Message, loadException);
            }

            if (!(symWriter is ISymUnmanagedWriter5 symWriter5))
            {
                throw new SymUnmanagedWriterException(new NotSupportedException(), implModuleName);
            }

            object metadataEmitAndImport = new SymWriterMetadataAdapter(metadataProvider);
            var pdbStream = new ComMemoryStream();

            try
            {
                if ((options & SymUnmanagedWriterCreationOptions.Deterministic) != 0)
                {
                    if (symWriter is ISymUnmanagedWriter8 symWriter8)
                    {
                        symWriter8.InitializeDeterministic(metadataEmitAndImport, pdbStream);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    // The file name is irrelevant as long as it's specified.
                    // SymWriter only uses it for filling CodeView debug directory data when asked for them, but we never do.
                    symWriter5.Initialize(metadataEmitAndImport, "filename.pdb", pdbStream, fullBuild: true);
                }
            }
            catch (Exception e)
            {
                throw new SymUnmanagedWriterException(e, implModuleName);
            }

            return new SymUnmanagedWriterImpl(pdbStream, symWriter5, implModuleName);
        }
    }
}
