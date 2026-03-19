// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen;

internal partial class CodeGenerator
{
    internal enum AddressKind
    {
        // reference may be written to
        Writeable,

        // reference itself will not be written to, but may be used for call, callvirt.
        // for all purposes it is the same as Writeable, except when fetching an address of an array element
        // where it results in a ".readonly" prefix to deal with array covariance.
        Constrained,

        // reference itself will not be written to, nor it will be used to modify fields.
        ReadOnly,

        // same as ReadOnly, but we are not supposed to get a reference to a clone
        // regardless of compat settings.
        ReadOnlyStrict,
    }

    internal static bool IsAnyReadOnly(AddressKind addressKind) => addressKind >= AddressKind.ReadOnly;

    /// <summary>
    /// Checks if expression directly or indirectly represents a value with its own home. In
    /// such cases it is possible to get a reference without loading into a temporary.
    /// </summary>
    /// <param name="expression">
    /// This should be a lowered node. This method does NOT expect nodes from initial binding.
    /// </param>
    internal static bool HasHome(
        BoundExpression expression,
        AddressKind addressKind,
        Symbol containingSymbol,
        bool peVerifyCompatEnabled,
        HashSet<LocalSymbol> stackLocalsOpt)
    {
        Debug.Assert(containingSymbol is object);

        switch (expression.Kind)
        {
            case BoundKind.ArrayAccess:
                if (addressKind == AddressKind.ReadOnly && !expression.Type.IsValueType && peVerifyCompatEnabled)
                {
                    // due to array covariance getting a reference may throw ArrayTypeMismatch when element is not a struct, 
                    // passing "readonly." prefix would prevent that, but it is unverifiable, so will make a copy in compat case
                    return false;
                }

                return true;

            case BoundKind.PointerIndirectionOperator:
            case BoundKind.RefValueOperator:
                return true;

            case BoundKind.ThisReference:
                var type = expression.Type;
                if (type.IsReferenceType)
                {
                    Debug.Assert(IsAnyReadOnly(addressKind), "`this` is readonly in classes");
                    return true;
                }

                if (!IsAnyReadOnly(addressKind) && containingSymbol is MethodSymbol { ContainingSymbol: NamedTypeSymbol, IsEffectivelyReadOnly: true })
                {
                    return false;
                }

                return true;

            case BoundKind.ThrowExpression:
                // vacuously this is true, we can take address of throw without temps
                return true;

            case BoundKind.Parameter:
                return IsAnyReadOnly(addressKind) ||
                    ((BoundParameter)expression).ParameterSymbol.RefKind is not (RefKind.In or RefKind.RefReadOnlyParameter);

            case BoundKind.Local:
                // locals have home unless they are byval stack locals or ref-readonly
                // locals in a mutating call
                var local = ((BoundLocal)expression).LocalSymbol;
                return !((CodeGenerator.IsStackLocal(local, stackLocalsOpt) && local.RefKind == RefKind.None) ||
                    (!IsAnyReadOnly(addressKind) && local.RefKind == RefKind.RefReadOnly));

            case BoundKind.Call:
                var methodRefKind = ((BoundCall)expression).Method.RefKind;
                return methodRefKind == RefKind.Ref ||
                       (IsAnyReadOnly(addressKind) && methodRefKind == RefKind.RefReadOnly);

            case BoundKind.Dup:
                //NB: Dup represents locals that do not need IL slot
                var dupRefKind = ((BoundDup)expression).RefKind;
                return dupRefKind == RefKind.Ref ||
                    (IsAnyReadOnly(addressKind) && dupRefKind == RefKind.RefReadOnly);

            case BoundKind.FieldAccess:
                return FieldAccessHasHome((BoundFieldAccess)expression, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

            case BoundKind.Sequence:
                return HasHome(((BoundSequence)expression).Value, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

            case BoundKind.AssignmentOperator:
                var assignment = (BoundAssignmentOperator)expression;
                if (!assignment.IsRef)
                {
                    return false;
                }
                var lhsRefKind = assignment.Left.GetRefKind();
                return lhsRefKind == RefKind.Ref ||
                    (IsAnyReadOnly(addressKind) && lhsRefKind is RefKind.RefReadOnly or RefKind.RefReadOnlyParameter);

            case BoundKind.ComplexConditionalReceiver:
                Debug.Assert(HasHome(
                    ((BoundComplexConditionalReceiver)expression).ValueTypeReceiver,
                    addressKind,
                    containingSymbol,
                    peVerifyCompatEnabled,
                    stackLocalsOpt));
                Debug.Assert(HasHome(
                    ((BoundComplexConditionalReceiver)expression).ReferenceTypeReceiver,
                    addressKind,
                    containingSymbol,
                    peVerifyCompatEnabled,
                    stackLocalsOpt));
                goto case BoundKind.ConditionalReceiver;

            case BoundKind.ConditionalReceiver:
                //ConditionalReceiver is a noop from Emit point of view. - it represents something that has already been pushed. 
                //We should never need a temp for it. 
                return true;

            case BoundKind.ConditionalOperator:
                var conditional = (BoundConditionalOperator)expression;

                // only ref conditional may be referenced as a variable
                if (!conditional.IsRef)
                {
                    return false;
                }

                // branch that has no home will need a temporary
                // if both have no home, just say whole expression has no home 
                // so we could just use one temp for the whole thing
                return HasHome(conditional.Consequence, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt)
                    && HasHome(conditional.Alternative, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

            default:
                return false;
        }
    }

    /// <summary>
    /// Special HasHome for fields.
    /// A field has a readable home unless the field is a constant.
    /// A ref readonly field doesn't have a writable home.
    /// Other fields have a writable home unless the field is a readonly value
    /// and is used outside of a constructor or init method.
    /// </summary>
    private static bool FieldAccessHasHome(
        BoundFieldAccess fieldAccess,
        AddressKind addressKind,
        Symbol containingSymbol,
        bool peVerifyCompatEnabled,
        HashSet<LocalSymbol> stackLocalsOpt)
    {
        Debug.Assert(containingSymbol is object);

        FieldSymbol field = fieldAccess.FieldSymbol;

        // const fields are literal values with no homes. (ex: decimal.Zero)
        if (field.IsConst)
        {
            return false;
        }

        if (field.RefKind is RefKind.Ref)
        {
            return true;
        }

        // in readonly situations where ref to a copy is not allowed, consider fields as addressable
        if (addressKind == AddressKind.ReadOnlyStrict)
        {
            return true;
        }

        // ReadOnly references can always be taken unless we are in peverify compat mode
        if (addressKind == AddressKind.ReadOnly && !peVerifyCompatEnabled)
        {
            return true;
        }

        // Some field accesses must be values; values do not have homes.
        if (fieldAccess.IsByValue)
        {
            return false;
        }

        if (field.RefKind == RefKind.RefReadOnly)
        {
            return false;
        }

        Debug.Assert(field.RefKind == RefKind.None);

        if (!field.IsReadOnly)
        {
            // in a case if we have a writeable struct field with a receiver that only has a readable home we would need to pass it via a temp.
            // it would be advantageous to make a temp for the field, not for the outer struct, since the field is smaller and we can get to is by fetching references.
            // NOTE: this would not be profitable if we have to satisfy verifier, since for verifiability 
            //       we would not be able to dig for the inner field using references and the outer struct will have to be copied to a temp anyways.
            if (!peVerifyCompatEnabled)
            {
                Debug.Assert(!IsAnyReadOnly(addressKind));

                var receiver = fieldAccess.ReceiverOpt;
                if (receiver?.Type.IsValueType == true)
                {
                    // Check receiver:
                    // has writeable home -> return true - the whole chain has writeable home (also a more common case)
                    // has readable home -> return false - we need to copy the field
                    // otherwise         -> return true  - the copy will be made at higher level so the leaf field can have writeable home

                    return HasHome(receiver, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt)
                        || !HasHome(receiver, AddressKind.ReadOnly, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);
                }
            }

            return true;
        }

        // while readonly fields have home it is not valid to refer to it when not constructing.
        if (!TypeSymbol.Equals(field.ContainingType, containingSymbol.ContainingSymbol as NamedTypeSymbol, TypeCompareKind.AllIgnoreOptions))
        {
            return false;
        }

        if (field.IsStatic)
        {
            return containingSymbol is MethodSymbol { MethodKind: MethodKind.StaticConstructor } or FieldSymbol { IsStatic: true };
        }
        else
        {
            return (containingSymbol is MethodSymbol { MethodKind: MethodKind.Constructor } or FieldSymbol { IsStatic: false } or MethodSymbol { IsInitOnly: true }) &&
                fieldAccess.ReceiverOpt.Kind == BoundKind.ThisReference;
        }
    }
}
