// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class FileBasedXmlDocumentationProvider : XmlDocumentationProvider
    {
        private readonly string filePath;

        public FileBasedXmlDocumentationProvider(string filePath)
        {
            Contract.ThrowIfNull(filePath);
            Contract.Requires(PathUtilities.IsAbsolute(filePath));

            this.filePath = filePath;
        }

        protected override XDocument GetXDocument()
        {
            return XDocument.Load(filePath);
        }

        public override bool Equals(object obj)
        {
            var other = obj as FileBasedXmlDocumentationProvider;
            return other != null && this.filePath == other.filePath;
        }

        public override int GetHashCode()
        {
            return this.filePath.GetHashCode();
        }
    }
}
