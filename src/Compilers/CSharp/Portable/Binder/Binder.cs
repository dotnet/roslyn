// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
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

        internal Binder(Binder next, Conversions conversions = null)
        {
            Debug.Assert(next != null);
            _next = next;
            this.Flags = next.Flags;
            this.Compilation = next.Compilation;
            _lazyConversions = conversions;
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

        private bool IsInsideNameof => this.EnclosingNameofArgument != null;

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
        /// Some nodes have special binders for their contents (like Blocks)
        /// </summary>
        internal virtual Binder GetBinder(SyntaxNode node)
        {
            return this.Next.GetBinder(node);
        }

        /// <summary>
        /// Get locals declared immediately in scope designated by the node.
        /// </summary>
        internal virtual ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            return this.Next.GetDeclaredLocalsForScope(scopeDesignator);
        }

        /// <summary>
        /// Get local functions declared immediately in scope designated by the node.
        /// </summary>
        internal virtual ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            return this.Next.GetDeclaredLocalFunctionsForScope(scopeDesignator);
        }

        /// <summary>
        /// If this binder owns a scope for locals, return syntax node that is used
        /// as the scope designator. Otherwise, null.
        /// </summary>
        internal virtual SyntaxNode ScopeDesignator
        {
            get
            {
                return null;
            }
        }

        internal virtual bool IsLocalFunctionsScopeBinder
        {
            get
            {
                return false;
            }
        }

        internal virtual bool IsLabelsScopeBinder
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// True if this is the top-level binder for a local function or lambda
        /// (including implicit lambdas from query expressions).
        /// </summary>
        internal virtual bool IsNestedFunctionBinder => false;

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
        /// Are we in a context where un-annotated types should be interpreted as non-null?
        /// </summary>
        internal bool AreNullableAnnotationsEnabled(SyntaxTree syntaxTree, int position)
        {
            bool? fromTree = ((CSharpSyntaxTree)syntaxTree).GetNullableContextState(position).AnnotationsState;

            if (fromTree != null)
            {
                return fromTree.GetValueOrDefault();
            }

            return AreNullableAnnotationsGloballyEnabled();
        }

        internal bool AreNullableAnnotationsEnabled(SyntaxToken token)
        {
            return AreNullableAnnotationsEnabled(token.SyntaxTree, token.SpanStart);
        }

        internal virtual bool AreNullableAnnotationsGloballyEnabled()
        {
            return Next.AreNullableAnnotationsGloballyEnabled();
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
        internal virtual TypeWithAnnotations GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            return Next.GetIteratorElementType(node, diagnostics);
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

        /// <summary>
        /// Get <see cref="QuickAttributeChecker"/> that can be used to quickly
        /// check for certain attribute applications in context of this binder.
        /// </summary>
        internal virtual QuickAttributeChecker QuickAttributeChecker
        {
            get
            {
                return _next.QuickAttributeChecker;
            }
        }

        internal virtual Imports GetImports(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _next.GetImports(basesBeingResolved);
        }

        protected virtual bool InExecutableBinder
            => _next.InExecutableBinder;

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
                switch (containingMember?.Kind)
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

        internal static void Error(DiagnosticBag diagnostics, DiagnosticInfo info, SyntaxNode syntax)
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
            if (conversion is { IsValid: true, Method: { } })
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

            var diagnosticKind = ReportDiagnosticsIfObsoleteInternal(diagnostics, leastOverriddenSymbol, node, containingMember, location);

            // CONSIDER: In place of hasBaseReceiver, dev11 also accepts cases where symbol.ContainingType is a "simple type" (e.g. int)
            // or a special by-ref type (e.g. ArgumentHandle).  These cases are probably more important for other checks performed by
            // ExpressionBinder::PostBindMethod, but they do appear to ObsoleteAttribute as well.  We're skipping them because they
            // don't make much sense for ObsoleteAttribute (e.g. this would seem to address the case where int.ToString has been made
            // obsolete but object.ToString has not).

            // If the overridden member was not definitely obsolete and this is a (non-virtual) base member
            // access, then check the overriding symbol as well.
            switch (diagnosticKind)
            {
                case ObsoleteDiagnosticKind.NotObsolete:
                case ObsoleteDiagnosticKind.Lazy:
                    if (checkOverridingSymbol)
                    {
                        Debug.Assert(diagnosticKind != ObsoleteDiagnosticKind.Lazy, "We forced attribute binding above.");
                        ReportDiagnosticsIfObsoleteInternal(diagnostics, symbol, node, containingMember, location);
                    }
                    break;
            }
        }

        internal static ObsoleteDiagnosticKind ReportDiagnosticsIfObsoleteInternal(DiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, Symbol containingMember, BinderFlags location)
        {
            Debug.Assert(diagnostics != null);

            var kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(symbol, containingMember);

            DiagnosticInfo info = null;
            switch (kind)
            {
                case ObsoleteDiagnosticKind.Diagnostic:
                    info = ObsoleteAttributeHelpers.CreateObsoleteDiagnostic(symbol, location);
                    break;
                case ObsoleteDiagnosticKind.Lazy:
                case ObsoleteDiagnosticKind.LazyPotentiallySuppressed:
                    info = new LazyObsoleteDiagnosticInfo(symbol, containingMember, location);
                    break;
            }

            if (info != null)
            {
                diagnostics.Add(info, node.GetLocation());
            }

            return kind;
        }

        internal static bool IsSymbolAccessibleConditional(
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
            ConsList<TypeSymbol> basesBeingResolved = null)
        {
            if (this.Flags.Includes(BinderFlags.IgnoreAccessibility))
            {
                failedThroughTypeCheck = false;
                return true;
            }

            return AccessCheck.IsSymbolAccessible(symbol, within, throughTypeOpt, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
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

        internal BoundExpression WrapWithVariablesIfAny(CSharpSyntaxNode scopeDesignator, BoundExpression expression)
        {
            var locals = this.GetDeclaredLocalsForScope(scopeDesignator);
            return (locals.IsEmpty)
                ? expression
                : new BoundSequence(scopeDesignator, locals, ImmutableArray<BoundExpression>.Empty, expression, expression.Type) { WasCompilerGenerated = true };
        }

        internal BoundStatement WrapWithVariablesIfAny(CSharpSyntaxNode scopeDesignator, BoundStatement statement)
        {
            Debug.Assert(statement.Kind != BoundKind.StatementList);
            var locals = this.GetDeclaredLocalsForScope(scopeDesignator);
            if (locals.IsEmpty)
            {
                return statement;
            }

            return new BoundBlock(statement.Syntax, locals, ImmutableArray.Create(statement))
            { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Should only be used with scopes that could declare local functions.
        /// </summary>
        internal BoundStatement WrapWithVariablesAndLocalFunctionsIfAny(CSharpSyntaxNode scopeDesignator, BoundStatement statement)
        {
            var locals = this.GetDeclaredLocalsForScope(scopeDesignator);
            var localFunctions = this.GetDeclaredLocalFunctionsForScope(scopeDesignator);
            if (locals.IsEmpty && localFunctions.IsEmpty)
            {
                return statement;
            }

            return new BoundBlock(statement.Syntax, locals, localFunctions,
                                  ImmutableArray.Create(statement))
            { WasCompilerGenerated = true };
        }

        internal string Dump()
        {
            return TreeDumper.DumpCompact(DumpAncestors());

            TreeDumperNode DumpAncestors()
            {
                TreeDumperNode current = null;

                for (Binder scope = this; scope != null; scope = scope.Next)
                {
                    var (description, snippet, locals) = Print(scope);
                    var sub = new List<TreeDumperNode>();
                    if (!locals.IsEmpty())
                    {
                        sub.Add(new TreeDumperNode("locals", locals, null));
                    }
                    var currentContainer = scope.ContainingMemberOrLambda;
                    if (currentContainer != null && currentContainer != scope.Next?.ContainingMemberOrLambda)
                    {
                        sub.Add(new TreeDumperNode("containing symbol", currentContainer.ToDisplayString(), null));
                    }
                    if (snippet != null)
                    {
                        sub.Add(new TreeDumperNode($"scope", $"{snippet} ({scope.ScopeDesignator.Kind()})", null));
                    }
                    if (current != null)
                    {
                        sub.Add(current);
                    }
                    current = new TreeDumperNode(description, null, sub);
                }

                return current;
            }

            (string description, string snippet, string locals) Print(Binder scope)
            {
                var locals = string.Join(", ", scope.Locals.SelectAsArray(s => s.Name));
                string snippet = null;
                if (scope.ScopeDesignator != null)
                {
                    var lines = scope.ScopeDesignator.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 1)
                    {
                        snippet = lines[0];
                    }
                    else
                    {
                        var first = lines[0];
                        var last = lines[lines.Length - 1].Trim();
                        var lastSize = Math.Min(last.Length, 12);
                        snippet = first.Substring(0, Math.Min(first.Length, 12)) + " ... " + last.Substring(last.Length - lastSize, lastSize);
                    }
                    snippet = snippet.IsEmpty() ? null : snippet;
                }

                var description = scope.GetType().Name;
                return (description, snippet, locals);
            }
        }
    }
}
