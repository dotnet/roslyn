// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace FakeSign
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

        private static bool ExecuteCore(string assemblyPath, bool unSign = false)
        {
            if (Directory.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"Expected file, not a directory: {assemblyPath}");
                return false;
            }

            if (!File.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"File not found: {assemblyPath}");
                return false;
            }

            using (var stream = OpenFile(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var reader = new PEReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                if (!Validate(reader, unSign))
                {
                    if (unSign)
                    {
                        Console.Error.WriteLine($"Unable to un-sign {assemblyPath}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unable to sign {assemblyPath}");
                    }
                    return false;
                }

                stream.Position = reader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;
                writer.Write((UInt32)(reader.PEHeaders.CorHeader.Flags ^ CorFlags.StrongNameSigned));
            }

            return true;
        }

        /// <summary>
        /// Returns true if the PE file meets all of the pre-conditions to be Open Source Signed.
        /// Returns false and logs msbuild errors otherwise.
        /// </summary>
        private static bool Validate(PEReader peReader, bool unSign)
        {
            if (!peReader.HasMetadata)
            {
                Console.Error.WriteLine("PE file is not a managed module.");
                return false;
            }

            var mdReader = peReader.GetMetadataReader();
            if (!mdReader.IsAssembly)
            {
                Console.Error.WriteLine("PE file is not an assembly.");
                return false;
            }

            CorHeader header = peReader.PEHeaders.CorHeader;
            if (unSign)
            {
                if ((header.Flags & CorFlags.StrongNameSigned) == 0)
                {
                    Console.Error.WriteLine("PE file is not strong-name signed.");
                    return false;
                }
            }
            else
            {
                if ((header.Flags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned)
                {
                    Console.Error.WriteLine("PE file is already strong-name signed.");
                    return false;
                }
            }

            if ((header.StrongNameSignatureDirectory.Size <= 0) || mdReader.GetAssemblyDefinition().PublicKey.IsNil)
            {
                Console.Error.WriteLine("PE file is not a delay-signed assembly.");
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
            string file = null;
            bool unSign = false;

            foreach (string arg in args)
            {
                if (arg.Length >= 2 && (arg[0] == '-' || arg[0] == '/'))
                {
                    switch (arg[1])
                    {
                        case '?':
                            goto Help;

                        case 'u':
                            unSign = true;
                            break;

                        default:
                            Console.Error.WriteLine($"Unrecognized switch {arg}");
                            goto Help;
                    }
                }
                else if (file != null)
                {
                    Console.Error.WriteLine("Too many arguments.");
                    goto Help;
                }
                else
                {
                    file = arg;
                }
            }

            if (!ExecuteCore(file, unSign))
            {
                Console.Error.WriteLine("Could not sign assembly");
                return 1;
            }

            return 0;

        Help:
            Console.Error.WriteLine("Usage:\nFakeSign [-u] assemblyPath");
            return 1;
        }
    }
}

