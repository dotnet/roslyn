// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
