// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Progression.CodeSchema;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

/// <summary>
/// A helper class that implements the creation of GraphNodeIds that matches the .dgml creation
/// by the metadata progression provider.
/// </summary>
internal static class GraphNodeIdCreation
{
    public static GraphNodeId GetIdForDocument(Document document)
    {
        return
            GraphNodeId.GetNested(
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(document.Project.FilePath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File, new Uri(document.FilePath, UriKind.RelativeOrAbsolute)));
    }

    internal static async Task<GraphNodeId> GetIdForNamespaceAsync(INamespaceSymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var builder = new CodeQualifiedIdentifierBuilder();

        var assembly = await GetAssemblyFullPathAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        if (assembly != null)
        {
            builder.Assembly = assembly;
        }

        builder.Namespace = symbol.ToDisplayString();

        return builder.ToQualifiedIdentifier();
    }

    internal static async Task<GraphNodeId> GetIdForTypeAsync(ITypeSymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var nodes = await GetPartialsForNamespaceAndTypeAsync(symbol, true, solution, cancellationToken).ConfigureAwait(false);
        var partials = nodes.ToArray();

        if (partials.Length == 1)
        {
            return partials[0];
        }
        else
        {
            return GraphNodeId.GetNested(partials);
        }
    }

    private static async Task<IEnumerable<GraphNodeId>> GetPartialsForNamespaceAndTypeAsync(ITypeSymbol symbol, bool includeNamespace, Solution solution, CancellationToken cancellationToken, bool isInGenericArguments = false)
    {
        var items = new List<GraphNodeId>();

        Uri assembly = null;
        if (includeNamespace)
        {
            assembly = await GetAssemblyFullPathAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        }

        var underlyingType = ChaseToUnderlyingType(symbol);

        if (symbol.TypeKind == TypeKind.TypeParameter)
        {
            var typeParameter = (ITypeParameterSymbol)symbol;

            if (typeParameter.TypeParameterKind == TypeParameterKind.Type)
            {
                if (includeNamespace && !typeParameter.ContainingNamespace.IsGlobalNamespace)
                {
                    if (assembly != null)
                    {
                        items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, assembly));
                    }

                    items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, typeParameter.ContainingNamespace.ToDisplayString()));
                }

                items.Add(await GetPartialForTypeAsync(symbol.ContainingType, CodeGraphNodeIdName.Type, solution, cancellationToken).ConfigureAwait(false));
            }

            items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Parameter, ((ITypeParameterSymbol)symbol).Ordinal.ToString()));
        }
        else
        {
            if (assembly != null)
            {
                items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, assembly));
            }

            if (underlyingType.TypeKind == TypeKind.Dynamic)
            {
                items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, "System"));
            }
            else if (underlyingType.ContainingNamespace != null && !underlyingType.ContainingNamespace.IsGlobalNamespace)
            {
                items.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, underlyingType.ContainingNamespace.ToDisplayString()));
            }

            items.Add(await GetPartialForTypeAsync(symbol, CodeGraphNodeIdName.Type, solution, cancellationToken, isInGenericArguments).ConfigureAwait(false));
        }

        return items;
    }

    private static async Task<GraphNodeId> GetPartialForTypeAsync(ITypeSymbol symbol, GraphNodeIdName nodeName, Solution solution, CancellationToken cancellationToken, bool isInGenericArguments = false)
    {
        if (symbol is IArrayTypeSymbol arrayType)
        {
            return await GetPartialForArrayTypeAsync(arrayType, nodeName, solution, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is INamedTypeSymbol namedType)
        {
            return await GetPartialForNamedTypeAsync(namedType, nodeName, solution, cancellationToken, isInGenericArguments).ConfigureAwait(false);
        }
        else if (symbol is IPointerTypeSymbol pointerType)
        {
            return await GetPartialForPointerTypeAsync(pointerType, nodeName, solution, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is ITypeParameterSymbol typeParameter)
        {
            return await GetPartialForTypeParameterSymbolAsync(typeParameter, nodeName, solution, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is IDynamicTypeSymbol)
        {
            return GetPartialForDynamicType(nodeName);
        }

        throw ExceptionUtilities.Unreachable();
    }

    private static GraphNodeId GetPartialForDynamicType(GraphNodeIdName nodeName)
    {
        // We always consider this to be the "Object" type since Progression takes a very metadata-ish view of the type
        return GraphNodeId.GetPartial(nodeName, "Object");
    }

    private static async Task<GraphNodeId> GetPartialForNamedTypeAsync(INamedTypeSymbol namedType, GraphNodeIdName nodeName, Solution solution, CancellationToken cancellationToken, bool isInGenericArguments = false)
    {
        // If this is a simple type, then we don't have much to do
        if (namedType.ContainingType == null && Equals(namedType.ConstructedFrom, namedType) && namedType.Arity == 0)
        {
            return GraphNodeId.GetPartial(nodeName, namedType.Name);
        }
        else
        {
            // For a generic type, we need to populate "type" property with the following form:
            //
            //      Type = (Name =...GenericParameterCount = GenericArguments =...ParentType =...)
            //
            //  where "Name" contains a symbol name
            //    and "GenericParameterCount" contains the number of type parameters,
            //    and "GenericArguments" contains its type parameters' node information. 
            //    and "ParentType" contains its containing type's node information.

            var partials = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeQualifiedName.Name, namedType.Name)
            };

            if (namedType.Arity > 0)
            {
                partials.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.GenericParameterCountIdentifier, namedType.Arity.ToString()));
            }

            // For the property "GenericArguments", we only populate them 
            // when type parameters are constructed using instance types (i.e., namedType.ConstructedFrom != namedType).
            // However, there is a case where we need to populate "GenericArguments" even though arguments are not marked as "constructed"
            // because a symbol is not marked as "constructed" when a type is constructed using its own type parameters.
            // To distinguish this case, we use "isInGenericArguments" flag which we pass either to populate arguments recursively or to populate "ParentType".

            var hasGenericArguments = (!Equals(namedType.ConstructedFrom, namedType) || isInGenericArguments) && namedType.TypeArguments != null && namedType.TypeArguments.Any();

            if (hasGenericArguments)
            {
                var genericArguments = new List<GraphNodeId>();
                foreach (var arg in namedType.TypeArguments)
                {
                    var nodes = await GetPartialsForNamespaceAndTypeAsync(arg, includeNamespace: true, solution: solution, cancellationToken: cancellationToken, isInGenericArguments: true).ConfigureAwait(false);
                    genericArguments.Add(GraphNodeId.GetNested(nodes.ToArray()));
                }

                partials.Add(GraphNodeId.GetArray(
                    CodeGraphNodeIdName.GenericArgumentsIdentifier,
                    genericArguments.ToArray()));
            }

            if (namedType.ContainingType != null)
            {
                partials.Add(await GetPartialForTypeAsync(namedType.ContainingType, CodeGraphNodeIdName.ParentType, solution, cancellationToken, hasGenericArguments).ConfigureAwait(false));
            }

            return GraphNodeId.GetPartial(nodeName, MakeCollectionIfNecessary(partials.ToArray()));
        }
    }

    private static async Task<GraphNodeId> GetPartialForPointerTypeAsync(IPointerTypeSymbol pointerType, GraphNodeIdName nodeName, Solution solution, CancellationToken cancellationToken)
    {
        var indirection = 1;

        while (pointerType.PointedAtType.TypeKind == TypeKind.Pointer)
        {
            indirection++;
            pointerType = (IPointerTypeSymbol)pointerType.PointedAtType;
        }

        var partials = new List<GraphNodeId>
        {
            GraphNodeId.GetPartial(CodeQualifiedName.Name, pointerType.PointedAtType.Name),
            GraphNodeId.GetPartial(CodeQualifiedName.Indirection, indirection.ToString())
        };

        if (pointerType.PointedAtType.ContainingType != null)
        {
            partials.Add(await GetPartialForTypeAsync(pointerType.PointedAtType.ContainingType, CodeGraphNodeIdName.ParentType, solution, cancellationToken).ConfigureAwait(false));
        }

        return GraphNodeId.GetPartial(nodeName, MakeCollectionIfNecessary(partials.ToArray()));
    }

    private static async Task<GraphNodeId> GetPartialForArrayTypeAsync(IArrayTypeSymbol arrayType, GraphNodeIdName nodeName, Solution solution, CancellationToken cancellationToken)
    {
        var partials = new List<GraphNodeId>();

        var underlyingType = ChaseToUnderlyingType(arrayType);

        if (underlyingType.TypeKind == TypeKind.Dynamic)
        {
            partials.Add(GraphNodeId.GetPartial(CodeQualifiedName.Name, "Object"));
        }
        else if (underlyingType.TypeKind != TypeKind.TypeParameter)
        {
            partials.Add(GraphNodeId.GetPartial(CodeQualifiedName.Name, underlyingType.Name));
        }

        partials.Add(GraphNodeId.GetPartial(CodeQualifiedName.ArrayRank, arrayType.Rank.ToString()));
        partials.Add(await GetPartialForTypeAsync(arrayType.ElementType, CodeGraphNodeIdName.ParentType, solution, cancellationToken).ConfigureAwait(false));

        return GraphNodeId.GetPartial(nodeName, MakeCollectionIfNecessary(partials.ToArray()));
    }

    private static async Task<GraphNodeId> GetPartialForTypeParameterSymbolAsync(ITypeParameterSymbol typeParameterSymbol, GraphNodeIdName nodeName, Solution solution, CancellationToken cancellationToken)
    {
        if (typeParameterSymbol.TypeParameterKind == TypeParameterKind.Method)
        {
            return GraphNodeId.GetPartial(nodeName,
                new GraphNodeIdCollection(false,
                    GraphNodeId.GetPartial(CodeGraphNodeIdName.Parameter, typeParameterSymbol.Ordinal.ToString())));
        }
        else
        {
            var nodes = await GetPartialsForNamespaceAndTypeAsync(typeParameterSymbol, false, solution, cancellationToken).ConfigureAwait(false);
            return GraphNodeId.GetPartial(nodeName,
                new GraphNodeIdCollection(false, nodes.ToArray()));
        }
    }

    private static ITypeSymbol ChaseToUnderlyingType(ITypeSymbol symbol)
    {
        while (symbol.TypeKind == TypeKind.Array)
        {
            symbol = ((IArrayTypeSymbol)symbol).ElementType;
        }

        while (symbol.TypeKind == TypeKind.Pointer)
        {
            symbol = ((IPointerTypeSymbol)symbol).PointedAtType;
        }

        return symbol;
    }

    public static async Task<GraphNodeId> GetIdForMemberAsync(ISymbol member, Solution solution, CancellationToken cancellationToken)
    {
        var partials = new List<GraphNodeId>();

        partials.AddRange(await GetPartialsForNamespaceAndTypeAsync(member.ContainingType, true, solution, cancellationToken).ConfigureAwait(false));

        var parameters = member.GetParameters();
        if (parameters.Any() || member.GetArity() > 0)
        {
            var memberPartials = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeQualifiedName.Name, member.MetadataName)
            };

            if (member.GetArity() > 0)
            {
                memberPartials.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.GenericParameterCountIdentifier, member.GetArity().ToString()));
            }

            if (parameters.Any())
            {
                var parameterTypeIds = new List<GraphNodeId>();
                foreach (var p in parameters)
                {
                    var parameterIds = await GetPartialsForNamespaceAndTypeAsync(p.Type, true, solution, cancellationToken).ConfigureAwait(false);
                    var nodes = parameterIds.ToList();
                    if (p.IsRefOrOut())
                    {
                        nodes.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.ParamKind, ParamKind.Ref));
                    }

                    parameterTypeIds.Add(GraphNodeId.GetNested(nodes.ToArray()));
                }

                if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Conversion)
                {
                    // For explicit/implicit conversion operators, we need to include the return type in the method Id,
                    // because there can be several conversion operators with same parameters and only differ by return type.
                    // For example,
                    // 
                    //     public class Class1
                    //     {
                    //         public static explicit (explicit) operator int(Class1 c) { ... }
                    //         public static explicit (explicit) operator double(Class1 c) { ... }
                    //     }

                    var nodes = await GetPartialsForNamespaceAndTypeAsync(methodSymbol.ReturnType, true, solution, cancellationToken).ConfigureAwait(false);
                    var returnTypePartial = nodes.ToList();
                    returnTypePartial.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.ParamKind, Microsoft.VisualStudio.GraphModel.CodeSchema.ParamKind.Return));

                    var returnCollection = GraphNodeId.GetNested(returnTypePartial.ToArray());
                    parameterTypeIds.Add(returnCollection);
                }

                memberPartials.Add(GraphNodeId.GetArray(
                                   CodeGraphNodeIdName.OverloadingParameters,
                                   parameterTypeIds.ToArray()));
            }

            partials.Add(GraphNodeId.GetPartial(
                        CodeGraphNodeIdName.Member,
                        MakeCollectionIfNecessary(memberPartials.ToArray())));
        }
        else
        {
            partials.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Member, member.MetadataName));
        }

        return GraphNodeId.GetNested(partials.ToArray());
    }

    private static object MakeCollectionIfNecessary(GraphNodeId[] array)
    {
        // Place the array of GraphNodeId's into the collection if necessary, so to make them appear in VS Properties Panel
        if (array.Length > 1)
        {
            return new GraphNodeIdCollection(false, array);
        }

        return GraphNodeId.GetNested(array);
    }

    private static IAssemblySymbol GetContainingAssembly(ISymbol symbol)
    {
        if (symbol.ContainingAssembly != null)
        {
            return symbol.ContainingAssembly;
        }

        if (symbol is not ITypeSymbol typeSymbol)
        {
            return null;
        }

        var underlyingType = ChaseToUnderlyingType(typeSymbol);
        if (Equals(typeSymbol, underlyingType))
        {
            // when symbol is for dynamic type
            return null;
        }

        return GetContainingAssembly(underlyingType);
    }

    private static async Task<Uri> GetAssemblyFullPathAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var containingAssembly = GetContainingAssembly(symbol);
        return await GetAssemblyFullPathAsync(containingAssembly, solution, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Uri> GetAssemblyFullPathAsync(IAssemblySymbol containingAssembly, Solution solution, CancellationToken cancellationToken)
    {
        if (containingAssembly == null)
        {
            return null;
        }

        var foundProject = solution.GetProject(containingAssembly, cancellationToken);
        if (foundProject != null)
        {
            if (solution.Workspace is VisualStudioWorkspace)
            {
                // TODO: audit the OutputFilePath and whether this is bin or obj
                if (!string.IsNullOrWhiteSpace(foundProject.OutputFilePath))
                {
                    return new Uri(foundProject.OutputFilePath, UriKind.RelativeOrAbsolute);
                }

                return null;
            }
        }
        else
        {
            // This symbol is not present in the source code, we need to resolve it from the references!
            // If a MetadataReference returned by Compilation.GetMetadataReference(AssemblySymbol) has a path, we could use it.                
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation != null)
                {
                    if (compilation.GetMetadataReference(containingAssembly) is PortableExecutableReference reference && !string.IsNullOrEmpty(reference.FilePath))
                    {
                        return new Uri(reference.FilePath, UriKind.RelativeOrAbsolute);
                    }
                }
            }
        }

        // If we are not in VS, return project.OutputFilePath as a reasonable fallback.
        // For an example, it could be AdhocWorkspace for unit tests.
        if (foundProject != null && !string.IsNullOrEmpty(foundProject.OutputFilePath))
        {
            return new Uri(foundProject.OutputFilePath, UriKind.Absolute);
        }

        return null;
    }

    internal static async Task<GraphNodeId> GetIdForAssemblyAsync(IAssemblySymbol assemblySymbol, Solution solution, CancellationToken cancellationToken)
    {
        var assembly = await GetAssemblyFullPathAsync(assemblySymbol, solution, cancellationToken).ConfigureAwait(false);
        if (assembly != null)
        {
            var builder = new CodeQualifiedIdentifierBuilder();
            builder.Assembly = assembly;
            return builder.ToQualifiedIdentifier();
        }

        return null;
    }

    internal static async Task<GraphNodeId> GetIdForParameterAsync(IParameterSymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        if (symbol.ContainingSymbol == null ||
            (symbol.ContainingSymbol.Kind != SymbolKind.Method && symbol.ContainingSymbol.Kind != SymbolKind.Property))
        {
            // We are only support parameters inside methods or properties.
            throw new ArgumentException("symbol");
        }

        var containingSymbol = symbol.ContainingSymbol;
        if (containingSymbol is IMethodSymbol method && method.AssociatedSymbol != null && method.AssociatedSymbol.Kind == SymbolKind.Property)
        {
            var property = (IPropertySymbol)method.AssociatedSymbol;
            if (property.Parameters.Any(static (p, symbol) => p.Name == symbol.Name, symbol))
            {
                containingSymbol = property;
            }
        }

        var memberId = await GetIdForMemberAsync(containingSymbol, solution, cancellationToken).ConfigureAwait(false);
        if (memberId != null)
        {
            return memberId + GraphNodeId.GetPartial(CodeGraphNodeIdName.Parameter, symbol.Name);
        }

        return null;
    }

    internal static async Task<GraphNodeId> GetIdForLocalVariableAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        if (symbol.ContainingSymbol == null ||
            (symbol.ContainingSymbol.Kind != SymbolKind.Method && symbol.ContainingSymbol.Kind != SymbolKind.Property))
        {
            // We are only support local variables inside methods or properties.
            throw new ArgumentException("symbol");
        }

        var memberId = await GetIdForMemberAsync(symbol.ContainingSymbol, solution, cancellationToken).ConfigureAwait(false);
        if (memberId != null)
        {
            var builder = new CodeQualifiedIdentifierBuilder(memberId);
            builder.LocalVariable = symbol.Name;
            builder.LocalVariableIndex = await GetLocalVariableIndexAsync(symbol, solution, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            return builder.ToQualifiedIdentifier();
        }

        return null;
    }

    /// <summary>
    /// Get the position of where a given local variable is defined considering there could be multiple variables with the same name in method body.
    /// For example, in "int M() { { int goo = 0; ...} { int goo = 1; ...} }",
    /// the return value for the first "goo" would be 0 while the value for the second one would be 1.
    /// It will be used to create a node with LocalVariableIndex for a non-zero value.
    /// In the above example, hence, a node id for the first "goo" would look like (... Member=M LocalVariable=bar)
    /// but an id for the second "goo" would be (... Member=M LocalVariable=bar LocalVariableIndex=1)
    /// </summary>
    private static async Task<int> GetLocalVariableIndexAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        var pos = 0;

        foreach (var reference in symbol.ContainingSymbol.DeclaringSyntaxReferences)
        {
            var currentNode = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            // For VB, we have to ask its parent to get local variables within this method body
            // since DeclaringSyntaxReferences return statement rather than enclosing block.
            if (currentNode != null && symbol.Language == LanguageNames.VisualBasic)
            {
                currentNode = currentNode.Parent;
            }

            if (currentNode != null)
            {
                var document = solution.GetDocument(currentNode.SyntaxTree);
                if (document == null)
                {
                    continue;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                foreach (var node in currentNode.DescendantNodes())
                {
                    var current = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                    if (current is { Kind: SymbolKind.Local or SymbolKind.RangeVariable } && current.Name == symbol.Name)
                    {
                        if (!current.Equals(symbol))
                        {
                            pos++;
                        }
                        else
                        {
                            return pos;
                        }
                    }
                }
            }
        }

        throw ExceptionUtilities.Unreachable();
    }
}
