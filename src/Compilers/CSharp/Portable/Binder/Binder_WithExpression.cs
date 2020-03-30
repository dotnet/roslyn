// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts an <see cref="ExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindWithExpression(WithExpressionSyntax syntax, DiagnosticBag diagnostics)
        {
            // PROTOTYPE: this entire method is likely to change
            var receiver = BindRValueWithoutTargetType(syntax.Receiver, diagnostics);
            var receiverType = receiver.Type;

            var lookupResult = LookupResult.GetInstance();
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;

            var args = ArrayBuilder<(Symbol?, BoundExpression)>.GetInstance();
            // Bind with expression arguments
            foreach (var initializer in syntax.Initializers)
            {
                var propName = initializer.NameEquals?.Name.Identifier.Text;
                Symbol? member = null;
                if (!(propName is null) && !(receiverType is null))
                {
                    this.LookupMembersInType(
                        lookupResult,
                        receiverType,
                        propName,
                        arity: 0,
                        basesBeingResolved: null,
                        options: LookupOptions.MustBeInstance,
                        originalBinder: this,
                        diagnose: false,
                        useSiteDiagnostics: ref useSiteDiagnostics);
                    if (lookupResult.IsSingleViable &&
                        lookupResult.SingleSymbolOrDefault is var sym)
                    {
                        switch (sym.Kind)
                        {
                            case SymbolKind.Property:
                                var prop = (PropertySymbol)sym;
                                var getter = prop.GetOwnOrInheritedGetMethod();
                                if (IsAccessible(getter, ref useSiteDiagnostics, receiverType))
                                {
                                    goto case SymbolKind.Field;
                                }
                                break;

                            case SymbolKind.Field:
                                member = sym;
                                break;
                        }
                    }

                    if (member is null)
                    {
                        Error(
                            diagnostics,
                            ErrorCode.ERR_NoSuchMemberOrExtension,
                            initializer.NameEquals!.Name.Location,
                            receiverType,
                            propName);
                    }
                }

                var expr = BindValue(initializer.Expression, diagnostics, BindValueKind.RValue);
                lookupResult.Clear();
                args.Add((member, expr));
            }

            if (receiverType is null || receiverType.IsVoidType())
            {
                diagnostics.Add(ErrorCode.ERR_InvalidWithReceiverType, syntax.Receiver.Location);
                receiverType = CreateErrorType();
            }

            if (receiverType.IsErrorType())
            {
                lookupResult.Free();
                return new BoundWithExpression(
                    syntax,
                    receiver,
                    withMethod: null,
                    withMembers: ImmutableArray<Symbol?>.Empty,
                    args.ToImmutableAndFree(),
                    receiverType);
            }

            // PROTOTYPE: The receiver type must have a single declared instance method called 'With'
            LookupMembersWithoutInheritance(
                lookupResult,
                receiverType,
                "With",
                arity: 0,
                LookupOptions.MustBeInstance | LookupOptions.MustBeInvocableIfMember,
                this,
                this.ContainingType,
                diagnose: false,
                ref useSiteDiagnostics,
                ConsList<TypeSymbol>.Empty);

            MethodSymbol? withMethod = null;
            if (lookupResult.IsSingleViable &&
                lookupResult.SingleSymbolOrDefault is MethodSymbol m &&
                m.ContainingType.Equals(receiverType, TypeCompareKind.ConsiderEverything))
            {
                withMethod = m;
            }
            else if (lookupResult.IsMultiViable)
            {
                // If there are multiple With methods, exclude overrides and
                // look for a single remaining one
                foreach (var member in lookupResult.Symbols)
                {
                    if (member is MethodSymbol { IsOverride: false } method &&
                        method.ContainingType.Equals(receiverType, TypeCompareKind.ConsiderEverything))
                    {
                        if (withMethod is null)
                        {
                            withMethod = method;
                        }
                        else
                        {
                            withMethod = null;
                            break;
                        }
                    }
                }
            }

            lookupResult.Clear();
            useSiteDiagnostics = null;
            ImmutableArray<Symbol?> withMembers;

            if (withMethod is null)
            {
                diagnostics.Add(ErrorCode.ERR_NoSingleWithMethod, syntax.Receiver.Location, receiverType);
                withMembers = default;
            }
            else
            {
                // Check return type
                if (!withMethod.ReturnType.IsEqualToOrDerivedFrom(
                    receiverType,
                    TypeCompareKind.ConsiderEverything,
                    ref useSiteDiagnostics))
                {
                    diagnostics.Add(
                        ErrorCode.ERR_ContainingTypeMustDeriveFromWithReturnType,
                        syntax.Receiver.Location,
                        receiverType,
                        withMethod.ReturnType);
                }

                useSiteDiagnostics = null;

                // Build WithMethod member list
                var matchingMembers = ArrayBuilder<Symbol?>.GetInstance(withMethod.ParameterCount);
                foreach (var p in withMethod.Parameters)
                {
                    this.LookupMembersInType(
                        lookupResult,
                        receiverType,
                        p.Name,
                        arity: 0,
                        basesBeingResolved: null,
                        options: LookupOptions.MustBeInstance,
                        originalBinder: this,
                        diagnose: false,
                        useSiteDiagnostics: ref useSiteDiagnostics);

                    Symbol? member;
                    if (!lookupResult.IsSingleViable ||
                        (lookupResult.SingleSymbolOrDefault is var symbol &&
                         symbol.Kind != SymbolKind.Field &&
                         symbol.Kind != SymbolKind.Property))
                    {
                        diagnostics.Add(
                            ErrorCode.ERR_WithParameterWithoutMatchingMember,
                            syntax.Receiver.Location,
                            receiverType,
                            p.Name);
                        member = null;
                    }
                    else
                    {
                        member = lookupResult.SingleSymbolOrDefault;
                        if (!member.GetTypeOrReturnType().Equals(
                                p.TypeWithAnnotations,
                                TypeCompareKind.ConsiderEverything))
                        {
                            diagnostics.Add(
                                ErrorCode.ERR_WithParameterTypeDoesntMatchMemberType,
                                syntax.Receiver.Location,
                                p.Name,
                                p.Type,
                                member.GetTypeOrReturnType());
                        }
                    }

                    lookupResult.Clear();
                    useSiteDiagnostics = null;
                    matchingMembers.Add(member);
                }
                withMembers = matchingMembers.ToImmutableAndFree();
            }

            // Verify that the member name exists in the 'With' method parameter list
            // and that there is an implicit conversion from the expression to the
            // property type
            for (int i = 0; i < args.Count; i++)
            {
                var (member, expr) = args[i];
                if (!(member is null))
                {
                    if (!withMembers.Contains(member))
                    {
                        diagnostics.Add(
                            ErrorCode.ERR_WithMemberArgumentDoesntMatchParameter,
                            syntax.Initializers[i].NameEquals!.Name.Location,
                            member.Name);
                    }

                    var memberType = member.GetTypeOrReturnType().Type;
                    var conversion = Conversions.ClassifyImplicitConversionFromExpression(
                        expr,
                        memberType,
                        ref useSiteDiagnostics);

                    if (!conversion.IsImplicit || !conversion.IsValid)
                    {
                        GenerateImplicitConversionError(
                            diagnostics,
                            expr.Syntax,
                            conversion,
                            expr,
                            memberType);
                    }
                }
                useSiteDiagnostics = null;
            }

            lookupResult.Free();

            return new BoundWithExpression(
                syntax,
                receiver,
                withMethod,
                withMembers,
                args.ToImmutableAndFree(),
                withMethod?.ReturnType ?? receiverType,
                hasErrors: false);
        }
    }
}