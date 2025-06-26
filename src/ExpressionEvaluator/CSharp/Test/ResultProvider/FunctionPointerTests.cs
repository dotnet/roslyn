// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using Xunit;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class FunctionPointerTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Root()
        {
            var source =
@"unsafe class C
{
    internal C(long p)
    {
        this.pfn = (int*)p;
    }
    int* pfn;
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                int i = 0x1234;
                long ptr = (long)&i;
                var type = assembly.GetType("C");
                var value = GetFunctionPointerField(CreateDkmClrValue(type.Instantiate(ptr)), "pfn");
                var evalResult = FormatResult("pfn", value);
                Verify(evalResult,
                    EvalResult("pfn", PointerToString(new IntPtr(ptr)), "System.Object*", "pfn", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Other));
            }
        }

        [Fact]
        public void Member()
        {
            var source =
@"unsafe class C
{
    internal C(long p)
    {
        this.pfn = (int*)p;
    }
    int* pfn;
}";
            var assembly = GetUnsafeAssembly(source);
            const long ptr = 0x0;
            DkmClrValue getMemberValue(DkmClrValue v, string m) => (m == "pfn") ? GetFunctionPointerField(v, m) : null;
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(assembly), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = type.Instantiate(ptr);
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Other));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("pfn", PointerToString(new IntPtr(ptr)), "int*", "o.pfn", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Other));
            }
        }

        private DkmClrValue GetFunctionPointerField(DkmClrValue value, string fieldName)
        {
            var valueType = value.Type.GetLmrType();
            var fieldInfo = valueType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fieldValue = fieldInfo.GetValue(value.RawValue);
            return CreateDkmClrValue(DkmClrValue.UnboxPointer(fieldValue), new DkmClrType(FunctionPointerType.Instance));
        }

        // Function pointer type has IsPointer == true and GetElementType() == null.
        private sealed class FunctionPointerType : TypeImpl
        {
            internal static readonly FunctionPointerType Instance = new FunctionPointerType();

            private FunctionPointerType() : base(typeof(object).MakePointerType())
            {
                Debug.Assert(this.IsPointer);
            }

            public override Type GetElementType()
            {
                return null;
            }

            public override bool IsFunctionPointer()
            {
                return true;
            }
        }
    }
}
