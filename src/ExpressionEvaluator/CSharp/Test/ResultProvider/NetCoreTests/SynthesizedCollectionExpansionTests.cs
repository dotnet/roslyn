// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests.NetCoreTests;

public class SynthesizedCollectionExpansionTests : CSharpResultProviderTestBase
{
    [Fact]
    public void SynthesizedCollections_Core()
    {
        // Synthesized collection types don't generate with a DebuggerTypeProxy attribute, but the ResultProvider should treat them specially and apply ICollectionDebugView as a type proxy.
        var source = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<int> x = [1];
        IEnumerable<int> y = [2, 3];
        IEnumerable<int> z = [.. x];
    }
}
";
        var assembly = GetAssembly(source);
        var types = assembly.GetTypes();
        var inspectionContext = CreateDkmInspectionContext(runtimeInstance: new DkmClrRuntimeInstance(typeof(object).Assembly));
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlySingleElementList`1")), 1, [1]);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyArray`1")), new int[] { 2, 3 }, [2, 3]);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyList`1")), new List<int>() { 1 }, [1]);

        void VerifySynthesizedType<T>(Type genericType, T ctorArgs, List<int> expectedChildValues)
        {
            var constructedType = genericType.MakeGenericType(typeof(int));
            var value = CreateDkmClrValue(constructedType.Instantiate(ctorArgs), constructedType, inspectionContext, evalFlags: DkmEvaluationResultFlags.None);
            var result = FormatResult("x", value, inspectionContext: inspectionContext);
            var children = GetChildren(result, inspectionContext: inspectionContext);
            DkmEvaluationResult[] expectedChildren =
            [
                ..expectedChildValues.Select((c, i) => EvalResult($"[{i}]", $"{c}", "int", $"new System.Collections.Generic.ICollectionDebugView<int>(x).Items[{i}]")),
                EvalResult("Raw View", null, "", "x, raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data)
            ];

            Verify(children, expectedChildren);
        }
    }
}
