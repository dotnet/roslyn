// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        private const string s_suppressionPrefix = "~";

        [StructLayout(LayoutKind.Auto)]
        private struct TargetSymbolResolver
        {
            private static readonly char[] s_nameDelimiters = { ':', '.', '+', '(', ')', '<', '>', '[', ']', '{', '}', ',', '&', '*', '`' };
            private static readonly string[] s_callingConventionStrings =
            {
                "[vararg]",
                "[cdecl]",
                "[fastcall]",
                "[stdcall]",
                "[thiscall]"
            };

            private static readonly ParameterInfo[] s_noParameters = Array.Empty<ParameterInfo>();

            private readonly Compilation _compilation;
            private readonly TargetScope _scope;
            private readonly string _name;
            private int _index;

            public TargetSymbolResolver(Compilation compilation, TargetScope scope, string fullyQualifiedName)
            {
                _compilation = compilation;
                _scope = scope;
                _name = fullyQualifiedName;
                _index = 0;
            }

            private static string RemovePrefix(string id, string prefix)
            {
                if (id != null && prefix != null && id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return id.Substring(prefix.Length);
                }

                return id;
            }

            public void Resolve(IList<ISymbol> results)
            {
                if (string.IsNullOrEmpty(_name))
                {
                    return;
                }

                // Try to parse the name as declaration ID generated from symbol's documentation comment Id.
                var nameWithoutPrefix = RemovePrefix(_name, s_suppressionPrefix);
                var docIdResults = DocumentationCommentId.GetSymbolsForDeclarationId(nameWithoutPrefix, _compilation);
                if (docIdResults.Length > 0)
                {
                    foreach (var result in docIdResults)
                    {
                        results.Add(result);
                    }

                    return;
                }

                // Parse 'e:' prefix used by FxCop to differentiate between event and non-event symbols of the same name.
                bool isEvent = false;
                if (_name.Length >= 2 && _name[0] == 'e' && _name[1] == ':')
                {
                    isEvent = true;
                    _index = 2;
                }

                INamespaceOrTypeSymbol containingSymbol = _compilation.GlobalNamespace;
                bool? segmentIsNamedTypeName = null;

                while (true)
                {
                    var segment = ParseNextNameSegment();

                    // Special case: Roslyn names indexers "this[]" in CSharp, FxCop names them "Item" with parameters in [] brackets
                    bool isIndexerProperty = false;
                    if (segment == "Item" && PeekNextChar() == '[')
                    {
                        isIndexerProperty = true;
                        if (_compilation.Language == LanguageNames.CSharp)
                        {
                            segment = "this[]";
                        }
                    }

                    var candidateMembers = containingSymbol.GetMembers(segment);
                    if (candidateMembers.Length == 0)
                    {
                        return;
                    }

                    if (segmentIsNamedTypeName.HasValue)
                    {
                        candidateMembers = segmentIsNamedTypeName.Value ?
                            candidateMembers.Where(s => s.Kind == SymbolKind.NamedType).ToImmutableArray() :
                            candidateMembers.Where(s => s.Kind != SymbolKind.NamedType).ToImmutableArray();

                        segmentIsNamedTypeName = null;
                    }

                    int? arity = null;
                    ParameterInfo[] parameters = null;

                    // Check for generic arity
                    if (_scope != TargetScope.Namespace && PeekNextChar() == '`')
                    {
                        ++_index;
                        arity = ReadNextInteger();
                    }

                    // Check for method or indexer parameter list
                    var nextChar = PeekNextChar();

                    if (!isIndexerProperty && nextChar == '(' || isIndexerProperty && nextChar == '[')
                    {
                        parameters = ParseParameterList();
                        if (parameters == null)
                        {
                            // Failed to resolve parameter list
                            return;
                        }
                    }
                    else if (nextChar == '.' || nextChar == '+')
                    {
                        ++_index;

                        if (arity > 0 || nextChar == '+')
                        {
                            // The name continues and either has an arity or specifically continues with a '+'
                            // so segment must be the name of a named type
                            containingSymbol = GetFirstMatchingNamedType(candidateMembers, arity ?? 0);
                        }
                        else
                        {
                            // The name continues with a '.' and does not specify a generic arity
                            // so segment must be the name of a namespace or a named type
                            containingSymbol = GetFirstMatchingNamespaceOrType(candidateMembers);
                        }

                        if (containingSymbol == null)
                        {
                            // If we cannot resolve the name on the left of the delimiter, we have no 
                            // hope of finding the symbol.
                            return;
                        }

                        if (containingSymbol.Kind == SymbolKind.NamedType)
                        {
                            // If segment resolves to a named type, that restricts what the next segment
                            // can resolve to depending on whether the name continues with '+' or '.'
                            segmentIsNamedTypeName = nextChar == '+';
                        }

                        continue;
                    }

                    if (_scope == TargetScope.Member && !isIndexerProperty && parameters != null)
                    {
                        TypeInfo? returnType = null;
                        if (PeekNextChar() == ':')
                        {
                            ++_index;
                            returnType = ParseNamedType(null);
                        }

                        foreach (var method in GetMatchingMethods(candidateMembers, arity, parameters, returnType))
                        {
                            results.Add(method);
                        }

                        return;
                    }

                    ISymbol singleResult;

                    switch (_scope)
                    {
                        case TargetScope.Namespace:
                            singleResult = candidateMembers.FirstOrDefault(s => s.Kind == SymbolKind.Namespace);
                            break;

                        case TargetScope.Type:
                            singleResult = GetFirstMatchingNamedType(candidateMembers, arity ?? 0);
                            break;

                        case TargetScope.Member:
                            if (isIndexerProperty)
                            {
                                singleResult = GetFirstMatchingIndexer(candidateMembers, parameters);
                            }
                            else if (isEvent)
                            {
                                singleResult = candidateMembers.FirstOrDefault(s => s.Kind == SymbolKind.Event);
                            }
                            else
                            {
                                singleResult = candidateMembers.FirstOrDefault(s =>
                                    s.Kind != SymbolKind.Namespace &&
                                    s.Kind != SymbolKind.NamedType);
                            }
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(_scope);
                    }

                    if (singleResult != null)
                    {
                        results.Add(singleResult);
                    }

                    return;
                }
            }

            private string ParseNextNameSegment()
            {
                // Ignore optional octothorpe in the member name used by FxCop to differentiate between
                // Orcas and Whidbey name providers. The fully-qualified member name format generated by each of
                // these name providers is similar enough that we can just ignore this character.
                if (PeekNextChar() == '#')
                {
                    ++_index;

                    // Ignore calling convention strings generated by FxCop for methods.
                    // Methods can't differ solely by calling convention in C# or VB.
                    if (PeekNextChar() == '[')
                    {
                        foreach (string callingConvention in s_callingConventionStrings)
                        {
                            if (callingConvention == _name.Substring(_index, callingConvention.Length))
                            {
                                _index += callingConvention.Length;
                                break;
                            }
                        }
                    }
                }

                string segment;

                // Find the end of the next name segment, special case constructors which start with '.'
                int delimiterOffset = PeekNextChar() == '.' ?
                    _name.IndexOfAny(s_nameDelimiters, _index + 1) :
                    _name.IndexOfAny(s_nameDelimiters, _index);

                if (delimiterOffset >= 0)
                {
                    segment = _name.Substring(_index, delimiterOffset - _index);
                    _index = delimiterOffset;
                }
                else
                {
                    segment = _name.Substring(_index);
                    _index = _name.Length;
                }

                return segment;
            }

            private char PeekNextChar()
            {
                return _index >= _name.Length ? '\0' : _name[_index];
            }

            private int ReadNextInteger()
            {
                int n = 0;

                while (_index < _name.Length && char.IsDigit(_name[_index]))
                {
                    n = n * 10 + (_name[_index] - '0');
                    ++_index;
                }

                return n;
            }

            private ParameterInfo[] ParseParameterList()
            {
                // Consume the opening parenthesis or bracket
                Debug.Assert(PeekNextChar() == '(' || PeekNextChar() == '[');
                ++_index;

                var nextChar = PeekNextChar();
                if (nextChar == ')' || nextChar == ']')
                {
                    // Empty parameter list
                    ++_index;
                    return s_noParameters;
                }

                var builder = new ArrayBuilder<ParameterInfo>();

                while (true)
                {
                    var parameter = ParseParameter();
                    if (parameter != null)
                    {
                        builder.Add(parameter.Value);
                    }
                    else
                    {
                        builder.Free();
                        return null;
                    }

                    if (PeekNextChar() == ',')
                    {
                        ++_index;
                    }
                    else
                    {
                        break;
                    }
                }

                nextChar = PeekNextChar();
                if (nextChar == ')' || nextChar == ']')
                {
                    // Consume the closing parenthesis or bracket
                    ++_index;
                }
                else
                {
                    // Malformed parameter list: missing close parenthesis or bracket
                    builder.Free();
                    return null;
                }

                return builder.ToArrayAndFree();
            }

            private ParameterInfo? ParseParameter()
            {
                var type = ParseType(null);
                if (type == null)
                {
                    return null;
                }

                var isRefOrOut = PeekNextChar() == '&';
                if (isRefOrOut)
                {
                    ++_index;
                }

                return new ParameterInfo(type.Value, isRefOrOut);
            }

            private TypeInfo? ParseType(ISymbol bindingContext)
            {
                TypeInfo? result;

                IgnoreCustomModifierList();

                if (PeekNextChar() == '!')
                {
                    result = ParseIndexedTypeParameter(bindingContext);
                }
                else
                {
                    result = ParseNamedType(bindingContext);

                    // If parsing as a named type failed, this could be a named type parameter,
                    // which we will only be able to resolve once we have a binding context.
                    if (bindingContext is { } && result is { HasValue: true, Value: { IsBound: false } })
                    {
                        _index = result.Value.StartIndex;
                        result = ParseNamedTypeParameter(bindingContext);
                    }
                }

                if (result == null)
                {
                    return null;
                }

                if (result.Value.IsBound)
                {
                    var typeSymbol = result.Value.Type;

                    // Handle pointer and array specifiers for bound types
                    while (true)
                    {
                        IgnoreCustomModifierList();

                        var nextChar = PeekNextChar();
                        if (nextChar == '[')
                        {
                            typeSymbol = ParseArrayType(typeSymbol);
                            if (typeSymbol == null)
                            {
                                return null;
                            }
                            continue;
                        }

                        if (nextChar == '*')
                        {
                            ++_index;
                            typeSymbol = _compilation.CreatePointerTypeSymbol(typeSymbol);
                            continue;
                        }

                        break;
                    }

                    return TypeInfo.Create(typeSymbol);
                }

                // Skip pointer and array specifiers for unbound types
                IgnorePointerAndArraySpecifiers();
                return result;
            }

            private void IgnoreCustomModifierList()
            {
                // NOTE: There is currently no way to create symbols
                // with custom modifiers from outside the compiler layer. In
                // particular, there is no language agnostic way to attach custom
                // modifiers to symbols. As a result we cannot match symbols which
                // have custom modifiers, because their public equals overrides in
                // general explicitly check custom modifiers. So we just ignore
                // custom modifier lists. This would only matter in the case that
                // someone targeted a SuppressMessageAttribute at a method that
                // overloads a method from metadata which uses custom modifiers.
                if (PeekNextChar() == '{')
                {
                    for (; _index < _name.Length && _name[_index] != '}'; ++_index) { }
                }
            }

            private void IgnorePointerAndArraySpecifiers()
            {
                bool inBrackets = false;
                for (; _index < _name.Length; ++_index)
                {
                    switch (PeekNextChar())
                    {
                        case '[':
                            inBrackets = true;
                            break;
                        case ']':
                            if (!inBrackets)
                            {
                                // End of indexer parameter list
                                return;
                            }
                            inBrackets = false;
                            break;
                        case '*':
                            break;
                        default:
                            if (!inBrackets)
                            {
                                // End of parameter type name
                                return;
                            }
                            break;
                    }
                }
            }

            private TypeInfo? ParseIndexedTypeParameter(ISymbol bindingContext)
            {
                var startIndex = _index;

                Debug.Assert(PeekNextChar() == '!');
                ++_index;

                if (PeekNextChar() == '!')
                {
                    // !! means this is a method type parameter
                    ++_index;
                    var methodTypeParameterIndex = ReadNextInteger();

                    var methodContext = bindingContext as IMethodSymbol;
                    if (methodContext != null)
                    {
                        var count = methodContext.TypeParameters.Length;
                        if (count > 0 && methodTypeParameterIndex < count)
                        {
                            return TypeInfo.Create(methodContext.TypeParameters[methodTypeParameterIndex]);
                        }

                        // No such parameter
                        return null;
                    }

                    // If there is no method context, then the type is unbound and must be bound later
                    return TypeInfo.CreateUnbound(startIndex);
                }

                // ! means this is a regular type parameter
                var typeParameterIndex = ReadNextInteger();

                if (bindingContext != null)
                {
                    var typeParameter = GetNthTypeParameter(bindingContext.ContainingType, typeParameterIndex);
                    if (typeParameter != null)
                    {
                        return TypeInfo.Create(typeParameter);
                    }

                    // no such parameter
                    return null;
                }

                // If there is no binding context, then the type is unbound and must be bound later
                return TypeInfo.CreateUnbound(startIndex);
            }

            private TypeInfo? ParseNamedTypeParameter(ISymbol bindingContext)
            {
                Debug.Assert(bindingContext != null);

                var typeParameterName = ParseNextNameSegment();

                var methodContext = bindingContext as IMethodSymbol;
                if (methodContext != null)
                {
                    // Check this method's type parameters for a name that matches
                    for (int i = 0; i < methodContext.TypeParameters.Length; ++i)
                    {
                        if (methodContext.TypeParameters[i].Name == typeParameterName)
                        {
                            return TypeInfo.Create(methodContext.TypeArguments[i]);
                        }
                    }
                }

                // Walk up the symbol tree until we find a type parameter with a name that matches
                for (var containingType = bindingContext.ContainingType; containingType != null; containingType = containingType.ContainingType)
                {
                    for (int i = 0; i < containingType.TypeParameters.Length; ++i)
                    {
                        if (containingType.TypeParameters[i].Name == typeParameterName)
                        {
                            return TypeInfo.Create(containingType.TypeArguments[i]);
                        }
                    }
                }

                return null;
            }

            private TypeInfo? ParseNamedType(ISymbol bindingContext)
            {
                INamespaceOrTypeSymbol containingSymbol = _compilation.GlobalNamespace;

                int startIndex = _index;

                while (true)
                {
                    var segment = ParseNextNameSegment();
                    var candidateMembers = containingSymbol.GetMembers(segment);
                    if (candidateMembers.Length == 0)
                    {
                        return TypeInfo.CreateUnbound(startIndex);
                    }

                    int arity = 0;
                    TypeInfo[] typeArguments = null;

                    // Check for generic arity
                    if (PeekNextChar() == '`')
                    {
                        ++_index;
                        arity = ReadNextInteger();
                    }

                    // Check for type argument list
                    if (PeekNextChar() == '<')
                    {
                        typeArguments = ParseTypeArgumentList(bindingContext);
                        if (typeArguments == null)
                        {
                            return null;
                        }

                        if (typeArguments.Any(a => !a.IsBound))
                        {
                            return TypeInfo.CreateUnbound(startIndex);
                        }
                    }

                    var nextChar = PeekNextChar();
                    if (nextChar == '.' || nextChar == '+')
                    {
                        ++_index;

                        if (arity > 0 || nextChar == '+')
                        {
                            // Segment is the name of a named type since the name has an arity or continues with a '+'
                            containingSymbol = GetFirstMatchingNamedType(candidateMembers, arity);
                        }
                        else
                        {
                            // Segment is the name of a namespace or type because the name continues with a '.'
                            containingSymbol = GetFirstMatchingNamespaceOrType(candidateMembers);
                        }

                        if (containingSymbol == null)
                        {
                            // If we cannot resolve the name on the left of the delimiter, we have no 
                            // hope of finding the symbol.
                            return null;
                        }

                        continue;
                    }

                    INamedTypeSymbol typeSymbol = GetFirstMatchingNamedType(candidateMembers, arity);
                    if (typeSymbol == null)
                    {
                        return null;
                    }

                    if (typeArguments != null)
                    {
                        typeSymbol = typeSymbol.Construct(typeArguments.Select(t => t.Type).ToArray());
                    }

                    return TypeInfo.Create(typeSymbol);
                }
            }

            private TypeInfo[] ParseTypeArgumentList(ISymbol bindingContext)
            {
                Debug.Assert(PeekNextChar() == '<');
                ++_index;

                var builder = new ArrayBuilder<TypeInfo>();

                while (true)
                {
                    var type = ParseType(bindingContext);
                    if (type == null)
                    {
                        builder.Free();
                        return null;
                    }

                    builder.Add(type.Value);

                    if (PeekNextChar() == ',')
                    {
                        ++_index;
                    }
                    else
                    {
                        break;
                    }
                }

                if (PeekNextChar() == '>')
                {
                    ++_index;
                }
                else
                {
                    builder.Free();
                    return null;
                }

                return builder.ToArrayAndFree();
            }

            private ITypeSymbol ParseArrayType(ITypeSymbol typeSymbol)
            {
                Debug.Assert(PeekNextChar() == '[');
                ++_index;
                int rank = 1;

                while (true)
                {
                    var nextChar = PeekNextChar();
                    if (nextChar == ',')
                    {
                        ++rank;
                    }
                    else if (nextChar == ']')
                    {
                        ++_index;
                        return _compilation.CreateArrayTypeSymbol(typeSymbol, rank);
                    }
                    else if (!char.IsDigit(nextChar) && nextChar != '.')
                    {
                        // Malformed array type specifier: invalid character
                        return null;
                    }

                    ++_index;
                }
            }

            private ISymbol GetFirstMatchingIndexer(ImmutableArray<ISymbol> candidateMembers, ParameterInfo[] parameters)
            {
                foreach (var symbol in candidateMembers)
                {
                    var propertySymbol = symbol as IPropertySymbol;
                    if (propertySymbol != null && AllParametersMatch(propertySymbol.Parameters, parameters))
                    {
                        return propertySymbol;
                    }
                }

                return null;
            }

            private ImmutableArray<IMethodSymbol> GetMatchingMethods(ImmutableArray<ISymbol> candidateMembers, int? arity, ParameterInfo[] parameters, TypeInfo? returnType)
            {
                var builder = new ArrayBuilder<IMethodSymbol>();

                foreach (var symbol in candidateMembers)
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    if (methodSymbol == null ||
                        (arity != null && methodSymbol.Arity != arity))
                    {
                        continue;
                    }

                    if (!AllParametersMatch(methodSymbol.Parameters, parameters))
                    {
                        continue;
                    }

                    if (returnType == null)
                    {
                        // If no return type specified, then any matches
                        builder.Add(methodSymbol);
                    }
                    else
                    {
                        // If return type is specified, then it must match
                        var boundReturnType = BindParameterOrReturnType(methodSymbol, returnType.Value);
                        if (boundReturnType != null && methodSymbol.ReturnType.Equals(boundReturnType))
                        {
                            builder.Add(methodSymbol);
                        }
                    }
                }

                return builder.ToImmutableAndFree();
            }

            private bool AllParametersMatch(ImmutableArray<IParameterSymbol> symbolParameters, ParameterInfo[] expectedParameters)
            {
                if (symbolParameters.Length != expectedParameters.Length)
                {
                    return false;
                }

                for (int i = 0; i < expectedParameters.Length; ++i)
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

                var parameterType = BindParameterOrReturnType(symbol.ContainingSymbol, parameterInfo.Type);

                return parameterType != null && symbol.Type.Equals(parameterType);
            }

            private ITypeSymbol BindParameterOrReturnType(ISymbol bindingContext, TypeInfo type)
            {
                if (type.IsBound)
                {
                    return type.Type;
                }

                var currentIndex = _index;
                _index = type.StartIndex;
                var result = this.ParseType(bindingContext);
                _index = currentIndex;

                return result?.Type;
            }

            private static INamedTypeSymbol GetFirstMatchingNamedType(ImmutableArray<ISymbol> candidateMembers, int arity)
            {
                return (INamedTypeSymbol)candidateMembers.FirstOrDefault(s =>
                    s.Kind == SymbolKind.NamedType &&
                    ((INamedTypeSymbol)s).Arity == arity);
            }

            private static INamespaceOrTypeSymbol GetFirstMatchingNamespaceOrType(ImmutableArray<ISymbol> candidateMembers)
            {
                return (INamespaceOrTypeSymbol)candidateMembers
                    .FirstOrDefault(s =>
                        s.Kind == SymbolKind.Namespace ||
                        s.Kind == SymbolKind.NamedType);
            }

            private static ITypeParameterSymbol GetNthTypeParameter(INamedTypeSymbol typeSymbol, int n)
            {
                var containingTypeParameterCount = GetTypeParameterCount(typeSymbol.ContainingType);
                if (n < containingTypeParameterCount)
                {
                    return GetNthTypeParameter(typeSymbol.ContainingType, n);
                }

                return typeSymbol.TypeParameters[n - containingTypeParameterCount];
            }

            private static int GetTypeParameterCount(INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol == null)
                {
                    return 0;
                }

                return typeSymbol.TypeParameters.Length + GetTypeParameterCount(typeSymbol.ContainingType);
            }

            [StructLayout(LayoutKind.Auto)]
            private struct TypeInfo
            {
                // The type, may be null if unbound.
                public readonly ITypeSymbol Type;

                // The start index into this.name for parsing this type if the type is not known
                // This index is used when rebinding later when the method context is known
                public readonly int StartIndex;

                public bool IsBound => this.Type != null;

                private TypeInfo(ITypeSymbol type, int startIndex)
                {
                    this.Type = type;
                    this.StartIndex = startIndex;
                }

                public static TypeInfo Create(ITypeSymbol type)
                {
                    Debug.Assert(type != null);
                    return new TypeInfo(type, -1);
                }

                public static TypeInfo CreateUnbound(int startIndex)
                {
                    Debug.Assert(startIndex >= 0);
                    return new TypeInfo(null, startIndex);
                }
            }

            [StructLayout(LayoutKind.Auto)]
            private struct ParameterInfo
            {
                public readonly TypeInfo Type;
                public readonly bool IsRefOrOut;

                public ParameterInfo(TypeInfo type, bool isRefOrOut)
                {
                    this.Type = type;
                    this.IsRefOrOut = isRefOrOut;
                }
            }
        }
    }
}
