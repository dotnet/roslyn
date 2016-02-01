// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public abstract class CSharpResultProviderTestBase : ResultProviderTestBase
    {
        private static readonly ResultProvider s_resultProvider = new CSharpResultProvider();
        private static readonly DkmInspectionContext s_inspectionContext = CreateDkmInspectionContext(s_resultProvider.Formatter, DkmEvaluationFlags.None, radix: 10);

        public CSharpResultProviderTestBase()
            : base(s_resultProvider, s_inspectionContext)
        {
        }

        public static Assembly GetAssembly(string source)
        {
            var comp = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source);
            return ReflectionUtilities.Load(comp.EmitToArray());
        }

        public static Assembly GetUnsafeAssembly(string source)
        {
            var comp = CSharpTestBaseBase.CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.UnsafeReleaseDll);
            return ReflectionUtilities.Load(comp.EmitToArray());
        }

        protected static string PointerToString(IntPtr pointer)
        {
            if (Environment.Is64BitProcess)
            {
                return string.Format("0x{0:x16}", pointer.ToInt64());
            }
            else
            {
                return string.Format("0x{0:x8}", pointer.ToInt32());
            }
        }
    }
}
