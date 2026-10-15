// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DocumentationCommentCompiler
    {
        private const int MaxInheritedDocumentationNodeCount = 10_000;
        private const int MaxInheritedDocumentationExpansionCount = 10_000;
        private const int MaxInheritedDocumentationDepth = 128;

        // Keep candidate selection and XPath handling aligned with Workspaces' ISymbolExtensions.GetDocumentationComment.
        // The compiler layer cannot reference Workspaces.
        private void AddInheritedDocumentationTags(
            Symbol symbol,
            string xmlText,
            ref HashSet<ParameterSymbol> documentedParameters,
            ref HashSet<TypeParameterSymbol> documentedTypeParameters)
        {
            XElement document;
            try
            {
                document = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                return;
            }

            var parameterNames = new HashSet<string>(StringComparer.Ordinal);
            var typeParameterNames = new HashSet<string>(StringComparer.Ordinal);
            var publicSymbol = symbol.GetPublicSymbol();
            var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { publicSymbol };
            var expandedDocumentation = new Dictionary<ISymbol, XDocument>(SymbolEqualityComparer.Default);
            var remainingNodeBudget = MaxInheritedDocumentationNodeCount;
            var remainingExpansionBudget = MaxInheritedDocumentationExpansionCount;
            ExpandInheritedDocumentation(publicSymbol, document, visitedSymbols, expandedDocumentation, ref remainingNodeBudget, ref remainingExpansionBudget, out _);
            CollectDocumentationNames(document, parameterNames, typeParameterNames);

            if (documentedParameters != null)
            {
                foreach (var parameter in GetParameters(symbol))
                {
                    if (parameterNames.Contains(parameter.Name))
                    {
                        documentedParameters.Add(parameter);
                    }
                }
            }

            if (documentedTypeParameters != null)
            {
                foreach (var typeParameter in GetTypeParameters(symbol))
                {
                    if (typeParameterNames.Contains(typeParameter.Name))
                    {
                        documentedTypeParameters.Add(typeParameter);
                    }
                }
            }
        }

        private void ExpandInheritedDocumentation(
            ISymbol member,
            XContainer container,
            HashSet<ISymbol> visitedSymbols,
            Dictionary<ISymbol, XDocument> expandedDocumentation,
            ref int remainingNodeBudget,
            ref int remainingExpansionBudget,
            out bool isComplete)
        {
            isComplete = true;
            foreach (var element in container.Descendants().ToArray())
            {
                if (!ElementNameIs(element, DocumentationCommentXmlNames.InheritdocElementName))
                {
                    continue;
                }

                var cref = element.Attribute(DocumentationCommentXmlNames.CrefAttributeName);
                var inheritedSymbol = cref == null
                    ? GetInheritdocCandidate(member)
                    : DocumentationCommentId.GetFirstSymbolForDeclarationId(cref.Value, _compilation);
                if (inheritedSymbol == null)
                {
                    element.Remove();
                    continue;
                }

                var path = element.Attribute(DocumentationCommentXmlNames.PathAttributeName)?.Value;
                var xpath = string.IsNullOrEmpty(path)
                    ? BuildXPathForElement(element.Parent)
                    : path.StartsWith("/", StringComparison.Ordinal) ? "/*" + path : path;
                var inheritedDocument = GetExpandedDocumentation(inheritedSymbol, visitedSymbols, expandedDocumentation, ref remainingNodeBudget, ref remainingExpansionBudget, out var inheritedIsComplete);
                isComplete &= inheritedIsComplete;
                var inheritedNodes = TrySelectNodes(inheritedDocument, xpath).ToArray();
                var nodeCount = 0;
                foreach (var node in inheritedNodes)
                {
                    nodeCount += CountNodes(node);
                }

                if (nodeCount > remainingNodeBudget)
                {
                    // Repeated inheritdoc references can multiply XML exponentially; stop conservatively before diagnostics exhaust memory.
                    element.Remove();
                    isComplete = false;
                    continue;
                }

                remainingNodeBudget -= nodeCount;
                element.ReplaceWith(inheritedNodes.Select(static node => CopyNode(node)).ToArray());
            }
        }

        private XDocument GetExpandedDocumentation(
            ISymbol member,
            HashSet<ISymbol> visitedSymbols,
            Dictionary<ISymbol, XDocument> expandedDocumentation,
            ref int remainingNodeBudget,
            ref int remainingExpansionBudget,
            out bool isComplete)
        {
            if (expandedDocumentation.TryGetValue(member, out var cachedDocument))
            {
                isComplete = true;
                return cachedDocument;
            }

            if (visitedSymbols.Count >= MaxInheritedDocumentationDepth)
            {
                isComplete = false;
                return new XDocument(new XElement("doc"));
            }

            if (remainingExpansionBudget == 0)
            {
                isComplete = false;
                return new XDocument(new XElement("doc"));
            }

            remainingExpansionBudget--;

            if (!visitedSymbols.Add(member))
            {
                isComplete = false;
                return new XDocument(new XElement("doc"));
            }

            try
            {
                var xmlText = member.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: _cancellationToken);
                if (string.IsNullOrEmpty(xmlText))
                {
                    if (IsEligibleForAutomaticInheritdoc(member) && GetInheritdocCandidate(member) is { } candidate)
                    {
                        var inheritedDocument = GetExpandedDocumentation(candidate, visitedSymbols, expandedDocumentation, ref remainingNodeBudget, ref remainingExpansionBudget, out isComplete);
                        if (isComplete)
                        {
                            expandedDocumentation[member] = inheritedDocument;
                        }

                        return inheritedDocument;
                    }

                    isComplete = true;
                    var emptyDocument = new XDocument(new XElement("doc"));
                    expandedDocumentation[member] = emptyDocument;
                    return emptyDocument;
                }

                XDocument document;
                try
                {
                    document = XDocument.Parse(xmlText, LoadOptions.PreserveWhitespace);
                }
                catch (XmlException)
                {
                    isComplete = true;
                    var emptyDocument = new XDocument(new XElement("doc"));
                    expandedDocumentation[member] = emptyDocument;
                    return emptyDocument;
                }

                ExpandInheritedDocumentation(member, document, visitedSymbols, expandedDocumentation, ref remainingNodeBudget, ref remainingExpansionBudget, out isComplete);
                if (isComplete)
                {
                    expandedDocumentation[member] = document;
                }

                return document;
            }
            finally
            {
                visitedSymbols.Remove(member);
            }
        }

        private static int CountNodes(XNode node)
        {
            var count = 1;
            if (node is XContainer container)
            {
                foreach (var child in container.Nodes())
                {
                    count += CountNodes(child);
                }
            }

            return count;
        }

        private static void CollectDocumentationNames(
            XContainer container,
            HashSet<string> parameterNames,
            HashSet<string> typeParameterNames)
        {
            foreach (var element in container.Descendants())
            {
                if (ElementNameIs(element, DocumentationCommentXmlNames.ParameterElementName))
                {
                    AddName(element, parameterNames);
                }
                else if (ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterElementName))
                {
                    AddName(element, typeParameterNames);
                }
            }
        }

        private static bool IsEligibleForAutomaticInheritdoc(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                return method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor)
                    && (method.IsOverride || GetInheritdocCandidate(member) != null);
            }

            return member.IsOverride
                || (member is IPropertySymbol or IEventSymbol) && GetInheritdocCandidate(member) != null;
        }

        private static ISymbol GetInheritdocCandidate(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                if (!method.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return method.ExplicitInterfaceImplementations[0];
                }

                if (method.IsOverride)
                {
                    return method.OverriddenMethod;
                }

                if (method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                {
                    return GetMatchingBaseConstructor(method);
                }

                return GetImplicitInterfaceImplementation(method);
            }

            if (member is IPropertySymbol property)
            {
                if (!property.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return property.ExplicitInterfaceImplementations[0];
                }

                if (property.IsOverride)
                {
                    return property.OverriddenProperty;
                }

                return GetImplicitInterfaceImplementation(property);
            }

            if (member is IEventSymbol @event)
            {
                if (!@event.ExplicitInterfaceImplementations.IsEmpty)
                {
                    return @event.ExplicitInterfaceImplementations[0];
                }

                if (@event.IsOverride)
                {
                    return @event.OverriddenEvent;
                }

                return GetImplicitInterfaceImplementation(@event);
            }

            if (member is INamedTypeSymbol type)
            {
                return type.TypeKind switch
                {
                    TypeKind.Class => type.BaseType,
                    TypeKind.Interface => type.Interfaces.FirstOrDefault(),
                    _ => null,
                };
            }

            return null;
        }

        private static ISymbol GetMatchingBaseConstructor(IMethodSymbol method)
        {
            var baseType = method.ContainingType?.BaseType;
            if (baseType == null)
            {
                return null;
            }

            foreach (var candidate in baseType.GetMembers().OfType<IMethodSymbol>())
            {
                if (candidate.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor)
                    || candidate.Parameters.Length != method.Parameters.Length
                    || candidate.IsStatic != method.IsStatic
                    || !SymbolEqualityComparer.Default.Equals(candidate.ReturnType, method.ReturnType))
                {
                    continue;
                }

                var parametersMatch = true;
                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    if (!SymbolEqualityComparer.Default.Equals(candidate.Parameters[i].Type, method.Parameters[i].Type))
                    {
                        parametersMatch = false;
                        break;
                    }
                }

                if (parametersMatch)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static ISymbol GetImplicitInterfaceImplementation(ISymbol member)
        {
            var containingType = member.ContainingType;
            if (containingType == null)
            {
                return null;
            }

            foreach (var @interface in containingType.AllInterfaces)
            {
                foreach (var interfaceMember in @interface.GetMembers())
                {
                    if (SymbolEqualityComparer.Default.Equals(
                        containingType.FindImplementationForInterfaceMember(interfaceMember),
                        member))
                    {
                        return interfaceMember;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<XNode> TrySelectNodes(XNode node, string xpath)
        {
            try
            {
                var result = System.Xml.XPath.Extensions.XPathEvaluate(node, xpath);
                return result is IEnumerable nodes
                    ? nodes.OfType<XNode>().Where(static selectedNode => selectedNode is not XDocument).ToArray()
                    : Array.Empty<XNode>();
            }
            catch (InvalidOperationException)
            {
                return Array.Empty<XNode>();
            }
            catch (XPathException)
            {
                return Array.Empty<XNode>();
            }
        }

        private static string BuildXPathForElement(XElement element)
        {
            if (ElementNameIs(element, "member") || ElementNameIs(element, "doc"))
            {
                return "/*/node()[not(self::overloads)]";
            }

            var path = "/node()[not(self::overloads)]";
            for (var current = element; current != null; current = current.Parent)
            {
                var currentName = current.Name.ToString();
                if (ElementNameIs(current, "member") || ElementNameIs(current, "doc"))
                {
                    currentName = "*";
                }

                path = "/" + currentName + path;
            }

            return path;
        }

        private static void AddName(XElement element, HashSet<string> names)
        {
            if (element.Attribute(DocumentationCommentXmlNames.NameAttributeName) is { } nameAttribute)
            {
                var nameSyntax = ParseNameAttribute(nameAttribute.ToString(), element.Name.LocalName);
                names.Add(nameSyntax.Identifier.Identifier.ValueText);
            }
        }

        private static XmlNameAttributeSyntax ParseNameAttribute(string attributeText, string elementName)
        {
            // NOTE: Rather than introducing a new code path that will have to be kept in
            // sync with other mode changes distributed throughout Lexer, SyntaxParser, and
            // DocumentationCommentParser, we'll just wrap the text in some lexable syntax
            // and then extract the piece we want.
            string commentText = string.Format(@"/// <{0} {1}/>", elementName, attributeText);

            SyntaxTriviaList leadingTrivia = SyntaxFactory.ParseLeadingTrivia(commentText, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
            Debug.Assert(leadingTrivia.Count == 1);
            SyntaxTrivia trivia = leadingTrivia.ElementAt(0);
            DocumentationCommentTriviaSyntax structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
            Debug.Assert(structure.Content.Count == 2);
            XmlEmptyElementSyntax elementSyntax = (XmlEmptyElementSyntax)structure.Content[1];
            Debug.Assert(elementSyntax.Attributes.Count == 1);
            return (XmlNameAttributeSyntax)elementSyntax.Attributes[0];
        }

        private static XNode CopyNode(XNode node)
            => node switch
            {
                XElement element => new XElement(element),
                XCData cdata => new XCData(cdata.Value),
                XText text => new XText(text.Value),
                XComment comment => new XComment(comment.Value),
                XProcessingInstruction instruction => new XProcessingInstruction(instruction.Target, instruction.Data),
                _ => throw ExceptionUtilities.UnexpectedValue(node.NodeType),
            };

        private static bool ElementNameIs(XElement element, string name)
            => string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);
    }
}
