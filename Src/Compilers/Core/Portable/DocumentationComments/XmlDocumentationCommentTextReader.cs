// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private XmlReader reader;
        private readonly Reader textReader = new Reader();

        private static readonly ObjectPool<XmlDocumentationCommentTextReader> pool = 
            new ObjectPool<XmlDocumentationCommentTextReader>(() => new XmlDocumentationCommentTextReader(), size: 2);

        public static XmlException ParseAndGetException(string text)
        {
            var reader = pool.Allocate();
            var retVal = reader.ParseInternal(text);
            pool.Free(reader);
            return retVal;
        }

        private static readonly XmlReaderSettings xmlSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };

        // internal for testing
        internal XmlException ParseInternal(string text)
        {
            textReader.SetText(text);

            if (reader == null)
            {
                reader = XmlReader.Create(textReader, xmlSettings);
            }

            try
            {
                do
                {
                    reader.Read();
                }
                while (!Reader.ReachedEnd(reader));

                return null;
            }
            catch (XmlException ex)
            {
                // The reader is in a bad state, so dispose of it and recreate a new one next time we get called.
                reader.Dispose();
                reader = null;
                textReader.Reset();
                return ex;
            }
        }           
    }
}
