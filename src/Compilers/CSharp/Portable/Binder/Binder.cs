// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal readonly BinderFlags Flags;

        /// <summary>
        /// Used to create a root binder.
        /// </summary>
        internal Binder(CSharpCompilation compilation)
        {
            RoslynDebug.Assert(compilation != null);
            RoslynDebug.Assert(this is BuckStopsHereBinder);
            this.Flags = compilation.Options.TopLevelBinderFlags;
            this.Compilation = compilation;
        }

        internal Binder(Binder next, Conversions? conversions = null)
        {
            RoslynDebug.Assert(next != null);
            Next = next;
            this.Flags = next.Flags;
            this.Compilation = next.Compilation;
            _lazyConversions = conversions;
        }

        protected Binder(Binder next, BinderFlags flags)
        {
            RoslynDebug.Assert(next != null);
            // Mutually exclusive.
            RoslynDebug.Assert(!flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));
            // Implied.
            RoslynDebug.Assert(!flags.Includes(BinderFlags.InNestedFinallyBlock) || flags.Includes(BinderFlags.InFinallyBlock | BinderFlags.InCatchBlock));
            Next = next;
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
        protected virtual SyntaxNode? EnclosingNameofArgument => NextRequired.EnclosingNameofArgument;

        internal virtual bool IsInsideNameof => NextRequired.IsInsideNameof;

        /// <summary>
        /// Get the next binder in which to look up a name, if not found by this binder.
        /// </summary>
        protected internal Binder? Next { get; }

        /// <summary>
        /// Get the next binder in which to look up a name, if not found by this binder, asserting if `Next` is null.
        /// </summary>
        protected internal Binder NextRequired
        {
            get
            {
                Debug.Assert(Next is not null);
                return Next;
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

                RoslynDebug.Assert(!this.Flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));

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
        internal bool CheckOverflowAtRuntime
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

        internal bool UseUpdatedEscapeRules => Compilation.SourceModule.UseUpdatedEscapeRules;

        /// <summary>
        /// Some nodes have special binders for their contents (like Blocks)
        /// </summary>
        internal virtual Binder? GetBinder(SyntaxNode node)
        {
            RoslynDebug.Assert(Next is object);
            return this.Next.GetBinder(node);
        }

        /// <summary>
        /// Gets a binder for a node that must be not null, and asserts
        /// if it is not.
        /// </summary>
        internal Binder GetRequiredBinder(SyntaxNode node)
        {
            var binder = GetBinder(node);
            RoslynDebug.Assert(binder is object);
            return binder;
        }

        /// <summary>
        /// Get locals declared immediately in scope designated by the node.
        /// </summary>
        internal virtual ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            RoslynDebug.Assert(Next is object);
            return this.Next.GetDeclaredLocalsForScope(scopeDesignator);
        }

        /// <summary>
        /// Get local functions declared immediately in scope designated by the node.
        /// </summary>
        internal virtual ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            RoslynDebug.Assert(Next is object);
            return this.Next.GetDeclaredLocalFunctionsForScope(scopeDesignator);
        }

        /// <summary>
        /// If this binder owns a scope for locals, return syntax node that is used
        /// as the scope designator. Otherwise, null.
        /// </summary>
        internal virtual SyntaxNode? ScopeDesignator
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

        internal bool InExpressionTree => (Flags & BinderFlags.InExpressionTree) == BinderFlags.InExpressionTree;

        /// <summary>
        /// True if this is the top-level binder for a local function or lambda
        /// (including implicit lambdas from query expressions).
        /// </summary>
        internal virtual bool IsNestedFunctionBinder => false;

        /// <summary>
        /// The member containing the binding context.  Note that for the purposes of the compiler,
        /// a lambda expression is considered a "member" of its enclosing method, field, or lambda.
        /// </summary>
        internal virtual Symbol? ContainingMemberOrLambda
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return Next.ContainingMemberOrLambda;
            }
        }

        /// <summary>
        /// Are we in a context where un-annotated types should be interpreted as non-null?
        /// </summary>
        internal bool AreNullableAnnotationsEnabled(SyntaxTree syntaxTree, int position)
        {
            CSharpSyntaxTree csTree = (CSharpSyntaxTree)syntaxTree;
            Syntax.NullableContextState context = csTree.GetNullableContextState(position);

            return context.AnnotationsState switch
            {
                Syntax.NullableContextState.State.Enabled => true,
                Syntax.NullableContextState.State.Disabled => false,
                Syntax.NullableContextState.State.ExplicitlyRestored => GetGlobalAnnotationState(),
                Syntax.NullableContextState.State.Unknown =>
                    // IsGeneratedCode may be slow, check global state first:
                    AreNullableAnnotationsGloballyEnabled() &&
                    !csTree.IsGeneratedCode(this.Compilation.Options.SyntaxTreeOptionsProvider, CancellationToken.None),
                _ => throw ExceptionUtilities.UnexpectedValue(context.AnnotationsState)
            };
        }

        internal bool AreNullableAnnotationsEnabled(SyntaxToken token)
        {
            RoslynDebug.Assert(token.SyntaxTree is object);
            return AreNullableAnnotationsEnabled(token.SyntaxTree, token.SpanStart);
        }

        internal virtual bool AreNullableAnnotationsGloballyEnabled()
        {
            RoslynDebug.Assert(Next is object);
            return Next.AreNullableAnnotationsGloballyEnabled();
        }

        protected bool GetGlobalAnnotationState()
        {
            switch (Compilation.Options.NullableContextOptions)
            {
                case NullableContextOptions.Enable:
                case NullableContextOptions.Annotations:
                    return true;

                case NullableContextOptions.Disable:
                case NullableContextOptions.Warnings:
                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(Compilation.Options.NullableContextOptions);
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
                RoslynDebug.Assert(Next is object);
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
                RoslynDebug.Assert(Next is object);
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
                RoslynDebug.Assert(Next is object);
                return Next.IsIndirectlyInIterator;
            }
        }

        /// <summary>
        /// If we are inside a context where a break statement is legal,
        /// returns the <see cref="GeneratedLabelSymbol"/> that a break statement would branch to.
        /// Returns null otherwise.
        /// </summary>
        internal virtual GeneratedLabelSymbol? BreakLabel
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return Next.BreakLabel;
            }
        }

        /// <summary>
        /// If we are inside a context where a continue statement is legal,
        /// returns the <see cref="GeneratedLabelSymbol"/> that a continue statement would branch to.
        /// Returns null otherwise.
        /// </summary>
        internal virtual GeneratedLabelSymbol? ContinueLabel
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return Next.ContinueLabel;
            }
        }

        /// <summary>
        /// Get the element type of this iterator.
        /// </summary>
        /// <returns>Element type of the current iterator, or an error type.</returns>
        internal virtual TypeWithAnnotations GetIteratorElementType()
        {
            RoslynDebug.Assert(Next is object);
            return Next.GetIteratorElementType();
        }

        /// <summary>
        /// The imports for all containing namespace declarations (innermost-to-outermost, including global),
        /// or null if there are none.
        /// </summary>
        internal virtual ImportChain? ImportChain
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return Next.ImportChain;
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
                RoslynDebug.Assert(Next is object);
                return Next.QuickAttributeChecker;
            }
        }

        protected virtual bool InExecutableBinder
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return Next.InExecutableBinder;
            }
        }

        /// <summary>
        /// The type containing the binding context
        /// </summary>
        internal NamedTypeSymbol? ContainingType
        {
            get
            {
                var member = this.ContainingMemberOrLambda;
                RoslynDebug.Assert(member is null || member.Kind != SymbolKind.ErrorType);
                return member switch
                {
                    null => null,
                    NamedTypeSymbol namedType => namedType,
                    _ => member.ContainingType
                };
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
                RoslynDebug.Assert(Next is object);
                return this.Next.ConstantFieldsInProgress;
            }
        }

        internal virtual ConsList<FieldSymbol> FieldsBeingBound
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return this.Next.FieldsBeingBound;
            }
        }

        internal virtual LocalSymbol? LocalInProgress
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return this.Next.LocalInProgress;
            }
        }

        internal virtual NamedTypeSymbol? ParamsCollectionTypeInProgress => null;

        internal virtual MethodSymbol? ParamsCollectionConstructorInProgress => null;

        internal virtual BoundExpression? ConditionalReceiverExpression
        {
            get
            {
                RoslynDebug.Assert(Next is object);
                return this.Next.ConditionalReceiverExpression;
            }
        }

        private Conversions? _lazyConversions;
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

        private OverloadResolution? _lazyOverloadResolution;
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

        internal static void Error(BindingDiagnosticBag diagnostics, DiagnosticInfo info, SyntaxNode syntax)
        {
            diagnostics.Add(new CSDiagnostic(info, syntax.Location));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, DiagnosticInfo info, Location location)
        {
            diagnostics.Add(new CSDiagnostic(info, location));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, CSharpSyntaxNode syntax)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), syntax.Location));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, CSharpSyntaxNode syntax, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), syntax.Location));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxToken token)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), token.GetLocation()));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxToken token, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), token.GetLocation()));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax)
        {
            var location = syntax.GetLocation();
            RoslynDebug.Assert(location is object);
            Error(diagnostics, code, location);
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, SyntaxNodeOrToken syntax, params object[] args)
        {
            var location = syntax.GetLocation();
            RoslynDebug.Assert(location is object);
            Error(diagnostics, code, location, args);
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, Location location)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code), location));
        }

        internal static void Error(BindingDiagnosticBag diagnostics, ErrorCode code, Location location, params object[] args)
        {
            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(code, args), location));
        }

        /// <summary>
        /// Issue an error or warning for a symbol if it is Obsolete. If there is not enough
        /// information to report diagnostics, then store the symbols so that diagnostics
        /// can be reported at a later stage.
        /// </summary>
        /// <remarks>
        /// This method is introduced to move the implicit conversion operator call from the caller
        /// so as to reduce the caller stack frame size
        /// </remarks>
        internal void ReportDiagnosticsIfObsolete(DiagnosticBag diagnostics, Symbol symbol, SyntaxNode node, bool hasBaseReceiver)
        {
            ReportDiagnosticsIfObsolete(diagnostics, symbol, (SyntaxNodeOrToken)node, hasBaseReceiver);
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

        internal void ReportDiagnosticsIfObsolete(BindingDiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, bool hasBaseReceiver)
        {
            if (diagnostics.DiagnosticBag is object)
            {
                ReportDiagnosticsIfObsolete(diagnostics.DiagnosticBag, symbol, node, hasBaseReceiver);
            }
        }

        internal void ReportDiagnosticsIfObsolete(BindingDiagnosticBag diagnostics, Conversion conversion, SyntaxNodeOrToken node, bool hasBaseReceiver)
        {
            if (conversion.IsValid && conversion.Method is object)
            {
                ReportDiagnosticsIfObsolete(diagnostics, conversion.Method, node, hasBaseReceiver);
            }
        }

        internal static void ReportDiagnosticsIfObsolete(
            DiagnosticBag diagnostics,
            Symbol symbol,
            SyntaxNodeOrToken node,
            bool hasBaseReceiver,
            Symbol? containingMember,
            NamedTypeSymbol? containingType,
            BinderFlags location)
        {
            RoslynDebug.Assert(symbol is object);

            RoslynDebug.Assert(symbol.Kind == SymbolKind.NamedType ||
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
                        RoslynDebug.Assert(diagnosticKind != ObsoleteDiagnosticKind.Lazy, "We forced attribute binding above.");
                        ReportDiagnosticsIfObsoleteInternal(diagnostics, symbol, node, containingMember, location);
                    }
                    break;
            }
        }

        internal static void ReportDiagnosticsIfObsolete(
            BindingDiagnosticBag diagnostics,
            Symbol symbol,
            SyntaxNodeOrToken node,
            bool hasBaseReceiver,
            Symbol? containingMember,
            NamedTypeSymbol? containingType,
            BinderFlags location)
        {
            if (diagnostics.DiagnosticBag is object)
            {
                ReportDiagnosticsIfObsolete(diagnostics.DiagnosticBag, symbol, node, hasBaseReceiver, containingMember, containingType, location);
            }
        }

        internal static ObsoleteDiagnosticKind ReportDiagnosticsIfObsoleteInternal(DiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, Symbol? containingMember, BinderFlags location)
        {
            RoslynDebug.Assert(diagnostics != null);

            var kind = ObsoleteAttributeHelpers.GetObsoleteDiagnosticKind(symbol, containingMember);

            DiagnosticInfo? info = null;
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
                if (node.AsNode() is ForEachStatementSyntax foreachSyntax)
                {
                    node = foreachSyntax.ForEachKeyword;
                }

                diagnostics.Add(info, node.GetLocation());
            }

            return kind;
        }

        internal static void ReportDiagnosticsIfObsoleteInternal(BindingDiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, Symbol containingMember, BinderFlags location)
        {
            if (diagnostics.DiagnosticBag is object)
            {
                ReportDiagnosticsIfObsoleteInternal(diagnostics.DiagnosticBag, symbol, node, containingMember, location);
            }
        }

        internal static bool IsDisallowedExtensionInOlderLangVer(MethodSymbol symbol)
        {
            return symbol.IsExtensionBlockMember() && (symbol.IsStatic || symbol.MethodKind != MethodKind.Ordinary);
        }

        internal static void ReportDiagnosticsIfDisallowedExtension(BindingDiagnosticBag diagnostics, MethodSymbol method, SyntaxNode syntax)
        {
            if (IsDisallowedExtensionInOlderLangVer(method))
            {
                MessageID.IDS_FeatureExtensions.CheckFeatureAvailability(diagnostics, syntax);
            }
        }

        internal void ReportIfDisallowedExtensionIndexer(BindingDiagnosticBag diagnostics, PropertySymbol property, SyntaxNode syntax)
        {
            if (property.IsIndexer && property.IsExtensionBlockMember() && property.ContainingModule != Compilation.SourceModule)
            {
                MessageID.IDS_FeatureExtensionIndexers.CheckFeatureAvailability(diagnostics, syntax);
            }
        }

        internal static void ReportDiagnosticsIfUnmanagedCallersOnly(BindingDiagnosticBag diagnostics, MethodSymbol symbol, SyntaxNodeOrToken syntax, bool isDelegateConversion)
        {
            var unmanagedCallersOnlyAttributeData = symbol.GetUnmanagedCallersOnlyAttributeData(forceComplete: false);
            if (unmanagedCallersOnlyAttributeData != null)
            {
                // Either we haven't yet bound the attributes of this method, or there is an UnmanagedCallersOnly present.
                // In the former case, we use a lazy diagnostic that may end up being ignored later, to avoid causing a
                // binding cycle.
                Debug.Assert(syntax.GetLocation() != null);
                diagnostics.Add(unmanagedCallersOnlyAttributeData == UnmanagedCallersOnlyAttributeData.Uninitialized
                                    ? new LazyUnmanagedCallersOnlyMethodCalledDiagnosticInfo(symbol, isDelegateConversion)
                                    : new CSDiagnosticInfo(isDelegateConversion
                                                               ? ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate
                                                               : ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly,
                                                           symbol),
                                syntax.GetLocation()!);
            }
        }

        internal static bool IsSymbolAccessibleConditional(
            Symbol symbol,
            AssemblySymbol within,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return AccessCheck.IsSymbolAccessible(symbol, within, ref useSiteInfo);
        }

        internal bool IsSymbolAccessibleConditional(
            Symbol symbol,
            NamedTypeSymbol within,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            TypeSymbol? throughTypeOpt = null)
        {
            return this.Flags.Includes(BinderFlags.IgnoreAccessibility) || AccessCheck.IsSymbolAccessible(symbol, within, ref useSiteInfo, throughTypeOpt);
        }

        internal bool IsSymbolAccessibleConditional(
            Symbol symbol,
            NamedTypeSymbol within,
            TypeSymbol throughTypeOpt,
            out bool failedThroughTypeCheck,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            ConsList<TypeSymbol>? basesBeingResolved = null)
        {
            if (this.Flags.Includes(BinderFlags.IgnoreAccessibility))
            {
                failedThroughTypeCheck = false;
                return true;
            }

            return AccessCheck.IsSymbolAccessible(symbol, within, throughTypeOpt, out failedThroughTypeCheck, ref useSiteInfo, basesBeingResolved);
        }

        /// <summary>
        /// Report diagnostics that should be reported when using a synthesized attribute. 
        /// </summary>
        internal static void ReportUseSiteDiagnosticForSynthesizedAttribute(
            CSharpCompilation compilation,
            WellKnownMember attributeMember,
            BindingDiagnosticBag diagnostics,
            Location? location = null,
            CSharpSyntaxNode? syntax = null)
        {
            RoslynDebug.Assert((location != null) ^ (syntax != null));

            // Dev11 reports use-site diagnostics when an optional attribute is found but is bad for some other reason 
            // (comes from an unified assembly). When the symbol is not found no error is reported. See test VersionUnification_UseSiteDiagnostics_OptionalAttributes.
            bool isOptional = WellKnownMembers.IsSynthesizedAttributeOptional(attributeMember);

            GetWellKnownTypeMember(compilation, attributeMember, diagnostics, location, syntax, isOptional);
        }

        /// <summary>
        /// Adds diagnostics that should be reported when using a synthesized attribute. 
        /// </summary>
        internal static void AddUseSiteDiagnosticForSynthesizedAttribute(
            CSharpCompilation compilation,
            WellKnownMember attributeMember,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            GetWellKnownTypeMember(compilation,
                attributeMember,
                out var memberUseSiteInfo,
                isOptional: WellKnownMembers.IsSynthesizedAttributeOptional(attributeMember));
            useSiteInfo.Add(memberUseSiteInfo);
        }

        public CompoundUseSiteInfo<AssemblySymbol> GetNewCompoundUseSiteInfo(BindingDiagnosticBag futureDestination)
        {
            return new CompoundUseSiteInfo<AssemblySymbol>(futureDestination, Compilation.Assembly);
        }

