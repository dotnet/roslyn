using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
            private readonly Func<int> _readInteger;
            private readonly Func<RefKind> _readRefKind;

            protected string Data { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public int Position;

            public Reader()
            {
                _readString = ReadString;
                _readInteger = ReadInteger;
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
                CancellationToken = default(CancellationToken);
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
                    return default(ImmutableArray<T>);
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

        private abstract class Reader<TSymbolResult, TStringResult> : Reader<TStringResult>
        {
            private readonly Dictionary<int, TSymbolResult> _idToResult = new Dictionary<int, TSymbolResult>();
            private readonly Func<TSymbolResult> _readSymbolKey;

            public Reader()
            {
                _readSymbolKey = ReadSymbolKey;
            }

            protected override void Initialize(string data, CancellationToken cancellationToken)
            {
                base.Initialize(data, cancellationToken);
            }

            public override void Dispose()
            {
                base.Dispose();
                _idToResult.Clear();
            }

            public TSymbolResult ReadFirstSymbolKey()
            {
                return ReadSymbolKeyWorker(first: true);
            }

            public TSymbolResult ReadSymbolKey()
            {
                return ReadSymbolKeyWorker(first: false);
            }

            private TSymbolResult ReadSymbolKeyWorker(bool first)
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
                    return default(TSymbolResult);
                }

                EatOpenParen();
                TSymbolResult result;

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

            protected abstract TSymbolResult ReadWorker(SymbolKeyType type);

            public ImmutableArray<TSymbolResult> ReadSymbolKeyArray()
            {
                return ReadArray(_readSymbolKey);
            }
        }

        private class GetHashCodeReader : Reader<int, int>
        {
            private static readonly ObjectPool<GetHashCodeReader> s_pool =
                new ObjectPool<GetHashCodeReader>(() => new GetHashCodeReader());

            private GetHashCodeReader()
            {
            }

            public static GetHashCodeReader GetReader(string data)
            {
                var reader = s_pool.Allocate();
                reader.Initialize(data);
                return reader;
            }

            public override void Dispose()
            {
                base.Dispose();

                s_pool.Free(this);
            }

            private void Initialize(string data)
            {
                base.Initialize(data, CancellationToken.None);
            }

            protected override int CreateResultForString(int start, int end, bool hasEmbeddedQuote)
            {
                var result = 1;

                // Note: we hash all strings to lowercase. It will mean more collisions, but
                // it provides a uniform hashing strategy for all keys.
                for (var i = start; i < end; i++)
                {
                    result = Hash.Combine((int)char.ToLower(Data[i]), result);
                }
                return result;
            }

            protected override int CreateNullForString()
            {
                return 0;
            }

            public int ReadSymbolKeyArrayHashCode()
            {
                return HashArray(ReadSymbolKeyArray());
            }

            public int ReadRefKindArrayHashCode()
            {
                var array = ReadRefKindArray();
                var value = 1;
                if (!array.IsDefault)
                {
                    foreach (var v in array)
                    {
                        value = Hash.Combine((int)v, value);
                    }
                }
                return value;
            }

            private static int HashArray(ImmutableArray<int> array)
            {
                var value = 1;
                if (!array.IsDefault)
                {
                    foreach (var v in array)
                    {
                        value = Hash.Combine(value, v);
                    }
                }
                return value;
            }

            protected override int ReadWorker(SymbolKeyType type)
            {
                switch (type)
                {
                    case SymbolKeyType.Alias: return AliasSymbolKey.GetHashCode(this);
                    case SymbolKeyType.BodyLevel: return BodyLevelSymbolKey.GetHashCode(this);
                    case SymbolKeyType.ConstructedMethod: return ConstructedMethodSymbolKey.GetHashCode(this);
                    case SymbolKeyType.NamedType: return NamedTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.ErrorType: return ErrorTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Field: return FieldSymbolKey.GetHashCode(this);
                    case SymbolKeyType.DynamicType: return DynamicTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Method: return MethodSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Namespace: return NamespaceSymbolKey.GetHashCode(this);
                    case SymbolKeyType.PointerType: return PointerTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Parameter: return ParameterSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Property: return PropertySymbolKey.GetHashCode(this);
                    case SymbolKeyType.ArrayType: return ArrayTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Assembly: return AssemblySymbolKey.GetHashCode(this);
                    case SymbolKeyType.TupleType: return TupleTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Module: return ModuleSymbolKey.GetHashCode(this);
                    case SymbolKeyType.Event: return EventSymbolKey.GetHashCode(this);
                    case SymbolKeyType.ReducedExtensionMethod: return ReducedExtensionMethodSymbolKey.GetHashCode(this);
                    case SymbolKeyType.TypeParameter: return TypeParameterSymbolKey.GetHashCode(this);
                    case SymbolKeyType.AnonymousType: return AnonymousTypeSymbolKey.GetHashCode(this);
                    case SymbolKeyType.TypeParameterOrdinal: return TypeParameterOrdinalSymbolKey.GetHashCode(this);
                }

                throw new NotImplementedException();
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
            {
                base.Initialize(data, CancellationToken.None);
            }

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
                        // All ther characters we pass along directly to the string builder.
                        _builder.Append(Eat(ch));
                    }
                }

                return _builder.ToString();
            }

            protected override object CreateResultForString(int start, int end, bool hasEmbeddedQuote)
            {
                // 'start' is right after the open quote, and 'end' is right before the close quote.
                // However, we want to include both quotes in teh result.
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

        private class SymbolKeyReader : Reader<SymbolKeyResolution, string>
        {
            private static readonly ObjectPool<SymbolKeyReader> s_readerPool =
                new ObjectPool<SymbolKeyReader>(() => new SymbolKeyReader());

            public Compilation Compilation { get; private set; }
            public bool IgnoreAssemblyKey { get; private set; }
            public SymbolEquivalenceComparer Comparer { get; private set; }

            public IMethodSymbol CurrentMethod;

            private SymbolKeyReader()
            {
            }

            public static SymbolKeyReader GetReader(
                string data, Compilation compilation,
                bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var reader = s_readerPool.Allocate();
                reader.Initialize(data, compilation, ignoreAssemblyKey, cancellationToken);
                return reader;
            }

            public override void Dispose()
            {
                base.Dispose();
                Compilation = null;
                IgnoreAssemblyKey = false;
                Comparer = null;
                CurrentMethod = null;

                // Place us back in the pool for future use.
                s_readerPool.Free(this);
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

            protected override SymbolKeyResolution ReadWorker(SymbolKeyType type)
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
                    case SymbolKeyType.TypeParameterOrdinal: return TypeParameterOrdinalSymbolKey.Resolve(this);
                }

                throw new NotImplementedException();
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
        }
    }
}