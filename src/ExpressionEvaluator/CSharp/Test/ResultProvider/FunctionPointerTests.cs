// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using System;
using System.Diagnostics;
using Xunit;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
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
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "pfn") ? GetFunctionPointerField(v, m) : null;
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(assembly), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(ptr), type);
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
        }
    }
}
