// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// True if this binder is in an unsafe context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Does not imply that this compilation allows unsafe regions (could be in an error recovery scenario).
        /// To determine that, check this.Compilation.Options.AllowUnsafe.
        /// </para>
        /// <para>
        /// This describes the current binding context, not necessarily the context of an already-bound operation.
        /// See <see cref="BoundExpression.IsInUnsafeRegion"/> for context-preservation rules.
        /// </para>
        /// </remarks>
        internal bool InUnsafeRegion
        {
            get { return this.Flags.Includes(BinderFlags.UnsafeRegion); }
        }

        internal void ReportDiagnosticsIfUnsafeMemberAccess(BindingDiagnosticBag diagnostics, Symbol symbol, SyntaxNodeOrToken node, bool isInUnsafeRegion)
        {
            if (diagnostics.DiagnosticBag is { } bag)
            {
                ReportDiagnosticsIfUnsafeMemberAccess(bag, symbol, node, static node => node.GetLocation(), isInUnsafeRegion);
            }
        }

        internal void ReportDiagnosticsIfUnsafeMemberAccess(DiagnosticBag diagnostics, Symbol symbol, Location? location, bool isInUnsafeRegion)
        {
            ReportDiagnosticsIfUnsafeMemberAccess(diagnostics, symbol, location, static l => l, isInUnsafeRegion);
        }

        private void ReportDiagnosticsIfUnsafeMemberAccess<T>(DiagnosticBag diagnostics, Symbol symbol, T arg, Func<T, Location?> location, bool isInUnsafeRegion)
        {
            if (IsInsideNameof)
            {
                return;
            }

            var useUpdatedMemorySafetyRules = this.Compilation.SourceModule.UseUpdatedMemorySafetyRules;
            var requiresUnsafeMode = symbol.GetRequiresUnsafeMode(this.FieldsBeingBound);
            if (!useUpdatedMemorySafetyRules && requiresUnsafeMode != RequiresUnsafeMode.Implicit)
            {
                return;
            }

            ReportDiagnosticsIfUnsafeMemberAccess(diagnostics, symbol, requiresUnsafeMode, arg, location, isInUnsafeRegion);

            if (useUpdatedMemorySafetyRules)
            {
                switch (symbol)
                {
                    case MethodSymbol methodSymbol:
                        {
                            var arity = methodSymbol.GetMemberArityIncludingExtension();
                            if (arity != 0)
                            {
                                var typeParameters = methodSymbol.GetTypeParametersIncludingExtension();
                                var typeArguments = methodSymbol.IsExtensionBlockMember()
                                    ? methodSymbol.ContainingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Concat(methodSymbol.TypeArgumentsWithAnnotations)
                                    : methodSymbol.TypeArgumentsWithAnnotations;
                                for (int i = 0; i < arity; i++)
                                {
                                    checkTypeArgumentWithConstructorConstraint(this, typeParameters[i], typeArguments[i].Type, symbol, arg, location, diagnostics, isInUnsafeRegion);
                                }
                            }
                        }
                        break;

                    case NamedTypeSymbol typeSymbol:
                        {
                            var arity = typeSymbol.TypeParameters.Length;
                            for (int i = 0; i < arity; i++)
                            {
                                checkTypeArgumentWithConstructorConstraint(this, typeSymbol.TypeParameters[i], typeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[i].Type, symbol, arg, location, diagnostics, isInUnsafeRegion);
                            }
                        }
                        break;
                }
            }

            static void checkTypeArgumentWithConstructorConstraint(Binder @this, TypeParameterSymbol typeParameter, TypeSymbol typeArgument, Symbol targetSymbol, T arg, Func<T, Location?> location, DiagnosticBag diagnostics, bool isInUnsafeRegion)
            {
                if (typeArgument is NamedTypeSymbol namedTypeArgument)
                {
                    // Resolving constraints or constructors while binding declarations can reenter binding or member synthesis.
                    DiagnosticInfo? diagnosticInfo = @this.ShouldCheckConstraints
                        ? @this.GetUnsafeConstructorConstraintDiagnosticInfo(typeParameter, namedTypeArgument, targetSymbol, isInUnsafeRegion)
                        : new LazyUnsafeConstructorConstraintDiagnosticInfo(@this, typeParameter, namedTypeArgument, targetSymbol, isInUnsafeRegion);

                    if (diagnosticInfo is not null)
                    {
                        diagnostics.Add(new CSDiagnostic(diagnosticInfo, location(arg)));
                    }
                }
            }
        }

        internal CSDiagnosticInfo? GetUnsafeConstructorConstraintDiagnosticInfo(TypeParameterSymbol typeParameter, NamedTypeSymbol typeArgument, Symbol targetSymbol, bool isInUnsafeRegion)
        {
            if (!typeParameter.HasConstructorConstraint && !typeParameter.IsValueType)
            {
                return null;
            }

            foreach (var constructor in typeArgument.InstanceConstructors)
            {
                if (constructor.ParameterCount == 0)
                {
                    return constructor.GetRequiresUnsafeMode(ConsList<FieldSymbol>.Empty) == RequiresUnsafeMode.Explicit
                        ? GetUnsafeDiagnosticInfo(
                            isInUnsafeRegion,
                            disallowedUnder: MemorySafetyRulesVersion.Version2,
                            ignoreUnsafeDiagnosticsSuppression: true,
                            sizeOfTypeOpt: null,
                            customErrorCode: ErrorCode.ERR_UnsafeConstructorConstraint,
                            customArgs: [constructor, typeParameter, targetSymbol.OriginalDefinition])
                        : null;
                }
            }

            return null;
        }

        private void ReportDiagnosticsIfUnsafeMemberAccess<T>(DiagnosticBag diagnostics, Symbol symbol, RequiresUnsafeMode requiresUnsafeMode, T arg, Func<T, Location?> location, bool isInUnsafeRegion)
        {
            Debug.Assert(this.Compilation.SourceModule.UseUpdatedMemorySafetyRules || requiresUnsafeMode == RequiresUnsafeMode.Implicit);

            if (requiresUnsafeMode != RequiresUnsafeMode.None)
            {
                ReportUnsafeIfNotAllowed(arg, location, diagnostics, isInUnsafeRegion, disallowedUnder: MemorySafetyRulesVersion.Version2,
                    customErrorCode: requiresUnsafeMode switch
                    {
                        RequiresUnsafeMode.Explicit => ErrorCode.ERR_UnsafeMemberOperation,
                        RequiresUnsafeMode.Implicit => ErrorCode.ERR_UnsafeMemberOperationCompat,
                        _ => throw ExceptionUtilities.UnexpectedValue(requiresUnsafeMode),
                    },
                    customArgs: [symbol]);
            }
        }

        /// <summary>
        /// If this fails, call <see cref="ReportDiagnosticsIfUnsafeMemberAccess(BindingDiagnosticBag, Symbol, SyntaxNodeOrToken, bool)"/> for the <paramref name="symbol"/> instead and add corresponding tests.
        /// </summary>
        [Conditional("DEBUG")]
        internal void AssertNotUnsafeMemberAccess(Symbol symbol)
        {
            AssertNotUnsafeMemberAccess(symbol, ShouldCheckConstraints);
        }

        /// <inheritdoc cref="AssertNotUnsafeMemberAccess(Symbol)"/>
        [Conditional("DEBUG")]
        internal static void AssertNotUnsafeMemberAccess(Symbol symbol, bool shouldCheckConstraints = true)
        {
            // Resolving `symbol.ToString()` can lead to errors in some cases
            // and `Debug.Assert` on .NET Framework evaluates all interpolated values eagerly,
            // so avoid evaluating that unless we are going to fail anyway.

            var requiresUnsafeMode = symbol.GetRequiresUnsafeMode(ConsList<FieldSymbol>.Empty);
            if (requiresUnsafeMode is not RequiresUnsafeMode.None)
            {
                Debug.Fail($"Symbol {symbol} has {nameof(RequiresUnsafeMode)}={requiresUnsafeMode}.");
            }

            if (symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event)
            {
                Debug.Fail($"Symbol {symbol} has {nameof(symbol.Kind)}={symbol.Kind}.");
            }

            if (shouldCheckConstraints && symbol is NamedTypeSymbol { TypeParameters.Length: > 0 })
            {
                Debug.Fail($"Symbol {symbol} is a generic type.");
            }
        }

        /// <summary>
        /// Reports CS9361 if an uninitialized span stackalloc requires an unsafe context under updated memory safety rules.
        /// </summary>
        internal void ReportUnsafeForUninitializedSpanStackAllocIfRequired(SyntaxNodeOrToken node, BindingDiagnosticBag diagnostics, bool hasInitializer, bool isInUnsafeRegion)
        {
            // Under the updated memory safety rules, a stackalloc_expression is unsafe if being converted to Span/ROS,
            // does not have an initializer, and is used within a member with SkipLocalsInitAttribute.
            // https://github.com/dotnet/roslyn/issues/82546: Confirm this rule with LDM.
            if (!hasInitializer &&
                Compilation.SourceModule.UseUpdatedMemorySafetyRules &&
                ContainingMemberOrLambda is MethodSymbol { AreLocalsZeroed: false })
            {
                ReportUnsafeIfNotAllowed(node, diagnostics, isInUnsafeRegion, disallowedUnder: MemorySafetyRulesVersion.Version2, customErrorCode: ErrorCode.ERR_UnsafeUninitializedStackAlloc);
            }
        }

        internal bool ReportUnsafeIfNotAllowed(
            SyntaxNodeOrToken node,
            BindingDiagnosticBag diagnostics,
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            TypeSymbol? sizeOfTypeOpt = null,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            return diagnostics.DiagnosticBag is { } bag &&
                ReportUnsafeIfNotAllowed(node, bag, isInUnsafeRegion, disallowedUnder, sizeOfTypeOpt, customErrorCode, customArgs);
        }

        internal bool ReportUnsafeIfNotAllowed(
            SyntaxNodeOrToken node,
            DiagnosticBag diagnostics,
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            TypeSymbol? sizeOfTypeOpt = null,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            Debug.Assert((node.Kind() == SyntaxKind.SizeOfExpression) == ((object?)sizeOfTypeOpt != null), "Should have a type for (only) sizeof expressions.");
            return ReportUnsafeIfNotAllowed(
                node,
                static node => node.GetLocation(),
                diagnostics,
                isInUnsafeRegion,
                disallowedUnder,
                sizeOfTypeOpt,
                customErrorCode,
                customArgs);
        }

        internal bool ReportUnsafeIfNotAllowed(
            Location? location,
            BindingDiagnosticBag diagnostics,
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            return diagnostics.DiagnosticBag is { } bag &&
                ReportUnsafeIfNotAllowed(location, bag, isInUnsafeRegion, disallowedUnder, customErrorCode, customArgs);
        }

        internal bool ReportUnsafeIfNotAllowed(
            Location? location,
            DiagnosticBag diagnostics,
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            return ReportUnsafeIfNotAllowed(
                location,
                static l => l,
                diagnostics,
                isInUnsafeRegion,
                disallowedUnder,
                sizeOfTypeOpt: null,
                customErrorCode,
                customArgs);
        }

        /// <param name="disallowedUnder">
        /// Memory safety rules which the current location is disallowed under.
        /// </param>
        /// <returns>True if a diagnostic was reported</returns>
        private bool ReportUnsafeIfNotAllowed<T>(
            T arg,
            Func<T, Location?> location,
            DiagnosticBag diagnostics,
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            TypeSymbol? sizeOfTypeOpt = null,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            var diagnosticInfo = GetUnsafeDiagnosticInfo(isInUnsafeRegion, disallowedUnder, ignoreUnsafeDiagnosticsSuppression: false, sizeOfTypeOpt, customErrorCode, customArgs);
            if (diagnosticInfo == null)
            {
                return false;
            }

            diagnostics.Add(new CSDiagnostic(diagnosticInfo, location(arg)));
            return true;
        }

        private CSDiagnosticInfo? GetUnsafeDiagnosticInfo(
            bool isInUnsafeRegion,
            MemorySafetyRulesVersion disallowedUnder,
            bool ignoreUnsafeDiagnosticsSuppression,
            TypeSymbol? sizeOfTypeOpt,
            ErrorCode? customErrorCode = null,
            object[]? customArgs = null)
        {
            Debug.Assert(sizeOfTypeOpt is null || disallowedUnder is MemorySafetyRulesVersion.Version1);

            if (!ignoreUnsafeDiagnosticsSuppression && this.Flags.Includes(BinderFlags.SuppressUnsafeDiagnostics))
            {
                return null;
            }
            else if (!isInUnsafeRegion)
            {
                var featureDiag = MessageID.IDS_FeatureUnsafeEvolution.GetFeatureAvailabilityDiagnosticInfo(this.Compilation);

                if (disallowedUnder is MemorySafetyRulesVersion.Version1)
                {
                    Debug.Assert(customErrorCode is null && customArgs is null);

                    // Feature available: pointer types are safe.
                    if (featureDiag is null)
                    {
                        return null;
                    }

                    // Feature not available but opted in: report feature availability error.
                    if (this.Compilation.SourceModule.UseUpdatedMemorySafetyRules)
                    {
                        return featureDiag;
                    }

                    // Legacy rules.
                    return ((object?)sizeOfTypeOpt == null)
                        ? new CSDiagnosticInfo(ErrorCode.ERR_UnsafeNeeded)
                        : new CSDiagnosticInfo(ErrorCode.ERR_SizeofUnsafe, sizeOfTypeOpt);
                }

                Debug.Assert(disallowedUnder is MemorySafetyRulesVersion.Version2);

                // Feature available: pointer operations are unsafe.
                if (featureDiag is null)
                {
                    return new CSDiagnosticInfo(customErrorCode ?? ErrorCode.ERR_UnsafeOperation, customArgs ?? []);
                }

                // Feature not available but opted in: report feature availability error.
                if (this.Compilation.SourceModule.UseUpdatedMemorySafetyRules)
                {
                    return featureDiag;
                }

                // This location is disallowed only under updated memory safety rules which are not enabled.
                // We report an error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnder: MemorySafetyRulesVersion.Version1`).
                return null;
            }
            else if (this.IsIndirectlyInIterator && MessageID.IDS_FeatureRefUnsafeInIteratorAsync.GetFeatureAvailabilityDiagnosticInfo(Compilation) is { } unsafeInIteratorDiagnosticInfo)
            {
                if (disallowedUnder is MemorySafetyRulesVersion.Version1)
                {
                    return unsafeInIteratorDiagnosticInfo;
                }

                // This location is disallowed only under updated memory safety rules.
                // We report the RefUnsafeInIteratorAsync langversion error elsewhere, usually at the pointer type itself
                // (where we are called with `disallowedUnder: MemorySafetyRulesVersion.Version1`).
                Debug.Assert(disallowedUnder is MemorySafetyRulesVersion.Version2);
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
