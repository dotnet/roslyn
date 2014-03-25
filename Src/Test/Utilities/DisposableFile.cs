// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Utilities;

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
                        FileUtilities.DeleteFileOnClose(Path);
                    }
                    catch (IOException)
                    {
                        throw new InvalidOperationException(string.Format(@"
The file '{0}' seems to have been opened in a way that prevents us from deleting it on close.
Is the file loaded as an assembly (e.g. via Assembly.LoadFile)?", Path));
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
    }
}
