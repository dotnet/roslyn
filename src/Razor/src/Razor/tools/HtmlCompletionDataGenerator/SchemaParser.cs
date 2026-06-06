// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using System.Xml;

namespace HtmlSchemaGenerator;

/// <summary>
/// Parses html.xsd + CommonHTMLTypes.xsd + aria.xsd + html.loc into an <see cref="HtmlSchema"/>.
/// Resolves attribute group references and simpleType enumerations.
/// </summary>
internal static class SchemaParser
{
    private const string XsdNs = "http://www.w3.org/2001/XMLSchema";
    private const string VsNs = "http://schemas.microsoft.com/Visual-Studio-Intellisense";

    public static HtmlSchema Parse(string schemaDir)
    {
        // Phase 1: Parse all XSD files to build lookup tables
        var htmlXsd = Path.Combine(schemaDir, "html.xsd");
        var commonXsd = Path.Combine(schemaDir, "CommonHTMLTypes.xsd");
        var locFile = Path.Combine(schemaDir, "1033", "html.loc");

        // Parse named types and attribute groups from all included files
        var context = new ParseContext();
        var i18nXsd = Path.Combine(schemaDir, "I18Languages.xsd");
        ParseXsdFile(i18nXsd, context);  // simpleTypes like i18LanguageCode
        ParseXsdFile(commonXsd, context);
        ParseXsdFile(htmlXsd, context);

        // Parse supplemental schemas (ARIA, Angular) — attributes that apply to all elements
        var ariaXsd = Path.Combine(schemaDir, "aria.xsd");
        var angularXsd = Path.Combine(schemaDir, "angular.xsd");
        var supplementalAttributes = ParseSupplementalAttributes(ariaXsd, context);
        supplementalAttributes.AddRange(ParseSupplementalAttributes(angularXsd, context));

        // Phase 2: Resolve top-level elements into flat element data
        var elements = new List<ElementData>();
        foreach (var (name, elementNode) in context.TopLevelElements)
        {
            var element = ResolveElement(name, elementNode, context);
            if (element != null)
            {
                elements.Add(element);
            }
        }

        // Phase 3: Compute global attributes (commonAttributeGroup + supplemental)
        var globalAttrs = ResolveAttributeGroup("commonAttributeGroup", context);
        globalAttrs.AddRange(supplementalAttributes);

        // Phase 4: Parse descriptions from .loc file
        var descriptions = ParseLocFile(locFile);

        return new HtmlSchema
        {
            Elements = elements.ToImmutableArray(),
            GlobalAttributes = globalAttrs.ToImmutableArray(),
            Descriptions = descriptions,
            ContentGroups = context.ContentGroups,
        };
    }

