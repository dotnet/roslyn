// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private abstract class Reader<TStringResult> : IDisposable where TStringResult : class
        {
            protected const char OpenParenChar = '(';
            protected const char CloseParenChar = ')';
            protected const char SpaceChar = ' ';
            protected const char DoubleQuoteChar = '"';

            private readonly ReadFunction<TStringResult?> _readString;
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

                Data = null!;
            }

            protected virtual void Initialize(string data, CancellationToken cancellationToken)
            {
                Data = data;
                CancellationToken = cancellationToken;
                Position = 0;
            }

            public virtual void Dispose()
            {
                Data = null!;
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

            public bool ReadBoolean(out string? failureReason)
            {
                failureReason = null;
                var val = ReadInteger();
                Debug.Assert(val is 0 or 1);
                return val == 1;
            }

            public TStringResult? ReadString()
                => ReadString(out _);

            public TStringResult ReadRequiredString()
            {
                var result = ReadString();
                Contract.ThrowIfNull(result);
                return result;
            }

            public TStringResult? ReadString(out string? failureReason)
            {
                failureReason = null;
                EatSpace();
                return ReadStringNoSpace();
            }

            protected TStringResult? ReadStringNoSpace()
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

            protected abstract TStringResult? CreateResultForString(int start, int end, bool hasEmbeddedQuote);
            protected abstract TStringResult? CreateNullForString();

            private void EatDoubleQuote()
                => Eat(DoubleQuoteChar);

            public PooledArrayBuilder<TStringResult?> ReadStringArray()
                => ReadSimpleArray(_readString, out _);

            public PooledArrayBuilder<bool> ReadBooleanArray()
                => ReadSimpleArray(_readBoolean, out _);

            public PooledArrayBuilder<RefKind> ReadRefKindArray()
                => ReadSimpleArray(_readRefKind, out _);

            public PooledArrayBuilder<T> ReadSimpleArray<T>(
                ReadFunction<T> readFunction,
                out string? failureReason)
            {
                // Keep in Sync with ReadSymbolArray in SymbolKeyReader

                var builder = PooledArrayBuilder<T>.GetInstance();
                EatSpace();

                Debug.Assert((SymbolKeyType)Data[Position] != SymbolKeyType.Null);

                EatOpenParen();
                Eat(SymbolKeyType.Array);

                string? totalFailureReason = null;
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

            public RefKind ReadRefKind(out string? failureReason)
            {
                failureReason = null;
                return (RefKind)ReadInteger();
            }
        }

        private class RemoveAssemblySymbolKeysReader : Reader<object>
        {
            private readonly StringBuilder _builder = new();

            private bool _skipString = false;

            public RemoveAssemblySymbolKeysReader()
            {
            }

            public void Initialize(string data)
                => base.Initialize(data, CancellationToken.None);

            public string RemoveAssemblySymbolKeys()
            {
                this.ReadFormatVersion();

                // read out the language as well, it's not part of any symbol key comparison
                this.SkipString();

                while (Position < Data.Length)
                {
                    var ch = Data[Position];
                    if (ch == OpenParenChar)
                    {
                        _builder.Append(Eat(OpenParenChar));

                        var type = (SymbolKeyType)Data[Position];
                        _builder.Append(Eat(type));

                        if (type == SymbolKeyType.Assembly)
                            SkipString();
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

            private void SkipString()
            {
                Debug.Assert(_skipString == false);
                _skipString = true;

                ReadString();

                Debug.Assert(_skipString == true);
                _skipString = false;
            }

            protected override object? CreateResultForString(int start, int end, bool hasEmbeddedQuote)
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

            protected override object? CreateNullForString()
                => null;
        }

        private delegate T ReadFunction<T>(out string? failureReason);

        private sealed class SymbolKeyReader : Reader<string>
        {
            private static readonly ObjectPool<SymbolKeyReader> s_readerPool = SharedPools.Default<SymbolKeyReader>();

            private readonly Dictionary<int, SymbolKeyResolution> _idToResult = new();
            private readonly ReadFunction<Location?> _readLocation;

            public Compilation Compilation { get; private set; }
            public bool IgnoreAssemblyKey { get; private set; }
            public SymbolEquivalenceComparer Comparer { get; private set; }

            private readonly List<IMethodSymbol?> _methodSymbolStack = new();
            private readonly Stack<ISymbol?> _contextualSymbolStack = new();

            public SymbolKeyReader()
            {
                _readLocation = ReadLocation;

                Compilation = null!;
                Comparer = null!;
            }

            public override void Dispose()
            {
                base.Dispose();
                _idToResult.Clear();
                Compilation = null!;
                IgnoreAssemblyKey = false;
                Comparer = null!;
                _methodSymbolStack.Clear();
                _contextualSymbolStack.Clear();

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

            internal bool ParameterTypesMatch<TOwningSymbol>(
                TOwningSymbol owningSymbol,
                Func<TOwningSymbol, int, ITypeSymbol?> getContextualType,
                ImmutableArray<IParameterSymbol> parameters)
                where TOwningSymbol : ISymbol
            {
                using var originalParameterTypes = this.ReadSymbolKeyArray<TOwningSymbol, ITypeSymbol>(owningSymbol, getContextualType, out _);

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
                        return false;
                }

                return true;
            }

            public MethodPopper PushMethod(IMethodSymbol? method)
            {
                _methodSymbolStack.Add(method);
                return new MethodPopper(this, method);
            }

            private void PopMethod(IMethodSymbol? method)
            {
                Contract.ThrowIfTrue(_methodSymbolStack.Count == 0);
                Contract.ThrowIfFalse(Equals(method, _methodSymbolStack[^1]));
                _methodSymbolStack.RemoveAt(_methodSymbolStack.Count - 1);
            }

            public IMethodSymbol? ResolveMethod(int index)
                => _methodSymbolStack[index];

            public ContextualSymbolPopper PushContextualSymbol(ISymbol? contextualSymbol)
            {
                _contextualSymbolStack.Push(contextualSymbol);
                return new ContextualSymbolPopper(this, contextualSymbol);
            }

            private void PopContextualSymbol(ISymbol? contextualSymbol)
            {
                Contract.ThrowIfTrue(_contextualSymbolStack.Count == 0);
                Contract.ThrowIfFalse(Equals(contextualSymbol, _contextualSymbolStack.Peek()));
                _contextualSymbolStack.Pop();
            }

            public ISymbol? CurrentContextualSymbol
                => _contextualSymbolStack.Count == 0 ? null : _contextualSymbolStack.Peek();

            public readonly ref struct MethodPopper(SymbolKeyReader reader, IMethodSymbol? method)
            {
                public void Dispose()
                    => reader.PopMethod(method);
            }

            public readonly ref struct ContextualSymbolPopper(SymbolKeyReader reader, ISymbol? contextualSymbol)
            {
                public void Dispose()
                    => reader.PopContextualSymbol(contextualSymbol);
            }

            internal SyntaxTree? GetSyntaxTree(string filePath)
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

            public SymbolKeyResolution ReadSymbolKey(ISymbol? contextualSymbol, out string? failureReason)
            {
                CancellationToken.ThrowIfCancellationRequested();
                using var _ = PushContextualSymbol(contextualSymbol);
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

            private SymbolKeyResolution ReadWorker(SymbolKeyType type, out string? failureReason)
                => type switch
                {
                    SymbolKeyType.Alias => AliasSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.BodyLevel => BodyLevelSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.ConstructedMethod => ConstructedMethodSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.NamedType => NamedTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.ErrorType => ErrorTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Field => FieldSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.FunctionPointer => FunctionPointerTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.DynamicType => DynamicTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.BuiltinOperator => BuiltinOperatorSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Method => MethodSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Namespace => NamespaceSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.PointerType => PointerTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Parameter => ParameterSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Property => PropertySymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.ArrayType => ArrayTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Assembly => AssemblySymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.TupleType => TupleTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Module => ModuleSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.Event => EventSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.ReducedExtensionMethod => ReducedExtensionMethodSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.TypeParameter => TypeParameterSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.AnonymousType => AnonymousTypeSymbolKey.Instance.Resolve(this, out failureReason),
                    SymbolKeyType.AnonymousFunctionOrDelegate => AnonymousFunctionOrDelegateSymbolKey.Resolve(this, out failureReason),
                    SymbolKeyType.TypeParameterOrdinal => TypeParameterOrdinalSymbolKey.Resolve(this, out failureReason),
                    _ => throw new NotImplementedException(),
                };

            private PooledArrayBuilder<SymbolKeyResolution> ReadSymbolKeyArray<TContextualSymbol>(
                TContextualSymbol? contextualSymbol,
                Func<TContextualSymbol, int, ISymbol?>? getContextualSymbol,
                out string? failureReason)
            {
                // Keep in Sync with ReadSimpleArray

                var builder = PooledArrayBuilder<SymbolKeyResolution>.GetInstance();
                EatSpace();

                Debug.Assert((SymbolKeyType)Data[Position] != SymbolKeyType.Null);

                EatOpenParen();
                Eat(SymbolKeyType.Array);

                string? totalFailureReason = null;
                var length = ReadInteger();
                for (var i = 0; i < length; i++)
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    var nextContextualSymbol = contextualSymbol is null ? null : getContextualSymbol?.Invoke(contextualSymbol, i);
                    builder.Builder.Add(ReadSymbolKey(nextContextualSymbol, out var elementFailureReason));

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

            /// <summary>
            /// Reads an array of symbols out from the key.  Note: the number of symbols returned will either be the
            /// same as the original amount written, or <c>default</c> will be returned. It will never be less or more.
            /// <c>default</c> will be returned if any elements could not be resolved to the requested <typeparamref
            /// name="TSymbol"/> type in the provided <see cref="SymbolKeyReader.Compilation"/>.
            /// <para>
            /// Callers should <see cref="IDisposable.Dispose"/> the instance returned.  No check is necessary if
            /// <c>default</c> was returned before calling <see cref="IDisposable.Dispose"/>
            /// </para>
            /// </summary>
            /// <remarks>
            /// If <c>default</c> is returned then <paramref name="failureReason"/> will be non-null.  Similarly, if
            /// <paramref name="failureReason"/> is non-null, then only <c>default</c> will be returned.
            /// </remarks>
            public PooledArrayBuilder<TSymbol> ReadSymbolKeyArray<TContextualSymbol, TSymbol>(
                TContextualSymbol? contextualSymbol,
                Func<TContextualSymbol, int, ISymbol?>? getContextualSymbol,
                out string? failureReason)
                where TContextualSymbol : ISymbol
                where TSymbol : ISymbol
            {
                using var resolutions = ReadSymbolKeyArray(
                    contextualSymbol, getContextualSymbol, out var elementsFailureReason);
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
                var substring = Data[start..end];
                var result = hasEmbeddedQuote
                    ? substring.Replace("\"\"", "\"")
                    : substring;
                return result;
            }

            protected override string? CreateNullForString()
                => null;

            #endregion

            #region Locations

            public Location? ReadLocation(out string? failureReason)
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

                    if (filePath == null)
                    {
                        failureReason = $"({nameof(ReadLocation)} failed -> '{nameof(filePath)}' came back null)";
                        return null;
                    }

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
                    var assemblyResolution = ReadSymbolKey(contextualSymbol: null, out var assemblyFailureReason);
                    var moduleName = ReadString();

                    if (assemblyFailureReason != null)
                    {
                        failureReason = $"{nameof(ReadLocation)} {nameof(assemblyResolution)} failed -> " + assemblyFailureReason;
                        return Location.None;
                    }

                    if (moduleName == null)
                    {
                        failureReason = $"({nameof(ReadLocation)} failed -> '{nameof(moduleName)}' came back null)";
                        return null;
                    }

                    // We may be resolving in a compilation where we don't have a module
                    // with this name.  In that case, just map this location to none.
                    if (assemblyResolution.GetAnySymbol() is IAssemblySymbol assembly)
                    {
                        var module = GetModule(assembly.Modules, moduleName);
                        if (module != null)
                        {
                            var location = module.Locations.FirstOrDefault();
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

            private static IModuleSymbol? GetModule(IEnumerable<IModuleSymbol> modules, string moduleName)
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

            public PooledArrayBuilder<Location?> ReadLocationArray(out string? failureReason)
                => ReadSimpleArray(_readLocation, out failureReason);

            #endregion
        }
    }
}
