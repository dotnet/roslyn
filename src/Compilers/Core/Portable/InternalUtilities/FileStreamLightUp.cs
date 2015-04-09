// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    internal static class FileStreamLightUp
    {
        private static readonly Lazy<Func<string, Stream>> s_lazyFileOpenStreamMethod = new Lazy<Func<string, Stream>>(() =>
        {
            Type file;
            try
            {
                // try contract name first:
                file = Type.GetType("System.IO.File, System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
            }
            catch
            {
                file = null;
            }

            if (file == null)
            {
                try
                {
                    // try corlib next:
                    file = typeof(object).GetTypeInfo().Assembly.GetType("System.IO.File");
                }
                catch
                {
                    file = null;
                }
            }

            try
            {
                var openRead = file?.GetTypeInfo().GetDeclaredMethod("OpenRead");
                return (Func<string, Stream>)openRead?.CreateDelegate(typeof(Func<string, Stream>));
            }
            catch
            {
                return null;
            }
        });

        internal static Stream OpenFileStream(string path)
        {
            var factory = s_lazyFileOpenStreamMethod.Value;
            if (factory == null)
            {
                throw new PlatformNotSupportedException();
            }

            Stream fileStream;
            try
            {
                fileStream = factory(path);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (IOException e)
            {
                if (e.GetType().Name == "DirectoryNotFoundException")
                {
                    throw new FileNotFoundException(e.Message, path, e);
                }

                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }

            return fileStream;
        }
    }
}
