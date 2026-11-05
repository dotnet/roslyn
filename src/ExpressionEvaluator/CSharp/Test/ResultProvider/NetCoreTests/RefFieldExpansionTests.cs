// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests.NetCoreTests;

public class RefFieldExpansionTests : CSharpResultProviderTestBase
{
    [Fact]
    public void RefField_ExceptionResult_DoesNotPreventSiblingExpansion()
    {
        var source = """
            public ref struct S
            {
                public ref int F;
                public int I;
            }
            """;

        var compilation = CSharpTestBase.CreateCompilation(
            source,
            targetFramework: TargetFramework.Net70,
            parseOptions: TestOptions.Regular11,
            options: TestOptions.ReleaseDll);

        compilation.VerifyEmitDiagnostics();
        var assembly = ReflectionUtilities.Load(compilation.EmitToArray());

        DkmClrValue? GetMemberValue(DkmClrValue parent, string memberName)
        {
            // Only intercept members of our synthetic S value.
            if (parent.Type.GetLmrType().FullName != "S")
            {
                return null;
            }

            var runtime = parent.Type.RuntimeInstance;

            switch (memberName)
            {
                case "F":
                    // The debugger returned an exception result rather than throwing from Dereference.
                    var exception = new NullReferenceException();

                    return new DkmClrValue(
                        value: exception,
                        hostObjectValue: exception,
                        type: runtime.GetType(typeof(NullReferenceException)),
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.ExceptionThrown,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Data,
                        access: DkmEvaluationResultAccessType.Public);

                case "I":
                    return new DkmClrValue(
                        value: 10,
                        hostObjectValue: 10,
                        type: runtime.GetType(typeof(int)),
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Data,
                        access: DkmEvaluationResultAccessType.Public);

                default:
                    return null;
            }
        }

        var runtime = new DkmClrRuntimeInstance(
            ReflectionUtilities.GetMscorlib(assembly),
            getMemberValue: GetMemberValue);

        using (runtime.Load())
        {
            // A synthetic carrier avoids boxing the ref struct.
            // Every instance field is supplied by GetMemberValue above.
            var value = CreateDkmClrValue(new object(), runtime.GetType("S"));
            var result = FormatResult("s", value);

            Verify(result,
                EvalResult(
                    "s", "{S}", "S", "s",
                    DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(result),
                EvalResult(
                    "F",
                    "'s.F' threw an exception of type 'System.NullReferenceException'",
                    "int {System.NullReferenceException}",
                    "s.F",
                    DkmEvaluationResultFlags.Expandable |
                    DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult(
                    "I", "10", "int", "s.I",
                    DkmEvaluationResultFlags.CanFavorite));
        }
    }

    [Fact]
    public void RefField_DereferenceThrows_DoesNotPreventSiblingExpansion()
    {
        var source = """
            public ref struct S
            {
                public ref int F;
                public int I;
            }
            """;

        var compilation = CSharpTestBase.CreateCompilation(
            source,
            targetFramework: TargetFramework.Net70,
            parseOptions: TestOptions.Regular11,
            options: TestOptions.ReleaseDll);

        compilation.VerifyEmitDiagnostics();
        var assembly = ReflectionUtilities.Load(compilation.EmitToArray());

        DkmClrValue? GetMemberValue(DkmClrValue parent, string memberName)
        {
            // Only intercept members of our synthetic S value.
            if (parent.Type.GetLmrType().FullName != "S")
            {
                return null;
            }

            var runtime = parent.Type.RuntimeInstance;

            switch (memberName)
            {
                case "F":
                    return new DkmClrValue(
                        value: null,
                        hostObjectValue: null,
                        type: runtime.GetType(typeof(int).MakeByRefType()),
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Data,
                        access: DkmEvaluationResultAccessType.Public)
                    {
                        // A null payload alone does not model an invalid managed reference in the mock.
                        DereferenceOverride = _ => throw new InvalidOperationException("Cannot dereference invalid value")
                    };

                case "I":
                    return new DkmClrValue(
                        value: 10,
                        hostObjectValue: 10,
                        type: runtime.GetType(typeof(int)),
                        alias: null,
                        evalFlags: DkmEvaluationResultFlags.None,
                        valueFlags: DkmClrValueFlags.None,
                        category: DkmEvaluationResultCategory.Data,
                        access: DkmEvaluationResultAccessType.Public);

                default:
                    return null;
            }
        }

        var runtime = new DkmClrRuntimeInstance(
            ReflectionUtilities.GetMscorlib(assembly),
            getMemberValue: GetMemberValue);

        using (runtime.Load())
        {
            // A synthetic carrier avoids boxing the ref struct.
            // Every instance field is supplied by GetMemberValue above.
            var value = CreateDkmClrValue(new object(), runtime.GetType("S"));
            var result = FormatResult("s", value);

            Verify(result,
                EvalResult(
                    "s", "{S}", "S", "s",
                    DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(result),
                EvalFailedResult(
                    "F",
                    "Cannot dereference invalid value"),
                EvalResult(
                    "I",
                    "10",
                    "int",
                    "s.I",
                    DkmEvaluationResultFlags.CanFavorite));
        }
    }
}
