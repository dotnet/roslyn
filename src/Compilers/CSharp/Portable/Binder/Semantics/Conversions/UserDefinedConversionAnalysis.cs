// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum UserDefinedConversionAnalysisKind : byte
    {
        ApplicableInNormalForm,
        ApplicableInLiftedForm
    }

    internal sealed class UserDefinedConversionAnalysis
    {
        public readonly TypeSymbol FromType;
        public readonly TypeSymbol ToType;
        public readonly TypeParameterSymbol ConstrainedToTypeOpt;
        public readonly MethodSymbol Operator;

        public readonly Conversion SourceConversion;
        public readonly Conversion TargetConversion;
        public readonly UserDefinedConversionAnalysisKind Kind;

        public static UserDefinedConversionAnalysis Normal(
            TypeParameterSymbol constrainedToTypeOpt,
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            return new UserDefinedConversionAnalysis(
                UserDefinedConversionAnalysisKind.ApplicableInNormalForm,
                constrainedToTypeOpt,
                op,
                sourceConversion,
                targetConversion,
                fromType,
                toType);
        }

        public static UserDefinedConversionAnalysis Lifted(
            TypeParameterSymbol constrainedToTypeOpt,
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            return new UserDefinedConversionAnalysis(
                UserDefinedConversionAnalysisKind.ApplicableInLiftedForm,
                constrainedToTypeOpt,
                op,
                sourceConversion,
                targetConversion,
                fromType,
                toType);
        }

        private UserDefinedConversionAnalysis(
            UserDefinedConversionAnalysisKind kind,
            TypeParameterSymbol constrainedToTypeOpt,
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            this.Kind = kind;
            this.ConstrainedToTypeOpt = constrainedToTypeOpt;
            this.Operator = op;
            this.SourceConversion = sourceConversion;
            this.TargetConversion = targetConversion;
            this.FromType = fromType;
            this.ToType = toType;
        }
    }
}