#if DEBUG
        // Helper to allow displaying the binder hierarchy in the debugger.
        internal Binder[] GetAllBinders()
        {
            var binders = ArrayBuilder<Binder>.GetInstance();
            for (Binder? binder = this; binder != null; binder = binder.Next)
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
                : new BoundSequence(scopeDesignator, locals, ImmutableArray<BoundExpression>.Empty, expression, getType()) { WasCompilerGenerated = true };

            TypeSymbol getType()
            {
                RoslynDebug.Assert(expression.Type is object);
                return expression.Type;
            }
        }

        internal BoundStatement WrapWithVariablesIfAny(CSharpSyntaxNode scopeDesignator, BoundStatement statement)
        {
            RoslynDebug.Assert(statement.Kind != BoundKind.StatementList);
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

            return new BoundBlock(statement.Syntax, locals, ImmutableArray<MethodSymbol>.CastUp(localFunctions), hasUnsafeModifier: false, instrumentation: null,
                                  ImmutableArray.Create(statement))
            { WasCompilerGenerated = true };
        }

        internal string Dump()
        {
            return TreeDumper.DumpCompact(dumpAncestors());

            TreeDumperNode dumpAncestors()
            {
                TreeDumperNode? current = null;

                for (Binder? scope = this; scope != null; scope = scope.Next)
                {
                    var (description, snippet, locals) = print(scope);
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
                        sub.Add(new TreeDumperNode($"scope", $"{snippet} ({scope.ScopeDesignator?.Kind()})", null));
                    }
                    if (current != null)
                    {
                        sub.Add(current);
                    }
                    current = new TreeDumperNode(description, null, sub);
                }

                RoslynDebug.Assert(current is object);
                return current;
            }

            static (string description, string? snippet, string locals) print(Binder scope)
            {
                var locals = string.Join(", ", scope.Locals.SelectAsArray(s => s.Name));
                string? snippet = null;
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
