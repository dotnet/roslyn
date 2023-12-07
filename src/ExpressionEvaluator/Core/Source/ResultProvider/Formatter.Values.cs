// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;
using TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    [Flags]
    internal enum GetValueFlags
    {
        None = 0x0,
        IncludeTypeName = 0x1,
        IncludeObjectId = 0x2,
    }

    // This class provides implementation for the "displaying values as strings" aspect of the Formatter component.
    internal abstract partial class Formatter
    {
        private string GetValueString(DkmClrValue value, DkmInspectionContext inspectionContext, ObjectDisplayOptions options, GetValueFlags flags)
        {
            if (value.IsError())
            {
                return (string)value.HostObjectValue;
            }

            if (UsesHexadecimalNumbers(inspectionContext))
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }

            var lmrType = value.Type.GetLmrType();
            if (IsPredefinedType(lmrType) && !lmrType.IsObject())
            {
                if (lmrType.IsString())
                {
                    var stringValue = (string)value.HostObjectValue;
                    if (stringValue == null)
                    {
                        return _nullString;
                    }
                    return IncludeObjectId(
                        value,
                        FormatString(stringValue, options),
                        flags);
                }
                else if (lmrType.IsCharacter())
                {
                    // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
                    if (value.HostObjectValue == null)
                    {
                        return _hostValueNotFoundString;
                    }

                    return IncludeObjectId(
                        value,
                        FormatLiteral((char)value.HostObjectValue, options | ObjectDisplayOptions.IncludeCodePoints),
                        flags);
                }
                else
                {
                    return IncludeObjectId(
                        value,
                        FormatPrimitive(value, options & ~(ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters), inspectionContext),
                        flags);
                }
            }
            else if (value.IsNull && !lmrType.IsPointer)
            {
                return _nullString;
            }
            else if (lmrType.IsEnum)
            {
                return IncludeObjectId(
                    value,
                    GetEnumDisplayString(lmrType, value, options, (flags & GetValueFlags.IncludeTypeName) != 0, inspectionContext),
                    flags);
            }
            else if (lmrType.IsArray)
            {
                return IncludeObjectId(
                    value,
                    GetArrayDisplayString(value.Type.AppDomain, lmrType, value.ArrayDimensions, value.ArrayLowerBounds, options),
                    flags);
            }
            else if (lmrType.IsPointer)
            {
                // NOTE: the HostObjectValue will have a size corresponding to the process bitness
                // and FormatPrimitive will adjust accordingly.
                var tmp = FormatPrimitive(value, ObjectDisplayOptions.UseHexadecimalNumbers, inspectionContext); // Always in hex.
                Debug.Assert(tmp != null);
                return tmp;
            }
            else if (lmrType.IsNullable())
            {
                var nullableValue = value.GetNullableValue(inspectionContext);
                // It should be impossible to nest nullables, so this recursion should introduce only a single extra stack frame.
                return nullableValue == null
                    ? _nullString
                    : GetValueString(nullableValue, inspectionContext, ObjectDisplayOptions.None, GetValueFlags.IncludeTypeName);
            }
            else if (lmrType.IsIntPtr())
            {
                // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
                if (value.HostObjectValue == null)
                {
                    return _hostValueNotFoundString;
                }

                if (IntPtr.Size == 8)
                {
                    var intPtr = ((IntPtr)value.HostObjectValue).ToInt64();
                    return FormatPrimitiveObject(intPtr, ObjectDisplayOptions.UseHexadecimalNumbers);
                }
                else
                {
                    var intPtr = ((IntPtr)value.HostObjectValue).ToInt32();
                    return FormatPrimitiveObject(intPtr, ObjectDisplayOptions.UseHexadecimalNumbers);
                }
            }
            else if (lmrType.IsUIntPtr())
            {
                // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
                if (value.HostObjectValue == null)
                {
                    return _hostValueNotFoundString;
                }

                if (UIntPtr.Size == 8)
                {
                    var uIntPtr = ((UIntPtr)value.HostObjectValue).ToUInt64();
                    return FormatPrimitiveObject(uIntPtr, ObjectDisplayOptions.UseHexadecimalNumbers);
                }
                else
                {
                    var uIntPtr = ((UIntPtr)value.HostObjectValue).ToUInt32();
                    return FormatPrimitiveObject(uIntPtr, ObjectDisplayOptions.UseHexadecimalNumbers);
                }
            }
            else
            {
                int cardinality;
                if (lmrType.IsTupleCompatible(out cardinality) && (cardinality > 1))
                {
                    var values = ArrayBuilder<string>.GetInstance();
                    if (value.TryGetTupleFieldValues(cardinality, values, inspectionContext))
                    {
                        return IncludeObjectId(
                            value,
                            GetTupleExpression(values.ToArrayAndFree()),
                            flags);
                    }
                    values.Free();
                }
            }

            // "value.EvaluateToString()" will check "Call string-conversion function on objects in variables windows"
            // (Tools > Options setting) and call "value.ToString()" if appropriate.
            return IncludeObjectId(
                value,
                string.Format(_defaultFormat, value.EvaluateToString(inspectionContext) ?? inspectionContext.GetTypeName(value.Type, CustomTypeInfo: null, FormatSpecifiers: NoFormatSpecifiers)),
                flags);
        }

        /// <summary>
        /// Gets the string representation of a character literal without including the numeric code point.
        /// </summary>
        private string GetValueStringForCharacter(DkmClrValue value, DkmInspectionContext inspectionContext, ObjectDisplayOptions options)
        {
            Debug.Assert(value.Type.GetLmrType().IsCharacter());
            if (UsesHexadecimalNumbers(inspectionContext))
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }
            // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
            if (value.HostObjectValue == null)
            {
                return _hostValueNotFoundString;
            }

            var charTemp = FormatLiteral((char)value.HostObjectValue, options);
            Debug.Assert(charTemp != null);
            return charTemp;
        }

        private bool HasUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            return value.EvalFlags.HasFlag(DkmEvaluationResultFlags.TruncatedString) || GetUnderlyingString(value, inspectionContext) != null;
        }

        private string GetUnderlyingString(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            var dataItem = value.GetDataItem<RawStringDataItem>();
            if (dataItem != null)
            {
                return dataItem.RawString;
            }

            string underlyingString = GetUnderlyingStringImpl(value, inspectionContext);
            dataItem = new RawStringDataItem(underlyingString);
            value.SetDataItem(DkmDataCreationDisposition.CreateNew, dataItem);
            return underlyingString;
        }

        private string GetUnderlyingStringImpl(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            Debug.Assert(!value.IsError());

            if (value.IsNull)
            {
                return null;
            }

            var lmrType = value.Type.GetLmrType();
            if (lmrType.IsEnum || lmrType.IsArray || lmrType.IsPointer)
            {
                return null;
            }

            if (lmrType.IsNullable())
            {
                var nullableValue = value.GetNullableValue(inspectionContext);
                return nullableValue != null ? GetUnderlyingStringImpl(nullableValue, inspectionContext) : null;
            }

            if (lmrType.IsString())
            {
                if (value.EvalFlags.HasFlag(DkmEvaluationResultFlags.TruncatedString))
                {
                    var extendedInspectionContext = inspectionContext.With(DkmEvaluationFlags.IncreaseMaxStringSize);
                    return value.EvaluateToString(extendedInspectionContext);
                }

                return (string)value.HostObjectValue;
            }
            else if (!IsPredefinedType(lmrType))
            {
                // Check for special cased non-primitives that have underlying strings
                if (lmrType.IsType("System.Data.SqlTypes", "SqlString"))
                {
                    var fieldValue = value.GetFieldValue(InternalWellKnownMemberNames.SqlStringValue, inspectionContext);
                    return fieldValue.HostObjectValue as string;
                }
                else if (lmrType.IsOrInheritsFrom("System.Xml.Linq", "XNode"))
                {
                    return value.EvaluateToString(inspectionContext);
                }
            }

            return null;
        }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
        /// <remarks>
        /// The corresponding native code is in EEUserStringBuilder::ErrTryAppendConstantEnum.
        /// The corresponding roslyn code is in 
        /// <see cref="M:Microsoft.CodeAnalysis.SymbolDisplay.AbstractSymbolDisplayVisitor`1.AddEnumConstantValue(Microsoft.CodeAnalysis.INamedTypeSymbol, System.Object, System.Boolean)"/>.
        /// NOTE: no curlies for enum values.
        /// </remarks>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        private string GetEnumDisplayString(Type lmrType, DkmClrValue value, ObjectDisplayOptions options, bool includeTypeName, DkmInspectionContext inspectionContext)
        {
            Debug.Assert(lmrType.IsEnum);
            Debug.Assert(value != null);

            object underlyingValue = value.HostObjectValue;
            // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
            if (underlyingValue == null)
            {
                return _hostValueNotFoundString;
            }

            string displayString;

            var fields = ArrayBuilder<EnumField>.GetInstance();
            FillEnumFields(fields, lmrType);
            // We will normalize/extend all enum values to ulong to ensure that we are always comparing the full underlying value.
            ulong valueForComparison = ConvertEnumUnderlyingTypeToUInt64(underlyingValue, Type.GetTypeCode(lmrType));
            var typeToDisplayOpt = includeTypeName ? lmrType : null;
            if (valueForComparison != 0 && IsFlagsEnum(lmrType))
            {
                displayString = GetNamesForFlagsEnumValue(fields, underlyingValue, valueForComparison, options, typeToDisplayOpt);
            }
            else
            {
                displayString = GetNameForEnumValue(fields, underlyingValue, valueForComparison, options, typeToDisplayOpt);
            }
            fields.Free();

            return displayString ?? FormatPrimitive(value, options, inspectionContext);
        }

        private static void FillEnumFields(ArrayBuilder<EnumField> fields, Type lmrType)
        {
            var fieldInfos = lmrType.GetFields();
            var enumTypeCode = Type.GetTypeCode(lmrType);

            foreach (var info in fieldInfos)
            {
                if (!info.IsSpecialName) // Skip __value.
                {
                    fields.Add(new EnumField(info.Name, ConvertEnumUnderlyingTypeToUInt64(info.GetRawConstantValue(), enumTypeCode)));
                }
            }

            fields.Sort(EnumField.Comparer);
        }

        protected static void FillUsedEnumFields(ArrayBuilder<EnumField> usedFields, ArrayBuilder<EnumField> fields, ulong underlyingValue)
        {
            var remaining = underlyingValue;
            foreach (var field in fields)
            {
                var fieldValue = field.Value;
                if (fieldValue == 0)
                    continue; // Otherwise, we'd tack the zero flag onto everything.

                if ((remaining & fieldValue) == fieldValue)
                {
                    remaining -= fieldValue;

                    usedFields.Add(field);

                    if (remaining == 0)
                        break;
                }
            }

            // The value contained extra bit flags that didn't correspond to any enum field.  We will
            // report "no fields used" here so the Formatter will just display the underlying value.
            if (remaining != 0)
            {
                usedFields.Clear();
            }
        }

        private static bool IsFlagsEnum(Type lmrType)
        {
            Debug.Assert(lmrType.IsEnum);

            var attributes = lmrType.GetCustomAttributesData();
            foreach (var attribute in attributes)
            {
                // NOTE: AttributeType is not available in 2.0
                if (attribute.Constructor.DeclaringType.FullName == "System.FlagsAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool UsesHexadecimalNumbers(DkmInspectionContext inspectionContext)
        {
            Debug.Assert(inspectionContext != null);

            return inspectionContext.Radix == 16;
        }

        /// <summary>
        /// Convert a boxed primitive (generally of the backing type of an enum) into a ulong.
        /// </summary>
        protected static ulong ConvertEnumUnderlyingTypeToUInt64(object value, TypeCode typeCode)
        {
            Debug.Assert(value != null);

            unchecked
            {
                switch (typeCode)
                {
                    case TypeCode.SByte:
                        return (ulong)(sbyte)value;
                    case TypeCode.Int16:
                        return (ulong)(short)value;
                    case TypeCode.Int32:
                        return (ulong)(int)value;
                    case TypeCode.Int64:
                        return (ulong)(long)value;
                    case TypeCode.Byte:
                        return (byte)value;
                    case TypeCode.UInt16:
                        return (ushort)value;
                    case TypeCode.UInt32:
                        return (uint)value;
                    case TypeCode.UInt64:
                        return (ulong)value;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeCode);
                }
            }
        }

        private string GetEditableValue(DkmClrValue value, DkmInspectionContext inspectionContext)
        {
            if (value.IsError())
            {
                return null;
            }

            if (value.EvalFlags.Includes(DkmEvaluationResultFlags.ReadOnly))
            {
                return null;
            }

            var type = value.Type.GetLmrType();

            if (type.IsEnum)
            {
                return this.GetValueString(value, inspectionContext, ObjectDisplayOptions.None, GetValueFlags.IncludeTypeName);
            }
            else if (type.IsDecimal())
            {
                return this.GetValueString(value, inspectionContext, ObjectDisplayOptions.IncludeTypeSuffix, GetValueFlags.None);
            }
            // The legacy EE didn't special-case strings or chars (when ",nq" was used,
            // you had to manually add quotes when editing) but it makes sense to
            // always automatically quote (non-null) strings and chars when editing.
            else if (type.IsString())
            {
                if (!value.IsNull)
                {
                    return this.GetValueString(value, inspectionContext, ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters, GetValueFlags.None);
                }
            }
            else if (type.IsCharacter())
            {
                return this.GetValueStringForCharacter(value, inspectionContext, ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters);
            }

            return null;
        }

        private string FormatPrimitive(DkmClrValue value, ObjectDisplayOptions options, DkmInspectionContext inspectionContext)
        {
            Debug.Assert(value != null);
            // check if HostObjectValue is null, since any of these types might actually be a synthetic value as well.
            if (value.HostObjectValue == null)
            {
                return _hostValueNotFoundString;
            }

            // DateTime is primitive in VB but not in C#.
            object obj;
            if (value.Type.GetLmrType().IsDateTime())
            {
                var dateDataValue = value.GetPropertyValue("Ticks", inspectionContext);
                obj = new DateTime((long)dateDataValue.HostObjectValue);
            }
            else
            {
                obj = value.HostObjectValue;
            }

            return FormatPrimitiveObject(obj, options);
        }

        private static string IncludeObjectId(DkmClrValue value, string valueStr, GetValueFlags flags)
        {
            Debug.Assert(valueStr != null);
            return (flags & GetValueFlags.IncludeObjectId) == 0
                ? valueStr
                : value.IncludeObjectId(valueStr);
        }

        #region Language-specific value formatting behavior

        internal abstract string GetArrayDisplayString(DkmClrAppDomain appDomain, Type lmrType, ReadOnlyCollection<int> sizes, ReadOnlyCollection<int> lowerBounds, ObjectDisplayOptions options);

        internal abstract string GetArrayIndexExpression(string[] indices);

        internal abstract string GetCastExpression(string argument, string type, DkmClrCastExpressionOptions options);

        internal abstract string GetNamesForFlagsEnumValue(ArrayBuilder<EnumField> fields, object value, ulong underlyingValue, ObjectDisplayOptions options, Type typeToDisplayOpt);

        internal abstract string GetNameForEnumValue(ArrayBuilder<EnumField> fields, object value, ulong underlyingValue, ObjectDisplayOptions options, Type typeToDisplayOpt);

        internal abstract string GetObjectCreationExpression(string type, string[] arguments);

        internal abstract string GetTupleExpression(string[] values);

        internal abstract string FormatLiteral(char c, ObjectDisplayOptions options);

        internal abstract string FormatLiteral(int value, ObjectDisplayOptions options);

        internal abstract string FormatPrimitiveObject(object value, ObjectDisplayOptions options);

        internal abstract string FormatString(string str, ObjectDisplayOptions options);

        #endregion
    }
}
