// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used by the DocumentationCommentCompiler(s) to check doc comments for XML parse errors.
    /// As a performance optimization, this class tries to re-use the same underlying <see cref="XmlReader"/> instance
    /// when possible. 
    /// </summary>
    internal partial class XmlDocumentationCommentTextReader
    {
        private XmlReader _reader;
        private readonly Reader _textReader = new Reader();

        private static readonly ObjectPool<XmlDocumentationCommentTextReader> s_pool =
            new ObjectPool<XmlDocumentationCommentTextReader>(() => new XmlDocumentationCommentTextReader(), size: 2);

        public static XmlException ParseAndGetException(string text)
        {
            var reader = s_pool.Allocate();
            var retVal = reader.ParseInternal(text);
            s_pool.Free(reader);
            return retVal;
        }

        private static readonly XmlReaderSettings s_xmlSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };

        // internal for testing
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.FxCop.Rules.Security.Xml.SecurityXmlRules", "CA3053:UseXmlSecureResolver",
            MessageId = "System.Xml.XmlReader.Create",
            Justification = @"For the call to XmlReader.Create() below, CA3053 recommends setting the
XmlReaderSettings.XmlResolver property to either null or an instance of XmlSecureResolver.
However, the said XmlResolver property no longer exists in .NET portable framework (i.e. core framework) which means there is no way to set it.
So we suppress this error until the reporting for CA3053 has been updated to account for .NET portable framework.")]
        internal XmlException ParseInternal(string text)
        {
            _textReader.SetText(text);

            if (_reader == null)
            {
                _reader = XmlReader.Create(_textReader, s_xmlSettings);
            }

            try
            {
                do
                {
                    _reader.Read();
                }
                while (!Reader.ReachedEnd(_reader));

                if (_textReader.Eof)
                {
                    _reader.Dispose();
                    _reader = null;
                    _textReader.Reset();
                }

                return null;
            }
            catch (XmlException ex)
            {
                // The reader is in a bad state, so dispose of it and recreate a new one next time we get called.
                _reader.Dispose();
                _reader = null;
                _textReader.Reset();
                return ex;
            }
        }
    }
}
