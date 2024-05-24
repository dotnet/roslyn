// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis;

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
        FunctionPointer = 'G',
        DynamicType = 'I',
        BuiltinOperator = 'L',
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

    private class SymbolKeyWriter : SymbolVisitor, IDisposable
    {
        private static readonly ObjectPool<SymbolKeyWriter> s_writerPool = SharedPools.Default<SymbolKeyWriter>();

        private readonly Action<ISymbol> _writeSymbolKey;
        private readonly Action<string?> _writeString;
        private readonly Action<Location?> _writeLocation;
        private readonly Action<bool> _writeBoolean;
        private readonly Action<IParameterSymbol> _writeParameterType;
        private readonly Action<IParameterSymbol> _writeRefKind;

        private readonly Dictionary<ISymbol, int> _symbolToId = [];
        private readonly StringBuilder _stringBuilder = new();

        public CancellationToken CancellationToken { get; private set; }

        private readonly List<IMethodSymbol> _methodSymbolStack = [];

        internal int _nestingCount;
        private int _nextId;

        public SymbolKeyWriter()
        {
            _writeSymbolKey = WriteSymbolKey;
            _writeString = WriteString;
            _writeLocation = WriteLocation;
            _writeBoolean = WriteBoolean;
            _writeParameterType = p => WriteSymbolKey(p.Type);
            _writeRefKind = p => WriteRefKind(p.RefKind);
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
            => CancellationToken = cancellationToken;

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
            => _stringBuilder.Append((char)type);

        private void EndKey()
        {
            _nestingCount--;
            _stringBuilder.Append(')');
        }

        internal void WriteSymbolKey(ISymbol? symbol)
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
            if (IsBodyLevelSymbol(symbol))
            {
                WriteType(SymbolKeyType.BodyLevel);
                BodyLevelSymbolKey.Create(symbol, this);
            }
            else
            {
                symbol.Accept(this);
            }

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
            => _stringBuilder.Append(' ');

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
            => WriteInteger(value ? 1 : 0);

        internal void WriteString(string? value)
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

        internal void WriteLocation(Location? location)
        {
            WriteSpace();
            if (location == null)
            {
                WriteType(SymbolKeyType.Null);
                return;
            }

            Debug.Assert(location.Kind is LocationKind.None or
                         LocationKind.SourceFile or
                         LocationKind.MetadataFile);

            WriteInteger((int)location.Kind);
            if (location.IsInSource)
            {
                WriteString(location.SourceTree.FilePath);
                WriteInteger(location.SourceSpan.Start);
                WriteInteger(location.SourceSpan.Length);
            }
            else if (location.Kind == LocationKind.MetadataFile)
            {
                WriteSymbolKey(location.MetadataModule!.ContainingAssembly);
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
            => WriteArray(symbols, _writeParameterType);

        internal void WriteBooleanArray(ImmutableArray<bool> array)
            => WriteArray(array, _writeBoolean);

        // annotating WriteStringArray and WriteLocationArray as allowing null elements
        // then causes issues where we can't pass ImmutableArrays of non-null elements

#nullable disable

        internal void WriteStringArray(ImmutableArray<string> strings)
            => WriteArray(strings, _writeString);

        internal void WriteLocationArray(ImmutableArray<Location> array)
            => WriteArray(array, _writeLocation);

#nullable enable

        internal void WriteRefKindArray(ImmutableArray<IParameterSymbol> values)
            => WriteArray(values, _writeRefKind);

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

        internal void WriteRefKind(RefKind refKind) => WriteInteger((int)refKind);

        public override void VisitAlias(IAliasSymbol aliasSymbol)
        {
            WriteType(SymbolKeyType.Alias);
            AliasSymbolKey.Instance.Create(aliasSymbol, this);
        }

        public override void VisitArrayType(IArrayTypeSymbol arrayTypeSymbol)
        {
            WriteType(SymbolKeyType.ArrayType);
            ArrayTypeSymbolKey.Instance.Create(arrayTypeSymbol, this);
        }

        public override void VisitAssembly(IAssemblySymbol assemblySymbol)
        {
            WriteType(SymbolKeyType.Assembly);
            AssemblySymbolKey.Instance.Create(assemblySymbol, this);
        }

        public override void VisitDynamicType(IDynamicTypeSymbol dynamicTypeSymbol)
        {
            WriteType(SymbolKeyType.DynamicType);
            DynamicTypeSymbolKey.Instance.Create(dynamicTypeSymbol, this);
        }

        public override void VisitField(IFieldSymbol fieldSymbol)
        {
            WriteType(SymbolKeyType.Field);
            FieldSymbolKey.Instance.Create(fieldSymbol, this);
        }

        public override void VisitLabel(ILabelSymbol labelSymbol)
            => throw ExceptionUtilities.Unreachable();

        public override void VisitLocal(ILocalSymbol localSymbol)
            => throw ExceptionUtilities.Unreachable();

        public override void VisitRangeVariable(IRangeVariableSymbol rangeVariableSymbol)
            => throw ExceptionUtilities.Unreachable();

        public override void VisitMethod(IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.Equals(methodSymbol.ConstructedFrom))
            {
                WriteType(SymbolKeyType.ConstructedMethod);
                ConstructedMethodSymbolKey.Instance.Create(methodSymbol, this);
            }
            else
            {
                switch (methodSymbol.MethodKind)
                {
                    case MethodKind.AnonymousFunction:
                        WriteType(SymbolKeyType.AnonymousFunctionOrDelegate);
                        AnonymousFunctionOrDelegateSymbolKey.Create(methodSymbol, this);
                        break;

                    case MethodKind.BuiltinOperator:
                        WriteType(SymbolKeyType.BuiltinOperator);
                        BuiltinOperatorSymbolKey.Instance.Create(methodSymbol, this);
                        break;

                    case MethodKind.ReducedExtension:
                        WriteType(SymbolKeyType.ReducedExtensionMethod);
                        ReducedExtensionMethodSymbolKey.Instance.Create(methodSymbol, this);
                        break;

                    case MethodKind.LocalFunction:
                        throw ExceptionUtilities.Unreachable();

                    default:
                        WriteType(SymbolKeyType.Method);
                        MethodSymbolKey.Instance.Create(methodSymbol, this);
                        break;
                }
            }
        }

        public override void VisitModule(IModuleSymbol moduleSymbol)
        {
            WriteType(SymbolKeyType.Module);
            ModuleSymbolKey.Instance.Create(moduleSymbol, this);
        }

        public override void VisitNamedType(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.TypeKind == TypeKind.Error)
            {
                WriteType(SymbolKeyType.ErrorType);
                ErrorTypeSymbolKey.Instance.Create(namedTypeSymbol, this);
            }
            else if (namedTypeSymbol.IsTupleType && namedTypeSymbol.TupleUnderlyingType is INamedTypeSymbol underlyingType && underlyingType != namedTypeSymbol)
            {
                // A tuple is a named type with some added information
                // We only need to store this extra information if there is some
                // (ie. the current type differs from the underlying type, which has no element names)
                WriteType(SymbolKeyType.TupleType);
                TupleTypeSymbolKey.Instance.Create(namedTypeSymbol, this);
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
                    AnonymousTypeSymbolKey.Instance.Create(namedTypeSymbol, this);
                }
            }
            else
            {
                WriteType(SymbolKeyType.NamedType);
                NamedTypeSymbolKey.Instance.Create(namedTypeSymbol, this);
            }
        }

        public override void VisitNamespace(INamespaceSymbol namespaceSymbol)
        {
            WriteType(SymbolKeyType.Namespace);
            NamespaceSymbolKey.Instance.Create(namespaceSymbol, this);
        }

        public override void VisitParameter(IParameterSymbol parameterSymbol)
        {
            WriteType(SymbolKeyType.Parameter);
            ParameterSymbolKey.Instance.Create(parameterSymbol, this);
        }

        public override void VisitPointerType(IPointerTypeSymbol pointerTypeSymbol)
        {
            WriteType(SymbolKeyType.PointerType);
            PointerTypeSymbolKey.Instance.Create(pointerTypeSymbol, this);
        }

        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            WriteType(SymbolKeyType.FunctionPointer);
            FunctionPointerTypeSymbolKey.Instance.Create(symbol, this);
        }

        public override void VisitProperty(IPropertySymbol propertySymbol)
        {
            WriteType(SymbolKeyType.Property);
            PropertySymbolKey.Instance.Create(propertySymbol, this);
        }

        public override void VisitEvent(IEventSymbol eventSymbol)
        {
            WriteType(SymbolKeyType.Event);
            EventSymbolKey.Instance.Create(eventSymbol, this);
        }

        public override void VisitTypeParameter(ITypeParameterSymbol typeParameterSymbol)
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
                TypeParameterSymbolKey.Instance.Create(typeParameterSymbol, this);
            }
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
                        if (typeParameter.DeclaringMethod!.Equals(method))
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
            Contract.ThrowIfFalse(method.Equals(_methodSymbolStack[^1]));
            _methodSymbolStack.RemoveAt(_methodSymbolStack.Count - 1);
        }
    }
}
