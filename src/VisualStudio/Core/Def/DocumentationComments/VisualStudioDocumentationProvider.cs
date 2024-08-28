// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DocumentationComments;

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

    protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken token = default)
    {
        var memberIndex = _lazyMemberIndex.Value;
        if (memberIndex == null)
        {
            return "";
        }

        if (ErrorHandler.Failed(memberIndex.ParseMemberSignature(documentationMemberID, out var methodID)))
        {
            return "";
        }

        if (ErrorHandler.Failed(memberIndex.GetMemberXML(methodID, out var xml)))
        {
            return "";
        }

        return xml;
    }

    private IVsXMLMemberIndex CreateXmlMemberIndex()
    {
        // This may fail if there is no XML file available for this assembly. We'll just leave
        // memberIndex null in this case.
        _memberIndexService.CreateXMLMemberIndex(_filePath, out var memberIndex);

        return memberIndex;
    }

    public override bool Equals(object obj)
        => obj is VisualStudioDocumentationProvider other &&
           string.Equals(_filePath, other._filePath, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(_filePath);
}
