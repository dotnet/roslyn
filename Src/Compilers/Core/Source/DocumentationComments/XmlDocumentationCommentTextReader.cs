// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used by the DocumentationCommentCompiler(s) to check doc comments for XML parse errors.
    /// As a performance optimization, this class tries to re-use the same underlying XmlTextReader instance
    /// when possible.
    /// </summary>
    internal partial class XmlDocumentationCommentTextReader
    {
        private XmlTextReader reader;
        private readonly XmlStream stream = new XmlStream();

        private static readonly ObjectPool<XmlDocumentationCommentTextReader>.Factory factory = () => new XmlDocumentationCommentTextReader();
        private static readonly ObjectPool<XmlDocumentationCommentTextReader> pool = new ObjectPool<XmlDocumentationCommentTextReader>(factory, size: 2);

        public static XmlException ParseAndGetException(string text)
        {
            var reader = pool.Allocate();
            var retVal = reader.ParseInternal(text);
            pool.Free(reader);
            return retVal;
        }

        private XmlException ParseInternal(string text)
        {
            stream.SetText(text);

            if (reader == null)
            {
                reader = new XmlTextReader(stream);
            }

            try
            {
                while (!ReachedEnd())
                {
                    reader.Read();
                }

                // No errors. Reset the text reader for next time.
                reader.ResetState();
                return null;
            }
            catch (XmlException ex)
            {
                // The reader is in a bad state and ResetState isn't going to help (because it has already
                // consumed bad text from the underlying stream and that can't be undone).
                // So, dispose of it and recreate a new one next time we get called.
                reader.Dispose();
                reader = null;
                return ex;
            }
        }

        private bool ReachedEnd()
        {
            return reader.Depth == 0 &&
                reader.NodeType == XmlNodeType.EndElement &&
                reader.Name == XmlStream.RootElementName;
        }
    }
}
