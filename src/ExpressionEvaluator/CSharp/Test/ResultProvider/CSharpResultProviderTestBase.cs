// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
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
            this(CreateDkmInspectionSession(formatter))
        {
        }

        internal CSharpResultProviderTestBase(DkmInspectionSession inspectionSession, DkmInspectionContext defaultInspectionContext = null) :
            base(inspectionSession, defaultInspectionContext ?? CreateDkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix: 10))
        {
        }

        internal static DkmInspectionContext CreateDkmInspectionContext(DkmEvaluationFlags evalFlags)
        {
            var inspectionSession = CreateDkmInspectionSession();
            return CreateDkmInspectionContext(inspectionSession, evalFlags, radix: 10);
        }

        private static DkmInspectionSession CreateDkmInspectionSession(CSharpFormatter formatter = null)
        {
            formatter = formatter ?? new CSharpFormatter();
            return new DkmInspectionSession(ImmutableArray.Create<IDkmClrFormatter>(formatter), ImmutableArray.Create<IDkmClrResultProvider>(new CSharpResultProvider(formatter, formatter)));
        }

        public static Assembly GetAssembly(string source)
        {
            var comp = CSharpTestBase.CreateCompilationWithMscorlib461AndCSharp(source);
            return ReflectionUtilities.Load(comp.EmitToArray());
        }

        public static Assembly GetUnsafeAssembly(string source)
        {
            var comp = CSharpTestBase.CreateCompilationWithMscorlib461AndCSharp(source, options: TestOptions.UnsafeReleaseDll);
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

        protected static string PointerToString(UIntPtr pointer)
        {
            if (Environment.Is64BitProcess)
            {
                return string.Format("0x{0:x16}", pointer.ToUInt64());
            }
            else
            {
                return string.Format("0x{0:x8}", pointer.ToUInt32());
            }
        }
    }
}
