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

        private static bool ExecuteCore(string assemblyPath, bool unSign = false, bool force = false)
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
                var mdReader = ValidateManagedAssemblyAndGetMetadataReader(reader);
                if (mdReader == null)
                {
                    Console.Error.WriteLine($"Cannot {(unSign ? "un-sign" : "sign")} {assemblyPath}.");
                    return false;
                }

                if (!force && !Validate(reader, mdReader, unSign))
                {
                    Console.Error.WriteLine($"Use the -f (force) option to {(unSign ? "un-sign" : "sign")} {assemblyPath} anyway.");
                    return false;
                }

                stream.Position = reader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;

                var flags = reader.PEHeaders.CorHeader.Flags;
                if (unSign)
                {
                    flags &= ~CorFlags.StrongNameSigned;
                }
                else
                {
                    flags |= CorFlags.StrongNameSigned;
                }

                writer.Write((UInt32)flags);
            }

            return true;
        }

        private static MetadataReader ValidateManagedAssemblyAndGetMetadataReader(PEReader peReader)
        {
            if (!peReader.HasMetadata)
            {
                Console.Error.WriteLine("PE file is not a managed module.");
                return null;
            }

            var mdReader = peReader.GetMetadataReader();
            if (!mdReader.IsAssembly)
            {
                Console.Error.WriteLine("PE file is not an assembly.");
                return null;
            }

            return mdReader;
        }

        /// <summary>
        /// Returns true if the PE file meets all of the pre-conditions to be Open Source Signed.
        /// Returns false and logs msbuild errors otherwise.
        /// </summary>
        private static bool Validate(PEReader peReader, MetadataReader mdReader, bool unSign)
        {
            CorHeader header = peReader.PEHeaders.CorHeader;
            var expectedStrongNameFlag = unSign ? CorFlags.StrongNameSigned : 0;
            var actualStrongNameFlag = header.Flags & CorFlags.StrongNameSigned;

            if (expectedStrongNameFlag != actualStrongNameFlag)
            {
                Console.Error.WriteLine($"PE file is {(unSign ? "not" : "already")} strong-name signed.");
                return false;
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
            bool force = false;

            foreach (string arg in args)
            {
                if (arg.Length >= 2 && (arg[0] == '-' || arg[0] == '/'))
                {
                    switch (arg[1])
                    {
                        case '?':
                            goto Help;

                        case 'u':
                        case 'U':
                            unSign = true;
                            break;


                        case 'f':
                        case 'F':
                            force = true;
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

            if (file == null)
            {
                Console.Error.WriteLine("Missing assemblyPath.");
                goto Help;
            }

            return ExecuteCore(file, unSign, force) ? 0 : 1;

        Help:
            Console.Error.Write(
@"Sets or removes the ""strong name signed"" flag in a managed assembly. This
creates an assembly that can be loaded in full trust without registering for
verification skipping.

FakeSign [-u] [-f] assemblyPath
    -u (unsign) Clears the strong name flag (default is to set the flag).
    -f (force) Updates even if nothing would change.
");
            return 1;
        }
    }
}
