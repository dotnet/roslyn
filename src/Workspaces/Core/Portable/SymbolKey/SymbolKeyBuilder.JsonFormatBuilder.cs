// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial class SymbolKeyBuilder
    {
        private class JsonFormatBuilder : SymbolKeyBuilder
        {
            private int _indentLevel;
            private bool _atListStart;
            private bool _needIndent;

            protected override void Reset()
            {
                base.Reset();
                _indentLevel = 0;
                _atListStart = false;
            }

            private void AppendIndentIfNeeded()
            {
                if (_needIndent)
                {
                    _builder.Append(' ', _indentLevel * 2);
                }

                _needIndent = false;
            }

            private void AppendLineBreak()
            {
                if (!_atListStart)
                {
                    _builder.Append(',');
                }

                _builder.AppendLine();
                _needIndent = true;
            }

            private void Append(bool value)
            {
                AppendIndentIfNeeded();
                _builder.Append(value);
            }

            private void Append(char value)
            {
                AppendIndentIfNeeded();
                _builder.Append(value);
            }

            private void Append(int value)
            {
                AppendIndentIfNeeded();
                _builder.Append(value);
            }

            private void Append(string value)
            {
                AppendIndentIfNeeded();
                _builder.Append(value);
            }

            private void AppendName(string name)
            {
                if (!_atListStart)
                {
                    _builder.Append(',');
                    AppendLineBreak();
                }

                AppendIndentIfNeeded();
                _builder.Append("\"" + name + "\": ");
                _atListStart = false;
            }

            private void AppendNameValue(string name, bool value)
            {
                AppendName(name);
                _builder.Append(value ? "true" : "false");
                _atListStart = false;
            }

            private void AppendNameValue(string name, int value)
            {
                AppendName(name);
                _builder.Append(value);
                _atListStart = false;
            }

            private void AppendNameValue(string name, string value)
            {
                AppendName(name);
                _builder.Append("\"" + value.Replace("\"", "\"\"") + "\"");
                _atListStart = false;
            }

            private void AppendNameValue(string name, ISymbol symbol)
            {
                AppendName(name);
                AppendSymbol(symbol);
                _atListStart = false;
            }

            private void AppendNameValue(string name, Location location)
            {
                AppendName(name);
                AppendLocation(location);
                _atListStart = false;
            }

            private void AppendNameValue(string name, in ImmutableArray<IParameterSymbol> parameters)
            {
                AppendName(name);
                AppendArrayStart();

                foreach (var parameter in parameters)
                {
                    AppendListStart('{');
                    AppendNameValue("refKind", parameter.RefKind.ToString());
                    AppendNameValue("type", parameter.Type);
                    AppendListEnd('}');
                }

                AppendArrayEnd();
                _atListStart = false;
            }

            protected override void AppendNull()
            {
                _builder.Append("null");
            }

            private void AppendListStart(char startChar)
            {
                Append(startChar);
                _atListStart = true;
                AppendLineBreak();
                _indentLevel++;
            }

            private void AppendListEnd(char endChar)
            {
                Debug.Assert(_indentLevel > 0);
                _indentLevel--;

                if (!_needIndent)
                {
                    AppendLineBreak();
                }

                Append(endChar);
            }

            protected override void AppendSymbolStart(ISymbol symbol, int symbolRefId)
            {
                AppendListStart('{');
                AppendNameValue("id", symbolRefId);
            }

            protected override void AppendSymbolEnd(ISymbol symbol, int symbolRefId)
            {
                AppendListEnd('}');
            }

            protected override void AppendSymbolReference(int symbolRefId)
            {
                Append('#');
                Append(symbolRefId);
            }

            protected override void AppendArray<T1, T2>(in ImmutableArray<T1> array, Action<T2> writeValue)
            {
                if (array.IsDefault)
                {
                    AppendNull();
                    return;
                }

                AppendArrayStart();

                foreach (var value in array)
                {
                    writeValue(value);
                }

                AppendArrayEnd();
            }

            private void AppendArrayStart()
            {
                AppendListStart('[');
            }

            private void AppendArrayEnd()
            {
                AppendListEnd(']');
            }

            protected override void AppendAliasData(string name, INamespaceOrTypeSymbol target, string filePath)
            {
                AppendNameValue("kind", "alias");
                AppendNameValue(nameof(name), name);
                AppendNameValue(nameof(target), target);
                AppendNameValue(nameof(filePath), filePath);
            }

            protected override void AppendAnonymousDelegateTypeOrFunctionData(bool isAnonymousDelegateType, Location location)
            {
                AppendNameValue("kind", "anonymousDelegateTypeOrFunction");
                AppendNameValue(nameof(isAnonymousDelegateType), isAnonymousDelegateType);
                AppendNameValue(nameof(location), location);
            }

            protected override void AppendAnonymousTypeData(ImmutableArray<ITypeSymbol> propertyTypes, ImmutableArray<string> propertyNames, ImmutableArray<bool> propertyIsReadOnly, ImmutableArray<Location> propertyLocations)
            {
                var length = propertyTypes.Length;

                Debug.Assert(propertyTypes.Length == length &&
                             propertyNames.Length == length &&
                             propertyIsReadOnly.Length == length &&
                             propertyLocations.Length == length);

                AppendNameValue("kind", "anonymousType");

                AppendName("properties");
                AppendArrayStart();

                for (var i = 0; i < length; i++)
                {
                    AppendListStart('{');
                    AppendNameValue("name", propertyNames[i]);
                    AppendNameValue("type", propertyTypes[i]);
                    AppendNameValue("type", propertyIsReadOnly[i]);
                    AppendNameValue("location", propertyLocations[i]);
                    AppendListEnd('}');
                }

                AppendArrayEnd();
            }

            protected override void AppendArrayTypeData(ITypeSymbol elementType, int rank)
            {
                AppendNameValue("kind", "arrayType");
                AppendNameValue(nameof(elementType), elementType);
                AppendNameValue(nameof(rank), rank);
            }

            protected override void AppendAssemblyData(string assemblyName)
            {
                AppendNameValue("kind", "assembly");
                AppendNameValue(nameof(assemblyName), assemblyName);
            }

            protected override void AppendBodyLevelData(string localName, ISymbol containingSymbol, int ordinal, SymbolKind kind)
            {
                AppendNameValue("kind", "bodyLevel");
                AppendNameValue(nameof(localName), localName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
                AppendNameValue(nameof(ordinal), ordinal);
                AppendNameValue(nameof(kind), kind.ToString());
            }

            protected override void AppendConstructedMethodData(IMethodSymbol constructedFrom, ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendNameValue("kind", "constructedMethod");
                AppendNameValue(nameof(constructedFrom), constructedFrom);
                AppendName(nameof(typeArguments));
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendDynamicTypeData()
            {
                AppendNameValue("kind", "dynamic");
            }

            protected override void AppendErrorTupleTypeData(ImmutableArray<ISymbol> elementTypes, ImmutableArray<string> friendlyNames, ImmutableArray<Location> locations)
            {
                Debug.Assert(elementTypes.Length == friendlyNames.Length &&
                             elementTypes.Length == locations.Length);

                AppendNameValue("kind", "tupleType");

                AppendName("elements");
                AppendArrayStart();

                var length = elementTypes.Length;

                for (var i = 0; i < length; i++)
                {
                    AppendListStart('{');
                    AppendNameValue("name", friendlyNames[i]);
                    AppendNameValue("type", elementTypes[i]);
                    AppendNameValue("location", locations[i]);
                    AppendListEnd('}');
                }

                AppendArrayEnd();
            }

            protected override void AppendErrorTypeData(string name, INamespaceOrTypeSymbol containingSymbol, int arity, ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendNameValue("kind", "errorType");
                AppendNameValue(nameof(name), name);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
                AppendNameValue(nameof(arity), arity);
                AppendName(nameof(typeArguments));
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendEventData(string metadataName, INamedTypeSymbol containingType)
            {
                AppendNameValue("kind", "event");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingType), containingType);
            }

            protected override void AppendFieldData(string metadataName, INamedTypeSymbol containingType)
            {
                AppendNameValue("kind", "field");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingType), containingType);
            }

            protected override void AppendLocationData(Location location)
            {
                Debug.Assert(location.Kind == LocationKind.None ||
                             location.Kind == LocationKind.SourceFile ||
                             location.Kind == LocationKind.MetadataFile);

                AppendListStart('{');
                AppendNameValue("kind", location.Kind.ToString());

                if (location.Kind == LocationKind.SourceFile)
                {
                    AppendNameValue("filePath", location.SourceTree.FilePath);
                    AppendNameValue("start", location.SourceSpan.Start);
                    AppendNameValue("length", location.SourceSpan.Length);
                }
                else if (location.Kind == LocationKind.MetadataFile)
                {
                    AppendNameValue("containingAssembly", location.MetadataModule.ContainingAssembly);
                    AppendNameValue("metadataName", location.MetadataModule.MetadataName);
                }

                AppendListEnd('}');
            }

            protected override void AppendMethodData(string metadataName, ISymbol containingSymbol, int arity, bool isPartialMethodImplementationPart, ImmutableArray<IParameterSymbol> parameters, ITypeSymbol returnType)
            {
                AppendNameValue("kind", "method");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
                AppendNameValue(nameof(arity), arity);
                AppendNameValue(nameof(isPartialMethodImplementationPart), isPartialMethodImplementationPart);
                AppendNameValue(nameof(parameters), parameters);
                AppendNameValue(nameof(returnType), returnType);
            }

            protected override void AppendModuleData(ISymbol containingSymbol)
            {
                AppendNameValue("kind", "module");
                AppendNameValue(nameof(containingSymbol), containingSymbol);
            }

            protected override void AppendNamedTypeData(string metadataName, ISymbol containingSymbol, int arity, TypeKind typeKind, bool isUnboundGenericType, ImmutableArray<ITypeSymbol> typeArguments)
            {
                AppendNameValue("kind", "namedType");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
                AppendNameValue(nameof(arity), arity);
                AppendNameValue(nameof(typeKind), typeKind.ToString());
                AppendNameValue(nameof(isUnboundGenericType), isUnboundGenericType);
                AppendName(nameof(typeArguments));
                AppendSymbolArray(typeArguments);
            }

            protected override void AppendNamespaceData(string metadataName, bool isCompilationGlobalNamespace, ISymbol containingSymbol)
            {
                AppendNameValue("kind", "namespace");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(isCompilationGlobalNamespace), isCompilationGlobalNamespace);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
            }

            protected override void AppendParameterData(string metadataName, ISymbol containingSymbol)
            {
                AppendNameValue("kind", "parameter");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
            }

            protected override void AppendPointerTypeData(ITypeSymbol pointedAtType)
            {
                AppendNameValue("kind", "pointerType");
                AppendNameValue(nameof(pointedAtType), pointedAtType);
            }

            protected override void AppendPropertyData(string metadataName, ISymbol containingSymbol, bool isIndexer, ImmutableArray<IParameterSymbol> parameters)
            {
                AppendNameValue("kind", "property");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
                AppendNameValue(nameof(isIndexer), isIndexer);
                AppendNameValue(nameof(parameters), parameters);
            }

            protected override void AppendReducedExtensionMethodData(IMethodSymbol reducedFrom, ITypeSymbol receiverType)
            {
                AppendNameValue("kind", "reducedExtensionMethod");
                AppendNameValue(nameof(reducedFrom), reducedFrom);
                AppendNameValue(nameof(receiverType), receiverType);
            }

            protected override void AppendTupleTypeData(INamedTypeSymbol tupleUnderlyingType, ImmutableArray<string> friendlyNames, ImmutableArray<Location> locations)
            {
                Debug.Assert(friendlyNames.Length == locations.Length);

                AppendNameValue("kind", "tupleType");

                AppendNameValue(nameof(tupleUnderlyingType), tupleUnderlyingType);

                AppendName("elements");
                AppendArrayStart();

                var length = friendlyNames.Length;

                for (var i = 0; i < length; i++)
                {
                    AppendListStart('{');
                    AppendNameValue("name", friendlyNames[i]);
                    AppendNameValue("location", locations[i]);
                    AppendListEnd('}');
                }

                AppendArrayEnd();
            }

            protected override void AppendTypeParameterData(string metadataName, ISymbol containingSymbol)
            {
                AppendNameValue("kind", "typeParameter");
                AppendNameValue(nameof(metadataName), metadataName);
                AppendNameValue(nameof(containingSymbol), containingSymbol);
            }

            protected override void AppendTypeParameterOrdinalData(int declaringSymbolRefId, int ordinal)
            {
                AppendNameValue("kind", "parameter");
                AppendName(nameof(declaringSymbolRefId));
                AppendSymbolReference(declaringSymbolRefId);
                AppendNameValue(nameof(ordinal), ordinal);
            }
        }
    }
}
