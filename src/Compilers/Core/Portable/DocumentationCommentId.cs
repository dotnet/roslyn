// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// APIs for constructing documentation comment id's, and finding symbols that match ids.
    /// </summary>
    public static class DocumentationCommentId
    {
        private class ListPool<T> : ObjectPool<List<T>>
        {
            public ListPool()
                : base(() => new List<T>(10), 10)
            { }

            public void ClearAndFree(List<T> list)
            {
                list.Clear();
                base.Free(list);
            }

            [Obsolete("Do not use Free, Use ClearAndFree instead.", error: true)]
            public new void Free(List<T> list)
            {
                throw new NotSupportedException();
            }
        }

        private static readonly ListPool<ISymbol> s_symbolListPool = new ListPool<ISymbol>();
        private static readonly ListPool<INamespaceOrTypeSymbol> s_namespaceOrTypeListPool = new ListPool<INamespaceOrTypeSymbol>();

        /// <summary>
        /// Creates an id string used by external documentation comment files to identify declarations of types,
        /// namespaces, methods, properties, etc.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="symbol"/> is <see langword="null"/>.</exception>
        /// <returns>The documentation comment Id for this symbol, if it can be created. <see langword="null"/> if it cannot be.</returns>
        public static string? CreateDeclarationId(ISymbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            var builder = PooledStringBuilder.GetInstance();
            var generator = new PrefixAndDeclarationGenerator(builder);
            generator.Visit(symbol);

            return generator.Failed ? null : builder.ToStringAndFree();
        }

        /// <summary>
        /// Creates an id string used to reference type symbols (not strictly declarations, includes arrays, pointers,
        /// type parameters, etc.).
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="symbol"/> is <see langword="null"/>.</exception>
        public static string CreateReferenceId(ISymbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (symbol is INamespaceSymbol)
            {
                // This is very odd.  For anything other than a namespace, we defer to ReferenceGenerator, which just
                // spits out a reference.  But for a namespace, we spit out a declaration (which means we prefix with
                // `N:`).  None of the other paths do this.  This is likely a bug, but it appears as if it has never
                // been noticed.
                var result = CreateDeclarationId(symbol);

                // All namespaces should succeed at being converted into a declaration ID.
                RoslynDebug.AssertNotNull(result);
                return result;
            }

            var builder = PooledStringBuilder.GetInstance();
            var generator = new ReferenceGenerator(builder, typeParameterContext: null);
            generator.Visit(symbol);
            return builder.ToStringAndFree();
        }

        /// <summary>
        /// Gets all declaration symbols that match the declaration id string
        /// </summary>
        public static ImmutableArray<ISymbol> GetSymbolsForDeclarationId(string id, Compilation compilation)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var results = s_symbolListPool.Allocate();
            try
            {
                Parser.ParseDeclaredSymbolId(id, compilation, results);
                return results.ToImmutableArray();
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
            }
        }

        /// <summary>
        /// Try to get all the declaration symbols that match the declaration id string.
        /// Returns true if at least one symbol matches.
        /// </summary>
        private static bool TryGetSymbolsForDeclarationId(string id, Compilation compilation, List<ISymbol> results)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            return Parser.ParseDeclaredSymbolId(id, compilation, results);
        }

        /// <summary>
        /// Gets the first declaration symbol that matches the declaration id string, order undefined.
        /// </summary>
        public static ISymbol? GetFirstSymbolForDeclarationId(string id, Compilation compilation)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var results = s_symbolListPool.Allocate();
            try
            {
                Parser.ParseDeclaredSymbolId(id, compilation, results);
                return results.Count == 0 ? null : results[0];
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
            }
        }

        /// <summary>
        /// Gets the symbols that match the reference id string.
        /// </summary>
        public static ImmutableArray<ISymbol> GetSymbolsForReferenceId(string id, Compilation compilation)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var results = s_symbolListPool.Allocate();
            try
            {
                TryGetSymbolsForReferenceId(id, compilation, results);
                return results.ToImmutableArray();
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
            }
        }

        /// <summary>
        /// Try to get all symbols that match the reference id string.
        /// Returns true if at least one symbol matches.
        /// </summary>
        private static bool TryGetSymbolsForReferenceId(string id, Compilation compilation, List<ISymbol> results)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (id.Length > 1 && id[0] == 'N' && id[1] == ':')
            {
                return TryGetSymbolsForDeclarationId(id, compilation, results);
            }

            return Parser.ParseReferencedSymbolId(id, compilation, results);
        }

        /// <summary>
        /// Gets the first symbol that matches the reference id string, order undefined.
        /// </summary>
        public static ISymbol? GetFirstSymbolForReferenceId(string id, Compilation compilation)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (id.Length > 1 && id[0] == 'N' && id[1] == ':')
            {
                return GetFirstSymbolForDeclarationId(id, compilation);
            }

            var results = s_symbolListPool.Allocate();
            try
            {
                Parser.ParseReferencedSymbolId(id, compilation, results);
                return results.Count == 0 ? null : results[0];
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
            }
        }

        private static int GetTotalTypeParameterCount(INamedTypeSymbol? symbol)
        {
            int n = 0;
            while (symbol != null)
            {
                n += symbol.TypeParameters.Length;
                symbol = symbol.ContainingSymbol as INamedTypeSymbol;
            }

            return n;
        }

        // encodes dots with alternate # character
        private static string EncodeName(string name)
        {
            if (name.IndexOf('.') >= 0)
            {
                return name.Replace('.', '#');
            }

            return name;
        }

        private static string EncodePropertyName(string name)
        {
            // convert C# indexer names to 'Item'
            if (name == "this[]")
            {
                name = "Item";
            }
            else if (name.EndsWith(".this[]"))
            {
                name = name.Substring(0, name.Length - 6) + "Item";
            }

            return name;
        }

        private static string DecodePropertyName(string name, string language)
        {
            // special case, csharp names indexers 'this[]', not 'Item'
            if (language == LanguageNames.CSharp)
            {
                if (name == "Item")
                {
                    name = "this[]";
                }
                else if (name.EndsWith(".Item"))
                {
                    name = name.Substring(0, name.Length - 4) + "this[]";
                }
            }

            return name;
        }

        /// <summary>
        /// Callers should only call into <see cref="SymbolVisitor{TResult}.Visit(ISymbol?)"/> and should check <see
        /// cref="Failed"/> to see if it failed (in the case of an arbitrary symbol) or that it produced an expected
        /// value (in the case a known symbol type was used).
        /// </summary>
        /// <remarks>
        /// This will always succeed for a <see cref="INamespaceSymbol"/> or <see cref="INamedTypeSymbol"/>.  It may not
        /// succeed for other symbols.
        /// <para/> Once used, an instance of this visitor should be discarded.  Specifically it is stateful, and will
        /// stay in the failed state once it transitions there.
        /// </remarks>
        private sealed class PrefixAndDeclarationGenerator : SymbolVisitor
        {
            private readonly StringBuilder _builder;
            private readonly DeclarationGenerator _generator;

            private bool _failed;

            public PrefixAndDeclarationGenerator(StringBuilder builder)
            {
                _builder = builder;
                _generator = new DeclarationGenerator(builder);
            }

            /// <summary>
            /// If we hit anything we don't know about, indicate failure.
            /// </summary>
            public override void DefaultVisit(ISymbol symbol)
            {
                _failed = true;
            }

            public bool Failed => _failed || _generator.Failed;

            public override void VisitEvent(IEventSymbol symbol)
            {
                _builder.Append("E:");
                _generator.Visit(symbol);
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                _builder.Append("F:");
                _generator.Visit(symbol);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                _builder.Append("P:");
                _generator.Visit(symbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                _builder.Append("M:");
                _generator.Visit(symbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                _builder.Append("N:");
                _generator.Visit(symbol);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                _builder.Append("T:");
                _generator.Visit(symbol);

                if (symbol.IsExtension)
                {
                    _builder.Append('.');
                    _builder.Append(symbol.ExtensionMarkerName);
                }
            }

            private sealed class DeclarationGenerator : SymbolVisitor<bool>
            {
                private readonly StringBuilder _builder;
                private ReferenceGenerator? _referenceGenerator;

                public bool Failed;

                public DeclarationGenerator(StringBuilder builder)
                {
                    _builder = builder;
                }

                private ReferenceGenerator GetReferenceGenerator(ISymbol typeParameterContext)
                {
                    if (_referenceGenerator == null || _referenceGenerator.TypeParameterContext != typeParameterContext)
                    {
                        _referenceGenerator = new ReferenceGenerator(_builder, typeParameterContext);
                    }

                    return _referenceGenerator;
                }

                /// <summary>
                /// If we hit anything we don't know about, indicate failure.
                /// </summary>
                public override bool DefaultVisit(ISymbol symbol)
                {
                    Failed = true;
                    return true;
                }

                public override bool VisitEvent(IEventSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                    }

                    _builder.Append(EncodeName(symbol.Name));
                    return true;
                }

                public override bool VisitField(IFieldSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                    }

                    _builder.Append(EncodeName(symbol.Name));
                    return true;
                }

                public override bool VisitProperty(IPropertySymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                    }

                    var name = EncodePropertyName(symbol.Name);
                    _builder.Append(EncodeName(name));

                    AppendParameters(symbol.Parameters);

                    return true;
                }

                public override bool VisitMethod(IMethodSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                        _builder.Append(EncodeName(symbol.Name));
                    }

                    if (symbol.TypeParameters.Length > 0)
                    {
                        _builder.Append("``");
                        _builder.Append(symbol.TypeParameters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    AppendParameters(symbol.Parameters);

                    if (!symbol.ReturnsVoid)
                    {
                        _builder.Append('~');
                        this.GetReferenceGenerator(symbol).Visit(symbol.ReturnType);
                    }

                    return true;
                }

                private void AppendParameters(ImmutableArray<IParameterSymbol> parameters)
                {
                    if (parameters.Length > 0)
                    {
                        _builder.Append('(');

                        for (int i = 0, n = parameters.Length; i < n; i++)
                        {
                            if (i > 0)
                            {
                                _builder.Append(',');
                            }

                            var p = parameters[i];
                            this.GetReferenceGenerator(p.ContainingSymbol!).Visit(p.Type);
                            if (p.RefKind != RefKind.None)
                            {
                                _builder.Append('@');
                            }
                        }

                        _builder.Append(')');
                    }
                }

                public override bool VisitNamespace(INamespaceSymbol symbol)
                {
                    if (symbol.IsGlobalNamespace)
                    {
                        return false;
                    }

                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                    }

                    _builder.Append(EncodeName(symbol.Name));
                    return true;
                }

                public override bool VisitNamedType(INamedTypeSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append('.');
                    }

                    _builder.Append(EncodeName(symbol.IsExtension ? symbol.ExtensionGroupingName : symbol.Name));

                    if (symbol.TypeParameters.Length > 0)
                    {
                        _builder.Append('`');
                        _builder.Append(symbol.TypeParameters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    return true;
                }
            }
        }

        private class ReferenceGenerator : SymbolVisitor<bool>
        {
            private readonly StringBuilder _builder;
            private readonly ISymbol? _typeParameterContext;

            public ReferenceGenerator(StringBuilder builder, ISymbol? typeParameterContext)
            {
                _builder = builder;
                _typeParameterContext = typeParameterContext;
            }

            public ISymbol? TypeParameterContext
            {
                get { return _typeParameterContext; }
            }

            private void BuildDottedName(ISymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append('.');
                }

                if (symbol is INamedTypeSymbol { IsExtension: true } extension)
                {
                    _builder.Append(EncodeName(extension.ExtensionGroupingName));
                }
                else
                {
                    _builder.Append(EncodeName(symbol.Name));
                }
            }

            public override bool VisitAlias(IAliasSymbol symbol)
            {
                return symbol.Target.Accept(this);
            }

            public override bool VisitNamespace(INamespaceSymbol symbol)
            {
                if (symbol.IsGlobalNamespace)
                {
                    return false;
                }

                this.BuildDottedName(symbol);
                return true;
            }

            public override bool VisitNamedType(INamedTypeSymbol symbol)
            {
                this.BuildDottedName(symbol);
                AppendArityOrTypeArguments(symbol);

                if (symbol.IsExtension)
                {
                    _builder.Append('.');
                    _builder.Append(symbol.ExtensionMarkerName);
                }

                return true;
            }

            private void AppendArityOrTypeArguments(INamedTypeSymbol symbol)
            {
                if (symbol.IsGenericType)
                {
                    if (symbol.OriginalDefinition == symbol)
                    {
                        _builder.Append('`');
                        _builder.Append(symbol.TypeParameters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if (symbol.TypeArguments.Length > 0)
                    {
                        _builder.Append('{');

                        for (int i = 0, n = symbol.TypeArguments.Length; i < n; i++)
                        {
                            if (i > 0)
                            {
                                _builder.Append(',');
                            }

                            this.Visit(symbol.TypeArguments[i]);
                        }

                        _builder.Append('}');
                    }
                }
            }

            public override bool VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _builder.Append("System.Object");

                return true;
            }

            public override bool VisitArrayType(IArrayTypeSymbol symbol)
            {
                this.Visit(symbol.ElementType);

                _builder.Append('[');

                for (int i = 0, n = symbol.Rank; i < n; i++)
                {
                    // TODO: bounds info goes here

                    if (i > 0)
                    {
                        _builder.Append(',');
                    }
                }

                _builder.Append(']');

                return true;
            }

            public override bool VisitPointerType(IPointerTypeSymbol symbol)
            {
                this.Visit(symbol.PointedAtType);
                _builder.Append('*');
                return true;
            }

            public override bool VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (!IsInScope(symbol))
                {
                    // reference to type parameter not in scope, make explicit scope reference

                    // Containing symbol may be null in error cases.
                    Debug.Assert(symbol.ContainingSymbol is null or INamedTypeSymbol or IMethodSymbol);
                    var declarer = new PrefixAndDeclarationGenerator(_builder);
                    declarer.Visit(symbol.ContainingSymbol);
                    Debug.Assert(!declarer.Failed);

                    _builder.Append(':');
                }

                if (symbol.DeclaringMethod != null)
                {
                    _builder.Append("``");
                    _builder.Append(symbol.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    // get count of all type parameter preceding the declaration of the type parameters containing symbol.
                    var container = symbol.ContainingSymbol?.ContainingSymbol;
                    var b = GetTotalTypeParameterCount(container as INamedTypeSymbol);
                    _builder.Append('`');
                    _builder.Append((b + symbol.Ordinal).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                return true;
            }

            private bool IsInScope(ITypeParameterSymbol typeParameterSymbol)
            {
                // determine if the type parameter is declared in scope defined by the typeParameterContext symbol
                var typeParameterDeclarer = typeParameterSymbol.ContainingSymbol;

                for (var scope = _typeParameterContext; scope != null; scope = scope.ContainingSymbol)
                {
                    if (scope == typeParameterDeclarer)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static class Parser
        {
            public static bool ParseDeclaredSymbolId(string id, Compilation compilation, List<ISymbol> results)
            {
                if (id == null)
                {
                    return false;
                }

                if (id.Length < 2)
                {
                    return false;
                }

                int index = 0;
                results.Clear();
                ParseDeclaredId(id, ref index, compilation, results);
                return results.Count > 0;
            }

            // only supports type symbols
            public static bool ParseReferencedSymbolId(string id, Compilation compilation, List<ISymbol> results)
            {
                if (id == null)
                {
                    return false;
                }

                int index = 0;
                results.Clear();
                ParseTypeSymbol(id, ref index, compilation, null, results);
                return results.Count > 0;
            }

            private static void ParseDeclaredId(string id, ref int index, Compilation compilation, List<ISymbol> results)
            {
                var kindChar = PeekNextChar(id, index);
                SymbolKind kind;

                switch (kindChar)
                {
                    case 'E':
                        kind = SymbolKind.Event;
                        break;
                    case 'F':
                        kind = SymbolKind.Field;
                        break;
                    case 'M':
                        kind = SymbolKind.Method;
                        break;
                    case 'N':
                        kind = SymbolKind.Namespace;
                        break;
                    case 'P':
                        kind = SymbolKind.Property;
                        break;
                    case 'T':
                        kind = SymbolKind.NamedType;
                        break;
                    default:
                        // Documentation comment id must start with E, F, M, N, P or T
                        return;
                }

                index++;
                if (PeekNextChar(id, index) == ':')
                {
                    index++;
                }

                var containers = s_namespaceOrTypeListPool.Allocate();
                try
                {
                    containers.Add(compilation.GlobalNamespace);

                    string name;
                    int arity;

                    // process dotted names
                    while (true)
                    {
                        name = ParseName(id, ref index);
                        arity = 0;

                        // has type parameters?
                        if (PeekNextChar(id, index) == '`')
                        {
                            index++;

                            // method type parameters?
                            if (PeekNextChar(id, index) == '`')
                            {
                                index++;
                            }

                            arity = ReadNextInteger(id, ref index);
                        }

                        if (PeekNextChar(id, index) == '.')
                        {
                            // must be a namespace or type since name continues after dot
                            index++;

                            if (arity > 0)
                            {
                                // only types have arity
                                GetMatchingTypes(containers, name, arity, isTerminal: false, results);
                            }
                            else if (kind == SymbolKind.Namespace)
                            {
                                // if the results kind is namespace, then all dotted names must be namespaces
                                GetMatchingNamespaces(containers, name, results);
                            }
                            else
                            {
                                // could be either
                                GetMatchingNamespaceOrTypes(containers, name, results);
                            }

                            if (results.Count == 0)
                            {
                                // no matches found before dot, cannot continue.
                                return;
                            }

                            // results become the new containers
                            containers.Clear();
                            containers.AddRange(results.OfType<INamespaceOrTypeSymbol>());
                            results.Clear();
                        }
                        else
                        {
                            // no more dots, so don't loop any more
                            break;
                        }
                    }

                    switch (kind)
                    {
                        case SymbolKind.Method:
                            GetMatchingMethods(id, ref index, containers, name, arity, compilation, results);
                            break;
                        case SymbolKind.NamedType:
                            GetMatchingTypes(containers, name, arity, isTerminal: true, results);
                            break;
                        case SymbolKind.Property:
                            GetMatchingProperties(id, ref index, containers, name, compilation, results);
                            break;
                        case SymbolKind.Event:
                            GetMatchingEvents(containers, name, results);
                            break;
                        case SymbolKind.Field:
                            GetMatchingFields(containers, name, results);
                            break;
                        case SymbolKind.Namespace:
                            GetMatchingNamespaces(containers, name, results);
                            break;
                    }
                }
                finally
                {
                    s_namespaceOrTypeListPool.ClearAndFree(containers);
                }
            }

            private static ITypeSymbol? ParseTypeSymbol(string id, ref int index, Compilation compilation, ISymbol? typeParameterContext)
            {
                var results = s_symbolListPool.Allocate();
                try
                {
                    ParseTypeSymbol(id, ref index, compilation, typeParameterContext, results);
                    if (results.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return (ITypeSymbol)results[0];
                    }
                }
                finally
                {
                    s_symbolListPool.ClearAndFree(results);
                }
            }

            private static void ParseTypeSymbol(string id, ref int index, Compilation compilation, ISymbol? typeParameterContext, List<ISymbol> results)
            {
                var ch = PeekNextChar(id, index);

                // context expression embedded in reference => <context-definition>:<type-parameter>
                // note: this is a deviation from the language spec
                if ((ch == 'M' || ch == 'T') && PeekNextChar(id, index + 1) == ':')
                {
                    var contexts = s_symbolListPool.Allocate();
                    try
                    {
                        ParseDeclaredId(id, ref index, compilation, contexts);
                        if (contexts.Count == 0)
                        {
                            // context cannot be bound, so abort
                            return;
                        }

                        if (PeekNextChar(id, index) == ':')
                        {
                            index++;

                            // try parsing following in all contexts
                            var startIndex = index;
                            foreach (var context in contexts)
                            {
                                index = startIndex;
                                ParseTypeSymbol(id, ref index, compilation, context, results);
                            }
                        }
                        else
                        {
                            // this was a definition where we expected a reference?
                            results.AddRange(contexts.OfType<ITypeSymbol>());
                        }
                    }
                    finally
                    {
                        s_symbolListPool.ClearAndFree(contexts);
                    }
                }
                else
                {
                    if (ch == '`')
                    {
                        ParseTypeParameterSymbol(id, ref index, typeParameterContext, results);
                    }
                    else
                    {
                        ParseNamedTypeSymbol(id, ref index, compilation, typeParameterContext, results);
                    }

                    // apply any array or pointer constructions to results
                    var startIndex = index;
                    var endIndex = index;

                    for (int i = 0; i < results.Count; i++)
                    {
                        index = startIndex;
                        var typeSymbol = (ITypeSymbol)results[i];

                        while (true)
                        {
                            if (PeekNextChar(id, index) == '[')
                            {
                                var bounds = ParseArrayBounds(id, ref index);
                                typeSymbol = compilation.CreateArrayTypeSymbol(typeSymbol, bounds);
                                continue;
                            }

                            if (PeekNextChar(id, index) == '*')
                            {
                                index++;
                                typeSymbol = compilation.CreatePointerTypeSymbol(typeSymbol);
                                continue;
                            }

                            break;
                        }

                        results[i] = typeSymbol;
                        endIndex = index;
                    }

                    index = endIndex;
                }
            }

            private static void ParseTypeParameterSymbol(string id, ref int index, ISymbol? typeParameterContext, List<ISymbol> results)
            {
                // skip the first `
                System.Diagnostics.Debug.Assert(PeekNextChar(id, index) == '`');
                index++;

                if (PeekNextChar(id, index) == '`')
                {
                    // `` means this is a method type parameter
                    index++;
                    var methodTypeParameterIndex = ReadNextInteger(id, ref index);

                    var methodContext = typeParameterContext as IMethodSymbol;
                    if (methodContext != null)
                    {
                        var count = methodContext.TypeParameters.Length;
                        if (count > 0 && methodTypeParameterIndex < count)
                        {
                            results.Add(methodContext.TypeParameters[methodTypeParameterIndex]);
                        }
                    }
                }
                else
                {
                    // regular type parameter
                    var typeParameterIndex = ReadNextInteger(id, ref index);

                    var methodContext = typeParameterContext as IMethodSymbol;
                    var typeContext = methodContext != null ? methodContext.ContainingType : typeParameterContext as INamedTypeSymbol;

                    if (typeContext != null && GetNthTypeParameter(typeContext, typeParameterIndex) is { } typeParameter)
                    {
                        results.Add(typeParameter);
                    }
                }
            }

            private static void ParseNamedTypeSymbol(string id, ref int index, Compilation compilation, ISymbol? typeParameterContext, List<ISymbol> results)
            {
                var containers = s_namespaceOrTypeListPool.Allocate();
                try
                {
                    containers.Add(compilation.GlobalNamespace);

                    // loop for dotted names
                    while (true)
                    {
                        var name = ParseName(id, ref index);

                        List<ITypeSymbol>? typeArguments = null;
                        int arity = 0;

                        // type arguments
                        if (PeekNextChar(id, index) == '{')
                        {
                            typeArguments = new List<ITypeSymbol>();
                            if (!ParseTypeArguments(id, ref index, compilation, typeParameterContext, typeArguments))
                            {
                                // if no type arguments are found then the type cannot be identified
                                continue;
                            }

                            arity = typeArguments.Count;
                        }
                        else if (PeekNextChar(id, index) == '`')
                        {
                            index++;
                            arity = ReadNextInteger(id, ref index);
                        }

                        if (arity != 0 || PeekNextChar(id, index) != '.')
                        {
                            GetMatchingTypes(containers, name, arity, isTerminal: false, results);

                            if (arity != 0 && typeArguments != null && typeArguments.Count != 0)
                            {
                                var typeArgs = typeArguments.ToArray();
                                for (int i = 0; i < results.Count; i++)
                                {
                                    results[i] = ((INamedTypeSymbol)results[i]).Construct(typeArgs);
                                }
                            }
                        }
                        else
                        {
                            GetMatchingNamespaceOrTypes(containers, name, results);
                        }

                        if (PeekNextChar(id, index) == '.')
                        {
                            index++;
                            containers.Clear();
                            CopyTo(results, containers);
                            results.Clear();
                            continue;
                        }

                        break;
                    }
                }
                finally
                {
                    s_namespaceOrTypeListPool.ClearAndFree(containers);
                }
            }

            private static int ParseArrayBounds(string id, ref int index)
            {
                index++;  // skip '['

                int bounds = 0;

                while (true)
                {
                    if (char.IsDigit(PeekNextChar(id, index)))
                    {
                        ReadNextInteger(id, ref index);
                    }

                    if (PeekNextChar(id, index) == ':')
                    {
                        index++;

                        if (char.IsDigit(PeekNextChar(id, index)))
                        {
                            ReadNextInteger(id, ref index);
                        }
                    }

                    bounds++;

                    if (PeekNextChar(id, index) == ',')
                    {
                        index++;
                        continue;
                    }

                    break;
                }

                if (PeekNextChar(id, index) == ']')
                {
                    index++;
                }

                return bounds;
            }

            private static bool ParseTypeArguments(string id, ref int index, Compilation compilation, ISymbol? typeParameterContext, List<ITypeSymbol> typeArguments)
            {
                index++; // skip over {

                while (true)
                {
                    var type = ParseTypeSymbol(id, ref index, compilation, typeParameterContext);

                    if (type == null)
                    {
                        // if a type argument cannot be identified, argument list is no good
                        return false;
                    }

                    // add first one
                    typeArguments.Add(type);

                    if (PeekNextChar(id, index) == ',')
                    {
                        index++;
                        continue;
                    }

                    break;
                }

                if (PeekNextChar(id, index) == '}')
                {
                    index++;
                }

                return true;
            }

            private static void GetMatchingTypes(List<INamespaceOrTypeSymbol> containers, string memberName, int arity, bool isTerminal, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    GetMatchingTypes(containers[i], memberName, arity, isTerminal: isTerminal, results);
                }
            }

            /// <param name="isTerminal">Indicates that we're looking at the last segment in a dotted chain.
            /// If we're in terminal position, we need to recognize the extension marker name so that
            /// `ContainingType.ExtensionGroupingName.ExtensionMarkerName` can be matched to the extension type.
            /// </param>
            private static void GetMatchingTypes(INamespaceOrTypeSymbol container, string memberName, int arity, bool isTerminal, List<ISymbol> results)
            {
                if (isTerminal
                    && container is INamedTypeSymbol { IsExtension: true } extension
                    && extension.ExtensionMarkerName == memberName
                    && arity == 0)
                {
                    results.Add(extension);
                    return;
                }

                var members = container.GetMembers(memberName);

                foreach (var symbol in members)
                {
                    if (symbol.Kind == SymbolKind.NamedType)
                    {
                        var namedType = (INamedTypeSymbol)symbol;
                        if (namedType.Arity == arity)
                        {
                            results.Add(namedType);
                        }
                    }
                }

                GetMatchingExtensions(container, memberName, arity, results);
            }

            private static void GetMatchingNamespaceOrTypes(List<INamespaceOrTypeSymbol> containers, string memberName, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    GetMatchingNamespaceOrTypes(containers[i], memberName, results);
                }
            }

            private static void GetMatchingNamespaceOrTypes(INamespaceOrTypeSymbol container, string memberName, List<ISymbol> results)
            {
                var members = container.GetMembers(memberName);

                foreach (var symbol in members)
                {
                    if (symbol.Kind == SymbolKind.Namespace || (symbol.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)symbol).Arity == 0))
                    {
                        results.Add(symbol);
                    }
                }

                GetMatchingExtensions(container, memberName, arity: 0, results);
            }

            private static void GetMatchingExtensions(INamespaceOrTypeSymbol container, string memberName, int arity, List<ISymbol> results)
            {
                if (container.IsNamespace)
                {
                    return;
                }

                ImmutableArray<INamedTypeSymbol> unnamedNamedTypes = container.GetTypeMembers("");
                foreach (var namedType in unnamedNamedTypes)
                {
                    if (namedType.IsExtension
                        && namedType.Arity == arity
                        && namedType.ExtensionGroupingName == memberName)
                    {
                        results.Add(namedType);
                    }
                }
            }

            private static void GetMatchingNamespaces(List<INamespaceOrTypeSymbol> containers, string memberName, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    GetMatchingNamespaces(containers[i], memberName, results);
                }
            }

            private static void GetMatchingNamespaces(INamespaceOrTypeSymbol container, string memberName, List<ISymbol> results)
            {
                var members = container.GetMembers(memberName);

                foreach (var symbol in members)
                {
                    if (symbol.Kind == SymbolKind.Namespace)
                    {
                        results.Add(symbol);
                    }
                }
            }

            private static void GetMatchingMethods(string id, ref int index, List<INamespaceOrTypeSymbol> containers, string memberName, int arity, Compilation compilation, List<ISymbol> results)
            {
                var parameters = s_parameterListPool.Allocate();
                try
                {
                    var startIndex = index;
                    var endIndex = index;

                    for (int i = 0, n = containers.Count; i < n; i++)
                    {
                        var members = containers[i].GetMembers(memberName);

                        foreach (var symbol in members)
                        {
                            index = startIndex;

                            var methodSymbol = symbol as IMethodSymbol;
                            if (methodSymbol != null && methodSymbol.Arity == arity)
                            {
                                parameters.Clear();

                                if (PeekNextChar(id, index) == '(')
                                {
                                    if (!ParseParameterList(id, ref index, compilation, methodSymbol, parameters))
                                    {
                                        // if the parameters cannot be identified (some error), then the symbol cannot match, try next method symbol
                                        continue;
                                    }
                                }

                                if (!AllParametersMatch(methodSymbol.Parameters, parameters))
                                {
                                    // parameters don't match, try next method symbol
                                    continue;
                                }

                                if (PeekNextChar(id, index) == '~')
                                {
                                    index++;
                                    ITypeSymbol? returnType = ParseTypeSymbol(id, ref index, compilation, methodSymbol);

                                    // if return type is specified, then it must match
                                    if (returnType != null && methodSymbol.ReturnType.Equals(returnType, SymbolEqualityComparer.CLRSignature))
                                    {
                                        // return type matches
                                        results.Add(methodSymbol);
                                        endIndex = index;
                                    }
                                }
                                else
                                {
                                    // no return type specified, then any matches
                                    results.Add(methodSymbol);
                                    endIndex = index;
                                }
                            }
                        }
                    }

                    index = endIndex;
                }
                finally
                {
                    s_parameterListPool.ClearAndFree(parameters);
                }
            }

            private static void GetMatchingProperties(string id, ref int index, List<INamespaceOrTypeSymbol> containers, string memberName, Compilation compilation, List<ISymbol> results)
            {
                int startIndex = index;
                int endIndex = index;

                List<ParameterInfo>? parameters = null;
                try
                {
                    for (int i = 0, n = containers.Count; i < n; i++)
                    {
                        memberName = DecodePropertyName(memberName, compilation.Language);
                        var members = containers[i].GetMembers(memberName);

                        foreach (var symbol in members)
                        {
                            index = startIndex;

                            var propertySymbol = symbol as IPropertySymbol;
                            if (propertySymbol != null)
                            {
                                if (PeekNextChar(id, index) == '(')
                                {
                                    if (parameters == null)
                                    {
                                        parameters = s_parameterListPool.Allocate();
                                    }
                                    else
                                    {
                                        parameters.Clear();
                                    }

                                    if (ParseParameterList(id, ref index, compilation, propertySymbol.ContainingSymbol!, parameters)
                                        && AllParametersMatch(propertySymbol.Parameters, parameters))
                                    {
                                        results.Add(propertySymbol);
                                        endIndex = index;
                                    }
                                }
                                else if (propertySymbol.Parameters.Length == 0)
                                {
                                    results.Add(propertySymbol);
                                    endIndex = index;
                                }
                            }
                        }
                    }

                    index = endIndex;
                }
                finally
                {
                    if (parameters != null)
                    {
                        s_parameterListPool.ClearAndFree(parameters);
                    }
                }
            }

            private static void GetMatchingFields(List<INamespaceOrTypeSymbol> containers, string memberName, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var members = containers[i].GetMembers(memberName);

                    foreach (var symbol in members)
                    {
                        if (symbol.Kind == SymbolKind.Field)
                        {
                            results.Add(symbol);
                        }
                    }
                }
            }

            private static void GetMatchingEvents(List<INamespaceOrTypeSymbol> containers, string memberName, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var members = containers[i].GetMembers(memberName);

                    foreach (var symbol in members)
                    {
                        if (symbol.Kind == SymbolKind.Event)
                        {
                            results.Add(symbol);
                        }
                    }
                }
            }

            private static bool AllParametersMatch(ImmutableArray<IParameterSymbol> symbolParameters, List<ParameterInfo> expectedParameters)
            {
                if (symbolParameters.Length != expectedParameters.Count)
                {
                    return false;
                }

                for (int i = 0; i < expectedParameters.Count; i++)
                {
                    if (!ParameterMatches(symbolParameters[i], expectedParameters[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool ParameterMatches(IParameterSymbol symbol, ParameterInfo parameterInfo)
            {
                // same ref'ness?
                if ((symbol.RefKind == RefKind.None) == parameterInfo.IsRefOrOut)
                {
                    return false;
                }

                var parameterType = parameterInfo.Type;

                return parameterType != null && symbol.Type.Equals(parameterType, SymbolEqualityComparer.CLRSignature);
            }

            private static ITypeParameterSymbol? GetNthTypeParameter(INamedTypeSymbol typeSymbol, int n)
            {
                var containingType = typeSymbol.ContainingType;
                var containingTypeParameterCount = GetTypeParameterCount(containingType);
                if (n < containingTypeParameterCount && containingType is not null)
                {
                    return GetNthTypeParameter(containingType, n);
                }

                var index = n - containingTypeParameterCount;
                var typeParameters = typeSymbol.TypeParameters;
                if (index < typeParameters.Length)
                {
                    return typeParameters[index];
                }

                return null;
            }

            private static int GetTypeParameterCount(INamedTypeSymbol? typeSymbol)
            {
                if (typeSymbol == null)
                {
                    return 0;
                }

                return typeSymbol.TypeParameters.Length + GetTypeParameterCount(typeSymbol.ContainingType);
            }

            [StructLayout(LayoutKind.Auto)]
            private readonly struct ParameterInfo
            {
                internal readonly ITypeSymbol Type;
                internal readonly bool IsRefOrOut;

                public ParameterInfo(ITypeSymbol type, bool isRefOrOut)
                {
                    this.Type = type;
                    this.IsRefOrOut = isRefOrOut;
                }
            }

            private static readonly ListPool<ParameterInfo> s_parameterListPool = new ListPool<ParameterInfo>();

            private static bool ParseParameterList(string id, ref int index, Compilation compilation, ISymbol typeParameterContext, List<ParameterInfo> parameters)
            {
                System.Diagnostics.Debug.Assert(typeParameterContext != null);

                index++; // skip over '('

                if (PeekNextChar(id, index) == ')')
                {
                    index++;
                    return true;
                }

                var parameter = ParseParameter(id, ref index, compilation, typeParameterContext);
                if (parameter == null)
                {
                    return false;
                }

                parameters.Add(parameter.Value);

                while (PeekNextChar(id, index) == ',')
                {
                    index++;

                    parameter = ParseParameter(id, ref index, compilation, typeParameterContext);
                    if (parameter == null)
                    {
                        return false;
                    }

                    parameters.Add(parameter.Value);
                }

                if (PeekNextChar(id, index) == ')')
                {
                    index++;
                }

                return true;
            }

            private static ParameterInfo? ParseParameter(string id, ref int index, Compilation compilation, ISymbol? typeParameterContext)
            {
                bool isRefOrOut = false;

                var type = ParseTypeSymbol(id, ref index, compilation, typeParameterContext);

                if (type == null)
                {
                    // if no type can be identified, then there is no parameter
                    return null;
                }

                if (PeekNextChar(id, index) == '@')
                {
                    index++;
                    isRefOrOut = true;
                }

                return new ParameterInfo(type, isRefOrOut);
            }

            private static char PeekNextChar(string id, int index)
            {
                return index >= id.Length ? '\0' : id[index];
            }

            private static readonly char[] s_nameDelimiters = { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~' };

            private static string ParseName(string id, ref int index)
            {
                string name;

                int delimiterOffset = id.IndexOfAny(s_nameDelimiters, index);
                if (delimiterOffset >= 0)
                {
                    name = id.Substring(index, delimiterOffset - index);
                    index = delimiterOffset;
                }
                else
                {
                    name = id.Substring(index);
                    index = id.Length;
                }

                return DecodeName(name);
            }

            // undoes dot encodings within names...
            private static string DecodeName(string name)
            {
                if (name.IndexOf('#') >= 0)
                {
                    return name.Replace('#', '.');
                }

                return name;
            }

            private static int ReadNextInteger(string id, ref int index)
            {
                int n = 0;

                while (index < id.Length && char.IsDigit(id[index]))
                {
                    n = n * 10 + (id[index] - '0');
                    index++;
                }

                return n;
            }

            private static void CopyTo<TSource, TDestination>(List<TSource> source, List<TDestination> destination)
                where TSource : class
                where TDestination : class
            {
                if (destination.Count + source.Count > destination.Capacity)
                {
                    destination.Capacity = destination.Count + source.Count;
                }

                for (int i = 0, n = source.Count; i < n; i++)
                {
                    destination.Add((TDestination)(object)source[i]);
                }
            }
        }
    }
}
