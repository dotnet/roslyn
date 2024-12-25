// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class NoPIATests : ExpressionCompilerTestBase
    {
        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1033598")]
        public void ExplicitEmbeddedType()
        {
            var source =
@"using System.Runtime.InteropServices;
[TypeIdentifier]
[Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9D8"")]
public interface I
{
    object F();
}
class C
{
    void M()
    {
        var o = (I)null;
    }
    static void Main()
    {
        (new C()).M();
    }
}";
            var compilation0 = CreateCompilation(source, options: TestOptions.DebugExe);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("this", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (I V_0) //o
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1035310")]
        public void EmbeddedType()
        {
            var sourcePIA =
@"using System.Runtime.InteropServices;
[assembly: PrimaryInteropAssembly(0, 0)]
[assembly: Guid(""863D5BC0-46A1-49AC-97AA-A5F0D441A9DA"")]
[ComImport]
[Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9DA"")]
public interface I
{
    object F();
}";
            var source =
@"class C
{
    static void M()
    {
        var o = (I)null;
    }
}";
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            var referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes: true);

            var compilation0 = CreateCompilation(source, new[] { referencePIA }, TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                var result = context.CompileExpression("o", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (I V_0) //o
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
            });
        }

        /// <summary>
        /// Duplicate type definitions: in PIA
        /// and as embedded type.
        /// </summary>
        [Fact]
        public void PIATypeAndEmbeddedType()
        {
            var sourcePIA =
@"using System.Runtime.InteropServices;
[assembly: PrimaryInteropAssembly(0, 0)]
[assembly: Guid(""863D5BC0-46A1-49AC-97AA-A5F0D441A9DC"")]
[ComImport]
[Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9DC"")]
public interface I
{
    object F();
}";
            var sourceA =
@"public class A
{
    public static void M(I x)
    {
    }
}";
            var sourceB =
@"class B
{
    static void Main()
    {
        I y = null;
        A.M(y);
    }
}";
            var modulePIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll).ToModuleInstance();

            // csc /t:library /l:PIA.dll A.cs
            var moduleA = CreateCompilation(
                sourceA,
                options: TestOptions.DebugDll,
                references: new[] { modulePIA.GetReference().WithEmbedInteropTypes(true) }).ToModuleInstance();

            // csc /r:A.dll /r:PIA.dll B.cs
            var moduleB = CreateCompilation(
                sourceB,
                options: TestOptions.DebugExe,
                references: new[] { moduleA.GetReference(), modulePIA.GetReference() }).ToModuleInstance();

            var runtime = CreateRuntimeInstance(new[] { MscorlibRef.ToModuleInstance(), moduleA, modulePIA, moduleB });
            var context = CreateMethodContext(runtime, "A.M");
            ResultProperties resultProperties;
            string error;

            // Bind to local of embedded PIA type.
            var testData = new CompilationTestData();
            context.CompileExpression("x", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");

            // Binding to method on original PIA should fail
            // since it was not included in embedded type.
            ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
            context.CompileExpression(
                "x.F()",
                DkmEvaluationFlags.TreatAsExpression,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                out resultProperties,
                out error,
                out missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            AssertEx.SetEqual(missingAssemblyIdentities, EvaluationContextBase.SystemCoreIdentity);
            Assert.Equal("error CS1061: 'I' does not contain a definition for 'F' and no accessible extension method 'F' accepting a first argument of type 'I' could be found (are you missing a using directive or an assembly reference?)", error);

            // Binding to method on original PIA should succeed
            // in assembly referencing PIA.dll.
            context = CreateMethodContext(runtime, "B.Main");
            testData = new CompilationTestData();
            context.CompileExpression("y.F()", out error, testData);
            Assert.Null(error);
            testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
// Code size        7 (0x7)
.maxstack  1
.locals init (I V_0) //y
IL_0000:  ldloc.0
IL_0001:  callvirt   ""object I.F()""
IL_0006:  ret
}");
        }
    }
}
