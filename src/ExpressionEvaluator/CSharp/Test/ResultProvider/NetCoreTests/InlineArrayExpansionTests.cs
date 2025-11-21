// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests.NetCoreTests;

public class InlineArrayExpansionTests : CSharpResultProviderTestBase
{
    [Fact]
    public void InlineArrayExpansion()
    {
        var hostObject = new SampleInlineArray<int>();
        for (int i = 0; i < SampleInlineArray<int>.Length; i++)
        {
            hostObject[i] = i;
        }

        var value = CreateDkmClrValue(hostObject, typeof(SampleInlineArray<int>), evalFlags: DkmEvaluationResultFlags.None);

        const string rootExpr = "new SampleInlineArray<int>()";
        var evalResult = (DkmSuccessEvaluationResult)FormatResult(rootExpr, value);
        Verify(evalResult,
            EvalResult(rootExpr, "0,1,2,3", "Microsoft.CodeAnalysis.ExpressionEvaluator.SampleInlineArray<int>", rootExpr, DkmEvaluationResultFlags.Expandable));

        Verify(GetChildren(evalResult),
            EvalResult("[0]", "0", "int", "(new SampleInlineArray<int>())[0]", DkmEvaluationResultFlags.None),
            EvalResult("[1]", "1", "int", "(new SampleInlineArray<int>())[1]", DkmEvaluationResultFlags.None),
            EvalResult("[2]", "2", "int", "(new SampleInlineArray<int>())[2]", DkmEvaluationResultFlags.None),
            EvalResult("[3]", "3", "int", "(new SampleInlineArray<int>())[3]", DkmEvaluationResultFlags.None));
    }
}
