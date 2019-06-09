// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.FxCop.Rules.Security.Xml.SecurityXmlRules", "CA3053:UseXmlSecureResolver",
            MessageId = "System.Xml.XmlReader.Create",
            Justification = @"For the call to XmlReader.Create() below, CA3053 recommends setting the
XmlReaderSettings.XmlResolver property to either null or an instance of XmlSecureResolver.
However, the said XmlResolver property no longer exists in .NET portable framework (i.e. core framework) which means there is no way to set it.
So we suppress this error until the reporting for CA3053 has been updated to account for .NET portable framework.")]
        private static KeyValuePair<string, XDocument> MakeValue(XmlReferenceResolver resolver, string resolvedPath)
        {
            CacheMissCount++;

            using (Stream stream = resolver.OpenReadChecked(resolvedPath))
            {
                using (XmlReader reader = XmlReader.Create(stream, s_xmlSettings))
                {
                    var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                    return KeyValuePairUtil.Create(resolvedPath, document);
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
