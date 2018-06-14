// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial class SymbolKeyWriter
    {
        private static readonly ObjectPool<SymbolKeyWriter> s_writerPool = new ObjectPool<SymbolKeyWriter>(() => new SymbolKeyWriter());

        private readonly StringBuilder _builder;
        private readonly Visitor _visitor;
        private readonly Dictionary<ISymbol, int> _symbolRefIdMap;

        private readonly Action<bool> _writeBool;
        private readonly Action<Location> _writeLocation;
        private readonly Action<IParameterSymbol> _writeParameterRefKind;
        private readonly Action<IParameterSymbol> _writeParameterType;
        private readonly Action<string> _writeString;
        private readonly Action<ISymbol> _writeSymbol;

        private CancellationToken _cancellationToken;
        private int _nestingCount;
        private int _nextSymbolRefId;

        private SymbolKeyWriter()
        {
            _builder = new StringBuilder();
            _visitor = new Visitor(this);
            _symbolRefIdMap = new Dictionary<ISymbol, int>();

            _writeBool = WriteBool;
            _writeLocation = WriteLocation;
            _writeParameterRefKind = p => WriteInt((int)p.RefKind);
            _writeParameterType = p => WriteSymbol(p.Type);
            _writeString = WriteString;
            _writeSymbol = WriteSymbol;
        }

        private void Clear()
        {
            _builder.Clear();
            _symbolRefIdMap.Clear();
            _cancellationToken = default;
            _nestingCount = 0;
            _nextSymbolRefId = 0;
        }

        public static string Write(ISymbol symbol, CancellationToken cancellationToken)
        {
            var writer = s_writerPool.Allocate();
            writer._cancellationToken = cancellationToken;
            writer.WriteFirstSymbol(symbol);

            var result = writer._builder.ToString();
            writer.Clear();

            // Place back in the pool for future use.
            s_writerPool.Free(writer);

            return result;
        }

        private void WriteFirstSymbol(ISymbol symbol)
        {
            WriteSymbolCore(symbol);
        }

        private void WriteSymbol(ISymbol symbol)
        {
            WriteSpace();
            WriteSymbolCore(symbol);
        }

        private void WriteSymbolCore(ISymbol symbol)
        {
            if (symbol == null)
            {
                WriteType(SymbolKeyType.Null);
                return;
            }

            // If we've already seen this symbol, just write a reference to it.
            if (_symbolRefIdMap.TryGetValue(symbol, out var symbolRefId))
            {
                WriteSymbolReference(symbolRefId);
                return;
            }

            symbolRefId = _nextSymbolRefId++;

            // Capture the reference ID for this symbol so that any recursive references
            // to the symbol can use it.
            _symbolRefIdMap.Add(symbol, symbolRefId);

            WriteSymbolStart();

            _visitor.Visit(symbol);

            WriteInt(symbolRefId);
            WriteSymbolEnd();
        }

        private void WriteBool(bool value)
        {
            WriteInt(value ? 1 : 0);
        }

        private void WriteInt(int value)
        {
            WriteSpace();
            _builder.Append(value);
        }

        private void WriteLocation(Location location)
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

            WriteInt((int)location.Kind);
            if (location.Kind == LocationKind.SourceFile)
            {
                WriteString(location.SourceTree.FilePath);
                WriteInt(location.SourceSpan.Start);
                WriteInt(location.SourceSpan.Length);
            }
            else if (location.Kind == LocationKind.MetadataFile)
            {
                WriteSymbol(location.MetadataModule.ContainingAssembly);
                WriteString(location.MetadataModule.MetadataName);
            }
        }

        private void WriteSymbolReference(int symbolRefId)
        {
            WriteSymbolStart();
            WriteType(SymbolKeyType.Reference);
            WriteInt(symbolRefId);
            WriteSymbolEnd();
        }

        private void WriteSpace()
        {
            _builder.Append(' ');
        }

        private void WriteString(string value)
        {
            WriteSpace();

            if (value == null)
            {
                WriteType(SymbolKeyType.Null);
            }
            else
            {
                _builder.Append('"');
                _builder.Append(value.Replace("\"", "\"\""));
                _builder.Append('"');
            }
        }

        private void WriteSymbolStart()
        {
            _builder.Append('(');
            _nestingCount++;
        }

        private void WriteSymbolEnd()
        {
            Debug.Assert(_nestingCount > 0);
            _nestingCount--;
            _builder.Append(')');
        }

        private void WriteType(char type)
        {
            _builder.Append(type);
        }

        private void WriteBoolArray(in ImmutableArray<bool> values)
        {
            WriteArray(values, _writeBool);
        }

        private void WriteLocationArray(in ImmutableArray<Location> values)
        {
            WriteArray(values, _writeLocation);
        }

        private void WriteParameterTypesArray(in ImmutableArray<IParameterSymbol> symbols)
        {
            WriteArray(symbols, _writeParameterType);
        }

        private void WriteParameterRefKindsArray(in ImmutableArray<IParameterSymbol> values)
        {
            WriteArray(values, _writeParameterRefKind);
        }

        private void WriteStringArray(in ImmutableArray<string> values)
        {
            WriteArray(values, _writeString);
        }

        private void WriteSymbolArray<TSymbol>(in ImmutableArray<TSymbol> symbols)
            where TSymbol : ISymbol
        {
            WriteArray(symbols, _writeSymbol);
        }

        private void WriteArray<T1, T2>(in ImmutableArray<T1> array, Action<T2> writeValue)
            where T1 : T2
        {
            WriteSpace();

            if (array.IsDefault)
            {
                WriteType(SymbolKeyType.Null);
                return;
            }

            WriteSymbolStart();
            WriteType(SymbolKeyType.Array);

            WriteInt(array.Length);

            foreach (var value in array)
            {
                writeValue(value);
            }

            WriteSymbolEnd();
        }
    }
}
