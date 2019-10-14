// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal sealed class ManagedResource
    {
        private readonly Func<Stream>? _streamProvider;
        private readonly IFileReference? _fileReference;
        private readonly uint _offset;
        private readonly string _name;
        private readonly bool _isPublic;

        /// <summary>
        /// <paramref name="streamProvider"/> streamProvider callers will dispose result after use.
        /// <paramref name="streamProvider"/> and <paramref name="fileReference"/> are mutually exclusive.
        /// </summary>
        internal ManagedResource(string name, bool isPublic, Func<Stream> streamProvider, IFileReference fileReference, uint offset)
        {
            Debug.Assert(streamProvider == null ^ fileReference == null);

            _streamProvider = streamProvider;
            _name = name;
            _fileReference = fileReference;
            _offset = offset;
            _isPublic = isPublic;
        }

        public void WriteData(BlobBuilder resourceWriter)
        {
            if (_fileReference == null)
            {
                try
                {
#nullable disable // Can '_streamProvider' be null? https://github.com/dotnet/roslyn/issues/39166
                    using (Stream stream = _streamProvider())
#nullable enable
                    {
                        if (stream == null)
                        {
                            throw new InvalidOperationException(CodeAnalysisResources.ResourceStreamProviderShouldReturnNonNullStream);
                        }

                        var count = (int)(stream.Length - stream.Position);
                        resourceWriter.WriteInt32(count);

                        int bytesWritten = resourceWriter.TryWriteBytes(stream, count);
                        if (bytesWritten != count)
                        {
                            throw new EndOfStreamException(
                                    string.Format(CultureInfo.CurrentUICulture, CodeAnalysisResources.ResourceStreamEndedUnexpectedly, bytesWritten, count));
                        }
                        resourceWriter.Align(8);
                    }
                }
                catch (Exception e)
                {
                    throw new ResourceException(_name, e);
                }
            }
        }

        public IFileReference? ExternalFile
        {
            get
            {
                return _fileReference;
            }
        }

        public uint Offset
        {
            get
            {
                return _offset;
            }
        }

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return SpecializedCollections.EmptyEnumerable<ICustomAttribute>(); }
        }

        public bool IsPublic
        {
            get { return _isPublic; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
