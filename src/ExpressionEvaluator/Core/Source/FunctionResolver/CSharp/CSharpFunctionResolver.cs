// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.FunctionResolution;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpFunctionResolver : FunctionResolver
    {
        public CSharpFunctionResolver()
        {
        }

        internal override RequestSignature GetParsedSignature(DkmRuntimeFunctionResolutionRequest request)
        {
            return MemberSignatureParser.Parse(request.FunctionName);
        }

        internal override bool IgnoreCase => false;

        internal override Guid LanguageId => DkmLanguageId.CSharp;
    }
}
