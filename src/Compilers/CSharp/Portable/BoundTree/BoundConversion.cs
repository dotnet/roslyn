// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundConversion
    {
        private partial void Validate()
        {
            Debug.Assert(!Binder.IsTypeOrValueExpression(Operand));
            Debug.Assert(!Binder.IsMethodGroupWithTypeOrValueReceiver(Operand));

            if (Conversion.IsTupleLiteralConversion ||
                (Conversion.IsNullable && Conversion.UnderlyingConversions[0].IsTupleLiteralConversion))
            {
                Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.TupleLiteral) != 0);
            }

            if ((InConversionGroupFlags & InConversionGroupFlags.TupleLiteral) != 0)
            {
                Debug.Assert(Conversion.IsTupleLiteralConversion ||
                             (Conversion.IsNullable && Conversion.UnderlyingConversions[0].IsTupleLiteralConversion));
                Debug.Assert(Operand is BoundConvertedTupleLiteral);
            }

            if ((InConversionGroupFlags & InConversionGroupFlags.TupleLiteralExplicitIdentity) != 0)
            {
                Debug.Assert(Conversion.IsIdentity);
                Debug.Assert(Operand is BoundConvertedTupleLiteral ||
                             (Operand is BoundConversion operandAsConversion &&
                              operandAsConversion.ConversionGroupOpt == ConversionGroupOpt &&
                              (operandAsConversion.InConversionGroupFlags & InConversionGroupFlags.TupleLiteral) != 0));
            }

            // Assert the shape of the conversion tree for user-defined conversions.
            if (Conversion.IsUserDefined)
            {
                if (InConversionGroupFlags is InConversionGroupFlags.LoweredFormOfUserDefinedConversionForExpressionTree or InConversionGroupFlags.TupleBinaryOperatorPendingLowering)
                {
                    Debug.Assert(ConversionGroupOpt is null);
                }
                else
                {
                    Debug.Assert(ConversionGroupOpt?.Conversion.IsUserDefined == true);
                }
            }

            if (ConversionGroupOpt?.Conversion.IsUserDefined == true)
            {
                if (Conversion.IsUserDefined)
                {
                    Debug.Assert(Conversion == ConversionGroupOpt.Conversion);

                    if (!ConversionGroupOpt.Conversion.IsValid)
                    {
                        Debug.Assert(InConversionGroupFlags == (InConversionGroupFlags.UserDefinedOperator | InConversionGroupFlags.UserDefinedErroneous));
                        Debug.Assert(Operand is not BoundConversion operandAsConversion || operandAsConversion.ConversionGroupOpt != ConversionGroupOpt);
                    }
                    else
                    {
                        Debug.Assert(InConversionGroupFlags == InConversionGroupFlags.UserDefinedOperator);

                        if (Operand is BoundConversion operandAsConversion && operandAsConversion.ConversionGroupOpt == ConversionGroupOpt)
                        {
                            Debug.Assert((operandAsConversion.InConversionGroupFlags & (InConversionGroupFlags.UserDefinedFromConversion | InConversionGroupFlags.UserDefinedFromConversionAdjustment)) != 0);
                        }
                        else
                        {
                            Debug.Assert(Conversion.UserDefinedFromConversion.IsIdentity ||
                                         (Conversion.UserDefinedFromConversion.IsTupleLiteralConversion &&
                                          Operand is BoundConvertedTupleLiteral));
                        }
                    }
                }
                else
                {
                    Debug.Assert(!ExplicitCastInCode);
                    Debug.Assert(ConversionGroupOpt.Conversion.IsValid);

                    if (ConversionGroupOpt.Conversion.IsImplicit)
                    {
                        Debug.Assert(ConversionsBase.IsEncompassingImplicitConversionKind(Conversion.Kind) ||
                                     (Conversion.IsExplicit && Conversion.IsNullable &&
                                      (InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversionAdjustment) != 0));
                    }

                    if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversion) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.UserDefinedAllFlags) == InConversionGroupFlags.UserDefinedFromConversion);
                        Debug.Assert(Operand is not BoundConversion operandAsConversion ||
                                     operandAsConversion.ConversionGroupOpt != ConversionGroupOpt);
                        Debug.Assert(Conversion == ConversionGroupOpt.Conversion.UserDefinedFromConversion);
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversionAdjustment) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.UserDefinedAllFlags) == InConversionGroupFlags.UserDefinedFromConversionAdjustment);
                        Debug.Assert(Conversion.IsNullable);
                        Debug.Assert(Conversion.IsExplicit);
                        Debug.Assert(Conversion.UnderlyingConversions[0].IsIdentity);

                        if (Operand is BoundConversion operandAsConversion && operandAsConversion.ConversionGroupOpt == ConversionGroupOpt)
                        {
                            Debug.Assert((operandAsConversion.InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversion) != 0);
                        }
                        else
                        {
                            Debug.Assert(ConversionGroupOpt.Conversion.UserDefinedFromConversion.IsIdentity ||
                                         (ConversionGroupOpt.Conversion.UserDefinedFromConversion.IsTupleLiteralConversion &&
                                          Operand is BoundConvertedTupleLiteral));
                        }
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedReturnTypeAdjustment) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.UserDefinedAllFlags) == InConversionGroupFlags.UserDefinedReturnTypeAdjustment);
                        Debug.Assert(Conversion.IsNullable);
                        Debug.Assert(Conversion.IsImplicit);
                        Debug.Assert(!Conversion.UnderlyingConversions[0].IsIdentity);
                        Debug.Assert(Operand is BoundConversion operandAsConversion &&
                                     operandAsConversion.ConversionGroupOpt == ConversionGroupOpt &&
                                     operandAsConversion.Conversion.IsUserDefined);
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFinal) != 0)
                    {
                        Debug.Assert(InConversionGroupFlags == InConversionGroupFlags.UserDefinedFinal);

                        Debug.Assert(Operand is BoundConversion operandAsConversion &&
                                     operandAsConversion.ConversionGroupOpt == ConversionGroupOpt &&
                                     (operandAsConversion.Conversion.IsUserDefined ||
                                      (operandAsConversion.InConversionGroupFlags & InConversionGroupFlags.UserDefinedReturnTypeAdjustment) != 0));

                    }
                    else
                    {
                        ExceptionUtilities.UnexpectedValue(InConversionGroupFlags);
                    }
                }
            }

            // Assert the shape of the conversion tree for union conversions.
            Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.UserDefinedAllFlags) == 0 || (InConversionGroupFlags & InConversionGroupFlags.UnionAllFlags) == 0);

            if (Conversion.IsUnion)
            {
                if (InConversionGroupFlags is InConversionGroupFlags.TupleBinaryOperatorPendingLowering)
                {
                    Debug.Assert(ConversionGroupOpt is null);
                }
                else
                {
                    Debug.Assert(ConversionGroupOpt?.Conversion.IsUnion == true);
                }
            }

            if (ConversionGroupOpt?.Conversion.IsUnion == true)
            {
                if (Conversion.IsUnion)
                {
                    Debug.Assert(Conversion == ConversionGroupOpt.Conversion);

                    Debug.Assert(InConversionGroupFlags == InConversionGroupFlags.UnionConstructor);

                    if (Operand is BoundConversion operandAsConversion && operandAsConversion.ConversionGroupOpt == ConversionGroupOpt)
                    {
                        Debug.Assert((operandAsConversion.InConversionGroupFlags & InConversionGroupFlags.UnionSourceConversion) != 0);
                    }
                    else
                    {
                        var sourceConversion = ConversionGroupOpt.Conversion.BestUnionConversionAnalysis.SourceConversion;
                        Debug.Assert(sourceConversion.IsIdentity ||
                                        (sourceConversion.IsTupleLiteralConversion &&
                                        Operand is BoundConvertedTupleLiteral));
                    }
                }
                else
                {
                    Debug.Assert(!ExplicitCastInCode);
                    Debug.Assert(ConversionsBase.IsEncompassingImplicitConversionKind(Conversion.Kind));

                    if ((InConversionGroupFlags & InConversionGroupFlags.UnionSourceConversion) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & InConversionGroupFlags.UnionAllFlags) == InConversionGroupFlags.UnionSourceConversion);
                        Debug.Assert(Operand is not BoundConversion operandAsConversion ||
                                     operandAsConversion.ConversionGroupOpt != ConversionGroupOpt);
                        Debug.Assert(Conversion == ConversionGroupOpt.Conversion.BestUnionConversionAnalysis.SourceConversion);
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UnionFinal) != 0)
                    {
                        Debug.Assert(InConversionGroupFlags == InConversionGroupFlags.UnionFinal);
                        Debug.Assert(Conversion.IsNullable);
                        Debug.Assert(Conversion.IsImplicit);
                        Debug.Assert(Conversion.UnderlyingConversions[0].IsIdentity);
                        Debug.Assert(Operand is BoundConversion operandAsConversion &&
                                     operandAsConversion.ConversionGroupOpt == ConversionGroupOpt &&
                                     operandAsConversion.Conversion.IsUnion);
                        Debug.Assert(Conversion == ConversionGroupOpt.Conversion.BestUnionConversionAnalysis.TargetConversion);
                    }
                    else
                    {
                        ExceptionUtilities.UnexpectedValue(InConversionGroupFlags);
                    }
                }
            }
        }
    }
}
