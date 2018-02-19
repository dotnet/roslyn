// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    internal struct BinaryOperatorAnalysisResult
    {
        public readonly Conversion LeftConversion;
        public readonly Conversion RightConversion;
        public readonly BinaryOperatorSignature Signature;
        public readonly OperatorAnalysisResultKind Kind;

        private BinaryOperatorAnalysisResult(OperatorAnalysisResultKind kind, BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
        {
            this.Kind = kind;
            this.Signature = signature;
            this.LeftConversion = leftConversion;
            this.RightConversion = rightConversion;
        }

        public bool IsValid
        {
            get { return this.Kind == OperatorAnalysisResultKind.Applicable; }
        }

        public bool HasValue
        {
            get { return this.Kind != OperatorAnalysisResultKind.Undefined; }
        }

        public override bool Equals(object obj)
        {
            // implement if needed
            throw ExceptionUtilities.Unreachable;
        }

        public override int GetHashCode()
        {
            // implement if needed
            throw ExceptionUtilities.Unreachable;
        }

        public static BinaryOperatorAnalysisResult Applicable(BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Applicable, signature, leftConversion, rightConversion);
        }

        public static BinaryOperatorAnalysisResult Inapplicable(BinaryOperatorSignature signature, Conversion leftConversion, Conversion rightConversion)
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Inapplicable, signature, leftConversion, rightConversion);
        }

        public BinaryOperatorAnalysisResult Worse()
        {
            return new BinaryOperatorAnalysisResult(OperatorAnalysisResultKind.Worse, this.Signature, this.LeftConversion, this.RightConversion);
        }
    }
}
