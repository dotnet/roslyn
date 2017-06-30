// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpLanguageInstructionDecoder : LanguageInstructionDecoder<CSharpCompilation, MethodSymbol, PEModuleSymbol, TypeSymbol, TypeParameterSymbol, ParameterSymbol>
    {
        public CSharpLanguageInstructionDecoder()
            : base(CSharpInstructionDecoder.Instance)
        {
        }
    }
}
