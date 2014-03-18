// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder for a method body, which places the method's parameters in scope
    /// and notes if the method is an iterator method.
    /// </summary>
    internal sealed class InMethodBinder : LocalScopeBinder
    {
        private readonly MultiDictionary<string, ParameterSymbol> parameterMap;
        private IteratorInfo iteratorInfo;
        private HashSet<string> lazyPossibleMultipleMeanings;

        private static readonly HashSet<string> emptySet = new HashSet<string>();

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
            : base(owner, enclosing)
        {
            Debug.Assert((object)owner != null);
            ForceSingleDefinitions(owner.TypeParameters);

            var parameters = owner.Parameters;
            if (!parameters.IsEmpty)
            {
                ForceSingleDefinitions(owner.Parameters);

                this.parameterMap = new MultiDictionary<string, ParameterSymbol>(parameters.Length, EqualityComparer<string>.Default);
                foreach (var parameter in parameters)
                {
                    this.parameterMap.Add(parameter.Name, parameter);
                }
            }
        }

        protected override bool CanHaveMultipleMeanings(string name)
        {
            var possible = this.lazyPossibleMultipleMeanings ??
                           (this.lazyPossibleMultipleMeanings = ComputeMultipleMeaningSet());

            return possible != emptySet && possible.Contains(name);
        }

        private HashSet<string> ComputeMultipleMeaningSet()
        {
            var ownerSourceSym = this.Owner as SourceMethodSymbol;
            if ((object)ownerSourceSym != null)
            {
                var block = ownerSourceSym.BlockSyntax;
                if (block != null)
                {
                    var collector = new MeaningCollector();
                    collector.Visit(block.CsGreen);
                    if (collector.names != null)
                    {
                        return collector.names;
                    }
                }
            }

            Debug.Assert(emptySet.Count == 0);
            return emptySet;
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.Owner;
            }
        }

        internal void MakeIterator()
        {
            if (this.iteratorInfo == null)
            {
                this.iteratorInfo = IteratorInfo.Empty;
            }
        }

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return this.iteratorInfo != null;
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
            TypeSymbol returnType = this.Owner.ReturnType;

            if (!this.IsDirectlyInIterator)
            {
                // This should only happen when speculating, but we don't have a good way to assert that since the
                // original binder isn't available here.
                // If we're speculating about a yield statement inside a non-iterator method, we'll try to be nice
                // and deduce an iterator element type from the return type.  If we didn't do this, the 
                // TypeInfo.ConvertedType of the yield statement would always be an error type.  However, we will 
                // not mutate any state (i.e. we won't store the result).
                return GetIteratorElementTypeFromReturnType(returnType, node, diagnostics) ?? CreateErrorType();
            }

            if (this.iteratorInfo == IteratorInfo.Empty)
            {
                TypeSymbol elementType = null;
                DiagnosticBag elementTypeDiagnostics = DiagnosticBag.GetInstance();

                elementType = GetIteratorElementTypeFromReturnType(returnType, node, elementTypeDiagnostics);

                if ((object)elementType == null)
                {
                    Error(elementTypeDiagnostics, ErrorCode.ERR_BadIteratorReturn, this.Owner.Locations[0], this.Owner, returnType);
                    elementType = CreateErrorType();
                }

                var info = new IteratorInfo(elementType, elementTypeDiagnostics.ToReadOnlyAndFree());

                Interlocked.CompareExchange(ref this.iteratorInfo, info, IteratorInfo.Empty);
            }

            if (node == null)
            {
                // node==null indicates this we are being called from the top-level of processing of a method. We report
                // the diagnostic, if any, at that time to ensure it is reported exactly once.
                diagnostics.AddRange(this.iteratorInfo.ElementTypeDiagnostics);
            }

            return this.iteratorInfo.ElementType;
        }

        private TypeSymbol GetIteratorElementTypeFromReturnType(TypeSymbol returnType, CSharpSyntaxNode errorLocationNode, DiagnosticBag diagnostics)
        {
            if (returnType.Kind == SymbolKind.NamedType)
            {
                switch (returnType.OriginalDefinition.SpecialType)
                {
                    case SpecialType.System_Collections_IEnumerable:
                    case SpecialType.System_Collections_IEnumerator:
                        return GetSpecialType(SpecialType.System_Object, diagnostics, errorLocationNode);
                    case SpecialType.System_Collections_Generic_IEnumerable_T:
                    case SpecialType.System_Collections_Generic_IEnumerator_T:
                        return ((NamedTypeSymbol)returnType).TypeArgumentsNoUseSiteDiagnostics[0];
                }
            }

            return null;
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (parameterMap == null || (options & LookupOptions.NamespaceAliasesOnly) != 0) return;

            Debug.Assert(result.IsClear);

            var count = parameterMap.GetCountForKey(name);
            if (count == 1)
            {
                ParameterSymbol p;
                parameterMap.TryGetSingleValue(name, out p);
                result.MergeEqual(originalBinder.CheckViability(p, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
            else if (count > 1)
            {
                var parameters = parameterMap[name];
                foreach (var sym in parameters)
                {
                    result.MergeEqual(originalBinder.CheckViability(sym, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                foreach (var parameter in this.Owner.Parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        // Two things can cause a name to have more than one meaning in a scope -
        // 1) declaration of a variable (local, lambda parameter, query variable etc...)
        // 2) use of the simple name in an invocation - when looking up "foo" in foo() we can match a 
        //    different symbol compared to regular use like foo.bar or foo + foo.
        //
        // This visitor collects all the names that are used in the above scenarios assuming that
        // the remaining set of names used in a method is typically larger and knowing that 
        // they cannot have multiple meaning allows to shorcircuit "single meaning" analysis.
        //
        // Since we are not interested in tree positions and parents, we will use green tree here.
        private sealed class MeaningCollector : Syntax.InternalSyntax.CSharpSyntaxVisitor
        {
            internal HashSet<string> names;

            private void Add(Syntax.InternalSyntax.SyntaxToken identifier)
            {
                if (identifier != null)
                {
                    var names = this.names ??
                                (this.names = new HashSet<string>());

                    names.Add(identifier.ValueText);
                }
            }

            public override void VisitVariableDeclarator(Syntax.InternalSyntax.VariableDeclaratorSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitCatchDeclaration(Syntax.InternalSyntax.CatchDeclarationSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitParameter(Syntax.InternalSyntax.ParameterSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitFromClause(Syntax.InternalSyntax.FromClauseSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitLetClause(Syntax.InternalSyntax.LetClauseSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitJoinClause(Syntax.InternalSyntax.JoinClauseSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitJoinIntoClause(Syntax.InternalSyntax.JoinIntoClauseSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitQueryContinuation(Syntax.InternalSyntax.QueryContinuationSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitForEachStatement(Syntax.InternalSyntax.ForEachStatementSyntax node)
            {
                Add(node.Identifier);
                VisitChildren(node);
            }

            public override void VisitInvocationExpression(Syntax.InternalSyntax.InvocationExpressionSyntax node)
            {
                var expr = node.Expression;
                if (expr.Kind == SyntaxKind.IdentifierName)
                {
                    Add(((Syntax.InternalSyntax.IdentifierNameSyntax)expr).Identifier);
                }
                VisitChildren(node);
            }

            public override void Visit(Syntax.InternalSyntax.CSharpSyntaxNode node)
            {
                VisitChildren(node);
            }

            public override void DefaultVisit(Syntax.InternalSyntax.CSharpSyntaxNode node)
            {
                VisitChildren(node);
            }

            private void VisitChildren(Syntax.InternalSyntax.CSharpSyntaxNode node)
            {
                var childCnt = node.SlotCount;

                for (int i = 0; i < childCnt; i++)
                {
                    var child = node.GetSlot(i);
                    if (child != null && child.SlotCount != 0)
                    {
                        if (child.IsList)
                        {
                            VisitChildren((Syntax.InternalSyntax.CSharpSyntaxNode)child);
                        }
                        else
                        {
                            ((Syntax.InternalSyntax.CSharpSyntaxNode)child).Accept(this);
                        }
                    }
                }
            }
        }
    }
}