// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder for a method body, which places the method's parameters in scope
    /// and notes if the method is an iterator method.
    /// Note: instances of this type can be re-used across different attempts at compiling the same method (caching by binder factory).
    /// </summary>
    internal sealed class InMethodBinder : LocalScopeBinder
    {
        private MultiDictionary<string, ParameterSymbol> _lazyParameterMap;
        private readonly MethodSymbol _methodSymbol;
        private SmallDictionary<string, Symbol> _lazyDefinitionMap;

#if DEBUG
        /// <summary>
        /// This map is used by <see cref="MethodCompiler.BindMethodBody(MethodSymbol, TypeCompilationState, BindingDiagnosticBag, bool, BoundNode?, bool, out ImportChain?, out bool, out bool, out MethodBodySemanticModel.InitialState)"/>
        /// and <see cref="Binder.BindIdentifier"/> to validate some assumptions around identifiers.
        /// 
        /// Values in the dictionary are bit flags.
        /// MethodCompiler.BindMethodBody adds keys with flag == 1 before binding a method body.
        /// Binder.BindIdentifier adds or updates keys with flag == 2.
        /// </summary>
        public ConcurrentDictionary<IdentifierNameSyntax, int> IdentifierMap;
#endif

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

        internal override bool IsNestedFunctionBinder => _methodSymbol.MethodKind == MethodKind.LocalFunction;

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return _methodSymbol.IsIterator;
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

        protected override void ValidateYield(YieldStatementSyntax node, BindingDiagnosticBag diagnostics)
        {
        }

        internal override TypeWithAnnotations GetIteratorElementType()
        {
            RefKind refKind = _methodSymbol.RefKind;
            TypeSymbol returnType = _methodSymbol.ReturnType;

            if (!this.IsDirectlyInIterator)
            {
                // This should only happen when speculating, but we don't have a good way to assert that since the
                // original binder isn't available here.
                // If we're speculating about a yield statement inside a non-iterator method, we'll try to be nice
                // and deduce an iterator element type from the return type.  If we didn't do this, the 
                // TypeInfo.ConvertedType of the yield statement would always be an error type.  However, we will 
                // not mutate any state (i.e. we won't store the result).
                var elementType = GetIteratorElementTypeFromReturnType(Compilation, refKind, returnType, errorLocation: null, diagnostics: null);
                return !elementType.IsDefault ? elementType : TypeWithAnnotations.Create(CreateErrorType());
            }

            return _methodSymbol.IteratorElementTypeWithAnnotations;
        }

        internal static TypeWithAnnotations GetIteratorElementTypeFromReturnType(CSharpCompilation compilation,
            RefKind refKind, TypeSymbol returnType, Location errorLocation, BindingDiagnosticBag diagnostics)
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
                            ReportUseSite(objectType, diagnostics, errorLocation);
                        }
                        return TypeWithAnnotations.Create(objectType);

                    case SpecialType.System_Collections_Generic_IEnumerable_T:
                    case SpecialType.System_Collections_Generic_IEnumerator_T:
                        return ((NamedTypeSymbol)returnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                }

                if (TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T), TypeCompareKind.ConsiderEverything) ||
                    TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T), TypeCompareKind.ConsiderEverything))
                {
                    return ((NamedTypeSymbol)returnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
                }
            }

            return default;
        }

        internal static bool IsAsyncStreamInterface(CSharpCompilation compilation, RefKind refKind, TypeSymbol returnType)
        {
            if (refKind == RefKind.None && returnType.Kind == SymbolKind.NamedType)
            {
                TypeSymbol originalDefinition = returnType.OriginalDefinition;

                if (TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T), TypeCompareKind.ConsiderEverything) ||
                    TypeSymbol.Equals(originalDefinition, compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T), TypeCompareKind.ConsiderEverything))
                {
                    return true;
                }
            }

            return false;
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
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
                    if ((this.Flags & BinderFlags.InEEMethodBinder) != 0 && parameter.Type.IsDisplayClassType())
                    {
                        // Display class parameters shouldn't be accessible in EE
                        continue;
                    }

                    parameterMap.Add(parameter.Name, parameter);
                }

                _lazyParameterMap = parameterMap;
            }

            foreach (var parameterSymbol in parameterMap[name])
            {
                result.MergeEqual(originalBinder.CheckViability(parameterSymbol, arity, options, null, diagnose, ref useSiteInfo));
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
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

        private static bool ReportConflictWithParameter(Symbol parameter, Symbol newSymbol, string name, Location newLocation, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            var locations = parameter.Locations;
            Debug.Assert(!locations.IsEmpty || parameter.IsImplicitlyDeclared);
            var oldLocation = parameter.GetFirstLocationOrNone();
            Debug.Assert(oldLocation != newLocation || oldLocation == Location.None || newLocation.SourceTree?.GetRoot().ContainsDiagnostics == true,
                "same nonempty location refers to different symbols?");
#endif 
            SymbolKind parameterKind = parameter.Kind;

            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            if (newSymbolKind == SymbolKind.ErrorType)
            {
                return true;
            }

            if (parameterKind == SymbolKind.Parameter)
            {
                switch (newSymbolKind)
                {
                    case SymbolKind.Parameter:
                    case SymbolKind.Local:
                        // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                        diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                        return true;

                    case SymbolKind.Method:
                        if (((MethodSymbol)newSymbol).MethodKind == MethodKind.LocalFunction)
                        {
                            goto case SymbolKind.Parameter;
                        }
                        break;

                    case SymbolKind.TypeParameter:
                        // Type parameter declaration name conflicts are not reported, for backwards compatibility.
                        return false;

                    case SymbolKind.RangeVariable:
                        // The range variable '{0}' conflicts with a previous declaration of '{0}'
                        diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                        return true;
                }
            }

            if (parameterKind == SymbolKind.TypeParameter)
            {
                switch (newSymbolKind)
                {
                    case SymbolKind.Parameter:
                    case SymbolKind.Local:
                        if (parameter.ContainingSymbol is NamedTypeSymbol { IsExtension: true })
                        {
                            diagnostics.Add(ErrorCode.ERR_LocalSameNameAsExtensionTypeParameter, newLocation, name);
                        }
                        else
                        {
                            // CS0412: '{0}': a parameter, local variable, or local function cannot have the same name as a method type parameter
                            diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, newLocation, name);
                        }

                        return true;

                    case SymbolKind.Method:
                        if (((MethodSymbol)newSymbol).MethodKind == MethodKind.LocalFunction)
                        {
                            goto case SymbolKind.Parameter;
                        }
                        break;

                    case SymbolKind.TypeParameter:
                        // Type parameter declaration name conflicts are detected elsewhere
                        return false;

                    case SymbolKind.RangeVariable:
                        // The range variable '{0}' cannot have the same name as a method type parameter
                        diagnostics.Add(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, newLocation, name);
                        return true;
                }
            }

            Debug.Assert(false, "what else could be defined in a method?");
            diagnostics.Add(ErrorCode.ERR_InternalError, newLocation);
            return true;
        }

        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, BindingDiagnosticBag diagnostics)
        {
            var parameters = _methodSymbol.Parameters;
            var typeParameters = _methodSymbol.TypeParameters;

            if (_methodSymbol.IsExtensionBlockMember())
            {
                typeParameters = _methodSymbol.ContainingType.TypeParameters.Concat(typeParameters);

                if (_methodSymbol.ContainingType.ExtensionParameter is { Name: not "" } receiver)
                {
                    parameters = parameters.Insert(0, receiver);
                }
            }

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

            Symbol existingDeclaration;
            if (map.TryGetValue(name, out existingDeclaration))
            {
                return ReportConflictWithParameter(existingDeclaration, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}
