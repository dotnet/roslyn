// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundExpression LowerConstantPattern(BoundConstantPattern pattern, BoundExpression input)
        {
            return CompareWithConstant(input, pattern.Value);
        }

        // A constant is categorized as either
        // 1. A large negative integral value (less than int.MinValue), in which case we translate via a
        //    call to a helper method PrivateImplementationDetails.AsLargeNegativeHelper
        // 2. A large positive integral value (greater than int.MaxValue), using helper AsLargePositiveHelper
        // 3. An integral value in the range of the type `int` using helper AsIntValueHelper
        // 4. Something else, which we translate into an invocation of object.Equals(object, object)
        private BoundExpression CompareWithConstant(BoundExpression input, BoundExpression boundConstant)
        {
            // PROTOTYPE(patterns): We can generate excellent code when the input is a constant,
            // PROTOTYPE(patterns): but we don't handle that today.
            // PROTOTYPE(patterns): Similarly, we can generate better code when the input is statically
            // PROTOTYPE(patterns): an integral type, but we currently treat all inputs the same as `object`.
            var value = boundConstant.ConstantValue;
            if (!value.IsIntegral)
            {
                // We use object.Equals for char, string, float, double, decimal, DateTime, and null
                return _factory.StaticCall(
                    _factory.SpecialType(SpecialType.System_Object),
                    // PROTOTYPE(patterns): Add a special member for `public static bool object.Equals(object, object)`
                    "Equals",
                    _factory.Convert(_factory.SpecialType(SpecialType.System_Object), input),
                    _factory.Convert(_factory.SpecialType(SpecialType.System_Object), boundConstant)
                    );
            }

            // We use one of three helper functions from PrivateImplementationDetails, depending on the value
            // of the constant. The helper function takes the input and produces an integral value that we
            // are to compare against the input. Then we generate code like so:
            //     SelectedHelperMethod((object)input, out var temp) && temp == Constant
            MethodSymbol helper = SelectIntegralPatternHelper(boundConstant);
            if (helper == null)
            {
                Debug.Assert(this.EmitModule == null);
                return new BoundBadExpression(
                    input.Syntax, LookupResultKind.NotReferencable, ImmutableArray<Symbol>.Empty,
                    ImmutableArray.Create<BoundNode>(input, boundConstant), _factory.SpecialType(SpecialType.System_Boolean));
            }

            Debug.Assert(helper.Parameters[0].Type.SpecialType == SpecialType.System_Object);
            var comparisonType = helper.Parameters[1].Type;
            var temp = _factory.SynthesizedLocal(comparisonType);
            var objectType = _factory.SpecialType(SpecialType.System_Object);
            var call = _factory.Call(null, helper,
                ImmutableArray.Create(RefKind.None, RefKind.Out),
                ImmutableArray.Create(_factory.Convert(objectType, input), _factory.Local(temp)));
            BinaryOperatorKind comparison;
            switch (comparisonType.SpecialType)
            {
                case SpecialType.System_Int32: comparison = BinaryOperatorKind.IntEqual; break;
                case SpecialType.System_Int64: comparison = BinaryOperatorKind.LongEqual; break;
                case SpecialType.System_UInt64: comparison = BinaryOperatorKind.ULongEqual; break;
                default: throw ExceptionUtilities.UnexpectedValue(comparisonType.Name);
            }

            var equalsTest =
                _factory.Binary(comparison, _factory.SpecialType(SpecialType.System_Boolean),
                    _factory.Local(temp),
                    _factory.Convert(comparisonType, boundConstant));
            return _factory.Sequence(temp, _factory.LogicalAnd(call, equalsTest));
        }

        private MethodSymbol SelectIntegralPatternHelper(BoundExpression boundConstant)
        {
            Debug.Assert(boundConstant.ConstantValue != null);
            var constantValue = boundConstant.ConstantValue;
            var node = boundConstant.Syntax;

            // Depending on the *value* of the constant value, we select one of three helper functions.
            // If it is within the range of values of an int, we use AsIntValue. If it is
            // outside that range on the negative side, we use AsLargeNegative. If it is outside that range
            // on the positive side, we use AsLargePositive.
            Debug.Assert(constantValue.IsIntegral);
            switch (constantValue.Discriminator)
            {
                case ConstantValueTypeDiscriminator.Int64:
                    {
                        // this can be inside the range of ints, or outside the range at either end, either too high or too low.
                        long value = constantValue.Int64Value;
                        return (value < int.MinValue) ? AsLargeNegativeHelper(node) :
                               (value > int.MaxValue) ? AsLargePositiveHelper(node) :  AsIntValueHelper(node);
                    }
                case ConstantValueTypeDiscriminator.UInt32:
                case ConstantValueTypeDiscriminator.UInt64:
                    {
                        // this can be inside the range of ints or outside the range at the positive side.
                        ulong value = constantValue.UInt64Value;
                        return (value > int.MaxValue) ? AsLargePositiveHelper(node) : AsIntValueHelper(node);
                    }
                default:
                    {
                        // all of the remaining integral types have value sets totally contained in the values of int.
                        return AsIntValueHelper(node);
                    }
            }
        }

        private MethodSymbol AsLargeNegativeHelper(CSharpSyntaxNode node)
        {
            return MakePrivateImplementationHelper(
                node,
                PrivateImplementationDetails.AsLargeNegativeName,
                (module, pi, diagnostics, node2) => new SynthesizedAsLargeNegativeMethod(module, pi, diagnostics, node2));
        }

        private MethodSymbol AsLargePositiveHelper(CSharpSyntaxNode node)
        {
            return MakePrivateImplementationHelper(
                node,
                PrivateImplementationDetails.AsLargePositiveName,
                (module, pi, diagnostics, node2) => new SynthesizedAsLargePositiveMethod(module, pi, diagnostics, node2));
        }

        private MethodSymbol AsIntValueHelper(CSharpSyntaxNode node)
        {
            return MakePrivateImplementationHelper(
                node,
                PrivateImplementationDetails.AsIntValueName,
                (module, pi, diagnostics, node2) => new SynthesizedAsIntValueMethod(module, pi, diagnostics, node2));
        }

        private MethodSymbol MakePrivateImplementationHelper(
            CSharpSyntaxNode node,
            string helperName,
            Func<Emit.PEModuleBuilder, PrivateImplementationDetails, DiagnosticBag, CSharpSyntaxNode, SynthesizedGlobalMethodSymbol> makeHelper)
        {
            var module = this.EmitModule;
            if (module == null)
            {
                return null;
            }

            // If we have already generated the helper, possibly on another thread, we don't need to regenerate it.
            var privateImplClass = module.GetPrivateImplClass(node, _diagnostics);
            var helper = (MethodSymbol)privateImplClass.GetMethod(helperName);
            if (helper == (object)null)
            {
                helper = makeHelper(module, privateImplClass, _diagnostics, node);
                Debug.Assert(helperName == helper.Name);
                if (!privateImplClass.TryAddSynthesizedMethod(helper))
                {
                    helper = (MethodSymbol)privateImplClass.GetMethod(helperName);
                }
            }

            return helper;
        }
    }

}
