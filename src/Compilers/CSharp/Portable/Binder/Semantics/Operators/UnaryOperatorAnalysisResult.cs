﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct UnaryOperatorAnalysisResult
    {
        public readonly UnaryOperatorSignature Signature;
        public readonly Conversion Conversion;
        public readonly OperatorAnalysisResultKind Kind;

        private UnaryOperatorAnalysisResult(OperatorAnalysisResultKind kind, UnaryOperatorSignature signature, Conversion conversion)
        {
            this.Kind = kind;
            this.Signature = signature;
            this.Conversion = conversion;
        }

        public bool IsValid
        {
            get { return this.Kind == OperatorAnalysisResultKind.Applicable; }
        }

        public bool HasValue
        {
            get { return this.Kind != OperatorAnalysisResultKind.Undefined; }
        }

        public static UnaryOperatorAnalysisResult Applicable(UnaryOperatorSignature signature, Conversion conversion)
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Applicable, signature, conversion);
        }

        public static UnaryOperatorAnalysisResult Inapplicable(UnaryOperatorSignature signature, Conversion conversion)
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Inapplicable, signature, conversion);
        }

        public UnaryOperatorAnalysisResult Worse()
        {
            return new UnaryOperatorAnalysisResult(OperatorAnalysisResultKind.Worse, this.Signature, this.Conversion);
        }
    }
}
