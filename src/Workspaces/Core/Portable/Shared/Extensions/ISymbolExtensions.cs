// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    /// <summary>
    /// Checks a given symbol for browsability based on its declaration location, attributes 
    /// explicitly limiting browsability, and whether showing of advanced members is enabled. 
    /// The optional editorBrowsableInfo parameters may be used to specify the symbols of the
    /// constructors of the various browsability limiting attributes because finding these 
    /// repeatedly over a large list of symbols can be slow. If these are not provided,
    /// they will be found in the compilation.
    /// </summary>
    public static bool IsEditorBrowsable(
        this ISymbol symbol,
        bool hideAdvancedMembers,
        Compilation compilation,
        EditorBrowsableInfo editorBrowsableInfo = default)
    {
        return IsEditorBrowsableWithState(
            symbol,
            hideAdvancedMembers,
            compilation,
            editorBrowsableInfo).isBrowsable;
    }

    // In addition to given symbol's browsability, also returns its EditorBrowsableState if it contains EditorBrowsableAttribute.
    public static (bool isBrowsable, bool isEditorBrowsableStateAdvanced) IsEditorBrowsableWithState(
        this ISymbol symbol,
        bool hideAdvancedMembers,
        Compilation compilation,
        EditorBrowsableInfo editorBrowsableInfo = default)
    {
        // Namespaces can't have attributes, so just return true here.  This also saves us a 
        // costly check if this namespace has any locations in source (since a merged namespace
        // needs to go collect all the locations).
        if (symbol.Kind == SymbolKind.Namespace)
        {
            return (isBrowsable: true, isEditorBrowsableStateAdvanced: false);
        }

        // check for IsImplicitlyDeclared so we don't spend time examining VB's embedded types.
        // This saves a few percent in typing scenarios.  An implicitly declared symbol can't
        // have attributes, so it can't be hidden by them.
        if (symbol.IsImplicitlyDeclared)
        {
            return (isBrowsable: true, isEditorBrowsableStateAdvanced: false);
        }

        if (editorBrowsableInfo.IsDefault)
        {
            editorBrowsableInfo = new EditorBrowsableInfo(compilation);
        }

        // Ignore browsability limiting attributes if the symbol is declared in source.
        // Check all locations since some of VB's embedded My symbols are declared in 
        // both source and the MyTemplateLocation.
        if (symbol.Locations.All(loc => loc.IsInSource))
        {
            // The HideModuleNameAttribute still applies to Modules defined in source
            return (!IsBrowsingProhibitedByHideModuleNameAttribute(symbol, editorBrowsableInfo.HideModuleNameAttribute), isEditorBrowsableStateAdvanced: false);
        }

        var (isProhibited, isEditorBrowsableStateAdvanced) = IsBrowsingProhibited(symbol, hideAdvancedMembers, editorBrowsableInfo);

        return (!isProhibited, isEditorBrowsableStateAdvanced);
    }

    private static (bool isProhibited, bool isEditorBrowsableStateAdvanced) IsBrowsingProhibited(
        ISymbol symbol,
        bool hideAdvancedMembers,
        EditorBrowsableInfo editorBrowsableInfo)
    {
        var attributes = symbol.GetAttributes();
        var isEditorBrowsableStateAdvanced = false;

        if (attributes.Length != 0)
        {
            (var isProhibited, isEditorBrowsableStateAdvanced) = IsBrowsingProhibitedByEditorBrowsableAttribute(attributes, hideAdvancedMembers, editorBrowsableInfo.EditorBrowsableAttributeConstructor);

            if (isProhibited
                || IsBrowsingProhibitedByTypeLibTypeAttribute(attributes, editorBrowsableInfo.TypeLibTypeAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibFuncAttribute(attributes, editorBrowsableInfo.TypeLibFuncAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibVarAttribute(attributes, editorBrowsableInfo.TypeLibVarAttributeConstructors)
                || IsBrowsingProhibitedByHideModuleNameAttribute(symbol, editorBrowsableInfo.HideModuleNameAttribute, attributes))
            {
                return (isProhibited: true, isEditorBrowsableStateAdvanced);
            }
        }

        if (symbol is IPropertySymbol propertySymbol)
        {
            // TypeLibFuncAttribute is specified on property methods
            var getterSymbol = propertySymbol.GetMethod;
            var setterSymbol = propertySymbol.SetMethod;

            if (getterSymbol != null || setterSymbol != null)
            {
                // indicate isProhibited if neither allows browsing
                if ((getterSymbol == null || IsBrowsingProhibitedByTypeLibFuncAttribute(getterSymbol.GetAttributes(), editorBrowsableInfo.TypeLibFuncAttributeConstructors))
                    && (setterSymbol == null || IsBrowsingProhibitedByTypeLibFuncAttribute(setterSymbol.GetAttributes(), editorBrowsableInfo.TypeLibFuncAttributeConstructors)))
                {
                    return (isProhibited: true, isEditorBrowsableStateAdvanced);
                }
            }
        }

        return (isProhibited: false, isEditorBrowsableStateAdvanced);
    }

    private static bool IsBrowsingProhibitedByHideModuleNameAttribute(
        ISymbol symbol, INamedTypeSymbol? hideModuleNameAttribute, ImmutableArray<AttributeData> attributes = default)
    {
        if (hideModuleNameAttribute == null || !symbol.IsModuleType())
        {
            return false;
        }

        attributes = attributes.IsDefault ? symbol.GetAttributes() : attributes;

        foreach (var attribute in attributes)
        {
            if (Equals(attribute.AttributeClass, hideModuleNameAttribute))
            {
                return true;
            }
        }

        return false;
    }

    private static (bool isProhibited, bool isEditorBrowsableStateAdvanced) IsBrowsingProhibitedByEditorBrowsableAttribute(
        ImmutableArray<AttributeData> attributes, bool hideAdvancedMembers, IMethodSymbol? constructor)
    {
        if (constructor == null)
            return (isProhibited: false, isEditorBrowsableStateAdvanced: false);

        foreach (var attribute in attributes)
        {
            if (Equals(attribute.AttributeConstructor, constructor) &&
                attribute.ConstructorArguments is [{ Value: int value }])
            {
                var state = (EditorBrowsableState)value;
                if (EditorBrowsableState.Never == state)
                    return (isProhibited: true, isEditorBrowsableStateAdvanced: false);

                if (EditorBrowsableState.Advanced == state)
                    return (isProhibited: hideAdvancedMembers, isEditorBrowsableStateAdvanced: true);
            }
        }

        return (isProhibited: false, isEditorBrowsableStateAdvanced: false);
    }

    private static bool IsBrowsingProhibitedByTypeLibTypeAttribute(
        ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
    {
        return IsBrowsingProhibitedByTypeLibAttributeWorker(
            attributes,
            constructors,
            TypeLibTypeFlagsFHidden);
    }

    private static bool IsBrowsingProhibitedByTypeLibFuncAttribute(
        ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
    {
        return IsBrowsingProhibitedByTypeLibAttributeWorker(
            attributes,
            constructors,
            TypeLibFuncFlagsFHidden);
    }

    private static bool IsBrowsingProhibitedByTypeLibVarAttribute(
        ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> constructors)
    {
        return IsBrowsingProhibitedByTypeLibAttributeWorker(
            attributes,
            constructors,
            TypeLibVarFlagsFHidden);
    }

    private const int TypeLibTypeFlagsFHidden = 0x0010;
    private const int TypeLibFuncFlagsFHidden = 0x0040;
    private const int TypeLibVarFlagsFHidden = 0x0040;

    private static bool IsBrowsingProhibitedByTypeLibAttributeWorker(
        ImmutableArray<AttributeData> attributes, ImmutableArray<IMethodSymbol> attributeConstructors, int hiddenFlag)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.ConstructorArguments.Length == 1)
            {
                foreach (var constructor in attributeConstructors)
                {
                    if (Equals(attribute.AttributeConstructor, constructor))
                    {
                        // Check for both constructor signatures. The constructor that takes a TypeLib*Flags reports an int argument.
                        var argumentValue = attribute.ConstructorArguments.First().Value;

                        int actualFlags;
                        if (argumentValue is int i)
                        {
                            actualFlags = i;
                        }
                        else if (argumentValue is short sh)
                        {
                            actualFlags = sh;
                        }
                        else
                        {
                            continue;
                        }

                        if ((actualFlags & hiddenFlag) == hiddenFlag)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public static DocumentationComment GetDocumentationComment(this ISymbol symbol, Compilation compilation, CultureInfo? preferredCulture = null, bool expandIncludes = false, bool expandInheritdoc = false, CancellationToken cancellationToken = default)
        => GetDocumentationComment(symbol, visitedSymbols: null, compilation, preferredCulture, expandIncludes, expandInheritdoc, cancellationToken);

    private static DocumentationComment GetDocumentationComment(ISymbol symbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, CultureInfo? preferredCulture, bool expandIncludes, bool expandInheritdoc, CancellationToken cancellationToken)
    {
        var xmlText = symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        if (expandInheritdoc)
        {
            if (string.IsNullOrEmpty(xmlText))
            {
                if (IsEligibleForAutomaticInheritdoc(symbol))
                {
                    xmlText = $@"<doc><inheritdoc/></doc>";
                }
                else
                {
                    return DocumentationComment.Empty;
                }
            }

            try
            {
                var element = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
                element.ReplaceNodes(RewriteMany(symbol, visitedSymbols, compilation, element.Nodes().ToArray(), cancellationToken));
                xmlText = element.ToString(SaveOptions.DisableFormatting);
            }
            catch (XmlException)
            {
                // Malformed documentation comments will produce an exception during parsing. This is not directly
                // actionable, so avoid the overhead of telemetry reporting for it.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1385578
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        return RoslynString.IsNullOrEmpty(xmlText) ? DocumentationComment.Empty : DocumentationComment.FromXmlFragment(xmlText);

        static bool IsEligibleForAutomaticInheritdoc(ISymbol symbol)
        {
            // Only the following symbols are eligible to inherit documentation without an <inheritdoc/> element:
            //
            // * Members that override an inherited member
            // * Members that implement an interface member
            if (symbol.IsOverride)
            {
                return true;
            }

            if (symbol.ContainingType is null)
            {
                // Observed with certain implicit operators, such as operator==(void*, void*).
                return false;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    if (symbol.ExplicitOrImplicitInterfaceImplementations().Any())
                    {
                        return true;
                    }

                    break;

                default:
                    break;
            }

            return false;
        }
    }

    private static XNode[] RewriteInheritdocElements(ISymbol symbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XNode node, CancellationToken cancellationToken)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            var element = (XElement)node;
            if (ElementNameIs(element, DocumentationCommentXmlNames.InheritdocElementName))
            {
                var rewritten = RewriteInheritdocElement(symbol, visitedSymbols, compilation, element, cancellationToken);
                if (rewritten is object)
                {
                    return rewritten;
                }
            }
        }

        var container = node as XContainer;
        if (container == null)
        {
            return [Copy(node, copyAttributeAnnotations: false)];
        }

        var oldNodes = container.Nodes();

        // Do this after grabbing the nodes, so we don't see copies of them.
        container = Copy(container, copyAttributeAnnotations: false);

        // WARN: don't use node after this point - use container since it's already been copied.

        if (oldNodes != null)
        {
            var rewritten = RewriteMany(symbol, visitedSymbols, compilation, oldNodes.ToArray(), cancellationToken);
            container.ReplaceNodes(rewritten);
        }

        return [container];
    }

    private static XNode[] RewriteMany(ISymbol symbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XNode[] nodes, CancellationToken cancellationToken)
    {
        var result = new List<XNode>();
        foreach (var child in nodes)
        {
            result.AddRange(RewriteInheritdocElements(symbol, visitedSymbols, compilation, child, cancellationToken));
        }

        return [.. result];
    }

    private static XNode[]? RewriteInheritdocElement(ISymbol memberSymbol, HashSet<ISymbol>? visitedSymbols, Compilation compilation, XElement element, CancellationToken cancellationToken)
    {
        var crefAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.CrefAttributeName));
        var pathAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName));

        var candidate = GetCandidateSymbol(memberSymbol);
        var hasCandidateCref = candidate is object;

        var hasCrefAttribute = crefAttribute is object;
        var hasPathAttribute = pathAttribute is object;
        if (!hasCrefAttribute && !hasCandidateCref)
        {
            // No cref available
            return null;
        }

        ISymbol? symbol;
        if (crefAttribute is null)
        {
            Contract.ThrowIfNull(candidate);
            symbol = candidate;
        }
        else
        {
            var crefValue = crefAttribute.Value;
            symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, compilation);
            if (symbol is null)
            {
                return null;
            }
        }

        visitedSymbols ??= [];
        if (!visitedSymbols.Add(symbol))
        {
            // Prevent recursion
            return null;
        }

        try
        {
            var inheritedDocumentation = GetDocumentationComment(symbol, visitedSymbols, compilation, preferredCulture: null, expandIncludes: true, expandInheritdoc: true, cancellationToken);
            if (inheritedDocumentation == DocumentationComment.Empty)
            {
                return [];
            }

            var document = XDocument.Parse(inheritedDocumentation.FullXmlFragment, LoadOptions.PreserveWhitespace);
            string xpathValue;
            if (string.IsNullOrEmpty(pathAttribute?.Value))
            {
                xpathValue = BuildXPathForElement(element.Parent!);
            }
            else
            {
                xpathValue = pathAttribute!.Value;
                if (xpathValue.StartsWith("/"))
                {
                    // Account for the root <doc> or <member> element
                    xpathValue = "/*" + xpathValue;
                }
            }

            // Consider the following code, we want Test<int>.Clone to say "Clones a Test<int>" instead of "Clones a int", thus
            // we rewrite `typeparamref`s as cref pointing to the correct type:
            /*
                public class Test<T> : ICloneable<Test<T>>
                {
                    /// <inheritdoc/>
                    public Test<T> Clone() => new();
                }

                /// <summary>A type that has clonable instances.</summary>
                /// <typeparam name="T">The type of instances that can be cloned.</typeparam>
                public interface ICloneable<T>
                {
                    /// <summary>Clones a <typeparamref name="T"/>.</summary>
                    public T Clone();
                }
            */
            // Note: there is no way to cref an instantiated generic type. See https://github.com/dotnet/csharplang/issues/401
            var typeParameterRefs = document.Descendants(DocumentationCommentXmlNames.TypeParameterReferenceElementName).ToImmutableArray();
            foreach (var typeParameterRef in typeParameterRefs)
            {
                if (typeParameterRef.Attribute(DocumentationCommentXmlNames.NameAttributeName) is XAttribute typeParamName)
                {
                    var index = symbol.OriginalDefinition.GetAllTypeParameters().IndexOf(p => p.Name == typeParamName.Value);
                    if (index >= 0)
                    {
                        var typeArgs = symbol.GetAllTypeArguments();
                        if (index < typeArgs.Length)
                        {
                            var docId = typeArgs[index].GetDocumentationCommentId();
                            if (docId != null && !docId.StartsWith("!"))
                            {
                                var replacement = new XElement(DocumentationCommentXmlNames.SeeElementName);
                                replacement.SetAttributeValue(DocumentationCommentXmlNames.CrefAttributeName, docId);
                                typeParameterRef.ReplaceWith(replacement);
                            }
                        }
                    }
                }
            }

            var loadedElements = TrySelectNodes(document, xpathValue);
            return loadedElements ?? [];
        }
        catch (XmlException)
        {
            return [];
        }
        finally
        {
            visitedSymbols.Remove(symbol);
        }

        // Local functions
        static ISymbol? GetCandidateSymbol(ISymbol memberSymbol)
        {
            if (memberSymbol.ExplicitInterfaceImplementations().Any())
            {
                return memberSymbol.ExplicitInterfaceImplementations().First();
            }
            else if (memberSymbol.IsOverride)
            {
                return memberSymbol.GetOverriddenMember();
            }

            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                {
                    var baseType = memberSymbol.ContainingType.BaseType;
#nullable disable // Can 'baseType' be null here? https://github.com/dotnet/roslyn/issues/39166
                    return baseType.Constructors.Where(c => IsSameSignature(methodSymbol, c)).FirstOrDefault();
#nullable enable
                }
                else
                {
                    // check for implicit interface
                    return methodSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
                }
            }
            else if (memberSymbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.TypeKind == TypeKind.Class)
                {
                    // Classes use the base type as the default inheritance candidate. A different target (e.g. an
                    // interface) can be provided via the 'path' attribute.
                    return typeSymbol.BaseType;
                }
                else if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    return typeSymbol.Interfaces.FirstOrDefault();
                }
                else
                {
                    // This includes structs, enums, and delegates as mentioned in the inheritdoc spec
                    return null;
                }
            }

            return memberSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
        }

        static bool IsSameSignature(IMethodSymbol left, IMethodSymbol right)
        {
            if (left.Parameters.Length != right.Parameters.Length)
            {
                return false;
            }

            if (left.IsStatic != right.IsStatic)
            {
                return false;
            }

            if (!left.ReturnType.Equals(right.ReturnType))
            {
                return false;
            }

            for (var i = 0; i < left.Parameters.Length; i++)
            {
                if (!left.Parameters[i].Type.Equals(right.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        static string BuildXPathForElement(XElement element)
        {
            if (ElementNameIs(element, "member") || ElementNameIs(element, "doc"))
            {
                // Avoid string concatenation allocations for inheritdoc as a top-level element
                return "/*/node()[not(self::overloads)]";
            }

            var path = "/node()[not(self::overloads)]";
            for (var current = element; current != null; current = current.Parent)
            {
                var currentName = current.Name.ToString();
                if (ElementNameIs(current, "member") || ElementNameIs(current, "doc"))
                {
                    // Allow <member> and <doc> to be used interchangeably
                    currentName = "*";
                }

                path = "/" + currentName + path;
            }

            return path;
        }
    }

    private static TNode Copy<TNode>(TNode node, bool copyAttributeAnnotations)
        where TNode : XNode
    {
        XNode copy;

        // Documents can't be added to containers, so our usual copy trick won't work.
        if (node.NodeType == XmlNodeType.Document)
        {
            copy = new XDocument(((XDocument)(object)node));
        }
        else
        {
            XContainer temp = new XElement("temp");
            temp.Add(node);
            copy = temp.LastNode!;
            temp.RemoveNodes();
        }

        Debug.Assert(copy != node);
        Debug.Assert(copy.Parent == null); // Otherwise, when we give it one, it will be copied.

        // Copy annotations, the above doesn't preserve them.
        // We need to preserve Location annotations as well as line position annotations.
        CopyAnnotations(node, copy);

        // We also need to preserve line position annotations for all attributes
        // since we report errors with attribute locations.
        if (copyAttributeAnnotations && node.NodeType == XmlNodeType.Element)
        {
            var sourceElement = (XElement)(object)node;
            var targetElement = (XElement)copy;

            var sourceAttributes = sourceElement.Attributes().GetEnumerator();
            var targetAttributes = targetElement.Attributes().GetEnumerator();
            while (sourceAttributes.MoveNext() && targetAttributes.MoveNext())
            {
                Debug.Assert(sourceAttributes.Current.Name == targetAttributes.Current.Name);
                CopyAnnotations(sourceAttributes.Current, targetAttributes.Current);
            }
        }

        return (TNode)copy;
    }

    private static void CopyAnnotations(XObject source, XObject target)
    {
        foreach (var annotation in source.Annotations<object>())
        {
            target.AddAnnotation(annotation);
        }
    }

    private static XNode[]? TrySelectNodes(XNode node, string xpath)
    {
        try
        {
            var xpathResult = (IEnumerable)System.Xml.XPath.Extensions.XPathEvaluate(node, xpath);

            // Throws InvalidOperationException if the result of the XPath is an XDocument:
            return xpathResult?.Cast<XNode>().ToArray();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (XPathException)
        {
            return null;
        }
    }

    private static bool ElementNameIs(XElement element, string name)
        => string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);

    /// <summary>
    /// First, remove symbols from the set if they are overridden by other symbols in the set.
    /// If a symbol is overridden only by symbols outside of the set, then it is not removed. 
    /// This is useful for filtering out symbols that cannot be accessed in a given context due
    /// to the existence of overriding members. Second, remove remaining symbols that are
    /// unsupported (e.g. pointer types in VB) or not editor browsable based on the EditorBrowsable
    /// attribute.
    /// </summary>
    public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbols<T>(
        this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
    {
        symbols = symbols.RemoveOverriddenSymbolsWithinSet();

        // Since all symbols are from the same compilation, find the required attribute
        // constructors once and reuse.
        var editorBrowsableInfo = new EditorBrowsableInfo(compilation);

        // PERF: HasUnsupportedMetadata may require recreating the syntax tree to get the base class, so first
        // check to see if we're referencing a symbol defined in source.
        return symbols.WhereAsArray((s, arg) =>
            // Check if symbol is namespace (which is always visible) first to avoid realizing all locations
            // of each namespace symbol, which might end up allocating in LOH
            (s.IsNamespace() || s.Locations.Any(static loc => loc.IsInSource) || !s.HasUnsupportedMetadata) &&
            !s.IsDestructor() &&
            s.IsEditorBrowsable(
                arg.hideAdvancedMembers,
                arg.editorBrowsableInfo.Compilation,
                arg.editorBrowsableInfo),
            (hideAdvancedMembers, editorBrowsableInfo));
    }

    private static ImmutableArray<T> RemoveOverriddenSymbolsWithinSet<T>(this ImmutableArray<T> symbols) where T : ISymbol
    {
        var overriddenSymbols = new MetadataUnifyingSymbolHashSet();

        foreach (var symbol in symbols)
        {
            var overriddenMember = symbol.GetOverriddenMember();
            if (overriddenMember != null && !overriddenSymbols.Contains(overriddenMember))
                overriddenSymbols.Add(overriddenMember);
        }

        return symbols.WhereAsArray(s => !overriddenSymbols.Contains(s));
    }

    public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols<T>(
        this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
    {
        return symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, compilation)
            .WhereAsArray(s => !s.RequiresUnsafeModifier());
    }
}
