// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class ManagedResource : Cci.IResource
    {
        private readonly Func<Stream> streamProvider;
        private readonly Cci.IFileReference fileReference;
        private readonly uint offset;
        private readonly string name;
        private readonly bool isPublic;

        /// <summary>
        /// <paramref name="streamProvider"/> streamProvider callers will dispose result after use.
        /// <paramref name="streamProvider"/> and <paramref name="fileReference"/> are mutually exclusive.
        /// </summary>
        internal ManagedResource(string name, bool isPublic, Func<Stream> streamProvider, Cci.IFileReference fileReference, uint offset)
        {
            Debug.Assert(streamProvider == null ^ fileReference == null);

            this.streamProvider = streamProvider;
            this.name = name;
            this.fileReference = fileReference;
            this.offset = offset;
            this.isPublic = isPublic;
        }

        public void WriteData(Cci.BinaryWriter resourceWriter)
        {
            if (fileReference == null)
            {
                try
                {
                    using (Stream stream = streamProvider())
                    {
                        if (stream == null)
                        {
                            throw new InvalidOperationException(CodeAnalysisResources.ResourceStreamProviderShouldReturnNonNullStream);
                        }

                        var count = (int)(stream.Length - stream.Position);
                        resourceWriter.WriteInt(count);

                        var to = resourceWriter.BaseStream;
                        var position = (int)to.Position;
                        to.Position = (uint)(position + count);
                        resourceWriter.Align(8);

                        var buffer = to.Buffer;
                        stream.Read(buffer, position, count);
                    }
                }
                catch (Exception e)
                {
                    throw new ResourceException(this.name, e);
                }
            }
        }

        public Cci.IFileReference ExternalFile
        {
            get
            {
                return fileReference;
            }
        }

        public uint Offset
        {
            get
            {
                return offset;
            }
        }

        public IEnumerable<Cci.ICustomAttribute> Attributes
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>(); }
        }

        public bool IsPublic
        {
            get { return isPublic; }
        }

        public string Name
        {
            get { return name; }
        }

        public Cci.IResource Resource
        {
            get { return this; }
        }
    }
}
