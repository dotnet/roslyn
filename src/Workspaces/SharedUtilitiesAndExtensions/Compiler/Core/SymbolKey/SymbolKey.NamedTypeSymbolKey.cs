// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class NamedTypeSymbolKey : AbstractSymbolKey<INamedTypeSymbol>
    {
        public static readonly NamedTypeSymbolKey Instance = new();

        public sealed override void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteSymbolKey(symbol.ContainingSymbol);
            visitor.WriteString(symbol.Name);
            visitor.WriteInteger(symbol.Arity);
            visitor.WriteString(symbol.IsFileLocal
                ? symbol.DeclaringSyntaxReferences[0].SyntaxTree.FilePath
                : null);
            visitor.WriteBoolean(symbol.IsUnboundGenericType);
            visitor.WriteBoolean(symbol.IsNativeIntegerType);
            visitor.WriteBoolean(symbol.SpecialType == SpecialType.System_IntPtr);

            if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
            {
                visitor.WriteSymbolKeyArray(symbol.TypeArguments);
            }
            else
            {
                visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
            }
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, INamedTypeSymbol? contextualSymbol, out string? failureReason)
        {
            var containingSymbolResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingSymbol, out var containingSymbolFailureReason);
            var name = reader.ReadRequiredString();
            var arity = reader.ReadInteger();
            var filePath = reader.ReadString();
            var isUnboundGenericType = reader.ReadBoolean();
            var isNativeIntegerType = reader.ReadBoolean();
            var signed = reader.ReadBoolean();

            using var typeArguments = reader.ReadSymbolKeyArray<INamedTypeSymbol, ITypeSymbol>(
                contextualSymbol,
                getContextualSymbol: static (contextualType, i) => SafeGet(contextualType.TypeArguments, i),
                out var typeArgumentsFailureReason);

            // If we started with nint/nuint go back to that specific type if the language allows for it.
            if (isNativeIntegerType && reader.Compilation.Language == LanguageNames.CSharp)
            {
                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.CreateNativeIntegerTypeSymbol(signed));
            }

            if (typeArgumentsFailureReason != null)
            {
                Contract.ThrowIfFalse(typeArguments.IsDefault);

                failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                return default;
            }

            Contract.ThrowIfTrue(typeArguments.IsDefault);

            var typeArgumentsArray = typeArguments.Count == 0 ? [] : typeArguments.Builder.ToArray();

            var normalResolution = ResolveNormalNamedType(
                containingSymbolResolution, containingSymbolFailureReason,
                name, arity, filePath, isUnboundGenericType, typeArgumentsArray,
                out failureReason);

            if (normalResolution.SymbolCount > 0)
                return normalResolution;

            return ResolveContextualErrorType(
                reader, contextualSymbol,
                containingSymbolResolution,
                name, arity, isUnboundGenericType, typeArgumentsArray,
                ref failureReason);
        }

        private static SymbolKeyResolution ResolveContextualErrorType(
            SymbolKeyReader reader,
            INamedTypeSymbol? contextualType,
            SymbolKeyResolution containingSymbolResolution,
            string name,
            int arity,
            bool isUnboundGenericType,
            ITypeSymbol[] typeArgumentsArray,
            ref string? failureReason)
        {
            // we weren't able to bind the container of this named type to something legitimate.  In normal cases,
            // that would be the end of resolution.  However, if we are binding in a scenario where we have a
            // contextual type that is an error type, we can see if our symbol key is a viable match for that error
            // type.  
            //
            // For example, consider if our symbol key references System.String, but we're resolving against a
            // compilation that is missing a reference to System.String, but has a method `Goo(string s)` which
            // references it.  This `string s` in `Goo` will be an error type symbol for `System.String` and we *do*
            // want to allow this to match.
            //
            // This is fundamentally inverted from normal resolution.  Normal resolution walks top down from the
            // root of the compilation to find the match.  However, error symbols cannot be found in that fashion.
            // Instead, we have to structurally match this error symbol against our own symbol key to see if it is
            // valid.
            if (contextualType is not IErrorTypeSymbol errorType)
                return default;

            // Check name/arity. If not hte same, this symbol key isn't referring to the same contextual type
            // that we're currently looking at.
            if (errorType.Name != name || errorType.Arity != arity)
                return default;

            // If we didn't successfully resolve the containing namespace, then use the contextual type's containing
            // namespace.  Note: we only do this for namespaces and not containing types.  For containing types we
            // use recursion to resolve that container properly.  If that failed, then we don't want to continue.
            if (containingSymbolResolution.SymbolCount == 0 && errorType.ContainingSymbol is INamespaceSymbol containingNamespace)
                containingSymbolResolution = new SymbolKeyResolution(containingNamespace);

            using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
            foreach (var container in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
            {
                result.AddIfNotNull(Construct(
                    reader.Compilation.CreateErrorTypeSymbol(container, name, arity),
                    isUnboundGenericType,
                    typeArgumentsArray));
            }

            return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed contextual error resolution)", out failureReason);
        }

        private static SymbolKeyResolution ResolveNormalNamedType(
            SymbolKeyResolution containingSymbolResolution,
            string? containingSymbolFailureReason,
            string name,
            int arity,
            string? filePath,
            bool isUnboundGenericType,
            ITypeSymbol[] typeArgumentsArray,
            out string? failureReason)
        {
            if (containingSymbolFailureReason != null)
            {
                failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(containingSymbolFailureReason)} failed -> {containingSymbolFailureReason})";
                return default;
            }

            using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
            foreach (var nsOrType in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
            {
                Resolve(
                    result, nsOrType, name, arity, filePath,
                    isUnboundGenericType, typeArgumentsArray);
            }

            return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed)", out failureReason);
        }

        private static void Resolve(
            PooledArrayBuilder<INamedTypeSymbol> result,
            INamespaceOrTypeSymbol container,
            string name,
            int arity,
            string? filePath,
            bool isUnboundGenericType,
            ITypeSymbol[] typeArguments)
        {
            foreach (var type in container.GetTypeMembers(name, arity))
            {
                // if this is a file-local type, then only resolve to a file-local type from this same file
                if (filePath != null)
                {
                    if (!type.IsFileLocal ||
                        // note: if we found 'IsFile' returned true, we can assume DeclaringSyntaxReferences is non-empty.
                        type.DeclaringSyntaxReferences[0].SyntaxTree.FilePath != filePath)
                    {
                        continue;
                    }
                }
                else if (type.IsFileLocal)
                {
                    // since this key lacks a file path it can't match against a file-local type
                    continue;
                }

                result.AddIfNotNull(Construct(type, isUnboundGenericType, typeArguments));
            }
        }

        private static INamedTypeSymbol Construct(INamedTypeSymbol type, bool isUnboundGenericType, ITypeSymbol[] typeArguments)
        {
            var currentType = typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
            currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;
            return currentType;
        }
    }
}
