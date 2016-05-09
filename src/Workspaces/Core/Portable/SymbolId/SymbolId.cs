// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// APIs for constructing and resolving ids for <see cref="ISymbol"/>
    /// </summary>
    internal static class SymbolId
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

        /// <summary>
        /// Creates an id string used to identify symbols.
        /// </summary>
        public static string CreateId(ISymbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            var builder = new StringBuilder();
            var generator = new Generator(builder, typeParameterContext: null);
            generator.Visit(symbol);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the symbols that match the id string in the compilation.
        /// </summary>
        public static ImmutableArray<ISymbol> GetSymbolsForId(string id, Compilation compilation)
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
                TryGetSymbolsForId(id, compilation, results);
                return results.ToImmutableArray();
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
            }
        }

        /// <summary>
        /// Try to get all symbols that match the id string in the compilation.
        /// Returns true if at least one symbol matches.
        /// </summary>
        private static bool TryGetSymbolsForId(string id, Compilation compilation, List<ISymbol> results)
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

            return Parser.Parse(id, compilation, results);
        }

        /// <summary>
        /// Gets the first symbol that matches the id string in the compilation, order undefined.
        /// </summary>
        public static ISymbol GetFirstSymbolForId(string id, Compilation compilation)
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
                Parser.Parse(id, compilation, results);
                return results.Count == 0 ? null : results[0];
            }
            finally
            {
                s_symbolListPool.ClearAndFree(results);
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

        private const char AliasPrefix = 'A';
        private const char EventPrefix = 'E';
        private const char FieldPrefix = 'F';
        private const char MethodPrefix = 'M';
        private const char ParameterPrefix = 'K';
        private const char PropertyPrefix = 'P';
        private const char NamedTypePrefix = 'T';
        private const char ErrorTypePrefix = 'Z';
        private const char LocalPrefix = 'V';
        private const char LabelPrefix = 'L';
        private const char NamespacePrefix = 'N';
        private const char RangeVariablePrefix = 'R';

        private struct Generator
        {
            private readonly StringBuilder _builder;
            private readonly ISymbol _typeParameterContext;

            public Generator(StringBuilder builder, ISymbol typeParameterContext)
            {
                _builder = builder;
                _typeParameterContext = typeParameterContext;
            }

            public bool Visit(ISymbol symbol)
            {
                if (symbol == null)
                {
                    return false;
                }

                switch (symbol.Kind)
                {
                    case SymbolKind.Alias:
                        return this.VisitAlias((IAliasSymbol)symbol);
                    case SymbolKind.ArrayType:
                        return this.VisitArrayType((IArrayTypeSymbol)symbol);
                    case SymbolKind.Event:
                        return this.VisitEvent((IEventSymbol)symbol);
                    case SymbolKind.Field:
                        return this.VisitField((IFieldSymbol)symbol);
                    case SymbolKind.Method:
                        return this.VisitMethod((IMethodSymbol)symbol);
                    case SymbolKind.NamedType:
                        return this.VisitNamedType((INamedTypeSymbol)symbol);
                    case SymbolKind.Namespace:
                        return this.VisitNamespace((INamespaceSymbol)symbol);
                    case SymbolKind.Parameter:
                        return this.VisitParameter((IParameterSymbol)symbol);
                    case SymbolKind.PointerType:
                        return this.VisitPointerType((IPointerTypeSymbol)symbol);
                    case SymbolKind.Property:
                        return this.VisitProperty((IPropertySymbol)symbol);
                    case SymbolKind.TypeParameter:
                        return this.VisitTypeParameter((ITypeParameterSymbol)symbol);
                    case SymbolKind.ErrorType:
                        return this.VisitErrorType((IErrorTypeSymbol)symbol);
                    case SymbolKind.Local:
                        return this.VisitLocal((ILocalSymbol)symbol);
                    case SymbolKind.Label:
                        return this.VisitLabel((ILabelSymbol)symbol);
                    case SymbolKind.RangeVariable:
                        return this.VisitRangeVariable((IRangeVariableSymbol)symbol);
                    case SymbolKind.DynamicType:
                        return this.VisitDynamicType((IDynamicTypeSymbol)symbol);

                    case SymbolKind.Assembly:
                    case SymbolKind.NetModule:
                    case SymbolKind.Preprocessing:
                    default:
                        throw new InvalidOperationException("Cannot generated a symbol id for symbol.");
                }
            }

            private bool VisitAlias(IAliasSymbol symbol)
            {
                var sref = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (sref != null)
                {
                    _builder.Append("A:");
                    _builder.Append(symbol.Name);
                    _builder.Append("'");
                    _builder.Append(sref.SyntaxTree.FilePath);
                    _builder.Append("'");
                }

                return true;
            }

            private bool VisitEvent(IEventSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(EventPrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));
                return true;
            }

            private bool VisitField(IFieldSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(FieldPrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));
                return true;
            }

            private bool VisitProperty(IPropertySymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(PropertyPrefix);
                _builder.Append(":");
                var name = EncodePropertyName(symbol.Name);
                _builder.Append(EncodeName(name));

                EncodeParameters(symbol.Parameters);

                return true;
            }

            private bool VisitMethod(IMethodSymbol symbol)
            {
                if (symbol.MethodKind == MethodKind.ReducedExtension)
                {
                    EncodeMethod(symbol.GetConstructedReducedFrom());
                    _builder.Append("!");
                    this.Visit(symbol.ReceiverType);
                }
                else
                {
                    EncodeMethod(symbol);
                }

                return true;
            }

            private void EncodeMethod(IMethodSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(MethodPrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));

                EncodeGenericMethodInfo(symbol);

                EncodeParameters(symbol.Parameters);

