// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class TypedConstantExtensions
    {
        /// <summary>
        /// Returns the System.String that represents the current TypedConstant.
        /// </summary>
        /// <returns>A System.String that represents the current TypedConstant.</returns>
        public static string ToCSharpString(this TypedConstant constant)
        {
            if (constant.IsNull)
            {
                return "null";
            }

            if (constant.Kind == TypedConstantKind.Array)
            {
                return "{" + string.Join(", ", constant.Values.Select(v => v.ToCSharpString())) + "}";
            }

            if (constant.Kind == TypedConstantKind.Type || constant.TypeInternal.SpecialType == SpecialType.System_Object)
            {
                return "typeof(" + constant.Value.ToString() + ")";
            }

            if (constant.Kind == TypedConstantKind.Enum)
            {
                // TODO (tomat): replace with SymbolDisplay
                return DisplayEnumConstant(constant);
            }

            return SymbolDisplay.FormatPrimitive(constant.ValueInternal, quoteStrings: true, useHexadecimalNumbers: false);
        }

        // Decode the value of enum constant
        private static string DisplayEnumConstant(TypedConstant constant)
        {
            Debug.Assert(constant.Kind == TypedConstantKind.Enum);

            // Create a ConstantValue of enum underlying type
            SpecialType splType = ((INamedTypeSymbol)constant.Type).EnumUnderlyingType.SpecialType;
            ConstantValue valueConstant = ConstantValue.Create(constant.ValueInternal, splType);

            string typeName = constant.Type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
            if (valueConstant.IsUnsigned)
            {
                return DisplayUnsignedEnumConstant(constant, splType, valueConstant.UInt64Value, typeName);
            }
            else
            {
                return DisplaySignedEnumConstant(constant, splType, valueConstant.Int64Value, typeName);
            }
        }

        private static string DisplayUnsignedEnumConstant(TypedConstant constant, SpecialType specialType, ulong constantToDecode, string typeName)
        {
            // Specified valueConstant might have an exact matching enum field
            // or it might be a bitwise Or of multiple enum fields.
            // For the later case, we keep track of the current value of
            // bitwise Or of possible enum fields.
            ulong curValue = 0;

            // Initialize the value string to empty
            PooledStringBuilder pooledBuilder = null;
            StringBuilder valueStringBuilder = null;

            // Iterate through all the constant members in the enum type
            var members = constant.Type.GetMembers();
            foreach (var member in members)
            {
                var field = member as IFieldSymbol;

                if ((object)field != null && field.HasConstantValue)
                {
                    ConstantValue memberConstant = ConstantValue.Create(field.ConstantValue, specialType);
                    ulong memberValue = memberConstant.UInt64Value;

                    // Do we have an exact matching enum field
                    if (memberValue == constantToDecode)
                    {
                        if (pooledBuilder != null)
                        {
                            pooledBuilder.Free();
                        }

                        return typeName + "." + field.Name;
                    }

                    // specifiedValue might be a bitwise Or of multiple enum fields
                    // Is the current member included in the specified value?
                    if ((memberValue & constantToDecode) == memberValue)
                    {
                        // update the current value
                        curValue = curValue | memberValue;

                        if (valueStringBuilder == null)
                        {
                            pooledBuilder = PooledStringBuilder.GetInstance();
                            valueStringBuilder = pooledBuilder.Builder;
                        }
                        else
                        {
                            valueStringBuilder.Append(" | ");
                        }

                        valueStringBuilder.Append(typeName);
                        valueStringBuilder.Append(".");
                        valueStringBuilder.Append(field.Name);
                    }
                }
            }

            if (pooledBuilder != null)
            {
                if (curValue == constantToDecode)
                {
                    // return decoded enum constant
                    return pooledBuilder.ToStringAndFree();
                }

                // Unable to decode the enum constant
                pooledBuilder.Free();
            }

            // Unable to decode the enum constant, just display the integral value
            return constant.ValueInternal.ToString();
        }

        private static string DisplaySignedEnumConstant(TypedConstant constant, SpecialType specialType, long constantToDecode, string typeName)
        {
            // Specified valueConstant might have an exact matching enum field
            // or it might be a bitwise Or of multiple enum fields.
            // For the later case, we keep track of the current value of
            // bitwise Or of possible enum fields.
            long curValue = 0;

            // Initialize the value string to empty
            PooledStringBuilder pooledBuilder = null;
            StringBuilder valueStringBuilder = null;

            // Iterate through all the constant members in the enum type
            var members = constant.Type.GetMembers();
            foreach (var member in members)
            {
                var field = member as IFieldSymbol;
                if ((object)field != null && field.HasConstantValue)
                {
                    ConstantValue memberConstant = ConstantValue.Create(field.ConstantValue, specialType);
                    long memberValue = memberConstant.Int64Value;

                    // Do we have an exact matching enum field
                    if (memberValue == constantToDecode)
                    {
                        if (pooledBuilder != null)
                        {
                            pooledBuilder.Free();
                        }

                        return typeName + "." + field.Name;
                    }

                    // specifiedValue might be a bitwise Or of multiple enum fields
                    // Is the current member included in the specified value?
                    if ((memberValue & constantToDecode) == memberValue)
                    {
                        // update the current value
                        curValue = curValue | memberValue;

                        if (valueStringBuilder == null)
                        {
                            pooledBuilder = PooledStringBuilder.GetInstance();
                            valueStringBuilder = pooledBuilder.Builder;
                        }
                        else
                        {
                            valueStringBuilder.Append(" | ");
                        }

                        valueStringBuilder.Append(typeName);
                        valueStringBuilder.Append(".");
                        valueStringBuilder.Append(field.Name);
                    }
                }
            }

            if (pooledBuilder != null)
            {
                if (curValue == constantToDecode)
                {
                    // return decoded enum constant
                    return pooledBuilder.ToStringAndFree();
                }

                // Unable to decode the enum constant
                pooledBuilder.Free();
            }

            // Unable to decode the enum constant, just display the integral value
            return constant.ValueInternal.ToString();
        }
    }
}
