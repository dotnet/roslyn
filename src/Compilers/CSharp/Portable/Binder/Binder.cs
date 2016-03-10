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
    /// A Binder converts names in to symbols and syntax nodes into bound trees. It is context
    /// dependent, relative to a location in source code.
    /// </summary>
    internal partial class Binder
    {
        internal CSharpCompilation Compilation { get; }
        private readonly Binder _next;

        internal readonly BinderFlags Flags;

        /// <summary>
        /// Used to create a root binder.
        /// </summary>
        internal Binder(CSharpCompilation compilation)
        {
            Debug.Assert(compilation != null);
            this.Flags = compilation.Options.TopLevelBinderFlags;
            this.Compilation = compilation;
        }

        internal Binder(Binder next)
        {
            Debug.Assert(next != null);
            _next = next;
            this.Flags = next.Flags;
            this.Compilation = next.Compilation;
        }

        protected Binder(Binder next, BinderFlags flags)
        {
            Debug.Assert(next != null);
            // Mutually exclusive.
            Debug.Assert(!flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));
            // Implied.
            Debug.Assert(!flags.Includes(BinderFlags.InNestedFinallyBlock) || flags.Includes(BinderFlags.InFinallyBlock | BinderFlags.InCatchBlock));
            _next = next;
            this.Flags = flags;
            this.Compilation = next.Compilation;
        }

        internal bool IsSemanticModelBinder
        {
            get
            {
                return this.Flags.Includes(BinderFlags.SemanticModel);
            }
        }

        // IsEarlyAttributeBinder is called relatively frequently so we want fast code here.
        internal bool IsEarlyAttributeBinder
        {
            get
            {
                return this.Flags.Includes(BinderFlags.EarlyAttributeBinding);
            }
        }

        // Return the nearest enclosing node being bound as a nameof(...) argument, if any, or null if none.
        protected virtual SyntaxNode EnclosingNameofArgument => null;

        /// <summary>
        /// Get the next binder in which to look up a name, if not found by this binder.
        /// </summary>
        internal protected Binder Next
        {
            get
            {
                return _next;
            }
        }

        /// <summary>
        /// <see cref="OverflowChecks.Enabled"/> if we are in an explicitly checked context (within checked block or expression).
        /// <see cref="OverflowChecks.Disabled"/> if we are in an explicitly unchecked context (within unchecked block or expression).
        /// <see cref="OverflowChecks.Implicit"/> otherwise.
        /// </summary>
        protected OverflowChecks CheckOverflow
        {
            get
            {
                // Although C# 4.0 specification says that checked context never flows in a lambda, 
                // the Dev10 compiler implementation always flows the context in, except for 
                // when the lambda is directly a "parameter" of the checked/unchecked expression.
                // For Roslyn we decided to change the spec and always flow the context in.
                // So we don't stop at lambda binder.

                Debug.Assert(!this.Flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));

                return this.Flags.Includes(BinderFlags.CheckedRegion)
                    ? OverflowChecks.Enabled
                    : this.Flags.Includes(BinderFlags.UncheckedRegion)
                        ? OverflowChecks.Disabled
                        : OverflowChecks.Implicit;
            }
        }

        /// <summary>
        /// True if instructions that check overflow should be generated.
        /// </summary>
        /// <remarks>
        /// Spec 7.5.12:
        /// For non-constant expressions (expressions that are evaluated at run-time) that are not 
        /// enclosed by any checked or unchecked operators or statements, the default overflow checking
        /// context is unchecked unless external factors (such as compiler switches and execution 
        /// environment configuration) call for checked evaluation.
        /// </remarks>
        protected bool CheckOverflowAtRuntime
        {
            get
            {
                var result = CheckOverflow;
                return result == OverflowChecks.Enabled || result == OverflowChecks.Implicit && Compilation.Options.CheckOverflow;
            }
        }

        /// <summary>
        /// True if the compiler should check for overflow while evaluating constant expressions.
        /// </summary>
        /// <remarks>
        /// Spec 7.5.12:
        /// For constant expressions (expressions that can be fully evaluated at compile-time), 
        /// the default overflow checking context is always checked. Unless a constant expression 
        /// is explicitly placed in an unchecked context, overflows that occur during the compile-time 
        /// evaluation of the expression always cause compile-time errors.
        /// </remarks>
        internal bool CheckOverflowAtCompileTime
        {
            get
            {
                return CheckOverflow != OverflowChecks.Disabled;
            }
        }

        /// <summary>
        /// Some nodes have special binder's for their contents (like Block's)
        /// </summary>
        internal virtual Binder GetBinder(CSharpSyntaxNode node)
        {
            return this.Next.GetBinder(node);
        }

        /// <summary>
        /// Get locals declared immediately in scope represented by the node.
        /// </summary>
        internal virtual ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            return this.Next.GetDeclaredLocalsForScope(node);
        }

        /// <summary>
        /// The member containing the binding context.  Note that for the purposes of the compiler,
        /// a lambda expression is considered a "member" of its enclosing method, field, or lambda.
        /// </summary>
        internal virtual Symbol ContainingMemberOrLambda
        {
            get
            {
                return Next.ContainingMemberOrLambda;
            }
        }

        /// <summary>
        /// Is the contained code within a member method body?
        /// </summary>
        /// <remarks>
        /// May be false in lambdas that are outside of member method bodies, e.g. lambdas in
        /// field initializers.
        /// </remarks>
        internal virtual bool IsInMethodBody
        {
            get
            {
                return Next.IsInMethodBody;
            }
        }

        /// <summary>
        /// Is the contained code within an iterator block?
        /// </summary>
        /// <remarks>
        /// Will be false in a lambda in an iterator.
        /// </remarks>
        internal virtual bool IsDirectlyInIterator
        {
            get
            {
                return Next.IsDirectlyInIterator;
            }
        }

        /// <summary>
        /// Is the contained code within the syntactic span of an
        /// iterator method?
        /// </summary>
        /// <remarks>
        /// Will be true in a lambda in an iterator.
        /// </remarks>
        internal virtual bool IsIndirectlyInIterator
        {
            get
            {
                return Next.IsIndirectlyInIterator;
            }
        }

        /// <summary>
        /// If we are inside a context where a break statement is legal,
        /// returns the <see cref="GeneratedLabelSymbol"/> that a break statement would branch to.
        /// Returns null otherwise.
        /// </summary>
        internal virtual GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return Next.BreakLabel;
            }
        }

        /// <summary>
        /// If we are inside a context where a continue statement is legal,
        /// returns the <see cref="GeneratedLabelSymbol"/> that a continue statement would branch to.
        /// Returns null otherwise.
        /// </summary>
        internal virtual GeneratedLabelSymbol ContinueLabel
        {
            get
            {
                return Next.ContinueLabel;
            }
        }

        /// <summary>
        /// Get the element type of this iterator.
        /// </summary>
        /// <param name="node">Node to report diagnostics, if any, such as "yield statement cannot be used
        /// inside a lambda expression"</param>
        /// <param name="diagnostics">Where to place any diagnostics</param>
        /// <returns>Element type of the current iterator, or an error type.</returns>
        internal virtual TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            return Next.GetIteratorElementType(node, diagnostics);
        }

        public virtual ConsList<LocalSymbol> ImplicitlyTypedLocalsBeingBound
        {
            get
            {
                return _next.ImplicitlyTypedLocalsBeingBound;
            }
        }

        /// <summary>
        /// The imports for all containing namespace declarations (innermost-to-outermost, including global),
        /// or null if there are none.
        /// </summary>
        internal virtual ImportChain ImportChain
        {
            get
            {
                return _next.ImportChain;
            }
        }

        internal virtual Imports GetImports(ConsList<Symbol> basesBeingResolved)
        {
            return _next.GetImports(basesBeingResolved);
        }

        /// <summary>
        /// The type containing the binding context
        /// </summary>
        internal NamedTypeSymbol ContainingType
        {
            get
            {
                var member = this.ContainingMemberOrLambda;
                Debug.Assert((object)member == null || member.Kind != SymbolKind.ErrorType);
                return (object)member == null
                    ? null
                    : member.Kind == SymbolKind.NamedType
                        ? (NamedTypeSymbol)member
                        : member.ContainingType;
            }
        }


        /// <summary>
        /// Returns true if the binder is binding top-level script code.
        /// </summary>
        internal bool BindingTopLevelScriptCode
        {
            get
            {
                var containingMember = this.ContainingMemberOrLambda;
                switch (containingMember.Kind)
                {
                    case SymbolKind.Method:
                        // global statements
                        return ((MethodSymbol)containingMember).IsScriptInitializer;

                    case SymbolKind.NamedType:
                        // script variable initializers
                        return ((NamedTypeSymbol)containingMember).IsScriptClass;

                    default:
                        return false;
                }
            }
        }

        internal virtual ConstantFieldsInProgress ConstantFieldsInProgress
        {
            get
            {
                return this.Next.ConstantFieldsInProgress;
            }
        }

        internal virtual ConsList<FieldSymbol> FieldsBeingBound
        {
            get
            {
                return this.Next.FieldsBeingBound;
            }
        }

        internal virtual LocalSymbol LocalInProgress
        {
            get
            {
                return this.Next.LocalInProgress;
            }
        }

        internal virtual BoundExpression ConditionalReceiverExpression
        {
            get
            {
                return this.Next.ConditionalReceiverExpression;
            }
        }

        private Conversions _lazyConversions;
        internal Conversions Conversions
        {
            get
            {
                if (_lazyConversions == null)
                {
                    Interlocked.CompareExchange(ref _lazyConversions, new Conversions(this), null);
                }

                return _lazyConversions;
            }
        }

        private OverloadResolution _lazyOverloadResolution;
        internal OverloadResolution OverloadResolution
        {
            get
            {
                if (_lazyOverloadResolution == null)
                {
                    Interlocked.CompareExchange(ref _lazyOverloadResolution, new OverloadResolution(this), null);
                }

                return _lazyOverloadResolution;
            }
        }

        internal static void Error(DiagnosticBag diagnostics, DiagnosticInfo info, CSharpSyntaxNode syntax)
        {
            diagnostics.Add(new CSDiagnostic(info, syntax.Location));
        }

        internal static void Error(DiagnosticBag diagnostics, DiagnosticInfo info, Location location)
        {
            diagnostics.Add(new CSDiagnostic(info, location));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, CSharpSyntaxNode syntax)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), syntax.Location));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, CSharpSyntaxNode syntax, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), syntax.Location));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, SyntaxToken token)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), token.GetLocation()));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, SyntaxToken token, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), token.GetLocation()));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax)
        {
            Error(diagnostics, code, syntax.GetLocation());
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax, params object[] args)
        {
            Error(diagnostics, code, syntax.GetLocation(), args);
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, Location location)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), location));
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, Location location, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), location));
        }

        /// <summary>
        /// Issue an error or warning for a symbol if it is Obsolete. If there is not enough
        /// information to report diagnostics, then store the symbols so that diagnostics
        /// can be reported at a later stage.
        /// </summary>
        internal void ReportDiagnosticsIfObsolete(DiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, bool hasBaseReceiver)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Event:
                case SymbolKind.Property:
                    ReportDiagnosticsIfObsolete(diagnostics, symbol, node, hasBaseReceiver, this.ContainingMemberOrLambda, this.ContainingType, this.Flags);
                    break;
            }
        }

        internal void ReportDiagnosticsIfObsolete(DiagnosticBag diagnostics, Conversion conversion, SyntaxNodeOrToken node, bool hasBaseReceiver)
        {
            if (conversion.IsValid && (object)conversion.Method != null)
            {
                ReportDiagnosticsIfObsolete(diagnostics, conversion.Method, node, hasBaseReceiver);
            }
        }

        internal static void ReportDiagnosticsIfObsolete(
            DiagnosticBag diagnostics,
            Symbol symbol,
            SyntaxNodeOrToken node,
            bool hasBaseReceiver,
            Symbol containingMember,
            NamedTypeSymbol containingType,
            BinderFlags location)
        {
            Debug.Assert((object)symbol != null);

            Debug.Assert(symbol.Kind == SymbolKind.NamedType ||
                         symbol.Kind == SymbolKind.Field ||
                         symbol.Kind == SymbolKind.Method ||
                         symbol.Kind == SymbolKind.Event ||
                         symbol.Kind == SymbolKind.Property);

            // Dev11 also reports on the unconstructed method.  It would be nice to report on 
            // the constructed method, but then we wouldn't be able to walk the override chain.
            if (symbol.Kind == SymbolKind.Method)
            {
                symbol = ((MethodSymbol)symbol).ConstructedFrom;
            }

            // There are two reasons to walk up to the least-overridden member:
            //   1) That's the method to which we will actually emit a call.
            //   2) We don't know what virtual dispatch will do at runtime so an
            //      overriding member is basically a shot in the dark.  Better to
            //      just be consistent and always use the least-overridden member.
            Symbol leastOverriddenSymbol = symbol.GetLeastOverriddenMember(containingType);

            bool checkOverridingSymbol = hasBaseReceiver && !ReferenceEquals(symbol, leastOverriddenSymbol);
            if (checkOverridingSymbol)
            {
                // If we have a base receiver, we must be done with declaration binding, so it should
                // be safe to decode diagnostics.  We want to do this since reporting for the overriding
                // member is conditional on reporting for the overridden member (i.e. we need a definite
                // answer so we don't double-report).  You might think that double reporting just results
                // in cascading diagnostics, but it's possible that the second diagnostic is an error
                // while the first is merely a warning.
                leastOverriddenSymbol.GetAttributes();
            }

            ThreeState reportedOnOverridden = ReportDiagnosticsIfObsoleteInternal(diagnostics, leastOverriddenSymbol, node, containingMember, location);

            // CONSIDER: In place of hasBaseReceiver, dev11 also accepts cases where symbol.ContainingType is a "simple type" (e.g. int)
            // or a special by-ref type (e.g. ArgumentHandle).  These cases are probably more important for other checks performed by
            // ExpressionBinder::PostBindMethod, but they do appear to ObsoleteAttribute as well.  We're skipping them because they
            // don't make much sense for ObsoleteAttribute (e.g. this would seem to address the case where int.ToString has been made
            // obsolete but object.ToString has not).

            // If the overridden member was not definitely obsolete and this is a (non-virtual) base member
            // access, then check the overriding symbol as well.
            if (reportedOnOverridden != ThreeState.True && checkOverridingSymbol)
            {
                Debug.Assert(reportedOnOverridden != ThreeState.Unknown, "We forced attribute binding above.");

                ReportDiagnosticsIfObsoleteInternal(diagnostics, symbol, node, containingMember, location);
            }
        }

        /// <returns>
        /// True if the symbol is definitely obsolete.
        /// False if the symbol is definitely not obsolete.
        /// Unknown if the symbol may be obsolete.
        /// 
        /// NOTE: The return value reflects obsolete-ness, not whether or not the diagnostic was reported.
        /// </returns>
        private static ThreeState ReportDiagnosticsIfObsoleteInternal(DiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, Symbol containingMember, BinderFlags location)
        {
            Debug.Assert(diagnostics != null);

            if (symbol.ObsoleteState == ThreeState.False)
            {
                return ThreeState.False;
            }

            var data = symbol.ObsoleteAttributeData;
            if (data == null)
            {
                // Obsolete attribute has errors.
                return ThreeState.False;
            }

            // If we haven't cracked attributes on the symbol at all or we haven't
            // cracked attribute arguments enough to be able to report diagnostics for
            // ObsoleteAttribute, store the symbol so that we can report diagnostics at a 
            // later stage.
            if (symbol.ObsoleteState == ThreeState.Unknown)
            {
                diagnostics.Add(new LazyObsoleteDiagnosticInfo(symbol, containingMember, location), node.GetLocation());
                return ThreeState.Unknown;
            }

            // After this point, always return True.

            var inObsoleteContext = ObsoleteAttributeHelpers.GetObsoleteContextState(containingMember);

            // If we are in a context that is already obsolete, there is no point reporting
            // more obsolete diagnostics.
            if (inObsoleteContext == ThreeState.True)
            {
                return ThreeState.True;
            }
            // If the context is unknown, then store the symbol so that we can do this check at a
            // later stage
            else if (inObsoleteContext == ThreeState.Unknown)
            {
                diagnostics.Add(new LazyObsoleteDiagnosticInfo(symbol, containingMember, location), node.GetLocation());
                return ThreeState.True;
            }

            // We have all the information we need to report diagnostics right now. So do it.
            var diagInfo = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(symbol, location);
            if (diagInfo != null)
            {
                diagnostics.Add(diagInfo, node.GetLocation());
                return ThreeState.True;
            }

            return ThreeState.True;
        }

        internal void ResolveOverloads<TMember>(
            ImmutableArray<TMember> members,
            ImmutableArray<TypeSymbol> typeArguments,
            ImmutableArray<ArgumentSyntax> arguments,
            OverloadResolutionResult<TMember> result,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            bool allowRefOmittedArguments)
            where TMember : Symbol
        {
            var methodsBuilder = ArrayBuilder<TMember>.GetInstance(members.Length);
            methodsBuilder.AddRange(members);

            var typeArgumentsBuilder = ArrayBuilder<TypeSymbol>.GetInstance(typeArguments.Length);
            typeArgumentsBuilder.AddRange(typeArguments);

            var analyzedArguments = AnalyzedArguments.GetInstance();
            var unusedDiagnostics = DiagnosticBag.GetInstance();
            foreach (var argumentSyntax in arguments)
            {
                BindArgumentAndName(analyzedArguments, unusedDiagnostics, false, argumentSyntax, allowArglist: false);
            }

            OverloadResolution.MethodOrPropertyOverloadResolution(
                methodsBuilder,
                typeArgumentsBuilder,
                analyzedArguments,
                result,
                isMethodGroupConversion: false,
                allowRefOmittedArguments: allowRefOmittedArguments,
                useSiteDiagnostics: ref useSiteDiagnostics);

            methodsBuilder.Free();
            typeArgumentsBuilder.Free();
            analyzedArguments.Free();
            unusedDiagnostics.Free();
        }

        internal bool IsSymbolAccessibleConditional(
            Symbol symbol,
            AssemblySymbol within,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return AccessCheck.IsSymbolAccessible(symbol, within, ref useSiteDiagnostics);
        }

        internal bool IsSymbolAccessibleConditional(
            Symbol symbol,
            NamedTypeSymbol within,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            TypeSymbol throughTypeOpt = null)
        {
            return this.Flags.Includes(BinderFlags.IgnoreAccessibility) || AccessCheck.IsSymbolAccessible(symbol, within, ref useSiteDiagnostics, throughTypeOpt);
        }

        internal bool IsSymbolAccessibleConditional(
            Symbol symbol,
            NamedTypeSymbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            ConsList<Symbol> basesBeingResolved = null)
        {
            if (this.Flags.Includes(BinderFlags.IgnoreAccessibility))
            {
                failedThroughTypeCheck = false;
                return true;
            }

            return AccessCheck.IsSymbolAccessible(symbol, within, throughTypeOpt, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        /// <summary>
        /// Expression lvalue and rvalue requirements.
        /// </summary>
        internal enum BindValueKind : byte
        {
            /// <summary>
            /// Expression is the RHS of an assignment operation.
            /// </summary>
            /// <remarks>
            /// The following are rvalues: values, variables, null literals, properties
            /// and indexers with getters, events. The following are not rvalues:
            /// namespaces, types, method groups, anonymous functions.
            /// </remarks>
            RValue,

            /// <summary>
            /// Expression is the RHS of an assignment operation
            /// and may be a method group.
            /// </summary>
            RValueOrMethodGroup,

            /// <summary>
            /// Expression is the LHS of a simple assignment operation.
            /// </summary>
            Assignment,

            /// <summary>
            /// Expression is the operand of an increment
            /// or decrement operation.
            /// </summary>
            IncrementDecrement,

            /// <summary>
            /// Expression is the LHS of a compound assignment
            /// operation (such as +=).
            /// </summary>
            CompoundAssignment,

            /// <summary>
            /// Expression is an out parameter.
            /// </summary>
            OutParameter,

            /// <summary>
            /// Expression is the operand of an address-of operation (&amp;).
            /// </summary>
            AddressOf,

            /// <summary>
            /// Expression is the receiver of a fixed buffer field access
            /// </summary>
            FixedReceiver,
        }

        /// <summary>
        /// Report diagnostics that should be reported when using a synthesized attribute. 
        /// </summary>
        internal static void ReportUseSiteDiagnosticForSynthesizedAttribute(
            CSharpCompilation compilation,
            WellKnownMember attributeMember,
            DiagnosticBag diagnostics,
            Location location = null,
            CSharpSyntaxNode syntax = null)
        {
            Debug.Assert((location != null) ^ (syntax != null));

            // Dev11 reports use-site diagnostics when an optional attribute is found but is bad for some other reason 
            // (comes from an unified assembly). When the symbol is not found no error is reported. See test VersionUnification_UseSiteDiagnostics_OptionalAttributes.
            bool isOptional = WellKnownMembers.IsSynthesizedAttributeOptional(attributeMember);

            GetWellKnownTypeMember(compilation, attributeMember, diagnostics, location, syntax, isOptional);
        }

#if DEBUG
        // Helper to allow displaying the binder hierarchy in the debugger.
        internal Binder[] GetAllBinders()
        {
            var binders = ArrayBuilder<Binder>.GetInstance();
            for (var binder = this; binder != null; binder = binder.Next)
            {
                binders.Add(binder);
            }
            return binders.ToArrayAndFree();
        }
#endif

    }
}
