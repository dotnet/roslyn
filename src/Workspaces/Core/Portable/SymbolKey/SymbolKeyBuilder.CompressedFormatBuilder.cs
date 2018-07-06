// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial class SymbolKeyBuilder
    {
        private class CompressedFormatBuilder : SymbolKeyBuilder
        {
            private readonly Action<bool> _appendBool;
            private readonly Action<IParameterSymbol> _appendParameterRefKind;
            private readonly Action<IParameterSymbol> _appendParameterType;
            private readonly Action<string> _appendString;

            private int _nestingCount;

            public CompressedFormatBuilder()
            {
                _appendBool = AppendBool;
                _appendParameterRefKind = p => AppendInt((int)p.RefKind);
                _appendParameterType = p => AppendSymbol(p.Type);
                _appendString = AppendString;
            }

            protected override void Reset()
            {
                base.Reset();
                _nestingCount = 0;
            }

            private void AppendBool(bool value)
            {
                AppendInt(value ? 1 : 0);
            }

            private void AppendInt(int value)
            {
                AppendSpace();
                _builder.Append(value);
            }

            private void AppendSpace()
            {
                _builder.Append(' ');
            }

            private void AppendString(string value)
            {
                AppendSpace();

                if (value == null)
                {
                    AppendNull();
                }
                else
                {
                    _builder.Append('"');
                    _builder.Append(value.Replace("\"", "\"\""));
                    _builder.Append('"');
                }
            }

            private void AppendType(char type)
            {
                _builder.Append(type);
            }

            protected override void AppendNull()
            {
                AppendType(SymbolKeyType.Null);
            }

            protected override void AppendSymbol(ISymbol symbol)
            {
                if (_builder.Length > 0)
                {
                    AppendSpace();
                }

                base.AppendSymbol(symbol);
            }

            protected override void AppendLocation(Location location)
            {
                AppendSpace();
                base.AppendLocation(location);
            }

            protected override void AppendSymbolStart(ISymbol symbol, int symbolRefId)
            {
                _builder.Append('(');
                _nestingCount++;
            }

            protected override void AppendSymbolEnd(ISymbol symbol, int symbolRefId)
            {
                Debug.Assert(_nestingCount > 0);
                _nestingCount--;

                AppendInt(symbolRefId);
                _builder.Append(')');
            }

            protected override void AppendSymbolReference(int symbolRefId)
            {
                if (_builder.Length > 0 && _builder[_builder.Length - 1] != ' ')
                {
                    AppendSpace();
                }

                _builder.Append('(');
                AppendType(SymbolKeyType.Reference);
                AppendInt(symbolRefId);
                _builder.Append(')');
            }

            protected override void AppendArray<T1, T2>(in ImmutableArray<T1> array, Action<T2> writeValue)
            {
                AppendSpace();

                if (array.IsDefault)
                {
                    AppendNull();
                    return;
                }

                _builder.Append('(');
                AppendType(SymbolKeyType.Array);

                AppendInt(array.Length);

                foreach (var value in array)
                {
                    writeValue(value);
                }

                _builder.Append(')');
            }

            private void WriteBoolArray(in ImmutableArray<bool> values)
            {
                AppendArray(values, _appendBool);
            }

            private void WriteParameterTypesArray(in ImmutableArray<IParameterSymbol> symbols)
            {
                AppendArray(symbols, _appendParameterType);
            }

            private void WriteParameterRefKindsArray(in ImmutableArray<IParameterSymbol> values)
            {
                AppendArray(values, _appendParameterRefKind);
            }

            private void WriteStringArray(in ImmutableArray<string> values)
            {
                AppendArray(values, _appendString);
            }

            protected override void AppendAliasData(string name, INamespaceOrTypeSymbol target, string filePath)
            {
                AppendType(SymbolKeyType.Alias);
                AppendString(name);
                AppendSymbol(target);
                AppendString(filePath);
            }

            protected override void AppendAnonymousDelegateTypeOrFunctionData(bool isAnonymousDelegateType, Location location)
            {
                // Write out if this was an anonymous delegate or anonymous function.
                // In both cases they'll have the same location (the location of 
                // the lambda that forced them into existence).  When we resolve the
                // symbol later, if it's an anonymous delegate, we'll first resolve to
                // the anonymous-function, then use that anonymous-function to get at
                // the synthesized anonymous delegate.

                AppendType(SymbolKeyType.AnonymousFunctionOrDelegate);
                AppendBool(isAnonymousDelegateType);
                AppendLocation(location);
            }

            protected override void AppendAnonymousTypeData(
                ImmutableArray<ITypeSymbol> propertyTypes,
                ImmutableArray<string> propertyNames,
                ImmutableArray<bool> propertyIsReadOnly,
                ImmutableArray<Location> propertyLocations)
            {
                AppendType(SymbolKeyType.AnonymousType);
                AppendSymbolArray(propertyTypes);
                WriteStringArray(propertyNames);
                WriteBoolArray(propertyIsReadOnly);
                AppendLocationArray(propertyLocations);
            }

            protected override void AppendArrayTypeData(ITypeSymbol elementType, int rank)
            {
                AppendType(SymbolKeyType.ArrayType);
                AppendSymbol(elementType);
                AppendInt(rank);
            }

            protected override void AppendAssemblyData(string assemblyName)
            {
                AppendType(SymbolKeyType.Assembly);
                AppendString(assemblyName);
            }

            protected override void AppendBodyLevelData(string localName, ISymbol containingSymbol, int ordinal, SymbolKind kind)
            {
                AppendType(SymbolKeyType.BodyLevel);
                AppendString(localName);
                AppendSymbol(containingSymbol);
                AppendInt(ordinal);
                AppendInt((int)kind);
            }

            protected override void AppendConstructedMethodData(IMethodSymbol constructedFrom, ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendType(SymbolKeyType.ConstructedMethod);
                AppendSymbol(constructedFrom);
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendDynamicTypeData()
            {
                AppendType(SymbolKeyType.DynamicType);
            }

            protected override void AppendErrorTupleTypeData(
                ImmutableArray<ISymbol> elementTypes,
                ImmutableArray<string> friendlyNames,
                ImmutableArray<Location> locations)
            {
                var isError = true;

                AppendType(SymbolKeyType.TupleType);
                AppendBool(isError);
                AppendSymbolArray(elementTypes);
                WriteStringArray(friendlyNames);
                AppendLocationArray(locations);
            }

            protected override void AppendErrorTypeData(string name, INamespaceOrTypeSymbol containingSymbol, int arity, ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendType(SymbolKeyType.ErrorType);
                AppendString(name);
                AppendSymbol(containingSymbol);
                AppendInt(arity);
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendEventData(string metadataName, INamedTypeSymbol containingType)
            {
                AppendType(SymbolKeyType.Event);
                AppendString(metadataName);
                AppendSymbol(containingType);
            }

            protected override void AppendFieldData(string metadataName, INamedTypeSymbol containingType)
            {
                AppendType(SymbolKeyType.Field);
                AppendString(metadataName);
                AppendSymbol(containingType);
            }

            protected override void AppendLocationData(Location location)
            {
                Debug.Assert(location.Kind == LocationKind.None ||
                             location.Kind == LocationKind.SourceFile ||
                             location.Kind == LocationKind.MetadataFile);

                AppendInt((int)location.Kind);

                if (location.Kind == LocationKind.SourceFile)
                {
                    AppendString(location.SourceTree.FilePath);
                    AppendInt(location.SourceSpan.Start);
                    AppendInt(location.SourceSpan.Length);
                }
                else if (location.Kind == LocationKind.MetadataFile)
                {
                    AppendSymbol(location.MetadataModule.ContainingAssembly);
                    AppendString(location.MetadataModule.MetadataName);
                }
            }

            protected override void AppendMethodData(
                string metadataName,
                ISymbol containingSymbol,
                int arity,
                bool isPartialMethodImplementationPart,
                ImmutableArray<IParameterSymbol> parameters,
                ITypeSymbol returnType)
            {
                AppendType(SymbolKeyType.Method);
                AppendString(metadataName);
                AppendSymbol(containingSymbol);
                AppendInt(arity);
                AppendBool(isPartialMethodImplementationPart);
                WriteParameterRefKindsArray(parameters);
                WriteParameterTypesArray(parameters);
                AppendSymbol(returnType);
            }

            protected override void AppendModuleData(ISymbol containingSymbol)
            {
                AppendType(SymbolKeyType.Module);
                AppendSymbol(containingSymbol);
            }

            protected override void AppendNamedTypeData(
                string metadataName,
                ISymbol containingSymbol,
                int arity,
                TypeKind typeKind,
                bool isUnboundGenericType,
                ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendType(SymbolKeyType.NamedType);
                AppendString(metadataName);
                AppendSymbol(containingSymbol);
                AppendInt(arity);
                AppendInt((int)typeKind);
                AppendBool(isUnboundGenericType);
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendNamespaceData(string metadataName, bool isCompilationGlobalNamespace, ISymbol containingSymbol)
            {
                AppendType(SymbolKeyType.Namespace);
                AppendString(metadataName);
                AppendBool(isCompilationGlobalNamespace);
                AppendSymbol(containingSymbol);
            }

            protected override void AppendParameterData(string metadataName, ISymbol containingSymbol)
            {
                AppendType(SymbolKeyType.Parameter);
                AppendString(metadataName);
                AppendSymbol(containingSymbol);
            }

            protected override void AppendPointerTypeData(ITypeSymbol pointedAtType)
            {
                AppendType(SymbolKeyType.PointerType);
                AppendSymbol(pointedAtType);
            }

            protected override void AppendPropertyData(string metadataName, ISymbol containingSymbol, bool isIndexer, ImmutableArray<IParameterSymbol> originalParameters)
            {
                AppendType(SymbolKeyType.Property);
                AppendString(metadataName);
                AppendSymbol(containingSymbol);
                AppendBool(isIndexer);
                WriteParameterRefKindsArray(originalParameters);
                WriteParameterTypesArray(originalParameters);
            }

            protected override void AppendReducedExtensionMethodData(IMethodSymbol reducedFrom, ITypeSymbol receiverType)
            {
                AppendType(SymbolKeyType.ReducedExtensionMethod);
                AppendSymbol(reducedFrom);
                AppendSymbol(receiverType);
            }

            protected override void AppendTupleTypeData(
                INamedTypeSymbol tupleUnderlyingType,
                ImmutableArray<string> friendlyNames,
                ImmutableArray<Location> locations)
            {
                var isError = false;

                AppendType(SymbolKeyType.TupleType);
                AppendBool(isError);
                AppendSymbol(tupleUnderlyingType);
                WriteStringArray(friendlyNames);
                AppendLocationArray(locations);
            }

            protected override void AppendTypeParameterData(string metadataName, ISymbol containingSymbol)
            {
                AppendType(SymbolKeyType.TypeParameter);
                AppendString(metadataName);
                AppendSymbol(containingSymbol);
            }

            protected override void AppendTypeParameterOrdinalData(int declaringSymbolRefId, int ordinal)
            {
                AppendType(SymbolKeyType.TypeParameterOrdinal);
                AppendSymbolReference(declaringSymbolRefId);
                AppendInt(ordinal);
            }
        }
    }
}
