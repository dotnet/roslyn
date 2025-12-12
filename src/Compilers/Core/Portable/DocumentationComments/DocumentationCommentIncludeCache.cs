// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class DocumentationCommentIncludeCache : CachingFactory<string, KeyValuePair<string, XDocument>>
    {
        // TODO: tune
        private const int Size = 5;

        /// <summary>
        /// WARN: This is a test hook - do not take a dependency on this.
        /// </summary>
        internal static int CacheMissCount { get; private set; }

        public DocumentationCommentIncludeCache(XmlReferenceResolver resolver)
            : base(Size,
                   key => MakeValue(resolver, key),
                   KeyHashCode,
                   KeyValueEquality)
        {
            CacheMissCount = 0;
        }

        public XDocument GetOrMakeDocument(string resolvedPath)
        {
            return GetOrMakeValue(resolvedPath).Value;
        }

        private static readonly XmlReaderSettings s_xmlSettings = new XmlReaderSettings()
        {
            // Dev12 prohibits DTD
            DtdProcessing = DtdProcessing.Prohibit
        };

        /// <exception cref="IOException"></exception>
        /// <exception cref="XmlException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static KeyValuePair<string, XDocument> MakeValue(XmlReferenceResolver resolver, string resolvedPath)
        {
            CacheMissCount++;

            using (Stream stream = resolver.OpenReadChecked(resolvedPath))
            {
                using (XmlReader reader = XmlReader.Create(stream, s_xmlSettings))
                {
                    var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                    return KeyValuePair.Create(resolvedPath, document);
                }
            }
        }

        private static int KeyHashCode(string resolvedPath)
        {
            return resolvedPath.GetHashCode();
        }

        private static bool KeyValueEquality(string resolvedPath, KeyValuePair<string, XDocument> pathAndDocument)
        {
            return resolvedPath == pathAndDocument.Key;
        }
    }
}
