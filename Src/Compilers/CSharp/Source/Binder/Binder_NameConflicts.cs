// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// <para>Once a simple name has been bound to a particular symbol, we need to ensure that symbol is the
        /// <em>only</em>
        /// binding for that simple name in that local scope or any nested local scope.  We do that by constructing a
        /// data structure that records, for each scope (LocalScopeBinder), the meaning of every simple name used within
        /// that scope.
        /// </para>
        /// <para>
        /// The "meaning" is recorded in a dictionary in the local scope that is keyed by simple name (string).  The
        /// meaning records the symbol that was previously found to be the symbol's definition, the location of the
        /// reference, and a "Direct" flag that distinguishes between references directly within that scope
        /// (Direct=true), and references that appear somewhere within more deeply nested scopes (Direct=false).  A
        /// meaning also has a "Reported" boolean flag that is set once a meaning conflict has been reported, to
        /// suppress duplicate or cascaded diagnostics.
        /// </para>
        /// <para>
        /// The algorithm works as follows: Once a definition or use of a particular symbol is found within a given
        /// local scope, an entry with Direct=true is made for that symbol.  It is then resolved against any meaning
        /// already stored for that scope.  Any entry for that name in the scope indicates an error, which is reported.
        /// If the existing entry was Direct=true, then both symbols were used or defined directly within that scope. In
        /// that case an error is reported and their "Reported" flags are set to true to suppress any further
        /// diagnostics. If the existing entry was "Direct=false", then a conflicting reference appeared in a more
        /// nested scope, and the conflict is reported a the location of the more nested scope; only the more nested's
        /// "Reported" flag is then set to true.  (Note that this does not depend on which "came first")
        /// </para>
        /// <para>
        /// If no entry was found in the innermost local scope, then a separate entry with "Direct=false" is created and
        /// resolved, one by one, against all enclosing local scopes.  A scope without an existing entry receives a
        /// reference to this new entry.  A scope with an existing "Direct=false" entry is skipped.  And a scope
        /// containing a "Direct=true" entry for a different symbol represents a conflict, which is reported at the
        /// position of the new "Direct=false" reference, which is the more nested scope.  We also set the "Reported"
        /// flag for the more nested reference.
        /// </para>
        /// <para>
        /// The correctness of this algorithm depends on a subtle condition that doesn't appear explicitly in the code,
        /// so it is worth calling out.  You might worry about reporting a single-definition rule conflict somewhere
        /// while processing a trial binding of a lambda which is later discarded.  Where has the diagnostic gone?  If the
        /// trial binding is discarded but the meaning's "Reported" flag has been set to true, won't we miss reporting
        /// an error?  In fact, that cannot happen.  This diagnostic is consistently reported during any (sequential)
        /// compilation of a given method's body, as explained below.  In "speculative" and SemanticModel trial bindings,
        /// which may occur in the face of concurrency, these diagnostics do not affect the result of the API (the
        /// diagnostics are discarded).
        /// </para>
        /// <para>
        /// The conflicts that arise from two meanings that are Direct references within the same scope do not cause a
        /// problem, because the scope and their entries are either both within the same lambda or not within a lambda
        /// at all.  If they are both within the same lambda, then the diagnostic will be reported each time we attempt
        /// to bind the innermost lambda body.
        /// </para>
        /// <para>
        /// The more subtle situation is when the conflict arises between a Direct and a !Direct meaning.  The Direct
        /// meaning represents a definition of a name or a use of a name in an outer scope, and the !Direct meaning
        /// within a more nested scope.  We always report such conflicts at the location of the more nested scope.  There
        /// are two cases to consider: either the Direct meaning is recorded in the scope before the !Direct entry, for
        /// example when we bind code such as
        /// </para>
        /// <code>{ int i; { int i; } }</code>
        /// <para>
        /// Or the !Direct entry is recorded first, for example in code such as</para>
        /// <code>{ { int i; } int i; }</code>
        /// <para>
        /// Let us first consider the former.  If the more nested reference appears within a lambda, the diagnostic will
        /// be reported during every binding of the lambda, because every binding introduces a new meaning for the
        /// nested variable.
        /// </para>
        /// <para>
        /// Let us now consider the latter situation, and consider what happens if the nested reference appears within a
        /// lambda.  We have the lambda possibly being bound before the outer variable declaration. In that case the
        /// error is not reported until we bind the outer definition. But now the error is reported outside the lambda
        /// binder.  The lambda bindings will appear to succeed without errors.
        /// </para>
        /// <para>
        /// The important points to note are that (1) in either case, we report exactly one error for the conflict, and
        /// (2) it is the same error in both cases, not depending on binding order.  However, in one case the lambda
        /// binding "fails", while in the other it "succeeds"; this may result in subtle differences observable in the
        /// SemanticModel API and possibly different suppression of cascaded diagnostics, as is seen in Dev10.  However,
        /// I have not been able to reproduce these effects in Roslyn
        /// </para>
        /// </summary>
        /// <param name="node">The simple name syntax whose meaning should be invariant within every enclosing
        /// scope</param>
        /// <param name="expression">The bound node to which the given simple name resolved</param>
        /// <param name="diagnostics">A bag into which diagnostics are to be reported</param>
        /// <param name="colorColorVariable">The variable whose name is ambiguous with its type</param>
        private void EnsureInvariantMeaningInScope(SimpleNameSyntax node, BoundExpression expression, DiagnosticBag diagnostics, Symbol colorColorVariable = null)
        {
            if (node.Kind != SyntaxKind.GenericName && !node.HasErrors)
            {
                Symbol symbol = ExpressionSymbol(expression);
                if ((object)symbol != null)
                {
                    if (this.CanHaveMultipleMeanings(symbol.Name))
                    {
                        this.EnsureInvariantMeaningInScope(symbol, node.Location, symbol.Name, diagnostics, colorColorVariable);
                    }
                }
            }
        }

        /// <summary>
        /// A symbol referenced by the bound expression for the purpose of ensuring an invariant meaning in a local
        /// scope.  For a method group, we select one of the methods.
        /// </summary>
        private static Symbol ExpressionSymbol(BoundExpression expression)
        {
            if (expression.Kind == BoundKind.MethodGroup)
            {
                var group = (BoundMethodGroup)expression;
                if (group.Methods.Length != 0)
                {
                    return group.Methods[0];
                }
            }

            return expression.ExpressionSymbol;
        }

        private bool EnsureLambdaParameterInvariantMeaningInScope(Location location, string name, DiagnosticBag diagnostics)
        {
            return EnsureInvariantMeaningInScope(null, location, name, diagnostics);
        }

        internal bool EnsureDeclarationInvariantMeaningInScope(Symbol symbol, DiagnosticBag diagnostics)
        {
            Location location = symbol.Locations.Length != 0 ? symbol.Locations[0] : symbol.ContainingSymbol.Locations[0];
            return EnsureInvariantMeaningInScope(symbol, location, symbol.Name, diagnostics);
        }

        internal void EnterParameters(Symbol memberSymbol, ImmutableArray<ParameterSymbol> parameters, DiagnosticBag diagnostics)
        {
            var meth = memberSymbol as MethodSymbol;
            if ((object)meth != null)
            {
                foreach (var tp in meth.TypeParameters)
                {
                    EnsureDeclarationInvariantMeaningInScope(tp, diagnostics);
                }
            }

            if (!parameters.IsEmpty)
            {
                foreach (var param in parameters)
                {
                    EnsureDeclarationInvariantMeaningInScope(param, diagnostics);
                }
            }
        }

        /// <remarks>
        /// Don't call this one directly - call one of the helpers.
        /// </remarks>
        protected virtual bool EnsureInvariantMeaningInScope(Symbol symbol, Location location, string name, DiagnosticBag diagnostics, Symbol colorColorVariable = null)
        {
            return this.Next.EnsureInvariantMeaningInScope(symbol, location, name, diagnostics, colorColorVariable);
        }
    }

    partial class LocalScopeBinder
    {
        /// <summary>
        /// A map from a name (string) to its single meaning within that scope.
        /// </summary>
        private SmallDictionary<string, SingleMeaning> singleMeaningTable;

        /// <summary>
        /// The meaning assigned to a name within a given scope.
        /// </summary>
        private abstract class SingleMeaning
        {
            /// <summary>
            /// The name
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// The symbol that is its meaning, if known.  May be null in the case of lambda parameters, to allow for the
            /// fact that each binding the lambda gets a different set of parameter symbols.
            /// </summary>
            public abstract Symbol Symbol { get; }

            /// <summary>
            /// True when used or defined directly in this scope. False when used or defined in some more nested,
            /// enclosed scope.
            /// </summary>
            public abstract bool Direct { get; }

            /// <summary>
            /// If this is a type disambiguated from a Color-Color situation, which variable was in scope at that point.
            /// </summary>
            public abstract Symbol ColorColorVariable { get; }

            /// <summary>
            /// The location of its use or declaration
            /// </summary>
            public abstract Location Location { get; }

            /// <summary>
            /// True once a diagnostic has been reported at this location.
            /// </summary>
            public abstract bool Reported { get; set; }

            /// <summary>
            /// The location of the use or definition, for the purpooses of reporting conflicts.
            /// </summary>
            public int Position
            {
                get
                {
                    return Location.SourceSpan.Start;
                }
            }

            /// <summary>
            /// Is this a definition (rather than use) of the given name.  This generally doesn't affect the legality of
            /// a conflict, but may change the diagnostic by which it is reported.
            /// </summary>
            public bool IsDefinition
            {
                get
                {
                    if ((object)Symbol == null)
                    {
                        return true;
                    }

                    var locations = Symbol.Locations;

                    return locations.Any() && locations[0].SourceSpan.Start == Location.SourceSpan.Start;
                }
            }
        }

        /// <summary>
        /// Direct meaning assigned to a name defined within a given scope.
        /// </summary>
        private sealed class DirectSingleMeaning : SingleMeaning
        {
            private readonly string name;
            private readonly Symbol symbol;
            private readonly Location location;
            private readonly Symbol colorColorVariable;
            private bool reported;

            public DirectSingleMeaning(string name, Symbol symbol, Location location, Symbol colorColorVariable = null)
            {
                this.name = name;
                this.symbol = symbol;
                this.location = location;
                this.colorColorVariable = colorColorVariable;
            }

            public override string Name
            {
                get { return name; }
            }

            public override Symbol Symbol
            {
                get { return symbol; }
            }

            public override Location Location
            {
                get { return location; }
            }

            public override Symbol ColorColorVariable
            {
                get { return colorColorVariable; }
            }

            public override bool Reported
            {
                get
                {
                    return reported;
                }
                set
                {
                    reported = value;
                }
            }

            public override bool Direct
            {
                get
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Indirect meaning that results from a name defined within some outer scope.
        /// </summary>
        private sealed class IndirectSingleMeaning : SingleMeaning
        {
            private DirectSingleMeaning directMeaning;

            public IndirectSingleMeaning(DirectSingleMeaning directMeaning)
            {
                this.directMeaning = directMeaning;
            }

            public override string Name
            {
                get { return directMeaning.Name; }
            }

            public override Symbol Symbol
            {
                get { return directMeaning.Symbol; }
            }

            public override Location Location
            {
                get { return directMeaning.Location; }
            }

            public override Symbol ColorColorVariable
            {
                get { return directMeaning.ColorColorVariable; }
            }

            public override bool Reported
            {
                get
                {
                    return directMeaning.Reported;
                }
                set
                {
                    directMeaning.Reported = value;
                }
            }

            public override bool Direct
            {
                get
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Given a meaning, ensure that it is the only meaning within this scope (or report a diagnostic that it isn't).
        /// Returns true if a diagnostic was reported.  Sets done=true if there is no need to check further enclosing
        /// scopes (for example, when this meaning is the same as a previous meaning, or it can be determined that a
        /// diagnostic had previously been reported).
        /// </summary>
        private bool EnsureSingleDefinition(SingleMeaning newMeaning, DiagnosticBag diagnostics, ref bool done)
        {
            if (singleMeaningTable == null) Interlocked.CompareExchange(ref singleMeaningTable, new SmallDictionary<string, SingleMeaning>(), null);

            lock (singleMeaningTable)
            {
                SingleMeaning oldMeaning;
                if (singleMeaningTable.TryGetValue(newMeaning.Name, out oldMeaning))
                {
                    if (oldMeaning.Direct)
                    {
                        return ResolveConflict(oldMeaning, newMeaning, diagnostics, ref done);
                    }
                    else
                    {
                        if (newMeaning.Direct)
                        {
                            singleMeaningTable[newMeaning.Name] = newMeaning;
                            return ResolveConflict(oldMeaning, newMeaning, diagnostics, ref done);
                        }
                        else
                        {
                            // both indirect
                            if ((object)oldMeaning.Symbol != null &&
                                newMeaning.Symbol == oldMeaning.Symbol &&
                                !newMeaning.IsDefinition)
                                done = true; // optimization
                            return false;
                        }
                    }
                }
                else
                {
                    singleMeaningTable.Add(newMeaning.Name, newMeaning);
                    return false;
                }
            }
        }

        /// <summary>
        /// We've found a possible violation of the single-definition rule.  Check that it is an actual conflict, and if
        /// so report it.
        /// </summary>
        /// <param name="oldMeaning">Previously recorded single meaning for this local scope</param>
        /// <param name="newMeaning">New single meaning found for this local scope</param>
        /// <param name="diagnostics">Where to place any diagnostics</param>
        /// <param name="done">Set to true if the caller should stop checking enclosing scope</param>
        /// <returns>true if an error was reported</returns>
        /// <remarks>
        /// Diagnostics produce by this method should not satisfy ErrorFacts.PreventsSuccessfulDelegateConversion because
        /// name conflicts are independent of the delegate type to which an anonymous function is converted.
        /// </remarks>
        private bool ResolveConflict(SingleMeaning oldMeaning, SingleMeaning newMeaning, DiagnosticBag diagnostics, ref bool done)
        {
            if ((object)oldMeaning.Symbol != null && oldMeaning.Symbol == newMeaning.Symbol)
            {
                // reference to same symbol, by far the most common case.
                done = !newMeaning.IsDefinition;
                return false;
            }

            return ResolveConflictComplex(oldMeaning, newMeaning, diagnostics, ref done);
        }

        private bool ResolveConflictComplex(SingleMeaning oldMeaning, SingleMeaning newMeaning, DiagnosticBag diagnostics, ref bool done)
        {
            done = true;
            if ((object)oldMeaning.Symbol != null && (object)newMeaning.Symbol != null && oldMeaning.IsDefinition && newMeaning.Symbol.Locations.Any() && oldMeaning.Symbol.Locations[0] == newMeaning.Symbol.Locations[0])
            {
                // a query variable and its corresponding lambda parameter, for example
                return false;
            }

            Debug.Assert(oldMeaning.Location != newMeaning.Location || oldMeaning.Location == Location.None, "same nonempty location refers to different symbols?");

            // Allow the color-color cases
            TypeSymbol typeMeaningType;
            SingleMeaning typeMeaning;
            SingleMeaning otherMeaning;
            if (IsTypeMeaning(oldMeaning, out typeMeaningType))
            {
                typeMeaning = oldMeaning;
                otherMeaning = newMeaning;
            }
            else
            {
                typeMeaning = newMeaning;
                otherMeaning = oldMeaning;
            }

            // Check for Color-Color conflicts (which are not errors)
            if (((object)typeMeaningType != null || IsTypeMeaning(typeMeaning, out typeMeaningType)) &&
                (object)typeMeaning.ColorColorVariable != null && typeMeaning.ColorColorVariable == otherMeaning.Symbol && // type reference must be contained within scope of the variable
                (!(typeMeaningType is TypeParameterSymbol) || typeMeaningType.ContainingSymbol.Kind != SymbolKind.Method)) // can't reuse a method type parameter name
            {
                done = false;
                return false;
            }

            if (oldMeaning.Direct == newMeaning.Direct)
            {
                int oldPosition = oldMeaning.Position;
                int newPosition = newMeaning.Position;
                SingleMeaning earlier = (oldPosition > newPosition) ? newMeaning : oldMeaning;
                SingleMeaning later = (oldPosition > newPosition) ? oldMeaning : newMeaning;

                Debug.Assert(earlier.Direct && later.Direct);
                if (earlier.Reported || later.Reported) return true;
                later.Reported = earlier.Reported = true;

                Symbol earlierSymbol = earlier.Symbol;
                Symbol laterSymbol = later.Symbol;

                // Quirk of the way we represent lambda parameters.                
                SymbolKind earlierSymbolKind = (object)earlierSymbol == null ? SymbolKind.Parameter : earlierSymbol.Kind;
                SymbolKind laterSymbolKind = (object)laterSymbol == null ? SymbolKind.Parameter : laterSymbol.Kind;

                if (laterSymbolKind == SymbolKind.ErrorType) return true;

                if (earlierSymbolKind == SymbolKind.Local && laterSymbolKind == SymbolKind.Local && earlier.IsDefinition && later.IsDefinition)
                {
                    // A local variable named '{0}' is already defined in this scope
                    diagnostics.Add(ErrorCode.ERR_LocalDuplicate, later.Location, later.Name);
                }
                else if (earlierSymbolKind == SymbolKind.TypeParameter && (laterSymbolKind == SymbolKind.Parameter || laterSymbolKind == SymbolKind.Local))
                {
                    // CS0412: 'X': a parameter or local variable cannot have the same name as a method type parameter
                    diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, later.Location, later.Name);
                }
                else if (earlierSymbolKind == SymbolKind.Parameter && laterSymbolKind == SymbolKind.Parameter)
                {
                    // The parameter name '{0}' is a duplicate
                    diagnostics.Add(ErrorCode.ERR_DuplicateParamName, later.Location, later.Name);
                }
                else if (earlierSymbolKind == SymbolKind.TypeParameter && laterSymbolKind == SymbolKind.TypeParameter)
                {
                    // Type parameter declaration name conflicts are detected elsewhere
                    return false;
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_NameIllegallyOverrides2, later.Location, laterSymbolKind.Localize(), GetMeaningDiagnosticArgument(later), earlierSymbolKind.Localize(), GetMeaningDiagnosticArgument(earlier));
                }

                return true;
            }

            SingleMeaning direct = oldMeaning.Direct ? oldMeaning : newMeaning;
            SingleMeaning indirect = oldMeaning.Direct ? newMeaning : oldMeaning;

            Symbol indirectSymbol = indirect.Symbol;
            Symbol directSymbol = direct.Symbol;

            // Quirk of the way we represent lambda parameters.
            SymbolKind indirectSymbolKind = (object)indirectSymbol == null ? SymbolKind.Parameter : indirectSymbol.Kind;
            SymbolKind directSymbolKind = (object)directSymbol == null ? SymbolKind.Parameter : directSymbol.Kind;

            if ((object)indirectSymbol == null || indirectSymbol.Locations.Any() && indirect.Location == indirectSymbol.Locations[0])
            {
                // indirect was a definition point.
                if (indirect.Reported) return true;
                indirect.Reported = true;

                if (directSymbolKind == SymbolKind.TypeParameter && (object)directSymbol != null && directSymbol.ContainingSymbol == ContainingMemberOrLambda)
                {
                    ErrorCode code = indirectSymbolKind == SymbolKind.RangeVariable
                        // The range variable '{0}' cannot have the same name as a method type parameter
                        ? ErrorCode.ERR_QueryRangeVariableSameAsTypeParam
                        // CS0412: 'X': a parameter or local variable cannot have the same name as a method type parameter
                        : ErrorCode.ERR_LocalSameNameAsTypeParam;

                    diagnostics.Add(code, indirect.Location, indirect.Name);
                }
                else if (directSymbolKind == SymbolKind.Local || directSymbolKind == SymbolKind.Parameter)
                {
                    ErrorCode code = indirectSymbolKind == SymbolKind.RangeVariable
                        // The range variable '{0}' conflicts with a previous declaration of '{0}'
                        ? ErrorCode.ERR_QueryRangeVariableOverrides
                        // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                        : ErrorCode.ERR_LocalIllegallyOverrides;

                    diagnostics.Add(code, indirect.Location, indirect.Name);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_NameIllegallyOverrides, indirect.Location, GetMeaningDiagnosticArgument(direct), GetMeaningDiagnosticArgument(indirect), directSymbolKind.Localize());
                }
            }
            else if (indirectSymbolKind == SymbolKind.Local && indirect.Location.SourceSpan.Start < indirectSymbol.Locations[0].SourceSpan.Start)
            {
                // this will be reported elsewhere as a use before definition
                return false;
            }
            else if (indirectSymbolKind == SymbolKind.ErrorType || directSymbolKind == SymbolKind.ErrorType)
            {
                // avoid cascaded errors
                return false;
            }
            else
            {
                diagnostics.Add(ErrorCode.ERR_NameIllegallyOverrides3, indirect.Location, indirectSymbolKind.Localize(), GetMeaningDiagnosticArgument(indirect), directSymbolKind.Localize(), GetMeaningDiagnosticArgument(direct));
            }

            return true;
        }

        private static object GetMeaningDiagnosticArgument(SingleMeaning meaning)
        {
            return (object)meaning.Symbol ?? meaning.Name;
        }

        private static bool IsTypeMeaning(SingleMeaning meaning, out TypeSymbol type)
        {
            type = GetTypeSymbol(meaning);
            return (object)type != null;
        }

        private static TypeSymbol GetTypeSymbol(SingleMeaning meaning)
        {
            Symbol symbol = meaning.Symbol;
            TypeSymbol type = symbol as TypeSymbol;
            if ((object)type != null) return type;

            AliasSymbol alias = symbol as AliasSymbol;
            return (object)alias == null ? null : alias.Target as TypeSymbol;
        }

        /// <summary>
        /// Enter each symbol as the unique single meaning in the current scope.  This assumes that any conflicts among
        /// them have been previously reported.
        /// </summary>
        protected void ForceSingleDefinitions<T>(ImmutableArray<T> symbols) where T : Symbol
        {
            if (symbols.IsEmpty) return;
            if (singleMeaningTable == null) Interlocked.CompareExchange(ref singleMeaningTable, new SmallDictionary<string, SingleMeaning>(), null);
            lock (singleMeaningTable)
            {
                foreach (var s in symbols)
                {
                    var location = s.Locations.Any() ? s.Locations[0] : s.ContainingSymbol.Locations[0];
                    singleMeaningTable[s.Name] = new DirectSingleMeaning(s.Name, s, location);
                }
            }
        }

        protected override bool EnsureInvariantMeaningInScope(Symbol symbol, Location location, string name, DiagnosticBag diagnostics, Symbol colorColorVariable = null)
        {
            if (string.IsNullOrEmpty(name)) return false;

            bool error = false;
            bool done = false;
            SingleMeaning newMeaning = new DirectSingleMeaning(name, symbol, location, colorColorVariable);
            for (Binder binder = this; binder != null; binder = binder.Next)
            {
                var scope = binder as LocalScopeBinder;
                if (scope == null) continue;
                error |= scope.EnsureSingleDefinition(newMeaning, diagnostics, ref done);
                if (done || scope is InMethodBinder || error) break; // no local scopes enclose methods

                if (newMeaning.Direct)
                {
                    newMeaning = new IndirectSingleMeaning((DirectSingleMeaning)newMeaning);
                }
            }

            return error;
        }
    }
}