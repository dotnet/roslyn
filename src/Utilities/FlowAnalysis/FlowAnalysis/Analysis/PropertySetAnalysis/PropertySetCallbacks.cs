using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

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
        /// <returns>Indication if the </returns>
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
        /// A <see cref="HazardousUsageEvaluator.ReturnEvaluationCallback"/> for all properties flagged being hazardous.
        /// </summary>
        /// <param name="propertySetAbstractValue">PropertySetAbstract value.</param>
        /// <returns>If all properties are flagged, then flagged; if all properties are unflagged, then unflagged; otherwise maybe flagged.</returns>
        public static HazardousUsageEvaluationResult HazardousIfAllFlagged(PropertySetAbstractValue propertySetAbstractValue)
        {
            if (propertySetAbstractValue.KnownValuesCount == 0)
            {
                // No known values implies only PropertySetAbstractValueKind.Unknown.
                return HazardousUsageEvaluationResult.MaybeFlagged;
            }

            bool allFlagged = true;
            bool allUnflagged = true;
            for (int i = 0; i < propertySetAbstractValue.KnownValuesCount; i++)
            {
                if (propertySetAbstractValue[i] != PropertySetAbstractValueKind.Flagged)
                {
                    allFlagged = false;
                }

                if (propertySetAbstractValue[i] != PropertySetAbstractValueKind.Unflagged)
                {
                    allUnflagged = false;
                }
            }

            if (allFlagged)
            {
                return HazardousUsageEvaluationResult.Flagged;
            }
            else if (allUnflagged)
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
        public static HazardousUsageEvaluationResult HazardousIfAllFlagged(
            IMethodSymbol methodSymbol,
            PropertySetAbstractValue propertySetAbstractValue)
        {
            return HazardousIfAllFlagged(propertySetAbstractValue);
        }
    }
}
