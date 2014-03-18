// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed partial class ModuleMetadata
    {
        /// <summary>
        /// Creates metadata module from a file containing a portable executable image.
        /// </summary>
        /// <param name="fullPath">Absolute file path.</param>
        /// <remarks>
        /// The file might remain mapped (and read-locked) until this object is disposed.
        /// The memory map is only created for large files. Small files are read into memory.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not a valid absolute path.</exception>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="FileNotFoundException">File <paramref name="fullPath"/> not found.</exception>
        public static ModuleMetadata CreateFromFile(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

            FileStream fileStream;
            try
            {
                // Use FileShare.Delete to support files that are opened with DeleteOnClose option.
                fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(e.Message, "fullPath");
            }
            catch (DirectoryNotFoundException e)
            {
                throw new FileNotFoundException(e.Message, fullPath, e);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }

            return CreateFromImageStream(fileStream);
        }
    }
}
