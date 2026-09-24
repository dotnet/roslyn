// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        // Synthesized collection types don't generate with a DebuggerTypeProxy attribute, but the ResultProvider
        // should still expand them as collections.
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
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlySingleElementList`1")), 1, [1], DkmEvaluationResultFlags.ReadOnly);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyArray`1")), new int[] { 2, 3 }, [2, 3]);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyList`1")), new List<int>() { 1 }, [1]);

        void VerifySynthesizedType<T>(Type genericType, T ctorArgs, List<int> expectedChildValues, DkmEvaluationResultFlags expectedChildFlags = DkmEvaluationResultFlags.None)
        {
            var constructedType = genericType.MakeGenericType(typeof(int));
            var value = CreateDkmClrValue(constructedType.Instantiate(ctorArgs), constructedType, inspectionContext, evalFlags: DkmEvaluationResultFlags.None);
            var result = FormatResult("x", value, inspectionContext: inspectionContext);
            var children = GetChildren(result, inspectionContext: inspectionContext);
            DkmEvaluationResult[] expectedChildren =
            [
                ..expectedChildValues.Select((c, i) => EvalResult($"[{i}]", $"{c}", "int", fullName: null, flags: expectedChildFlags)),
                EvalResult("Raw View", null, "", "x, raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data)
            ];

            Verify(children, expectedChildren);
        }
    }

    [Fact]
    public void SynthesizedCollections_RawView()
    {
        var source = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<int> x = [1, 2];
    }
}
";
        var assembly = GetAssembly(source);
        var type = assembly.GetTypes().First(t => t.Name.Equals("<>z__ReadOnlyArray`1")).MakeGenericType(typeof(int));
        var inspectionContext = CreateDkmInspectionContext(
            DkmEvaluationFlags.ShowValueRaw,
            runtimeInstance: new DkmClrRuntimeInstance(typeof(object).Assembly));
        var value = CreateDkmClrValue(type.Instantiate(new int[] { 1, 2 }), type, inspectionContext, evalFlags: DkmEvaluationResultFlags.None);

        var result = FormatResult("x", "x, raw", value, inspectionContext: inspectionContext);
        var children = GetChildren(result, inspectionContext: inspectionContext);
        Assert.DoesNotContain(children, child => child.Name == "[0]");
    }

    [Fact]
    public void SynthesizedCollections_NestedArrayElement()
    {
        var source = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<int> x = [1, 2];
    }
}
";
        var assembly = GetAssembly(source);
        var synthesizedType = assembly.GetTypes().First(t => t.Name.Equals("<>z__ReadOnlyArray`1")).MakeGenericType(typeof(int));
        var synthesizedValue = synthesizedType.Instantiate(new int[] { 1, 2 });
        var inspectionContext = CreateDkmInspectionContext(runtimeInstance: new DkmClrRuntimeInstance(typeof(object).Assembly));
        var value = CreateDkmClrValue(new object[] { synthesizedValue }, typeof(object[]), inspectionContext, evalFlags: DkmEvaluationResultFlags.None);

        var result = FormatResult("a", value, inspectionContext: inspectionContext);
        var arrayChildren = GetChildren(result, inspectionContext: inspectionContext);
        var synthesizedElement = (DkmSuccessEvaluationResult)arrayChildren.Single();
        Assert.Equal("[0]", synthesizedElement.Name);
        Assert.Equal("Count = 2", synthesizedElement.Value);

        var synthesizedChildren = GetChildren(synthesizedElement, inspectionContext: inspectionContext);
        Verify(
            synthesizedChildren,
            EvalResult("[0]", "1", "int", fullName: null),
            EvalResult("[1]", "2", "int", fullName: null),
            EvalResult("Raw View", null, "", synthesizedElement.FullName + ", raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data));
    }

    [Fact]
    public void SynthesizedCollections_DynamicElement()
    {
        var source = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<dynamic> x = [1];
        IEnumerable<dynamic> y = [2, 3];
        IEnumerable<dynamic> z = [.. x];
    }
}
";
        var assembly = GetAssembly(source);
        var types = assembly.GetTypes();
        var inspectionContext = CreateDkmInspectionContext(runtimeInstance: new DkmClrRuntimeInstance(typeof(object).Assembly));
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlySingleElementList`1")), 1, [1], DkmEvaluationResultFlags.ReadOnly);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyArray`1")), new object[] { 2, 3 }, [2, 3]);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyList`1")), new List<object>() { 1 }, [1]);

        void VerifySynthesizedType<T>(Type genericType, T ctorArgs, List<int> expectedChildValues, DkmEvaluationResultFlags expectedChildFlags = DkmEvaluationResultFlags.None)
        {
            var constructedType = genericType.MakeGenericType(typeof(object));
            var value = CreateDkmClrValue(constructedType.Instantiate(ctorArgs), constructedType, inspectionContext, evalFlags: DkmEvaluationResultFlags.None);
            var result = FormatResult(
                "x",
                "x",
                value,
                declaredType: new DkmClrType((TypeImpl)typeof(IEnumerable<object>)),
                declaredTypeInfo: MakeCustomTypeInfo(false, true),
                inspectionContext: inspectionContext);
            var children = GetChildren(result, inspectionContext: inspectionContext);
            DkmEvaluationResult[] expectedChildren =
            [
                ..expectedChildValues.Select((c, i) => EvalResult($"[{i}]", $"{c}", "dynamic {int}", fullName: null, flags: expectedChildFlags)),
                EvalResult("Raw View", null, "", "x, raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data)
            ];

            Verify(children, expectedChildren);
        }
    }

    [Fact]
    public void SynthesizedCollections_TupleElementNames()
    {
        var source = @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<(int A, int B)> x = [(1, 2)];
        IEnumerable<(int A, int B)> y = [(3, 4), (5, 6)];
        IEnumerable<(int A, int B)> z = [.. x];
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] names)
        {
        }
    }
}
";
        var assembly = GetAssembly(source);
        var types = assembly.GetTypes();
        var inspectionContext = CreateDkmInspectionContext(runtimeInstance: new DkmClrRuntimeInstance(typeof(object).Assembly));
        var tupleValue = new ValueTuple<int, int>(1, 2);
        var tupleElementNames = CustomTypeInfo.Create(
            dynamicFlags: null,
            tupleElementNames: new ReadOnlyCollection<string?>(["A", "B"]));

        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlySingleElementList`1")), tupleValue, DkmEvaluationResultFlags.ReadOnly);
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyArray`1")), new[] { tupleValue });
        VerifySynthesizedType(types.First(t => t.Name.Equals("<>z__ReadOnlyList`1")), new List<ValueTuple<int, int>>() { tupleValue });

        void VerifySynthesizedType<T>(Type genericType, T ctorArgs, DkmEvaluationResultFlags expectedChildFlags = DkmEvaluationResultFlags.None)
        {
            var constructedType = genericType.MakeGenericType(typeof(ValueTuple<int, int>));
            var value = CreateDkmClrValue(constructedType.Instantiate(ctorArgs), constructedType, inspectionContext, evalFlags: DkmEvaluationResultFlags.None);
            var result = FormatResult(
                "x",
                "x",
                value,
                declaredType: new DkmClrType((TypeImpl)typeof(IEnumerable<ValueTuple<int, int>>)),
                declaredTypeInfo: tupleElementNames,
                inspectionContext: inspectionContext);
            var children = GetChildren(result, inspectionContext: inspectionContext);
            Verify(
                children,
                EvalResult("[0]", "(1, 2)", "(int A, int B)", fullName: null, flags: expectedChildFlags | DkmEvaluationResultFlags.Expandable),
                EvalResult("Raw View", null, "", "x, raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data));
        }
    }
}
