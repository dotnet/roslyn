// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Testing
{
    public sealed partial class ReferenceAssemblies
    {
        private sealed class FileSystemSemaphore
        {
            private readonly string _path;

            public FileSystemSemaphore(string path)
            {
                _path = path ?? throw new ArgumentNullException(nameof(path));

                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            internal async Task<Releaser> WaitAsync(CancellationToken cancellationToken)
            {
                while (true)
                {
                    try
                    {
                        return new Releaser(File.Open(_path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None));
                    }
                    catch (IOException)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
            }

            public readonly struct Releaser : IDisposable
            {
                private readonly FileStream _fileStream;

                public Releaser(FileStream fileStream)
                {
                    _fileStream = fileStream;
                }

                public void Dispose()
                {
                    _fileStream?.Dispose();
                }
            }
        }
    }
}
