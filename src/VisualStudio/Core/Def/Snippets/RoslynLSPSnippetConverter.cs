// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using MSXML;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

[Export]
[Shared]
internal sealed class RoslynLSPSnippetConverter
{
    /// <summary>
    /// A generated random string which is used to identify LSP completion snippets from other snippets.
    /// </summary>
    private const string LSPSnippetDescriptionSentinel = "c59dfd22de644b3d9a482c3490e2b015";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynLSPSnippetConverter()
    {
    }

    public bool TryConvert(RoslynLSPSnippetList lspSnippet, [NotNullWhen(true)] out DOMDocument? vsSnippet)
    {
        XNamespace snippetNamespace = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet";

        var template = new StringBuilder();
        var declarations = new List<XElement>();
        var specifiedEndLocation = false;
        for (var i = 0; i < lspSnippet.Count; i++)
        {
            var piece = lspSnippet[i];

            switch (piece)
            {
                case RoslynLSPSnippetTextSyntax snippetText:
                    template.Append(snippetText.Text);
                    break;
                case RoslynLSPSnippetEndLocationSyntax:
                    specifiedEndLocation = true;
                    template.Append("$end$");
                    break;
                case RoslynLSPSnippetTabStopSyntax tabStop:
                    var tabStopIndexString = tabStop.TabStopIndex.ToString(CultureInfo.InvariantCulture);
                    template.Append('$').Append(tabStopIndexString).Append('$');
                    declarations.Add(new XElement(
                        snippetNamespace.GetName("Literal"),
                        new XElement(snippetNamespace.GetName("ID"), new XText(tabStopIndexString)),
                        new XElement(snippetNamespace.GetName("Default"), new XText(tabStopIndexString))));
                    break;
                case RoslynLSPSnippetPlaceholderSyntax placeholder:
                    var placeholderTabStopIndexString = placeholder.TabStopIndex.ToString(CultureInfo.InvariantCulture);
                    template.Append('$').Append(placeholderTabStopIndexString).Append('$');
                    declarations.Add(new XElement(
                        snippetNamespace.GetName("Literal"),
                        new XElement(snippetNamespace.GetName("ID"), new XText(placeholderTabStopIndexString)),
                        new XElement(snippetNamespace.GetName("Default"), new XText(placeholder.Placeholder))));

                    break;
            }
        }

        if (!specifiedEndLocation)
        {
            // No end location was specified, lets default to the end of the snippet
            template.Append("$end$");
        }

        // A snippet is manually constructed. Replacement fields are added for each argument, and the field name
        // matches the parameter name.
        // https://docs.microsoft.com/en-us/visualstudio/ide/code-snippets-schema-reference?view=vs-2019
        var vsSnippetDocument = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                snippetNamespace.GetName("CodeSnippets"),
                new XElement(
                    snippetNamespace.GetName("CodeSnippet"),
                    new XAttribute(snippetNamespace.GetName("Format"), "1.0.0"),
                    new XElement(
                        snippetNamespace.GetName("Header"),
                        new XElement(snippetNamespace.GetName("Title"), new XText("Converted LSP Snippet")),
                        new XElement(snippetNamespace.GetName("Description"), new XText(LSPSnippetDescriptionSentinel))),
                    new XElement(
                        snippetNamespace.GetName("Snippet"),
                        new XElement(snippetNamespace.GetName("Declarations"), declarations.ToArray()),
                        new XElement(snippetNamespace.GetName("Code"), new XCData(template.ToString()))))));

        vsSnippet = (DOMDocument)new DOMDocumentClass();
        if (!vsSnippet.loadXML(vsSnippetDocument.ToString(SaveOptions.OmitDuplicateNamespaces)))
        {
            vsSnippet = null;
            return false;
        }

        return true;
    }
}
