// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Progression.CodeSchema;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal static class GraphBuilder
{
    private static string GetIconName(string groupName, string itemName)
        => string.Format("Microsoft.VisualStudio.{0}.{1}", groupName, itemName);

    private static string GetIconName(string groupName, Accessibility symbolAccessibility)
    {
        switch (symbolAccessibility)
        {
            case Accessibility.Private:
                return GetIconName(groupName, "Private");

            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.ProtectedOrInternal:
                return GetIconName(groupName, "Protected");

            case Accessibility.Internal:
                return GetIconName(groupName, "Internal");

            case Accessibility.Public:
            case Accessibility.NotApplicable:
                return GetIconName(groupName, "Public");

            default:
                throw new ArgumentException();
        }
    }

    internal static async Task<GraphNode> GetOrCreateNodeAsync(Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        GraphNode node;

        switch (symbol.Kind)
        {
            case SymbolKind.Assembly:
                node = await GetOrCreateNodeAssemblyAsync(graph, (IAssemblySymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Namespace:
                node = await GetOrCreateNodeForNamespaceAsync(graph, (INamespaceSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.NamedType:
            case SymbolKind.ErrorType:
                node = await GetOrCreateNodeForNamedTypeAsync(graph, (INamedTypeSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Method:
                node = await GetOrCreateNodeForMethodAsync(graph, (IMethodSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Field:
                node = await GetOrCreateNodeForFieldAsync(graph, (IFieldSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Property:
                node = await GetOrCreateNodeForPropertyAsync(graph, (IPropertySymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Event:
                node = await GetOrCreateNodeForEventAsync(graph, (IEventSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Parameter:
                node = await GetOrCreateNodeForParameterAsync(graph, (IParameterSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            case SymbolKind.Local:
            case SymbolKind.RangeVariable:
                node = await GetOrCreateNodeForLocalVariableAsync(graph, symbol, solution, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new ArgumentException("symbol");
        }

        UpdatePropertiesForNode(symbol, node);
        UpdateLabelsForNode(symbol, solution, node);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForParameterAsync(Graph graph, IParameterSymbol parameterSymbol, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForParameterAsync(parameterSymbol, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);
        node.AddCategory(CodeNodeCategories.Parameter);

        node.SetValue<bool>(Properties.IsByReference, parameterSymbol.RefKind == RefKind.Ref);
        node.SetValue<bool>(Properties.IsOut, parameterSymbol.RefKind == RefKind.Out);
        node.SetValue<bool>(Properties.IsParameterArray, parameterSymbol.IsParams);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForLocalVariableAsync(Graph graph, ISymbol localSymbol, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForLocalVariableAsync(localSymbol, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);
        node.AddCategory(NodeCategories.LocalExpression);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeAssemblyAsync(Graph graph, IAssemblySymbol assemblySymbol, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForAssemblyAsync(assemblySymbol, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);
        node.AddCategory(CodeNodeCategories.Assembly);

        return node;
    }

    private static void UpdateLabelsForNode(ISymbol symbol, Solution solution, GraphNode node)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.NamedType:
                var typeSymbol = (INamedTypeSymbol)symbol;
                if (typeSymbol.IsGenericType)
                {
                    // Some consumers like CodeMap want to show types in an unified way for both C# and VB.
                    // Therefore, populate a common label property using only name and its type parameters.
                    // For example, VB's "Goo(Of T)" or C#'s "Goo<T>(): T" will be shown as "Goo<T>".
                    // This property will be used for drag-and-drop case.
                    var commonLabel = new System.Text.StringBuilder();
                    commonLabel.Append(typeSymbol.Name);
                    commonLabel.Append('<');
                    commonLabel.Append(string.Join(", ", typeSymbol.TypeParameters.Select(t => t.Name)));
                    commonLabel.Append('>');
                    node[Microsoft.VisualStudio.ArchitectureTools.ProgressiveReveal.ProgressiveRevealSchema.CommonLabel] = commonLabel.ToString();

                    return;
                }
                else
                {
                    node.Label = symbol.Name;
                }

                break;

            case SymbolKind.Method:
                var methodSymbol = (IMethodSymbol)symbol;
                if (methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetConstructorLabel(methodSymbol.ContainingSymbol.Name);
                }
                else if (methodSymbol.MethodKind == MethodKind.StaticConstructor)
                {
                    node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetStaticConstructorLabel(methodSymbol.ContainingSymbol.Name);
                }
                else if (methodSymbol.MethodKind == MethodKind.Destructor)
                {
                    node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetFinalizerLabel(methodSymbol.ContainingSymbol.Name);
                }
                else
                {
                    node.Label = methodSymbol.Name;
                }

                break;

            case SymbolKind.Property:
                node.Label = symbol.MetadataName;

                var propertySymbol = (IPropertySymbol)symbol;
                if (propertySymbol.IsIndexer && LanguageNames.CSharp == propertySymbol.Language)
                {
                    // For C# indexer, we will strip off the "[]"
                    node.Label = symbol.Name.Replace("[]", string.Empty);
                }

                break;

            case SymbolKind.Namespace:
                // Use a name with its parents (e.g., A.B.C)
                node.Label = symbol.ToDisplayString();
                break;

            default:
                node.Label = symbol.Name;
                break;
        }

        // When a node is dragged and dropped from SE to CodeMap, its label could be reset during copying to clipboard.
        // So, we try to keep its label that we computed above in a common label property, which CodeMap can access later.
        node[Microsoft.VisualStudio.ArchitectureTools.ProgressiveReveal.ProgressiveRevealSchema.CommonLabel] = node.Label;
    }

    private static void UpdatePropertiesForNode(ISymbol symbol, GraphNode node)
    {
        // Set accessibility properties
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.Public:
                node[Properties.IsPublic] = true;
                break;

            case Accessibility.Internal:
                node[Properties.IsInternal] = true;
                break;

            case Accessibility.Protected:
                node[Properties.IsProtected] = true;
                break;

            case Accessibility.Private:
                node[Properties.IsPrivate] = true;
                break;

            case Accessibility.ProtectedOrInternal:
                node[Properties.IsProtectedOrInternal] = true;
                break;

            case Accessibility.ProtectedAndInternal:
                node[Properties.IsProtected] = true;
                node[Properties.IsInternal] = true;
                break;

            case Accessibility.NotApplicable:
                break;
        }

        // Set common properties
        if (symbol.IsAbstract)
        {
            node[Properties.IsAbstract] = true;
        }

        if (symbol.IsSealed)
        {
            // For VB module, do not set IsFinal since it's not inheritable.
            if (!symbol.IsModuleType())
            {
                node[Properties.IsFinal] = true;
            }
        }

        if (symbol.IsStatic)
        {
            node[Properties.IsStatic] = true;
        }

        if (symbol.IsVirtual)
        {
            node[Properties.IsVirtual] = true;
        }

        if (symbol.IsOverride)
        {
            // The property name is a misnomer, but this is what the previous providers do.
            node[Microsoft.VisualStudio.Progression.DgmlProperties.IsOverloaded] = true;
        }

        // Set type-specific properties
        if (symbol is ITypeSymbol typeSymbol && typeSymbol.IsAnonymousType)
        {
            node[Properties.IsAnonymous] = true;
        }
        else if (symbol is IMethodSymbol methodSymbol)
        {
            UpdateMethodPropertiesForNode(methodSymbol, node);
        }
    }

    private static void UpdateMethodPropertiesForNode(IMethodSymbol symbol, GraphNode node)
    {
        if (symbol.HidesBaseMethodsByName)
        {
            node[Properties.IsHideBySignature] = true;
        }

        if (symbol.IsExtensionMethod)
        {
            node[Properties.IsExtension] = true;
        }

        switch (symbol.MethodKind)
        {
            case MethodKind.AnonymousFunction:
                node[Properties.IsAnonymous] = true;
                break;

            case MethodKind.BuiltinOperator:
            case MethodKind.UserDefinedOperator:
                node[Properties.IsOperator] = true;
                break;

            case MethodKind.Constructor:
            case MethodKind.StaticConstructor:
                node[Properties.IsConstructor] = true;
                break;

            case MethodKind.Conversion:
                // Operator implicit/explicit
                node[Properties.IsOperator] = true;
                break;

            case MethodKind.Destructor:
                node[Properties.IsFinalizer] = true;
                break;

            case MethodKind.PropertyGet:
                node[Properties.IsPropertyGet] = true;
                break;

            case MethodKind.PropertySet:
                node[Properties.IsPropertySet] = true;
                break;
        }
    }

    private static async Task<GraphNode> GetOrCreateNodeForNamespaceAsync(Graph graph, INamespaceSymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForNamespaceAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);
        node.AddCategory(CodeNodeCategories.Namespace);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForNamedTypeAsync(Graph graph, INamedTypeSymbol namedType, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForTypeAsync(namedType, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);
        string iconGroupName;

        switch (namedType.TypeKind)
        {
            case TypeKind.Class:
                node.AddCategory(CodeNodeCategories.Class);
                iconGroupName = "Class";
                break;

            case TypeKind.Delegate:
                node.AddCategory(CodeNodeCategories.Delegate);
                iconGroupName = "Delegate";
                break;

            case TypeKind.Enum:
                node.AddCategory(CodeNodeCategories.Enum);
                iconGroupName = "Enum";
                break;

            case TypeKind.Interface:
                node.AddCategory(CodeNodeCategories.Interface);
                iconGroupName = "Interface";
                break;

            case TypeKind.Module:
                node.AddCategory(CodeNodeCategories.Module);
                iconGroupName = "Module";
                break;

            case TypeKind.Struct:
                node.AddCategory(CodeNodeCategories.Struct);
                iconGroupName = "Struct";
                break;

            case TypeKind.Error:
                node.AddCategory(CodeNodeCategories.Type);
                iconGroupName = "Error";
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(namedType.TypeKind);
        }

        node[DgmlNodeProperties.Icon] = GetIconName(iconGroupName, namedType.DeclaredAccessibility);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForMethodAsync(Graph graph, IMethodSymbol method, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForMemberAsync(method, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);

        node.AddCategory(CodeNodeCategories.Method);

        var isOperator = method.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion;
        node[DgmlNodeProperties.Icon] = isOperator
            ? GetIconName("Operator", method.DeclaredAccessibility)
            : GetIconName("Method", method.DeclaredAccessibility);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForFieldAsync(Graph graph, IFieldSymbol field, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForMemberAsync(field, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);

        node.AddCategory(CodeNodeCategories.Field);

        if (field.ContainingType.TypeKind == TypeKind.Enum)
        {
            node[DgmlNodeProperties.Icon] = GetIconName("EnumMember", field.DeclaredAccessibility);
        }
        else
        {
            node[DgmlNodeProperties.Icon] = GetIconName("Field", field.DeclaredAccessibility);
        }

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForPropertyAsync(Graph graph, IPropertySymbol property, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForMemberAsync(property, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);

        node.AddCategory(CodeNodeCategories.Property);

        node[DgmlNodeProperties.Icon] = GetIconName("Property", property.DeclaredAccessibility);

        return node;
    }

    private static async Task<GraphNode> GetOrCreateNodeForEventAsync(Graph graph, IEventSymbol eventSymbol, Solution solution, CancellationToken cancellationToken)
    {
        var id = await GraphNodeIdCreation.GetIdForMemberAsync(eventSymbol, solution, cancellationToken).ConfigureAwait(false);
        var node = graph.Nodes.GetOrCreate(id);

        node.AddCategory(CodeNodeCategories.Event);

        node[DgmlNodeProperties.Icon] = GetIconName("Event", eventSymbol.DeclaredAccessibility);

        return node;
    }
}