    private static void ParseXsdFile(string path, ParseContext context)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Warning: XSD file not found: {path}");
            return;
        }

        var doc = new XmlDocument();
        doc.Load(path);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xsd", XsdNs);
        nsMgr.AddNamespace("vs", VsNs);

        // Collect named complexTypes
        foreach (XmlNode node in doc.SelectNodes("//xsd:complexType[@name]", nsMgr)!)
        {
            var name = node.Attributes!["name"]!.Value;
            context.ComplexTypes[name] = node;
        }

        // Collect named simpleTypes (for enumeration values)
        foreach (XmlNode node in doc.SelectNodes("//xsd:simpleType[@name]", nsMgr)!)
        {
            var name = node.Attributes!["name"]!.Value;
            context.SimpleTypes[name] = node;

            // Track multivalue simpleTypes (e.g., anchorLinkType, linkLinkType)
            if (string.Equals(GetVsAttribute(node, VsSchemaAttributes.MultiValue), "true", StringComparison.OrdinalIgnoreCase))
            {
                context.MultiValueSimpleTypes.Add(name);
            }
        }

        // Collect named attribute groups
        foreach (XmlNode groupNode in doc.SelectNodes("/xsd:schema/xsd:attributeGroup[@name]", nsMgr)!)
        {
            var name = groupNode.Attributes!["name"]!.Value;
            context.AttributeGroups[name] = groupNode;
        }

        // Collect top-level element declarations (only from html.xsd — not CommonHTMLTypes)
        if (path.EndsWith("html.xsd", StringComparison.OrdinalIgnoreCase))
        {
            foreach (XmlNode elemNode in doc.SelectNodes("/xsd:schema/xsd:element[@name]", nsMgr)!)
            {
                var name = elemNode.Attributes!["name"]!.Value;
                // Skip SVG-prefixed elements
                if (!name.StartsWith("svg:", StringComparison.OrdinalIgnoreCase))
                {
                    context.TopLevelElements[name] = elemNode;
                }
            }

            // Collect named content groups (flowContent, phrasingContent, etc.)
            foreach (XmlNode groupNode in doc.SelectNodes("/xsd:schema/xsd:group[@name]", nsMgr)!)
            {
                var groupName = groupNode.Attributes!["name"]!.Value;
                var children = new List<string>();
                foreach (XmlNode refNode in groupNode.SelectNodes(".//xsd:element[@ref]", nsMgr)!)
                {
                    var refName = refNode.Attributes!["ref"]!.Value;
                    children.Add(refName);
                }

                if (children.Count > 0)
                {
                    context.ContentGroups[groupName] = children;
                }
            }
        }
    }

    /// <summary>
    /// Parses a supplemental XSD file (e.g., aria.xsd, angular.xsd) and extracts global attributes
    /// declared under the <c>___all___</c> element. Reads <c>vs:icon</c> from the schema root
    /// and attaches it to each <see cref="AttributeData"/>.
    /// </summary>
    private static List<AttributeData> ParseSupplementalAttributes(string xsdPath, ParseContext context)
    {
        var attributes = new List<AttributeData>();
        if (!File.Exists(xsdPath))
        {
            return attributes;
        }

        var doc = new XmlDocument();
        doc.Load(xsdPath);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xsd", XsdNs);
        nsMgr.AddNamespace("vs", VsNs);

        // Read the schema-level vs:icon (e.g., "aria16.png", "angular16.png")
        var schemaNode = doc.DocumentElement;
        var icon = schemaNode?.GetAttribute(VsSchemaAttributes.Icon, VsNs);
        if (string.IsNullOrEmpty(icon))
        {
            icon = null;
        }

        // Supplemental schemas use ___all___ element to declare attributes that apply globally
        var allElement = doc.SelectSingleNode("//xsd:element[@name='___all___']", nsMgr);
        if (allElement == null)
        {
            return attributes;
        }

        foreach (XmlNode attrNode in allElement.SelectNodes(".//xsd:attribute[@name]", nsMgr)!)
        {
            var attr = ParseAttributeNode(attrNode, context, icon);
            if (attr != null)
            {
                attributes.Add(attr);
            }
        }

        return attributes;
    }

    private static ElementData? ResolveElement(string name, XmlNode elementNode, ParseContext context)
    {
        var vsNonBrowseable = GetVsAttribute(elementNode, VsSchemaAttributes.NonBrowseable);

        // Deprecated elements are never shown in completions — skip entirely.
        if (string.Equals(vsNonBrowseable, "true", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var vsDesc = GetVsAttribute(elementNode, VsSchemaAttributes.Description);

        var descId = -1;
        if (vsDesc != null && int.TryParse(vsDesc, out var id))
        {
            descId = id;
        }

        var vsDisallowedAncestor = GetVsAttribute(elementNode, VsSchemaAttributes.DisallowedAncestor);
        var vsImplicitClosure = string.Equals(GetVsAttribute(elementNode, VsSchemaAttributes.ImplicitClosure), "true", StringComparison.OrdinalIgnoreCase);

        var element = new ElementData
        {
            Name = name,
            DescriptionId = descId,
            HasCustomIcon = GetVsAttribute(elementNode, VsSchemaAttributes.Icon) != null,
            DisallowedAncestors = vsDisallowedAncestor,
            IsImplicitlyClosed = vsImplicitClosure,
            HasExternalCompletion = string.Equals(name, "script", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "style", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "svg", StringComparison.OrdinalIgnoreCase),
            Baseline = GetVsAttribute(elementNode, VsSchemaAttributes.Baseline) ?? "",
            BaselineDate = GetVsAttribute(elementNode, VsSchemaAttributes.BaselineDate) ?? "",
        };

        // Get attributes from the element's type or inline complexType
        var typeAttr = elementNode.Attributes?["type"]?.Value;
        if (typeAttr != null && !typeAttr.StartsWith("xsd:", StringComparison.OrdinalIgnoreCase)
                            && !typeAttr.StartsWith("svg:", StringComparison.OrdinalIgnoreCase))
        {
            // Named complexType reference
            ResolveComplexTypeAttributes(typeAttr, element.Attributes, context);
            ResolveComplexTypeChildren(typeAttr, element, context);

            // Inherit vs:implicitclosure from the complexType if not set on the element itself
            if (!element.IsImplicitlyClosed && context.ComplexTypes.TryGetValue(typeAttr, out var typeNode))
            {
                if (string.Equals(GetVsAttribute(typeNode, VsSchemaAttributes.ImplicitClosure), "true", StringComparison.OrdinalIgnoreCase))
                {
                    element.IsImplicitlyClosed = true;
                }
            }
        }

        // Inline complexType within the element
        var nsMgr = CreateNsMgr(elementNode.OwnerDocument!);
        var inlineType = elementNode.SelectSingleNode("xsd:complexType", nsMgr);
        if (inlineType != null)
        {
            CollectAttributesFromTypeNode(inlineType, element.Attributes, context);
            CollectChildrenFromTypeNode(inlineType, element, context, nsMgr);
        }

        // Check for transparent content model
        var vsContentModel = GetVsAttribute(elementNode, VsSchemaAttributes.ContentModel);
        if (string.Equals(vsContentModel, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            element.AllowedChildren.Clear();
            element.AllowedChildren.Add("*"); // sentinel for transparent
        }

        return element;
    }

    private static void ResolveComplexTypeAttributes(string typeName, List<AttributeData> target, ParseContext context)
    {
        if (!context.ComplexTypes.TryGetValue(typeName, out var typeNode))
        {
            return;
        }

        CollectAttributesFromTypeNode(typeNode, target, context);
    }

    private static void ResolveComplexTypeChildren(string typeName, ElementData element, ParseContext context)
    {
        if (!context.ComplexTypes.TryGetValue(typeName, out var typeNode))
        {
            return;
        }

        var nsMgr = CreateNsMgr(typeNode.OwnerDocument!);
        CollectChildrenFromTypeNode(typeNode, element, context, nsMgr);
    }

    private static void CollectChildrenFromTypeNode(XmlNode typeNode, ElementData element, ParseContext context, XmlNamespaceManager nsMgr)
    {
        // Check for group references (e.g., <xsd:group ref="flowContent"/>)
        foreach (XmlNode groupRef in typeNode.SelectNodes(".//xsd:group[@ref]", nsMgr)!)
        {
            var refName = groupRef.Attributes!["ref"]!.Value;
            if (context.ContentGroups.TryGetValue(refName, out var groupChildren))
            {
                foreach (var child in groupChildren)
                {
                    if (!element.AllowedChildren.Contains(child))
                    {
                        element.AllowedChildren.Add(child);
                    }
                }
            }
        }

        // Check for inline element references (e.g., <xsd:element ref="tr"/>)
        foreach (XmlNode elemRef in typeNode.SelectNodes(".//xsd:element[@ref]", nsMgr)!)
        {
            var refName = elemRef.Attributes!["ref"]!.Value;
            if (!element.AllowedChildren.Contains(refName))
            {
                element.AllowedChildren.Add(refName);
            }
        }

        // Check for inline element declarations (e.g., <xsd:element name="caption" type="..."/>)
        foreach (XmlNode elemDecl in typeNode.SelectNodes(".//xsd:element[@name]", nsMgr)!)
        {
            var declName = elemDecl.Attributes!["name"]!.Value;
            if (!element.AllowedChildren.Contains(declName))
            {
                element.AllowedChildren.Add(declName);
            }
        }
    }

    private static void CollectAttributesFromTypeNode(XmlNode typeNode, List<AttributeData> target, ParseContext context)
    {
        var nsMgr = CreateNsMgr(typeNode.OwnerDocument!);

        // Attribute group references first (base definitions)
        foreach (XmlNode groupRef in typeNode.SelectNodes("xsd:attributeGroup[@ref]", nsMgr)!)
        {
            var refName = groupRef.Attributes!["ref"]!.Value;
            // Don't expand commonAttributeGroup inline — those become global attributes
            if (!string.Equals(refName, "commonAttributeGroup", StringComparison.OrdinalIgnoreCase))
            {
                var resolved = ResolveAttributeGroup(refName, context);
                target.AddRange(resolved);
            }
        }

        // Inline attributes second (overrides)
        foreach (XmlNode attrNode in typeNode.SelectNodes("xsd:attribute[@name]", nsMgr)!)
        {
            var attr = ParseAttributeNode(attrNode, context);
            if (attr != null)
            {
                // Remove any earlier definition with the same name (inline overrides group)
                target.RemoveAll(a => string.Equals(a.Name, attr.Name, StringComparison.OrdinalIgnoreCase));
                target.Add(attr);
            }
        }
    }

    private static List<AttributeData> ResolveAttributeGroup(string groupName, ParseContext context)
    {
        var result = new List<AttributeData>();
        if (!context.AttributeGroups.TryGetValue(groupName, out var groupNode))
        {
            return result;
        }

        var nsMgr = CreateNsMgr(groupNode.OwnerDocument!);

        // Nested attribute group references first (base definitions)
        foreach (XmlNode nestedRef in groupNode.SelectNodes("xsd:attributeGroup[@ref]", nsMgr)!)
        {
            var refName = nestedRef.Attributes!["ref"]!.Value;
            var nested = ResolveAttributeGroup(refName, context);
            result.AddRange(nested);
        }

        // Direct attributes in the group second (overrides nested groups)
        foreach (XmlNode attrNode in groupNode.SelectNodes("xsd:attribute[@name]", nsMgr)!)
        {
            var attr = ParseAttributeNode(attrNode, context);
            if (attr != null)
            {
                result.RemoveAll(a => string.Equals(a.Name, attr.Name, StringComparison.OrdinalIgnoreCase));
                result.Add(attr);
            }
        }

        return result;
    }

    private static AttributeData? ParseAttributeNode(XmlNode attrNode, ParseContext context, string? icon = null)
    {
        var name = attrNode.Attributes?["name"]?.Value;
        if (name == null)
        {
            return null;
        }

        var vsNonBrowseable = GetVsAttribute(attrNode, VsSchemaAttributes.NonBrowseable);
        if (string.Equals(vsNonBrowseable, "true", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var vsStandalone = GetVsAttribute(attrNode, VsSchemaAttributes.Standalone);
        var vsDesc = GetVsAttribute(attrNode, VsSchemaAttributes.Description);
        var vsOmType = GetVsAttribute(attrNode, VsSchemaAttributes.OmType);
        var vsMultiValue = GetVsAttribute(attrNode, VsSchemaAttributes.MultiValue);

        var isBoolean = string.Equals(vsStandalone, "true", StringComparison.OrdinalIgnoreCase);

        // Also detect xsd:boolean type as boolean (e.g., aria-atomic type="xsd:boolean")
        var typeAttr = attrNode.Attributes?["type"]?.Value;
        if (!isBoolean && string.Equals(typeAttr, "xsd:boolean", StringComparison.OrdinalIgnoreCase))
        {
            isBoolean = true;
        }
        var isEvent = string.Equals(vsOmType, "event", StringComparison.OrdinalIgnoreCase);

        var descId = -1;
        if (vsDesc != null && int.TryParse(vsDesc, out var id))
        {
            descId = id;
        }

        // Determine if this attribute's value completion is owned by an external provider.
        // Sources: xsd:anyURI type (URLs/file paths), vs:multivalue (space-separated values),
        // multivalue simpleType reference, or hardcoded names (class, style, id).
        var typeRef = attrNode.Attributes?["type"]?.Value;
        var isMultiValue = string.Equals(vsMultiValue, "true", StringComparison.OrdinalIgnoreCase)
            || (typeRef != null && context.MultiValueSimpleTypes.Contains(typeRef));

        var hasExternalCompletion = isMultiValue
            || string.Equals(typeRef, "xsd:anyURI", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "class", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "style", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "id", StringComparison.OrdinalIgnoreCase);

        var attr = new AttributeData
        {
            Name = name,
            DescriptionId = descId,
            IsBoolean = isBoolean,
            IsEvent = isEvent,
            Icon = icon,
            HasExternalCompletion = hasExternalCompletion,
            Baseline = GetVsAttribute(attrNode, VsSchemaAttributes.Baseline) ?? "",
            BaselineDate = GetVsAttribute(attrNode, VsSchemaAttributes.BaselineDate) ?? "",
        };

        // Resolve enumeration values
        if (typeRef != null && !typeRef.StartsWith("xsd:", StringComparison.OrdinalIgnoreCase))
        {
            // Named simpleType reference
            var values = ResolveSimpleTypeValues(typeRef, context);
            attr.Values.AddRange(values);
            attr.ValueTypeName = typeRef;
        }

        // Inline simpleType with enumerations
        var nsMgr = CreateNsMgr(attrNode.OwnerDocument!);
        var inlineType = attrNode.SelectSingleNode("xsd:simpleType", nsMgr);
        if (inlineType != null)
        {
            var values = CollectEnumerationValues(inlineType, nsMgr);
            attr.Values.AddRange(values);
        }

        return attr;
    }

    private static List<string> ResolveSimpleTypeValues(string typeName, ParseContext context)
    {
        if (!context.SimpleTypes.TryGetValue(typeName, out var typeNode))
        {
            return new List<string>();
        }

        var nsMgr = CreateNsMgr(typeNode.OwnerDocument!);
        return CollectEnumerationValues(typeNode, nsMgr);
    }

    private static List<string> CollectEnumerationValues(XmlNode typeNode, XmlNamespaceManager nsMgr)
    {
        var values = new List<string>();
        foreach (XmlNode enumNode in typeNode.SelectNodes(".//xsd:enumeration[@value]", nsMgr)!)
        {
            var value = enumNode.Attributes!["value"]!.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value.Trim());
            }
        }
        return values;
    }

    private static Dictionary<int, DescriptionData> ParseLocFile(string locPath)
    {
        var descriptions = new Dictionary<int, DescriptionData>();
        if (!File.Exists(locPath))
        {
            Console.Error.WriteLine($"Warning: .loc file not found: {locPath}");
            return descriptions;
        }

        var doc = new XmlDocument();
        doc.Load(locPath);

        foreach (XmlNode node in doc.SelectNodes("root/*")!)
        {
            var locIdAttr = node.Attributes?["_locid"];
            if (locIdAttr != null && int.TryParse(locIdAttr.Value, out var id))
            {
                var url = node.Attributes?["url"]?.Value ?? "";
                var text = NormalizeWhitespace(node.InnerText);
                descriptions[id] = new DescriptionData { Text = text, Url = url };
            }
        }

        return descriptions;
    }

    /// <summary>
    /// Normalizes whitespace in LOC file descriptions: skips \r, preserves \n (for markdown
    /// paragraph/bullet structure), and collapses runs of spaces/tabs within a line.
    /// </summary>
    private static string NormalizeWhitespace(string input)
    {
        var trimmed = input.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var prevChar = 'X';

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];

            if (ch == '\r')
            {
                // Skip carriage returns; \n handles line breaks.
            }
            else if (ch == '\t' || ch == ' ')
            {
                if (prevChar != ' ' && prevChar != '\t')
                {
                    sb.Append(' ');
                }
            }
            else
            {
                sb.Append(ch);
            }

            prevChar = ch;
        }

        return sb.ToString();
    }

    private static string? GetVsAttribute(XmlNode node, string localName)
    {
        return node.Attributes?[localName, VsNs]?.Value;
    }

    private static XmlNamespaceManager CreateNsMgr(XmlDocument doc)
    {
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("xsd", XsdNs);
        nsMgr.AddNamespace("vs", VsNs);
        return nsMgr;
    }
}

internal sealed class ParseContext
{
    public Dictionary<string, XmlNode> ComplexTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, XmlNode> SimpleTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, XmlNode> AttributeGroups { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, XmlNode> TopLevelElements { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// SimpleType names that have vs:multivalue="true" (e.g., "anchorLinkType", "linkLinkType").
    /// Attributes referencing these types inherit the multivalue behavior.
    /// </summary>
    public HashSet<string> MultiValueSimpleTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Named content groups (e.g., "flowContent" → ["a","abbr","address",...]).
    /// Populated during XSD parsing from &lt;xsd:group name="..."&gt; definitions.
    /// </summary>
    public Dictionary<string, List<string>> ContentGroups { get; } = new(StringComparer.OrdinalIgnoreCase);
}
