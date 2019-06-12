// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Common callbacks for <see cref="PropertySetAnalysis"/>.
    /// </summary>
    internal static class PropertySetCallbacks
    {
        /// <summary>
        /// A <see cref="PropertyMapper.PointsToAbstractValueCallback"/> for flagging assigning null to a property.
        /// </summary>
        /// <param name="pointsToAbstractValue">Value assigned to the property.</param>
        /// <returns>Flagged if null, Unflagged if not null, MaybeFlagged otherwise.</returns>
        public static PropertySetAbstractValueKind FlagIfNull(PointsToAbstractValue pointsToAbstractValue)
        {
            switch (pointsToAbstractValue.NullState)
            {
                case NullAbstractValue.Null:
                    return PropertySetAbstractValueKind.Flagged;

                case NullAbstractValue.NotNull:
                    return PropertySetAbstractValueKind.Unflagged;

                default:
                    return PropertySetAbstractValueKind.MaybeFlagged;
            }
        }

        /// <summary>
        /// Enumerates literal values to map to a property set abstract value.
        /// </summary>
        /// <param name="valueContentAbstractValue">Abstract value containing the literal values to examine.</param>
        /// <param name="badLiteralValuePredicate">Predicate function to determine if a literal value is bad.</param>
        /// <returns>Mapped kind.</returns>
        /// <remarks>
        /// Null is not handled by this.  Look at the <see cref="PointsToAbstractValue"/> if you need to treat null as bad.
        /// 
        /// All literal values are bad => Flagged
        /// Some but not all literal are bad => MaybeFlagged
        /// All literal values are known and none are bad => Unflagged
        /// Otherwise => Unknown
        /// </remarks>
        public static PropertySetAbstractValueKind EvaluateLiteralValues(
            ValueContentAbstractValue valueContentAbstractValue,
            Func<object, bool> badLiteralValuePredicate)
        {
            Debug.Assert(valueContentAbstractValue != null);
            Debug.Assert(badLiteralValuePredicate != null);

            switch (valueContentAbstractValue.NonLiteralState)
            {
                case ValueContainsNonLiteralState.No:
                    if (valueContentAbstractValue.LiteralValues.IsEmpty)
                    {
                        return PropertySetAbstractValueKind.Unflagged;
                    }

                    bool allValuesBad = true;
                    bool someValuesBad = false;
                    foreach (object literalValue in valueContentAbstractValue.LiteralValues)
                    {
                        if (badLiteralValuePredicate(literalValue))
                        {
                            someValuesBad = true;
                        }
                        else
                        {
                            allValuesBad = false;
                        }

                        if (!allValuesBad && someValuesBad)
                        {
                            break;
                        }
                    }

                    if (allValuesBad)
                    {
                        // We know all values are bad, so we can say Flagged.
                        return PropertySetAbstractValueKind.Flagged;
                    }
                    else if (someValuesBad)
                    {
                        // We know all values but some values are bad, so we can say MaybeFlagged.
                        return PropertySetAbstractValueKind.MaybeFlagged;
                    }
                    else
                    {
                        // We know all values are good, so we can say Unflagged.
                        return PropertySetAbstractValueKind.Unflagged;
                    }

                case ValueContainsNonLiteralState.Maybe:
                    if (valueContentAbstractValue.LiteralValues.Any(badLiteralValuePredicate))
                    {
                        // We don't know all values but know some values are bad, so we can say MaybeFlagged.
                        return PropertySetAbstractValueKind.MaybeFlagged;
                    }
                    else
                    {
                        // We don't know all values but didn't find any bad value, so we can say who knows.
                        return PropertySetAbstractValueKind.Unknown;
                    }

                default:
                    return PropertySetAbstractValueKind.Unknown;
            }
        }

        /// <summary>
        /// A <see cref="HazardousUsageEvaluator.EvaluationCallback"/> for all properties flagged being hazardous.
        /// </summary>
        /// <param name="propertySetAbstractValue">PropertySetAbstract value.</param>
        /// <returns>If all properties are flagged, then flagged; if at least one property is unflagged, then unflagged; otherwise maybe flagged.</returns>
        public static HazardousUsageEvaluationResult HazardousIfAllFlagged(PropertySetAbstractValue propertySetAbstractValue)
        {
            if (propertySetAbstractValue.KnownValuesCount == 0)
            {
                // No known values implies only PropertySetAbstractValueKind.Unknown.
                return HazardousUsageEvaluationResult.MaybeFlagged;
            }

            bool allFlagged = true;
            bool atLeastOneUnflagged = false;
            for (int i = 0; i < propertySetAbstractValue.KnownValuesCount; i++)
            {
                if (propertySetAbstractValue[i] != PropertySetAbstractValueKind.Flagged)
                {
                    allFlagged = false;
                }

                if (propertySetAbstractValue[i] == PropertySetAbstractValueKind.Unflagged)
                {
                    atLeastOneUnflagged = true;
                    break;
                }
            }

            if (allFlagged)
            {
                return HazardousUsageEvaluationResult.Flagged;
            }
            else if (atLeastOneUnflagged)
            {
                return HazardousUsageEvaluationResult.Unflagged;
            }
            else
            {
                return HazardousUsageEvaluationResult.MaybeFlagged;
            }
        }

        /// <summary>
        /// A <see cref="HazardousUsageEvaluator.InvocationEvaluationCallback"/> for all properties flagged being hazardous.
        /// </summary>
        /// <param name="methodSymbol">Ignored.  If your scenario cares about the method, don't use this.</param>
        /// <param name="propertySetAbstractValue">PropertySetAbstract value.</param>
        /// <returns>If all properties are flagged, then flagged; if all properties are unflagged, then unflagged; otherwise maybe flagged.</returns>
        [SuppressMessage("Usage", "CA1801", Justification = "Intentionally ignored; have to match delegate signature")]
        [SuppressMessage("Usage", "IDE0060", Justification = "Intentionally ignored; have to match delegate signature")]
        public static HazardousUsageEvaluationResult HazardousIfAllFlagged(
            IMethodSymbol methodSymbol,
            PropertySetAbstractValue propertySetAbstractValue)
        {
            return HazardousIfAllFlagged(propertySetAbstractValue);
        }
    }
}
