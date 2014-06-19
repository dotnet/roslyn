using System;
using System.Collections.Generic;
using System.IO;
using Roslyn.Compilers.MetadataReader.PEFile;
using Roslyn.Utilities;

namespace Roslyn.Compilers.MetadataReader
{
    internal static class MetadataFactory
    {
        internal static Assembly CreateAssemblyFromFile(string metadataFile, string resolvedPath, MetadataFileProvider fileProvider)
        {
            PEFileReader peFileReader = CreatePEFileReaderFromFile(metadataFile);

            if (!peFileReader.IsAssembly)
            {
                throw new MetadataReaderException(MetadataReaderErrorKind.InvalidPEKind);
            }

            var modules = CreateAssemblyModules(peFileReader, resolvedPath, fileProvider);
            return new Assembly(metadataFile, modules);
        }

        private static ReadOnlyArray<Module> CreateAssemblyModules(PEFileReader peFileReader, string assemblyResolvedPath, MetadataFileProvider fileProvider)
        {
            List<Module> modules = new List<Module>();
            modules.Add(new Module(peFileReader));

            foreach (string moduleFileName in peFileReader.GetMetadataModuleNames())
            {
                string modulePath;
                try
                {
                    modulePath = fileProvider.ProvideModuleFile(assemblyResolvedPath, moduleFileName);
                }
                catch (Exception e)
                {
                    throw new FileNotFoundException(e.Message, moduleFileName);
                }

                if (modulePath == null)
                {
                    throw new FileNotFoundException("Module not found", moduleFileName);
                }

                modules.Add(CreateModuleFromFile(modulePath));
            }

            return modules.AsReadOnly<Module>();
        }

        internal static Assembly CreateAssemblyFromBytes(AssemblyBytesReference reference)
        {
            // make a copy of the byte[], so that the user can't change the bytes that the reader consumes:
            PEFileReader peFileReader = CreatePEFileReaderFromBytes(reference.UniqueName, (byte[])reference.Bytes.Clone());

            if (!peFileReader.IsAssembly)
            {
                throw new MetadataReaderException(MetadataReaderErrorKind.InvalidPEKind);
            }

            return new Assembly(reference.UniqueName, ReadOnlyArray.Singleton(new Module(peFileReader)));
        }

        internal static Module CreateModuleFromFile(string path)
        {
            PEFileReader peFileReader = CreatePEFileReaderFromFile(path);

            if (peFileReader.IsAssembly)
            {
                throw new MetadataReaderException(MetadataReaderErrorKind.InvalidPEKind);
            }

            return new Module(peFileReader);
        }

        internal static PEFileReader CreatePEFileReaderFromFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                var binaryDocumentMemoryBlock = MemoryMappedFile.CreateMemoryMappedFile(stream, path);
                PEFileReader peFileReader = new PEFileReader(binaryDocumentMemoryBlock);

                Contract.ThrowIfTrue(peFileReader.ReaderState < ReaderState.Metadata);
                return peFileReader;
            }
        }

        internal static PEFileReader CreatePEFileReaderFromBytes(string uniqueName, byte[] bytes)
        {
            var binaryDocumentMemoryBlock = new ByteArrayMemoryBlock(uniqueName, bytes);
            PEFileReader peFileReader = new PEFileReader(binaryDocumentMemoryBlock);

            Contract.ThrowIfTrue(peFileReader.ReaderState < ReaderState.Metadata);
            return peFileReader;
        }
    }
}
