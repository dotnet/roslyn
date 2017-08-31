using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class PortableStrongNameProvider : StrongNameProvider
    {
        internal readonly ImmutableArray<string> _keySearchPaths;
        internal IOOperations IOOp { get; set; } = new IOOperations();

        public override bool Equals(object other)
        {
            if (other is null)
            {
                return false;
            }

            if (other.GetType() != this.GetType()) {
                return false;
            }

            return Capability == (other as PortableStrongNameProvider).Capability;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public PortableStrongNameProvider(ImmutableArray<string> keySearchPaths = default(ImmutableArray<string>))
        {
            _keySearchPaths = keySearchPaths.NullToEmpty(); 
        }

        internal override SigningCapability Capability => SigningCapability.SignsPeBuilder;

        internal override StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider)
        {
            var keyPair = default(ImmutableArray<byte>);
            var publicKey = default(ImmutableArray<byte>);
            string container = null;

            if (!string.IsNullOrEmpty(keyFilePath))
            {
                try
                {
                    string resolvedKeyFile = IOOp.ResolveStrongNameKeyFile(keyFilePath, _keySearchPaths);
                    if (resolvedKeyFile == null)
                    {
                        throw new FileNotFoundException(CodeAnalysisResources.FileNotFound, keyFilePath);
                    }

                    Debug.Assert(PathUtilities.IsAbsolute(resolvedKeyFile));
                    var fileContent = ImmutableArray.Create(IOOp.ReadAllBytes(resolvedKeyFile));
                    return StrongNameKeys.CreateHelper(fileContent, keyFilePath);
                }
                catch (Exception ex)
                {
                    return new StrongNameKeys(StrongNameKeys.GetKeyFileError(messageProvider, keyFilePath, ex.Message));
                }
                // it turns out that we don't need IClrStrongName to retrieve a key file,
                // so there's no need for a catch of ClrStrongNameMissingException in this case
            }

            return new StrongNameKeys(keyPair, publicKey, null, container, keyFilePath);
        }

        internal override Stream CreateInputStream()
        {
            throw new InvalidOperationException();
        }

        internal override void SignStream(StrongNameKeys keys, Stream inputStream, Stream outputStream)
        {
            throw new InvalidOperationException();
        }

        internal override void SignPeBuilder(ExtendedPEBuilder peBuilder, BlobBuilder peBlob, RSAParameters privkey)
        {
            peBuilder.Sign(peBlob, content => SigningUtilities.CalculateRsaSignature(content, privkey));
        }
    }
}
