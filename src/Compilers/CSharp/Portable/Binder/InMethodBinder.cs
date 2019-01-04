// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder for a method body, which places the method's parameters in scope
    /// and notes if the method is an iterator method.
    /// </summary>
    internal sealed class InMethodBinder : LocalScopeBinder
    {
        private MultiDictionary<string, ParameterSymbol> _lazyParameterMap;
        private readonly MethodSymbol _methodSymbol;
        private SmallDictionary<string, Symbol> _lazyDefinitionMap;
        private IteratorInfo _iteratorInfo;

        private class IteratorInfo
        {
            public static readonly IteratorInfo Empty = new IteratorInfo(null, default(ImmutableArray<Diagnostic>));

            public readonly TypeSymbol ElementType;
            public readonly ImmutableArray<Diagnostic> ElementTypeDiagnostics;

            public IteratorInfo(TypeSymbol elementType, ImmutableArray<Diagnostic> elementTypeDiagnostics)
            {
                this.ElementType = elementType;
                this.ElementTypeDiagnostics = elementTypeDiagnostics;
            }
        }

        public InMethodBinder(MethodSymbol owner, Binder enclosing)
            : base(enclosing, enclosing.Flags & ~BinderFlags.AllClearedAtExecutableCodeBoundary)
        {
            Debug.Assert(!enclosing.Flags.Includes(BinderFlags.InCatchFilter));
            Debug.Assert((object)owner != null);
            _methodSymbol = owner;
        }

        private static void RecordDefinition<T>(SmallDictionary<string, Symbol> declarationMap, ImmutableArray<T> definitions) where T : Symbol
        {
            foreach (Symbol s in definitions)
            {
                if (!declarationMap.ContainsKey(s.Name))
                {
                    declarationMap.Add(s.Name, s);
                }
            }
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }

        internal override uint LocalScopeDepth => Binder.TopLevelScope;

        protected override bool InExecutableBinder => true;

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return _methodSymbol;
            }
        }

        internal override bool IsInMethodBody
        {
            get
            {
                return true;
            }
        }

        internal void MakeIterator()
        {
            if (_iteratorInfo == null)
            {
                _iteratorInfo = IteratorInfo.Empty;
            }
        }

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return _iteratorInfo != null;
            }
        }

        internal override bool IsIndirectlyInIterator
        {
            get
            {
                return IsDirectlyInIterator; // Sic: indirectly iff directly
            }
        }

        internal override GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return null;
            }
        }

        internal override GeneratedLabelSymbol ContinueLabel
        {
            get
            {
                return null;
            }
        }

        internal override TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            RefKind refKind = _methodSymbol.RefKind;
            TypeSymbol returnType = _methodSymbol.ReturnType.TypeSymbol;

            if (!this.IsDirectlyInIterator)
            {
                // This should only happen when speculating, but we don't have a good way to assert that since the
                // original binder isn't available here.
                // If we're speculating about a yield statement inside a non-iterator method, we'll try to be nice
                // and deduce an iterator element type from the return type.  If we didn't do this, the 
                // TypeInfo.ConvertedType of the yield statement would always be an error type.  However, we will 
                // not mutate any state (i.e. we won't store the result).
                return GetIteratorElementTypeFromReturnType(refKind, returnType, node, diagnostics) ?? CreateErrorType();
            }

            if (_iteratorInfo == IteratorInfo.Empty)
            {
                TypeSymbol elementType = null;
                DiagnosticBag elementTypeDiagnostics = DiagnosticBag.GetInstance();

                elementType = GetIteratorElementTypeFromReturnType(refKind, returnType, node, elementTypeDiagnostics);

                if ((object)elementType == null)
                {
                    if (refKind != RefKind.None)
                    {
                        Error(elementTypeDiagnostics, ErrorCode.ERR_BadIteratorReturnRef, _methodSymbol.Locations[0], _methodSymbol);
                    }
                    else if (!returnType.IsErrorType())
                    {
                        Error(elementTypeDiagnostics, ErrorCode.ERR_BadIteratorReturn, _methodSymbol.Locations[0], _methodSymbol, returnType);
                    }
                    elementType = CreateErrorType();
                }

                var info = new IteratorInfo(elementType, elementTypeDiagnostics.ToReadOnlyAndFree());

                Interlocked.CompareExchange(ref _iteratorInfo, info, IteratorInfo.Empty);
            }

            if (node == null)
            {
                // node==null indicates this we are being called from the top-level of processing of a method. We report
                // the diagnostic, if any, at that time to ensure it is reported exactly once.
                diagnostics.AddRange(_iteratorInfo.ElementTypeDiagnostics);
            }

            return _iteratorInfo.ElementType;
        }

        private TypeSymbol GetIteratorElementTypeFromReturnType(RefKind refKind, TypeSymbol returnType, CSharpSyntaxNode errorLocationNode, DiagnosticBag diagnostics)
        {
            return GetIteratorElementTypeFromReturnType(Compilation, refKind, returnType, errorLocationNode, diagnostics).TypeSymbol;
        }

        internal static TypeSymbolWithAnnotations GetIteratorElementTypeFromReturnType(CSharpCompilation compilation, RefKind refKind, TypeSymbol returnType, CSharpSyntaxNode errorLocationNode, DiagnosticBag diagnostics)
        {
            if (refKind == RefKind.None && returnType.Kind == SymbolKind.NamedType)
            {
                TypeSymbol originalDefinition = returnType.OriginalDefinition;
                switch (originalDefinition.SpecialType)
                {
                    case SpecialType.System_Collections_IEnumerable:
                    case SpecialType.System_Collections_IEnumerator:
                        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
                        if (diagnostics != null)
                        {
                            ReportUseSiteDiagnostics(objectType, diagnostics, errorLocationNode);
                        }
                        return TypeSymbolWithAnnotations.Create(objectType);

                    case SpecialType.System_Collections_Generic_IEnumerable_T:
                    case SpecialType.System_Collections_Generic_IEnumerator_T:
                        return ((NamedTypeSymbol)returnType).TypeArgumentsNoUseSiteDiagnostics[0];
                }

                if (TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T), TypeCompareKind.ConsiderEverything2) ||
                    TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T), TypeCompareKind.ConsiderEverything2))
                {
                    return ((NamedTypeSymbol)returnType).TypeArgumentsNoUseSiteDiagnostics[0];
                }
            }

            return default;
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);

            if (_methodSymbol.ParameterCount == 0 || (options & LookupOptions.NamespaceAliasesOnly) != 0)
            {
                return;
            }

            var parameterMap = _lazyParameterMap;
            if (parameterMap == null)
            {
                var parameters = _methodSymbol.Parameters;
                parameterMap = new MultiDictionary<string, ParameterSymbol>(parameters.Length, EqualityComparer<string>.Default);
                foreach (var parameter in parameters)
                {
                    parameterMap.Add(parameter.Name, parameter);
                }

                _lazyParameterMap = parameterMap;
            }

            foreach (var parameterSymbol in parameterMap[name])
            {
                result.MergeEqual(originalBinder.CheckViability(parameterSymbol, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                foreach (var parameter in _methodSymbol.Parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        internal static bool ReportConflictWithParameter(Symbol parameter, Symbol newSymbol, string name, Location newLocation, DiagnosticBag diagnostics)
        {
            var oldLocation = parameter.Locations[0];
            Debug.Assert(oldLocation != newLocation || oldLocation == Location.None || newLocation.SourceTree?.GetRoot().ContainsDiagnostics == true,
                "same nonempty location refers to different symbols?");
            SymbolKind parameterKind = parameter.Kind;

            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            if (newSymbolKind == SymbolKind.ErrorType)
            {
                return true;
            }

            if (parameterKind == SymbolKind.Parameter)
            {
                if (newSymbolKind == SymbolKind.Parameter || newSymbolKind == SymbolKind.Local ||
                    (newSymbolKind == SymbolKind.Method &&
                     ((MethodSymbol)newSymbol).MethodKind == MethodKind.LocalFunction))
                {
                    // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                    return true;
                }

                if (newSymbolKind == SymbolKind.RangeVariable)
                {
                    // The range variable '{0}' conflicts with a previous declaration of '{0}'
                    diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                    return true;
                }
            }

            if (parameterKind == SymbolKind.TypeParameter)
            {
                if (newSymbolKind == SymbolKind.Parameter || newSymbolKind == SymbolKind.Local ||
                    (newSymbolKind == SymbolKind.Method &&
                     ((MethodSymbol)newSymbol).MethodKind == MethodKind.LocalFunction))
                {
                    // CS0412: '{0}': a parameter, local variable, or local function cannot have the same name as a method type parameter
                    diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, newLocation, name);
                    return true;
                }

                if (newSymbolKind == SymbolKind.TypeParameter)
                {
                    // Type parameter declaration name conflicts are detected elsewhere
                    return false;
                }

                if (newSymbolKind == SymbolKind.RangeVariable)
                {
                    // The range variable '{0}' cannot have the same name as a method type parameter
                    diagnostics.Add(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, newLocation, name);
                    return true;
                }
            }

            Debug.Assert(false, "what else could be defined in a method?");
            return true;
        }


        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            Symbol existingDeclaration;


            var parameters = _methodSymbol.Parameters;
            var typeParameters = _methodSymbol.TypeParameters;

            if (parameters.IsEmpty && typeParameters.IsEmpty)
            {
                return false;
            }

            var map = _lazyDefinitionMap;

            if (map == null)
            {
                map = new SmallDictionary<string, Symbol>();
                RecordDefinition(map, parameters);
                RecordDefinition(map, typeParameters);

                _lazyDefinitionMap = map;
            }

            if (map.TryGetValue(name, out existingDeclaration))
            {
                return ReportConflictWithParameter(existingDeclaration, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}
