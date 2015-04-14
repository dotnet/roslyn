// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Roslyn.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class FileBasedXmlDocumentationProvider : XmlDocumentationProvider
    {
        private readonly string _filePath;

        public FileBasedXmlDocumentationProvider(string filePath)
        {
            Contract.ThrowIfNull(filePath);
            Contract.Requires(PathUtilities.IsAbsolute(filePath));

            _filePath = filePath;
        }

        protected override Stream GetSourceStream(CancellationToken cancellationToken)
        {
            return new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        }

        public override bool Equals(object obj)
        {
            var other = obj as FileBasedXmlDocumentationProvider;
            return other != null && _filePath == other._filePath;
        }

        public override int GetHashCode()
        {
            return _filePath.GetHashCode();
        }
    }
}
