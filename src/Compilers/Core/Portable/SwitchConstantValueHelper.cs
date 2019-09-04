// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Contains helper methods for switch statement label constants
    /// </summary>
    internal static class SwitchConstantValueHelper
    {
        public static bool IsValidSwitchCaseLabelConstant(ConstantValue constant)
        {
            switch (constant.Discriminator)
            {
                case ConstantValueTypeDiscriminator.Null:
                case ConstantValueTypeDiscriminator.SByte:
                case ConstantValueTypeDiscriminator.Byte:
                case ConstantValueTypeDiscriminator.Int16:
                case ConstantValueTypeDiscriminator.UInt16:
                case ConstantValueTypeDiscriminator.Int32:
                case ConstantValueTypeDiscriminator.UInt32:
                case ConstantValueTypeDiscriminator.Int64:
                case ConstantValueTypeDiscriminator.UInt64:
                case ConstantValueTypeDiscriminator.Char:
                case ConstantValueTypeDiscriminator.Boolean:
                case ConstantValueTypeDiscriminator.String:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Method used to compare ConstantValues for switch statement case labels
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:
        /// Less than zero:     first instance precedes second in the sort order.
        /// Zero:               first instance occurs in the same position in the sort order as second.
        /// Greater than zero:  first instance follows second in the sort order.
        /// </returns>
        public static int CompareSwitchCaseLabelConstants(ConstantValue first, ConstantValue second)
        {
            Debug.Assert(first != null);
            Debug.Assert(second != null);

            Debug.Assert(IsValidSwitchCaseLabelConstant(first));
            Debug.Assert(IsValidSwitchCaseLabelConstant(second));

            if (first.IsNull)
            {
                // first instance has ConstantValue Null.
                // If second instance ConstantValue is Null, both instances are equal.
                // Else, second instance is greater.
                return second.IsNull ? 0 : -1;
            }
            else if (second.IsNull)
            {
                // second instance has ConstantValue Null and first instance has non-null ConstantValue.
                // first instance is greater.
                return 1;
            }

            switch (first.Discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte:
                case ConstantValueTypeDiscriminator.Int16:
                case ConstantValueTypeDiscriminator.Int32:
                case ConstantValueTypeDiscriminator.Int64:
                    return first.Int64Value.CompareTo(second.Int64Value);

                case ConstantValueTypeDiscriminator.Boolean:
                case ConstantValueTypeDiscriminator.Byte:
                case ConstantValueTypeDiscriminator.UInt16:
                case ConstantValueTypeDiscriminator.UInt32:
                case ConstantValueTypeDiscriminator.UInt64:
                case ConstantValueTypeDiscriminator.Char:
                    return first.UInt64Value.CompareTo(second.UInt64Value);

                case ConstantValueTypeDiscriminator.String:
                    Debug.Assert(second.IsString);
                    return string.CompareOrdinal(first.StringValue, second.StringValue);

                default:
                    throw ExceptionUtilities.UnexpectedValue(first.Discriminator);
            }
        }

        public class SwitchLabelsComparer : EqualityComparer<object>
        {
            public override bool Equals(object first, object second)
            {
                Debug.Assert(first != null && second != null);

                var firstConstant = first as ConstantValue;
                if (firstConstant != null)
                {
                    var secondConstant = second as ConstantValue;
                    if (secondConstant != null)
                    {
                        if (!IsValidSwitchCaseLabelConstant(firstConstant)
                            || !IsValidSwitchCaseLabelConstant(secondConstant))
                        {
                            // We don't care about invalid case labels with duplicate value as
                            // we will generate diagnostics for invalid case label.
                            return firstConstant.Equals(secondConstant);
                        }

                        return CompareSwitchCaseLabelConstants(firstConstant, secondConstant) == 0;
                    }
                }

                var firstString = first as string;
                if (firstString != null)
                {
                    return string.Equals(firstString, second as string, System.StringComparison.Ordinal);
                }

                return first.Equals(second);
            }

            public override int GetHashCode(object obj)
            {
                var constant = obj as ConstantValue;
                if (constant != null)
                {
                    switch (constant.Discriminator)
                    {
                        case ConstantValueTypeDiscriminator.SByte:
                        case ConstantValueTypeDiscriminator.Int16:
                        case ConstantValueTypeDiscriminator.Int32:
                        case ConstantValueTypeDiscriminator.Int64:
                            return constant.Int64Value.GetHashCode();

                        case ConstantValueTypeDiscriminator.Boolean:
                        case ConstantValueTypeDiscriminator.Byte:
                        case ConstantValueTypeDiscriminator.UInt16:
                        case ConstantValueTypeDiscriminator.UInt32:
                        case ConstantValueTypeDiscriminator.UInt64:
                        case ConstantValueTypeDiscriminator.Char:
                            return constant.UInt64Value.GetHashCode();

                        case ConstantValueTypeDiscriminator.String:
                            return constant.RopeValue.GetHashCode();
                    }
                }

                return obj.GetHashCode();
            }
        }
    }
}
