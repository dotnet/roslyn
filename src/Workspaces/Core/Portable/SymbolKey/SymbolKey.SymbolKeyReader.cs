// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
                return ReadIntegerRaw_DoNotCallDirectly();
            }

            public int ReadFormatVersion()
                => ReadIntegerRaw_DoNotCallDirectly();

            private int ReadIntegerRaw_DoNotCallDirectly()
            {
                Debug.Assert(char.IsNumber(Data[Position]));

                var value = 0;

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

            public PooledArrayBuilder<TStringResult> ReadStringArray()
                => ReadArray(_readString);

            public PooledArrayBuilder<bool> ReadBooleanArray()
                => ReadArray(_readBoolean);

            public PooledArrayBuilder<RefKind> ReadRefKindArray()
                => ReadArray(_readRefKind);

            public PooledArrayBuilder<T> ReadArray<T>(Func<T> readFunction)
            {
                var builder = PooledArrayBuilder<T>.GetInstance();
                EatSpace();

                Debug.Assert((SymbolKeyType)Data[Position] != SymbolKeyType.Null);

                EatOpenParen();
                Eat(SymbolKeyType.Array);

                var length = ReadInteger();
                for (var i = 0; i < length; i++)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    builder.Builder.Add(readFunction());
                }

                EatCloseParen();
                return builder;
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

            private readonly List<IMethodSymbol> _methodSymbolStack = new List<IMethodSymbol>();
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
                PooledArrayBuilder<ITypeSymbol> originalParameterTypes)
            {
                if (originalParameterTypes.IsDefault || parameters.Length != originalParameterTypes.Count)
                {
                    return false;
                }

                // We are checking parameters for equality, if they refer to method type parameters,
                // then we don't want to recurse through the method (which would then recurse right
                // back into the parameters).  So we use a signature type comparer as it will properly
                // compare method type parameters by ordinal.
                var signatureComparer = Comparer.SignatureTypeEquivalenceComparer;

                for (var i = 0; i < originalParameterTypes.Count; i++)
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
                Contract.ThrowIfFalse(Equals(methodOpt, _methodSymbolStack[_methodSymbolStack.Count - 1]));
                _methodSymbolStack.RemoveAt(_methodSymbolStack.Count - 1);
            }

            public IMethodSymbol ResolveMethod(int index)
                => _methodSymbolStack[index];

            internal SyntaxTree GetSyntaxTree(string filePath)
            {
                foreach (var tree in this.Compilation.SyntaxTrees)
                {
                    if (tree.FilePath == filePath)
                    {
                        return tree;
                    }
                }

                return null;
            }

            #region Symbols

            public SymbolKeyResolution ReadSymbolKey()
            {
                CancellationToken.ThrowIfCancellationRequested();
                EatSpace();

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
                => type switch
                {
                    SymbolKeyType.Alias => AliasSymbolKey.Resolve(this),
                    SymbolKeyType.BodyLevel => BodyLevelSymbolKey.Resolve(this),
                    SymbolKeyType.ConstructedMethod => ConstructedMethodSymbolKey.Resolve(this),
                    SymbolKeyType.NamedType => NamedTypeSymbolKey.Resolve(this),
                    SymbolKeyType.ErrorType => ErrorTypeSymbolKey.Resolve(this),
                    SymbolKeyType.Field => FieldSymbolKey.Resolve(this),
                    SymbolKeyType.DynamicType => DynamicTypeSymbolKey.Resolve(this),
                    SymbolKeyType.Method => MethodSymbolKey.Resolve(this),
                    SymbolKeyType.Namespace => NamespaceSymbolKey.Resolve(this),
                    SymbolKeyType.PointerType => PointerTypeSymbolKey.Resolve(this),
                    SymbolKeyType.Parameter => ParameterSymbolKey.Resolve(this),
                    SymbolKeyType.Property => PropertySymbolKey.Resolve(this),
                    SymbolKeyType.ArrayType => ArrayTypeSymbolKey.Resolve(this),
                    SymbolKeyType.Assembly => AssemblySymbolKey.Resolve(this),
                    SymbolKeyType.TupleType => TupleTypeSymbolKey.Resolve(this),
                    SymbolKeyType.Module => ModuleSymbolKey.Resolve(this),
                    SymbolKeyType.Event => EventSymbolKey.Resolve(this),
                    SymbolKeyType.ReducedExtensionMethod => ReducedExtensionMethodSymbolKey.Resolve(this),
                    SymbolKeyType.TypeParameter => TypeParameterSymbolKey.Resolve(this),
                    SymbolKeyType.AnonymousType => AnonymousTypeSymbolKey.Resolve(this),
                    SymbolKeyType.AnonymousFunctionOrDelegate => AnonymousFunctionOrDelegateSymbolKey.Resolve(this),
                    SymbolKeyType.TypeParameterOrdinal => TypeParameterOrdinalSymbolKey.Resolve(this),
                    _ => throw new NotImplementedException(),
                };

            /// <summary>
            /// Reads an array of symbols out from the key.  Note: the number of symbols returned 
            /// will either be the same as the original amount written, or <c>default</c> will be 
            /// returned. It will never be less or more.  <c>default</c> will be returned if any 
            /// elements could not be resolved to the requested <typeparamref name="TSymbol"/> type 
            /// in the provided <see cref="Compilation"/>.
            /// 
            /// Callers should <see cref="IDisposable.Dispose"/> the instance returned.  No check is
            /// necessary if <c>default</c> was returned before calling <see cref="IDisposable.Dispose"/>
            /// </summary>
            public PooledArrayBuilder<TSymbol> ReadSymbolKeyArray<TSymbol>() where TSymbol : ISymbol
            {
                using var resolutions = ReadArray(_readSymbolKey);

                var result = PooledArrayBuilder<TSymbol>.GetInstance();
                foreach (var resolution in resolutions)
                {
                    if (resolution.GetAnySymbol() is TSymbol castedSymbol)
                    {
                        result.AddIfNotNull(castedSymbol);
                    }
                    else
                    {
                        result.Dispose();
                        return default;
                    }
                }

                return result;
            }

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
                            var module = GetModule(assembly.Modules, moduleName);
                            if (module != null)
                            {
                                var location = FirstOrDefault(module.Locations);
                                if (location != null)
                                {
                                    return location;
                                }
                            }
                        }
                    }
                }

                return Location.None;
            }

            private IModuleSymbol GetModule(IEnumerable<IModuleSymbol> modules, string moduleName)
            {
                foreach (var module in modules)
                {
                    if (module.MetadataName == moduleName)
                    {
                        return module;
                    }
                }

                return null;
            }

            public PooledArrayBuilder<Location> ReadLocationArray()
                => ReadArray(_readLocation);

            #endregion
        }
    }
}
