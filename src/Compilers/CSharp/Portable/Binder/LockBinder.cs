// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using LockTypeInfo = (MethodSymbol EnterScopeMethod, TypeSymbol ScopeType, MethodSymbol ScopeDisposeMethod);

    internal sealed class LockBinder : LockOrUsingBinder
    {
        private readonly LockStatementSyntax _syntax;

        public LockBinder(Binder enclosing, LockStatementSyntax syntax)
            : base(enclosing)
        {
            _syntax = syntax;
        }

        protected override ExpressionSyntax TargetExpressionSyntax
        {
            get
            {
                return _syntax.Expression;
            }
        }

        internal override BoundStatement BindLockStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            // Allow method groups during binding and then rule them out when we check that the expression has
            // a reference type.
            ExpressionSyntax exprSyntax = TargetExpressionSyntax;
            BoundExpression expr = BindTargetExpression(diagnostics, originalBinder);
            TypeSymbol? exprType = expr.Type;

            bool hasErrors = false;

            if (exprType is null)
            {
                if (expr.ConstantValueOpt != ConstantValue.Null || Compilation.FeatureStrictEnabled) // Dev10 allows the null literal.
                {
                    Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, expr.Display);
                    hasErrors = true;
                }
            }
            else if (!exprType.IsReferenceType && (exprType.IsValueType || Compilation.FeatureStrictEnabled))
            {
                Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, exprType);
                hasErrors = true;
            }

            if (exprType?.IsWellKnownTypeLock() == true &&
                TryFindLockTypeInfo(exprType, diagnostics, exprSyntax) is { } lockTypeInfo)
            {
                CheckFeatureAvailability(exprSyntax, MessageID.IDS_FeatureLockObject, diagnostics);

                // Report use-site errors for members we will use in lowering.
                _ = diagnostics.ReportUseSite(lockTypeInfo.EnterScopeMethod, exprSyntax) ||
                    diagnostics.ReportUseSite(lockTypeInfo.ScopeType, exprSyntax) ||
                    diagnostics.ReportUseSite(lockTypeInfo.ScopeDisposeMethod, exprSyntax);

                ReportDiagnosticsIfUnsafeMemberAccess(diagnostics, lockTypeInfo.EnterScopeMethod, exprSyntax);
                AssertNotUnsafeMemberAccess(lockTypeInfo.ScopeType);
                ReportDiagnosticsIfUnsafeMemberAccess(diagnostics, lockTypeInfo.ScopeDisposeMethod, exprSyntax);
            }

            BoundStatement stmt = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);
            Debug.Assert(this.Locals.IsDefaultOrEmpty);
            return new BoundLockStatement(_syntax, expr, stmt, hasErrors);
        }

        // Keep consistent with ISymbolExtensions.TryFindLockTypeInfo.
        internal static LockTypeInfo? TryFindLockTypeInfo(TypeSymbol lockType, BindingDiagnosticBag diagnostics, SyntaxNode syntax)
        {
            const string LockTypeFullName = $"{nameof(System)}.{nameof(System.Threading)}.{WellKnownMemberNames.LockTypeName}";

            var enterScopeMethod = TryFindPublicVoidParameterlessMethod(lockType, WellKnownMemberNames.EnterScopeMethodName);
            if (enterScopeMethod is not { ReturnsVoid: false, RefKind: RefKind.None })
            {
                Error(diagnostics, ErrorCode.ERR_MissingPredefinedMember, syntax, LockTypeFullName, WellKnownMemberNames.EnterScopeMethodName);
                return null;
            }

            var scopeType = enterScopeMethod.ReturnType;
            if (scopeType is not NamedTypeSymbol { Name: WellKnownMemberNames.LockScopeTypeName, Arity: 0, IsValueType: true, IsRefLikeType: true, DeclaredAccessibility: Accessibility.Public } ||
                !TypeSymbol.Equals(scopeType.ContainingType, lockType, TypeCompareKind.ConsiderEverything))
            {
                Error(diagnostics, ErrorCode.ERR_MissingPredefinedMember, syntax, LockTypeFullName, WellKnownMemberNames.EnterScopeMethodName);
                return null;
            }

            var disposeMethod = TryFindPublicVoidParameterlessMethod(scopeType, WellKnownMemberNames.DisposeMethodName);
            if (disposeMethod is not { ReturnsVoid: true })
            {
                Error(diagnostics, ErrorCode.ERR_MissingPredefinedMember, syntax, $"{LockTypeFullName}+{WellKnownMemberNames.LockScopeTypeName}", WellKnownMemberNames.DisposeMethodName);
                return null;
            }

            return new LockTypeInfo
            {
                EnterScopeMethod = enterScopeMethod,
                ScopeType = scopeType,
                ScopeDisposeMethod = disposeMethod,
            };
        }

        // Keep consistent with ISymbolExtensions.TryFindPublicVoidParameterlessMethod.
        private static MethodSymbol? TryFindPublicVoidParameterlessMethod(TypeSymbol type, string name)
        {
            var members = type.GetMembers(name);
            MethodSymbol? result = null;
            foreach (var member in members)
            {
                if (member is MethodSymbol
                    {
                        ParameterCount: 0,
                        Arity: 0,
                        IsStatic: false,
                        DeclaredAccessibility: Accessibility.Public,
                        MethodKind: MethodKind.Ordinary,
                    } method)
                {
                    if (result is not null)
                    {
                        // Ambiguous method found.
                        return null;
                    }

                    result = method;
                }
            }

            return result;
        }
    }
}
