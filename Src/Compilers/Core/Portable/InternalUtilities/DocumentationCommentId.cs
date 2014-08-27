// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// APIs for constructing documentation comment id's, and finding symbols that match ids.
    /// </summary>
    internal class DocumentationCommentId
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
                ClearAndFree(list);
            }
        }

        private static ListPool<ISymbol> symbolListPool = new ListPool<ISymbol>();
        private static ListPool<INamespaceOrTypeSymbol> namespaceOrTypeListPool = new ListPool<INamespaceOrTypeSymbol>();
        internal static string SuppressionPrefix = "~";

        /// <summary>
        /// Creates an id string used by external documenation comment files to identify declarations
        /// of types, namespaces, methods, properties, etc.
        /// </summary>
        public static string CreateDeclarationId(ISymbol symbol, string prefixOpt = null)
        {
            var builder = new StringBuilder();
            var generator = new DeclarationGenerator(builder);
            generator.Visit(symbol);
            var idString = builder.ToString();
            return prefixOpt != null ? prefixOpt + idString : idString;
        }

        /// <summary>
        /// Creates an id string used to reference type symbols (not strictly declarations, includes
        /// arrays, pointers, type parameters, etc.)
        /// </summary>
        public static string CreateReferenceId(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol)
            {
                return CreateDeclarationId(symbol);
            }

            var builder = new StringBuilder();
            var generator = new ReferenceGenerator(builder, typeParameterContext: null);
            generator.Visit(symbol);
            return builder.ToString();
        }

        /// <summary>
        /// Gets all declaration symbols that match the declaration id string
        /// </summary>
        public static IEnumerable<ISymbol> GetSymbolsForDeclarationId(string id, Compilation compilation, string prefixOpt = null)
        {
            List<ISymbol> results;
            TryGetSymbolsForDeclarationId(id, compilation, out results, prefixOpt);
            return results;
        }

        /// <summary>
        /// Try get all declaration symbols that match the declaration id string
        /// </summary>
        public static bool TryGetSymbolsForDeclarationId(string id, Compilation compilation, out List<ISymbol> results, string prefixOpt = null)
        {
            results = new List<ISymbol>();
            id = HandlePrefix(id, prefixOpt);
            return Parser.ParseDeclaredSymbolId(id, compilation, results);
        }

        private static string HandlePrefix(string id, string prefixOpt)
        {
            if (prefixOpt != null)
            {
                if (id == null || !id.StartsWith(prefixOpt))
                {
                    return null;
                }
                else
                {
                    return id.Substring(prefixOpt.Length);
                }
            }

            return id;
        }

        /// <summary>
        /// Gets the first declaration symbol that matches the declaration id string, order undefined.
        /// </summary>
        public static ISymbol GetFirstSymbolForDeclarationId(string id, Compilation compilation)
        {
            var results = symbolListPool.Allocate();
            try
            {
                Parser.ParseDeclaredSymbolId(id, compilation, results);
                if (results.Count == 0)
                {
                    return null;
                }
                else
                {
                    return results[0];
                }
            }
            finally
            {
                symbolListPool.ClearAndFree(results);
            }
        }

        /// <summary>
        /// Gets the symbols that match the reference id string.
        /// </summary>
        public static IEnumerable<ISymbol> GetSymbolsForReferenceId(string id, Compilation compilation)
        {
            if (id.Length > 1 && id[0] == 'N' && id[1] == ':')
            {
                return GetSymbolsForDeclarationId(id, compilation);
            }
            else
            {
                var results = new List<ISymbol>();
                Parser.ParseReferencedSymbolId(id, compilation, results);
                return results;
            }
        }

        /// <summary>
        /// Gets the first symbol that matches the reference id string, order undefined.
        /// </summary>
        public static ISymbol GetFirstSymbolForReferenceId(string id, Compilation compilation)
        {
            if (id.Length > 1 && id[0] == 'N' && id[1] == ':')
            {
                return GetFirstSymbolForDeclarationId(id, compilation);
            }
            else
            {
                var results = symbolListPool.Allocate();
                try
                {
                    Parser.ParseReferencedSymbolId(id, compilation, results);
                    if (results.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return results[0];
                    }
                }
                finally
                {
                    symbolListPool.ClearAndFree(results);
                }
            }
        }

        private static int GetTotalTypeParameterCount(INamedTypeSymbol symbol)
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
            else
            {
                return name;
            }
        }

        private class DeclarationGenerator : SymbolVisitor
        {
            private StringBuilder builder;
            private Generator generator;

            public DeclarationGenerator(StringBuilder builder)
            {
                this.builder = builder;
                this.generator = new Generator(builder);
            }

            public override void DefaultVisit(ISymbol symbol)
            {
                throw new InvalidOperationException("Cannot generated a documentation comment id for symbol.");
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                this.builder.Append("E:");
                this.generator.Visit(symbol);
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                this.builder.Append("F:");
                this.generator.Visit(symbol);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                this.builder.Append("P:");
                this.generator.Visit(symbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                this.builder.Append("M:");
                this.generator.Visit(symbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                this.builder.Append("N:");
                this.generator.Visit(symbol);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                this.builder.Append("T:");
                this.generator.Visit(symbol);
            }

            private class Generator : SymbolVisitor<bool>
            {
                private StringBuilder builder;
                private ReferenceGenerator referenceGenerator;

                public Generator(StringBuilder builder)
                {
                    this.builder = builder;
                }

                private ReferenceGenerator GetReferenceGenerator(ISymbol typeParameterContext)
                {
                    if (this.referenceGenerator == null || this.referenceGenerator.TypeParameterContext != typeParameterContext)
                    {
                        this.referenceGenerator = new ReferenceGenerator(this.builder, typeParameterContext);
                    }

                    return this.referenceGenerator;
                }

                public override bool DefaultVisit(ISymbol symbol)
                {
                    throw new InvalidOperationException("Cannot generated a documentation comment id for symbol.");
                }

                public override bool VisitEvent(IEventSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        this.builder.Append(".");
                    }

                    this.builder.Append(EncodeName(symbol.Name));
                    return true;
                }

                public override bool VisitField(IFieldSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        this.builder.Append(".");
                    }

                    this.builder.Append(EncodeName(symbol.Name));
                    return true;
                }

                public override bool VisitProperty(IPropertySymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        this.builder.Append(".");
                    }

                    if (symbol.Name == "this[]")
                    {
                        this.builder.Append("Item");
                    }
                    else
                    {
                        this.builder.Append(EncodeName(symbol.Name));
                    }

                    return true;
                }

                public override bool VisitMethod(IMethodSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        this.builder.Append(".");
                        this.builder.Append(EncodeName(symbol.Name));
                    }

                    if (symbol.TypeParameters.Length > 0)
                    {
                        this.builder.Append("``");
                        this.builder.Append(symbol.TypeParameters.Length);
                    }

                    if (symbol.Parameters.Length > 0)
                    {
                        this.builder.Append("(");

                        for (int i = 0, n = symbol.Parameters.Length; i < n; i++)
                        {
                            if (i > 0)
                            {
                                this.builder.Append(",");
                            }

                            var p = symbol.Parameters[i];
                            this.GetReferenceGenerator(symbol).Visit(p.Type);
                            if (p.RefKind != RefKind.None)
                            {
                                this.builder.Append("@");
                            }
                        }

                        this.builder.Append(")");
                    }

                    if (!symbol.ReturnsVoid)
                    {
                        this.builder.Append("~");
                        this.GetReferenceGenerator(symbol).Visit(symbol.ReturnType);
                    }

                    return true;
                }

                public override bool VisitNamespace(INamespaceSymbol symbol)
                {
                    if (symbol.IsGlobalNamespace)
                    {
                        return false;
                    }
                    else
                    {
                        if (this.Visit(symbol.ContainingSymbol))
                        {
                            this.builder.Append(".");
                        }

                        this.builder.Append(EncodeName(symbol.Name));
                        return true;
                    }
                }

                public override bool VisitNamedType(INamedTypeSymbol symbol)
                {
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        this.builder.Append(".");
                    }

                    this.builder.Append(EncodeName(symbol.Name));

                    if (symbol.TypeParameters.Length > 0)
                    {
                        this.builder.Append("`");
                        this.builder.Append(symbol.TypeParameters.Length);
                    }

                    return true;
                }
            }
        }

        private class ReferenceGenerator : SymbolVisitor<bool>
        {
            private readonly StringBuilder builder;
            private readonly ISymbol typeParameterContext;

            public ReferenceGenerator(StringBuilder builder, ISymbol typeParameterContext)
            {
                this.builder = builder;
                this.typeParameterContext = typeParameterContext;
            }

            public ISymbol TypeParameterContext
            {
                get { return this.typeParameterContext; }
            }

            private void BuildDottedName(ISymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    this.builder.Append(".");
                }

                this.builder.Append(EncodeName(symbol.Name));
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
                else
                {
                    this.BuildDottedName(symbol);
                    return true;
                }
            }

            public override bool VisitNamedType(INamedTypeSymbol symbol)
            {
                this.BuildDottedName(symbol);

                if (symbol.IsGenericType)
                {
                    if (symbol.OriginalDefinition == symbol)
                    {
                        this.builder.Append("`");
                        this.builder.Append(symbol.TypeParameters.Length);
                    }
                    else if (symbol.TypeArguments.Length > 0)
                    {
                        this.builder.Append("{");

                        for (int i = 0, n = symbol.TypeArguments.Length; i < n; i++)
                        {
                            var arg = symbol.TypeArguments[i];

                            if (i > 0)
                            {
                                this.builder.Append(",");
                            }

                            this.Visit(symbol.TypeArguments[i]);
                        }

                        this.builder.Append("}");
                    }
                }

                return true;
            }

            public override bool VisitArrayType(IArrayTypeSymbol symbol)
            {
                this.Visit(symbol.ElementType);

                this.builder.Append("[");

                for (int i = 0, n = symbol.Rank; i < n; i++)
                {
                    // TODO: bounds info goes here

                    if (i > 0)
                    {
                        this.builder.Append(",");
                    }
                }

                this.builder.Append("]");

                return true;
            }

            public override bool VisitPointerType(IPointerTypeSymbol symbol)
            {
                this.Visit(symbol.PointedAtType);
                this.builder.Append("*");
                return true;
            }

            public override bool VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (!IsInScope(symbol))
                {
                    // reference to type parameter not in scope, make explicit scope reference
                    var declarer = new DeclarationGenerator(this.builder);
                    declarer.Visit(symbol.ContainingSymbol);
                    this.builder.Append(":");
                }

                if (symbol.DeclaringMethod != null)
                {
                    this.builder.Append("``");
                    this.builder.Append(symbol.Ordinal);
                }
                else
                {
                    // get count of all type parameter preceding the declaration of the type parameters containing symbol.
                    var container = symbol.ContainingSymbol != null ? symbol.ContainingSymbol.ContainingSymbol : null;
                    var b = GetTotalTypeParameterCount(container as INamedTypeSymbol);
                    this.builder.Append("`");
                    this.builder.Append(b + symbol.Ordinal);
                }

                return true;
            }

            private bool IsInScope(ITypeParameterSymbol typeParameterSymbol)
            {
                // determine if the type parameter is declared in scope defined by the typeParameterContext symbol
                var typeParameterDeclarer = typeParameterSymbol.ContainingSymbol;

                for (var scope = this.typeParameterContext; scope != null; scope = scope.ContainingSymbol)
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
                return results.Count != 0;
            }

            // only supports type symbols
            public static bool ParseReferencedSymbolId(string id, Compilation compilation, List<ISymbol> results)
            {
                int index = 0;
                results.Clear();
                ParseTypeSymbol(id, ref index, compilation, null, results);
                return results.Count != 0;
            }

            private static void ParseDeclaredId(string id, ref int index, Compilation compilation, List<ISymbol> results)
            {
                var kindChar = PeekNextChar(id, index);
                SymbolKind kind = default(SymbolKind);

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

                var containers = namespaceOrTypeListPool.Allocate();
                try
                {
                    containers.Add(compilation.GlobalNamespace);

                    string name;
                    int arity = 0;

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
                                GetMatchingTypes(containers, name, arity, results);
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
                            GetMatchingTypes(containers, name, arity, results);
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
                    namespaceOrTypeListPool.ClearAndFree(containers);
                }
            }

            private static ITypeSymbol ParseTypeSymbol(string id, ref int index, Compilation compilation, ISymbol typeParameterContext)
            {
                var results = symbolListPool.Allocate();
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
                    symbolListPool.ClearAndFree(results);
                }
            }

            private static void ParseTypeSymbol(string id, ref int index, Compilation compilation, ISymbol typeParameterContext, List<ISymbol> results)
            {
                var ch = PeekNextChar(id, index);

                // context expression embedded in reference => <context-definition>:<type-parameter>
                // note: this is a deviation from the language spec
                if ((ch == 'M' || ch == 'T') && PeekNextChar(id, index + 1) == ':')
                {
                    var contexts = symbolListPool.Allocate();
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
                        symbolListPool.ClearAndFree(contexts);
                    }
                }
                else
                {
                    if (ch == '`')
                    {
                        ParseTypeParameterSymbol(id, ref index, compilation, typeParameterContext, results);
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
                            else if (PeekNextChar(id, index) == '*')
                            {
                                index++;
                                typeSymbol = compilation.CreatePointerTypeSymbol(typeSymbol);
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }

                        results[i] = typeSymbol;
                        endIndex = index;
                    }

                    index = endIndex;
                }
            }

            private static void ParseTypeParameterSymbol(string id, ref int index, Compilation compilation, ISymbol typeParameterContext, List<ISymbol> results)
            {
                var startIndex = index;

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

                    if (typeContext != null)
                    {
                        results.Add(GetNthTypeParameter(typeContext, typeParameterIndex));
                    }
                }
            }

            private static void ParseNamedTypeSymbol(string id, ref int index, Compilation compilation, ISymbol typeParameterContext, List<ISymbol> results)
            {
                var containers = namespaceOrTypeListPool.Allocate();
                try
                {
                    containers.Add(compilation.GlobalNamespace);

                    // loop for dotted names
                    while (true)
                    {
                        var name = ParseName(id, ref index);

                        List<ITypeSymbol> typeArguments = null;
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
                            GetMatchingTypes(containers, name, arity, results);

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
                        else
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    namespaceOrTypeListPool.ClearAndFree(containers);
                }
            }

            private static int ParseArrayBounds(string id, ref int index)
            {
                index++;  // skip '['

                int bounds = 0;

                while (true)
                {
                    int lowerBound = 0;
                    int size = -1;

                    if (char.IsDigit(PeekNextChar(id, index)))
                    {
                        lowerBound = ReadNextInteger(id, ref index);
                    }

                    if (PeekNextChar(id, index) == ':')
                    {
                        index++;

                        if (char.IsDigit(PeekNextChar(id, index)))
                        {
                            size = ReadNextInteger(id, ref index);
                        }
                    }

                    bounds++;

                    if (PeekNextChar(id, index) == ',')
                    {
                        index++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (PeekNextChar(id, index) == ']')
                {
                    index++;
                }

                return bounds;
            }

            private static bool ParseTypeArguments(string id, ref int index, Compilation compilation, ISymbol typeParameterContext, List<ITypeSymbol> typeArguments)
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
                    else
                    {
                        break;
                    }
                }

                if (PeekNextChar(id, index) == '}')
                {
                    index++;
                }

                return true;
            }

            private static void GetMatchingTypes(List<INamespaceOrTypeSymbol> containers, string memberName, int arity, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    GetMatchingTypes(containers[i], memberName, arity, results);
                }
            }

            private static void GetMatchingTypes(INamespaceOrTypeSymbol container, string memberName, int arity, List<ISymbol> results)
            {
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
                var parameters = parameterListPool.Allocate();
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
                                if (parameters != null)
                                {
                                    parameters.Clear();
                                }

                                if (PeekNextChar(id, index) == '(')
                                {
                                    if (!ParseParameterList(id, ref index, compilation, methodSymbol, parameters))
                                    {
                                        // if the parameters cannot be identified (some error), then the symbol cannot match, try next method symbol
                                        continue;
                                    }
                                }

                                if (parameters == null)
                                {
                                    if (methodSymbol.Parameters.Length != 0)
                                    {
                                        // parameters don't match, try next method symbol
                                        continue;
                                    }
                                }
                                else if (!AllParametersMatch(id, methodSymbol.Parameters, parameters, compilation))
                                {
                                    // parameters don't match, try next method symbol
                                    continue;
                                }

                                ITypeSymbol returnType = null;
                                if (PeekNextChar(id, index) == '~')
                                {
                                    index++;
                                    returnType = ParseTypeSymbol(id, ref index, compilation, methodSymbol);

                                    // if return type is specified, then it must match
                                    if (returnType != null && methodSymbol.ReturnType.Equals(returnType))
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
                    parameterListPool.ClearAndFree(parameters);
                }
            }

            private static void GetMatchingProperties(string id, ref int index, List<INamespaceOrTypeSymbol> containers, string memberName, Compilation compilation, List<ISymbol> results)
            {
                int startIndex = index;
                int endIndex = index;

                List<ParameterInfo> parameters = null;
                try
                {
                    for (int i = 0, n = containers.Count; i < n; i++)
                    {
                        // special case, csharp names indexers 'this[]', not 'Item'
                        if (memberName == "Item"
                            && compilation.Language == LanguageNames.CSharp)
                        {
                            memberName = "this[]";
                        }

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
                                        parameters = parameterListPool.Allocate();
                                    }
                                    else
                                    {
                                        parameters.Clear();
                                    }

                                    if (ParseParameterList(id, ref index, compilation, propertySymbol.ContainingSymbol, parameters)
                                        && AllParametersMatch(id, propertySymbol.Parameters, parameters, compilation))
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
                        parameterListPool.ClearAndFree(parameters);
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

            private static bool AllParametersMatch(string id, ImmutableArray<IParameterSymbol> symbolParameters, List<ParameterInfo> expectedParameters, Compilation compilation)
            {
                if (symbolParameters.Length != expectedParameters.Count)
                {
                    return false;
                }

                for (int i = 0; i < expectedParameters.Count; i++)
                {
                    if (!ParameterMatches(id, symbolParameters[i], expectedParameters[i], compilation))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool ParameterMatches(string id, IParameterSymbol symbol, ParameterInfo parameterInfo, Compilation compilation)
            {
                // same ref'ness?
                if ((symbol.RefKind == RefKind.None) != !parameterInfo.IsRefOrOut)
                {
                    return false;
                }

                var methodSymbol = symbol.ContainingSymbol as IMethodSymbol;
                var parameterType = parameterInfo.Type;

                return parameterType != null && symbol.Type.Equals(parameterType);
            }

            private static ITypeParameterSymbol GetNthTypeParameter(INamedTypeSymbol typeSymbol, int n)
            {
                var containingTypeParameterCount = GetTypeParameterCount(typeSymbol.ContainingType as INamedTypeSymbol);
                if (n < containingTypeParameterCount)
                {
                    return GetNthTypeParameter(typeSymbol.ContainingType as INamedTypeSymbol, n);
                }
                else
                {
                    var index = n - containingTypeParameterCount;
                    return typeSymbol.TypeParameters[index];
                }
            }

            private static int GetTypeParameterCount(INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol == null)
                {
                    return 0;
                }
                else
                {
                    return typeSymbol.TypeParameters.Length + GetTypeParameterCount(typeSymbol.ContainingType as INamedTypeSymbol);
                }
            }

            private struct ParameterInfo
            {
                internal readonly ITypeSymbol Type;
                internal bool IsRefOrOut;

                public ParameterInfo(ITypeSymbol type, bool isRefOrOut)
                {
                    this.Type = type;
                    this.IsRefOrOut = isRefOrOut;
                }
            }

            private static ListPool<ParameterInfo> parameterListPool = new ListPool<ParameterInfo>();

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

            private static ParameterInfo? ParseParameter(string id, ref int index, Compilation compilation, ISymbol typeParameterContext)
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
                return (index >= id.Length) ? '\0' : id[index];
            }

            private static readonly char[] nameDelimiters = new char[] { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~' };

            private static string ParseName(string id, ref int index)
            {
                string name;

                int delimiterOffset = id.IndexOfAny(nameDelimiters, index);
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
                else
                {
                    return name;
                }
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
