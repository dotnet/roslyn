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

                    const InConversionGroupFlags all =
                        InConversionGroupFlags.UserDefinedOperator |
                        InConversionGroupFlags.UserDefinedFromConversion |
                        InConversionGroupFlags.UserDefinedFromConversionAdjustment |
                        InConversionGroupFlags.UserDefinedReturnTypeAdjustment |
                        InConversionGroupFlags.UserDefinedFinal |
                        InConversionGroupFlags.UserDefinedErroneous;

                    if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversion) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & all) == InConversionGroupFlags.UserDefinedFromConversion);
                        Debug.Assert(Operand is not BoundConversion operandAsConversion ||
                                     operandAsConversion.ConversionGroupOpt != ConversionGroupOpt);
                        Debug.Assert(Conversion == ConversionGroupOpt.Conversion.UserDefinedFromConversion);
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFromConversionAdjustment) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & all) == InConversionGroupFlags.UserDefinedFromConversionAdjustment);
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
                        Debug.Assert((InConversionGroupFlags & all) == InConversionGroupFlags.UserDefinedReturnTypeAdjustment);
                        Debug.Assert(Conversion.IsNullable);
                        Debug.Assert(Conversion.IsImplicit);
                        Debug.Assert(!Conversion.UnderlyingConversions[0].IsIdentity);
                        Debug.Assert(Operand is BoundConversion operandAsConversion &&
                                     operandAsConversion.ConversionGroupOpt == ConversionGroupOpt &&
                                     operandAsConversion.Conversion.IsUserDefined);
                    }
                    else if ((InConversionGroupFlags & InConversionGroupFlags.UserDefinedFinal) != 0)
                    {
                        Debug.Assert((InConversionGroupFlags & all) == InConversionGroupFlags.UserDefinedFinal);

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
        }
    }
}
