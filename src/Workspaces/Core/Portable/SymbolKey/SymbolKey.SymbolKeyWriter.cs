// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private enum SymbolKeyType
        {
            Alias = 'A',
            BodyLevel = 'B',
            ConstructedMethod = 'C',
            NamedType = 'D',
            ErrorType = 'E',
            Field = 'F',
            DynamicType = 'I',
            Method = 'M',
            Namespace = 'N',
            PointerType = 'O',
            Parameter = 'P',
            Property = 'Q',
            ArrayType = 'R',
            Assembly = 'S',
            TupleType = 'T',
            Module = 'U',
            Event = 'V',
            AnonymousType = 'W',
            ReducedExtensionMethod = 'X',
            TypeParameter = 'Y',
            AnonymousFunctionOrDelegate = 'Z',

            // Not to be confused with ArrayType.  This indicates an array of elements in the stream.
            Array = '%',
            Reference = '#',
            Null = '!',
            TypeParameterOrdinal = '@',
        }

        private class SymbolKeyWriter : SymbolVisitor<object>, IDisposable
        {
            private static readonly ObjectPool<SymbolKeyWriter> s_writerPool =
                new ObjectPool<SymbolKeyWriter>(() => new SymbolKeyWriter());

            private readonly Action<ISymbol> _writeSymbolKey;
            private readonly Action<string> _writeString;
            private readonly Action<Location> _writeLocation;
            private readonly Action<bool> _writeBoolean;
            private readonly Action<IParameterSymbol> _writeParameterType;
            private readonly Action<IParameterSymbol> _writeRefKind;

            private readonly Dictionary<ISymbol, int> _symbolToId = new Dictionary<ISymbol, int>();
            private readonly StringBuilder _stringBuilder = new StringBuilder();

            public CancellationToken CancellationToken { get; private set; }

            private readonly List<IMethodSymbol> _methodSymbolStack = new List<IMethodSymbol>();

            internal int _nestingCount;
            private int _nextId;

            private SymbolKeyWriter()
            {
                _writeSymbolKey = WriteSymbolKey;
                _writeString = WriteString;
                _writeLocation = WriteLocation;
                _writeBoolean = WriteBoolean;
                _writeParameterType = p => WriteSymbolKey(p.Type);
                _writeRefKind = p => WriteInteger((int)p.RefKind);
            }

            public void Dispose()
            {
                _symbolToId.Clear();
                _stringBuilder.Clear();
                _methodSymbolStack.Clear();
                CancellationToken = default;
                _nestingCount = 0;
                _nextId = 0;

                // Place us back in the pool for future use.
                s_writerPool.Free(this);
            }

            public static SymbolKeyWriter GetWriter(CancellationToken cancellationToken)
            {
                var visitor = s_writerPool.Allocate();
                visitor.Initialize(cancellationToken);
                return visitor;
            }

            private void Initialize(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
            }

            public string CreateKey()
            {
                Debug.Assert(_nestingCount == 0);
                return _stringBuilder.ToString();
            }

            private void StartKey()
            {
                _stringBuilder.Append('(');
                _nestingCount++;
            }

            private void WriteType(SymbolKeyType type)
            {
                _stringBuilder.Append((char)type);
            }

            private void EndKey()
            {
                _nestingCount--;
                _stringBuilder.Append(')');
            }

            internal void WriteSymbolKey(ISymbol symbol)
            {
                WriteSpace();

                if (symbol == null)
                {
                    WriteType(SymbolKeyType.Null);
                    return;
                }

                int id;
                var shouldWriteOrdinal = ShouldWriteTypeParameterOrdinal(symbol, out _);
                if (!shouldWriteOrdinal)
                {
                    if (_symbolToId.TryGetValue(symbol, out id))
                    {
                        StartKey();
                        WriteType(SymbolKeyType.Reference);
                        WriteInteger(id);
                        EndKey();
                        return;
                    }
                }

                id = _nextId;
                _nextId++;

                StartKey();
                symbol.Accept(this);

                if (!shouldWriteOrdinal)
                {
                    // Note: it is possible in some situations to hit the same symbol 
                    // multiple times.  For example, if you have:
                    //
                    //      Goo<Z>(List<Z> list)
                    //
                    // If we start with the symbol for "list" then we'll see the following
                    // chain of symbols hit:
                    //
                    //      List<Z>     
                    //          Z
                    //              Goo<Z>(List<Z>)
                    //                  List<Z>
                    //
                    // The recursion is prevented because when we hit 'Goo' we mark that
                    // we're writing out a signature.  And, in signature mode we only write
                    // out the ordinal for 'Z' without recursing.  However, even though
                    // we prevent the recursion, we still hit List<Z> twice.  After writing
                    // the innermost one out, we'll give it a reference ID.  When we
                    // then hit the outermost one, we want to just reuse that one.
                    if (_symbolToId.TryGetValue(symbol, out var existingId))
                    {
                        // While we recursed, we already hit this symbol.  Use its ID as our
                        // ID.
                        id = existingId;
                    }
                    else
                    {
                        // Haven't hit this symbol before, write out its fresh ID.
                        _symbolToId.Add(symbol, id);
                    }
                }

                // Now write out the ID for this symbol so that any future hits of it can 
                // write out a reference to it instead.
                WriteInteger(id);

                EndKey();
            }

            private void WriteSpace()
            {
                _stringBuilder.Append(' ');
            }

            internal void WriteFormatVersion(int version)
                => WriteIntegerRaw_DoNotCallDirectly(version);

            internal void WriteInteger(int value)
            {
                WriteSpace();
                WriteIntegerRaw_DoNotCallDirectly(value);
            }

            private void WriteIntegerRaw_DoNotCallDirectly(int value)
                => _stringBuilder.Append(value.ToString(CultureInfo.InvariantCulture));

            internal void WriteBoolean(bool value)
            {
                WriteInteger(value ? 1 : 0);
            }

            internal void WriteString(string value)
            {
                // Strings are quoted, with all embedded quotes being doubled to escape them.
                WriteSpace();
                if (value == null)
                {
                    WriteType(SymbolKeyType.Null);
                }
                else
                {
                    _stringBuilder.Append('"');
                    _stringBuilder.Append(value.Replace("\"", "\"\""));
                    _stringBuilder.Append('"');
                }
            }

            internal void WriteLocation(Location location)
            {
                WriteSpace();
                if (location == null)
                {
                    WriteType(SymbolKeyType.Null);
                    return;
                }

                Debug.Assert(location.Kind == LocationKind.None ||
                             location.Kind == LocationKind.SourceFile ||
                             location.Kind == LocationKind.MetadataFile);

                WriteInteger((int)location.Kind);
                if (location.Kind == LocationKind.SourceFile)
                {
                    WriteString(location.SourceTree.FilePath);
                    WriteInteger(location.SourceSpan.Start);
                    WriteInteger(location.SourceSpan.Length);
                }
                else if (location.Kind == LocationKind.MetadataFile)
                {
                    WriteSymbolKey(location.MetadataModule.ContainingAssembly);
                    WriteString(location.MetadataModule.MetadataName);
                }
            }

            /// <summary>
            /// Writes out the provided symbols to the key.  The array provided must not
            /// be <c>default</c>.
            /// </summary>
            internal void WriteSymbolKeyArray<TSymbol>(ImmutableArray<TSymbol> symbols)
                where TSymbol : ISymbol
            {
                WriteArray(symbols, _writeSymbolKey);
            }

            internal void WriteParameterTypesArray(ImmutableArray<IParameterSymbol> symbols)
            {
                WriteArray(symbols, _writeParameterType);
            }

            internal void WriteStringArray(ImmutableArray<string> strings)
            {
                WriteArray(strings, _writeString);
            }

            internal void WriteBooleanArray(ImmutableArray<bool> array)
            {
                WriteArray(array, _writeBoolean);
            }

            internal void WriteLocationArray(ImmutableArray<Location> array)
            {
                WriteArray(array, _writeLocation);
            }

            internal void WriteRefKindArray(ImmutableArray<IParameterSymbol> values)
            {
                WriteArray(values, _writeRefKind);
            }

            private void WriteArray<T, U>(ImmutableArray<T> array, Action<U> writeValue)
                where T : U
            {
                WriteSpace();
                Debug.Assert(!array.IsDefault);

                StartKey();
                WriteType(SymbolKeyType.Array);

                WriteInteger(array.Length);
                foreach (var value in array)
                {
                    writeValue(value);
                }

                EndKey();
            }

            public override object VisitAlias(IAliasSymbol aliasSymbol)
            {
                WriteType(SymbolKeyType.Alias);
                AliasSymbolKey.Create(aliasSymbol, this);
                return null;
            }

            public override object VisitArrayType(IArrayTypeSymbol arrayTypeSymbol)
            {
                WriteType(SymbolKeyType.ArrayType);
                ArrayTypeSymbolKey.Create(arrayTypeSymbol, this);
                return null;
            }

            public override object VisitAssembly(IAssemblySymbol assemblySymbol)
            {
                WriteType(SymbolKeyType.Assembly);
                AssemblySymbolKey.Create(assemblySymbol, this);
                return null;
            }

            public override object VisitDynamicType(IDynamicTypeSymbol dynamicTypeSymbol)
            {
                WriteType(SymbolKeyType.DynamicType);
                DynamicTypeSymbolKey.Create(this);
                return null;
            }

            public override object VisitField(IFieldSymbol fieldSymbol)
            {
                WriteType(SymbolKeyType.Field);
                FieldSymbolKey.Create(fieldSymbol, this);
                return null;
            }

            public override object VisitLabel(ILabelSymbol labelSymbol)
            {
                WriteType(SymbolKeyType.BodyLevel);
                BodyLevelSymbolKey.Create(labelSymbol, this);
                return null;
            }

            public override object VisitLocal(ILocalSymbol localSymbol)
            {
                WriteType(SymbolKeyType.BodyLevel);
                BodyLevelSymbolKey.Create(localSymbol, this);
                return null;
            }

            public override object VisitRangeVariable(IRangeVariableSymbol rangeVariableSymbol)
            {
                WriteType(SymbolKeyType.BodyLevel);
                BodyLevelSymbolKey.Create(rangeVariableSymbol, this);
                return null;
            }

            public override object VisitMethod(IMethodSymbol methodSymbol)
            {
                if (!methodSymbol.Equals(methodSymbol.ConstructedFrom))
                {
                    WriteType(SymbolKeyType.ConstructedMethod);
                    ConstructedMethodSymbolKey.Create(methodSymbol, this);
                }
                else
                {
                    switch (methodSymbol.MethodKind)
                    {
                        case MethodKind.ReducedExtension:
                            WriteType(SymbolKeyType.ReducedExtensionMethod);
                            ReducedExtensionMethodSymbolKey.Create(methodSymbol, this);
                            break;

                        case MethodKind.AnonymousFunction:
                            WriteType(SymbolKeyType.AnonymousFunctionOrDelegate);
                            AnonymousFunctionOrDelegateSymbolKey.Create(methodSymbol, this);
                            break;

                        case MethodKind.LocalFunction:
                            WriteType(SymbolKeyType.BodyLevel);
                            BodyLevelSymbolKey.Create(methodSymbol, this);
                            break;

                        default:
                            WriteType(SymbolKeyType.Method);
                            MethodSymbolKey.Create(methodSymbol, this);
                            break;
                    }
                }

                return null;
            }

            public override object VisitModule(IModuleSymbol moduleSymbol)
            {
                WriteType(SymbolKeyType.Module);
                ModuleSymbolKey.Create(moduleSymbol, this);
                return null;
            }

            public override object VisitNamedType(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Error)
                {
                    WriteType(SymbolKeyType.ErrorType);
                    ErrorTypeSymbolKey.Create(namedTypeSymbol, this);
                }
                else if (namedTypeSymbol.IsTupleType)
                {
                    WriteType(SymbolKeyType.TupleType);
                    TupleTypeSymbolKey.Create(namedTypeSymbol, this);
                }
                else if (namedTypeSymbol.IsAnonymousType)
                {
                    if (namedTypeSymbol.IsAnonymousDelegateType())
                    {
                        WriteType(SymbolKeyType.AnonymousFunctionOrDelegate);
                        AnonymousFunctionOrDelegateSymbolKey.Create(namedTypeSymbol, this);
                    }
                    else
                    {
                        WriteType(SymbolKeyType.AnonymousType);
                        AnonymousTypeSymbolKey.Create(namedTypeSymbol, this);
                    }
                }
                else
                {
                    WriteType(SymbolKeyType.NamedType);
                    NamedTypeSymbolKey.Create(namedTypeSymbol, this);
                }

                return null;
            }

            public override object VisitNamespace(INamespaceSymbol namespaceSymbol)
            {
                WriteType(SymbolKeyType.Namespace);
                NamespaceSymbolKey.Create(namespaceSymbol, this);
                return null;
            }

            public override object VisitParameter(IParameterSymbol parameterSymbol)
            {
                WriteType(SymbolKeyType.Parameter);
                ParameterSymbolKey.Create(parameterSymbol, this);
                return null;
            }

            public override object VisitPointerType(IPointerTypeSymbol pointerTypeSymbol)
            {
                WriteType(SymbolKeyType.PointerType);
                PointerTypeSymbolKey.Create(pointerTypeSymbol, this);
                return null;
            }

            public override object VisitProperty(IPropertySymbol propertySymbol)
            {
                WriteType(SymbolKeyType.Property);
                PropertySymbolKey.Create(propertySymbol, this);
                return null;
            }

            public override object VisitEvent(IEventSymbol eventSymbol)
            {
                WriteType(SymbolKeyType.Event);
                EventSymbolKey.Create(eventSymbol, this);
                return null;
            }

            public override object VisitTypeParameter(ITypeParameterSymbol typeParameterSymbol)
            {
                // If it's a reference to a method type parameter, and we're currently writing
                // out a signture, then only write out the ordinal of type parameter.  This 
                // helps prevent recursion problems in cases like "Goo<T>(T t).
                if (ShouldWriteTypeParameterOrdinal(typeParameterSymbol, out var methodIndex))
                {
                    WriteType(SymbolKeyType.TypeParameterOrdinal);
                    TypeParameterOrdinalSymbolKey.Create(typeParameterSymbol, methodIndex, this);
                }
                else
                {
                    WriteType(SymbolKeyType.TypeParameter);
                    TypeParameterSymbolKey.Create(typeParameterSymbol, this);
                }
                return null;
            }

            public bool ShouldWriteTypeParameterOrdinal(ISymbol symbol, out int methodIndex)
            {
                if (symbol.Kind == SymbolKind.TypeParameter)
                {
                    var typeParameter = (ITypeParameterSymbol)symbol;
                    if (typeParameter.TypeParameterKind == TypeParameterKind.Method)
                    {
                        for (int i = 0, n = _methodSymbolStack.Count; i < n; i++)
                        {
                            var method = _methodSymbolStack[i];
                            if (typeParameter.DeclaringMethod.Equals(method))
                            {
                                methodIndex = i;
                                return true;
                            }
                        }
                    }
                }

                methodIndex = -1;
                return false;
            }

            public void PushMethod(IMethodSymbol method)
                => _methodSymbolStack.Add(method);

            public void PopMethod(IMethodSymbol method)
            {
                Contract.ThrowIfTrue(_methodSymbolStack.Count == 0);
                Contract.ThrowIfFalse(method.Equals(_methodSymbolStack[_methodSymbolStack.Count - 1]));
                _methodSymbolStack.RemoveAt(_methodSymbolStack.Count - 1);
            }
        }
    }
}
