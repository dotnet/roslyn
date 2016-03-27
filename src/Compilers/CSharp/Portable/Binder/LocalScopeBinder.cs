// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalScopeBinder : Binder
    {
        private ImmutableArray<LocalSymbol> _locals;
        private ImmutableArray<LocalFunctionSymbol> _localFunctions;
        private ImmutableArray<LabelSymbol> _labels;

        internal LocalScopeBinder(Binder next)
            : this(next, next.Flags)
        {
        }

        internal LocalScopeBinder(Binder next, BinderFlags flags)
            : base(next, flags)
        {
        }

        internal sealed override ImmutableArray<LocalSymbol> Locals
        {
            get
            {
                if (_locals.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _locals, BuildLocals(), default(ImmutableArray<LocalSymbol>));
                }

                return _locals;
            }
        }

        protected virtual ImmutableArray<LocalSymbol> BuildLocals()
        {
            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal sealed override ImmutableArray<LocalFunctionSymbol> LocalFunctions
        {
            get
            {
                if (_localFunctions.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _localFunctions, BuildLocalFunctions(), default(ImmutableArray<LocalFunctionSymbol>));
                }

                return _localFunctions;
            }
        }

        protected virtual ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            return ImmutableArray<LocalFunctionSymbol>.Empty;
        }

        internal sealed override ImmutableArray<LabelSymbol> Labels
        {
            get
            {
                if (_labels.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _labels, BuildLabels(), default(ImmutableArray<LabelSymbol>));
                }

                return _labels;
            }
        }

        protected virtual ImmutableArray<LabelSymbol> BuildLabels()
        {
            return ImmutableArray<LabelSymbol>.Empty;
        }

        private SmallDictionary<string, LocalSymbol> _lazyLocalsMap;
        private SmallDictionary<string, LocalSymbol> LocalsMap
        {
            get
            {
                if (_lazyLocalsMap == null && this.Locals.Length > 0)
                {
                    _lazyLocalsMap = BuildMap(this.Locals);
                }

                return _lazyLocalsMap;
            }
        }

        private SmallDictionary<string, LocalFunctionSymbol> _lazyLocalFunctionsMap;
        private SmallDictionary<string, LocalFunctionSymbol> LocalFunctionsMap
        {
            get
            {
                if (_lazyLocalFunctionsMap == null && this.LocalFunctions.Length > 0)
                {
                    _lazyLocalFunctionsMap = BuildMap(this.LocalFunctions);
                }

                return _lazyLocalFunctionsMap;
            }
        }

        private SmallDictionary<string, LabelSymbol> _lazyLabelsMap;
        private SmallDictionary<string, LabelSymbol> LabelsMap
        {
            get
            {
                if (_lazyLabelsMap == null && this.Labels.Length > 0)
                {
                    _lazyLabelsMap = BuildMap(this.Labels);
                }

                return _lazyLabelsMap;
            }
        }

        private static SmallDictionary<string, TSymbol> BuildMap<TSymbol>(ImmutableArray<TSymbol> array)
            where TSymbol : Symbol
        {
            Debug.Assert(array.Length > 0);

            var map = new SmallDictionary<string, TSymbol>();

            // NOTE: in a rare case of having two symbols with same name the one closer to the array's start wins.
            for (int i = array.Length - 1; i >= 0; i--)
            {
                var symbol = array[i];
                map[symbol.Name] = symbol;
            }

            return map;
        }

        protected ImmutableArray<LocalSymbol> BuildLocals(SyntaxList<StatementSyntax> statements)
        {
            ArrayBuilder<LocalSymbol> locals = null;
            foreach (var statement in statements)
            {
                var innerStatement = statement;

                // drill into any LabeledStatements -- atomic LabelStatements have been bound into
                // wrapped LabeledStatements by this point
                while (innerStatement.Kind() == SyntaxKind.LabeledStatement)
                {
                    innerStatement = ((LabeledStatementSyntax)innerStatement).Statement;
                }

                switch (innerStatement.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:
                        {
                    var decl = (LocalDeclarationStatementSyntax)innerStatement;
                    if (locals == null)
                    {
                        locals = ArrayBuilder<LocalSymbol>.GetInstance();
                    }

                    RefKind refKind = decl.RefKeyword.Kind().GetRefKind();
                    LocalDeclarationKind kind = decl.IsConst ? LocalDeclarationKind.Constant : LocalDeclarationKind.RegularVariable;

                    foreach (var vdecl in decl.Declaration.Variables)
                    {
                        var localSymbol = MakeLocal(refKind, decl.Declaration, vdecl, kind);
                        locals.Add(localSymbol);
                    }
                        }
                        break;
                    case SyntaxKind.LetStatement:
                        {
                            var decl = (LetStatementSyntax)innerStatement;
                            if (locals == null)
                            {
                                locals = ArrayBuilder<LocalSymbol>.GetInstance();
                            }

                            if (decl.Pattern != null)
                            {
                                // Patterns from the let statement introduce bindings into the enclosing scope.
                                BuildAndAddPatternVariables(locals, decl.Pattern);
                            }
                            else
                            {
                                TypeSyntax type = null; // in the syntax "var x = 1", there is no syntax for the variable's type.
                                var localSymbol = SourceLocalSymbol.MakeLocal(this.ContainingMemberOrLambda, this, RefKind.None, type, decl.Identifier, LocalDeclarationKind.PatternVariable);
                                locals.Add(localSymbol);
                            }
                        }
                        break;
                    default:
                        // no other statement introduces local variables into the enclosing scope
                        break;
                }
            }

            return locals?.ToImmutableAndFree() ?? ImmutableArray<LocalSymbol>.Empty;
        }


        protected ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions(SyntaxList<StatementSyntax> statements)
        {
            ArrayBuilder<LocalFunctionSymbol> locals = null;
            foreach (var statement in statements)
            {
                var innerStatement = statement;

                // drill into any LabeledStatements -- atomic LabelStatements have been bound into
                // wrapped LabeledStatements by this point
                while (innerStatement.Kind() == SyntaxKind.LabeledStatement)
                {
                    innerStatement = ((LabeledStatementSyntax)innerStatement).Statement;
                }

                if (innerStatement.Kind() == SyntaxKind.LocalFunctionStatement)
                {
                    var decl = (LocalFunctionStatementSyntax)innerStatement;
                    if (locals == null)
                    {
                        locals = ArrayBuilder<LocalFunctionSymbol>.GetInstance();
                    }

                    var localSymbol = MakeLocalFunction(decl);
                    locals.Add(localSymbol);
                }
            }

            if (locals != null)
            {
                return locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalFunctionSymbol>.Empty;
        }

        protected SourceLocalSymbol MakeLocal(RefKind refKind, VariableDeclarationSyntax declaration, VariableDeclaratorSyntax declarator, LocalDeclarationKind kind)
        {
            return SourceLocalSymbol.MakeLocal(
                this.ContainingMemberOrLambda,
                this,
                refKind,
                declaration.Type,
                declarator.Identifier,
                kind,
                declarator.Initializer);
        }

        protected LocalFunctionSymbol MakeLocalFunction(LocalFunctionStatementSyntax declaration)
        {
            return new LocalFunctionSymbol(
                this,
                this.ContainingType,
                this.ContainingMemberOrLambda,
                declaration);
        }

        protected void BuildLabels(SyntaxList<StatementSyntax> statements, ref ArrayBuilder<LabelSymbol> labels)
        {
            var containingMethod = (MethodSymbol)this.ContainingMemberOrLambda;
            foreach (var statement in statements)
            {
                BuildLabels(containingMethod, statement, ref labels);
            }
        }

        internal static void BuildLabels(MethodSymbol containingMethod, StatementSyntax statement, ref ArrayBuilder<LabelSymbol> labels)
        {
            while (statement.Kind() == SyntaxKind.LabeledStatement)
            {
                var labeledStatement = (LabeledStatementSyntax)statement;
                if (labels == null)
                {
                    labels = ArrayBuilder<LabelSymbol>.GetInstance();
                }

                var labelSymbol = new SourceLabelSymbol(containingMethod, labeledStatement.Identifier);
                labels.Add(labelSymbol);
                statement = labeledStatement.Statement;
            }
        }

        /// <summary>
        /// Call this when you are sure there is a local declaration on this token.  Returns the local.
        /// </summary>
        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            LocalSymbol result = null;
            if (LocalsMap != null && LocalsMap.TryGetValue(nameToken.ValueText, out result))
            {
                if (result.IdentifierToken == nameToken) return (SourceLocalSymbol)result;

                // in error cases we might have more than one declaration of the same name in the same scope
                foreach (var local in this.Locals)
                {
                    if (local.IdentifierToken == nameToken)
                    {
                        return (SourceLocalSymbol)local;
                    }
                }
            }

            return base.LookupLocal(nameToken);
        }

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            LocalFunctionSymbol result = null;
            if (LocalFunctionsMap != null && LocalFunctionsMap.TryGetValue(nameToken.ValueText, out result))
            {
                if (result.NameToken == nameToken) return result;

                // in error cases we might have more than one declaration of the same name in the same scope
                foreach (var local in this.LocalFunctions)
                {
                    if (local.NameToken == nameToken)
                    {
                        return local;
                    }
                }
            }

            return base.LookupLocalFunction(nameToken);
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(options.AreValid());
            Debug.Assert(result.IsClear);

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                var labelsMap = this.LabelsMap;
                if (labelsMap != null)
                {
                    LabelSymbol labelSymbol;
                    if (labelsMap.TryGetValue(name, out labelSymbol))
                    {
                        result.MergeEqual(LookupResult.Good(labelSymbol));
                    }
                }
                return;
            }

            var localsMap = this.LocalsMap;
            if (localsMap != null && (options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                LocalSymbol localSymbol;
                if (localsMap.TryGetValue(name, out localSymbol))
                {
                    result.MergeEqual(originalBinder.CheckViability(localSymbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
                }
            }

            var localFunctionsMap = this.LocalFunctionsMap;
            if (localFunctionsMap != null && options.CanConsiderLocals())
            {
                LocalFunctionSymbol localSymbol;
                if (localFunctionsMap.TryGetValue(name, out localSymbol))
                {
                    result.MergeEqual(originalBinder.CheckViability(localSymbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
                }
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            Debug.Assert(options.AreValid());

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                if (this.LabelsMap != null)
                {
                    foreach (var label in this.LabelsMap)
                    {
                        result.AddSymbol(label.Value, label.Key, 0);
                    }
                }
            }
            if (options.CanConsiderLocals())
            {
                if (this.LocalsMap != null)
                {
                    foreach (var local in this.LocalsMap)
                    {
                        if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, null))
                        {
                            result.AddSymbol(local.Value, local.Key, 0);
                        }
                    }
                }
                if (this.LocalFunctionsMap != null)
                {
                    foreach (var local in this.LocalFunctionsMap)
                    {
                        if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, null))
                        {
                            result.AddSymbol(local.Value, local.Key, 0);
                        }
                    }
                }
            }
        }

        private bool ReportConflictWithLocal(Symbol local, Symbol newSymbol, string name, Location newLocation, DiagnosticBag diagnostics)
        {
            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            if (newSymbolKind == SymbolKind.ErrorType) return true;

            var declaredInThisScope = false;

            declaredInThisScope |= newSymbolKind == SymbolKind.Local && this.Locals.Contains((LocalSymbol)newSymbol);
            declaredInThisScope |= newSymbolKind == SymbolKind.Method && this.LocalFunctions.Contains((LocalFunctionSymbol)newSymbol);

            if (declaredInThisScope && newLocation.SourceSpan.Start >= local.Locations[0].SourceSpan.Start)
            {
                // TODO: Message should change to something like "A {0} named '{1}' is already defined in this scope"

                // A local variable named '{0}' is already defined in this scope
                diagnostics.Add(ErrorCode.ERR_LocalDuplicate, newLocation, name);
                return true;
            }

            if (newSymbolKind == SymbolKind.Local || newSymbolKind == SymbolKind.Parameter || newSymbolKind == SymbolKind.Method || newSymbolKind == SymbolKind.TypeParameter)
            {
                // TODO: Fix up the message for local functions and type parameters. Maybe like the above todo - $"A {newSymbolKind.Localize()} named '{name}' cannot ..."
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

            Debug.Assert(false, "what else can be declared inside a local scope?");
            return false;
        }


        internal virtual bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            LocalSymbol existingLocal = null;
            LocalFunctionSymbol existingLocalFunction = null;

            var localsMap = this.LocalsMap;
            var localFunctionsMap = this.LocalFunctionsMap;

            // TODO: Handle case where 'name' exists in both localsMap and localFunctionsMap. Right now locals are preferred over local functions.
            if ((localsMap != null && localsMap.TryGetValue(name, out existingLocal)) ||
                (localFunctionsMap != null && localFunctionsMap.TryGetValue(name, out existingLocalFunction)))
            {
                var existingSymbol = (Symbol)existingLocal ?? existingLocalFunction;
                if (symbol == existingSymbol)
                {
                    // reference to same symbol, by far the most common case.
                    return false;
                }

                return ReportConflictWithLocal(existingSymbol, symbol, name, location, diagnostics);
            }

            return false;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode node)
        {
            return ImmutableArray<LocalFunctionSymbol>.Empty;
        }
    }
}
