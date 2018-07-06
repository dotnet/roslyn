// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal class SymbolKeyResolver
    {
        private const char OpenParen = '(';
        private const char CloseParen = ')';
        private const char Space = ' ';
        private const char DoubleQuote = '"';

        private static readonly ObjectPool<SymbolKeyResolver> s_resolverPool = new ObjectPool<SymbolKeyResolver>(() => new SymbolKeyResolver());

        private readonly Dictionary<int, ResolvedSymbolInfo> _symbolRefIdToResolutionMap;

        private readonly Func<ResolvedSymbolInfo> _resolveSymbol;

        private string _symbolKey;
        private int _position;
        private Compilation _compilation;
        private CancellationToken _cancellationToken;
        private bool _ignoreAssemblyNames;

        private SymbolKeyResolver()
        {
            _symbolRefIdToResolutionMap = new Dictionary<int, ResolvedSymbolInfo>();
            _resolveSymbol = ResolveSymbol;
        }

        protected virtual void Reset()
        {
            _symbolRefIdToResolutionMap.Clear();

            _symbolKey = null;
            _position = 0;
            _compilation = null;
            _cancellationToken = default;
            _ignoreAssemblyNames = false;
        }

        public static ResolvedSymbolInfo Resolve(string symbolKey, Compilation compilation, bool ignoreAssemblyNames = false, CancellationToken cancellationToken = default)
        {
            var resolver = s_resolverPool.Allocate();

            resolver._symbolKey = symbolKey;
            resolver._position = 0;
            resolver._compilation = compilation;
            resolver._cancellationToken = cancellationToken;
            resolver._ignoreAssemblyNames = ignoreAssemblyNames;

            var result = resolver.ResolveSymbol();

            // Place back in the pool for future use.
            s_resolverPool.Free(resolver);

            return result;
        }

        private ResolvedSymbolInfo ResolveSymbol()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            SkipSpaces();

            if (_symbolKey[_position] == SymbolKeyType.Null)
            {
                _position++;
                return default;
            }

            SkipChar(OpenParen);

            var type = _symbolKey[_position++];

            ResolvedSymbolInfo result;
            if (type == SymbolKeyType.Reference)
            {
                var symbolRefId = ReadInt();
                result = _symbolRefIdToResolutionMap[symbolRefId];
            }
            else
            {
                result = ResolveSymbol(type);
                var symbolRefId = ReadInt();
                _symbolRefIdToResolutionMap[symbolRefId] = result;
            }

            SkipChar(CloseParen);

            return result;
        }

        private ResolvedSymbolInfo ResolveSymbol(char type)
        {
            switch (type)
            {
                case SymbolKeyType.Alias: return ResolveAliasSymbol();
                case SymbolKeyType.AnonymousFunctionOrDelegate: return ResolveAnonymousFunctionOrDelegateTypeSymbol();
                case SymbolKeyType.AnonymousType: return ResolveAnonymousTypeSymbol();
                case SymbolKeyType.ArrayType: return ResolveArrayTypeSymbol();
                case SymbolKeyType.Assembly: return ResolveAssemblySymbol();
                case SymbolKeyType.BodyLevel: return ResolveBodyLevelSymbol();
                case SymbolKeyType.ConstructedMethod: return ResolveConstructedMethodSymbol();
                case SymbolKeyType.DynamicType: return ResolveDynamicTypeSymbol();
                case SymbolKeyType.ErrorType: return ResolveErrorTypeSymbol();
                case SymbolKeyType.Event: return ResolveEventSymbol();
                case SymbolKeyType.Field: return ResolveFieldSymbol();
                case SymbolKeyType.Method: return ResolveMethodSymbol();
                case SymbolKeyType.Module: return ResolveModuleSymbol();
                case SymbolKeyType.NamedType: return ResolveNamedTypeSymbol();
                case SymbolKeyType.Namespace: return ResolveNamespaceSymbol();
                case SymbolKeyType.Parameter: return ResolveParameterSymbol();
                case SymbolKeyType.PointerType: return ResolvePointerTypeSymbol();
                case SymbolKeyType.Property: return ResolvePropertySybmol();
                case SymbolKeyType.ReducedExtensionMethod: return ResolveReducedExtensionMethodSymbol();
                case SymbolKeyType.TupleType: return ResolveTupleTypeSymbol();
                case SymbolKeyType.TypeParameter: return ResolveTypeParameterSymbol();
                case SymbolKeyType.TypeParameterOrdinal: return ResolveTypeParameterOrdinalSymbol();
            }

            throw new NotSupportedException();
        }

        private void SkipChar(char expected)
        {
            var ch = _symbolKey[_position++];

            if (ch != expected)
            {
                throw new InvalidOperationException();
            }
        }

        private void SkipSpaces()
        {
            while (_symbolKey[_position] == ' ')
            {
                _position++;
            }
        }

        private bool ReadBool()
        {
            var value = ReadInt();

            if (value != 0 && value != 1)
            {
                throw new InvalidOperationException();
            }

            return value == 1;
        }

        private int ReadInt()
        {
            SkipSpaces();

            var ch = _symbolKey[_position];

            if (!char.IsNumber(ch))
            {
                throw new InvalidOperationException();
            }

            var result = 0;

            do
            {
                result = (result * 10) + (ch - '0');
                ch = _symbolKey[++_position];
            }
            while (char.IsNumber(ch));

            return result;
        }

        private string ReadString()
        {
            SkipSpaces();

            if (_symbolKey[_position] == SymbolKeyType.Null)
            {
                _position++;
                return null;
            }

            SkipChar(DoubleQuote);

            var start = _position;
            var hasEscapedQuote = false;

            while (true)
            {
                if (_symbolKey[_position] != DoubleQuote)
                {
                    _position++;
                    continue;
                }

                if (_position + 1 < _symbolKey.Length && _symbolKey[_position + 1] == DoubleQuote)
                {
                    hasEscapedQuote = true;
                    _position += 2;
                    continue;
                }

                break;
            }

            var end = _position;

            SkipChar(DoubleQuote);

            var result = _symbolKey.Substring(start, end - start);

            return hasEscapedQuote
                ? result.Replace("\"\"", "\"")
                : result;
        }

        private ImmutableArray<T> ReadArray<T>(Func<T> readFunction)
        {
            SkipSpaces();

            if (_symbolKey[_position] == SymbolKeyType.Null)
            {
                _position++;
                return default;
            }

            SkipChar(OpenParen);
            SkipChar(SymbolKeyType.Array);

            var length = ReadInt();
            var builder = ImmutableArray.CreateBuilder<T>(length);

            for (var i = 0; i < length; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                builder.Add(readFunction());
            }

            SkipChar(CloseParen);

            return builder.MoveToImmutable();
        }

        public ImmutableArray<ResolvedSymbolInfo> ResolveSymbolArray()
            => ReadArray(_resolveSymbol);

        private ResolvedSymbolInfo ResolveAliasSymbol()
        {
            var name = ReadString();
            var resolvedTarget = ResolveSymbol();
            var filePath = ReadString();

            var syntaxTree = _compilation.SyntaxTrees.FirstOrDefault(tree => StringComparer.OrdinalIgnoreCase.Equals(tree.FilePath, filePath));
            if (syntaxTree != null)
            {
                var target = resolvedTarget.GetAnySymbol();
                if (target != null)
                {
                    var semanticModel = _compilation.GetSemanticModel(syntaxTree);
                    var result = ResolveAliasSymbol(name, target, syntaxTree.GetRoot(_cancellationToken), semanticModel, _cancellationToken);
                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                }
            }

            return default;
        }

        private ResolvedSymbolInfo? ResolveAliasSymbol(string name, ISymbol target, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Alias)
                {
                    var aliasSymbol = (IAliasSymbol)symbol;
                    if (aliasSymbol.Name == name &&
                        SymbolEquivalenceComparer.Instance.Equals(aliasSymbol.Target, target))
                    {
                        return new ResolvedSymbolInfo(aliasSymbol);
                    }
                }
                else if (symbol.Kind != SymbolKind.Namespace)
                {
                    // Don't recurse into anything except namespaces. We can't find aliases any deeper than that.
                    return null;
                }
            }

            foreach (var child in node.ChildNodes())
            {
                var result = ResolveAliasSymbol(name, target, child, semanticModel, cancellationToken);
                if (result.HasValue)
                {
                    return result;
                }
            }

            return null;
        }

        private ResolvedSymbolInfo ResolveAnonymousFunctionOrDelegateTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveAnonymousTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveArrayTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveAssemblySymbol()
        {
            var assemblyName = ReadString();

            var assemblySymbols = ArrayBuilder<IAssemblySymbol>.GetInstance();

            if (_ignoreAssemblyNames || _compilation.Assembly.Identity.Name == assemblyName)
            {
                assemblySymbols.Add(_compilation.Assembly);
            }

            // Might need keys for symbols from previous script compilations.
            foreach (var assembly in _compilation.GetReferencedAssemblySymbols())
            {
                if (_ignoreAssemblyNames || assembly.Identity.Name == assemblyName)
                {
                    assemblySymbols.Add(assembly);
                }
            }

            return ResolvedSymbolInfo.Create(assemblySymbols.ToImmutableAndFree());
        }

        private ResolvedSymbolInfo ResolveBodyLevelSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveConstructedMethodSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveDynamicTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveErrorTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveEventSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveFieldSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveMethodSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveModuleSymbol()
        {
            var resolvedContainingSymbol = ResolveSymbol();

            var modules = ArrayBuilder<IModuleSymbol>.GetInstance();

            foreach (var assemblySymbol in resolvedContainingSymbol.GetAllSymbols<IAssemblySymbol>())
            {
                // Don't check ModuleIds for equality because in practice, no-one uses them,
                // and there is no way to set netmodule name programmatically using Roslyn
                modules.AddRange(assemblySymbol.Modules);
            }

            return ResolvedSymbolInfo.Create(modules.ToImmutableAndFree());
        }

        private ResolvedSymbolInfo ResolveNamedTypeSymbol()
        {
            var metadataName = ReadString();
            var resolvedContainingSymbol = ResolveSymbol();
            var arity = ReadInt();
            var typeKind = (TypeKind)ReadInt();
            var isUnboundGenericType = ReadBool();
            var resolvedTypeArguments = ResolveSymbolArray();

            var builder = ArrayBuilder<INamedTypeSymbol>.GetInstance();

            foreach (var containingSymbol in resolvedContainingSymbol.GetAllSymbols<INamespaceOrTypeSymbol>())
            {
                var backtickIndex = metadataName.IndexOf('`');
                if (backtickIndex > 0)
                {
                    metadataName = metadataName.Substring(0, backtickIndex);
                }

                var types = containingSymbol.GetTypeMembers(metadataName, arity);
                var constructedTypes = ConstructTypes(types, resolvedTypeArguments, arity);

                if (isUnboundGenericType)
                {
                    builder.AddRange(builder.SelectAsArray(t => t.ConstructUnboundGenericType()));
                }
                else
                {
                    builder.AddRange(constructedTypes);
                }
            }

            return ResolvedSymbolInfo.Create(builder.ToImmutableAndFree());
        }

        private ResolvedSymbolInfo ResolveNamespaceSymbol()
        {
            var metadataName = ReadString();
            var isCompilationGlobalNamespace = ReadBool();
            var resolvedContainingSymbol = ResolveSymbol();

            if (isCompilationGlobalNamespace)
            {
                return new ResolvedSymbolInfo(_compilation.GlobalNamespace);
            }

            var builder = ArrayBuilder<INamespaceSymbol>.GetInstance();

            foreach (var symbol in resolvedContainingSymbol.GetAllSymbols())
            {
                switch (symbol)
                {
                    case IAssemblySymbol assemblySymbol:
                        Debug.Assert(metadataName == string.Empty);
                        builder.Add(assemblySymbol.GlobalNamespace);
                        break;
                    case IModuleSymbol moduleSymbol:
                        Debug.Assert(metadataName == string.Empty);
                        builder.Add(moduleSymbol.GlobalNamespace);
                        break;
                    case INamespaceSymbol namespaceSymbol:
                        foreach (var member in namespaceSymbol.GetMembers(metadataName))
                        {
                            if (member is INamespaceSymbol childNamespaceSymbol)
                            {
                                builder.Add(childNamespaceSymbol);
                            }
                        }

                        break;
                    default:
                        builder.Clear();
                        break;
                }
            }

            var namespaces = builder.ToImmutableAndFree();

            return ResolvedSymbolInfo.Create(namespaces);
        }

        private ResolvedSymbolInfo ResolveParameterSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolvePointerTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolvePropertySybmol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveReducedExtensionMethodSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveTupleTypeSymbol()
        {
            throw new NotImplementedException();
        }

        private ResolvedSymbolInfo ResolveTypeParameterSymbol()
        {
            var metadataName = ReadString();
            var resolvedContainingSymbol = ResolveSymbol();

            var result = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

            foreach (var container in resolvedContainingSymbol.GetAllSymbols())
            {
                switch (container)
                {
                    case INamedTypeSymbol namedTypeSymbol:
                        AddTypeParameters(namedTypeSymbol.TypeParameters);
                        break;
                    case IMethodSymbol methodSymbol:
                        AddTypeParameters(methodSymbol.TypeParameters);
                        break;
                }
            }

            return ResolvedSymbolInfo.Create(result.ToImmutableAndFree());

            void AddTypeParameters(ImmutableArray<ITypeParameterSymbol> typeParameters)
            {
                // TODO(dustinca): Should we check case-insensitively for VB here?
                foreach (var typeParameter in typeParameters)
                {
                    if (_compilation.NamesAreEqual(typeParameter.MetadataName, metadataName))
                    {
                        result.Add(typeParameter);
                    }
                }
            }
        }

        private ResolvedSymbolInfo ResolveTypeParameterOrdinalSymbol()
        {
            throw new NotImplementedException();
        }

        private readonly static Func<ITypeSymbol, bool> s_typeIsNull = t => t == null;

        private static ImmutableArray<INamedTypeSymbol> ConstructTypes(ImmutableArray<INamedTypeSymbol> types, ImmutableArray<ResolvedSymbolInfo> resolvedTypeArguments, int arity)
        {
            if (arity == 0 || resolvedTypeArguments.IsDefault)
            {
                return types;
            }

            var typeArguments = resolvedTypeArguments
                .SelectAsArray(r => r.GetFirstSymbol<ITypeSymbol>())
                .ToArray();

            if (typeArguments.Any(s_typeIsNull))
            {
                return ImmutableArray<INamedTypeSymbol>.Empty;
            }

            return types.SelectAsArray(t => t.Construct(typeArguments));
        }
    }
}
