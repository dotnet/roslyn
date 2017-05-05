// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.FunctionResolution;

namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class VisualBasicFunctionResolver : FunctionResolver
    {
        public VisualBasicFunctionResolver()
        {
        }

        internal override RequestSignature GetParsedSignature(DkmRuntimeFunctionResolutionRequest request)
        {
            return MemberSignatureParser.Parse(request.FunctionName);
        }

        internal override bool IgnoreCase => true;

        internal override Guid LanguageId => DkmLanguageId.VB;
    }
}
