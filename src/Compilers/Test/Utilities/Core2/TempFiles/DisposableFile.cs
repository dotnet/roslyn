// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class DisposableFile : TempFile, IDisposable
    {
        public DisposableFile(string path)
            : base(path)
        {
        }

        public DisposableFile(string prefix = null, string extension = null, string directory = null, string callerSourcePath = null, int callerLineNumber = 0)
            : base(prefix, extension, directory, callerSourcePath, callerLineNumber)
        {
        }

        public void Dispose()
        {
            if (Path != null && File.Exists(Path))
            {
                try
                {
                    File.Delete(Path);
                }
                catch (UnauthorizedAccessException)
                {
                    try
                    {
                        // the file might still be memory-mapped, delete on close:
                        DeleteFileOnClose(Path);
                    }
                    catch (IOException ex)
                    {
                        throw new InvalidOperationException(string.Format(@"
The file '{0}' seems to have been opened in a way that prevents us from deleting it on close.
Is the file loaded as an assembly (e.g. via Assembly.LoadFile)?

{1}: {2}", Path, ex.GetType().Name, ex.Message), ex);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //  We should ignore this exception if we got it the second time, 
                        //  the most important reason is that the file has already been 
                        //  scheduled for deletion and will be deleted when all handles 
                        //  are closed.
                    }
                }
            }
        }

        [DllImport("kernel32.dll", PreserveSig = false)]
        private static extern void SetFileInformationByHandle(SafeFileHandle handle, int fileInformationClass, ref uint fileDispositionInfoDeleteFile, int bufferSize);

        private const int FileDispositionInfo = 4;

        internal static void PrepareDeleteOnCloseStreamForDisposal(FileStream stream)
        {
            // tomat: Set disposition to "delete" on the stream, so to avoid ForeFront EndPoint
            // Protection driver scanning the file. Note that after calling this on a file that's open with DeleteOnClose, 
            // the file can't be opened again, not even by the same process.
            uint trueValue = 1;
            SetFileInformationByHandle(stream.SafeFileHandle, FileDispositionInfo, ref trueValue, sizeof(uint));
        }

        /// <summary>
        /// Marks given file for automatic deletion when all its handles are closed.
        /// Note that after doing this the file can't be opened again, not even by the same process.
        /// </summary>
        internal static void DeleteFileOnClose(string fullPath)
        {
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete | FileShare.ReadWrite, 8, FileOptions.DeleteOnClose))
            {
                PrepareDeleteOnCloseStreamForDisposal(stream);
            }
        }
    }
}
