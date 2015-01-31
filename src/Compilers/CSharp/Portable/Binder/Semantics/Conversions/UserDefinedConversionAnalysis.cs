// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public readonly MethodSymbol Operator;

        public readonly Conversion SourceConversion;
        public readonly Conversion TargetConversion;
        public readonly UserDefinedConversionAnalysisKind Kind;

        public static UserDefinedConversionAnalysis Normal(
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            return new UserDefinedConversionAnalysis(
                UserDefinedConversionAnalysisKind.ApplicableInNormalForm,
                op,
                sourceConversion,
                targetConversion,
                fromType,
                toType);
        }

        public static UserDefinedConversionAnalysis Lifted(
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            return new UserDefinedConversionAnalysis(
                UserDefinedConversionAnalysisKind.ApplicableInLiftedForm,
                op,
                sourceConversion,
                targetConversion,
                fromType,
                toType);
        }

        private UserDefinedConversionAnalysis(
            UserDefinedConversionAnalysisKind kind,
            MethodSymbol op,
            Conversion sourceConversion,
            Conversion targetConversion,
            TypeSymbol fromType,
            TypeSymbol toType)
        {
            this.Kind = kind;
            this.Operator = op;
            this.SourceConversion = sourceConversion;
            this.TargetConversion = targetConversion;
            this.FromType = fromType;
            this.ToType = toType;
        }
    }
}
