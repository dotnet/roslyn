// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalScopeBinder : Binder
    {
        private ImmutableArray<LocalSymbol> _locals;
        private ImmutableArray<LocalFunctionSymbol> _localFunctions;
        private ImmutableArray<LabelSymbol> _labels;
        private readonly uint _localScopeDepth;

        internal LocalScopeBinder(Binder next)
            : this(next, next.Flags)
        {
        }

        internal LocalScopeBinder(Binder next, BinderFlags flags)
            : base(next, flags)
        {
            var parentDepth = next.LocalScopeDepth;

            if (parentDepth != Binder.TopLevelScope)
            {
                _localScopeDepth = parentDepth + 1;
            }
            else
            {
                //NOTE: TopLevel is special.
                //For our purpose parameters and top level locals are on that level.
                var parentScope = next;
                while (parentScope != null)
                {
                    if (parentScope is InMethodBinder || parentScope is WithLambdaParametersBinder)
                    {
                        _localScopeDepth = Binder.TopLevelScope;
                        break;
                    }

                    if (parentScope is LocalScopeBinder)
                    {
                        _localScopeDepth = Binder.TopLevelScope + 1;
                        break;
                    }

                    parentScope = parentScope.Next;
                    Debug.Assert(parentScope != null);
                }
            }
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

        protected ImmutableArray<LocalSymbol> BuildLocals(SyntaxList<StatementSyntax> statements, Binder enclosingBinder)
        {
#if DEBUG
            Binder currentBinder = enclosingBinder;

            while (true)
            {
                if (this == currentBinder)
                {
                    break;
                }

                currentBinder = currentBinder.Next;
            }
#endif

            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance();
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
                            Binder localDeclarationBinder = enclosingBinder.GetBinder(innerStatement) ?? enclosingBinder;
                            var decl = (LocalDeclarationStatementSyntax)innerStatement;

                            decl.Declaration.Type.VisitRankSpecifiers((rankSpecifier, args) =>
                            {
                                foreach (var expression in rankSpecifier.Sizes)
                                {
                                    if (expression.Kind() != SyntaxKind.OmittedArraySizeExpression)
                                    {
                                        ExpressionVariableFinder.FindExpressionVariables(args.localScopeBinder, args.locals, expression, args.localDeclarationBinder);
                                    }
                                }
                            }, (localScopeBinder: this, locals: locals, localDeclarationBinder: localDeclarationBinder));

                            LocalDeclarationKind kind;
                            if (decl.IsConst)
                            {
                                kind = LocalDeclarationKind.Constant;
                            }
                            else if (decl.UsingKeyword != default(SyntaxToken))
                            {
                                kind = LocalDeclarationKind.UsingVariable;
                            }
                            else
                            {
                                kind = LocalDeclarationKind.RegularVariable;
                            }
                            foreach (var vdecl in decl.Declaration.Variables)
                            {
                                var localSymbol = MakeLocal(decl.Declaration, vdecl, kind, localDeclarationBinder);
                                locals.Add(localSymbol);

                                // also gather expression-declared variables from the bracketed argument lists and the initializers
                                ExpressionVariableFinder.FindExpressionVariables(this, locals, vdecl, localDeclarationBinder);
                            }
                        }
                        break;

                    case SyntaxKind.ExpressionStatement:
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.YieldReturnStatement:
                    case SyntaxKind.ReturnStatement:
                    case SyntaxKind.ThrowStatement:
                        ExpressionVariableFinder.FindExpressionVariables(this, locals, innerStatement, enclosingBinder.GetBinder(innerStatement) ?? enclosingBinder);
                        break;

                    case SyntaxKind.SwitchStatement:
                        var switchStatement = (SwitchStatementSyntax)innerStatement;
                        ExpressionVariableFinder.FindExpressionVariables(this, locals, innerStatement, enclosingBinder.GetBinder(switchStatement.Expression) ?? enclosingBinder);
                        break;

                    case SyntaxKind.LockStatement:
                        Binder statementBinder = enclosingBinder.GetBinder(innerStatement);
                        Debug.Assert(statementBinder != null); // Lock always has a binder.
                        ExpressionVariableFinder.FindExpressionVariables(this, locals, innerStatement, statementBinder);
                        break;

                    default:
                        // no other statement introduces local variables into the enclosing scope
                        break;
                }
            }

            return locals.ToImmutableAndFree();
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

        protected SourceLocalSymbol MakeLocal(VariableDeclarationSyntax declaration, VariableDeclaratorSyntax declarator, LocalDeclarationKind kind, Binder initializerBinderOpt = null)
        {
            return SourceLocalSymbol.MakeLocal(
                this.ContainingMemberOrLambda,
                this,
                true,
                declaration.Type,
                declarator.Identifier,
                kind,
                declarator.Initializer,
                initializerBinderOpt);
        }

        protected LocalFunctionSymbol MakeLocalFunction(LocalFunctionStatementSyntax declaration)
        {
            return new LocalFunctionSymbol(
                this,
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

        internal override uint LocalScopeDepth => _localScopeDepth;

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                        if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, result, null))
                        {
                            result.AddSymbol(local.Value, local.Key, 0);
                        }
                    }
                }
                if (this.LocalFunctionsMap != null)
                {
                    foreach (var local in this.LocalFunctionsMap)
                    {
                        if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, result, null))
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
                // A local variable or function named '{0}' is already defined in this scope
                diagnostics.Add(ErrorCode.ERR_LocalDuplicate, newLocation, name);
                return true;
            }

            switch (newSymbolKind)
            {
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                case SymbolKind.Method:
                case SymbolKind.TypeParameter:
                    // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                    return true;

                case SymbolKind.RangeVariable:
                    // The range variable '{0}' conflicts with a previous declaration of '{0}'
                    diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                    return true;
            }

            Debug.Assert(false, "what else can be declared inside a local scope?");
            diagnostics.Add(ErrorCode.ERR_InternalError, newLocation);
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
    }
}
