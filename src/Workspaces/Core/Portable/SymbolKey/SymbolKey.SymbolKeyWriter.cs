// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
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
            ReducedExtensionMethod = 'X',
            TypeParameter = 'Y',
            AnonymousType = 'Z',

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
            private readonly Action<IParameterSymbol> _writeParameterType;
            private readonly Action<IParameterSymbol> _writeRefKind;

            private readonly Dictionary<ISymbol, int> _symbolToId = new Dictionary<ISymbol, int>();
            private readonly StringBuilder _stringBuilder = new StringBuilder();

            public Compilation Compilation { get; private set; }
            public CancellationToken CancellationToken { get; private set; }
            public bool WritingSignature;

            internal int _nestingCount;
            private int _nextId;

            private SymbolKeyWriter()
            {
                _writeSymbolKey = WriteSymbolKey;
                _writeString = WriteString;
                _writeParameterType = p => WriteSymbolKey(p.Type);
                _writeRefKind = p => WriteInteger((int)p.RefKind);
            }

            public void Dispose()
            {
                _symbolToId.Clear();
                _stringBuilder.Clear();
                Compilation = null;
                CancellationToken = default(CancellationToken);
                _nestingCount = 0;
                _nextId = 0;

                // Place us back in the pool for future use.
                s_writerPool.Free(this);
            }

            public static SymbolKeyWriter GetWriter(Compilation compilation, CancellationToken cancellationToken)
            {
                var visitor = s_writerPool.Allocate();
                visitor.Initialize(compilation, cancellationToken);
                return visitor;
            }

            private void Initialize(Compilation compilation, CancellationToken cancellationToken)
            {
                Compilation = compilation;
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
                WriteSymbolKey(symbol, first: false);
            }

            internal void WriteFirstSymbolKey(ISymbol symbol)
            {
                WriteSymbolKey(symbol, first: true);
            }

            private void WriteSymbolKey(ISymbol symbol, bool first)
            {
                if (!first)
                {
                    WriteSpace();
                }

                if (symbol == null)
                {
                    WriteType(SymbolKeyType.Null);
                    return;
                }

                var shouldWriteOrdinal = ShouldWriteTypeParameterOrdinal(symbol);
                int id;
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
                    //      Foo<Z>(List<Z> list)
                    //
                    // If we start with the symbol for "list" then we'll see the following
                    // chain of symbols hit:
                    //
                    //      List<Z>     
                    //          Z
                    //              Foo<Z>(List<Z>)
                    //                  List<Z>
                    //
                    // The recursion is prevented because when we hit 'Foo' we mark that
                    // we're writing out a signature.  And, in signature mode we only write
                    // out the ordinal for 'Z' without recursing.  However, even though
                    // we prevent the recursion, we still hit List<Z> twice.  After writing
                    // the innermost one out, we'll give it a reference ID.  When we
                    // then hit the outermost one, we want to just reuse that one.
                    int existingId;
                    if (_symbolToId.TryGetValue(symbol, out existingId))
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

            internal void WriteInteger(int value)
            {
                WriteSpace();
                _stringBuilder.Append(value);
            }

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

            internal void WriteRefKindArray(ImmutableArray<IParameterSymbol> values)
            {
                WriteArray(values, _writeRefKind);
            }

            private void WriteArray<T, U>(ImmutableArray<T> array, Action<U> writeValue)
                where T : U
            {
                WriteSpace();
                if (array.IsDefault)
                {
                    WriteType(SymbolKeyType.Null);
                    return;
                }

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
                else if (methodSymbol.MethodKind == MethodKind.ReducedExtension)
                {
                    WriteType(SymbolKeyType.ReducedExtensionMethod);
                    ReducedExtensionMethodSymbolKey.Create(methodSymbol, this);
                }
                else
                {
                    WriteType(SymbolKeyType.Method);
                    MethodSymbolKey.Create(methodSymbol, this);
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
                    WriteType(SymbolKeyType.AnonymousType);
                    AnonymousTypeSymbolKey.Create(namedTypeSymbol, this);
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
                // helps prevent recursion problems in cases like "Foo<T>(T t).
                if (ShouldWriteTypeParameterOrdinal(typeParameterSymbol))
                {
                    WriteType(SymbolKeyType.TypeParameterOrdinal);
                    TypeParameterOrdinalSymbolKey.Create(typeParameterSymbol, this);
                }
                else
                {
                    WriteType(SymbolKeyType.TypeParameter);
                    TypeParameterSymbolKey.Create(typeParameterSymbol, this);
                }
                return null;
            }

            private bool ShouldWriteTypeParameterOrdinal(ISymbol symbol)
            {
                return WritingSignature &&
                    symbol.Kind == SymbolKind.TypeParameter &&
                    ((ITypeParameterSymbol)symbol).TypeParameterKind != TypeParameterKind.Type;
            }
        }
    }
}