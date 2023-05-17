// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The `bool PrintMembers(StringBuilder)` method is responsible for printing members declared
    /// in the containing type that are "printable" (public fields and properties),
    /// and delegating to the base to print inherited printable members. Base members get printed first.
    /// It returns true if the record contains some printable members.
    /// The method is used to implement `ToString()`.
    /// </summary>
    internal sealed class SynthesizedRecordPrintMembers : SynthesizedRecordOrdinaryMethod
    {
        public SynthesizedRecordPrintMembers(
            SourceMemberContainerTypeSymbol containingType,
            IEnumerable<Symbol> userDefinedMembers,
            int memberOffset,
            BindingDiagnosticBag diagnostics)
            : base(
                  containingType,
                  WellKnownMemberNames.PrintMembersMethodName,
                  isReadOnly: IsReadOnly(containingType, userDefinedMembers),
                  hasBody: true,
                  memberOffset: memberOffset,
                  diagnostics)
        {
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics)
        {
            var result = (ContainingType.IsRecordStruct || (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType() && ContainingType.IsSealed)) ?
                DeclarationModifiers.Private :
                DeclarationModifiers.Protected;

            if (ContainingType.IsRecord && !ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
            {
                result |= DeclarationModifiers.Override;
            }
            else
            {
                result |= ContainingType.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual;
            }

            Debug.Assert((result & ~allowedModifiers) == 0);
#if DEBUG
            Debug.Assert(modifiersAreValid(result));
#endif
            return result;

#if DEBUG
            bool modifiersAreValid(DeclarationModifiers modifiers)
            {
                if (ContainingType.IsRecordStruct)
                {
                    return modifiers == DeclarationModifiers.Private;
                }

                if ((modifiers & DeclarationModifiers.AccessibilityMask) != DeclarationModifiers.Private &&
                    (modifiers & DeclarationModifiers.AccessibilityMask) != DeclarationModifiers.Protected)
                {
                    return false;
                }

                modifiers &= ~DeclarationModifiers.AccessibilityMask;

                switch (modifiers)
                {
                    case DeclarationModifiers.None:
                    case DeclarationModifiers.Override:
                    case DeclarationModifiers.Virtual:
                        return true;
                    default:
                        return false;
                }
            }
#endif
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            var annotation = ContainingType.IsRecordStruct ? NullableAnnotation.Oblivious : NullableAnnotation.NotAnnotated;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                        new SourceSimpleParameterSymbol(owner: this,
                            TypeWithAnnotations.Create(Binder.GetWellKnownType(compilation, WellKnownType.System_Text_StringBuilder, diagnostics, location), annotation),
                            ordinal: 0, RefKind.None, ScopedKind.None, "builder", Locations)),
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            var overridden = OverriddenMethod;

            if (overridden is object &&
                !overridden.ContainingType.Equals(ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions))
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseMethod, GetFirstLocation(), this, ContainingType.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            try
            {
                ImmutableArray<Symbol> printableMembers = ContainingType.GetMembers().WhereAsArray(m => isPrintable(m));

                if (ReturnType.IsErrorType() ||
                    printableMembers.Any(static m => m.GetTypeOrReturnType().Type.IsErrorType()))
                {
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                ArrayBuilder<BoundStatement> block;
                BoundParameter builder = F.Parameter(this.Parameters[0]);
                if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType() || ContainingType.IsRecordStruct)
                {
                    if (printableMembers.IsEmpty)
                    {
                        // return false;
                        F.CloseMethod(F.Return(F.Literal(false)));
                        return;
                    }
                    block = ArrayBuilder<BoundStatement>.GetInstance();

                    if (!ContainingType.IsRecordStruct)
                    {
                        var ensureStackMethod = F.WellKnownMethod(
                            WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__EnsureSufficientExecutionStack,
                            isOptional: true);
                        if (ensureStackMethod is not null)
                        {
                            block.Add(F.ExpressionStatement(
                                F.Call(receiver: null, ensureStackMethod)));
                        }
                    }
                }
                else
                {
                    MethodSymbol? basePrintMethod = OverriddenMethod;
                    if (basePrintMethod is null ||
                        basePrintMethod.ReturnType.SpecialType != SpecialType.System_Boolean)
                    {
                        F.CloseMethod(F.ThrowNull()); // an error was reported in base checks already
                        return;
                    }

                    var basePrintCall = F.Call(receiver: F.Base(ContainingType.BaseTypeNoUseSiteDiagnostics), basePrintMethod, builder);
                    if (printableMembers.IsEmpty)
                    {
                        // return base.PrintMembers(builder);
                        F.CloseMethod(F.Return(basePrintCall));
                        return;
                    }
                    else
                    {
                        block = ArrayBuilder<BoundStatement>.GetInstance();
                        // if (base.PrintMembers(builder))
                        //     builder.Append(", ")
                        block.Add(F.If(basePrintCall, makeAppendString(F, builder, ", ")));
                    }
                }

                Debug.Assert(!printableMembers.IsEmpty);

                for (var i = 0; i < printableMembers.Length; i++)
                {
                    // builder.Append(", <name> = "); // if previous members exist
                    // builder.Append("<name> = "); // if it is the first member

                    // The only printable members are fields and properties,
                    // which cannot be generic so as to have variant names

                    var member = printableMembers[i];
                    var memberHeader = $"{member.Name} = ";
                    if (i > 0)
                    {
                        memberHeader = ", " + memberHeader;
                    }

                    block.Add(makeAppendString(F, builder, memberHeader));

                    var value = member.Kind switch
                    {
                        SymbolKind.Field => F.Field(F.This(), (FieldSymbol)member),
                        SymbolKind.Property => F.Property(F.This(), (PropertySymbol)member),
                        _ => throw ExceptionUtilities.UnexpectedValue(member.Kind)
                    };

                    // builder.Append((object)<value>); OR builder.Append(<value>.ToString()); for value types

                    Debug.Assert(value.Type is not null);
                    if (value.Type.IsValueType)
                    {
                        block.Add(F.ExpressionStatement(
                            F.Call(receiver: builder,
                                F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__AppendString),
                                F.Call(value, F.SpecialMethod(SpecialMember.System_Object__ToString)))));
                    }
                    else
                    {
                        block.Add(F.ExpressionStatement(
                            F.Call(receiver: builder,
                                F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__AppendObject),
                                F.Convert(F.SpecialType(SpecialType.System_Object), value))));
                    }
                }

                block.Add(F.Return(F.Literal(true)));

                F.CloseMethod(F.Block(block.ToImmutableAndFree()));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }

            static BoundStatement makeAppendString(SyntheticBoundNodeFactory F, BoundParameter builder, string value)
            {
                return F.ExpressionStatement(F.Call(receiver: builder, F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__AppendString), F.StringLiteral(value)));
            }

            static bool isPrintable(Symbol m)
            {
                if (!IsPublicInstanceMember(m))
                {
                    return false;
                }

                if (m.Kind is SymbolKind.Field && m is not TupleErrorFieldSymbol)
                {
                    return true;
                }

                if (m.Kind is SymbolKind.Property)
                {
                    var property = (PropertySymbol)m;
                    return IsPrintableProperty(property);
                }

                return false;
            }
        }

        internal static void VerifyOverridesPrintMembersFromBase(MethodSymbol overriding, BindingDiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = overriding.ContainingType.BaseTypeNoUseSiteDiagnostics;
            if (baseType.IsObjectType())
            {
                return;
            }

            bool reportAnError = false;

            if (!overriding.IsOverride)
            {
                reportAnError = true;
            }
            else
            {
                var overridden = overriding.OverriddenMethod;

                if (overridden is object &&
                    !overridden.ContainingType.Equals(baseType, TypeCompareKind.AllIgnoreOptions))
                {
                    reportAnError = true;
                }
            }

            if (reportAnError)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseMethod, overriding.GetFirstLocation(), overriding, baseType);
            }
        }

        private static bool IsReadOnly(NamedTypeSymbol containingType, IEnumerable<Symbol> userDefinedMembers)
        {
            return containingType.IsReadOnly || (containingType.IsRecordStruct && AreAllPrintablePropertyGettersReadOnly(userDefinedMembers));
        }

        private static bool AreAllPrintablePropertyGettersReadOnly(IEnumerable<Symbol> members)
        {
            foreach (var member in members)
            {
                if (member.Kind != SymbolKind.Property)
                {
                    continue;
                }

                var property = (PropertySymbol)member;
                if (!IsPublicInstanceMember(property) || !IsPrintableProperty(property))
                {
                    continue;
                }

                var getterMethod = property.GetMethod;
                if (property.GetMethod is not null && !getterMethod.IsEffectivelyReadOnly)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPublicInstanceMember(Symbol m)
        {
            return m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic;
        }

        private static bool IsPrintableProperty(PropertySymbol property)
        {
            return !property.IsIndexer && !property.IsOverride && property.GetMethod is not null;
        }
    }
}
