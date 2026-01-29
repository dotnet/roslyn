// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TempRoot : IDisposable
    {
        private readonly ConcurrentBag<IDisposable> _temps = new ConcurrentBag<IDisposable>();
        public static readonly string Root;
        private bool _disposed;

        static TempRoot()
        {
            var tempDirectory = new DirectoryInfo(Path.GetTempPath());

#if NET
            if (tempDirectory.LinkTarget != null)
            {
                tempDirectory = (DirectoryInfo)Directory.ResolveLinkTarget(tempDirectory.FullName, true);
            }
            else
            {
                var parentDirectory = tempDirectory.Parent;
                while (parentDirectory != null && parentDirectory.LinkTarget == null)
                {
                    parentDirectory = parentDirectory.Parent;
                }

                if (parentDirectory != null)
                {
                    var relativePath = Path.GetRelativePath(parentDirectory.FullName, tempDirectory.FullName);
                    var realPath = Directory.ResolveLinkTarget(parentDirectory.FullName, true).FullName;
                    tempDirectory = new DirectoryInfo(Path.GetFullPath(Path.Combine(realPath, relativePath)));
                }
            }
#endif


            Root = Path.Combine(tempDirectory.FullName, "RoslynTests");
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            _disposed = true;
            while (_temps.TryTake(out var temp))
            {
                try
                {
                    if (temp != null)
                    {
                        temp.Dispose();
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(TempRoot));
            }
        }

        public TempDirectory CreateDirectory()
        {
            CheckDisposed();
            var dir = new DisposableDirectory(this);
            _temps.Add(dir);
            return dir;
        }

        public TempFile CreateFile(string prefix = null, string extension = null, string directory = null, [CallerFilePath] string callerSourcePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            CheckDisposed();
            return AddFile(new DisposableFile(prefix, extension, directory, callerSourcePath, callerLineNumber));
        }

        public DisposableFile AddFile(DisposableFile file)
        {
            CheckDisposed();
            _temps.Add(file);
            return file;
        }

        internal static void CreateStream(string fullPath, FileMode mode)
        {
            using (var file = new FileStream(fullPath, mode))
            {
            }
        }
    }
}
