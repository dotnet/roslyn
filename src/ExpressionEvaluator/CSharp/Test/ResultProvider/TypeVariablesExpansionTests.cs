// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class TypeVariablesExpansionTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void TypeVariables()
        {
            var source0 =
@"class A
{
}
class B : A
{
    internal static object F = 1;
}";
            var assembly0 = GetAssembly(source0);
            var type0 = assembly0.GetType("B");
            var source1 =
@".class private abstract sealed beforefieldinit specialname '<>c__TypeVariables'<T,U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(source1, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly1 = ReflectionUtilities.Load(assemblyBytes);
            var type1 = assembly1.GetType(ExpressionCompilerConstants.TypeVariablesClassName).MakeGenericType([typeof(int), type0]);
            var value = CreateDkmClrValue(value: null, type: type1, valueFlags: DkmClrValueFlags.Synthetic);
            var evalResult = FormatResult("typevars", value);
            Verify(evalResult,
                EvalResult("Type variables", "", "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("T", "int", "int", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("U", "B", "B", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }
    }
}
