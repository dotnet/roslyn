// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

            private readonly ReadFunction<TStringResult> _readString;
            private readonly ReadFunction<bool> _readBoolean;
            private readonly ReadFunction<RefKind> _readRefKind;

            protected string Data { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public int Position;

            public Reader()
            {
                _readString = ReadString;
                _readBoolean = ReadBoolean;
                _readRefKind = ReadRefKind;
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
                => Eat((char)type);

            protected char Eat(char c)
            {
                Debug.Assert(Data[Position] == c);
                Position++;
                return c;
            }

            protected void EatCloseParen()
                => Eat(CloseParenChar);

            protected void EatOpenParen()
                => Eat(OpenParenChar);

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
                => Eat(SpaceChar);

            public bool ReadBoolean()
                => ReadBoolean(out _);

            public bool ReadBoolean(out string failureReason)
            {
                failureReason = null;
                var val = ReadInteger();
                Debug.Assert(val == 0 || val == 1);
                return val == 1;
            }

            public TStringResult ReadString()
                => ReadString(out _);

            public TStringResult ReadString(out string failureReason)
            {
                failureReason = null;
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
                => Eat(DoubleQuoteChar);

            public PooledArrayBuilder<TStringResult> ReadStringArray()
                => ReadArray(_readString, out _);

            public PooledArrayBuilder<bool> ReadBooleanArray()
                => ReadArray(_readBoolean, out _);

            public PooledArrayBuilder<RefKind> ReadRefKindArray()
                => ReadArray(_readRefKind, out _);

            public PooledArrayBuilder<T> ReadArray<T>(ReadFunction<T> readFunction, out string failureReason)
            {
                var builder = PooledArrayBuilder<T>.GetInstance();
                EatSpace();

                Debug.Assert((SymbolKeyType)Data[Position] != SymbolKeyType.Null);

                EatOpenParen();
                Eat(SymbolKeyType.Array);

                string totalFailureReason = null;
                var length = ReadInteger();
                for (var i = 0; i < length; i++)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    builder.Builder.Add(readFunction(out var elementFailureReason));

                    if (elementFailureReason != null)
                    {
                        var reason = $"element {i} failed {elementFailureReason}";
                        totalFailureReason = totalFailureReason == null
                            ? $"({reason})"
                            : $"({totalFailureReason} -> {reason})";
                    }
                }

                EatCloseParen();
                failureReason = totalFailureReason;
                return builder;
            }

            public RefKind ReadRefKind()
                => ReadRefKind(out _);

            public RefKind ReadRefKind(out string failureReason)
            {
                failureReason = null;
                return (RefKind)ReadInteger();
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
                => null;
        }

        private delegate T ReadFunction<T>(out string failureReason);

        private class SymbolKeyReader : Reader<string>
        {
            private static readonly ObjectPool<SymbolKeyReader> s_readerPool = SharedPools.Default<SymbolKeyReader>();

            private readonly Dictionary<int, SymbolKeyResolution> _idToResult = new Dictionary<int, SymbolKeyResolution>();
            private readonly ReadFunction<SymbolKeyResolution> _readSymbolKey;
            private readonly ReadFunction<Location> _readLocation;

            public Compilation Compilation { get; private set; }
            public bool IgnoreAssemblyKey { get; private set; }
            public SymbolEquivalenceComparer Comparer { get; private set; }

            private readonly List<IMethodSymbol> _methodSymbolStack = new List<IMethodSymbol>();

            public SymbolKeyReader()
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
                Comparer = null;
                _methodSymbolStack.Clear();

                // Place us back in the pool for future use.
                s_readerPool.Free(this);
            }

            public static SymbolKeyReader GetReader(
                string data, Compilation compilation,
                bool ignoreAssemblyKey,
                CancellationToken cancellationToken)
            {
                var reader = s_readerPool.Allocate();
                reader.Initialize(data, compilation, ignoreAssemblyKey, cancellationToken);
                return reader;
            }

            private void Initialize(
                string data,
                Compilation compilation,
                bool ignoreAssemblyKey,
                CancellationToken cancellationToken)
            {
                base.Initialize(data, cancellationToken);
                Compilation = compilation;
                IgnoreAssemblyKey = ignoreAssemblyKey;

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

            public SymbolKeyResolution ReadSymbolKey(out string failureReason)
            {
                CancellationToken.ThrowIfCancellationRequested();
                EatSpace();

                var type = (SymbolKeyType)Data[Position];
                if (type == SymbolKeyType.Null)
                {
                    Eat(type);
                    failureReason = null;
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
                    failureReason = null;
                }
                else
                {
                    result = ReadWorker(type, out failureReason);
                    var id = ReadInteger();
                    _idToResult[id] = result;
                }

                EatCloseParen();

                return result;
            }

            private SymbolKeyResolution ReadWorker(SymbolKeyType type, out string failureReason)
                => type switch
                {
                    SymbolKeyType.Alias => AliasSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.BodyLevel => BodyLevelSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.ConstructedMethod => ConstructedMethodSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.NamedType => NamedTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.ErrorType => ErrorTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Field => FieldSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.FunctionPointer => FunctionPointerTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.DynamicType => DynamicTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Method => MethodSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Namespace => NamespaceSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.PointerType => PointerTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Parameter => ParameterSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Property => PropertySymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.ArrayType => ArrayTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Assembly => AssemblySymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.TupleType => TupleTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Module => ModuleSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.Event => EventSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.ReducedExtensionMethod => ReducedExtensionMethodSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.TypeParameter => TypeParameterSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.AnonymousType => AnonymousTypeSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.AnonymousFunctionOrDelegate => AnonymousFunctionOrDelegateSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.TypeParameterOrdinal => TypeParameterOrdinalSymbolKey.Resolve(this, out failureReason),
                    _ => throw new NotImplementedException(),
                };

            /// <summary>
            /// Reads an array of symbols out from the key.  Note: the number of symbols returned 
            /// will either be the same as the original amount written, or <c>default</c> will be 
            /// returned. It will never be less or more.  <c>default</c> will be returned if any 
            /// elements could not be resolved to the requested <typeparamref name="TSymbol"/> type 
            /// in the provided <see cref="SymbolKeyReader.Compilation"/>.
            /// 
            /// Callers should <see cref="IDisposable.Dispose"/> the instance returned.  No check is
            /// necessary if <c>default</c> was returned before calling <see cref="IDisposable.Dispose"/>
            /// </summary>
            public PooledArrayBuilder<TSymbol> ReadSymbolKeyArray<TSymbol>(out string failureReason) where TSymbol : ISymbol
            {
                using var resolutions = ReadArray(_readSymbolKey, out var elementsFailureReason);
                if (elementsFailureReason != null)
                {
                    failureReason = elementsFailureReason;
                    return default;
                }

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
                        failureReason = $"({nameof(ReadSymbolKeyArray)} incorrect type for element)";
                        return default;
                    }
                }

                failureReason = null;
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
                => null;

            #endregion

            #region Locations

            public Location ReadLocation(out string failureReason)
            {
                EatSpace();
                if ((SymbolKeyType)Data[Position] == SymbolKeyType.Null)
                {
                    Eat(SymbolKeyType.Null);
                    failureReason = null;
                    return null;
                }

                var kind = (LocationKind)ReadInteger();
                if (kind == LocationKind.None)
                {
                    failureReason = null;
                    return Location.None;
                }
                else if (kind == LocationKind.SourceFile)
                {
                    var filePath = ReadString();
                    var start = ReadInteger();
                    var length = ReadInteger();

                    var syntaxTree = GetSyntaxTree(filePath);
                    if (syntaxTree == null)
                    {
                        failureReason = $"({nameof(ReadLocation)} failed -> '{filePath}' not in compilation)";
                        return null;
                    }

                    failureReason = null;
                    return Location.Create(syntaxTree, new TextSpan(start, length));
                }
                else if (kind == LocationKind.MetadataFile)
                {
                    var assemblyResolution = ReadSymbolKey(out var assemblyFailureReason);
                    var moduleName = ReadString();

                    if (assemblyFailureReason != null)
                    {
                        failureReason = $"{nameof(ReadLocation)} {nameof(assemblyResolution)} failed -> " + assemblyFailureReason;
                        return Location.None;
                    }

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
                                failureReason = null;
                                return location;
                            }
                        }
                    }

                    failureReason = null;
                    return Location.None;
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }

            public SymbolKeyResolution? ResolveLocation(Location location)
            {
                if (location.SourceTree != null)
                {
                    var node = location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, CancellationToken);
                    var semanticModel = Compilation.GetSemanticModel(location.SourceTree);
                    var symbol = semanticModel.GetDeclaredSymbol(node, CancellationToken);
                    if (symbol != null)
                        return new SymbolKeyResolution(symbol);

                    var info = semanticModel.GetSymbolInfo(node, CancellationToken);
                    if (info.Symbol != null)
                        return new SymbolKeyResolution(info.Symbol);

                    if (info.CandidateSymbols.Length > 0)
                        return new SymbolKeyResolution(info.CandidateSymbols, info.CandidateReason);
                }

                return null;
            }

            private static IModuleSymbol GetModule(IEnumerable<IModuleSymbol> modules, string moduleName)
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

            public PooledArrayBuilder<Location> ReadLocationArray(out string failureReason)
                => ReadArray(_readLocation, out failureReason);

            #endregion
        }
    }
}
