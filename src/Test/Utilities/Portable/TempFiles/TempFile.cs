// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class TempFile
    {
        private readonly string _path;

        internal TempFile(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));
            _path = path;
        }

        internal TempFile(string prefix, string extension, string directory, string callerSourcePath, int callerLineNumber)
        {
            while (true)
            {
                if (prefix == null)
                {
                    prefix = System.IO.Path.GetFileName(callerSourcePath) + "_" + callerLineNumber.ToString() + "_";
                }

                _path = System.IO.Path.Combine(directory ?? TempRoot.Root, prefix + Guid.NewGuid() + (extension ?? ".tmp"));

                try
                {
                    TempRoot.CreateStream(_path, FileMode.CreateNew);
                    break;
                }
                catch (PathTooLongException)
                {
                    throw;
                }
                catch (DirectoryNotFoundException)
                {
                    throw;
                }
                catch (IOException)
                {
                    // retry
                }
            }
        }

        public FileStream Open(FileAccess access = FileAccess.ReadWrite)
        {
            return new FileStream(_path, FileMode.Open, access);
        }

        public string Path
        {
            get { return _path; }
        }

        public TempFile WriteAllText(string content, Encoding encoding)
        {
            File.WriteAllText(_path, content, encoding);
            return this;
        }

        public TempFile WriteAllText(string content)
        {
            File.WriteAllText(_path, content);
            return this;
        }

        public async Task<TempFile> WriteAllTextAsync(string content, Encoding encoding)
        {
            using (var sw = new StreamWriter(File.Create(_path), encoding))
            {
                await sw.WriteAsync(content).ConfigureAwait(false);
            }

            return this;
        }

        public Task<TempFile> WriteAllTextAsync(string content)
        {
            return WriteAllTextAsync(content, Encoding.UTF8);
        }

        public TempFile WriteAllBytes(byte[] content)
        {
            File.WriteAllBytes(_path, content);
            return this;
        }

        public TempFile WriteAllBytes(ImmutableArray<byte> content)
        {
            content.WriteToFile(_path);
            return this;
        }

        public string ReadAllText()
        {
            return File.ReadAllText(_path);
        }

        public byte[] ReadAllBytes()
        {
            return File.ReadAllBytes(_path);
        }

        public TempFile CopyContentFrom(string path)
        {
            return WriteAllBytes(File.ReadAllBytes(path));
        }

        public override string ToString()
        {
            return _path;
        }
    }
}
