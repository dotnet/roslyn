// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Compilation
    {
        /// <summary>
        /// Describes the kind of real signing that is being done during Emit. In the case of public signing
        /// this value will be <see cref="None"/>.
        /// </summary>
        internal enum EmitStreamSignKind
        {
            None,

            /// <summary>
            /// This form of signing occurs in memory using the <see cref="PEBuilder"/> APIs. This is the default 
            /// form of signing and will be used when a strong name key is provided in a file on disk.
            /// </summary>
            SignedWithBuilder,

            /// <summary>
            /// This form of signing occurs using the <see cref="IClrStrongName"/> COM APIs. This form of signing
            /// requires the unsigned PE to be written to disk before it can be signed (typically by writing it
            /// out to the %TEMP% folder). This signing is used when the key in a key container, the signing 
            /// requires a counter signature or customers opted in via the UseLegacyStrongNameProvider feature 
            /// flag.
            /// </summary>
            SignedWithFile,
        }

        /// <summary>
        /// This type abstracts away the legacy COM based signing implementation for PE streams. Under the hood
        /// a temporary file must be created on disk (at the last possible moment), emitted to, signed on disk
        /// and then copied back to the original <see cref="Stream"/>. Only when legacy signing is enabled though.
        /// </summary>
        internal sealed class EmitStream
        {
            private readonly EmitStreamProvider _emitStreamProvider;
            private readonly EmitStreamSignKind _emitStreamSignKind;
            private readonly StrongNameProvider? _strongNameProvider;
            private readonly StrongNameKeys _strongNameKeys;
            private (Stream emitStream, Stream tempStream, string tempFilePath)? _tempInfo;
            private bool _created;

            internal EmitStream(
                EmitStreamProvider emitStreamProvider,
                EmitStreamSignKind emitStreamSignKind,
                StrongNameKeys strongNameKeys,
                StrongNameProvider? strongNameProvider)
            {
                RoslynDebug.Assert(emitStreamProvider != null);
                RoslynDebug.Assert(strongNameProvider != null || emitStreamSignKind == EmitStreamSignKind.None);
                _emitStreamProvider = emitStreamProvider;
                _emitStreamSignKind = emitStreamSignKind;
                _strongNameProvider = strongNameProvider;
                _strongNameKeys = strongNameKeys;
            }

            internal Func<Stream?> GetCreateStreamFunc(CommonMessageProvider messageProvider, DiagnosticBag diagnostics)
            {
                return () => CreateStream(messageProvider, diagnostics);
            }

            internal void Close()
            {
                // The emitStream tuple element is _deliberately_ not disposed here. That is owned by 
                // the EmitStreamProvider not us.
                if (_tempInfo is (Stream _, Stream tempStream, string tempFilePath))
                {
                    _tempInfo = null;
                    try
                    {
                        tempStream.Dispose();
                    }
                    finally
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                        }
                        catch
                        {
                            // Not much to do if we can't delete from the temp directory
                        }
                    }
                }
            }

            /// <summary>
            /// Create the stream which should be used for Emit. This should only be called one time.
            /// </summary>
            /// <remarks>
            /// The <see cref="Stream"/> returned here is owned by this type and should not be disposed 
            /// by the caller.
            /// </remarks>
            private Stream? CreateStream(CommonMessageProvider messageProvider, DiagnosticBag diagnostics)
            {
                RoslynDebug.Assert(!_created);
                RoslynDebug.Assert(diagnostics != null);

                _created = true;
                if (diagnostics.HasAnyErrors())
                {
                    return null;
                }

                if (_emitStreamSignKind == EmitStreamSignKind.SignedWithFile)
                {
                    // Signing is going to be done with on disk files and that requires us to manage 
                    // multiple Stream instances. One for the on disk file and one for the actual emit
                    // stream the final PE should be written to.
                    RoslynDebug.Assert(_strongNameProvider != null);

                    var fileSystem = _strongNameProvider.FileSystem;
                    var tempDir = fileSystem.GetSigningTempPath();
                    if (tempDir is null)
                    {
                        diagnostics.Add(StrongNameKeys.GetError(
                            _strongNameKeys.KeyFilePath,
                            _strongNameKeys.KeyContainer,
                            new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.SigningTempPathUnavailable)),
                            messageProvider));
                        return null;
                    }

                    var emitStream = _emitStreamProvider.GetOrCreateStream(diagnostics);
                    if (emitStream is null)
                    {
                        return null;
                    }

                    Stream tempStream;
                    string tempFilePath;
                    try
                    {
                        Func<string, Stream> streamConstructor = path => fileSystem.CreateFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

                        tempFilePath = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
                        tempStream = FileUtilities.CreateFileStreamChecked(streamConstructor, tempFilePath);
                    }
                    catch (IOException e)
                    {
                        throw new Cci.PeWritingException(e);
                    }

                    _tempInfo = (emitStream, tempStream, tempFilePath);
                    return tempStream;
                }
                else
                {
                    // Signing, if it occurs, will be done in memory so we can just return the final 
                    // Stream directly here.
                    return _emitStreamProvider.GetOrCreateStream(diagnostics);
                }
            }

            internal bool Complete(CommonMessageProvider messageProvider, DiagnosticBag diagnostics)
            {
                RoslynDebug.Assert(_created);
                RoslynDebug.Assert(_emitStreamSignKind != EmitStreamSignKind.SignedWithFile || _tempInfo.HasValue);

                try
                {
                    if (_tempInfo is (Stream emitStream, Stream tempStream, string tempFilePath))
                    {
                        RoslynDebug.Assert(_emitStreamSignKind == EmitStreamSignKind.SignedWithFile);
                        RoslynDebug.Assert(_strongNameProvider is object);

                        try
                        {
                            // Dispose the temp stream to ensure all of the contents are written to 
                            // disk.
                            tempStream.Dispose();

                            _strongNameProvider.SignFile(_strongNameKeys, tempFilePath);

                            using (var tempFileStream = new FileStream(tempFilePath, FileMode.Open))
                            {
                                tempFileStream.CopyTo(emitStream);
                            }
                        }
                        catch (DesktopStrongNameProvider.ClrStrongNameMissingException)
                        {
                            diagnostics.Add(StrongNameKeys.GetError(_strongNameKeys.KeyFilePath, _strongNameKeys.KeyContainer,
                                new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.AssemblySigningNotSupported)), messageProvider));
                            return false;
                        }
                        catch (IOException ex)
                        {
                            diagnostics.Add(StrongNameKeys.GetError(_strongNameKeys.KeyFilePath, _strongNameKeys.KeyContainer, ex.Message, messageProvider));
                            return false;
                        }
                    }
                }
                finally
                {
                    Close();
                }

                return true;
            }
        }
    }
}
