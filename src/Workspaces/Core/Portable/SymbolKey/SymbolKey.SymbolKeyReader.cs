// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private abstract class Reader<TStringResult> : IDisposable
        {
            protected const char OpenParenChar = '(';
            protected const char CloseParenChar = ')';
            protected const char SpaceChar = ' ';
            protected const char DoubleQuoteChar = '"';

            private readonly Func<TStringResult> _readString;
            private readonly Func<bool> _readBoolean;
            private readonly Func<RefKind> _readRefKind;

            protected string Data { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public int Position;

            public Reader()
            {
                _readString = ReadString;
                _readBoolean = ReadBoolean;
                _readRefKind = () => (RefKind)ReadInteger();
            }

            protected virtual void Initialize(string data, CancellationToken cancellationToken)
            {
                Data = data;
                CancellationToken = cancellationToken;
                Position = 0;
            }

            public virtual void Dispose()
            {
                Data = null;
                CancellationToken = default;
            }

            protected char Eat(SymbolKeyType type)
            {
                return Eat((char)type);
            }

            protected char Eat(char c)
            {
                Debug.Assert(Data[Position] == c);
                Position++;
                return c;
            }

            protected void EatCloseParen()
            {
                Eat(CloseParenChar);
            }

            protected void EatOpenParen()
            {
                Eat(OpenParenChar);
            }

            public int ReadInteger()
            {
                EatSpace();
                Debug.Assert(char.IsNumber(Data[Position]));

                int value = 0;

                var start = Position;
                while (char.IsNumber(Data[Position]))
                {
                    var digit = Data[Position] - '0';

                    value *= 10;
                    value += digit;

                    Position++;
                }

                Debug.Assert(start != Position);
                return value;
            }

            protected char EatSpace()
            {
                return Eat(SpaceChar);
            }

            public bool ReadBoolean()
            {
                var val = ReadInteger();
                Debug.Assert(val == 0 || val == 1);
                return val == 1;
            }

            public TStringResult ReadString()
            {
                EatSpace();
                return ReadStringNoSpace();
            }

            protected TStringResult ReadStringNoSpace()
            {
                if ((SymbolKeyType)Data[Position] == SymbolKeyType.Null)
                {
                    Eat(SymbolKeyType.Null);
                    return CreateNullForString();
                }

                EatDoubleQuote();

                var start = Position;

                var hasEmbeddedQuote = false;
                while (true)
                {
                    if (Data[Position] != DoubleQuoteChar)
                    {
                        Position++;
                        continue;
                    }

                    // We have a quote.  See if it's the final quote, or if it's an escaped
                    // embedded quote.
                    if (Data[Position + 1] == DoubleQuoteChar)
                    {
                        hasEmbeddedQuote = true;
                        Position += 2;
                        continue;
                    }

                    break;
                }

                var end = Position;
                EatDoubleQuote();

                var result = CreateResultForString(start, end, hasEmbeddedQuote);

                return result;
            }

            protected abstract TStringResult CreateResultForString(int start, int end, bool hasEmbeddedQuote);
            protected abstract TStringResult CreateNullForString();

            private void EatDoubleQuote()
            {
                Eat(DoubleQuoteChar);
            }

            public ImmutableArray<TStringResult> ReadStringArray()
            {
                return ReadArray(_readString);
            }

            public ImmutableArray<bool> ReadBooleanArray()
            {
                return ReadArray(_readBoolean);
            }

            public ImmutableArray<RefKind> ReadRefKindArray()
            {
                return ReadArray(_readRefKind);
            }

            public ImmutableArray<T> ReadArray<T>(Func<T> readFunction)
            {
                EatSpace();

                if ((SymbolKeyType)Data[Position] == SymbolKeyType.Null)
                {
                    Eat(SymbolKeyType.Null);
                    return default;
                }

                EatOpenParen();
                Eat(SymbolKeyType.Array);

                var length = ReadInteger();
                var builder = ImmutableArray.CreateBuilder<T>(length);

                for (var i = 0; i < length; i++)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    builder.Add(readFunction());
                }

                EatCloseParen();

                return builder.MoveToImmutable();
            }
        }

        private class RemoveAssemblySymbolKeysReader : Reader<object>
        {
            private readonly StringBuilder _builder = new StringBuilder();

            private bool _skipString = false;

            public RemoveAssemblySymbolKeysReader()
            {
            }

            public void Initialize(string data)
                => base.Initialize(data, CancellationToken.None);

            public string RemoveAssemblySymbolKeys()
            {
                while (Position < Data.Length)
                {
                    var ch = Data[Position];
                    if (ch == OpenParenChar)
                    {
                        _builder.Append(Eat(OpenParenChar));

                        var type = (SymbolKeyType)Data[Position];
                        _builder.Append(Eat(type));
                        if (type == SymbolKeyType.Assembly)
                        {
                            Debug.Assert(_skipString == false);
                            _skipString = true;
                            ReadString();

                            Debug.Assert(_skipString == true);
                            _skipString = false;
                        }
                    }
                    else if (Data[Position] == DoubleQuoteChar)
                    {
                        ReadStringNoSpace();
                    }
                    else
                    {
                        // All other characters we pass along directly to the string builder.
                        _builder.Append(Eat(ch));
                    }
                }

                return _builder.ToString();
            }

            protected override object CreateResultForString(int start, int end, bool hasEmbeddedQuote)
            {
                // 'start' is right after the open quote, and 'end' is right before the close quote.
                // However, we want to include both quotes in the result.
                _builder.Append(DoubleQuoteChar);
                if (!_skipString)
                {
                    for (var i = start; i < end; i++)
                    {
                        _builder.Append(Data[i]);
                    }
                }
                _builder.Append(DoubleQuoteChar);
                return null;
            }

            protected override object CreateNullForString()
            {
                return null;
            }
        }

        private class SymbolKeyReader : Reader<string>
        {
            private static readonly ObjectPool<SymbolKeyReader> s_readerPool =
                new ObjectPool<SymbolKeyReader>(() => new SymbolKeyReader());

            private readonly Dictionary<int, SymbolKeyResolution> _idToResult = new Dictionary<int, SymbolKeyResolution>();
            private readonly Func<SymbolKeyResolution> _readSymbolKey;
            private readonly Func<Location> _readLocation;

            public Compilation Compilation { get; private set; }
            public bool IgnoreAssemblyKey { get; private set; }
            public SymbolEquivalenceComparer Comparer { get; private set; }

            private List<IMethodSymbol> _methodSymbolStack = new List<IMethodSymbol>();
            private bool _resolveLocations;

            private SymbolKeyReader()
            {
                _readSymbolKey = ReadSymbolKey;
                _readLocation = ReadLocation;
            }

            public override void Dispose()
            {
                base.Dispose();
                _idToResult.Clear();
                Compilation = null;
                IgnoreAssemblyKey = false;
                _resolveLocations = false;
                Comparer = null;
                _methodSymbolStack.Clear();

                // Place us back in the pool for future use.
                s_readerPool.Free(this);
            }

            public static SymbolKeyReader GetReader(
                string data, Compilation compilation,
                bool ignoreAssemblyKey, bool resolveLocations,
                CancellationToken cancellationToken)
            {
                var reader = s_readerPool.Allocate();
                reader.Initialize(data, compilation, ignoreAssemblyKey, resolveLocations, cancellationToken);
                return reader;
            }

            private void Initialize(
                string data,
                Compilation compilation,
                bool ignoreAssemblyKey,
                bool resolveLocations,
                CancellationToken cancellationToken)
            {
                base.Initialize(data, cancellationToken);
                Compilation = compilation;
                IgnoreAssemblyKey = ignoreAssemblyKey;
                _resolveLocations = resolveLocations;

                Comparer = ignoreAssemblyKey
                    ? SymbolEquivalenceComparer.IgnoreAssembliesInstance
                    : SymbolEquivalenceComparer.Instance;
            }

            internal bool ParameterTypesMatch(
                ImmutableArray<IParameterSymbol> parameters,
                ITypeSymbol[] originalParameterTypes)
            {
                if (parameters.Length != originalParameterTypes.Length)
                {
                    return false;
                }

                // We are checking parameters for equality, if they refer to method type parameters,
                // then we don't want to recurse through the method (which would then recurse right
                // back into the parameters).  So we use a signature type comparer as it will properly
                // compare method type parameters by ordinal.
                var signatureComparer = Comparer.SignatureTypeEquivalenceComparer;

                for (int i = 0; i < originalParameterTypes.Length; i++)
                {
                    if (!signatureComparer.Equals(originalParameterTypes[i], parameters[i].Type))
                    {
                        return false;
                    }
                }

                return true;
            }

            public void PushMethod(IMethodSymbol methodOpt)
                => _methodSymbolStack.Add(methodOpt);

            public void PopMethod(IMethodSymbol methodOpt)
            {
                Contract.ThrowIfTrue(_methodSymbolStack.Count == 0);
                Contract.ThrowIfFalse(Equals(methodOpt, _methodSymbolStack.Last()));
                _methodSymbolStack.RemoveAt(_methodSymbolStack.Count - 1);
            }

            public IMethodSymbol ResolveMethod(int index)
                => _methodSymbolStack[index];

            internal SyntaxTree GetSyntaxTree(string filePath)
                => this.Compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == filePath);

            #region Symbols

            public SymbolKeyResolution ReadFirstSymbolKey()
            {
                return ReadSymbolKeyWorker(first: true);
            }

            public SymbolKeyResolution ReadSymbolKey()
            {
                return ReadSymbolKeyWorker(first: false);
            }

            private SymbolKeyResolution ReadSymbolKeyWorker(bool first)
            {
                CancellationToken.ThrowIfCancellationRequested();
                if (!first)
                {
                    EatSpace();
                }

                var type = (SymbolKeyType)Data[Position];
                if (type == SymbolKeyType.Null)
                {
                    Eat(type);
                    return default;
                }

                EatOpenParen();
                SymbolKeyResolution result;

                type = (SymbolKeyType)Data[Position];
                Eat(type);

                if (type == SymbolKeyType.Reference)
                {
                    var id = ReadInteger();
                    result = _idToResult[id];
                }
                else
                {
                    result = ReadWorker(type);
                    var id = ReadInteger();
                    _idToResult[id] = result;
                }

                EatCloseParen();

                return result;
            }

            private SymbolKeyResolution ReadWorker(SymbolKeyType type)
            {
                switch (type)
                {
                    case SymbolKeyType.Alias: return AliasSymbolKey.Resolve(this);
                    case SymbolKeyType.BodyLevel: return BodyLevelSymbolKey.Resolve(this);
                    case SymbolKeyType.ConstructedMethod: return ConstructedMethodSymbolKey.Resolve(this);
                    case SymbolKeyType.NamedType: return NamedTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.ErrorType: return ErrorTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.Field: return FieldSymbolKey.Resolve(this);
                    case SymbolKeyType.DynamicType: return DynamicTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.Method: return MethodSymbolKey.Resolve(this);
                    case SymbolKeyType.Namespace: return NamespaceSymbolKey.Resolve(this);
                    case SymbolKeyType.PointerType: return PointerTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.Parameter: return ParameterSymbolKey.Resolve(this);
                    case SymbolKeyType.Property: return PropertySymbolKey.Resolve(this);
                    case SymbolKeyType.ArrayType: return ArrayTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.Assembly: return AssemblySymbolKey.Resolve(this);
                    case SymbolKeyType.TupleType: return TupleTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.Module: return ModuleSymbolKey.Resolve(this);
                    case SymbolKeyType.Event: return EventSymbolKey.Resolve(this);
                    case SymbolKeyType.ReducedExtensionMethod: return ReducedExtensionMethodSymbolKey.Resolve(this);
                    case SymbolKeyType.TypeParameter: return TypeParameterSymbolKey.Resolve(this);
                    case SymbolKeyType.AnonymousType: return AnonymousTypeSymbolKey.Resolve(this);
                    case SymbolKeyType.AnonymousFunctionOrDelegate: return AnonymousFunctionOrDelegateSymbolKey.Resolve(this);
                    case SymbolKeyType.TypeParameterOrdinal: return TypeParameterOrdinalSymbolKey.Resolve(this);
                }

                throw new NotImplementedException();
            }

            public ImmutableArray<SymbolKeyResolution> ReadSymbolKeyArray()
                => ReadArray(_readSymbolKey);

            #endregion

            #region Strings

            protected override string CreateResultForString(int start, int end, bool hasEmbeddedQuote)
            {
                var substring = Data.Substring(start, end - start);
                var result = hasEmbeddedQuote
                    ? substring.Replace("\"\"", "\"")
                    : substring;
                return result;
            }

            protected override string CreateNullForString()
            {
                return null;
            }

            #endregion

            #region Locations

            public Location ReadLocation()
            {
                EatSpace();
                if ((SymbolKeyType)Data[Position] == SymbolKeyType.Null)
                {
                    Eat(SymbolKeyType.Null);
                    return null;
                }

                var kind = (LocationKind)ReadInteger();
                if (kind == LocationKind.SourceFile)
                {
                    var filePath = ReadString();
                    var start = ReadInteger();
                    var length = ReadInteger();

                    if (_resolveLocations)
                    {
                        // The syntax tree can be null if we're resolving this location in a compilation
                        // that does not contain this file.  In this case, just map this location to None.
                        var syntaxTree = GetSyntaxTree(filePath);
                        if (syntaxTree != null)
                        {
                            return Location.Create(syntaxTree, new TextSpan(start, length));
                        }
                    }
                }
                else if (kind == LocationKind.MetadataFile)
                {
                    var assemblyResolution = ReadSymbolKey();
                    var moduleName = ReadString();

                    if (_resolveLocations)
                    {
                        // We may be resolving in a compilation where we don't have a module
                        // with this name.  In that case, just map this location to none.
                        if (assemblyResolution.GetAnySymbol() is IAssemblySymbol assembly)
                        {
                            var module = assembly.Modules.FirstOrDefault(m => m.MetadataName == moduleName);
                            var location = module?.Locations.FirstOrDefault();
                            if (location != null)
                            {
                                return location;
                            }
                        }
                    }
                }

                return Location.None;
            }

            private Location CreateModuleLocation(
                SymbolKeyResolution assembly, string moduleName)
            {
                var symbol = assembly.GetAnySymbol() as IAssemblySymbol;
                Debug.Assert(symbol != null);
                var module = symbol.Modules.FirstOrDefault(m => m.MetadataName == moduleName);
                return module.Locations.FirstOrDefault();
            }

            public ImmutableArray<Location> ReadLocationArray()
                => ReadArray(_readLocation);

            #endregion
        }
    }
}
