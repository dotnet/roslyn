// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class XmlDocumentationProvider : DocumentationProvider
    {
        private readonly NonReentrantLock _gate = new NonReentrantLock();
        private Dictionary<string, string> _docComments;

        protected abstract Stream GetSourceStream(CancellationToken cancellationToken);

        public static XmlDocumentationProvider Create(byte[] xmlDocCommentBytes)
        {
            return new ContentBasedXmlDocumentationProvider(xmlDocCommentBytes);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.FxCop.Rules.Security.Xml.SecurityXmlRules", "CA3053:UseXmlSecureResolver",
            MessageId = "System.Xml.XmlReader.Create",
            Justification = @"For the call to XmlReader.Create() below, CA3053 recommends setting the
XmlReaderSettings.XmlResolver property to either null or an instance of XmlSecureResolver.
However, the said XmlResolver property no longer exists in .NET portable framework (i.e. core framework) which means there is no way to set it.
So we suppress this error until the reporting for CA3053 has been updated to account for .NET portable framework.")]
        private XDocument GetXDocument(CancellationToken cancellationToken)
        {
            using (var stream = GetSourceStream(cancellationToken))
            using (var xmlReader = XmlReader.Create(stream, s_xmlSettings))
            {
                return XDocument.Load(xmlReader);
            }
        }

        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_docComments == null)
            {
                using (_gate.DisposableWait(cancellationToken))
                {
                    try
                    {
                        _docComments = new Dictionary<string, string>();

                        XDocument doc = this.GetXDocument(cancellationToken);
                        foreach (var e in doc.Descendants("member"))
                        {
                            if (e.Attribute("name") != null)
                            {
                                _docComments[e.Attribute("name").Value] = string.Concat(e.Nodes());
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

            protected override Stream GetSourceStream(CancellationToken cancellationToken)
            {
                return SerializableBytes.CreateReadableStream(_xmlDocCommentBytes, cancellationToken);
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
