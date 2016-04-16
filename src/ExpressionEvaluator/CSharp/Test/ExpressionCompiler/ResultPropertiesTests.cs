// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ResultPropertiesTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void Category()
        {
            var source = @"
class C
{
    int P { get; set; }
    int F;
    int M() { return 0; }

    void Test(int p)
    {
        int l;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                foreach (var expr in new[] { "this", "null", "1", "F", "p", "l" })
                {
                    Assert.Equal(DkmEvaluationResultCategory.Data, GetResultProperties(context, expr).Category);
                }

                Assert.Equal(DkmEvaluationResultCategory.Method, GetResultProperties(context, "M()").Category);
                Assert.Equal(DkmEvaluationResultCategory.Property, GetResultProperties(context, "P").Category);
            });
        }

        [Fact]
        public void StorageType()
        {
            var source = @"
class C
{
    int P { get; set; }
    int F;
    int M() { return 0; }

    static int SP { get; set; }
    static int SF;
    static int SM() { return 0; }

    void Test(int p)
    {
        int l;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                foreach (var expr in new[] { "this", "null", "1", "P", "F", "M()", "p", "l" })
                {
                    Assert.Equal(DkmEvaluationResultStorageType.None, GetResultProperties(context, expr).StorageType);
                }

                foreach (var expr in new[] { "SP", "SF", "SM()" })
                {
                    Assert.Equal(DkmEvaluationResultStorageType.Static, GetResultProperties(context, expr).StorageType);
                }
            });
        }

        [Fact]
        public void AccessType()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private int32 Private
  .field family int32 Protected
  .field assembly int32 Internal
  .field public int32 Public
  .field famorassem int32 ProtectedInternal
  .field famandassem int32 ProtectedAndInternal

  .method public hidebysig instance void 
          Test() cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method C::.ctor

} // end of class C
";
            var module = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(ilSource);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var context = CreateMethodContext(runtime, methodName: "C.Test");

            Assert.Equal(DkmEvaluationResultAccessType.Private, GetResultProperties(context, "Private").AccessType);
            Assert.Equal(DkmEvaluationResultAccessType.Protected, GetResultProperties(context, "Protected").AccessType);
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "Internal").AccessType);
            Assert.Equal(DkmEvaluationResultAccessType.Public, GetResultProperties(context, "Public").AccessType);

            // As in dev12.
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "ProtectedInternal").AccessType);
            Assert.Equal(DkmEvaluationResultAccessType.Internal, GetResultProperties(context, "ProtectedAndInternal").AccessType);

            Assert.Equal(DkmEvaluationResultAccessType.None, GetResultProperties(context, "null").AccessType);
        }

        [Fact]
        public void AccessType_Nested()
        {
            var source = @"
using System;

internal class C
{
    public int F;

    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                // Used the declared accessibility, rather than the effective accessibility.
                Assert.Equal(DkmEvaluationResultAccessType.Public, GetResultProperties(context, "F").AccessType);
            });
        }

        [Fact]
        public void ModifierFlags_Virtual()
        {
            var source = @"
using System;

class C
{
    public int P { get; set; }
    public int M() { return 0; }
    public event Action E;
    
    public virtual int VP { get; set; }
    public virtual int VM() { return 0; }
    public virtual event Action VE;

    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "P").ModifierFlags);
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "VP").ModifierFlags);

                Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "M()").ModifierFlags);
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "VM()").ModifierFlags);

                // Field-like events are borderline since they bind as event accesses, but get emitted as field accesses.
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "E").ModifierFlags);
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "VE").ModifierFlags);
            });
        }

        [Fact]
        public void ModifierFlags_Virtual_Variations()
        {
            var source = @"
using System;

abstract class Base
{
    public abstract int Override { get; set; }
}

abstract class Derived : Base
{
    public override int Override { get; set; }
    public abstract int Abstract { get; set; }

    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "Derived.Test");

                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "Abstract").ModifierFlags);
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Virtual, GetResultProperties(context, "Override").ModifierFlags);
            });
        }

        [Fact]
        public void ModifierFlags_Constant()
        {
            var source = @"
using System;

class C
{
    int F = 1;
    const int CF = 1;
    static readonly int SRF = 1;

    void Test(int p)
    {
        int l = 2;
        const int cl = 2;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                foreach (var expr in new[] { "null", "1", "1 + 1", "CF", "cl" })
                {
                    Assert.Equal(DkmEvaluationResultTypeModifierFlags.Constant, GetResultProperties(context, expr).ModifierFlags);
                }

                foreach (var expr in new[] { "this", "F", "SRF", "p", "l" })
                {
                    Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, expr).ModifierFlags);
                }
            });
        }

        [Fact]
        public void ModifierFlags_Volatile()
        {
            var source = @"
using System;

class C
{
    int F = 1;
    volatile int VF = 1;

    void Test(int p)
    {
        int l;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, GetResultProperties(context, "F").ModifierFlags);
                Assert.Equal(DkmEvaluationResultTypeModifierFlags.Volatile, GetResultProperties(context, "VF").ModifierFlags);
            });
        }

        [Fact]
        public void Assignment()
        {
            var source = @"
class C
{
    public virtual int P { get; set; }

    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileAssignment("P", "1", NoAliases, DebuggerDiagnosticFormatter.Instance, out resultProperties, out error, out missingAssemblyIdentities, EnsureEnglishUICulture.PreferredOrNull, testData);
                Assert.Null(error);
                Assert.Empty(missingAssemblyIdentities);

                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect, resultProperties.Flags);
                Assert.Equal(default(DkmEvaluationResultCategory), resultProperties.Category); // Not Data
                Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType); // Not Public
                Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
                Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags); // Not Virtual
            });
        }

        [Fact]
        public void LocalDeclaration()
        {
            var source = @"
class C
{
    public virtual int P { get; set; }

    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                ResultProperties resultProperties;
                string error;
                var testData = new CompilationTestData();
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileExpression(
                    "int z = 1;",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                Assert.Null(error);
                Assert.Empty(missingAssemblyIdentities);

                Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags);
                Assert.Equal(default(DkmEvaluationResultCategory), resultProperties.Category); // Not Data
                Assert.Equal(default(DkmEvaluationResultAccessType), resultProperties.AccessType);
                Assert.Equal(default(DkmEvaluationResultStorageType), resultProperties.StorageType);
                Assert.Equal(default(DkmEvaluationResultTypeModifierFlags), resultProperties.ModifierFlags);
            });
        }

        [Fact]
        public void Error()
        {
            var source = @"
class C
{
    void Test()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, methodName: "C.Test");

                VerifyErrorResultProperties(context, "x => x");
                VerifyErrorResultProperties(context, "Test");
                VerifyErrorResultProperties(context, "Missing");
                VerifyErrorResultProperties(context, "C");
            });
        }

        private static ResultProperties GetResultProperties(EvaluationContext context, string expr)
        {
            ResultProperties resultProperties;
            string error;
            context.CompileExpression(expr, out resultProperties, out error);
            Assert.Null(error);
            return resultProperties;
        }

        private static void VerifyErrorResultProperties(EvaluationContext context, string expr)
        {
            ResultProperties resultProperties;
            string error;
            context.CompileExpression(expr, out resultProperties, out error);
            Assert.NotNull(error);
            Assert.Equal(default(ResultProperties), resultProperties);
        }
    }
}
