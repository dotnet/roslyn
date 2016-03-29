// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public abstract class CSharpResultProviderTestBase : ResultProviderTestBase
    {
        public CSharpResultProviderTestBase() : this(new CSharpFormatter())
        {
        }

        private CSharpResultProviderTestBase(CSharpFormatter formatter) :
            this(new DkmInspectionSession(ImmutableArray.Create<IDkmClrFormatter>(formatter), ImmutableArray.Create<IDkmClrResultProvider>(new CSharpResultProvider(formatter, formatter))))
        {
        }

        internal CSharpResultProviderTestBase(DkmInspectionSession inspectionSession, DkmInspectionContext defaultInspectionContext = null) :
            base(inspectionSession, defaultInspectionContext ?? CreateDkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix: 10))
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
