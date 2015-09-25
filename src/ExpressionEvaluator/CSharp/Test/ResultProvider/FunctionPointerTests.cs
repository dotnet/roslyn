// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using System;
using System.Diagnostics;
using Xunit;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FunctionPointerTests : CSharpResultProviderTestBase
    {
        [Fact(Skip = "Tests are failing in Jenkins queues")]
        public void Root()
        {
            const int ptr = 0x1234;
            var value = CreateDkmClrValue(ptr, type: new DkmClrType(FunctionPointerType.Instance));
            var evalResult = FormatResult("pfn", value);
            Verify(evalResult,
                EvalResult("pfn", PointerToString(new IntPtr(ptr)), "System.Object*", "pfn", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Other));
        }

        [Fact(Skip = "Tests are failing in Jenkins queues")]
        public void Member()
        {
            var source =
@"class C
{
    object pfn;
}";
            const int ptr = 0x0;
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "pfn") ? CreateDkmClrValue(ptr, type: new DkmClrType(FunctionPointerType.Instance)) : null;
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Other));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("pfn", PointerToString(new IntPtr(ptr)), "object {System.Object*}", "o.pfn", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Other));
            }
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
