// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    internal readonly struct BinaryOperatorAnalysisResult : IMemberResolutionResultWithPriority<MethodSymbol>
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

        MethodSymbol IMemberResolutionResultWithPriority<MethodSymbol>.MemberWithPriority => Signature.Method;

        public override bool Equals(object obj)
        {
            // implement if needed
            throw ExceptionUtilities.Unreachable();
        }

        public override int GetHashCode()
        {
            // implement if needed
            throw ExceptionUtilities.Unreachable();
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
