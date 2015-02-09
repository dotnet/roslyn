// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class InstructionDecoder
    {
        internal abstract string GetName(DkmClrInstructionAddress instructionAddress, bool includeParameterTypes, bool includeParameterNames, ArrayBuilder<string> argumentValues);
        internal abstract string GetReturnType(DkmClrInstructionAddress instructionAddress);
    }

    internal abstract class InstructionDecoder<TMethodSymbol> : InstructionDecoder where TMethodSymbol : class, IMethodSymbol
    {
        internal static readonly SymbolDisplayFormat DisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeExplicitInterface,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        internal override string GetName(DkmClrInstructionAddress instructionAddress, bool includeParameterTypes, bool includeParameterNames, ArrayBuilder<string> argumentValues)
        {
            var method = this.GetMethod(instructionAddress);
            return this.GetName(method, includeParameterTypes, includeParameterNames, argumentValues);
        }

        internal override string GetReturnType(DkmClrInstructionAddress instructionAddress)
        {
            var method = this.GetMethod(instructionAddress);
            return method.ReturnType.ToDisplayString(DisplayFormat);
        }

        internal abstract void AppendFullName(StringBuilder builder, TMethodSymbol method);

        internal abstract TMethodSymbol GetMethod(DkmClrInstructionAddress instructionAddress);

        internal string GetName(TMethodSymbol method, bool includeParameterTypes, bool includeParameterNames, ArrayBuilder<string> argumentValues = null)
        {
            Debug.Assert((argumentValues == null) || (method.Parameters.Length == argumentValues.Count));

            var pooled = PooledStringBuilder.GetInstance();
            var builder = pooled.Builder;

            // "full name" of method...
            AppendFullName(builder, method);

            // parameter list...
            var includeArgumentValues = argumentValues != null;
            if (includeParameterTypes || includeParameterNames || includeArgumentValues)
            {
                builder.Append('(');
                var parameters = method.Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    var parameter = parameters[i];

                    if (includeParameterTypes)
                    {
                        builder.Append(parameter.Type.ToDisplayString(DisplayFormat));
                    }

                    if (includeParameterNames)
                    {
                        if (includeParameterTypes)
                        {
                            builder.Append(' ');
                        }

                        builder.Append(parameter.Name);
                    }

                    if (includeArgumentValues)
                    {
                        var argumentValue = argumentValues[i];
                        if (argumentValue != null)
                        {
                            if (includeParameterTypes || includeParameterNames)
                            {
                                builder.Append(" = ");
                            }

                            builder.Append(argumentValue);
                        }
                    }
                }
                builder.Append(')');
            }

            return pooled.ToStringAndFree();
        }
    }
}
