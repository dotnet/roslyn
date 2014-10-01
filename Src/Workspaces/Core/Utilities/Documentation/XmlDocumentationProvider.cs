// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class XmlDocumentationProvider : DocumentationProvider
    {
        private NonReentrantLock gate = new NonReentrantLock();
        private Dictionary<string, string> docComments;

        public static XmlDocumentationProvider Create(string filePath)
        {
            return new FileBasedXmlDocumentationProvider(filePath);
        }

        public static XmlDocumentationProvider Create(byte[] xmlDocCommentBytes)
        {
            return new ContentBasedXmlDocumentationProvider(xmlDocCommentBytes);
        }

        protected abstract XmlDocument GetXmlDocument();

        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.docComments == null)
            {
                try
                {
                    using (this.gate.DisposableWait(cancellationToken))
                    {
                        XmlDocument doc = this.GetXmlDocument();
                        this.docComments = new Dictionary<string, string>();
                        foreach (var e in doc.GetElementsByTagName("member").OfType<XmlElement>())
                        {
                            if (e.HasAttribute("name"))
                            {
                                this.docComments[e.GetAttribute("name")] = e.InnerXml;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }

            string docComment;
            return this.docComments.TryGetValue(documentationMemberID, out docComment) ? docComment : "";
        }

        private static readonly XmlReaderSettings XmlSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        private sealed class FileBasedXmlDocumentationProvider : XmlDocumentationProvider
        {
            private readonly string filePath;

            public FileBasedXmlDocumentationProvider(string filePath)
            {
                Contract.ThrowIfNull(filePath);
                Contract.Requires(PathUtilities.IsAbsolute(filePath));

                this.filePath = filePath;
            }

            protected override XmlDocument GetXmlDocument()
            {
                var doc = new XmlDocument();
                using (XmlReader reader = XmlReader.Create(this.filePath, XmlSettings))
                {
                    doc.Load(reader);
                }

                return doc;
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

        private sealed class ContentBasedXmlDocumentationProvider : XmlDocumentationProvider
        {
            private readonly byte[] xmlDocCommentBytes;

            public ContentBasedXmlDocumentationProvider(byte[] xmlDocCommentBytes)
            {
                Contract.ThrowIfNull(xmlDocCommentBytes);

                this.xmlDocCommentBytes = xmlDocCommentBytes;
            }

            protected override XmlDocument GetXmlDocument()
            {
                using (var stream = SerializableBytes.CreateReadableStream(this.xmlDocCommentBytes, CancellationToken.None))
                using (var xmlReader = XmlReader.Create(stream, XmlSettings))
                {
                    var doc = new XmlDocument();
                    doc.Load(xmlReader);
                    return doc;
                }
            }

            public override bool Equals(object obj)
            {
                var other = obj as ContentBasedXmlDocumentationProvider;
                return other != null && EqualsHelper(other);
            }

            private bool EqualsHelper(ContentBasedXmlDocumentationProvider other)
            {
                // Check for reference equality first
                if (this == other || this.xmlDocCommentBytes == other.xmlDocCommentBytes)
                {
                    return true;
                }

                // Compare byte sequences
                if (this.xmlDocCommentBytes.Length != other.xmlDocCommentBytes.Length)
                {
                    return false;
                }

                for (int i = 0; i < this.xmlDocCommentBytes.Length; i++)
                {
                    if (this.xmlDocCommentBytes[i] != other.xmlDocCommentBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return Hash.CombineValues(this.xmlDocCommentBytes);
            }
        }
    }
}