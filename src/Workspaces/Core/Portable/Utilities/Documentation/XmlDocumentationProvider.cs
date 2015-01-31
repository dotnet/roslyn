// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class XmlDocumentationProvider : DocumentationProvider
    {
        private NonReentrantLock _gate = new NonReentrantLock();
        private Dictionary<string, string> _docComments;

        public static XmlDocumentationProvider Create(byte[] xmlDocCommentBytes)
        {
            return new ContentBasedXmlDocumentationProvider(xmlDocCommentBytes);
        }

        protected abstract XDocument GetXDocument();

        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_docComments == null)
            {
                using (_gate.DisposableWait(cancellationToken))
                {
                    try
                    {
                        _docComments = new Dictionary<string, string>();

                        XDocument doc = this.GetXDocument();
                        foreach (var e in doc.Descendants("member"))
                        {
                            if (e.Attribute("name") != null)
                            {
                                _docComments[e.Attribute("name").Value] = e.Value;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            string docComment;
            return _docComments.TryGetValue(documentationMemberID, out docComment) ? docComment : "";
        }

        private static readonly XmlReaderSettings s_xmlSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
        };

        private sealed class ContentBasedXmlDocumentationProvider : XmlDocumentationProvider
        {
            private readonly byte[] _xmlDocCommentBytes;

            public ContentBasedXmlDocumentationProvider(byte[] xmlDocCommentBytes)
            {
                Contract.ThrowIfNull(xmlDocCommentBytes);

                _xmlDocCommentBytes = xmlDocCommentBytes;
            }

            protected override XDocument GetXDocument()
            {
                using (var stream = SerializableBytes.CreateReadableStream(_xmlDocCommentBytes, CancellationToken.None))
                using (var xmlReader = XmlReader.Create(stream, s_xmlSettings))
                {
                    return XDocument.Load(xmlReader);
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
                if (this == other || _xmlDocCommentBytes == other._xmlDocCommentBytes)
                {
                    return true;
                }

                // Compare byte sequences
                if (_xmlDocCommentBytes.Length != other._xmlDocCommentBytes.Length)
                {
                    return false;
                }

                for (int i = 0; i < _xmlDocCommentBytes.Length; i++)
                {
                    if (_xmlDocCommentBytes[i] != other._xmlDocCommentBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return Hash.CombineValues(_xmlDocCommentBytes);
            }
        }
    }
}
