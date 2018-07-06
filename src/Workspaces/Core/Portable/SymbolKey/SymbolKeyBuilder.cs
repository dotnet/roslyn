// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal abstract partial class SymbolKeyBuilder
    {
        private static readonly ObjectPool<CompressedFormatBuilder> s_compressedFormatBuilderPool = new ObjectPool<CompressedFormatBuilder>(() => new CompressedFormatBuilder());
        private static readonly ObjectPool<JsonFormatBuilder> s_jsonFormatBuilderPool = new ObjectPool<JsonFormatBuilder>(() => new JsonFormatBuilder());

        private readonly StringBuilder _builder;
        private readonly Visitor _visitor;
        private readonly Dictionary<ISymbol, int> _symbolRefIdMap;

        private readonly Action<Location> _writeLocation;
        private readonly Action<ISymbol> _writeSymbol;

        private CancellationToken _cancellationToken;
        private int _nextSymbolRefId;

        protected SymbolKeyBuilder()
        {
            _builder = new StringBuilder();
            _visitor = new Visitor(this);
            _symbolRefIdMap = new Dictionary<ISymbol, int>();

            _writeLocation = AppendLocation;
            _writeSymbol = AppendSymbol;
        }

        protected virtual void Reset()
        {
            _builder.Clear();
            _symbolRefIdMap.Clear();
            _cancellationToken = default;
            _nextSymbolRefId = 0;
        }

        public static string Create(ISymbol symbol, CancellationToken cancellationToken = default)
        {
            var writer = s_compressedFormatBuilderPool.Allocate();
            writer._cancellationToken = cancellationToken;
            writer.AppendSymbol(symbol);

            var result = writer._builder.ToString();
            writer.Reset();

            // Place back in the pool for future use.
            s_compressedFormatBuilderPool.Free(writer);

            return result;
        }

        protected abstract void AppendSymbolReference(int symbolRefId);
        protected abstract void AppendSymbolStart(ISymbol symbol, int symbolRefId);
        protected abstract void AppendSymbolEnd(ISymbol symbol, int symbolRefId);

        protected abstract void AppendArray<T1, T2>(in ImmutableArray<T1> array, Action<T2> writeValue) where T1 : T2;

        protected abstract void AppendAliasData(string name, INamespaceOrTypeSymbol target, string filePath);
        protected abstract void AppendAnonymousDelegateTypeOrFunctionData(bool isAnonymousDelegateType, Location location);
        protected abstract void AppendAnonymousTypeData(ImmutableArray<ITypeSymbol> propertyTypes, ImmutableArray<string> propertyNames, ImmutableArray<bool> propertyIsReadOnly, ImmutableArray<Location> propertyLocations);
        protected abstract void AppendArrayTypeData(ITypeSymbol elementType, int rank);
        protected abstract void AppendAssemblyData(string assemblyName);
        protected abstract void AppendBodyLevelData(string localName, ISymbol containingSymbol, int ordinal, SymbolKind kind);
        protected abstract void AppendConstructedMethodData(IMethodSymbol constructedFrom, ImmutableArray<ITypeSymbol> typeArguments);
        protected abstract void AppendDynamicTypeData();
        protected abstract void AppendErrorTupleTypeData(ImmutableArray<ISymbol> elementTypes, ImmutableArray<string> friendlyNames, ImmutableArray<Location> locations);
        protected abstract void AppendErrorTypeData(string name, INamespaceOrTypeSymbol containingSymbol, int arity, ImmutableArray<ITypeSymbol> typeArguments);
        protected abstract void AppendEventData(string metadataName, INamedTypeSymbol containingType);
        protected abstract void AppendFieldData(string metadataName, INamedTypeSymbol containingType);
        protected abstract void AppendLocationData(Location location);
        protected abstract void AppendMethodData(string metadataName, ISymbol containingSymbol, int arity, bool isPartialMethodImplementationPart, ImmutableArray<IParameterSymbol> parameters, ITypeSymbol returnType);
        protected abstract void AppendModuleData(ISymbol containingSymbol);
        protected abstract void AppendNamedTypeData(string metadataName, ISymbol containingSymbol, int arity, TypeKind typeKind, bool isUnboundGenericType, ImmutableArray<ITypeSymbol> typeArguments);
        protected abstract void AppendNamespaceData(string metadataName, bool isCompilationGlobalNamespace, ISymbol containingSymbol);
        protected abstract void AppendParameterData(string metadataName, ISymbol containingSymbol);
        protected abstract void AppendPointerTypeData(ITypeSymbol pointedAtType);
        protected abstract void AppendPropertyData(string metadataName, ISymbol containingSymbol, bool isIndexer, ImmutableArray<IParameterSymbol> parameters);
        protected abstract void AppendReducedExtensionMethodData(IMethodSymbol reducedFrom, ITypeSymbol receiverType);
        protected abstract void AppendTupleTypeData(INamedTypeSymbol tupleUnderlyingType, ImmutableArray<string> friendlyNames, ImmutableArray<Location> locations);
        protected abstract void AppendTypeParameterData(string metadataName, ISymbol containingSymbol);
        protected abstract void AppendTypeParameterOrdinalData(int declaringSymbolRefId, int ordinal);

        protected virtual void AppendSymbol(ISymbol symbol)
        {
            if (symbol == null)
            {
                AppendNull();
                return;
            }

            // If we've already seen this symbol, just write a reference to it.
            if (_symbolRefIdMap.TryGetValue(symbol, out var symbolRefId))
            {
                AppendSymbolReference(symbolRefId);
                return;
            }

            symbolRefId = _nextSymbolRefId++;

            // Capture the reference ID for this symbol so that any recursive references
            // to the symbol can use it.
            _symbolRefIdMap.Add(symbol, symbolRefId);

            AppendSymbolStart(symbol, symbolRefId);

            _visitor.Visit(symbol);

            AppendSymbolEnd(symbol, symbolRefId);
        }

        protected virtual void AppendLocation(Location location)
        {
            if (location == null)
            {
                AppendNull();
                return;
            }

            AppendLocationData(location);
        }

        protected abstract void AppendNull();

        private void AppendLocationArray(in ImmutableArray<Location> values)
        {
            AppendArray(values, _writeLocation);
        }

        private void AppendSymbolArray<TSymbol>(in ImmutableArray<TSymbol> symbols)
            where TSymbol : ISymbol
        {
            AppendArray(symbols, _writeSymbol);
        }

        private void AppendAliasSymbol(IAliasSymbol symbol)
        {
            var name = symbol.Name;
            var target = symbol.Target;
            var filePath = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath ?? "";

            AppendAliasData(name, target, filePath);
        }

        private void AppendAnonymousDelegateTypeSymbol(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsAnonymousDelegateType());

            var isAnonymousDelegateType = true;
            var location = symbol.Locations.FirstOrDefault();

            AppendAnonymousDelegateTypeOrFunctionData(isAnonymousDelegateType, location);
        }

        private void AppendAnonymousFunctionSymbol(IMethodSymbol symbol)
        {
            Debug.Assert(symbol.MethodKind == MethodKind.AnonymousFunction);

            var isAnonymousDelegateType = false;
            var location = symbol.Locations.FirstOrDefault();

            AppendAnonymousDelegateTypeOrFunctionData(isAnonymousDelegateType, location);
        }

        private void AppendAnonymousTypeSymbol(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsAnonymousType);

            var propertyTypes = ArrayBuilder<ITypeSymbol>.GetInstance();
            var propertyNames = ArrayBuilder<string>.GetInstance();
            var propertyIsReadOnly = ArrayBuilder<bool>.GetInstance();
            var propertyLocations = ArrayBuilder<Location>.GetInstance();

            foreach (var member in symbol.GetMembers())
            {
                if (member is IPropertySymbol propertySymbol)
                {
                    propertyTypes.Add(propertySymbol.Type);
                    propertyNames.Add(propertySymbol.Name);
                    propertyIsReadOnly.Add(propertySymbol.SetMethod == null);
                    propertyLocations.Add(propertySymbol.Locations.FirstOrDefault());
                }
            }

            AppendAnonymousTypeData(
                propertyTypes.ToImmutableAndFree(),
                propertyNames.ToImmutableAndFree(),
                propertyIsReadOnly.ToImmutableAndFree(),
                propertyLocations.ToImmutableAndFree());
        }

        private void AppendArrayTypeSymbol(IArrayTypeSymbol symbol)
        {
            var elementType = symbol.ElementType;
            var rank = symbol.Rank;

            AppendArrayTypeData(elementType, rank);
        }

        private void AppendAssemblySymbol(IAssemblySymbol symbol)
        {
            // For now, we only store the name portion of an assembly's identity.
            var assemblyName = symbol.Identity.Name;

            AppendAssemblyData(assemblyName);
        }

        private void AppendBodyLevelSymbol(ISymbol symbol)
        {
            var localName = symbol.Name;
            var containingSymbol = symbol.ContainingSymbol;

            while (!containingSymbol.DeclaringSyntaxReferences.Any())
            {
                containingSymbol = containingSymbol.ContainingSymbol;
            }

            var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;
            var kind = symbol.Kind;
            var ordinal = 0;

            var symbols = GetSymbols(compilation, containingSymbol, kind, localName, _cancellationToken);

            for (var i = 0; i < symbols.Length; i++)
            {
                var possibleSymbol = symbols[i];

                if (possibleSymbol.Equals(symbol))
                {
                    ordinal = i;
                    break;
                }
            }

            AppendBodyLevelData(localName, containingSymbol, ordinal, kind);
        }

        private void AppendConstructedMethodSymbol(IMethodSymbol symbol)
        {
            var constructedFrom = symbol.ConstructedFrom;
            var typeArguments = symbol.TypeArguments;

            AppendConstructedMethodData(constructedFrom, typeArguments);
        }

        private void AppendDynamicTypeSymbol(IDynamicTypeSymbol symbol)
        {
            AppendDynamicTypeData();
        }

        private void AppendErrorTypeSymbol(INamedTypeSymbol symbol)
        {
            var name = symbol.Name;
            var containingSymbol = symbol.ContainingSymbol as INamespaceOrTypeSymbol;
            var arity = symbol.Arity;
            var typeArguments = !symbol.Equals(symbol.ConstructedFrom)
                ? symbol.TypeArguments
                : default;

            AppendErrorTypeData(name, containingSymbol, arity, typeArguments);
        }

        private void AppendEventSymbol(IEventSymbol symbol)
        {
            var metadataName = symbol.MetadataName;
            var containingType = symbol.ContainingType;

            AppendEventData(metadataName, containingType);
        }

        private void AppendFieldSymbol(IFieldSymbol symbol)
        {
            var metadataName = symbol.MetadataName;
            var containingType = symbol.ContainingType;

            AppendFieldData(metadataName, containingType);
        }

        private void AppendModuleSymbol(IModuleSymbol symbol)
        {
            var containingSymbol = symbol.ContainingSymbol;

            AppendModuleData(containingSymbol);
        }

        private void AppendLabelSymbol(ILabelSymbol symbol)
        {
            AppendBodyLevelSymbol(symbol);
        }

        private void AppendLocalSymbol(ILocalSymbol symbol)
        {
            AppendBodyLevelSymbol(symbol);
        }

        private void AppendParameterSymbol(IParameterSymbol symbol)
        {
            var metadataName = symbol.MetadataName;
            var containingSymbol = symbol.ContainingSymbol;

            AppendParameterData(metadataName, containingSymbol);
        }

        private void AppendMethodSymbol(IMethodSymbol symbol)
        {
            switch (symbol.MethodKind)
            {
                case MethodKind.ReducedExtension:
                    AppendReducedExtensionMethodSymbol(symbol);
                    break;
                case MethodKind.AnonymousFunction:
                    AppendAnonymousFunctionSymbol(symbol);
                    break;
                case MethodKind.LocalFunction:
                    AppendBodyLevelSymbol(symbol);
                    break;
                default:
                    if (!symbol.Equals(symbol.ConstructedFrom))
                    {
                        AppendConstructedMethodSymbol(symbol);
                    }
                    else
                    {
                        var metadataName = symbol.MetadataName;
                        var containingSymbol = symbol.ContainingSymbol;
                        var arity = symbol.Arity;
                        var isPartialMethodImplementationPart = symbol.PartialDefinitionPart != null;
                        var parameters = symbol.OriginalDefinition.Parameters;
                        var returnType = symbol.MethodKind == MethodKind.Conversion
                            ? symbol.ReturnType
                            : null;

                        AppendMethodData(metadataName, containingSymbol, arity, isPartialMethodImplementationPart, parameters, returnType);
                    }

                    break;
            }
        }

        private void AppendNamedTypeSymbol(INamedTypeSymbol symbol)
        {
            if (symbol.Kind == SymbolKind.ErrorType)
            {
                AppendErrorTypeSymbol(symbol);
            }
            else if (symbol.IsTupleType)
            {
                AppendTupleTypeSymbol(symbol);
            }
            else if (symbol.IsAnonymousType)
            {
                if (symbol.IsDelegateType())
                {
                    AppendAnonymousDelegateTypeSymbol(symbol);
                }
                else
                {
                    AppendAnonymousTypeSymbol(symbol);
                }
            }
            else
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;
                var arity = symbol.Arity;
                var typeKind = symbol.TypeKind;
                var isUnboundGenericType = symbol.IsUnboundGenericType;
                var typeArguments = !symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType
                    ? symbol.TypeArguments
                    : default;

                AppendNamedTypeData(metadataName, containingSymbol, arity, typeKind, isUnboundGenericType, typeArguments);
            }
        }

        private void AppendNamespaceSymbol(INamespaceSymbol symbol)
        {
            var metadataName = symbol.MetadataName;
            var isCompilationGlobalNamespace = false;
            var containingSymbol = (ISymbol)symbol.ContainingNamespace;

            // The containing symbol can be one of many things:
            //
            //   1. Null when this is the global namespace for a compilation.
            //   2. The SymbolId for an assembly symbol if this is the global namespace for an assembly.
            //   3. The SymbolId for a module symbol if this is the global namespace for a module.
            //   4. The SymbolId for the containing namespace symbol if this is not a global namespace.

            if (containingSymbol == null)
            {
                // A global namespace can either belong to a module, assembly or to a compilation.
                Debug.Assert(symbol.IsGlobalNamespace);

                switch (symbol.NamespaceKind)
                {
                    case NamespaceKind.Module:
                        containingSymbol = symbol.ContainingModule;
                        break;
                    case NamespaceKind.Assembly:
                        containingSymbol = symbol.ContainingAssembly;
                        break;
                    case NamespaceKind.Compilation:
                        isCompilationGlobalNamespace = true;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported namespace kind: {symbol.NamespaceKind}");
                }
            }

            AppendNamespaceData(metadataName, isCompilationGlobalNamespace, containingSymbol);
        }

        private void AppendPointerTypeSymbol(IPointerTypeSymbol symbol)
        {
            var pointedAtType = symbol.PointedAtType;

            AppendPointerTypeData(pointedAtType);
        }

        private void AppendPropertySymbol(IPropertySymbol symbol)
        {
            var metadataName = symbol.MetadataName;
            var containingSymbol = symbol.ContainingSymbol;
            var isIndexer = symbol.IsIndexer;
            var parameters = symbol.OriginalDefinition.Parameters;

            AppendPropertyData(metadataName, containingSymbol, isIndexer, parameters);
        }

        private void AppendRangeVariableSymbol(IRangeVariableSymbol symbol)
        {
            AppendBodyLevelSymbol(symbol);
        }

        private void AppendReducedExtensionMethodSymbol(IMethodSymbol symbol)
        {
            var reducedFrom = symbol.ReducedFrom;
            var receiverType = symbol.ReceiverType;

            AppendReducedExtensionMethodData(reducedFrom, receiverType);
        }

        private void AppendTupleTypeSymbol(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsTupleType);

            var friendlyNames = ArrayBuilder<string>.GetInstance();
            var locations = ArrayBuilder<Location>.GetInstance();
            var isError = symbol.TupleUnderlyingType.TypeKind == TypeKind.Error;

            foreach (var element in symbol.TupleElements)
            {
                friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                locations.Add(element.Locations.FirstOrDefault() ?? Location.None);
            }

            if (isError)
            {
                var elementTypes = ArrayBuilder<ISymbol>.GetInstance();

                foreach (var element in symbol.TupleElements)
                {
                    elementTypes.Add(element.Type);
                }

                AppendErrorTupleTypeData(
                    elementTypes.ToImmutableAndFree(),
                    friendlyNames.ToImmutableAndFree(),
                    locations.ToImmutableAndFree());
            }
            else
            {
                AppendTupleTypeData(
                    symbol.TupleUnderlyingType,
                    friendlyNames.ToImmutableAndFree(),
                    locations.ToImmutableAndFree());
            }
        }

        private void AppendTypeParameterSymbol(ITypeParameterSymbol symbol)
        {
            // If this is a method type parameter, then we should try to write out the ordinal to that method
            // to avoid further recursion in cases like M<T>(T t).

            if (symbol.TypeParameterKind == TypeParameterKind.Method &&
                _symbolRefIdMap.TryGetValue(symbol.DeclaringMethod, out var methodRefId))
            {
                var ordinal = symbol.Ordinal;
                AppendTypeParameterOrdinalData(methodRefId, ordinal);
            }
            else
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;

                AppendTypeParameterData(metadataName, containingSymbol);
            }
        }

        private static ImmutableArray<ISymbol> GetSymbols(
            Compilation compilation, ISymbol containingSymbol,
            SymbolKind kind, string localName,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance();

            foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
            {
                // This operation can potentially fail. If containingSymbol came from 
                // a SpeculativeSemanticModel, containingSymbol.ContainingAssembly.Compilation
                // may not have been rebuilt to reflect the trees used by the 
                // SpeculativeSemanticModel to produce containingSymbol. In that case,
                // asking the ContainingAssembly's compilation for a SemanticModel based
                // on trees for containingSymbol with throw an ArgumentException.
                // Unfortunately, the best way to avoid this (currently) is to see if
                // we're asking for a model for a tree that's part of the compilation.
                // (There's no way to get back to a SemanticModel from a symbol).

                // TODO (rchande): It might be better to call compilation.GetSemanticModel
                // and catch the ArgumentException. The compilation internally has a 
                // Dictionary<SyntaxTree, ...> that it uses to check if the SyntaxTree
                // is applicable wheras the public interface requires us to enumerate
                // the entire IEnumerable of trees in the Compilation.
                if (!compilation.SyntaxTrees.Contains(declaringLocation.SyntaxTree))
                {
                    continue;
                }

                var node = declaringLocation.GetSyntax(cancellationToken);
                if (node.Language == LanguageNames.VisualBasic)
                {
                    node = node.Parent;
                }

                var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

                foreach (var n in node.DescendantNodes())
                {
                    var symbol = semanticModel.GetDeclaredSymbol(n, cancellationToken);

                    if (symbol != null &&
                        symbol.Kind == kind &&
                        compilation.NamesAreEqual(symbol.Name, localName))
                    {
                        result.Add(symbol);
                    }
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
