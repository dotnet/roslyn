// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DocumentationCommentCompiler
    {
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
                document = XElement.Parse("<doc>" + xmlText + "</doc>", LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                return;
            }

            var parameterNames = new HashSet<string>(StringComparer.Ordinal);
            var typeParameterNames = new HashSet<string>(StringComparer.Ordinal);
            var publicSymbol = symbol.GetPublicSymbol();
            var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { publicSymbol };
            CollectDocumentationNames(publicSymbol, document, parameterNames, typeParameterNames, visitedSymbols);

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

        private void CollectDocumentationNames(
            ISymbol member,
            XNode node,
            HashSet<string> parameterNames,
            HashSet<string> typeParameterNames,
            HashSet<ISymbol> visitedSymbols)
        {
            if (node is XElement element)
            {
                if (ElementNameIs(element, DocumentationCommentXmlNames.ParameterElementName))
                {
                    AddName(element, parameterNames);
                }
                else if (ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterElementName))
                {
                    AddName(element, typeParameterNames);
                }
                else if (ElementNameIs(element, DocumentationCommentXmlNames.InheritdocElementName))
                {
                    var cref = element.Attribute(DocumentationCommentXmlNames.CrefAttributeName);
                    var inheritedSymbol = cref == null
                        ? GetInheritdocCandidate(member)
                        : DocumentationCommentId.GetFirstSymbolForDeclarationId(cref.Value, _compilation);

                    if (inheritedSymbol != null)
                    {
                        var path = element.Attribute(DocumentationCommentXmlNames.PathAttributeName)?.Value;
                        var xpath = string.IsNullOrEmpty(path)
                            ? BuildXPathForElement(element.Parent)
                            : path.StartsWith("/", StringComparison.Ordinal) ? "/*" + path : path;

                        CollectInheritedDocumentationNames(inheritedSymbol, xpath, parameterNames, typeParameterNames, visitedSymbols);
                    }

                    return;
                }

                foreach (var child in element.Nodes())
                {
                    CollectDocumentationNames(member, child, parameterNames, typeParameterNames, visitedSymbols);
                }
            }
        }

        private void CollectInheritedDocumentationNames(
            ISymbol member,
            string xpath,
            HashSet<string> parameterNames,
            HashSet<string> typeParameterNames,
            HashSet<ISymbol> visitedSymbols)
        {
            if (!visitedSymbols.Add(member))
            {
                return;
            }

            try
            {
                var xmlText = member.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: _cancellationToken);
                if (string.IsNullOrEmpty(xmlText))
                {
                    if (IsEligibleForAutomaticInheritdoc(member) && GetInheritdocCandidate(member) is { } candidate)
                    {
                        CollectInheritedDocumentationNames(candidate, xpath, parameterNames, typeParameterNames, visitedSymbols);
                    }

                    return;
                }

                XDocument document;
                try
                {
                    document = XDocument.Parse(xmlText, LoadOptions.PreserveWhitespace);
                }
                catch (XmlException)
                {
                    return;
                }

                foreach (var inheritedNode in TrySelectNodes(document, xpath))
                {
                    CollectDocumentationNames(member, inheritedNode, parameterNames, typeParameterNames, visitedSymbols);
                }
            }
            finally
            {
                visitedSymbols.Remove(member);
            }
        }

        private static bool IsEligibleForAutomaticInheritdoc(ISymbol member)
            => member.IsOverride
                || (member is IMethodSymbol or IPropertySymbol or IEventSymbol) && GetInheritdocCandidate(member) != null;

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
                return result is IEnumerable nodes ? nodes.OfType<XNode>().ToArray() : Array.Empty<XNode>();
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
            var name = element.Attribute(DocumentationCommentXmlNames.NameAttributeName)?.Value;
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        private static bool ElementNameIs(XElement element, string name)
            => string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);
    }
}