// return types are not use for overloading in C# and VB
#if false
                if (!symbol.ReturnsVoid)
                {
                    _builder.Append("~");
                    new Generator(_builder, symbol).Visit(symbol.ReturnType);
                }
#endif
            }

            private void EncodeGenericMethodInfo(IMethodSymbol symbol)
            {
                if (symbol.IsGenericMethod)
                {
                    if (object.ReferenceEquals(symbol.OriginalDefinition, symbol))
                    {
                        if (symbol.TypeParameters.Length > 0)
                        {
                            _builder.Append("`");
                            _builder.Append(symbol.TypeParameters.Length);
                        }
                    }
                    else if (symbol.TypeArguments.Length > 0)
                    {
                        _builder.Append("{");

                        for (int i = 0, n = symbol.TypeArguments.Length; i < n; i++)
                        {
                            if (i > 0)
                            {
                                _builder.Append(",");
                            }

                            new Generator(_builder, symbol.ConstructedFrom).Visit(symbol.TypeArguments[i]);
                        }

                        _builder.Append("}");
                    }
                }
            }

            private void EncodeParameters(ImmutableArray<IParameterSymbol> parameters)
            {
                if (parameters.Length > 0)
                {
                    _builder.Append("(");

                    for (int i = 0, n = parameters.Length; i < n; i++)
                    {
                        if (i > 0)
                        {
                            _builder.Append(",");
                        }

                        var p = parameters[i];
                        new Generator(_builder, p.ContainingSymbol).Visit(p.Type);
                        if (p.RefKind != RefKind.None)
                        {
                            _builder.Append("@");
                        }
                    }

                    _builder.Append(")");
                }
            }

            private bool VisitParameter(IParameterSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(ParameterPrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));

                return true;
            }

            private bool VisitNamespace(INamespaceSymbol symbol)
            {
                if (symbol.IsGlobalNamespace)
                {
                    return false;
                }

                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(NamespacePrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));
                return true;
            }

            private bool VisitNamedType(INamedTypeSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(NamedTypePrefix);
                _builder.Append(":");
                _builder.Append(EncodeName(symbol.Name));

                EncodeGenericTypeInfo(symbol);

                return true;
            }

            private void EncodeGenericTypeInfo(INamedTypeSymbol symbol)
            {
                if (symbol.IsGenericType)
                {
                    if (symbol.OriginalDefinition == symbol)
                    {
                        _builder.Append("`");
                        _builder.Append(symbol.TypeParameters.Length);
                    }
                    else if (symbol.TypeArguments.Length > 0)
                    {
                        _builder.Append("{");

                        for (int i = 0, n = symbol.TypeArguments.Length; i < n; i++)
                        {
                            if (i > 0)
                            {
                                _builder.Append(",");
                            }

                            this.Visit(symbol.TypeArguments[i]);
                        }

                        _builder.Append("}");
                    }
                }
            }

            private bool VisitErrorType(IErrorTypeSymbol symbol)
            {
                _builder.Append(ErrorTypePrefix);
                _builder.Append(":");
                _builder.Append(symbol.Name);

                EncodeGenericTypeInfo(symbol);

                return true;
            }

            private bool VisitArrayType(IArrayTypeSymbol symbol)
            {
                this.Visit(symbol.ElementType);

                _builder.Append("[");

                for (int i = 0, n = symbol.Rank; i < n; i++)
                {
                    if (i > 0)
                    {
                        _builder.Append(",");
                    }
                }

                _builder.Append("]");

                return true;
            }

            private bool VisitPointerType(IPointerTypeSymbol symbol)
            {
                this.Visit(symbol.PointedAtType);
                _builder.Append("*");
                return true;
            }

            private bool VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (!IsInScope(symbol))
                {
                    // reference to type parameter not in scope, make explicit scope reference
                    if (this.Visit(symbol.ContainingSymbol))
                    {
                        _builder.Append(":");
                    }
                }

                if (symbol.DeclaringMethod != null)
                {
                    _builder.Append("``");
                    _builder.Append(symbol.Ordinal);
                }
                else
                {
                    // get count of all type parameter preceding the declaration of the type parameters containing symbol.
                    var container = symbol.ContainingSymbol?.ContainingSymbol;
                    var b = GetTotalTypeParameterCount(container as INamedTypeSymbol);
                    _builder.Append("`");
                    _builder.Append(b + symbol.Ordinal);
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

            private bool VisitLocal(ILocalSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(LocalPrefix);
                _builder.Append(":");
                _builder.Append(symbol.Name);

                EncodeOccurrence(symbol);

                return true;
            }

            private bool VisitLabel(ILabelSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(LabelPrefix);
                _builder.Append(":");
                _builder.Append(symbol.Name);

                EncodeOccurrence(symbol);

                return true;
            }

            private bool VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                if (this.Visit(symbol.ContainingSymbol))
                {
                    _builder.Append(".");
                }

                _builder.Append(RangeVariablePrefix);
                _builder.Append(":");
                _builder.Append(symbol.Name);

                EncodeOccurrence(symbol);
                return true;
            }

            private void EncodeOccurrence(ISymbol symbol)
            {
                var occurrence = GetInteriorSymbolOccurrence(symbol);
                if (occurrence > 0)
                {
                    _builder.Append("`");
                    _builder.Append(occurrence);
                }
            }

            public bool VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _builder.Append("*");
                return true;
            }
        }

        private static int GetInteriorSymbolOccurrence(ISymbol symbol)
        {
            var symbols = s_symbolListPool.Allocate();
            try
            {
                int n = 0;

                GetInteriorSymbols(symbol.ContainingSymbol, symbols);

                foreach (var sym in symbols)
                {
                    if (sym.Kind == symbol.Kind && sym.Name == symbol.Name)
                    {
                        if (sym.Equals(symbol))
                        {
                            return n;
                        }

                        // sym.Equals(symbol) not working for locals of empty anonymous types?
                        // since this code is only called on source that originated symbol, then spans should match
                        if (sym.DeclaringSyntaxReferences.Length > 0 
                            && symbol.DeclaringSyntaxReferences.Length > 0
                            && sym.DeclaringSyntaxReferences[0].Span == symbol.DeclaringSyntaxReferences[0].Span)
                        {
                            return n;
                        }

                        n++;
                    }
                }

                return n;
            }
            finally
            {
                s_symbolListPool.ClearAndFree(symbols);
            }
        }

        private static ISymbol GetMatchingInteriorSymbol(ISymbol containingSymbol, SymbolKind kind, string name, int occurrence)
        {
            var symbols = s_symbolListPool.Allocate();
            try
            {
                int n = 0;

                GetInteriorSymbols(containingSymbol, symbols);

                foreach (var sym in symbols)
                {
                    if (sym.Kind == kind && sym.Name == name)
                    {
                        if (occurrence == n)
                        {
                            return sym;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }

                return null;
            }
            finally
            {
                s_symbolListPool.ClearAndFree(symbols);
            }
        }

        private static void GetInteriorSymbols(ISymbol containingSymbol, List<ISymbol> symbols)
        {
            var compilation = (containingSymbol.ContainingAssembly as ISourceAssemblySymbol)?.Compilation;
            if (compilation != null)
            {
                foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
                {
                    var node = declaringLocation.GetSyntax();
                    if (node.Language == LanguageNames.VisualBasic)
                    {
                        node = node.Parent;
                    }

                    var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                    GetDeclaredSymbols(semanticModel, node, symbols);
                }
            }
        }

        private static ISymbol GetMatchingDeclaredSymbol(SemanticModel model, SyntaxNode root, SymbolKind kind, string name, int occurrence)
        {
            var symbols = s_symbolListPool.Allocate();
            try
            {
                int n = 0;

                GetDeclaredSymbols(model, root, symbols);

                foreach (var sym in symbols)
                {
                    if (sym.Kind == kind && sym.Name == name)
                    {
                        if (occurrence == n)
                        {
                            return sym;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }

                return null;
            }
            finally
            {
                s_symbolListPool.ClearAndFree(symbols);
            }
        }

        private static void GetDeclaredSymbols(SemanticModel model, SyntaxNode root, List<ISymbol> symbols)
        {
            // use shotgun approach to identify all declared symbols..  
            // TODO: make an API on semantic model that does this efficiently
            foreach (var node in root.DescendantNodes())
            {
                var symbol = model.GetDeclaredSymbol(node);
                if (symbol != null)
                {
                    symbols.Add(symbol);
                }
            }
        }

        private struct Parser
        {
            private readonly string _id;
            private readonly Compilation _compilation;
            private ISymbol _typeParameterContext;
            private int _index;

            private Parser(string id, int index, Compilation compilation, ISymbol typeParameterContext)
            {
                _id = id;
                _compilation = compilation;
                _typeParameterContext = typeParameterContext;
                _index = index;
            }

            public static bool Parse(string id, Compilation compilation, List<ISymbol> results)
            {
                if (id == null)
                {
                    return false;
                }

                results.Clear();
                new Parser(id, 0, compilation, null).ParseSymbolId(results);

                return results.Count > 0;
            }

            private void ParseSymbolId(List<ISymbol> results)
            {
                if (PeekNextChar() == '*')
                {
                    _index++;
                    results.Add(_compilation.DynamicType);
                }
                else if (PeekNextChar() == '`' && _typeParameterContext != null)
                {
                    ParseTypeParameterSymbol(results);
                }
                else if (IsNamePrefix())
                {
                    if (PeekNextChar() == AliasPrefix)
                    {
                        _index += 2;

                        var name = ParseName();
                        var filePath = ParseStringLiteral();
                        GetMatchingAliases(name, filePath, results);
                        return;
                    }
                    else
                    {
                        ParseNamedSymbol(results);
                    }
                }

                // follow on array and pointer construction
                if (PeekNextChar() == '[' || PeekNextChar() == '*')
                {
                    ParseArrayAndPointerTypes(results);
                }
            }

            private void ParseNamedSymbol(List<ISymbol> results)
            {
                var containers = s_symbolListPool.Allocate();
                try
                {
                    containers.Add(_compilation.GlobalNamespace);

                    char prefix;
                    string name;

                    // process dotted names
                    while (true)
                    {
                        ParsePrefixedName(out prefix, out name);

                        switch (prefix)
                        {
                            case MethodPrefix:
                                GetMatchingMethods(containers, name, results);
                                break;
                            case NamedTypePrefix:
                                GetMatchingTypes(containers, name, results);
                                break;
                            case PropertyPrefix:
                                GetMatchingProperties(containers, name, results);
                                break;
                            case EventPrefix:
                                GetMatchingEvents(containers, name, results);
                                break;
                            case FieldPrefix:
                                GetMatchingFields(containers, name, results);
                                break;
                            case NamespacePrefix:
                                GetMatchingNamespaces(containers, name, results);
                                break;
                            case ErrorTypePrefix:
                                GetErrorTypes(containers, name, results);
                                break;
                            case ParameterPrefix:
                                GetMatchingParameterSymbols(containers, name, results);
                                break;
                            case LocalPrefix:
                                GetMatchingInteriorSymbols(containers, SymbolKind.Local, name, results);
                                break;
                            case LabelPrefix:
                                GetMatchingInteriorSymbols(containers, SymbolKind.Label, name, results);
                                break;
                            case RangeVariablePrefix:
                                GetMatchingInteriorSymbols(containers, SymbolKind.RangeVariable, name, results);
                                break;
                            default:
                                throw new InvalidOperationException("Missing or unknown name prefix.");
                        }

                        if (PeekNextChar() != '.')
                        {
                            break;
                        }

                        _index++;

                        // results become the new containers
                        containers.Clear();
                        containers.AddRange(results);
                        results.Clear();
                    }

                    // a type parameter evaluated in an explicit context?
                    var nextChar = PeekNextChar();
                    if (nextChar == ':')
                    {
                        _index++;

                        var originalContext = _typeParameterContext;
                        _typeParameterContext = results.FirstOrDefault();

                        results.Clear();
                        ParseTypeParameterSymbol(results);
                        _typeParameterContext = originalContext;
                    }
                }
                finally
                {
                    s_symbolListPool.ClearAndFree(containers);
                }
            }

            private bool IsNamePrefix()
            {
                return IsNamePrefix(PeekNextChar()) && PeekNextChar(1) == ':';
            }

            private char? ParseNamePrefix()
            {
                if (PeekNextChar(1) == ':')
                {
                    var prefix = PeekNextChar();
                    if (IsNamePrefix(prefix))
                    {
                        _index += 2;
                        return prefix;
                    }
                }

                return null;
            }

            private static bool IsNamePrefix(char ch)
            {
                switch (ch)
                {
                    case AliasPrefix:
                    case EventPrefix:
                    case FieldPrefix:
                    case MethodPrefix:
                    case NamespacePrefix:
                    case PropertyPrefix:
                    case NamedTypePrefix:
                    case ErrorTypePrefix:
                    case LocalPrefix:
                    case LabelPrefix:
                    case RangeVariablePrefix:
                    case ParameterPrefix:
                        return true;
                    default:
                        return false;
                }

            }

            private bool ParsePrefixedName(out char prefix, out string name)
            {
                var maybePrefix = ParseNamePrefix();
                if (maybePrefix != null)
                {
                    prefix = maybePrefix.Value;
                    name = ParseName();
                    return true;
                }
                else
                {
                    prefix = default(char);
                    name = null;
                    return false;
                }
            }

            private ITypeSymbol ParseTypeSymbol()
            {
                var results = s_symbolListPool.Allocate();
                try
                {
                    ParseSymbolId(results);
                    if (results.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return results[0] as ITypeSymbol;
                    }
                }
                finally
                {
                    s_symbolListPool.ClearAndFree(results);
                }
            }

            private void ParseArrayAndPointerTypes(List<ISymbol> symbols)
            {
                while (true)
                {
                    var nextChar = PeekNextChar();
                    if (nextChar == '[')
                    {
                        var bounds = ParseArrayBounds();
                        ConstructArray(symbols, bounds);
                    }
                    else if (nextChar == '*')
                    {
                        _index++;
                        ConstructPointer(symbols);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void ConstructArray(List<ISymbol> symbols, int bounds)
            {
                for (int i = 0; i < symbols.Count; )
                {
                    var typeSymbol = symbols[i] as ITypeSymbol;
                    if (typeSymbol != null)
                    {
                        symbols[i] = _compilation.CreateArrayTypeSymbol(typeSymbol, bounds);
                        i++;
                    }
                    else
                    {
                        symbols.RemoveAt(i);
                    }
                }
            }

            private void ConstructPointer(List<ISymbol> symbols)
            {
                for (int i = 0; i < symbols.Count;)
                {
                    var typeSymbol = symbols[i] as ITypeSymbol;
                    if (typeSymbol != null)
                    {
                        symbols[i] = _compilation.CreatePointerTypeSymbol(typeSymbol);
                        i++;
                    }
                    else
                    {
                        symbols.RemoveAt(i);
                    }
                }
            }

            private int ParseArrayBounds()
            {
                _index++;  // skip '['

                int bounds = 0;

                while (true)
                {
                    if (char.IsDigit(PeekNextChar()))
                    {
                        ParseIntegerLiteral();
                    }

                    if (PeekNextChar() == ':')
                    {
                        _index++;

                        if (char.IsDigit(PeekNextChar()))
                        {
                            ParseIntegerLiteral();
                        }
                    }

                    bounds++;

                    if (PeekNextChar() == ',')
                    {
                        _index++;
                        continue;
                    }

                    break;
                }

                if (PeekNextChar() == ']')
                {
                    _index++;
                }

                return bounds;
            }

            private ITypeSymbol[] ParseTypeArguments()
            {
                var list = s_symbolListPool.Allocate();
                try
                {
                    ParseTypeArguments(list);
                    return list.OfType<ITypeSymbol>().ToArray();
                }
                finally
                {
                    s_symbolListPool.ClearAndFree(list);
                }
            }

            private void ParseTypeArguments(List<ISymbol> typeArguments)
            {
                if (PeekNextChar() == '{')
                {
                    _index++;

                    while (true)
                    {
                        var type = ParseTypeSymbol();

                        if (type == null)
                        {
                            // if a type argument cannot be identified, argument list is no good
                            return;
                        }

                        typeArguments.Add(type);

                        if (PeekNextChar() == ',')
                        {
                            _index++;
                            continue;
                        }

                        break;
                    }

                    if (PeekNextChar() == '}')
                    {
                        _index++;
                    }
                }
            }

            private void ParseTypeParameterSymbol(List<ISymbol> results)
            {
                // skip the first `
                System.Diagnostics.Debug.Assert(PeekNextChar() == '`');
                _index++;

                if (PeekNextChar() == '`')
                {
                    // `` means this is a method type parameter
                    _index++;
                    var methodTypeParameterIndex = ParseIntegerLiteral();

                    var methodContext = _typeParameterContext as IMethodSymbol;
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
                    var typeParameterIndex = ParseIntegerLiteral();

                    var methodContext = _typeParameterContext as IMethodSymbol;
                    var typeContext = methodContext != null ? methodContext.ContainingType : _typeParameterContext as INamedTypeSymbol;

                    if (typeContext != null)
                    {
                        results.Add(GetNthTypeParameter(typeContext, typeParameterIndex));
                    }
                }
            }

            private void GetErrorTypes(IReadOnlyList<ISymbol> containers, string memberName, List<ISymbol> results)
            {
                ITypeSymbol[] typeArguments = null;
                if (PeekNextChar() == '{')
                {
                    typeArguments = ParseTypeArguments();
                }

                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var container = containers[i] as INamespaceOrTypeSymbol;
                    if (container != null)
                    {
                        results.Add(GetErrorType(container, memberName, typeArguments));
                    }
                }
            }

            private ITypeSymbol GetErrorType(INamespaceOrTypeSymbol container, string name, ITypeSymbol[] typeArguments)
            {
                var errorType = _compilation.CreateErrorTypeSymbol(container, name, typeArguments != null ? typeArguments.Length : 0);
                if (typeArguments != null)
                {
                    errorType = errorType.Construct(typeArguments);
                }

                return errorType;
            }

            private void GetMatchingTypes(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var container = containers[i] as INamespaceOrTypeSymbol;
                    if (container != null)
                    {
                        GetMatchingTypes(container, name, results);
                    }
                }
            }

            private void GetMatchingTypes(INamespaceOrTypeSymbol container, string name, List<ISymbol> results)
            {
                var typeArguments = s_symbolListPool.Allocate();
                try
                {
                    var startIndex = _index;
                    var endIndex = _index;

                    var members = container.GetMembers(name);

                    foreach (var symbol in members)
                    {
                        if (symbol.Kind == SymbolKind.NamedType)
                        {
                            var namedType = (INamedTypeSymbol)symbol;
                            _index = startIndex;

                            // has type arguments?
                            if (PeekNextChar() == '{')
                            {
                                //_typeParameterContext = namedType;

                                typeArguments.Clear();
                                ParseTypeArguments(typeArguments);

                                if (namedType.Arity != typeArguments.Count)
                                {
                                    // if no type arguments are found then the type cannot be identified
                                    continue;
                                }

                                namedType = namedType.Construct(typeArguments.Cast<ITypeSymbol>().ToArray());
                            }
                            // has type parameters?
                            else if (PeekNextChar() == '`')
                            {
                                _index++;
                                var arity = ParseIntegerLiteral();
                                if (arity != namedType.Arity)
                                {
                                    continue;
                                }
                            }
                            else if (namedType.Arity > 0)
                            {
                                continue;
                            }

                            endIndex = _index;
                            results.Add(namedType);
                        }
                    }

                    _index = endIndex;
                }
                finally
                {
                    s_symbolListPool.ClearAndFree(typeArguments);
                }
            }

            private void GetMatchingNamespaces(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    GetMatchingNamespaces(containers[i] as INamespaceOrTypeSymbol, name, results);
                }
            }

            private void GetMatchingNamespaces(INamespaceOrTypeSymbol container, string name, List<ISymbol> results)
            {
                if (container != null)
                {
                    var members = container.GetMembers(name);

                    foreach (var symbol in members)
                    {
                        if (symbol.Kind == SymbolKind.Namespace)
                        {
                            results.Add(symbol);
                        }
                    }
                }
            }

            private void GetMatchingMethods(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                var originalContext = _typeParameterContext;
                var typeArguments = s_symbolListPool.Allocate();
                var parameters = s_parameterListPool.Allocate();
                try
                {
                    var startIndex = _index;
                    var endIndex = _index;

                    for (int i = 0, n = containers.Count; i < n; i++)
                    {
                        var container = containers[i] as INamespaceOrTypeSymbol;
                        if (container != null)
                        {
                            var members = container.GetMembers(name);

                            foreach (var symbol in members)
                            {

                                _index = startIndex;

                                var methodSymbol = symbol as IMethodSymbol;
                                if (methodSymbol != null)
                                {
                                    // has type arguments?
                                    if (PeekNextChar() == '{')
                                    {
                                        _typeParameterContext = methodSymbol;

                                        typeArguments.Clear();
                                        ParseTypeArguments(typeArguments);

                                        if (methodSymbol.Arity != typeArguments.Count)
                                        {
                                            // if no type arguments are found then the type cannot be identified
                                            continue;
                                        }

                                        methodSymbol = methodSymbol.Construct(typeArguments.Cast<ITypeSymbol>().ToArray());
                                    }
                                    // has type parameters?
                                    else if (PeekNextChar() == '`')
                                    {
                                        _index++;
                                        var arity = ParseIntegerLiteral();
                                        if (arity != methodSymbol.Arity)
                                        {
                                            continue;
                                        }
                                    }

                                    if (PeekNextChar() == '(')
                                    {
                                        parameters?.Clear();
                                        _typeParameterContext = methodSymbol;

                                        if (!ParseParameterList(parameters) || !AllParametersMatch(methodSymbol.Parameters, parameters))
                                        {
                                            // if the parameters cannot be identified (some error), or parameter types don't match
                                            continue;
                                        }
                                    }
                                    else if (methodSymbol.Parameters.Length != 0)
                                    {
                                        continue;
                                    }

                                    // extension method target type
                                    if (PeekNextChar() == '!')
                                    {
                                        _index++;

                                        var targetType = ParseTypeSymbol();
                                        if (targetType == null || !methodSymbol.IsStatic || methodSymbol.Parameters.Length == 0)
                                        {
                                            continue;
                                        }

                                        methodSymbol = methodSymbol.ReduceExtensionMethod(targetType);
                                    }

                                    endIndex = _index;
                                    results.Add(methodSymbol);
                                }
                            }
                        }
                    }

                    _index = endIndex;
                }
                finally
                {
                    s_parameterListPool.ClearAndFree(parameters);
                    s_symbolListPool.ClearAndFree(typeArguments);
                    _typeParameterContext = originalContext;
                }
            }

            private void GetMatchingProperties(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                int startIndex = _index;
                int endIndex = _index;

                var originalContext = _typeParameterContext;
                List<ParameterInfo> parameters = null;
                try
                {
                    for (int i = 0, n = containers.Count; i < n; i++)
                    {
                        name = DecodePropertyName(name, _compilation.Language);

                        var container = containers[i] as INamespaceOrTypeSymbol;
                        if (container != null)
                        {
                            _typeParameterContext = container;
                            var members = container.GetMembers(name);

                            foreach (var symbol in members)
                            {
                                _index = startIndex;

                                var propertySymbol = symbol as IPropertySymbol;
                                if (propertySymbol != null)
                                {
                                    if (PeekNextChar() == '(')
                                    {
                                        if (parameters == null)
                                        {
                                            parameters = s_parameterListPool.Allocate();
                                        }
                                        else
                                        {
                                            parameters.Clear();
                                        }

                                        if (ParseParameterList(parameters)
                                            && AllParametersMatch(propertySymbol.Parameters, parameters))
                                        {
                                            results.Add(propertySymbol);
                                            endIndex = _index;
                                        }
                                    }
                                    else if (propertySymbol.Parameters.Length == 0)
                                    {
                                        results.Add(propertySymbol);
                                        endIndex = _index;
                                    }
                                }
                            }
                        }
                    }

                    _index = endIndex;
                }
                finally
                {
                    if (parameters != null)
                    {
                        s_parameterListPool.ClearAndFree(parameters);
                    }

                    _typeParameterContext = originalContext;
                }
            }

            private void GetMatchingFields(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var container = containers[i] as INamespaceOrTypeSymbol;
                    if (container != null)
                    {
                        var members = container.GetMembers(name);

                        foreach (var symbol in members)
                        {
                            if (symbol.Kind == SymbolKind.Field)
                            {
                                results.Add(symbol);
                            }
                        }
                    }
                }
            }

            private void GetMatchingEvents(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> results)
            {
                for (int i = 0, n = containers.Count; i < n; i++)
                {
                    var container = containers[i] as INamespaceOrTypeSymbol;
                    if (container != null)
                    {
                        var members = container.GetMembers(name);

                        foreach (var symbol in members)
                        {
                            if (symbol.Kind == SymbolKind.Event)
                            {
                                results.Add(symbol);
                            }
                        }
                    }
                }
            }

            private bool AllParametersMatch(ImmutableArray<IParameterSymbol> symbolParameters, List<ParameterInfo> expectedParameters)
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

            private bool ParameterMatches(IParameterSymbol symbol, ParameterInfo parameterInfo)
            {
                // same ref'ness?
                if ((symbol.RefKind == RefKind.None) == parameterInfo.IsRefOrOut)
                {
                    return false;
                }

                var parameterType = parameterInfo.Type;

                return parameterType != null && symbol.Type.Equals(parameterType);
            }

            private ITypeParameterSymbol GetNthTypeParameter(INamedTypeSymbol typeSymbol, int n)
            {
                var containingTypeParameterCount = GetTypeParameterCount(typeSymbol.ContainingType);
                if (n < containingTypeParameterCount)
                {
                    return GetNthTypeParameter(typeSymbol.ContainingType, n);
                }

                var index = n - containingTypeParameterCount;
                return typeSymbol.TypeParameters[index];
            }

            private int GetTypeParameterCount(INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol == null)
                {
                    return 0;
                }

                return typeSymbol.TypeParameters.Length + GetTypeParameterCount(typeSymbol.ContainingType);
            }

            [StructLayout(LayoutKind.Auto)]
            private struct ParameterInfo
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

            private bool ParseParameterList(List<ParameterInfo> parameters)
            {
                System.Diagnostics.Debug.Assert(_typeParameterContext != null);

                _index++; // skip over '('

                if (PeekNextChar() == ')')
                {
                    _index++;
                    return true;
                }

                var parameter = ParseParameter();
                if (parameter == null)
                {
                    return false;
                }

                parameters.Add(parameter.Value);

                while (PeekNextChar() == ',')
                {
                    _index++;

                    parameter = ParseParameter();
                    if (parameter == null)
                    {
                        return false;
                    }

                    parameters.Add(parameter.Value);
                }

                if (PeekNextChar() == ')')
                {
                    _index++;
                }

                return true;
            }

            private ParameterInfo? ParseParameter()
            {
                bool isRefOrOut = false;

                var type = ParseTypeSymbol();

                if (type == null)
                {
                    // if no type can be identified, then there is no parameter
                    return null;
                }

                if (PeekNextChar() == '@')
                {
                    _index++;
                    isRefOrOut = true;
                }

                return new ParameterInfo(type, isRefOrOut);
            }

            private void GetParameterReferences(List<ISymbol> symbols)
            {
                if (PeekNextChar() == '#')
                {
                    _index++;
                    var ordinal = ParseIntegerLiteral();

                    for (int i = 0; i < symbols.Count;)
                    {
                        var method = symbols[i] as IMethodSymbol;
                        if (method != null && ordinal < method.Parameters.Length)
                        {
                            symbols[i] = method.Parameters[ordinal];
                            i++;
                            continue;
                        }

                        var property = symbols[i] as IPropertySymbol;
                        if (property != null && ordinal < property.Parameters.Length)
                        {
                            symbols[i] = property.Parameters[ordinal];
                            i++;
                            continue;
                        }

                        // not a symbol with parameters? 
                        symbols.RemoveAt(i);
                    }
                }
            }

            private void GetMatchingParameterSymbols(IReadOnlyList<ISymbol> containers, string name, List<ISymbol> symbols)
            {
                for (int i = 0; i < containers.Count; i++)
                {
                    var method = containers[i] as IMethodSymbol;
                    if (method != null)
                    {
                        var parameter = method.Parameters.FirstOrDefault(p => p.Name == name);
                        if (parameter != null)
                        {
                            symbols.Add(parameter);
                        }
                    }

                    var property = containers[i] as IPropertySymbol;
                    if (property != null)
                    {
                        var parameter = property.Parameters.FirstOrDefault(p => p.Name == name);
                        if (parameter != null)
                        {
                            symbols.Add(parameter);
                        }
                    }
                }
            }

            private void GetMatchingInteriorSymbols(IReadOnlyList<ISymbol> containers, SymbolKind kind, string name, List<ISymbol> symbols)
            {
                int occurrence = 0;
                if (PeekNextChar() == '`')
                {
                    _index++;
                    occurrence = ParseIntegerLiteral();
                }

                for (int i = 0; i < containers.Count; i++)
                {
                    var symbol = GetMatchingInteriorSymbol(containers[i], kind, name, occurrence);
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                }
            }

            private void GetMatchingAliases(string aliasName, string filePath, List<ISymbol> results)
            {
                var tree = _compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == filePath);
                if (tree != null)
                {
                    var model = _compilation.GetSemanticModel(tree);
                    var symbol = GetMatchingDeclaredSymbol(model, tree.GetRoot(), SymbolKind.Alias, aliasName, 0);
                    if (symbol != null)
                    {
                        results.Add(symbol);
                    }
                }
            }

            private char PeekNextChar(int offset = 0)
            {
                return _index + offset >= _id.Length ? '\0' : _id[_index + offset];
            }

            private static readonly char[] s_nameDelimiters = { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~', '!' };

            private string ParseName()
            {
                string name;

                int delimiterOffset = _id.IndexOfAny(s_nameDelimiters, _index);
                if (delimiterOffset >= 0)
                {
                    name = _id.Substring(_index, delimiterOffset - _index);
                    _index = delimiterOffset;
                }
                else
                {
                    name = _id.Substring(_index);
                    _index = _id.Length;
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

            private string ParseStringLiteral()
            {
                if (PeekNextChar() == '\'')
                {
                    _index++;

                    var start = _index;
                    char ch; 
                    while ((ch = PeekNextChar()) != '\'' && ch != '\0')
                    {
                        _index++;
                    }

                    var end = _index;

                    if (PeekNextChar() == '\'')
                    {
                        _index++;
                    }

                    return _id.Substring(start, end - start);
                }

                return "";
            }

            private int ParseIntegerLiteral()
            {
                int n = 0;

                while (_index < _id.Length && char.IsDigit(_id[_index]))
                {
                    n = n * 10 + (_id[_index] - '0');
                    _index++;
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
