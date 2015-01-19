// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ConsoleApplication3
{    
    /// <summary>
    /// Takes a delay-signed assembly and flips its CLI header "strong-name signed" bit without
    /// adding a correct signature. This creates an assembly that can be loaded in full trust
    /// without registering for verification skipping. The assembly cannot be installed to the
    /// GAC.
    /// </summary>
    /// <remarks>
    /// This code is taken largely from the Microsoft.BuildTools project and their OSS signing
    /// process.  
    ///
    /// https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks/OpenSourceSign.cs
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// The number of bytes from the start of the <see cref="CorHeader"/> to its <see cref="CorFlags"/>.
        /// </summary>
        private const int OffsetFromStartOfCorHeaderToFlags =
           sizeof(Int32)  // byte count
         + sizeof(Int16)  // major version
         + sizeof(Int16)  // minor version
         + sizeof(Int64); // metadata directory

        private static bool ExecuteCore(string assemblyPath)
        {
            using (var stream = OpenFile(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var reader = new PEReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                if (!Validate(reader))
                {
                    Console.WriteLine("Unable to sign {0}", assemblyPath);
                    return false;
                }

                stream.Position = reader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;
                writer.Write((UInt32)(reader.PEHeaders.CorHeader.Flags | CorFlags.StrongNameSigned));
            }

            return true;
        }

        /// <summary>
        /// Returns true if the PE file meets all of the pre-conditions to be Open Source Signed.
        /// Returns false and logs msbuild errors otherwise.
        /// </summary>
        private static bool Validate(PEReader peReader)
        {
            if (!peReader.HasMetadata)
            {
                Console.WriteLine("PE file is not a managed module.");
                return false;
            }

            var mdReader = peReader.GetMetadataReader();
            if (!mdReader.IsAssembly)
            {
                Console.WriteLine("PE file is not an assembly.");
                return false;
            }

            CorHeader header = peReader.PEHeaders.CorHeader;
            if ((header.Flags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned)
            {
                Console.WriteLine("PE file is already strong-name signed.");
                return false;
            }

            if ((header.StrongNameSignatureDirectory.Size <= 0) || mdReader.GetAssemblyDefinition().PublicKey.IsNil)
            {
                Console.WriteLine("PE file is not a delay-signed assembly.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Wraps FileStream constructor to normalize all unpreventable exceptions to IOException.
        /// </summary>
        private static FileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            try
            {
                return new FileStream(path, mode, access, share);
            }
            catch (ArgumentException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            catch (NotSupportedException ex)
            {
                throw new IOException(ex.Message, ex);
            }
        }

        internal static int Main(string[] args)
        {
            // Create a byte array to hold the information.
            if (args.Length == 0)
            {
                Console.WriteLine("No file passed");
                return 1;
            }

            if (!ExecuteCore(args[0]))
            {
                Console.WriteLine("Could not sign assembly");
                return 1;
            }

            return 0;
        }
    }
}

