// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DocumentationComments
{
    internal class VisualStudioDocumentationProvider : DocumentationProvider
    {
        private readonly string _filePath;
        private readonly IVsXMLMemberIndexService _memberIndexService;
        private readonly Lazy<IVsXMLMemberIndex> _lazyMemberIndex;

        public VisualStudioDocumentationProvider(string filePath, IVsXMLMemberIndexService memberIndexService)
        {
            Contract.ThrowIfNull(memberIndexService);
            Contract.ThrowIfNull(filePath);

            _filePath = filePath;
            _memberIndexService = memberIndexService;

            _lazyMemberIndex = new Lazy<IVsXMLMemberIndex>(CreateXmlMemberIndex, isThreadSafe: true);
        }

        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken token = default(CancellationToken))
        {
            var memberIndex = _lazyMemberIndex.Value;
            if (memberIndex == null)
            {
                return "";
            }

            uint methodID;
            if (ErrorHandler.Failed(memberIndex.ParseMemberSignature(documentationMemberID, out methodID)))
            {
                return "";
            }

            string xml;
            if (ErrorHandler.Failed(memberIndex.GetMemberXML(methodID, out xml)))
            {
                return "";
            }

            return xml;
        }

        private IVsXMLMemberIndex CreateXmlMemberIndex()
        {
            // This may fail if there is no XML file available for this assembly. We'll just leave
            // memberIndex null in this case.
            IVsXMLMemberIndex memberIndex;
            _memberIndexService.CreateXMLMemberIndex(_filePath, out memberIndex);

            return memberIndex;
        }

        public override bool Equals(object obj)
        {
            var other = obj as VisualStudioDocumentationProvider;
            return other != null && string.Equals(_filePath, other._filePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_filePath);
        }
    }
}
