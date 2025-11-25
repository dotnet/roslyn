// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NET
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalStateTracingTests : CSharpTestBase
    {
        // TODO: https://github.com/dotnet/roslyn/issues/66809
        // arrays (stloc, stelem),
        // collections, anonymous types, tuples
        // LogLocalStoreUnmanaged with local/parameter typed to a generic parameter
        // Primary constructor with field initializers

        private static readonly EmitOptions s_emitOptions = GetEmitOptions(InstrumentationKindExtensions.LocalStateTracing);

        private static EmitOptions GetEmitOptions(params InstrumentationKind[] kinds)
        {
            var options = EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.CreateRange(kinds));
            options.TestOnly_AllowLocalStateTracing();
            return options;
        }

        private const string TrackerTypeName = "Microsoft.CodeAnalysis.Runtime.LocalStoreTracker";

        private static readonly string s_helpers = """
namespace Microsoft.CodeAnalysis.Runtime
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using static System.Console;

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public unsafe readonly ref partial struct LocalStoreTracker
    {
        private static int s_stateMachineId;

        private readonly MethodBase M;
        private readonly ParameterInfo[] Parameters;

        private LocalStoreTracker(MethodBase m)
        {
            M = m;
            Parameters = m.GetParameters();
        }

        public static LocalStoreTracker LogMethodEntry(int methodId)
            => Entry(methodId, lambdaId: 0, stateMachineId: 0);

        public static LocalStoreTracker LogLambdaEntry(int methodId, int lambdaId)
            => Entry(methodId, lambdaId, stateMachineId: 0);

        public static LocalStoreTracker LogStateMachineMethodEntry(int methodId, ulong stateMachineId)
            => Entry(methodId, lambdaId: 0, stateMachineId);

        public static LocalStoreTracker LogStateMachineLambdaEntry(int methodId, int lambdaId, ulong stateMachineId)
            => Entry(methodId, lambdaId, stateMachineId);

        public void LogReturn()
            => WriteLine($"{MethodDisplay(M)}: Returned");

        public static ulong GetNewStateMachineInstanceId()
            => unchecked((ulong)Interlocked.Increment(ref s_stateMachineId));

        private static LocalStoreTracker Entry(int methodId, int lambdaId, ulong stateMachineId)
        {
            var module = typeof(LocalStoreTracker).Assembly.Modules.Single();
            var method = module.ResolveMethod(methodId + 0x06000000);
            var message = $"{MethodDisplay(method)}: Entered";

            if (lambdaId > 0)
            {
                var lambda = module.ResolveMethod(lambdaId + 0x06000000);
                message += $" lambda '{MethodDisplay(lambda)}'";
                method = lambda;
            }

            if (stateMachineId > 0)
            {
                message += $" state machine #{stateMachineId}";
            }

            WriteLine(message);
            return new(method);
        }

        private void WL(object value, int index)
            => WriteLine($"{MethodDisplay(M)}: {L(index)} = {ConvertToString(value)}");

        private void WP(object value, int index)
            => WriteLine($"{MethodDisplay(M)}: {P(index)} = {ConvertToString(value)}");

        private string L(int index)
            => (index >= 0x10000) ? $"L'{UnmangleFieldName(M.Module.ResolveField(index - 0x10000 + 0x04000000).Name)}'" : $"L{index}";

        private string P(int index)
            => (index >= 0x10000) ? $"P'{M.Module.ResolveField(index - 0x10000 + 0x04000000).Name}'" : $"P'{Parameters[index].Name}'[{index}]";

        private static string UnmangleFieldName(string name)
            => (name[0] == '<') ? name.Substring(1, name.IndexOf('>') - 1) : name;

        private static string MemoryToString(void* address, int size)
            => "<" + BitConverter.ToString(new Span<byte>(address, size).ToArray()) + ">";

        private string ConvertToString(object value) => value switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            object o => o.ToString(),
            null => "null",
        };

        public void LogLocalStore(bool value, int index) => WL(value, index);
        public void LogLocalStore(byte value, int index) => WL(value, index);
        public void LogLocalStore(ushort value, int index) => WL(value, index);
        public void LogLocalStore(uint value, int index) => WL(value, index);
        public void LogLocalStore(ulong value, int index) => WL(value, index);
        public void LogLocalStore(float value, int index) => WL(value, index);
        public void LogLocalStore(double value, int index) => WL(value, index);
        public void LogLocalStore(decimal value, int index) => WL(value, index);
        public void LogLocalStore(string value, int index) => WL(value, index);
        public void LogLocalStore(object value, int index) => WL(value, index);
        public void LogLocalStore(void* value, int index) => WL((nuint)value, index);
        public void LogLocalStoreUnmanaged(void* address, int size, int index) => WL(MemoryToString(address, size), index);
        public void LogLocalStoreParameterAlias(int sourceParameterIndex, int targetLocalIndex) { WriteLine($"{M.Name}: {L(targetLocalIndex)} -> {P(sourceParameterIndex)}"); }

        public void LogParameterStore(bool value, int index) => WP(value, index);
        public void LogParameterStore(byte value, int index) => WP(value, index);
        public void LogParameterStore(ushort value, int index) => WP(value, index);
        public void LogParameterStore(uint value, int index) => WP(value, index);
        public void LogParameterStore(ulong value, int index) => WP(value, index);
        public void LogParameterStore(float value, int index) => WP(value, index);
        public void LogParameterStore(double value, int index) => WP(value, index);
        public void LogParameterStore(decimal value, int index) => WP(value, index);
        public void LogParameterStore(string value, int index) => WP(value, index);
        public void LogParameterStore(object value, int index) => WP(value, index);
        public void LogParameterStore(void* value, int index) => WP((nuint)value, index);
        public void LogParameterStoreUnmanaged(void* address, int size, int index) => WP(MemoryToString(address, size), index);
        public void LogParameterStoreParameterAlias(int sourceParameterIndex, int targetParameterIndex) { WriteLine($"{M.Name}: {P(targetParameterIndex)} -> {P(sourceParameterIndex)}"); }

        public void LogLocalStoreLocalAlias(int sourceLocalIndex, int targetLocalIndex) { WriteLine($"{M.Name}: {L(targetLocalIndex)} -> {L(sourceLocalIndex)}"); }

        private static string MethodDisplay(MethodBase method)
        {
            bool includeContainingType = INCLUDE_CONTAINING_TYPE;
            return includeContainingType
                ? method.DeclaringType.FullName + "." + method.Name
                : method.Name;
        }
    }
}
""";
        /// <param name="displayContainingType">Set to true to include the containing type when displaying a method being entered.</param>
        private static string WithHelpers(string source, bool displayContainingType = false)
        {
            return source + s_helpers.Replace("INCLUDE_CONTAINING_TYPE", displayContainingType ? "true" : "false");
        }

        private const TargetFramework s_targetFramework = TargetFramework.Net70;

        private static readonly Verification s_verification = Verification.Fails with
        {
            ILVerifyMessage = """
            [LogMethodEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x9 }
            [LogLambdaEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x9 }
            [LogStateMachineMethodEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x8 }
            [LogStateMachineLambdaEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x8 }
            [Entry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xbc }
            [MemoryToString]: Unmanaged pointers are not a verifiable type. { Offset = 0x5 }
            [LogLocalStore]: Unmanaged pointers are not a verifiable type. { Offset = 0x1 }
            [LogLocalStoreUnmanaged]: Unmanaged pointers are not a verifiable type. { Offset = 0x1 }
            [LogParameterStore]: Unmanaged pointers are not a verifiable type. { Offset = 0x1 }
            [LogParameterStoreUnmanaged]: Unmanaged pointers are not a verifiable type. { Offset = 0x1 }
            """
        };

        private CompilationVerifier CompileAndVerify(string source, string? ilVerifyMessage = null, string? expectedOutput = null, TargetFramework targetFramework = s_targetFramework)
            => CompileAndVerify(
                source,
                options: (expectedOutput != null) ? TestOptions.UnsafeDebugExe : TestOptions.UnsafeDebugDll,
                emitOptions: s_emitOptions,
                verify: s_verification with { ILVerifyMessage = ilVerifyMessage + Environment.NewLine + s_verification.ILVerifyMessage },
                targetFramework: targetFramework,
                expectedOutput: expectedOutput);

        // Only used to diagnose test verification failures (rename CompileAndVerify to CompileAndVerifyFails and rerun).
        public CompilationVerifier CompileAndVerifyFails(string source, string? ilVerifyMessage = null, string? expectedOutput = null)
            => CompileAndVerify(
                source,
                options: (expectedOutput != null) ? TestOptions.UnsafeDebugExe : TestOptions.UnsafeDebugDll,
                emitOptions: s_emitOptions,
                verify: Verification.Fails,
                targetFramework: s_targetFramework,
                expectedOutput: null);

        private static void AssertNotInstrumented(CompilationVerifier verifier, string qualifiedMethodName)
            => AssertInstrumented(verifier, qualifiedMethodName, expected: false);

        private static void AssertInstrumented(CompilationVerifier verifier, string qualifiedMethodName, bool expected = true)
        {
            var il = verifier.VisualizeIL(qualifiedMethodName);
            var isInstrumented = il.Contains(TrackerTypeName);

            AssertEx.AreEqual(expected, isInstrumented,
                $"Method '{qualifiedMethodName}' should {(expected ? "be" : "not be")} instrumented. Actual IL:{Environment.NewLine}{il}");
        }

        [Fact]
        public void Composition_AllInstrumentations()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] p)
    {
        string[] a = p = null;
        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
}
") + CSharpInstrumentationChecker.InstrumentationHelperSource;

            var verifier = CompileAndVerify(
                source,
                options: TestOptions.UnsafeDebugExe,
                emitOptions: GetEmitOptions(
                    InstrumentationKindExtensions.LocalStateTracing,
                    InstrumentationKind.TestCoverage,
                    InstrumentationKind.ModuleCancellation,
                    InstrumentationKind.StackOverflowProbing),
                verify: s_verification with
                {
                    ILVerifyMessage = """
                    [LogMethodEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x19 }
                    [LogLambdaEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x19 }
                    [LogStateMachineMethodEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x18 }
                    [LogStateMachineLambdaEntry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x18 }
                    [Entry]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xcc }
                    [MemoryToString]: Unmanaged pointers are not a verifiable type. { Offset = 0x15 }
                    [LogLocalStore]: Unmanaged pointers are not a verifiable type. { Offset = 0x11 }
                    [LogLocalStoreUnmanaged]: Unmanaged pointers are not a verifiable type. { Offset = 0x11 }
                    [LogParameterStore]: Unmanaged pointers are not a verifiable type. { Offset = 0x11 }
                    [LogParameterStoreUnmanaged]: Unmanaged pointers are not a verifiable type. { Offset = 0x11 }
                    [CreatePayload]: Expected numeric type on the stack. { Offset = 0x1f, Found = address of '[System.Runtime]System.Guid' }
                    [CreatePayload]: Expected numeric type on the stack. { Offset = 0x1f, Found = address of '[System.Runtime]System.Guid' }
                    """
                },
                targetFramework: s_targetFramework);

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      144 (0x90)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                string[] V_1, //a
                bool[] V_2)
  // sequence point: <hidden>
  IL_0000:  ldsflda    ""System.Threading.CancellationToken <PrivateImplementationDetails>.ModuleCancellationToken""
  IL_0005:  call       ""void System.Threading.CancellationToken.ThrowIfCancellationRequested()""
  IL_000a:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack()""
  IL_000f:  nop
  IL_0010:  ldtoken    ""void C.Main(string[])""
  IL_0015:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_001a:  stloc.0
  .try
  {
    // sequence point: {
    IL_001b:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
    IL_0020:  ldtoken    ""void C.Main(string[])""
    IL_0025:  ldelem.ref
    IL_0026:  stloc.2
    IL_0027:  ldloc.2
    IL_0028:  brtrue.s   IL_004f
    IL_002a:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
    IL_002f:  ldtoken    ""void C.Main(string[])""
    IL_0034:  ldtoken    Source Document 0
    IL_0039:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
    IL_003e:  ldtoken    ""void C.Main(string[])""
    IL_0043:  ldelema    ""bool[]""
    IL_0048:  ldc.i4.3
    IL_0049:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
    IL_004e:  stloc.2
    IL_004f:  ldloc.2
    IL_0050:  ldc.i4.0
    IL_0051:  ldc.i4.1
    IL_0052:  stelem.i1
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.0
    IL_0057:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_005c:  nop
    // sequence point: string[] a = p = null;
    IL_005d:  ldloc.2
    IL_005e:  ldc.i4.1
    IL_005f:  ldc.i4.1
    IL_0060:  stelem.i1
    IL_0061:  ldloca.s   V_0
    IL_0063:  ldloca.s   V_0
    IL_0065:  ldnull
    IL_0066:  dup
    IL_0067:  starg.s    V_0
    IL_0069:  ldc.i4.0
    IL_006a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_006f:  nop
    IL_0070:  ldarg.0
    IL_0071:  dup
    IL_0072:  stloc.1
    IL_0073:  ldc.i4.1
    IL_0074:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_0079:  nop
    // sequence point: Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    IL_007a:  ldloc.2
    IL_007b:  ldc.i4.2
    IL_007c:  ldc.i4.1
    IL_007d:  stelem.i1
    IL_007e:  call       ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload()""
    IL_0083:  nop
    // sequence point: }
    IL_0084:  leave.s    IL_008f
  }
  finally
  {
    // sequence point: <hidden>
    IL_0086:  ldloca.s   V_0
    IL_0088:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_008d:  nop
    IL_008e:  endfinally
  }
  // sequence point: }
  IL_008f:  ret
}
");
        }

        [Fact]
        public void Composition_LocalStateTracing_TestCoverage_LocallySuppressed()
        {
            var source = WithHelpers(@"
class C
{
    static void Main()
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        void F()
        {
            int a = 1;    
        }

        F();

        Microsoft.CodeAnalysis.Runtime.Instrumentation.FlushPayload();
    }
}
") + CSharpInstrumentationChecker.InstrumentationHelperSource;

            var verifier = CompileAndVerify(
                source,
                options: TestOptions.UnsafeDebugExe,
                emitOptions: GetEmitOptions(InstrumentationKindExtensions.LocalStateTracing, InstrumentationKind.TestCoverage),
                verify: s_verification with
                {
                    ILVerifyMessage = s_verification.ILVerifyMessage + Environment.NewLine + """
                    [CreatePayload]: Expected numeric type on the stack. { Offset = 0xf, Found = address of '[System.Runtime]System.Guid' }
                    [CreatePayload]: Expected numeric type on the stack. { Offset = 0xf, Found = address of '[System.Runtime]System.Guid' }
                    """
                },
                targetFramework: s_targetFramework);

            verifier.VerifyMethodBody("C.<Main>g__F|0_0", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1) //a
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""void C.<Main>g__F|0_0()""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: {
    IL_0010:  nop
    // sequence point: int a = 1;
    IL_0011:  ldloca.s   V_0
    IL_0013:  ldc.i4.1
    IL_0014:  dup
    IL_0015:  stloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_001c:  nop
    // sequence point: }
    IL_001d:  leave.s    IL_0028
  }
  finally
  {
    // sequence point: <hidden>
    IL_001f:  ldloca.s   V_0
    IL_0021:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0026:  nop
    IL_0027:  endfinally
  }
  // sequence point: }
  IL_0028:  ret
}
");
        }

        [Fact]
        public void EmitDifference()
        {
            var source = WithHelpers(@"
class C
{
    static void F()
    {
        int a = 0xFFFF;

        static void LF() { int b = 0xEEEE; }

        LF();
    }
}
");

            var compilation0 = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, targetFramework: s_targetFramework);
            var compilation1 = compilation0.WithSource(source);

            var f0 = (IMethodSymbol)compilation0.GetMember("C.F").GetPublicSymbol();
            var f1 = (IMethodSymbol)compilation1.GetMember("C.F").GetPublicSymbol();

            using var md0 = ModuleMetadata.CreateFromImage(compilation0.EmitToArray());
            var generation0 = EmitBaseline.CreateInitialBaseline(compilation0, md0, debugInformationProvider: _ => default, localSignatureProvider: _ => default, hasPortableDebugInformation: true);

            var diff = compilation1.EmitDifference(
                generation0,
                edits: ImmutableArray.Create(
                    new SemanticEdit(f0, f1, ImmutableArray.Create(InstrumentationKindExtensions.LocalStateTracing))));

            diff.VerifyIL(@"
{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldc.i4     0x3
  IL_0005:  call       0x06000007
  IL_000a:  stloc.0
  IL_000b:  nop
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4     0xffff
  IL_0013:  dup
  IL_0014:  stloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  call       0x06000018
  IL_001b:  nop
  IL_001c:  nop
  IL_001d:  call       0x06000031
  IL_0022:  nop
  IL_0023:  leave.s    IL_002e
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       0x0600000B
  IL_002c:  nop
  IL_002d:  endfinally
  IL_002e:  ret
}
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  ldc.i4     0x3
  IL_0005:  ldc.i4     0x31
  IL_000a:  call       0x06000008
  IL_000f:  stloc.0
  IL_0010:  nop
  IL_0011:  ldloca.s   V_0
  IL_0013:  ldc.i4     0xeeee
  IL_0018:  dup
  IL_0019:  stloc.1
  IL_001a:  ldc.i4.1
  IL_001b:  call       0x06000018
  IL_0020:  nop
  IL_0021:  leave.s    IL_002c
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       0x0600000B
  IL_002a:  nop
  IL_002b:  endfinally
  IL_002c:  ret
}
");
        }

        [Fact]
        public void HelpersNotInstrumented()
        {
            var source = WithHelpers(@"
namespace Microsoft.CodeAnalysis.Runtime
{
    partial struct LocalStoreTracker
    {
        public class NestedHelpers
        {
            void F(int a) => a = 1;

            public class SuperNestedHelpers
            {
                void F(int a) => a = 1;
            }
        }
    }
}
");
            var verifier = CompileAndVerify(source);
            foreach (var entry in verifier.TestData.Methods)
            {
                string actualIL = verifier.VisualizeIL(entry.Value);
                Assert.False(actualIL.Contains(TrackerTypeName + ".Log"));
            }
        }

        [Fact]
        public void HelpersMissing()
        {
            var source = @"
using System;

class C
{
    DateTime F(int p, ref int q)
    {
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }
} ";

            var compilation = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, targetFramework: s_targetFramework);

            compilation.VerifyEmitDiagnostics(s_emitOptions,
                // (4,1): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry'
                // class C
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"class C
{
    DateTime F(int p, ref int q)
    {
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }
}").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogMethodEntry").WithLocation(4, 1),
                // (4,1): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn'
                // class C
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"class C
{
    DateTime F(int p, ref int q)
    {
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }
}").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogReturn").WithLocation(4, 1),
                // (7,5): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogParameterStore").WithLocation(7, 5),
                // (7,5): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogParameterStore").WithLocation(7, 5),
                // (7,5): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogMethodEntry").WithLocation(7, 5),
                // (7,5): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn'
                //     {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"{
        var a = DateTime.Now;
        p = 1;
        q = 1;

        ref var r = ref a;
        ref var s = ref q;

        return a;
    }").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogReturn").WithLocation(7, 5),
                // (8,13): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreUnmanaged'
                //         var a = DateTime.Now;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "a = DateTime.Now").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogLocalStoreUnmanaged").WithLocation(8, 13),
                // (9,9): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore'
                //         p = 1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "p = 1").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogParameterStore").WithLocation(9, 9),
                // (10,9): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore'
                //         q = 1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "q = 1").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogParameterStore").WithLocation(10, 9),
                // (12,17): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreLocalAlias'
                //         ref var r = ref a;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "r = ref a").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogLocalStoreLocalAlias").WithLocation(12, 17),
                // (13,17): error CS0656: Missing compiler required member 'Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreParameterAlias'
                //         ref var s = ref q;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "s = ref q").WithArguments("Microsoft.CodeAnalysis.Runtime.LocalStoreTracker", "LogLocalStoreParameterAlias").WithLocation(13, 17));
        }

        [Fact]
        public void ObjectInitializers_NotInstrumented()
        {
            var source = WithHelpers(@"
class C
{
    int X;

    public C F() => new C() { X = 1 };
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C V_1)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: new C() { X = 1 }
    IL_000b:  newobj     ""C..ctor()""
    IL_0010:  dup
    IL_0011:  ldc.i4.1
    IL_0012:  stfld      ""int C.X""
    IL_0017:  stloc.1
    IL_0018:  leave.s    IL_0023
  }
  finally
  {
    // sequence point: <hidden>
    IL_001a:  ldloca.s   V_0
    IL_001c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0021:  nop
    IL_0022:  endfinally
  }
  // sequence point: <hidden>
  IL_0023:  ldloc.1
  IL_0024:  ret
}
");
        }

        [Fact]
        public void WithExpressions_NotInstrumented()
        {
            var source = WithHelpers(@"
record class C(int X)
{
    public C F() => new C(0) with { X = 1 };
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C V_1)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: new C(0) with { X = 1 }
    IL_000b:  ldc.i4.0
    IL_000c:  newobj     ""C..ctor(int)""
    IL_0011:  callvirt   ""C C.<Clone>$()""
    IL_0016:  dup
    IL_0017:  ldc.i4.1
    IL_0018:  callvirt   ""void C.X.init""
    IL_001d:  nop
    IL_001e:  stloc.1
    IL_001f:  leave.s    IL_002a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0021:  ldloca.s   V_0
    IL_0023:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0028:  nop
    IL_0029:  endfinally
  }
  // sequence point: <hidden>
  IL_002a:  ldloc.1
  IL_002b:  ret
}
");
        }

        [Fact]
        public void MemberAssignment_NotInstrumented()
        {
            var source = WithHelpers(@"
class C
{
    int X;
    int P { get => 1; set { X = value; } }

    public void F() { X = 1; P = 2; }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: X = 1;
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.1
    IL_000e:  stfld      ""int C.X""
    // sequence point: P = 2;
    IL_0013:  ldarg.0
    IL_0014:  ldc.i4.2
    IL_0015:  call       ""void C.P.set""
    IL_001a:  nop
    // sequence point: }
    IL_001b:  leave.s    IL_0026
  }
  finally
  {
    // sequence point: <hidden>
    IL_001d:  ldloca.s   V_0
    IL_001f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0024:  nop
    IL_0025:  endfinally
  }
  // sequence point: }
  IL_0026:  ret
}
");
        }

        [Fact]
        public void AutoPropertyAccessors_NotInstrumented()
        {
            var source = WithHelpers(@"
class C
{
    int P { get; set; }
}
");
            var verifier = CompileAndVerify(source);

            AssertNotInstrumented(verifier, "C.P.get");
            AssertNotInstrumented(verifier, "C.P.set");
        }

        [Fact]
        public void EventAccessors_NotInstrumented()
        {
            var source = WithHelpers(@"
using System;

class C
{
    event Action E;
}
");

            var verifier = CompileAndVerify(source);

            AssertNotInstrumented(verifier, "C.E.add");
            AssertNotInstrumented(verifier, "C.E.remove");
        }

        [Fact]
        public void SimpleLocalsAndParameters()
        {
            var source = WithHelpers(@"
class C
{
    public void F(int p, int q)
    {
        int x = 1;
        p = x = 2;
        for (int i = 0; i < 10; i++)
        {
            q = x += 2;
        }
    }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size      150 (0x96)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //x
                int V_2, //i
                int V_3,
                bool V_4)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F(int, int)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.1
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0014:  nop
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldarg.2
    IL_0018:  ldc.i4.1
    IL_0019:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_001e:  nop
    // sequence point: int x = 1;
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldc.i4.1
    IL_0022:  dup
    IL_0023:  stloc.1
    IL_0024:  ldc.i4.1
    IL_0025:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_002a:  nop
    // sequence point: p = x = 2;
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldloca.s   V_0
    IL_002f:  ldc.i4.2
    IL_0030:  dup
    IL_0031:  stloc.1
    IL_0032:  ldc.i4.1
    IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0038:  nop
    IL_0039:  ldloc.1
    IL_003a:  dup
    IL_003b:  starg.s    V_1
    IL_003d:  ldc.i4.0
    IL_003e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0043:  nop
    // sequence point: int i = 0
    IL_0044:  ldloca.s   V_0
    IL_0046:  ldc.i4.0
    IL_0047:  dup
    IL_0048:  stloc.2
    IL_0049:  ldc.i4.2
    IL_004a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_004f:  nop
    // sequence point: <hidden>
    IL_0050:  br.s       IL_007f
    // sequence point: {
    IL_0052:  nop
    // sequence point: q = x += 2;
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldloca.s   V_0
    IL_0057:  ldloc.1
    IL_0058:  ldc.i4.2
    IL_0059:  add
    IL_005a:  dup
    IL_005b:  stloc.1
    IL_005c:  ldc.i4.1
    IL_005d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0062:  nop
    IL_0063:  ldloc.1
    IL_0064:  dup
    IL_0065:  starg.s    V_2
    IL_0067:  ldc.i4.1
    IL_0068:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_006d:  nop
    // sequence point: }
    IL_006e:  nop
    // sequence point: i++
    IL_006f:  ldloc.2
    IL_0070:  stloc.3
    IL_0071:  ldloca.s   V_0
    IL_0073:  ldloc.3
    IL_0074:  ldc.i4.1
    IL_0075:  add
    IL_0076:  dup
    IL_0077:  stloc.2
    IL_0078:  ldc.i4.2
    IL_0079:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_007e:  nop
    // sequence point: i < 10
    IL_007f:  ldloc.2
    IL_0080:  ldc.i4.s   10
    IL_0082:  clt
    IL_0084:  stloc.s    V_4
    // sequence point: <hidden>
    IL_0086:  ldloc.s    V_4
    IL_0088:  brtrue.s   IL_0052
    // sequence point: }
    IL_008a:  leave.s    IL_0095
  }
  finally
  {
    // sequence point: <hidden>
    IL_008c:  ldloca.s   V_0
    IL_008e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0093:  nop
    IL_0094:  endfinally
  }
  // sequence point: }
  IL_0095:  ret
}
");
        }

        [Theory]
        [InlineData("I", "object", "c", "C")]
        [InlineData("C", "object", "c", "C")]
        [InlineData("object", "object", "c", "C")]
        [InlineData("string", "string", "\"str\"", "str")]
        [InlineData("bool", "bool", "true", "True")]
        [InlineData("byte", "byte", "123", "123")]
        [InlineData("ushort", "ushort", "15", "15")]
        [InlineData("char", "ushort", "'c'", "99")]
        [InlineData("int", "uint", "-58", "4294967238")]
        [InlineData("uint", "uint", "74", "74")]
        [InlineData("long", "ulong", "942254", "942254")]
        [InlineData("ulong", "ulong", "1321321321321", "1321321321321")]
        [InlineData("float", "float", "1.2345F", "1.2345")]
        [InlineData("double", "double", "1.245656", "1.245656")]
        [InlineData("decimal", "decimal", "20.34M", "20.34")]
        [InlineData("void*", "void*", "(void*)100000", "100000", false)]
        [InlineData("byte*", "void*", "(byte*)100000", "100000", false)]
        [InlineData("delegate*<int>", "void*", "(delegate*<int>)100000", "100000")]
        public void SpecialTypes_NoConv(string typeName, string targetType, string valueSource, string value, bool verificationPasses = true)
        {
            var source = WithHelpers($$"""
interface I {}

unsafe class C : I
{
    static readonly C c = new();
    static readonly {{typeName}} s = {{valueSource}};

    static void F({{typeName}} p)
    {
        var x = p = s;
    }

    static void Main() => F(s);
}
""");
            var verifier = CompileAndVerify(
                source,
                ilVerifyMessage: verificationPasses ? "" : @"
[F]: Unmanaged pointers are not a verifiable type. { Offset = 0xd }
[F]: Unmanaged pointers are not a verifiable type. { Offset = 0x28 }
",
                expectedOutput: $@"
Main: Entered
.cctor: Entered
.ctor: Entered
.ctor: Returned
.cctor: Returned
F: Entered
F: P'p'[0] = {value}
F: P'p'[0] = {value}
F: L1 = {value}
F: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.F", $@"
{{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                {typeName} V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F({typeName})""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0014:  nop
    // sequence point: var x = p = s;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldloca.s   V_0
    IL_0019:  ldsfld     ""{typeName} C.s""
    IL_001e:  dup
    IL_001f:  starg.s    V_0
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0027:  nop
    IL_0028:  ldarg.0
    IL_0029:  dup
    IL_002a:  stloc.1
    IL_002b:  ldc.i4.1
    IL_002c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore({targetType}, int)""
    IL_0031:  nop
    // sequence point: }}
    IL_0032:  leave.s    IL_003d
  }}
  finally
  {{
    // sequence point: <hidden>
    IL_0034:  ldloca.s   V_0
    IL_0036:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003b:  nop
    IL_003c:  endfinally
  }}
  // sequence point: }}
  IL_003d:  ret
}}
");
        }

        [Theory]
        [InlineData("sbyte", "byte", "conv.u1", "-1", "255")]
        [InlineData("short", "ushort", "conv.u2", "-1", "65535")]
        public void SpecialTypes_ConvU(string typeName, string targetType, string conversion, string valueSource, string value)
        {
            var source = WithHelpers($$"""
class C
{
    static readonly {{typeName}} s = {{valueSource}};

    static void F({{typeName}} p)
    {
        var x = p = s;
    }

    static void Main() => F(s);
}
""");
            var verifier = CompileAndVerify(source, expectedOutput: $@"
Main: Entered
.cctor: Entered
.cctor: Returned
F: Entered
F: P'p'[0] = {value}
F: P'p'[0] = {value}
F: L1 = {value}
F: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.F", $@"
{{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                {typeName} V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F({typeName})""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  {conversion}
    IL_000f:  ldc.i4.0
    IL_0010:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0015:  nop
    // sequence point: var x = p = s;
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldsfld     ""{typeName} C.s""
    IL_001f:  dup
    IL_0020:  starg.s    V_0
    IL_0022:  {conversion}
    IL_0023:  ldc.i4.0
    IL_0024:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0029:  nop
    IL_002a:  ldarg.0
    IL_002b:  dup
    IL_002c:  stloc.1
    IL_002d:  {conversion}
    IL_002e:  ldc.i4.1
    IL_002f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore({targetType}, int)""
    IL_0034:  nop
    // sequence point: }}
    IL_0035:  leave.s    IL_0040
  }}
  finally
  {{
    // sequence point: <hidden>
    IL_0037:  ldloca.s   V_0
    IL_0039:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003e:  nop
    IL_003f:  endfinally
  }}
  // sequence point: }}
  IL_0040:  ret
}}
");
        }

        [Theory]
        [InlineData("byte", "byte")]
        [InlineData("ushort", "ushort")]
        [InlineData("int", "uint")]
        [InlineData("uint", "uint")]
        [InlineData("long", "ulong")]
        [InlineData("ulong", "ulong")]
        public void Enums_NoConv(string typeName, string targetType)
        {
            var source = WithHelpers($$"""
enum E : {{typeName}}
{
}

class C
{
    private static readonly E s;

    public void F(E p)
    {
        var x = p = s;
    }
}
""");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", $@"
{{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                E V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F(E)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.1
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0014:  nop
    // sequence point: var x = p = s;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldloca.s   V_0
    IL_0019:  ldsfld     ""E C.s""
    IL_001e:  dup
    IL_001f:  starg.s    V_1
    IL_0021:  ldc.i4.0
    IL_0022:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0027:  nop
    IL_0028:  ldarg.1
    IL_0029:  dup
    IL_002a:  stloc.1
    IL_002b:  ldc.i4.1
    IL_002c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore({targetType}, int)""
    IL_0031:  nop
    // sequence point: }}
    IL_0032:  leave.s    IL_003d
  }}
  finally
  {{
    // sequence point: <hidden>
    IL_0034:  ldloca.s   V_0
    IL_0036:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003b:  nop
    IL_003c:  endfinally
  }}
  // sequence point: }}
  IL_003d:  ret
}}
");
        }

        [Theory]
        [InlineData("sbyte", "byte", "conv.u1")]
        [InlineData("short", "ushort", "conv.u2")]
        public void Enums_ConvU(string typeName, string targetType, string conversion)
        {
            var source = WithHelpers($$"""
enum E : {{typeName}}
{
}

class C
{
    private static readonly E s;

    public void F(E p)
    {
        var x = p = s;
    }
}
""");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", $@"
{{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                E V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F(E)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.1
    IL_000e:  {conversion}
    IL_000f:  ldc.i4.0
    IL_0010:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0015:  nop
    // sequence point: var x = p = s;
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldsfld     ""E C.s""
    IL_001f:  dup
    IL_0020:  starg.s    V_1
    IL_0022:  {conversion}
    IL_0023:  ldc.i4.0
    IL_0024:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore({targetType}, int)""
    IL_0029:  nop
    IL_002a:  ldarg.1
    IL_002b:  dup
    IL_002c:  stloc.1
    IL_002d:  {conversion}
    IL_002e:  ldc.i4.1
    IL_002f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore({targetType}, int)""
    IL_0034:  nop
    // sequence point: }}
    IL_0035:  leave.s    IL_0040
  }}
  finally
  {{
    // sequence point: <hidden>
    IL_0037:  ldloca.s   V_0
    IL_0039:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003e:  nop
    IL_003f:  endfinally
  }}
  // sequence point: }}
  IL_0040:  ret
}}
");
        }

#pragma warning disable
        private struct S1 { }
        private struct S2<T> where T : struct { T t; }
        private struct S3 { System.DateTime X; System.Guid Y; decimal Z; unsafe void* P; }
#pragma warning restore

        public static IEnumerable<object[]> UnmanagedStruct_TestData
        {
            get
            {
                yield return new object[] { "", "nint", "IntPtr", Unsafe.SizeOf<nint>() };
                yield return new object[] { "", "nuint", "UIntPtr", Unsafe.SizeOf<nuint>() };
                yield return new object[] { "", "System.Int128", "'[System.Runtime]System.Int128'", Unsafe.SizeOf<Int128>() };
                yield return new object[] { "", "System.Guid", "'[System.Runtime]System.Guid'", Unsafe.SizeOf<Guid>() };
                yield return new object[] { "", "System.ValueTuple<int, bool>", "'[System.Runtime]System.ValueTuple`2<int32,bool>'", Unsafe.SizeOf<(int, bool)>() };
                yield return new object[] { "struct S { }", "S", "'S'", Unsafe.SizeOf<S1>() };
                yield return new object[] { "struct S<T> where T : struct { T t; }", "S<int>", "'S`1<int32>'", Unsafe.SizeOf<S2<int>>() };
                yield return new object[] { "struct S { System.DateTime X; System.Guid Y; decimal Z; unsafe void* P; }", "S", "'S'", Unsafe.SizeOf<S3>() };
                yield return new object[] { "", "System.ValueTuple<int?, bool?>", "'[System.Runtime]System.ValueTuple`2<System.Nullable`1<int32>,System.Nullable`1<bool>>'", Unsafe.SizeOf<(int?, bool?)>() };
                yield return new object[] { "", "int?", "'[System.Runtime]System.Nullable`1<int32>'", Unsafe.SizeOf<int?>() };
            }
        }

        [Theory]
        [MemberData(nameof(UnmanagedStruct_TestData))]
        public void UnmanagedStruct(string definition, string typeName, string ilTypeName, int expectedSize)
        {
            var expectedValue = $"<{string.Join("-", Enumerable.Repeat("00", expectedSize))}>";

            var source = WithHelpers($$"""
C.F(default);

{{definition}}

class C
{
    static readonly {{typeName}} s;

    public static void F({{typeName}} p)
    {
        var x = p = s;
    }
}
""");

            var verifier = CompileAndVerify(
                source,
                ilVerifyMessage: $@"
[F]: Expected numeric type on the stack. {{ Offset = 0xf, Found = address of {ilTypeName} }}
",
                expectedOutput: $@"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: P'p'[0] = {expectedValue}
F: P'p'[0] = {expectedValue}
F: L1 = {expectedValue}
F: Returned
<Main>$: Returned
");

            verifier.VerifyMethodBody("C.F", $@"
{{
      // Code size       86 (0x56)
      .maxstack  5
      .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                    {typeName} V_1) //x
      // sequence point: <hidden>
      IL_0000:  ldtoken    ""void C.F({typeName})""
      IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
      IL_000a:  stloc.0
      .try
      {{
        // sequence point: {{
        IL_000b:  ldloca.s   V_0
        IL_000d:  ldarga.s   V_0
        IL_000f:  conv.u
        IL_0010:  sizeof     ""{typeName}""
        IL_0016:  ldc.i4.0
        IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStoreUnmanaged(void*, int, int)""
        IL_001c:  nop
        // sequence point: var x = p = s;
        IL_001d:  ldloca.s   V_0
        IL_001f:  ldloca.s   V_0
        IL_0021:  ldsfld     ""{typeName} C.s""
        IL_0026:  starg.s    V_0
        IL_0028:  ldarga.s   V_0
        IL_002a:  conv.u
        IL_002b:  sizeof     ""{typeName}""
        IL_0031:  ldc.i4.0
        IL_0032:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStoreUnmanaged(void*, int, int)""
        IL_0037:  nop
        IL_0038:  ldarg.0
        IL_0039:  stloc.1
        IL_003a:  ldloca.s   V_1
        IL_003c:  conv.u
        IL_003d:  sizeof     ""{typeName}""
        IL_0043:  ldc.i4.1
        IL_0044:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreUnmanaged(void*, int, int)""
        IL_0049:  nop
        // sequence point: }}
        IL_004a:  leave.s    IL_0055
      }}
      finally
      {{
        // sequence point: <hidden>
        IL_004c:  ldloca.s   V_0
        IL_004e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
        IL_0053:  nop
        IL_0054:  endfinally
      }}
      // sequence point: }}
      IL_0055:  ret
    }}
", ilFormat: SymbolDisplayFormat.ILVisualizationFormat.RemoveCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType));
        }

        [Theory]
        [InlineData("", "System.ValueTuple<int, string>", "(0, )")]
        [InlineData("struct S { string A; }", "S", "S")]
        [InlineData("struct S { string A; }", "S?", "")]
        public void ManagedStruct(string definition, string typeName, string value)
        {
            var source = WithHelpers($$"""
C.F(default);

{{definition}}

class C
{
    private static readonly {{typeName}} s;

    public static void F({{typeName}} p)
    {
        var x = p = s;
    }
}
""");
            var verifier = CompileAndVerify(source, expectedOutput: $@"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: P'p'[0] = {value}
F: P'p'[0] = {value}
F: L1 = {value}
F: Returned
<Main>$: Returned
");

            // TODO: why is the stloc+ldloc.a of V_2 emitted? https://github.com/dotnet/roslyn/issues/66810
            // IL_0045: stloc.2
            // IL_0046: ldloca.s V_2

            verifier.VerifyMethodBody("C.F", $@"
{{
  // Code size      102 (0x66)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                {typeName} V_1, //x
                {typeName} V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F({typeName})""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarga.s   V_0
    IL_000f:  constrained. ""{typeName}""
    IL_0015:  callvirt   ""string object.ToString()""
    IL_001a:  ldc.i4.0
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(string, int)""
    IL_0020:  nop
    // sequence point: var x = p = s;
    IL_0021:  ldloca.s   V_0
    IL_0023:  ldloca.s   V_0
    IL_0025:  ldsfld     ""{typeName} C.s""
    IL_002a:  dup
    IL_002b:  starg.s    V_0
    IL_002d:  stloc.2
    IL_002e:  ldloca.s   V_2
    IL_0030:  constrained. ""{typeName}""
    IL_0036:  callvirt   ""string object.ToString()""
    IL_003b:  ldc.i4.0
    IL_003c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(string, int)""
    IL_0041:  nop
    IL_0042:  ldarg.0
    IL_0043:  dup
    IL_0044:  stloc.1
    IL_0045:  stloc.2
    IL_0046:  ldloca.s   V_2
    IL_0048:  constrained. ""{typeName}""
    IL_004e:  callvirt   ""string object.ToString()""
    IL_0053:  ldc.i4.1
    IL_0054:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
    IL_0059:  nop
    // sequence point: }}
    IL_005a:  leave.s    IL_0065
  }}
  finally
  {{
    // sequence point: <hidden>
    IL_005c:  ldloca.s   V_0
    IL_005e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0063:  nop
    IL_0064:  endfinally
  }}
  // sequence point: }}
  IL_0065:  ret
}}
");
        }

        [Fact]
        public void RefStructWithRefFieldAndNoToStringOverride()
        {
            var source = WithHelpers("""
S.F(new S());

ref struct S
{
    ref int X;

    public static void F(S p)
    {
        int a = 1;
        var x = p = new S();
    }
}
""");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: L1 = 1
F: Returned
<Main>$: Returned
");

            // writes to x and p are not logged since we can't invoke ToString()
            verifier.VerifyMethodBody("S.F", @"
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                S V_2) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void S.F(S)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: var x = p = new S();
    IL_0018:  ldarga.s   V_0
    IL_001a:  initobj    ""S""
    IL_0020:  ldarg.0
    IL_0021:  stloc.2
    // sequence point: }
    IL_0022:  leave.s    IL_002d
  }
  finally
  {
    // sequence point: <hidden>
    IL_0024:  ldloca.s   V_0
    IL_0026:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_002b:  nop
    IL_002c:  endfinally
  }
  // sequence point: }
  IL_002d:  ret
}
");
        }

        [Fact]
        public void RefStructWithRefFieldAndToStringOverride()
        {
            var source = WithHelpers("""
S.F(new S());

ref struct S
{
    ref int X;

    public static void F(S p)
    {
        var x = p = new S();
    }

    public override string ToString() => "str";
}
""");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
ToString: Entered
ToString: Returned
F: P'p'[0] = str
ToString: Entered
ToString: Returned
F: P'p'[0] = str
ToString: Entered
ToString: Returned
F: L1 = str
F: Returned
<Main>$: Returned
");

            verifier.VerifyMethodBody("S.F", @"
 {
  // Code size      103 (0x67)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                S V_1, //x
                S V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void S.F(S)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarga.s   V_0
    IL_000f:  constrained. ""S""
    IL_0015:  callvirt   ""string object.ToString()""
    IL_001a:  ldc.i4.0
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(string, int)""
    IL_0020:  nop
    // sequence point: var x = p = new S();
    IL_0021:  ldloca.s   V_0
    IL_0023:  ldloca.s   V_0
    IL_0025:  ldarga.s   V_0
    IL_0027:  initobj    ""S""
    IL_002d:  ldarg.0
    IL_002e:  stloc.2
    IL_002f:  ldloca.s   V_2
    IL_0031:  constrained. ""S""
    IL_0037:  callvirt   ""string object.ToString()""
    IL_003c:  ldc.i4.0
    IL_003d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(string, int)""
    IL_0042:  nop
    IL_0043:  ldarg.0
    IL_0044:  dup
    IL_0045:  stloc.1
    IL_0046:  stloc.2
    IL_0047:  ldloca.s   V_2
    IL_0049:  constrained. ""S""
    IL_004f:  callvirt   ""string object.ToString()""
    IL_0054:  ldc.i4.1
    IL_0055:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
    IL_005a:  nop
    // sequence point: }
    IL_005b:  leave.s    IL_0066
  }
  finally
  {
    // sequence point: <hidden>
    IL_005d:  ldloca.s   V_0
    IL_005f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0064:  nop
    IL_0065:  endfinally
  }
  // sequence point: }
  IL_0066:  ret
}");
        }

        [Fact]
        public void RefStructTypeParameter()
        {
            var source = WithHelpers("""
S.F(new S());

ref struct S
{
    ref int X;

    public static void F<T>(T p)
        where T : struct, allows ref struct
    {
        int a = 1;
        var x = p = default(T);
    }
}
""");
            var verifier = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net90,
                expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: L1 = 1
F: Returned
<Main>$: Returned
");

            // writes to x and p are not logged since we can't invoke ToString()
            verifier.VerifyMethodBody("S.F<T>(T)", @"
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
            int V_1, //a
            T V_2) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void S.F<T>(T)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: var x = p = default(T);
    IL_0018:  ldarga.s   V_0
    IL_001a:  initobj    ""T""
    IL_0020:  ldarg.0
    IL_0021:  stloc.2
    // sequence point: }
    IL_0022:  leave.s    IL_002d
  }
  finally
  {
    // sequence point: <hidden>
    IL_0024:  ldloca.s   V_0
    IL_0026:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_002b:  nop
    IL_002c:  endfinally
  }
  // sequence point: }
  IL_002d:  ret
}
");
        }

        [Fact]
        public void UnmanagedRefStruct()
        {
            var source = WithHelpers($$"""
C.F(default);

ref struct S { int X; }

class C
{
    public static void F(S p)
    {
        var x = p = new S();
    }
}
""");
            var verifier = CompileAndVerify(
                source,
                expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: P'p'[0] = <00-00-00-00>
F: P'p'[0] = <00-00-00-00>
F: L1 = <00-00-00-00>
F: Returned
<Main>$: Returned
                ",
                ilVerifyMessage: @"
[F]: Expected numeric type on the stack. { Offset = 0xf, Found = address of 'S' }
");

            verifier.VerifyMethodBody("C.F", $@"
{{
    // Code size       87 (0x57)
    .maxstack  5
    .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                S V_1) //x
    // sequence point: <hidden>
    IL_0000:  ldtoken    ""void C.F(S)""
    IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
    IL_000a:  stloc.0
    .try
    {{
    // sequence point: {{
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarga.s   V_0
    IL_000f:  conv.u
    IL_0010:  sizeof     ""S""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStoreUnmanaged(void*, int, int)""
    IL_001c:  nop
    // sequence point: var x = p = new S();
    IL_001d:  ldloca.s   V_0
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldarga.s   V_0
    IL_0023:  initobj    ""S""
    IL_0029:  ldarga.s   V_0
    IL_002b:  conv.u
    IL_002c:  sizeof     ""S""
    IL_0032:  ldc.i4.0
    IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStoreUnmanaged(void*, int, int)""
    IL_0038:  nop
    IL_0039:  ldarg.0
    IL_003a:  stloc.1
    IL_003b:  ldloca.s   V_1
    IL_003d:  conv.u
    IL_003e:  sizeof     ""S""
    IL_0044:  ldc.i4.1
    IL_0045:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreUnmanaged(void*, int, int)""
    IL_004a:  nop
    // sequence point: }}
    IL_004b:  leave.s    IL_0056
    }}
    finally
    {{
    // sequence point: <hidden>
    IL_004d:  ldloca.s   V_0
    IL_004f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0054:  nop
    IL_0055:  endfinally
    }}
    // sequence point: }}
    IL_0056:  ret
}}");
        }

        [Fact]
        public void Dynamic()
        {
            var source = WithHelpers(@"
class C
{
    static void Main()
    {
        dynamic x = 1;
        x++;
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L1 = 1
Main: L1 = 2
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      128 (0x80)
  .maxstack  9
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                object V_1, //x
                object V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: dynamic x = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  box        ""int""
    IL_0014:  dup
    IL_0015:  stloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_001c:  nop
    // sequence point: x++;
    IL_001d:  ldloc.1
    IL_001e:  stloc.2
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__0""
    IL_0026:  brfalse.s  IL_002a
    IL_0028:  br.s       IL_0056
    IL_002a:  ldc.i4.0
    IL_002b:  ldc.i4.s   54
    IL_002d:  ldtoken    ""C""
    IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0037:  ldc.i4.1
    IL_0038:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
    IL_003d:  dup
    IL_003e:  ldc.i4.0
    IL_003f:  ldc.i4.0
    IL_0040:  ldnull
    IL_0041:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
    IL_0046:  stelem.ref
    IL_0047:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
    IL_004c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
    IL_0051:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__0""
    IL_0056:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__0""
    IL_005b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
    IL_0060:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__0""
    IL_0065:  ldloc.2
    IL_0066:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
    IL_006b:  dup
    IL_006c:  stloc.1
    IL_006d:  ldc.i4.1
    IL_006e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_0073:  nop
    // sequence point: }
    IL_0074:  leave.s    IL_007f
  }
  finally
  {
    // sequence point: <hidden>
    IL_0076:  ldloca.s   V_0
    IL_0078:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_007d:  nop
    IL_007e:  endfinally
  }
  // sequence point: }
  IL_007f:  ret
}
");
        }

        [Fact]
        public void ThisAssignment_NotInstrumented()
        {
            var source = WithHelpers($$"""
struct S
{
    void F()
    {
        this = new S();
    }
}
""");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("S.F", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void S.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: this = new S();
    IL_000c:  ldarg.0
    IL_000d:  initobj    ""S""
    // sequence point: }
    IL_0013:  leave.s    IL_001e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0015:  ldloca.s   V_0
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_001c:  nop
    IL_001d:  endfinally
  }
  // sequence point: }
  IL_001e:  ret
}
");
        }

        [Fact]
        public void RefAssignments()
        {
            var source = WithHelpers(@"
class C
{
    static void G(int p1, ref int p2, out int p3, ref int p4)
    {
        int a = 1;
        int b = 2;

        p3 = 3;
        p4 = ref p2;
        
        ref int r1 = ref a;
        ref int r2 = ref p1;
        ref int r3 = ref p2;
        ref int r4 = ref p3;
        ref int r5 = ref r1;

        if (F(ref r1, ref r2, ref r3, ref r4, out r5))
        {
            r1 = r2;
        }
    }

    static bool F(ref int a1, ref int a2, ref int a3, ref int a4, out int a5)
    {
        a5 = 0;
        return true;
    }

    static void Main()
    {
        int a = 1;
        G(1, ref a, out var b, ref a);
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L1 = 1
G: Entered
G: P'p1'[0] = 1
G: P'p2'[1] = 1
G: P'p4'[3] = 1
G: L1 = 1
G: L2 = 2
G: P'p3'[2] = 3
G: P'p4'[3] -> P'p2'[1]
G: L3 -> L1
G: L4 -> P'p1'[0]
G: L5 -> P'p2'[1]
G: L6 -> P'p3'[2]
G: L7 -> L3
F: Entered
F: P'a1'[0] = 1
F: P'a2'[1] = 1
F: P'a3'[2] = 1
F: P'a4'[3] = 3
F: P'a5'[4] = 0
F: Returned
G: L3 = 0
G: L4 = 1
G: L5 = 1
G: L6 = 3
G: L7 = 0
G: L3 = 1
G: Returned
Main: L1 = 1
Main: L2 = 3
Main: L1 = 1
Main: Returned
");

            // TODO: eliminate https://github.com/dotnet/roslyn/issues/66810
            // IL_007b:  ldloc.2
            // IL_007c:  ldind.i4
            // IL_007d:  pop

            verifier.VerifyMethodBody("C.G", @"
 {
  // Code size      297 (0x129)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                int V_2, //b
                int& V_3, //r1
                int& V_4, //r2
                int& V_5, //r3
                int& V_6, //r4
                int& V_7, //r5
                int V_8,
                bool V_9)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.G(int, ref int, out int, ref int)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0014:  nop
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldarg.1
    IL_0018:  ldind.i4
    IL_0019:  ldc.i4.1
    IL_001a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_001f:  nop
    IL_0020:  ldloca.s   V_0
    IL_0022:  ldarg.3
    IL_0023:  ldind.i4
    IL_0024:  ldc.i4.3
    IL_0025:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_002a:  nop
    // sequence point: int a = 1;
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldc.i4.1
    IL_002e:  dup
    IL_002f:  stloc.1
    IL_0030:  ldc.i4.1
    IL_0031:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0036:  nop
    // sequence point: int b = 2;
    IL_0037:  ldloca.s   V_0
    IL_0039:  ldc.i4.2
    IL_003a:  dup
    IL_003b:  stloc.2
    IL_003c:  ldc.i4.2
    IL_003d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0042:  nop
    // sequence point: p3 = 3;
    IL_0043:  ldloca.s   V_0
    IL_0045:  ldarg.2
    IL_0046:  ldc.i4.3
    IL_0047:  dup
    IL_0048:  stloc.s    V_8
    IL_004a:  stind.i4
    IL_004b:  ldloc.s    V_8
    IL_004d:  ldc.i4.2
    IL_004e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0053:  nop
    // sequence point: p4 = ref p2;
    IL_0054:  ldloca.s   V_0
    IL_0056:  ldarg.1
    IL_0057:  starg.s    V_3
    IL_0059:  ldc.i4.1
    IL_005a:  ldc.i4.3
    IL_005b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStoreParameterAlias(int, int)""
    IL_0060:  nop
    // sequence point: ref int r1 = ref a;
    IL_0061:  ldloca.s   V_0
    IL_0063:  ldloca.s   V_1
    IL_0065:  stloc.3
    IL_0066:  ldc.i4.1
    IL_0067:  ldc.i4.3
    IL_0068:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreLocalAlias(int, int)""
    IL_006d:  nop
    IL_006e:  ldloc.3
    IL_006f:  ldind.i4
    IL_0070:  pop
    // sequence point: ref int r2 = ref p1;
    IL_0071:  ldloca.s   V_0
    IL_0073:  ldarga.s   V_0
    IL_0075:  stloc.s    V_4
    IL_0077:  ldc.i4.0
    IL_0078:  ldc.i4.4
    IL_0079:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreParameterAlias(int, int)""
    IL_007e:  nop
    IL_007f:  ldloc.s    V_4
    IL_0081:  ldind.i4
    IL_0082:  pop
    // sequence point: ref int r3 = ref p2;
    IL_0083:  ldloca.s   V_0
    IL_0085:  ldarg.1
    IL_0086:  stloc.s    V_5
    IL_0088:  ldc.i4.1
    IL_0089:  ldc.i4.5
    IL_008a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreParameterAlias(int, int)""
    IL_008f:  nop
    IL_0090:  ldloc.s    V_5
    IL_0092:  ldind.i4
    IL_0093:  pop
    // sequence point: ref int r4 = ref p3;
    IL_0094:  ldloca.s   V_0
    IL_0096:  ldarg.2
    IL_0097:  stloc.s    V_6
    IL_0099:  ldc.i4.2
    IL_009a:  ldc.i4.6
    IL_009b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreParameterAlias(int, int)""
    IL_00a0:  nop
    IL_00a1:  ldloc.s    V_6
    IL_00a3:  ldind.i4
    IL_00a4:  pop
    // sequence point: ref int r5 = ref r1;
    IL_00a5:  ldloca.s   V_0
    IL_00a7:  ldloc.3
    IL_00a8:  stloc.s    V_7
    IL_00aa:  ldc.i4.3
    IL_00ab:  ldc.i4.7
    IL_00ac:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreLocalAlias(int, int)""
    IL_00b1:  nop
    IL_00b2:  ldloc.s    V_7
    IL_00b4:  ldind.i4
    IL_00b5:  pop
    // sequence point: if (F(ref r1, ref r2, ref r3, ref r4, out r5))
    IL_00b6:  ldloc.3
    IL_00b7:  ldloc.s    V_4
    IL_00b9:  ldloc.s    V_5
    IL_00bb:  ldloc.s    V_6
    IL_00bd:  ldloc.s    V_7
    IL_00bf:  call       ""bool C.F(ref int, ref int, ref int, ref int, out int)""
    IL_00c4:  ldloca.s   V_0
    IL_00c6:  ldloc.3
    IL_00c7:  ldind.i4
    IL_00c8:  ldc.i4.3
    IL_00c9:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00ce:  nop
    IL_00cf:  ldloca.s   V_0
    IL_00d1:  ldloc.s    V_4
    IL_00d3:  ldind.i4
    IL_00d4:  ldc.i4.4
    IL_00d5:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00da:  nop
    IL_00db:  ldloca.s   V_0
    IL_00dd:  ldloc.s    V_5
    IL_00df:  ldind.i4
    IL_00e0:  ldc.i4.5
    IL_00e1:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00e6:  nop
    IL_00e7:  ldloca.s   V_0
    IL_00e9:  ldloc.s    V_6
    IL_00eb:  ldind.i4
    IL_00ec:  ldc.i4.6
    IL_00ed:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00f2:  nop
    IL_00f3:  ldloca.s   V_0
    IL_00f5:  ldloc.s    V_7
    IL_00f7:  ldind.i4
    IL_00f8:  ldc.i4.7
    IL_00f9:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00fe:  nop
    IL_00ff:  stloc.s    V_9
    // sequence point: <hidden>
    IL_0101:  ldloc.s    V_9
    IL_0103:  brfalse.s  IL_011d
    // sequence point: {
    IL_0105:  nop
    // sequence point: r1 = r2;
    IL_0106:  ldloca.s   V_0
    IL_0108:  ldloc.3
    IL_0109:  ldloc.s    V_4
    IL_010b:  ldind.i4
    IL_010c:  dup
    IL_010d:  stloc.s    V_8
    IL_010f:  stind.i4
    IL_0110:  ldloc.s    V_8
    IL_0112:  ldc.i4.3
    IL_0113:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0118:  nop
    IL_0119:  ldloc.3
    IL_011a:  ldind.i4
    IL_011b:  pop
    // sequence point: }
    IL_011c:  nop
    // sequence point: }
    IL_011d:  leave.s    IL_0128
  }
  finally
  {
    // sequence point: <hidden>
    IL_011f:  ldloca.s   V_0
    IL_0121:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0126:  nop
    IL_0127:  endfinally
  }
  // sequence point: }
  IL_0128:  ret
}");
        }

        /// <summary>
        /// We don't currently track local aliases to ref fields.
        /// </summary>
        [Fact]
        public void RefAssignments_RefFields_NotInstrumented()
        {
            var source = WithHelpers(@"
ref struct S
{
    public ref int X;
    public ref int Y;

    public override string ToString() => ""str"";
}

class C
{
    static void Main()
    {
        int a = 1;
        scoped var s = new S();

        s.X = ref a;                // ignored
        s.X = 2;                    // not recognized as update of a

        ref int x = ref s.X;        // ignored
        x = 3;                      // not recognized as update of a

        s.Y = ref s.X;              // ignored
        s.Y = 4;                    // not recognized as update of a
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L1 = 1
ToString: Entered
ToString: Returned
Main: L2 = str
Main: L3 = 3
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      136 (0x88)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                S V_2, //s
                int& V_3, //x
                S V_4,
                int V_5)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: scoped var s = new S();
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldloca.s   V_2
    IL_001c:  initobj    ""S""
    IL_0022:  ldloc.2
    IL_0023:  stloc.s    V_4
    IL_0025:  ldloca.s   V_4
    IL_0027:  constrained. ""S""
    IL_002d:  callvirt   ""string object.ToString()""
    IL_0032:  ldc.i4.2
    IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
    IL_0038:  nop
    // sequence point: s.X = ref a;
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldloca.s   V_1
    IL_003d:  stfld      ""ref int S.X""
    // sequence point: s.X = 2;
    IL_0042:  ldloc.2
    IL_0043:  ldfld      ""ref int S.X""
    IL_0048:  ldc.i4.2
    IL_0049:  stind.i4
    // sequence point: ref int x = ref s.X;
    IL_004a:  ldloca.s   V_2
    IL_004c:  ldfld      ""ref int S.X""
    IL_0051:  stloc.3
    // sequence point: x = 3;
    IL_0052:  ldloca.s   V_0
    IL_0054:  ldloc.3
    IL_0055:  ldc.i4.3
    IL_0056:  dup
    IL_0057:  stloc.s    V_5
    IL_0059:  stind.i4
    IL_005a:  ldloc.s    V_5
    IL_005c:  ldc.i4.3
    IL_005d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0062:  nop
    IL_0063:  ldloc.3
    IL_0064:  ldind.i4
    IL_0065:  pop
    // sequence point: s.Y = ref s.X;
    IL_0066:  ldloca.s   V_2
    IL_0068:  ldloca.s   V_2
    IL_006a:  ldfld      ""ref int S.X""
    IL_006f:  stfld      ""ref int S.Y""
    // sequence point: s.Y = 4;
    IL_0074:  ldloc.2
    IL_0075:  ldfld      ""ref int S.Y""
    IL_007a:  ldc.i4.4
    IL_007b:  stind.i4
    // sequence point: }
    IL_007c:  leave.s    IL_0087
  }
  finally
  {
    // sequence point: <hidden>
    IL_007e:  ldloca.s   V_0
    IL_0080:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0085:  nop
    IL_0086:  endfinally
  }
  // sequence point: }
  IL_0087:  ret
}
");
        }

        [Fact]
        public void RefArguments_Call()
        {
            var source = WithHelpers(@"
class C
{
    static int G(ref int x, ref long y) => 1;
    
    static void Main()
    {
        int a = 1;
        long b = 2;
        int c = G(y: ref b, x: ref a) + 1;
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L1 = 1
Main: L2 = 2
G: Entered
G: P'x'[0] = 1
G: P'y'[1] = 2
G: Returned
Main: L2 = 2
Main: L1 = 1
Main: L3 = 2
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size       91 (0x5b)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                long V_2, //b
                int V_3) //c
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: long b = 2;
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldc.i4.2
    IL_001b:  conv.i8
    IL_001c:  dup
    IL_001d:  stloc.2
    IL_001e:  ldc.i4.2
    IL_001f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0024:  nop
    // sequence point: int c = G(y: ref b, x: ref a) + 1;
    IL_0025:  ldloca.s   V_0
    IL_0027:  ldloca.s   V_1
    IL_0029:  ldloca.s   V_2
    IL_002b:  call       ""int C.G(ref int, ref long)""
    IL_0030:  ldloca.s   V_0
    IL_0032:  ldloc.2
    IL_0033:  ldc.i4.2
    IL_0034:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0039:  nop
    IL_003a:  ldloca.s   V_0
    IL_003c:  ldloc.1
    IL_003d:  ldc.i4.1
    IL_003e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0043:  nop
    IL_0044:  ldc.i4.1
    IL_0045:  add
    IL_0046:  dup
    IL_0047:  stloc.3
    IL_0048:  ldc.i4.3
    IL_0049:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_004e:  nop
    // sequence point: }
    IL_004f:  leave.s    IL_005a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0051:  ldloca.s   V_0
    IL_0053:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0058:  nop
    IL_0059:  endfinally
  }
  // sequence point: }
  IL_005a:  ret
}
");
        }

        [Fact]
        public void RefArguments_Call_Void()
        {
            var source = WithHelpers(@"
class C
{
    static void G(ref int x, ref long y)
    {        
    }
    
    static void Main()
    {
        int a = 1;
        long b = 2;
        G(y: ref b, x: ref a);
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L1 = 1
Main: L2 = 2
G: Entered
G: P'x'[0] = 1
G: P'y'[1] = 2
G: Returned
Main: L2 = 2
Main: L1 = 1
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{ 
  // Code size       79 (0x4f)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
              int V_1, //a
              long V_2) //b
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: long b = 2;
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldc.i4.2
    IL_001b:  conv.i8
    IL_001c:  dup
    IL_001d:  stloc.2
    IL_001e:  ldc.i4.2
    IL_001f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0024:  nop
    // sequence point: G(y: ref b, x: ref a);
    IL_0025:  ldloca.s   V_1
    IL_0027:  ldloca.s   V_2
    IL_0029:  call       ""void C.G(ref int, ref long)""
    IL_002e:  nop
    IL_002f:  ldloca.s   V_0
    IL_0031:  ldloc.2
    IL_0032:  ldc.i4.2
    IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0038:  nop
    IL_0039:  ldloca.s   V_0
    IL_003b:  ldloc.1
    IL_003c:  ldc.i4.1
    IL_003d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0042:  nop
    // sequence point: }
    IL_0043:  leave.s    IL_004e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0045:  ldloca.s   V_0
    IL_0047:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_004c:  nop
    IL_004d:  endfinally
  }
  // sequence point: }
  IL_004e:  ret
}
");
        }

        [Fact]
        public void RefArguments_Call_IgnoreNonLocalRefs()
        {
            var source = WithHelpers(@"
class C
{
    int A;
    long[] B;

    public void G(ref int x, ref long y)
    {        
    }
    
    public void F()
    {
        var c = new C();

        G(y: ref c.B[1], x: ref c.A);
    }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C V_1, //c
                long& V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: var c = new C();
    IL_000c:  ldloca.s   V_0
    IL_000e:  newobj     ""C..ctor()""
    IL_0013:  dup
    IL_0014:  stloc.1
    IL_0015:  ldc.i4.1
    IL_0016:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_001b:  nop
    // sequence point: G(y: ref c.B[1], x: ref c.A);
    IL_001c:  ldarg.0
    IL_001d:  ldloc.1
    IL_001e:  ldfld      ""long[] C.B""
    IL_0023:  ldc.i4.1
    IL_0024:  ldelema    ""long""
    IL_0029:  stloc.2
    IL_002a:  ldloc.1
    IL_002b:  ldflda     ""int C.A""
    IL_0030:  ldloc.2
    IL_0031:  call       ""void C.G(ref int, ref long)""
    IL_0036:  nop
    // sequence point: }
    IL_0037:  leave.s    IL_0042
  }
  finally
  {
    // sequence point: <hidden>
    IL_0039:  ldloca.s   V_0
    IL_003b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0040:  nop
    IL_0041:  endfinally
  }
  // sequence point: }
  IL_0042:  ret
}
");
        }

        [Fact]
        public void RefArguments_ObjectCreation()
        {
            var source = WithHelpers(@"
class C
{
    public C(ref int x, ref long y)
    {        
    }
    
    public void F()
    {
        int a = 1;
        long b = 2;
        var c = new C(y: ref b, x: ref a);
    }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
 {
  // Code size       89 (0x59)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                long V_2, //b
                C V_3) //c
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: long b = 2;
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldc.i4.2
    IL_001b:  conv.i8
    IL_001c:  dup
    IL_001d:  stloc.2
    IL_001e:  ldc.i4.2
    IL_001f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0024:  nop
    // sequence point: var c = new C(y: ref b, x: ref a);
    IL_0025:  ldloca.s   V_0
    IL_0027:  ldloca.s   V_1
    IL_0029:  ldloca.s   V_2
    IL_002b:  newobj     ""C..ctor(ref int, ref long)""
    IL_0030:  ldloca.s   V_0
    IL_0032:  ldloc.2
    IL_0033:  ldc.i4.2
    IL_0034:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0039:  nop
    IL_003a:  ldloca.s   V_0
    IL_003c:  ldloc.1
    IL_003d:  ldc.i4.1
    IL_003e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0043:  nop
    IL_0044:  dup
    IL_0045:  stloc.3
    IL_0046:  ldc.i4.3
    IL_0047:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_004c:  nop
    // sequence point: }
    IL_004d:  leave.s    IL_0058
  }
  finally
  {
    // sequence point: <hidden>
    IL_004f:  ldloca.s   V_0
    IL_0051:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0056:  nop
    IL_0057:  endfinally
  }
  // sequence point: }
  IL_0058:  ret
}
");
        }

        [Fact]
        public void RefArguments_RefSelf()
        {
            var source = WithHelpers(@"
static class C
{
    static void F()
    {
        int a = 1;
        long b = 2;
        a.G(y: ref b, x: ref a);
    }

    static void G(this ref int self, ref int x, ref long y) { }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.F", @"
 {
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                long V_2) //b
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldc.i4.1
    IL_000f:  dup
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0017:  nop
    // sequence point: long b = 2;
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldc.i4.2
    IL_001b:  conv.i8
    IL_001c:  dup
    IL_001d:  stloc.2
    IL_001e:  ldc.i4.2
    IL_001f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0024:  nop
    // sequence point: a.G(y: ref b, x: ref a);
    IL_0025:  ldloca.s   V_1
    IL_0027:  ldloca.s   V_1
    IL_0029:  ldloca.s   V_2
    IL_002b:  call       ""void C.G(ref int, ref int, ref long)""
    IL_0030:  nop
    IL_0031:  ldloca.s   V_0
    IL_0033:  ldloc.1
    IL_0034:  ldc.i4.1
    IL_0035:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_003a:  nop
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldloc.2
    IL_003e:  ldc.i4.2
    IL_003f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_0044:  nop
    IL_0045:  ldloca.s   V_0
    IL_0047:  ldloc.1
    IL_0048:  ldc.i4.1
    IL_0049:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_004e:  nop
    // sequence point: }
    IL_004f:  leave.s    IL_005a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0051:  ldloca.s   V_0
    IL_0053:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0058:  nop
    IL_0059:  endfinally
  }
  // sequence point: }
  IL_005a:  ret
}");
        }

        [Fact]
        public void RefArguments_CollectionInitializer()
        {
            var source = WithHelpers(@"
using System.Collections;
using System.Collections.Generic;

struct C : IEnumerable<int>
{
    public void F()
    {
        var c = new C() { {1} };
    }

    public IEnumerator<int> GetEnumerator() => null;
    IEnumerator IEnumerable.GetEnumerator() => null;
}

static class Extensions
{
    public static void Add(this ref C self, int item) { }
}
");
            var verifier = CompileAndVerify(source, ilVerifyMessage: @"
[F]: Expected numeric type on the stack. { Offset = 0x23, Found = address of 'C' }
[Add]: Expected numeric type on the stack. { Offset = 0xe, Found = address of 'C' }
");

            verifier.VerifyMethodBody("C.F", @"
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C V_1, //c
                C V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: var c = new C() { {1} };
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldloca.s   V_2
    IL_0010:  initobj    ""C""
    IL_0016:  ldloca.s   V_2
    IL_0018:  ldc.i4.1
    IL_0019:  call       ""void Extensions.Add(ref C, int)""
    IL_001e:  nop
    IL_001f:  ldloc.2
    IL_0020:  stloc.1
    IL_0021:  ldloca.s   V_1
    IL_0023:  conv.u
    IL_0024:  sizeof     ""C""
    IL_002a:  ldc.i4.1
    IL_002b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreUnmanaged(void*, int, int)""
    IL_0030:  nop
    // sequence point: }
    IL_0031:  leave.s    IL_003c
  }
  finally
  {
    // sequence point: <hidden>
    IL_0033:  ldloca.s   V_0
    IL_0035:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003a:  nop
    IL_003b:  endfinally
  }
  // sequence point: }
  IL_003c:  ret
}
");
        }

        [Fact]
        public void RefArguments_FunctionPointerInvocation()
        {
            var source = WithHelpers(@"
unsafe class C
{
    void F(delegate*<ref int, ref long, void> f)
    {
        int a = 1;
        long b = 2;
        f(ref a, ref b);
    }
}
");
            var verifier = CompileAndVerify(source, ilVerifyMessage: """
                [F]: ImportCalli not implemented
                """);

            verifier.VerifyMethodBody("C.F", @"
 {
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                long V_2, //b
                delegate*<ref int, ref long, void> V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.F(delegate*<ref int, ref long, void>)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.1
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(void*, int)""
    IL_0014:  nop
    // sequence point: int a = 1;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldc.i4.1
    IL_0018:  dup
    IL_0019:  stloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0020:  nop
    // sequence point: long b = 2;
    IL_0021:  ldloca.s   V_0
    IL_0023:  ldc.i4.2
    IL_0024:  conv.i8
    IL_0025:  dup
    IL_0026:  stloc.2
    IL_0027:  ldc.i4.2
    IL_0028:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_002d:  nop
    // sequence point: f(ref a, ref b);
    IL_002e:  ldarg.1
    IL_002f:  stloc.3
    IL_0030:  ldloca.s   V_1
    IL_0032:  ldloca.s   V_2
    IL_0034:  ldloc.3
    IL_0035:  calli      ""delegate*<ref int, ref long, void>""
    IL_003a:  nop
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldloc.1
    IL_003e:  ldc.i4.1
    IL_003f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0044:  nop
    IL_0045:  ldloca.s   V_0
    IL_0047:  ldloc.2
    IL_0048:  ldc.i4.2
    IL_0049:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(ulong, int)""
    IL_004e:  nop
    // sequence point: }
    IL_004f:  leave.s    IL_005a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0051:  ldloca.s   V_0
    IL_0053:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0058:  nop
    IL_0059:  endfinally
  }
  // sequence point: }
  IL_005a:  ret
}
");
        }

        [Fact]
        public void Initializers_NoConstructorBody_Static()
        {
            var source = WithHelpers(@"
C.F(out var _);

class C
{
    static int A = F(out var x) + (x = 1);
    static int B = F(out var x) + (x = 2);

    public static int F(out int a) => a = 1;
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
F: Entered
F: P'a'[0] = 1
F: Returned
<Main>$: Returned
");
            verifier.VerifyMethodBody("C..cctor", @"
{
  // Code size       95 (0x5f)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //x
                int V_2) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C..cctor()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: static int A = F(out var x) + (x = 1);
    IL_000b:  ldloca.s   V_1
    IL_000d:  call       ""int C.F(out int)""
    IL_0012:  ldloca.s   V_0
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.1
    IL_0016:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_001b:  nop
    IL_001c:  ldloca.s   V_0
    IL_001e:  ldc.i4.1
    IL_001f:  dup
    IL_0020:  stloc.1
    IL_0021:  ldc.i4.1
    IL_0022:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0027:  nop
    IL_0028:  ldloc.1
    IL_0029:  add
    IL_002a:  stsfld     ""int C.A""
    // sequence point: static int B = F(out var x) + (x = 2);
    IL_002f:  ldloca.s   V_2
    IL_0031:  call       ""int C.F(out int)""
    IL_0036:  ldloca.s   V_0
    IL_0038:  ldloc.2
    IL_0039:  ldc.i4.2
    IL_003a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_003f:  nop
    IL_0040:  ldloca.s   V_0
    IL_0042:  ldc.i4.2
    IL_0043:  dup
    IL_0044:  stloc.2
    IL_0045:  ldc.i4.2
    IL_0046:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_004b:  nop
    IL_004c:  ldloc.2
    IL_004d:  add
    IL_004e:  stsfld     ""int C.B""
    IL_0053:  leave.s    IL_005e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0055:  ldloca.s   V_0
    IL_0057:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_005c:  nop
    IL_005d:  endfinally
  }
  // sequence point: <hidden>
  IL_005e:  ret
}");
        }

        [Fact]
        public void Initializers_Lambda()
        {
            var source = WithHelpers(@"
using System;

class C
{
    static Action A = new Action(() => { int x = 1; });

    static void Main() { A(); }
}
");

            var expectedOutput = @"
Main: Entered
.cctor: Entered
.cctor: Returned
.cctor: Entered lambda '<.cctor>b__3_0'
<.cctor>b__3_0: L1 = 1
<.cctor>b__3_0: Returned
Main: Returned
";

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);

            verifier.VerifyMethodBody("C..cctor", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C..cctor()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: static Action A = new Action(() => { int x = 1; });
    IL_000b:  ldsfld     ""C.<>c C.<>c.<>9""
    IL_0010:  ldftn      ""void C.<>c.<.cctor>b__3_0()""
    IL_0016:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_001b:  stsfld     ""System.Action C.A""
    IL_0020:  leave.s    IL_002b
  }
  finally
  {
    // sequence point: <hidden>
    IL_0022:  ldloca.s   V_0
    IL_0024:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0029:  nop
    IL_002a:  endfinally
  }
  // sequence point: <hidden>
  IL_002b:  ret
}
");

            // TODO: is this sequence point correct? https://github.com/dotnet/roslyn/issues/66811
            // sequence point: }
            // IL_001d:  leave.s    IL_0028

            verifier.VerifyMethodBody("C.<>c.<.cctor>b__3_0", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C..cctor()""
  IL_0005:  ldtoken    ""void C.<>c.<.cctor>b__3_0()""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: {
    IL_0010:  nop
    // sequence point: int x = 1;
    IL_0011:  ldloca.s   V_0
    IL_0013:  ldc.i4.1
    IL_0014:  dup
    IL_0015:  stloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_001c:  nop
    // sequence point: }
    IL_001d:  leave.s    IL_0028
  }
  finally
  {
    // sequence point: <hidden>
    IL_001f:  ldloca.s   V_0
    IL_0021:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0026:  nop
    IL_0027:  endfinally
  }
  // sequence point: }
  IL_0028:  ret
}
");
        }

        [Fact]
        public void Initializers_NoConstructorBody()
        {
            var source = WithHelpers(@"
var _ = new C();

class C
{
    int A = F(out var x) + (x = 2);

    public static int F(out int a) => a = 1;
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
.ctor: Entered
F: Entered
F: P'a'[0] = 1
F: Returned
.ctor: L1 = 1
.ctor: L1 = 2
.ctor: Returned
<Main>$: L1 = C
<Main>$: Returned
");
            verifier.VerifyMethodBody("C..ctor", @"
{
  // Code size       67 (0x43)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1) //x
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C..ctor()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: int A = F(out var x) + (x = 2);
    IL_000b:  ldarg.0
    IL_000c:  ldloca.s   V_1
    IL_000e:  call       ""int C.F(out int)""
    IL_0013:  ldloca.s   V_0
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_001c:  nop
    IL_001d:  ldloca.s   V_0
    IL_001f:  ldc.i4.2
    IL_0020:  dup
    IL_0021:  stloc.1
    IL_0022:  ldc.i4.1
    IL_0023:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0028:  nop
    IL_0029:  ldloc.1
    IL_002a:  add
    IL_002b:  stfld      ""int C.A""
    IL_0030:  ldarg.0
    IL_0031:  call       ""object..ctor()""
    IL_0036:  nop
    IL_0037:  leave.s    IL_0042
  }
  finally
  {
    // sequence point: <hidden>
    IL_0039:  ldloca.s   V_0
    IL_003b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0040:  nop
    IL_0041:  endfinally
  }
  // sequence point: <hidden>
  IL_0042:  ret
}");
        }

        [Fact]
        public void Constructors()
        {
            var source = WithHelpers(@"
class B
{
    public B(int p) {}
}

class C : B
{
    int A = F(out var x) + (x = 1);

    C() : base(F(out var y) + (y = 2))
    {
        int z = 3;
    }

    static int F(out int a) => a = 4;
    static void Main() => new C();
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
.ctor: Entered
F: Entered
F: P'a'[0] = 4
F: Returned
.ctor: L2 = 4
.ctor: L2 = 1
F: Entered
F: P'a'[0] = 4
F: Returned
.ctor: L1 = 4
.ctor: L1 = 2
.ctor: Entered
.ctor: P'p'[0] = 6
.ctor: Returned
.ctor: L3 = 3
.ctor: Returned
Main: Returned
");
            verifier.VerifyMethodBody("B..ctor", @"
{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""B..ctor(int)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.1
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0014:  nop
    // sequence point: public B(int p)
    IL_0015:  ldarg.0
    IL_0016:  call       ""object..ctor()""
    IL_001b:  nop
    // sequence point: {
    IL_001c:  nop
    // sequence point: }
    IL_001d:  leave.s    IL_0028
  }
  finally
  {
    // sequence point: <hidden>
    IL_001f:  ldloca.s   V_0
    IL_0021:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0026:  nop
    IL_0027:  endfinally
  }
  // sequence point: }
  IL_0028:  ret
}
");
            verifier.VerifyMethodBody("C..ctor", @"
 {
  // Code size      111 (0x6f)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //y
                int V_2, //x
                int V_3) //z
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""C..ctor()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: int A = F(out var x) + (x = 1);
    IL_000b:  ldarg.0
    IL_000c:  ldloca.s   V_2
    IL_000e:  call       ""int C.F(out int)""
    IL_0013:  ldloca.s   V_0
    IL_0015:  ldloc.2
    IL_0016:  ldc.i4.2
    IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_001c:  nop
    IL_001d:  ldloca.s   V_0
    IL_001f:  ldc.i4.1
    IL_0020:  dup
    IL_0021:  stloc.2
    IL_0022:  ldc.i4.2
    IL_0023:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0028:  nop
    IL_0029:  ldloc.2
    IL_002a:  add
    IL_002b:  stfld      ""int C.A""
    // sequence point: base(F(out var y) + (y = 2))
    IL_0030:  ldarg.0
    IL_0031:  ldloca.s   V_1
    IL_0033:  call       ""int C.F(out int)""
    IL_0038:  ldloca.s   V_0
    IL_003a:  ldloc.1
    IL_003b:  ldc.i4.1
    IL_003c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0041:  nop
    IL_0042:  ldloca.s   V_0
    IL_0044:  ldc.i4.2
    IL_0045:  dup
    IL_0046:  stloc.1
    IL_0047:  ldc.i4.1
    IL_0048:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_004d:  nop
    IL_004e:  ldloc.1
    IL_004f:  add
    IL_0050:  call       ""B..ctor(int)""
    IL_0055:  nop
    // sequence point: {
    IL_0056:  nop
    // sequence point: int z = 3;
    IL_0057:  ldloca.s   V_0
    IL_0059:  ldc.i4.3
    IL_005a:  dup
    IL_005b:  stloc.3
    IL_005c:  ldc.i4.3
    IL_005d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0062:  nop
    // sequence point: }
    IL_0063:  leave.s    IL_006e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0065:  ldloca.s   V_0
    IL_0067:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_006c:  nop
    IL_006d:  endfinally
  }
  // sequence point: }
  IL_006e:  ret
}
");
        }

        [Fact]
        public void EmbeddedStatement()
        {
            var source = WithHelpers(@"
class C
{
    void M()
    {
        while(true)
            G(F(out var x), x = 1);
    }

    int F(out int a) => a = 1;
    void G(int a, int b) {}
}
");

            var verifier = CompileAndVerify(source);
            verifier.VerifyMethodBody("C.M", @"
{
  // Code size       65 (0x41)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //x
                bool V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.M()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: <hidden>
    IL_000c:  br.s       IL_0034
    // sequence point: G(F(out var x), x = 1);
    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  ldloca.s   V_1
    IL_0012:  call       ""int C.F(out int)""
    IL_0017:  ldloca.s   V_0
    IL_0019:  ldloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0020:  nop
    IL_0021:  ldloca.s   V_0
    IL_0023:  ldc.i4.1
    IL_0024:  dup
    IL_0025:  stloc.1
    IL_0026:  ldc.i4.1
    IL_0027:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_002c:  nop
    IL_002d:  ldloc.1
    IL_002e:  call       ""void C.G(int, int)""
    IL_0033:  nop
    // sequence point: while(true)
    IL_0034:  ldc.i4.1
    IL_0035:  stloc.2
    IL_0036:  br.s       IL_000e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0038:  ldloca.s   V_0
    IL_003a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003f:  nop
    IL_0040:  endfinally
  }
}
");
        }

        [Fact]
        public void Lambdas()
        {
            var source = WithHelpers(@"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        {
            int b = 2;
            {
                int c = 3;
                
                F(() => c += 1);
            }

            F(() => a += b);
        }
    }

    static void F(Func<int> f) { f(); }
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L'a' = 1
Main: L'b' = 2
Main: L'c' = 3
F: Entered
F: P'f'[0] = System.Func`1[System.Int32]
Main: Entered lambda '<Main>b__1'
<Main>b__1: L'c' = 4
<Main>b__1: Returned
F: Returned
F: Entered
F: P'f'[0] = System.Func`1[System.Int32]
Main: Entered lambda '<Main>b__0'
<Main>b__0: L'a' = 3
<Main>b__0: Returned
F: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.<>c__DisplayClass0_1.<Main>b__0()", @"
{
  // Code size       86 (0x56)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""int C.<>c__DisplayClass0_1.<Main>b__0()""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: a += b
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.0
    IL_0013:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0018:  ldarg.0
    IL_0019:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_001e:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""int C.<>c__DisplayClass0_1.b""
    IL_0029:  add
    IL_002a:  dup
    IL_002b:  stloc.1
    IL_002c:  stfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0031:  ldloc.1
    IL_0032:  ldtoken    ""int C.<>c__DisplayClass0_0.a""
    IL_0037:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_003c:  nop
    IL_003d:  ldarg.0
    IL_003e:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0043:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0048:  stloc.2
    IL_0049:  leave.s    IL_0054
  }
  finally
  {
    // sequence point: <hidden>
    IL_004b:  ldloca.s   V_0
    IL_004d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0052:  nop
    IL_0053:  endfinally
  }
  // sequence point: <hidden>
  IL_0054:  ldloc.2
  IL_0055:  ret
}
");

            verifier.VerifyMethodBody("C.<>c__DisplayClass0_2.<Main>b__1()", @"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""int C.<>c__DisplayClass0_2.<Main>b__1()""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: c += 1
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0019:  ldc.i4.1
    IL_001a:  add
    IL_001b:  dup
    IL_001c:  stloc.1
    IL_001d:  stfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0022:  ldloc.1
    IL_0023:  ldtoken    ""int C.<>c__DisplayClass0_2.c""
    IL_0028:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_002d:  nop
    IL_002e:  ldarg.0
    IL_002f:  ldfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0034:  stloc.2
    IL_0035:  leave.s    IL_0040
  }
  finally
  {
    // sequence point: <hidden>
    IL_0037:  ldloca.s   V_0
    IL_0039:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003e:  nop
    IL_003f:  endfinally
  }
  // sequence point: <hidden>
  IL_0040:  ldloc.2
  IL_0041:  ret
}
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      183 (0xb7)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C.<>c__DisplayClass0_0 V_1, //CS$<>8__locals0
                int V_2,
                C.<>c__DisplayClass0_1 V_3, //CS$<>8__locals1
                C.<>c__DisplayClass0_2 V_4) //CS$<>8__locals2
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_000b:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
    IL_0010:  stloc.1
    // sequence point: {
    IL_0011:  nop
    // sequence point: int a = 1;
    IL_0012:  ldloca.s   V_0
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.1
    IL_0016:  dup
    IL_0017:  stloc.2
    IL_0018:  stfld      ""int C.<>c__DisplayClass0_0.a""
    IL_001d:  ldloc.2
    IL_001e:  ldtoken    ""int C.<>c__DisplayClass0_0.a""
    IL_0023:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0028:  nop
    IL_0029:  ldloc.1
    IL_002a:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_002f:  pop
    // sequence point: <hidden>
    IL_0030:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
    IL_0035:  stloc.3
    IL_0036:  ldloc.3
    IL_0037:  ldloc.1
    IL_0038:  stfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    // sequence point: {
    IL_003d:  nop
    // sequence point: int b = 2;
    IL_003e:  ldloca.s   V_0
    IL_0040:  ldloc.3
    IL_0041:  ldc.i4.2
    IL_0042:  dup
    IL_0043:  stloc.2
    IL_0044:  stfld      ""int C.<>c__DisplayClass0_1.b""
    IL_0049:  ldloc.2
    IL_004a:  ldtoken    ""int C.<>c__DisplayClass0_1.b""
    IL_004f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0054:  nop
    IL_0055:  ldloc.3
    IL_0056:  ldfld      ""int C.<>c__DisplayClass0_1.b""
    IL_005b:  pop
    // sequence point: <hidden>
    IL_005c:  newobj     ""C.<>c__DisplayClass0_2..ctor()""
    IL_0061:  stloc.s    V_4
    // sequence point: {
    IL_0063:  nop
    // sequence point: int c = 3;
    IL_0064:  ldloca.s   V_0
    IL_0066:  ldloc.s    V_4
    IL_0068:  ldc.i4.3
    IL_0069:  dup
    IL_006a:  stloc.2
    IL_006b:  stfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0070:  ldloc.2
    IL_0071:  ldtoken    ""int C.<>c__DisplayClass0_2.c""
    IL_0076:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_007b:  nop
    IL_007c:  ldloc.s    V_4
    IL_007e:  ldfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0083:  pop
    // sequence point: F(() => c += 1);
    IL_0084:  ldloc.s    V_4
    IL_0086:  ldftn      ""int C.<>c__DisplayClass0_2.<Main>b__1()""
    IL_008c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_0091:  call       ""void C.F(System.Func<int>)""
    IL_0096:  nop
    // sequence point: }
    IL_0097:  nop
    // sequence point: F(() => a += b);
    IL_0098:  ldloc.3
    IL_0099:  ldftn      ""int C.<>c__DisplayClass0_1.<Main>b__0()""
    IL_009f:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
    IL_00a4:  call       ""void C.F(System.Func<int>)""
    IL_00a9:  nop
    // sequence point: }
    IL_00aa:  nop
    // sequence point: }
    IL_00ab:  leave.s    IL_00b6
  }
  finally
  {
    // sequence point: <hidden>
    IL_00ad:  ldloca.s   V_0
    IL_00af:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_00b4:  nop
    IL_00b5:  endfinally
  }
  // sequence point: }
  IL_00b6:  ret
}
");
        }

        [Fact]
        public void Lambdas_Parameters()
        {
            var source = WithHelpers(@"
using System;

class C
{
    static void Main()
    {
        F(b =>
        {
            int a = 1;
            b = 2;
            ref int c = ref a;
            ref int d = ref b;
        });
    }

    static void F(Action<int> f) => f(3);
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
F: Entered
F: P'f'[0] = System.Action`1[System.Int32]
Main: Entered lambda '<Main>b__0_0'
<Main>b__0_0: P'b'[0] = 3
<Main>b__0_0: L1 = 1
<Main>b__0_0: P'b'[0] = 2
<Main>b__0_0: L2 -> L1
<Main>b__0_0: L3 -> P'b'[0]
<Main>b__0_0: Returned
F: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.<>c.<Main>b__0_0", @"
{
  // Code size       95 (0x5f)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                int& V_2, //c
                int& V_3) //d
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""void C.<>c.<Main>b__0_0(int)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: {
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0019:  nop
    // sequence point: int a = 1;
    IL_001a:  ldloca.s   V_0
    IL_001c:  ldc.i4.1
    IL_001d:  dup
    IL_001e:  stloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0025:  nop
    // sequence point: b = 2;
    IL_0026:  ldloca.s   V_0
    IL_0028:  ldc.i4.2
    IL_0029:  dup
    IL_002a:  starg.s    V_1
    IL_002c:  ldc.i4.0
    IL_002d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0032:  nop
    // sequence point: ref int c = ref a;
    IL_0033:  ldloca.s   V_0
    IL_0035:  ldloca.s   V_1
    IL_0037:  stloc.2
    IL_0038:  ldc.i4.1
    IL_0039:  ldc.i4.2
    IL_003a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreLocalAlias(int, int)""
    IL_003f:  nop
    IL_0040:  ldloc.2
    IL_0041:  ldind.i4
    IL_0042:  pop
    // sequence point: ref int d = ref b;
    IL_0043:  ldloca.s   V_0
    IL_0045:  ldarga.s   V_1
    IL_0047:  stloc.3
    IL_0048:  ldc.i4.0
    IL_0049:  ldc.i4.3
    IL_004a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStoreParameterAlias(int, int)""
    IL_004f:  nop
    IL_0050:  ldloc.3
    IL_0051:  ldind.i4
    IL_0052:  pop
    // sequence point: }
    IL_0053:  leave.s    IL_005e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0055:  ldloca.s   V_0
    IL_0057:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_005c:  nop
    IL_005d:  endfinally
  }
  // sequence point: }
  IL_005e:  ret
}
");
        }

        [Fact]
        public void Lambdas_LiftedParameters()
        {
            var source = WithHelpers(@"
using System;

class C
{
    static void Main()
    {
        G(1);
    }
    
    static void G(int a)
    {
        F(b => 
        {
            a = b;
            return F(c => ++b);
        });
    }

    static int F(Func<int, int> f) => f(2);
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
G: Entered
G: P'a'[0] = 1
F: Entered
F: P'f'[0] = System.Func`2[System.Int32,System.Int32]
G: Entered lambda '<G>b__0'
<G>b__0: P'b'[0] = 2
<G>b__0: P'a' = 2
F: Entered
F: P'f'[0] = System.Func`2[System.Int32,System.Int32]
G: Entered lambda '<G>b__1'
<G>b__1: P'c'[0] = 2
<G>b__1: P'b' = 3
<G>b__1: Returned
F: Returned
<G>b__0: Returned
F: Returned
G: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.G", @"
{
  // Code size       69 (0x45)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C.<>c__DisplayClass1_0 V_1) //CS$<>8__locals0
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.G(int)""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_000b:  newobj     ""C.<>c__DisplayClass1_0..ctor()""
    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  ldarg.0
    IL_0013:  stfld      ""int C.<>c__DisplayClass1_0.a""
    // sequence point: {
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldloc.1
    IL_001b:  ldfld      ""int C.<>c__DisplayClass1_0.a""
    IL_0020:  ldc.i4.0
    IL_0021:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0026:  nop
    // sequence point: F(b =>  ...         });
    IL_0027:  ldloc.1
    IL_0028:  ldftn      ""int C.<>c__DisplayClass1_0.<G>b__0(int)""
    IL_002e:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
    IL_0033:  call       ""int C.F(System.Func<int, int>)""
    IL_0038:  pop
    // sequence point: }
    IL_0039:  leave.s    IL_0044
  }
  finally
  {
    // sequence point: <hidden>
    IL_003b:  ldloca.s   V_0
    IL_003d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0042:  nop
    IL_0043:  endfinally
  }
  // sequence point: }
  IL_0044:  ret
}
");
            verifier.VerifyMethodBody("C.<>c__DisplayClass1_0.<G>b__0", @"
{
  // Code size      110 (0x6e)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C.<>c__DisplayClass1_1 V_1, //CS$<>8__locals0
                int V_2,
                int V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.G(int)""
  IL_0005:  ldtoken    ""int C.<>c__DisplayClass1_0.<G>b__0(int)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0010:  newobj     ""C.<>c__DisplayClass1_1..ctor()""
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldarg.1
    IL_0018:  stfld      ""int C.<>c__DisplayClass1_1.b""
    // sequence point: {
    IL_001d:  ldloca.s   V_0
    IL_001f:  ldloc.1
    IL_0020:  ldfld      ""int C.<>c__DisplayClass1_1.b""
    IL_0025:  ldc.i4.0
    IL_0026:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_002b:  nop
    // sequence point: a = b;
    IL_002c:  ldloca.s   V_0
    IL_002e:  ldarg.0
    IL_002f:  ldloc.1
    IL_0030:  ldfld      ""int C.<>c__DisplayClass1_1.b""
    IL_0035:  dup
    IL_0036:  stloc.2
    IL_0037:  stfld      ""int C.<>c__DisplayClass1_0.a""
    IL_003c:  ldloc.2
    IL_003d:  ldtoken    ""int C.<>c__DisplayClass1_0.a""
    IL_0042:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0047:  nop
    IL_0048:  ldarg.0
    IL_0049:  ldfld      ""int C.<>c__DisplayClass1_0.a""
    IL_004e:  pop
    // sequence point: return F(c => ++b);
    IL_004f:  ldloc.1
    IL_0050:  ldftn      ""int C.<>c__DisplayClass1_1.<G>b__1(int)""
    IL_0056:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
    IL_005b:  call       ""int C.F(System.Func<int, int>)""
    IL_0060:  stloc.3
    IL_0061:  leave.s    IL_006c
  }
  finally
  {
    // sequence point: <hidden>
    IL_0063:  ldloca.s   V_0
    IL_0065:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_006a:  nop
    IL_006b:  endfinally
  }
  // sequence point: }
  IL_006c:  ldloc.3
  IL_006d:  ret
}");
            verifier.VerifyMethodBody("C.<>c__DisplayClass1_1.<G>b__1", @"
{
  // Code size       80 (0x50)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2,
                int V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.G(int)""
  IL_0005:  ldtoken    ""int C.<>c__DisplayClass1_1.<G>b__1(int)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0019:  nop
    // sequence point: ++b
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""int C.<>c__DisplayClass1_1.b""
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  stloc.1
    IL_0023:  ldloca.s   V_0
    IL_0025:  ldarg.0
    IL_0026:  ldloc.1
    IL_0027:  dup
    IL_0028:  stloc.2
    IL_0029:  stfld      ""int C.<>c__DisplayClass1_1.b""
    IL_002e:  ldloc.2
    IL_002f:  ldtoken    ""int C.<>c__DisplayClass1_1.b""
    IL_0034:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0039:  nop
    IL_003a:  ldarg.0
    IL_003b:  ldfld      ""int C.<>c__DisplayClass1_1.b""
    IL_0040:  pop
    IL_0041:  ldloc.1
    IL_0042:  stloc.3
    IL_0043:  leave.s    IL_004e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0045:  ldloca.s   V_0
    IL_0047:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_004c:  nop
    IL_004d:  endfinally
  }
  // sequence point: <hidden>
  IL_004e:  ldloc.3
  IL_004f:  ret
}
");
        }

        [Fact, CompilerTrait(CompilerFeature.Async)]
        public void StateMachine_Async()
        {
            var source = WithHelpers(@"
using System.Threading.Tasks;

class C
{
    static async Task M(int p)
    {
        int a = p;
        F(out var b);
        await Task.FromResult(1);
        int c = b;
    }

    static int F(out int a) => a = 1;
    static async Task Main() => await M(2);
}
");

            var expectedOutput = @"
Main: Entered state machine #1
M: Entered state machine #2
M: P'p'[0] = 2
M: L'a' = 2
F: Entered
F: P'a'[0] = 1
F: Returned
M: L'b' = 1
M: L'c' = 1
M: Returned
Main: Returned
";

            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);

            verifier.VerifyMethodBody("C.M", @"
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (C.<M>d__0 V_0)
  IL_0000:  newobj     ""C.<M>d__0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_000c:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0011:  ldloc.0
  IL_0012:  ldarg.0
  IL_0013:  stfld      ""int C.<M>d__0.p""
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.m1
  IL_001a:  stfld      ""int C.<M>d__0.<>1__state""
  IL_001f:  ldloc.0
  IL_0020:  call       ""ulong Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.GetNewStateMachineInstanceId()""
  IL_0025:  stfld      ""ulong C.<M>d__0.<>I""
  IL_002a:  ldloc.0
  IL_002b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_0030:  ldloca.s   V_0
  IL_0032:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<C.<M>d__0>(ref C.<M>d__0)""
  IL_0037:  ldloc.0
  IL_0038:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
  IL_003d:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_0042:  ret
}
");

            verifier.VerifyMethodBody("C.<M>d__0..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  ret
}
");
            // TODO: remove unnecessary IL:  https://github.com/dotnet/roslyn/issues/66810
            // IL_0038: ldarg.0
            // IL_0039: ldfld      ""int C.<Main>d__0.<a>5__1""
            // IL_003e: pop

            verifier.VerifyMethodBody("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      304 (0x130)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                C.<M>d__0 V_4,
                System.Exception V_5)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""System.Threading.Tasks.Task C.M(int)""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""ulong C.<M>d__0.<>I""
  IL_000b:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineMethodEntry(int, ulong)""
  IL_0010:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""int C.<M>d__0.<>1__state""
    IL_0017:  stloc.1
    .try
    {
      // sequence point: <hidden>
      IL_0018:  ldloc.1
      IL_0019:  brfalse.s  IL_001d
      IL_001b:  br.s       IL_0022
      IL_001d:  br         IL_00ad
      // sequence point: {
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldarg.0
      IL_0025:  ldfld      ""int C.<M>d__0.p""
      IL_002a:  ldc.i4.0
      IL_002b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
      IL_0030:  nop
      // sequence point: int a = p;
      IL_0031:  ldloca.s   V_0
      IL_0033:  ldarg.0
      IL_0034:  ldarg.0
      IL_0035:  ldfld      ""int C.<M>d__0.p""
      IL_003a:  dup
      IL_003b:  stloc.2
      IL_003c:  stfld      ""int C.<M>d__0.<a>5__1""
      IL_0041:  ldloc.2
      IL_0042:  ldtoken    ""int C.<M>d__0.<a>5__1""
      IL_0047:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_004c:  nop
      IL_004d:  ldarg.0
      IL_004e:  ldfld      ""int C.<M>d__0.<a>5__1""
      IL_0053:  pop
      // sequence point: F(out var b);
      IL_0054:  ldarg.0
      IL_0055:  ldflda     ""int C.<M>d__0.<b>5__2""
      IL_005a:  call       ""int C.F(out int)""
      IL_005f:  pop
      IL_0060:  ldloca.s   V_0
      IL_0062:  ldarg.0
      IL_0063:  ldfld      ""int C.<M>d__0.<b>5__2""
      IL_0068:  ldtoken    ""int C.<M>d__0.<b>5__2""
      IL_006d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_0072:  nop
      // sequence point: await Task.FromResult(1);
      IL_0073:  ldc.i4.1
      IL_0074:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
      IL_0079:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_007e:  stloc.3
      // sequence point: <hidden>
      IL_007f:  ldloca.s   V_3
      IL_0081:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0086:  brtrue.s   IL_00c9
      IL_0088:  ldarg.0
      IL_0089:  ldc.i4.0
      IL_008a:  dup
      IL_008b:  stloc.1
      IL_008c:  stfld      ""int C.<M>d__0.<>1__state""
      // async: yield
      IL_0091:  ldarg.0
      IL_0092:  ldloc.3
      IL_0093:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_0098:  ldarg.0
      IL_0099:  stloc.s    V_4
      IL_009b:  ldarg.0
      IL_009c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
      IL_00a1:  ldloca.s   V_3
      IL_00a3:  ldloca.s   V_4
      IL_00a5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<M>d__0)""
      IL_00aa:  nop
      IL_00ab:  leave.s    IL_0124
      // async: resume
      IL_00ad:  ldarg.0
      IL_00ae:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_00b3:  stloc.3
      IL_00b4:  ldarg.0
      IL_00b5:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_00ba:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00c0:  ldarg.0
      IL_00c1:  ldc.i4.m1
      IL_00c2:  dup
      IL_00c3:  stloc.1
      IL_00c4:  stfld      ""int C.<M>d__0.<>1__state""
      IL_00c9:  ldloca.s   V_3
      IL_00cb:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00d0:  pop
      // sequence point: int c = b;
      IL_00d1:  ldloca.s   V_0
      IL_00d3:  ldarg.0
      IL_00d4:  ldarg.0
      IL_00d5:  ldfld      ""int C.<M>d__0.<b>5__2""
      IL_00da:  dup
      IL_00db:  stloc.2
      IL_00dc:  stfld      ""int C.<M>d__0.<c>5__3""
      IL_00e1:  ldloc.2
      IL_00e2:  ldtoken    ""int C.<M>d__0.<c>5__3""
      IL_00e7:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_00ec:  nop
      IL_00ed:  ldarg.0
      IL_00ee:  ldfld      ""int C.<M>d__0.<c>5__3""
      IL_00f3:  pop
      IL_00f4:  leave.s    IL_0110
    }
    catch System.Exception
    {
      // sequence point: <hidden>
      IL_00f6:  stloc.s    V_5
      IL_00f8:  ldarg.0
      IL_00f9:  ldc.i4.s   -2
      IL_00fb:  stfld      ""int C.<M>d__0.<>1__state""
      IL_0100:  ldarg.0
      IL_0101:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
      IL_0106:  ldloc.s    V_5
      IL_0108:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
      IL_010d:  nop
      IL_010e:  leave.s    IL_0124
    }
    // sequence point: }
    IL_0110:  ldarg.0
    IL_0111:  ldc.i4.s   -2
    IL_0113:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: <hidden>
    IL_0118:  ldarg.0
    IL_0119:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<M>d__0.<>t__builder""
    IL_011e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
    IL_0123:  nop
    IL_0124:  leave.s    IL_012f
  }
  finally
  {
    // sequence point: <hidden>
    IL_0126:  ldloca.s   V_0
    IL_0128:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_012d:  nop
    IL_012e:  endfinally
  }
  // sequence point: <hidden>
  IL_012f:  ret
}
");

            expectedOutput = """
Main: Entered
M: Entered
M: P'p'[0] = 2
M: L1 = 2
F: Entered
F: P'a'[0] = 1
F: Returned
M: L2 = 1
M: L3 = 1
M: Returned
Main: Returned
""";
            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.UnsafeDebugExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                 emitOptions: s_emitOptions, verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyMethodBody("C.M", """
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                int V_2, //b
                int V_3) //c
  // sequence point: <hidden>
  IL_0000:  ldtoken    "System.Threading.Tasks.Task C.M(int)"
  IL_0005:  call       "Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)"
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)"
    IL_0014:  nop
    // sequence point: int a = p;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldarg.0
    IL_0018:  dup
    IL_0019:  stloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
    IL_0020:  nop
    // sequence point: F(out var b);
    IL_0021:  ldloca.s   V_2
    IL_0023:  call       "int C.F(out int)"
    IL_0028:  pop
    IL_0029:  ldloca.s   V_0
    IL_002b:  ldloc.2
    IL_002c:  ldc.i4.2
    IL_002d:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
    IL_0032:  nop
    // sequence point: await Task.FromResult(1);
    IL_0033:  ldc.i4.1
    IL_0034:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
    IL_0039:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
    IL_003e:  pop
    // sequence point: int c = b;
    IL_003f:  ldloca.s   V_0
    IL_0041:  ldloc.2
    IL_0042:  dup
    IL_0043:  stloc.3
    IL_0044:  ldc.i4.3
    IL_0045:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
    IL_004a:  nop
    IL_004b:  leave.s    IL_0056
  }
  finally
  {
    // sequence point: <hidden>
    IL_004d:  ldloca.s   V_0
    IL_004f:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()"
    IL_0054:  nop
    IL_0055:  endfinally
  }
  // sequence point: <hidden>
  IL_0056:  ret
}
""");
        }

        [Fact]
        public void StateMachine_Iterator()
        {
            var source = WithHelpers(@"
using System.Linq;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> M(int p)
    {
        int a = p;
        F(out var b);
        yield return 1;
        int c = b;
    }

    static int F(out int a) => a = 1;

    static void Main() => M(2).ToArray();
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
M: Entered state machine #1
M: P'p'[0] = 2
M: L'a' = 2
F: Entered
F: P'a'[0] = 1
F: Returned
M: L'b' = 1
M: Returned
M: Entered state machine #1
M: L'c' = 1
M: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.M", @"
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""C.<M>d__0..ctor(int)""
  IL_0007:  dup
  IL_0008:  ldarg.0
  IL_0009:  stfld      ""int C.<M>d__0.<>3__p""
  IL_000e:  ret
}
");
            verifier.VerifyMethodBody("C.<M>d__0..ctor", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int C.<M>d__0.<>1__state""
  IL_000e:  ldarg.0
  IL_000f:  call       ""int System.Environment.CurrentManagedThreadId.get""
  IL_0014:  stfld      ""int C.<M>d__0.<>l__initialThreadId""
  IL_0019:  ldarg.0
  IL_001a:  call       ""ulong Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.GetNewStateMachineInstanceId()""
  IL_001f:  stfld      ""ulong C.<M>d__0.<>I""
  IL_0024:  ret
}
");

            // TODO: remove unnecessary IL:
            // ldarg.0
            // ldfld      ""int C.<M>d__0.<a>5__1""
            // pop

            verifier.VerifyMethodBody("C.<M>d__0.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      209 (0xd1)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                bool V_2,
                int V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""System.Collections.Generic.IEnumerable<int> C.M(int)""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""ulong C.<M>d__0.<>I""
  IL_000b:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineMethodEntry(int, ulong)""
  IL_0010:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""int C.<M>d__0.<>1__state""
    IL_0017:  stloc.1
    IL_0018:  ldloc.1
    IL_0019:  brfalse.s  IL_0023
    IL_001b:  br.s       IL_001d
    IL_001d:  ldloc.1
    IL_001e:  ldc.i4.1
    IL_001f:  beq.s      IL_0025
    IL_0021:  br.s       IL_0027
    IL_0023:  br.s       IL_002e
    IL_0025:  br.s       IL_0098
    IL_0027:  ldc.i4.0
    IL_0028:  stloc.2
    IL_0029:  leave      IL_00cf
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.m1
    IL_0030:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: {
    IL_0035:  ldloca.s   V_0
    IL_0037:  ldarg.0
    IL_0038:  ldfld      ""int C.<M>d__0.p""
    IL_003d:  ldc.i4.0
    IL_003e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0043:  nop
    // sequence point: int a = p;
    IL_0044:  ldloca.s   V_0
    IL_0046:  ldarg.0
    IL_0047:  ldarg.0
    IL_0048:  ldfld      ""int C.<M>d__0.p""
    IL_004d:  dup
    IL_004e:  stloc.3
    IL_004f:  stfld      ""int C.<M>d__0.<a>5__1""
    IL_0054:  ldloc.3
    IL_0055:  ldtoken    ""int C.<M>d__0.<a>5__1""
    IL_005a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_005f:  nop
    IL_0060:  ldarg.0
    IL_0061:  ldfld      ""int C.<M>d__0.<a>5__1""
    IL_0066:  pop
    // sequence point: F(out var b);
    IL_0067:  ldarg.0
    IL_0068:  ldflda     ""int C.<M>d__0.<b>5__2""
    IL_006d:  call       ""int C.F(out int)""
    IL_0072:  pop
    IL_0073:  ldloca.s   V_0
    IL_0075:  ldarg.0
    IL_0076:  ldfld      ""int C.<M>d__0.<b>5__2""
    IL_007b:  ldtoken    ""int C.<M>d__0.<b>5__2""
    IL_0080:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0085:  nop
    // sequence point: yield return 1;
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.1
    IL_0088:  stfld      ""int C.<M>d__0.<>2__current""
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.1
    IL_008f:  stfld      ""int C.<M>d__0.<>1__state""
    IL_0094:  ldc.i4.1
    IL_0095:  stloc.2
    IL_0096:  leave.s    IL_00cf
    // sequence point: <hidden>
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.m1
    IL_009a:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: int c = b;
    IL_009f:  ldloca.s   V_0
    IL_00a1:  ldarg.0
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""int C.<M>d__0.<b>5__2""
    IL_00a8:  dup
    IL_00a9:  stloc.3
    IL_00aa:  stfld      ""int C.<M>d__0.<c>5__3""
    IL_00af:  ldloc.3
    IL_00b0:  ldtoken    ""int C.<M>d__0.<c>5__3""
    IL_00b5:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_00ba:  nop
    IL_00bb:  ldarg.0
    IL_00bc:  ldfld      ""int C.<M>d__0.<c>5__3""
    IL_00c1:  pop
    // sequence point: }
    IL_00c2:  ldc.i4.0
    IL_00c3:  stloc.2
    IL_00c4:  leave.s    IL_00cf
  }
  finally
  {
    // sequence point: <hidden>
    IL_00c6:  ldloca.s   V_0
    IL_00c8:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_00cd:  nop
    IL_00ce:  endfinally
  }
  // sequence point: <hidden>
  IL_00cf:  ldloc.2
  IL_00d0:  ret
}");
        }

        [Fact, CompilerTrait(CompilerFeature.AsyncStreams)]
        public void StateMachine_AsyncIterator()
        {
            var source = WithHelpers(@"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async IAsyncEnumerable<int> M(int p)
    {
        await Task.FromResult(1);
        int a = p;
        F(out var b);
        yield return 1;
        int c = b;
    }

    static int F(out int a) => a = 1;

    static async Task Main()
    {
        await foreach (var n in M(2)) {}
    }
}
");

            var expectedOutput = @"
Main: Entered state machine #1
M: Entered state machine #2
M: P'p'[0] = 2
M: L'a' = 2
F: Entered
F: P'a'[0] = 1
F: Returned
M: L'b' = 1
M: Returned
Main: L'n' = 1
M: Entered state machine #2
M: L'c' = 1
M: Returned
Main: Returned
";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);

            verifier.VerifyMethodBody("C.M", @"
 {
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""C.<M>d__0..ctor(int)""
  IL_0007:  dup
  IL_0008:  ldarg.0
  IL_0009:  stfld      ""int C.<M>d__0.<>3__p""
  IL_000e:  ret
}
");

            // TODO: remove unnecessary IL:  https://github.com/dotnet/roslyn/issues/66810
            // IL_0038: ldarg.0
            // IL_0039: ldfld      ""int C.<M>d__0.<a>5__1""
            // IL_003e: pop

            verifier.VerifyMethodBody("C.<M>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      457 (0x1c9)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<M>d__0 V_3,
                int V_4,
                System.Exception V_5)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""System.Collections.Generic.IAsyncEnumerable<int> C.M(int)""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""ulong C.<M>d__0.<>I""
  IL_000b:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineMethodEntry(int, ulong)""
  IL_0010:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""int C.<M>d__0.<>1__state""
    IL_0017:  stloc.1
    .try
    {
      // sequence point: <hidden>
      IL_0018:  ldloc.1
      IL_0019:  ldc.i4.s   -4
      IL_001b:  sub
      IL_001c:  switch    (
        IL_0037,
        IL_003c,
        IL_0040,
        IL_0040,
        IL_003e)
      IL_0035:  br.s       IL_0040
      IL_0037:  br         IL_011f
      IL_003c:  br.s       IL_0040
      IL_003e:  br.s       IL_00a1
      IL_0040:  ldarg.0
      IL_0041:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
      IL_0046:  brfalse.s  IL_004d
      IL_0048:  leave      IL_0186
      IL_004d:  ldarg.0
      IL_004e:  ldc.i4.m1
      IL_004f:  dup
      IL_0050:  stloc.1
      IL_0051:  stfld      ""int C.<M>d__0.<>1__state""
      // sequence point: {
      IL_0056:  ldloca.s   V_0
      IL_0058:  ldarg.0
      IL_0059:  ldfld      ""int C.<M>d__0.p""
      IL_005e:  ldc.i4.0
      IL_005f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
      IL_0064:  nop
      // sequence point: await Task.FromResult(1);
      IL_0065:  ldc.i4.1
      IL_0066:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
      IL_006b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0070:  stloc.2
      // sequence point: <hidden>
      IL_0071:  ldloca.s   V_2
      IL_0073:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_0078:  brtrue.s   IL_00bd
      IL_007a:  ldarg.0
      IL_007b:  ldc.i4.0
      IL_007c:  dup
      IL_007d:  stloc.1
      IL_007e:  stfld      ""int C.<M>d__0.<>1__state""
      // async: yield
      IL_0083:  ldarg.0
      IL_0084:  ldloc.2
      IL_0085:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_008a:  ldarg.0
      IL_008b:  stloc.3
      IL_008c:  ldarg.0
      IL_008d:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
      IL_0092:  ldloca.s   V_2
      IL_0094:  ldloca.s   V_3
      IL_0096:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<M>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<M>d__0)""
      IL_009b:  nop
      IL_009c:  leave      IL_01bd
      // async: resume
      IL_00a1:  ldarg.0
      IL_00a2:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_00a7:  stloc.2
      IL_00a8:  ldarg.0
      IL_00a9:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<M>d__0.<>u__1""
      IL_00ae:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00b4:  ldarg.0
      IL_00b5:  ldc.i4.m1
      IL_00b6:  dup
      IL_00b7:  stloc.1
      IL_00b8:  stfld      ""int C.<M>d__0.<>1__state""
      IL_00bd:  ldloca.s   V_2
      IL_00bf:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00c4:  pop
      // sequence point: int a = p;
      IL_00c5:  ldloca.s   V_0
      IL_00c7:  ldarg.0
      IL_00c8:  ldarg.0
      IL_00c9:  ldfld      ""int C.<M>d__0.p""
      IL_00ce:  dup
      IL_00cf:  stloc.s    V_4
      IL_00d1:  stfld      ""int C.<M>d__0.<a>5__1""
      IL_00d6:  ldloc.s    V_4
      IL_00d8:  ldtoken    ""int C.<M>d__0.<a>5__1""
      IL_00dd:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_00e2:  nop
      IL_00e3:  ldarg.0
      IL_00e4:  ldfld      ""int C.<M>d__0.<a>5__1""
      IL_00e9:  pop
      // sequence point: F(out var b);
      IL_00ea:  ldarg.0
      IL_00eb:  ldflda     ""int C.<M>d__0.<b>5__2""
      IL_00f0:  call       ""int C.F(out int)""
      IL_00f5:  pop
      IL_00f6:  ldloca.s   V_0
      IL_00f8:  ldarg.0
      IL_00f9:  ldfld      ""int C.<M>d__0.<b>5__2""
      IL_00fe:  ldtoken    ""int C.<M>d__0.<b>5__2""
      IL_0103:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_0108:  nop
      // sequence point: yield return 1;
      IL_0109:  ldarg.0
      IL_010a:  ldc.i4.1
      IL_010b:  stfld      ""int C.<M>d__0.<>2__current""
      IL_0110:  ldarg.0
      IL_0111:  ldc.i4.s   -4
      IL_0113:  dup
      IL_0114:  stloc.1
      IL_0115:  stfld      ""int C.<M>d__0.<>1__state""
      IL_011a:  leave      IL_01b0
      // sequence point: <hidden>
      IL_011f:  ldarg.0
      IL_0120:  ldc.i4.m1
      IL_0121:  dup
      IL_0122:  stloc.1
      IL_0123:  stfld      ""int C.<M>d__0.<>1__state""
      IL_0128:  ldarg.0
      IL_0129:  ldfld      ""bool C.<M>d__0.<>w__disposeMode""
      IL_012e:  brfalse.s  IL_0132
      IL_0130:  leave.s    IL_0186
      // sequence point: int c = b;
      IL_0132:  ldloca.s   V_0
      IL_0134:  ldarg.0
      IL_0135:  ldarg.0
      IL_0136:  ldfld      ""int C.<M>d__0.<b>5__2""
      IL_013b:  dup
      IL_013c:  stloc.s    V_4
      IL_013e:  stfld      ""int C.<M>d__0.<c>5__3""
      IL_0143:  ldloc.s    V_4
      IL_0145:  ldtoken    ""int C.<M>d__0.<c>5__3""
      IL_014a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_014f:  nop
      IL_0150:  ldarg.0
      IL_0151:  ldfld      ""int C.<M>d__0.<c>5__3""
      IL_0156:  pop
      IL_0157:  leave.s    IL_0186
    }
    catch System.Exception
    {
      // sequence point: <hidden>
      IL_0159:  stloc.s    V_5
      IL_015b:  ldarg.0
      IL_015c:  ldc.i4.s   -2
      IL_015e:  stfld      ""int C.<M>d__0.<>1__state""
      IL_0163:  ldarg.0
      IL_0164:  ldc.i4.0
      IL_0165:  stfld      ""int C.<M>d__0.<>2__current""
      IL_016a:  ldarg.0
      IL_016b:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
      IL_0170:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
      IL_0175:  nop
      IL_0176:  ldarg.0
      IL_0177:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
      IL_017c:  ldloc.s    V_5
      IL_017e:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
      IL_0183:  nop
      IL_0184:  leave.s    IL_01bd
    }
    // sequence point: }
    IL_0186:  ldarg.0
    IL_0187:  ldc.i4.s   -2
    IL_0189:  stfld      ""int C.<M>d__0.<>1__state""
    // sequence point: <hidden>
    IL_018e:  ldarg.0
    IL_018f:  ldc.i4.0
    IL_0190:  stfld      ""int C.<M>d__0.<>2__current""
    IL_0195:  ldarg.0
    IL_0196:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<M>d__0.<>t__builder""
    IL_019b:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_01a0:  nop
    IL_01a1:  ldarg.0
    IL_01a2:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_01a7:  ldc.i4.0
    IL_01a8:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
    IL_01ad:  nop
    IL_01ae:  leave.s    IL_01c8
    IL_01b0:  ldarg.0
    IL_01b1:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<M>d__0.<>v__promiseOfValueOrEnd""
    IL_01b6:  ldc.i4.1
    IL_01b7:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
    IL_01bc:  nop
    IL_01bd:  leave.s    IL_01c8
  }
  finally
  {
    // sequence point: <hidden>
    IL_01bf:  ldloca.s   V_0
    IL_01c1:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_01c6:  nop
    IL_01c7:  endfinally
  }
  // sequence point: <hidden>
  IL_01c8:  ret
}");
            expectedOutput = """
Main: Entered
M: Entered state machine #1
M: P'p'[0] = 2
M: L'a' = 2
F: Entered
F: P'a'[0] = 1
F: Returned
M: L'b' = 1
M: Returned
Main: L5 = 1
M: Entered state machine #1
M: L'c' = 1
M: Returned
Main: Returned
""";
            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.UnsafeDebugExe);
            verifier = CompileAndVerify(comp, emitOptions: s_emitOptions,
                expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput), verify: Verification.Skipped);
            verifier.VerifyDiagnostics();

            verifier.VerifyMethodBody("C.<M>d__0.System.Collections.Generic.IAsyncEnumerator<int>.MoveNextAsync()", """
{
  // Code size      292 (0x124)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2,
                bool V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    "System.Collections.Generic.IAsyncEnumerable<int> C.M(int)"
  IL_0005:  ldarg.0
  IL_0006:  ldfld      "ulong C.<M>d__0.<>I"
  IL_000b:  call       "Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineMethodEntry(int, ulong)"
  IL_0010:  stloc.0
  .try
  {
    IL_0011:  ldarg.0
    IL_0012:  ldfld      "int C.<M>d__0.<>1__state"
    IL_0017:  stloc.1
    .try
    {
      IL_0018:  ldloc.1
      IL_0019:  ldc.i4.s   -4
      IL_001b:  beq.s      IL_0026
      IL_001d:  br.s       IL_001f
      IL_001f:  ldloc.1
      IL_0020:  ldc.i4.s   -3
      IL_0022:  beq.s      IL_002b
      IL_0024:  br.s       IL_002d
      IL_0026:  br         IL_00b5
      IL_002b:  br.s       IL_002d
      IL_002d:  ldarg.0
      IL_002e:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_0033:  brfalse.s  IL_003a
      IL_0035:  leave      IL_0106
      IL_003a:  ldarg.0
      IL_003b:  ldc.i4.m1
      IL_003c:  dup
      IL_003d:  stloc.1
      IL_003e:  stfld      "int C.<M>d__0.<>1__state"
      // sequence point: {
      IL_0043:  ldloca.s   V_0
      IL_0045:  ldarg.0
      IL_0046:  ldfld      "int C.<M>d__0.p"
      IL_004b:  ldc.i4.0
      IL_004c:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)"
      IL_0051:  nop
      // sequence point: await Task.FromResult(1);
      IL_0052:  ldc.i4.1
      IL_0053:  call       "System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)"
      IL_0058:  call       "int System.Runtime.CompilerServices.AsyncHelpers.Await<int>(System.Threading.Tasks.Task<int>)"
      IL_005d:  pop
      // sequence point: int a = p;
      IL_005e:  ldloca.s   V_0
      IL_0060:  ldarg.0
      IL_0061:  ldarg.0
      IL_0062:  ldfld      "int C.<M>d__0.p"
      IL_0067:  dup
      IL_0068:  stloc.2
      IL_0069:  stfld      "int C.<M>d__0.<a>5__1"
      IL_006e:  ldloc.2
      IL_006f:  ldtoken    "int C.<M>d__0.<a>5__1"
      IL_0074:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
      IL_0079:  nop
      IL_007a:  ldarg.0
      IL_007b:  ldfld      "int C.<M>d__0.<a>5__1"
      IL_0080:  pop
      // sequence point: F(out var b);
      IL_0081:  ldarg.0
      IL_0082:  ldflda     "int C.<M>d__0.<b>5__2"
      IL_0087:  call       "int C.F(out int)"
      IL_008c:  pop
      IL_008d:  ldloca.s   V_0
      IL_008f:  ldarg.0
      IL_0090:  ldfld      "int C.<M>d__0.<b>5__2"
      IL_0095:  ldtoken    "int C.<M>d__0.<b>5__2"
      IL_009a:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
      IL_009f:  nop
      // sequence point: yield return 1;
      IL_00a0:  ldarg.0
      IL_00a1:  ldc.i4.1
      IL_00a2:  stfld      "int C.<M>d__0.<>2__current"
      IL_00a7:  ldarg.0
      IL_00a8:  ldc.i4.s   -4
      IL_00aa:  dup
      IL_00ab:  stloc.1
      IL_00ac:  stfld      "int C.<M>d__0.<>1__state"
      IL_00b1:  ldc.i4.1
      IL_00b2:  stloc.3
      IL_00b3:  leave.s    IL_0122
      // sequence point: <hidden>
      IL_00b5:  ldarg.0
      IL_00b6:  ldc.i4.m1
      IL_00b7:  dup
      IL_00b8:  stloc.1
      IL_00b9:  stfld      "int C.<M>d__0.<>1__state"
      IL_00be:  ldarg.0
      IL_00bf:  ldfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_00c4:  brfalse.s  IL_00c8
      IL_00c6:  leave.s    IL_0106
      // sequence point: int c = b;
      IL_00c8:  ldloca.s   V_0
      IL_00ca:  ldarg.0
      IL_00cb:  ldarg.0
      IL_00cc:  ldfld      "int C.<M>d__0.<b>5__2"
      IL_00d1:  dup
      IL_00d2:  stloc.2
      IL_00d3:  stfld      "int C.<M>d__0.<c>5__3"
      IL_00d8:  ldloc.2
      IL_00d9:  ldtoken    "int C.<M>d__0.<c>5__3"
      IL_00de:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)"
      IL_00e3:  nop
      IL_00e4:  ldarg.0
      IL_00e5:  ldfld      "int C.<M>d__0.<c>5__3"
      IL_00ea:  pop
      IL_00eb:  ldarg.0
      IL_00ec:  ldc.i4.1
      IL_00ed:  stfld      "bool C.<M>d__0.<>w__disposeMode"
      IL_00f2:  leave.s    IL_0106
    }
    catch System.Exception
    {
      // sequence point: <hidden>
      IL_00f4:  pop
      IL_00f5:  ldarg.0
      IL_00f6:  ldc.i4.s   -2
      IL_00f8:  stfld      "int C.<M>d__0.<>1__state"
      IL_00fd:  ldarg.0
      IL_00fe:  ldc.i4.0
      IL_00ff:  stfld      "int C.<M>d__0.<>2__current"
      IL_0104:  rethrow
    }
    // sequence point: }
    IL_0106:  ldarg.0
    IL_0107:  ldc.i4.s   -2
    IL_0109:  stfld      "int C.<M>d__0.<>1__state"
    // sequence point: <hidden>
    IL_010e:  ldarg.0
    IL_010f:  ldc.i4.0
    IL_0110:  stfld      "int C.<M>d__0.<>2__current"
    IL_0115:  ldc.i4.0
    IL_0116:  stloc.3
    IL_0117:  leave.s    IL_0122
  }
  finally
  {
    // sequence point: <hidden>
    IL_0119:  ldloca.s   V_0
    IL_011b:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()"
    IL_0120:  nop
    IL_0121:  endfinally
  }
  // sequence point: <hidden>
  IL_0122:  ldloc.3
  IL_0123:  ret
}
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Async)]
        public void StateMachine_Lambda_Async()
        {
            var source = WithHelpers(@"
using System;
using System.Threading.Tasks;

class C
{
    static async Task Main() 
    {
        await F(async p =>
        {
            int a = p;
            return await Task.FromResult(1);
        });
    }

    static async Task F(Func<int, Task<int>> t) => await t(2);
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered state machine #1
F: Entered state machine #2
F: P't'[0] = System.Func`2[System.Int32,System.Threading.Tasks.Task`1[System.Int32]]
Main: Entered lambda '<Main>b__0_0' state machine #3
<Main>b__0_0: P'p'[0] = 2
<Main>b__0_0: L'a' = 2
<Main>b__0_0: Returned
F: Returned
Main: Returned
");
            verifier.VerifyMethodBody("C.<>c.<<Main>b__0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      256 (0x100)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                C.<>c.<<Main>b__0_0>d V_5,
                System.Exception V_6)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""System.Threading.Tasks.Task C.Main()""
  IL_0005:  ldtoken    ""System.Threading.Tasks.Task<int> C.<>c.<Main>b__0_0(int)""
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""ulong C.<>c.<<Main>b__0_0>d.<>I""
  IL_0010:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineLambdaEntry(int, int, ulong)""
  IL_0015:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""int C.<>c.<<Main>b__0_0>d.<>1__state""
    IL_001c:  stloc.1
    .try
    {
      // sequence point: <hidden>
      IL_001d:  ldloc.1
      IL_001e:  brfalse.s  IL_0022
      IL_0020:  br.s       IL_0024
      IL_0022:  br.s       IL_0092
      // sequence point: {
      IL_0024:  ldloca.s   V_0
      IL_0026:  ldarg.0
      IL_0027:  ldfld      ""int C.<>c.<<Main>b__0_0>d.p""
      IL_002c:  ldc.i4.0
      IL_002d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
      IL_0032:  nop
      // sequence point: int a = p;
      IL_0033:  ldloca.s   V_0
      IL_0035:  ldarg.0
      IL_0036:  ldarg.0
      IL_0037:  ldfld      ""int C.<>c.<<Main>b__0_0>d.p""
      IL_003c:  dup
      IL_003d:  stloc.3
      IL_003e:  stfld      ""int C.<>c.<<Main>b__0_0>d.<a>5__1""
      IL_0043:  ldloc.3
      IL_0044:  ldtoken    ""int C.<>c.<<Main>b__0_0>d.<a>5__1""
      IL_0049:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_004e:  nop
      IL_004f:  ldarg.0
      IL_0050:  ldfld      ""int C.<>c.<<Main>b__0_0>d.<a>5__1""
      IL_0055:  pop
      // sequence point: return await Task.FromResult(1);
      IL_0056:  ldc.i4.1
      IL_0057:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
      IL_005c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0061:  stloc.s    V_4
      // sequence point: <hidden>
      IL_0063:  ldloca.s   V_4
      IL_0065:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_006a:  brtrue.s   IL_00af
      IL_006c:  ldarg.0
      IL_006d:  ldc.i4.0
      IL_006e:  dup
      IL_006f:  stloc.1
      IL_0070:  stfld      ""int C.<>c.<<Main>b__0_0>d.<>1__state""
      // async: yield
      IL_0075:  ldarg.0
      IL_0076:  ldloc.s    V_4
      IL_0078:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<>c.<<Main>b__0_0>d.<>u__1""
      IL_007d:  ldarg.0
      IL_007e:  stloc.s    V_5
      IL_0080:  ldarg.0
      IL_0081:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<>c.<<Main>b__0_0>d.<>t__builder""
      IL_0086:  ldloca.s   V_4
      IL_0088:  ldloca.s   V_5
      IL_008a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<>c.<<Main>b__0_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<>c.<<Main>b__0_0>d)""
      IL_008f:  nop
      IL_0090:  leave.s    IL_00f4
      // async: resume
      IL_0092:  ldarg.0
      IL_0093:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<>c.<<Main>b__0_0>d.<>u__1""
      IL_0098:  stloc.s    V_4
      IL_009a:  ldarg.0
      IL_009b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<>c.<<Main>b__0_0>d.<>u__1""
      IL_00a0:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00a6:  ldarg.0
      IL_00a7:  ldc.i4.m1
      IL_00a8:  dup
      IL_00a9:  stloc.1
      IL_00aa:  stfld      ""int C.<>c.<<Main>b__0_0>d.<>1__state""
      IL_00af:  ldarg.0
      IL_00b0:  ldloca.s   V_4
      IL_00b2:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00b7:  stfld      ""int C.<>c.<<Main>b__0_0>d.<>s__2""
      IL_00bc:  ldarg.0
      IL_00bd:  ldfld      ""int C.<>c.<<Main>b__0_0>d.<>s__2""
      IL_00c2:  stloc.2
      IL_00c3:  leave.s    IL_00df
    }
    catch System.Exception
    {
      // sequence point: <hidden>
      IL_00c5:  stloc.s    V_6
      IL_00c7:  ldarg.0
      IL_00c8:  ldc.i4.s   -2
      IL_00ca:  stfld      ""int C.<>c.<<Main>b__0_0>d.<>1__state""
      IL_00cf:  ldarg.0
      IL_00d0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<>c.<<Main>b__0_0>d.<>t__builder""
      IL_00d5:  ldloc.s    V_6
      IL_00d7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
      IL_00dc:  nop
      IL_00dd:  leave.s    IL_00f4
    }
    // sequence point: }
    IL_00df:  ldarg.0
    IL_00e0:  ldc.i4.s   -2
    IL_00e2:  stfld      ""int C.<>c.<<Main>b__0_0>d.<>1__state""
    // sequence point: <hidden>
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> C.<>c.<<Main>b__0_0>d.<>t__builder""
    IL_00ed:  ldloc.2
    IL_00ee:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
    IL_00f3:  nop
    IL_00f4:  leave.s    IL_00ff
  }
  finally
  {
    // sequence point: <hidden>
    IL_00f6:  ldloca.s   V_0
    IL_00f8:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_00fd:  nop
    IL_00fe:  endfinally
  }
  // sequence point: <hidden>
  IL_00ff:  ret
}
");
        }

        [Fact]
        public void StateMachine_LocalFunction_Iterator()
        {
            var source = WithHelpers(@"
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        IEnumerable<int> f(int p)
        {
            int a = p;
            yield return 1;       
        }
        
        foreach (var n in M(f))
        {
        }
    }

    static IEnumerable<int> M(Func<int, IEnumerable<int>> p)
        => p(2);
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
M: Entered
M: P'p'[0] = System.Func`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]
M: Returned
Main: Entered lambda '<Main>g__f|0_0' state machine #1
<Main>g__f|0_0: P'p'[0] = 2
<Main>g__f|0_0: L'a' = 2
<Main>g__f|0_0: Returned
Main: L2 = 1
Main: Entered lambda '<Main>g__f|0_0' state machine #1
<Main>g__f|0_0: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.<<Main>g__f|0_0>d.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      145 (0x91)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                bool V_2,
                int V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""System.Collections.Generic.IEnumerable<int> C.<Main>g__f|0_0(int)""
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""ulong C.<<Main>g__f|0_0>d.<>I""
  IL_0010:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineLambdaEntry(int, int, ulong)""
  IL_0015:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    IL_001c:  stloc.1
    IL_001d:  ldloc.1
    IL_001e:  brfalse.s  IL_0028
    IL_0020:  br.s       IL_0022
    IL_0022:  ldloc.1
    IL_0023:  ldc.i4.1
    IL_0024:  beq.s      IL_002a
    IL_0026:  br.s       IL_002c
    IL_0028:  br.s       IL_0030
    IL_002a:  br.s       IL_007b
    IL_002c:  ldc.i4.0
    IL_002d:  stloc.2
    IL_002e:  leave.s    IL_008f
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4.m1
    IL_0032:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    // sequence point: {
    IL_0037:  ldloca.s   V_0
    IL_0039:  ldarg.0
    IL_003a:  ldfld      ""int C.<<Main>g__f|0_0>d.p""
    IL_003f:  ldc.i4.0
    IL_0040:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0045:  nop
    // sequence point: int a = p;
    IL_0046:  ldloca.s   V_0
    IL_0048:  ldarg.0
    IL_0049:  ldarg.0
    IL_004a:  ldfld      ""int C.<<Main>g__f|0_0>d.p""
    IL_004f:  dup
    IL_0050:  stloc.3
    IL_0051:  stfld      ""int C.<<Main>g__f|0_0>d.<a>5__1""
    IL_0056:  ldloc.3
    IL_0057:  ldtoken    ""int C.<<Main>g__f|0_0>d.<a>5__1""
    IL_005c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0061:  nop
    IL_0062:  ldarg.0
    IL_0063:  ldfld      ""int C.<<Main>g__f|0_0>d.<a>5__1""
    IL_0068:  pop
    // sequence point: yield return 1;
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.1
    IL_006b:  stfld      ""int C.<<Main>g__f|0_0>d.<>2__current""
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.1
    IL_0072:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    IL_0077:  ldc.i4.1
    IL_0078:  stloc.2
    IL_0079:  leave.s    IL_008f
    // sequence point: <hidden>
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    // sequence point: }
    IL_0082:  ldc.i4.0
    IL_0083:  stloc.2
    IL_0084:  leave.s    IL_008f
  }
  finally
  {
    // sequence point: <hidden>
    IL_0086:  ldloca.s   V_0
    IL_0088:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_008d:  nop
    IL_008e:  endfinally
  }
  // sequence point: <hidden>
  IL_008f:  ldloc.2
  IL_0090:  ret
}");
        }

        [Fact, CompilerTrait(CompilerFeature.AsyncStreams)]
        public void StateMachine_LocalFunction_AsyncIterator()
        {
            var source = WithHelpers(@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async Task Main()
    {
        async IAsyncEnumerable<int> f(int p)
        {
            await Task.FromResult(1);
            int a = p;
            yield return 1;       
        }
        
        await foreach (var n in M(f))
        {
        }
    }

    static IAsyncEnumerable<int> M(Func<int, IAsyncEnumerable<int>> p)
        => p(2);
}
");

            var expectedOutput = @"
Main: Entered state machine #1
M: Entered
M: P'p'[0] = System.Func`2[System.Int32,System.Collections.Generic.IAsyncEnumerable`1[System.Int32]]
M: Returned
Main: Entered lambda '<Main>g__f|0_0' state machine #2
<Main>g__f|0_0: P'p'[0] = 2
<Main>g__f|0_0: L'a' = 2
<Main>g__f|0_0: Returned
Main: L'n' = 1
Main: Entered lambda '<Main>g__f|0_0' state machine #2
<Main>g__f|0_0: Returned
Main: Returned
";
            var verifier = CompileAndVerify(source, expectedOutput: expectedOutput);

            verifier.VerifyMethodBody("C.<<Main>g__f|0_0>d.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      391 (0x187)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                C.<<Main>g__f|0_0>d V_3,
                int V_4,
                System.Exception V_5)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""System.Threading.Tasks.Task C.Main()""
  IL_0005:  ldtoken    ""System.Collections.Generic.IAsyncEnumerable<int> C.<Main>g__f|0_0(int)""
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""ulong C.<<Main>g__f|0_0>d.<>I""
  IL_0010:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogStateMachineLambdaEntry(int, int, ulong)""
  IL_0015:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0016:  ldarg.0
    IL_0017:  ldfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    IL_001c:  stloc.1
    .try
    {
      // sequence point: <hidden>
      IL_001d:  ldloc.1
      IL_001e:  ldc.i4.s   -4
      IL_0020:  sub
      IL_0021:  switch    (
        IL_003c,
        IL_0041,
        IL_0045,
        IL_0045,
        IL_0043)
      IL_003a:  br.s       IL_0045
      IL_003c:  br         IL_0102
      IL_0041:  br.s       IL_0045
      IL_0043:  br.s       IL_00a6
      IL_0045:  ldarg.0
      IL_0046:  ldfld      ""bool C.<<Main>g__f|0_0>d.<>w__disposeMode""
      IL_004b:  brfalse.s  IL_0052
      IL_004d:  leave      IL_0144
      IL_0052:  ldarg.0
      IL_0053:  ldc.i4.m1
      IL_0054:  dup
      IL_0055:  stloc.1
      IL_0056:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      // sequence point: {
      IL_005b:  ldloca.s   V_0
      IL_005d:  ldarg.0
      IL_005e:  ldfld      ""int C.<<Main>g__f|0_0>d.p""
      IL_0063:  ldc.i4.0
      IL_0064:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
      IL_0069:  nop
      // sequence point: await Task.FromResult(1);
      IL_006a:  ldc.i4.1
      IL_006b:  call       ""System.Threading.Tasks.Task<int> System.Threading.Tasks.Task.FromResult<int>(int)""
      IL_0070:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
      IL_0075:  stloc.2
      // sequence point: <hidden>
      IL_0076:  ldloca.s   V_2
      IL_0078:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
      IL_007d:  brtrue.s   IL_00c2
      IL_007f:  ldarg.0
      IL_0080:  ldc.i4.0
      IL_0081:  dup
      IL_0082:  stloc.1
      IL_0083:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      // async: yield
      IL_0088:  ldarg.0
      IL_0089:  ldloc.2
      IL_008a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<<Main>g__f|0_0>d.<>u__1""
      IL_008f:  ldarg.0
      IL_0090:  stloc.3
      IL_0091:  ldarg.0
      IL_0092:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<Main>g__f|0_0>d.<>t__builder""
      IL_0097:  ldloca.s   V_2
      IL_0099:  ldloca.s   V_3
      IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, C.<<Main>g__f|0_0>d>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref C.<<Main>g__f|0_0>d)""
      IL_00a0:  nop
      IL_00a1:  leave      IL_017b
      // async: resume
      IL_00a6:  ldarg.0
      IL_00a7:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<<Main>g__f|0_0>d.<>u__1""
      IL_00ac:  stloc.2
      IL_00ad:  ldarg.0
      IL_00ae:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> C.<<Main>g__f|0_0>d.<>u__1""
      IL_00b3:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
      IL_00b9:  ldarg.0
      IL_00ba:  ldc.i4.m1
      IL_00bb:  dup
      IL_00bc:  stloc.1
      IL_00bd:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      IL_00c2:  ldloca.s   V_2
      IL_00c4:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
      IL_00c9:  pop
      // sequence point: int a = p;
      IL_00ca:  ldloca.s   V_0
      IL_00cc:  ldarg.0
      IL_00cd:  ldarg.0
      IL_00ce:  ldfld      ""int C.<<Main>g__f|0_0>d.p""
      IL_00d3:  dup
      IL_00d4:  stloc.s    V_4
      IL_00d6:  stfld      ""int C.<<Main>g__f|0_0>d.<a>5__1""
      IL_00db:  ldloc.s    V_4
      IL_00dd:  ldtoken    ""int C.<<Main>g__f|0_0>d.<a>5__1""
      IL_00e2:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_00e7:  nop
      IL_00e8:  ldarg.0
      IL_00e9:  ldfld      ""int C.<<Main>g__f|0_0>d.<a>5__1""
      IL_00ee:  pop
      // sequence point: yield return 1;
      IL_00ef:  ldarg.0
      IL_00f0:  ldc.i4.1
      IL_00f1:  stfld      ""int C.<<Main>g__f|0_0>d.<>2__current""
      IL_00f6:  ldarg.0
      IL_00f7:  ldc.i4.s   -4
      IL_00f9:  dup
      IL_00fa:  stloc.1
      IL_00fb:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      IL_0100:  leave.s    IL_016e
      // sequence point: <hidden>
      IL_0102:  ldarg.0
      IL_0103:  ldc.i4.m1
      IL_0104:  dup
      IL_0105:  stloc.1
      IL_0106:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      IL_010b:  ldarg.0
      IL_010c:  ldfld      ""bool C.<<Main>g__f|0_0>d.<>w__disposeMode""
      IL_0111:  brfalse.s  IL_0115
      IL_0113:  leave.s    IL_0144
      // sequence point: <hidden>
      IL_0115:  leave.s    IL_0144
    }
    catch System.Exception
    {
      // sequence point: <hidden>
      IL_0117:  stloc.s    V_5
      IL_0119:  ldarg.0
      IL_011a:  ldc.i4.s   -2
      IL_011c:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
      IL_0121:  ldarg.0
      IL_0122:  ldc.i4.0
      IL_0123:  stfld      ""int C.<<Main>g__f|0_0>d.<>2__current""
      IL_0128:  ldarg.0
      IL_0129:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<Main>g__f|0_0>d.<>t__builder""
      IL_012e:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
      IL_0133:  nop
      IL_0134:  ldarg.0
      IL_0135:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<Main>g__f|0_0>d.<>v__promiseOfValueOrEnd""
      IL_013a:  ldloc.s    V_5
      IL_013c:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetException(System.Exception)""
      IL_0141:  nop
      IL_0142:  leave.s    IL_017b
    }
    // sequence point: }
    IL_0144:  ldarg.0
    IL_0145:  ldc.i4.s   -2
    IL_0147:  stfld      ""int C.<<Main>g__f|0_0>d.<>1__state""
    // sequence point: <hidden>
    IL_014c:  ldarg.0
    IL_014d:  ldc.i4.0
    IL_014e:  stfld      ""int C.<<Main>g__f|0_0>d.<>2__current""
    IL_0153:  ldarg.0
    IL_0154:  ldflda     ""System.Runtime.CompilerServices.AsyncIteratorMethodBuilder C.<<Main>g__f|0_0>d.<>t__builder""
    IL_0159:  call       ""void System.Runtime.CompilerServices.AsyncIteratorMethodBuilder.Complete()""
    IL_015e:  nop
    IL_015f:  ldarg.0
    IL_0160:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<Main>g__f|0_0>d.<>v__promiseOfValueOrEnd""
    IL_0165:  ldc.i4.0
    IL_0166:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
    IL_016b:  nop
    IL_016c:  leave.s    IL_0186
    IL_016e:  ldarg.0
    IL_016f:  ldflda     ""System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool> C.<<Main>g__f|0_0>d.<>v__promiseOfValueOrEnd""
    IL_0174:  ldc.i4.1
    IL_0175:  call       ""void System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore<bool>.SetResult(bool)""
    IL_017a:  nop
    IL_017b:  leave.s    IL_0186
  }
  finally
  {
    // sequence point: <hidden>
    IL_017d:  ldloca.s   V_0
    IL_017f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0184:  nop
    IL_0185:  endfinally
  }
  // sequence point: <hidden>
  IL_0186:  ret
}");

            expectedOutput = """
Main: Entered
M: Entered
M: P'p'[0] = System.Func`2[System.Int32,System.Collections.Generic.IAsyncEnumerable`1[System.Int32]]
M: Returned
Main: Entered lambda '<Main>g__f|0_0' state machine #1
<Main>g__f|0_0: P'p'[0] = 2
<Main>g__f|0_0: L'a' = 2
<Main>g__f|0_0: Returned
Main: L5 = 1
Main: Entered lambda '<Main>g__f|0_0' state machine #1
<Main>g__f|0_0: Returned
Main: Returned
""";
            var comp = CreateRuntimeAsyncCompilation(source, options: TestOptions.UnsafeDebugExe);
            verifier = CompileAndVerify(comp, expectedOutput: RuntimeAsyncTestHelpers.ExpectedOutput(expectedOutput),
                emitOptions: s_emitOptions, verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void LocalFunctions()
        {
            var source = WithHelpers(@"
using System;

class C
{
    static void Main()
    {
        int a = 1;
        {
            int b = 2;
            {
                int c = 3;
                int f() => c += 1;
                f();
            }

            int g() => a += b;
            g();
        }
    }
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L'a' = 1
Main: L'b' = 2
Main: L'c' = 3
Main: Entered lambda '<Main>g__f|0_1'
<Main>g__f|0_1: L'c' = 4
<Main>g__f|0_1: Returned
Main: Entered lambda '<Main>g__g|0_0'
<Main>g__g|0_0: L'a' = 3
<Main>g__g|0_0: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.<Main>g__f|0_1", @"
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""int C.<Main>g__f|0_1(ref C.<>c__DisplayClass0_2)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: c += 1
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0019:  ldc.i4.1
    IL_001a:  add
    IL_001b:  dup
    IL_001c:  stloc.1
    IL_001d:  stfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0022:  ldloc.1
    IL_0023:  ldtoken    ""int C.<>c__DisplayClass0_2.c""
    IL_0028:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_002d:  nop
    IL_002e:  ldarg.0
    IL_002f:  ldfld      ""int C.<>c__DisplayClass0_2.c""
    IL_0034:  stloc.2
    IL_0035:  leave.s    IL_0040
  }
  finally
  {
    // sequence point: <hidden>
    IL_0037:  ldloca.s   V_0
    IL_0039:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003e:  nop
    IL_003f:  endfinally
  }
  // sequence point: <hidden>
  IL_0040:  ldloc.2
  IL_0041:  ret
}
");
            verifier.VerifyMethodBody("C.<Main>g__g|0_0", @"
{
  // Code size       71 (0x47)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""int C.<Main>g__g|0_0(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    // sequence point: a += b
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0019:  ldarg.1
    IL_001a:  ldfld      ""int C.<>c__DisplayClass0_1.b""
    IL_001f:  add
    IL_0020:  dup
    IL_0021:  stloc.1
    IL_0022:  stfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0027:  ldloc.1
    IL_0028:  ldtoken    ""int C.<>c__DisplayClass0_0.a""
    IL_002d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0032:  nop
    IL_0033:  ldarg.0
    IL_0034:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0039:  stloc.2
    IL_003a:  leave.s    IL_0045
  }
  finally
  {
    // sequence point: <hidden>
    IL_003c:  ldloca.s   V_0
    IL_003e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0043:  nop
    IL_0044:  endfinally
  }
  // sequence point: <hidden>
  IL_0045:  ldloc.2
  IL_0046:  ret
}");
            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      120 (0x78)
  .maxstack  4
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                C.<>c__DisplayClass0_0 V_1, //CS$<>8__locals0
                int V_2,
                C.<>c__DisplayClass0_1 V_3, //CS$<>8__locals1
                C.<>c__DisplayClass0_2 V_4) //CS$<>8__locals2
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  nop
    // sequence point: int a = 1;
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldloca.s   V_1
    IL_0010:  ldc.i4.1
    IL_0011:  dup
    IL_0012:  stloc.2
    IL_0013:  stfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0018:  ldloc.2
    IL_0019:  ldtoken    ""int C.<>c__DisplayClass0_0.a""
    IL_001e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0023:  nop
    // sequence point: {
    IL_0024:  nop
    // sequence point: int b = 2;
    IL_0025:  ldloca.s   V_0
    IL_0027:  ldloca.s   V_3
    IL_0029:  ldc.i4.2
    IL_002a:  dup
    IL_002b:  stloc.2
    IL_002c:  stfld      ""int C.<>c__DisplayClass0_1.b""
    IL_0031:  ldloc.2
    IL_0032:  ldtoken    ""int C.<>c__DisplayClass0_1.b""
    IL_0037:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_003c:  nop
    // sequence point: {
    IL_003d:  nop
    // sequence point: int c = 3;
    IL_003e:  ldloca.s   V_0
    IL_0040:  ldloca.s   V_4
    IL_0042:  ldc.i4.3
    IL_0043:  dup
    IL_0044:  stloc.2
    IL_0045:  stfld      ""int C.<>c__DisplayClass0_2.c""
    IL_004a:  ldloc.2
    IL_004b:  ldtoken    ""int C.<>c__DisplayClass0_2.c""
    IL_0050:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0055:  nop
    IL_0056:  nop
    // sequence point: f();
    IL_0057:  ldloca.s   V_4
    IL_0059:  call       ""int C.<Main>g__f|0_1(ref C.<>c__DisplayClass0_2)""
    IL_005e:  pop
    // sequence point: }
    IL_005f:  nop
    IL_0060:  nop
    // sequence point: g();
    IL_0061:  ldloca.s   V_1
    IL_0063:  ldloca.s   V_3
    IL_0065:  call       ""int C.<Main>g__g|0_0(ref C.<>c__DisplayClass0_0, ref C.<>c__DisplayClass0_1)""
    IL_006a:  pop
    // sequence point: }
    IL_006b:  nop
    // sequence point: }
    IL_006c:  leave.s    IL_0077
  }
  finally
  {
    // sequence point: <hidden>
    IL_006e:  ldloca.s   V_0
    IL_0070:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0075:  nop
    IL_0076:  endfinally
  }
  // sequence point: }
  IL_0077:  ret
}
");
        }

        [Fact]
        public void Queries()
        {
            var source = WithHelpers(@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        int a = 1;
        {
            int b = 2;
            var x = from item in new[] { 10, 20 }
                    let c = a + b
                    select item + (a = b);
            x.ToArray();
        }
    }
}
");

            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: L'a' = 1
Main: L'b' = 2
Main: L4 = System.Linq.Enumerable+ArraySelectIterator`2[System.Int32,System.Int32]
Main: Entered lambda '<Main>b__0'
<Main>b__0: P'item'[0] = 10
<Main>b__0: Returned
Main: Entered lambda '<Main>b__1'
<Main>b__1: P'<>h__TransparentIdentifier0'[0] = { item = 10, c = 3 }
<Main>b__1: L'a' = 2
<Main>b__1: Returned
Main: Entered lambda '<Main>b__0'
<Main>b__0: P'item'[0] = 20
<Main>b__0: Returned
Main: Entered lambda '<Main>b__1'
<Main>b__1: P'<>h__TransparentIdentifier0'[0] = { item = 20, c = 4 }
<Main>b__1: L'a' = 2
<Main>b__1: Returned
Main: Returned
");

            verifier.VerifyMethodBody("C.<>c__DisplayClass0_1.<Main>b__1", @"
 {
  // Code size       91 (0x5b)
  .maxstack  5
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1,
                int V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""int C.<>c__DisplayClass0_1.<Main>b__1(<anonymous type: int item, int c>)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0019:  nop
    // sequence point: item + (a = b)
    IL_001a:  ldarg.1
    IL_001b:  callvirt   ""int <>f__AnonymousType0<int, int>.item.get""
    IL_0020:  ldloca.s   V_0
    IL_0022:  ldarg.0
    IL_0023:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0028:  ldarg.0
    IL_0029:  ldfld      ""int C.<>c__DisplayClass0_1.b""
    IL_002e:  dup
    IL_002f:  stloc.1
    IL_0030:  stfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0035:  ldloc.1
    IL_0036:  ldtoken    ""int C.<>c__DisplayClass0_0.a""
    IL_003b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0040:  nop
    IL_0041:  ldarg.0
    IL_0042:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0047:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_004c:  add
    IL_004d:  stloc.2
    IL_004e:  leave.s    IL_0059
  }
  finally
  {
    // sequence point: <hidden>
    IL_0050:  ldloca.s   V_0
    IL_0052:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0057:  nop
    IL_0058:  endfinally
  }
  // sequence point: <hidden>
  IL_0059:  ldloc.2
  IL_005a:  ret
}");

            verifier.VerifyMethodBody("C.<>c__DisplayClass0_1.<Main>b__0", @"
{
  // Code size       64 (0x40)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                <>f__AnonymousType0<int, int> V_1)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main()""
  IL_0005:  ldtoken    ""<anonymous type: int item, int c> C.<>c__DisplayClass0_1.<Main>b__0(int)""
  IL_000a:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)""
  IL_000f:  stloc.0
  .try
  {
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)""
    IL_0019:  nop
    // sequence point: a + b
    IL_001a:  ldarg.1
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""C.<>c__DisplayClass0_0 C.<>c__DisplayClass0_1.CS$<>8__locals1""
    IL_0021:  ldfld      ""int C.<>c__DisplayClass0_0.a""
    IL_0026:  ldarg.0
    IL_0027:  ldfld      ""int C.<>c__DisplayClass0_1.b""
    IL_002c:  add
    IL_002d:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
    IL_0032:  stloc.1
    IL_0033:  leave.s    IL_003e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0035:  ldloca.s   V_0
    IL_0037:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_003c:  nop
    IL_003d:  endfinally
  }
  // sequence point: <hidden>
  IL_003e:  ldloc.1
  IL_003f:  ret
}");
        }

        [Fact]
        public void ExpressionLambdas()
        {
            var source = WithHelpers(@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> expression = a => a + 1;
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
<Main>$: L1 = a => (a + 1)
<Main>$: Returned
");
            verifier.VerifyMethodBody("<top-level-statements-entry-point>", @"
{
  // Code size      107 (0x6b)
  .maxstack  6
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                System.Linq.Expressions.Expression<System.Func<int, int>> V_1, //expression
                System.Linq.Expressions.ParameterExpression V_2)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""<top-level-statements-entry-point>""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    // sequence point: Expression<Func<int, int>> expression = a => a + 1;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldtoken    ""int""
    IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_0021:  ldstr      ""a""
    IL_0026:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
    IL_002b:  stloc.2
    IL_002c:  ldloc.2
    IL_002d:  ldc.i4.1
    IL_002e:  box        ""int""
    IL_0033:  ldtoken    ""int""
    IL_0038:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
    IL_003d:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
    IL_0042:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression.Add(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression)""
    IL_0047:  ldc.i4.1
    IL_0048:  newarr     ""System.Linq.Expressions.ParameterExpression""
    IL_004d:  dup
    IL_004e:  ldc.i4.0
    IL_004f:  ldloc.2
    IL_0050:  stelem.ref
    IL_0051:  call       ""System.Linq.Expressions.Expression<System.Func<int, int>> System.Linq.Expressions.Expression.Lambda<System.Func<int, int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
    IL_0056:  dup
    IL_0057:  stloc.1
    IL_0058:  ldc.i4.1
    IL_0059:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
    IL_005e:  nop
    IL_005f:  leave.s    IL_006a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0061:  ldloca.s   V_0
    IL_0063:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0068:  nop
    IL_0069:  endfinally
  }
  // sequence point: <hidden>
  IL_006a:  ret
}
");
        }

        [Fact]
        public void TopLevelCode()
        {
            var source = WithHelpers(@"
int a = 1;
int b = 2;
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
<Main>$: L1 = 1
<Main>$: L2 = 2
<Main>$: Returned
");
            verifier.VerifyMethodBody("<top-level-statements-entry-point>", @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                int V_1, //a
                int V_2) //b
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""<top-level-statements-entry-point>""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    // sequence point: int a = 1;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldc.i4.1
    IL_0018:  dup
    IL_0019:  stloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_0020:  nop
    // sequence point: int b = 2;
    IL_0021:  ldloca.s   V_0
    IL_0023:  ldc.i4.2
    IL_0024:  dup
    IL_0025:  stloc.2
    IL_0026:  ldc.i4.2
    IL_0027:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
    IL_002c:  nop
    IL_002d:  leave.s    IL_0038
  }
  finally
  {
    // sequence point: <hidden>
    IL_002f:  ldloca.s   V_0
    IL_0031:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0036:  nop
    IL_0037:  endfinally
  }
  // sequence point: <hidden>
  IL_0038:  ret
}
");
        }

        [Fact]
        public void ExceptionHandler_CatchAll()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        string s;
        try
        {
            s = args[0];
        }
        catch
        {
            s = ""error"";
        }
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: P'args'[0] = System.String[]
Main: L1 = error
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
 {
  // Code size       72 (0x48)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                string V_1) //s
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    .try
    {
      // sequence point: {
      IL_0015:  nop
      // sequence point: s = args[0];
      IL_0016:  ldloca.s   V_0
      IL_0018:  ldarg.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.ref
      IL_001b:  dup
      IL_001c:  stloc.1
      IL_001d:  ldc.i4.1
      IL_001e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0023:  nop
      // sequence point: }
      IL_0024:  nop
      IL_0025:  leave.s    IL_003c
    }
    catch object
    {
      // sequence point: catch
      IL_0027:  pop
      // sequence point: {
      IL_0028:  nop
      // sequence point: s = ""error"";
      IL_0029:  ldloca.s   V_0
      IL_002b:  ldstr      ""error""
      IL_0030:  dup
      IL_0031:  stloc.1
      IL_0032:  ldc.i4.1
      IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0038:  nop
      // sequence point: }
      IL_0039:  nop
      IL_003a:  leave.s    IL_003c
    }
    // sequence point: }
    IL_003c:  leave.s    IL_0047
  }
  finally
  {
    // sequence point: <hidden>
    IL_003e:  ldloca.s   V_0
    IL_0040:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0045:  nop
    IL_0046:  endfinally
  }
  // sequence point: }
  IL_0047:  ret
}");
        }

        [Fact]
        public void ExceptionHandler_CatchType()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        string s;
        try
        {
            s = args[0];
        }
        catch (System.Exception)
        {
            s = ""error"";
        }
    }
}
");
            var verifier = CompileAndVerify(source);

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                string V_1) //s
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    .try
    {
      // sequence point: {
      IL_0015:  nop
      // sequence point: s = args[0];
      IL_0016:  ldloca.s   V_0
      IL_0018:  ldarg.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.ref
      IL_001b:  dup
      IL_001c:  stloc.1
      IL_001d:  ldc.i4.1
      IL_001e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0023:  nop
      // sequence point: }
      IL_0024:  nop
      IL_0025:  leave.s    IL_003c
    }
    catch System.Exception
    {
      // sequence point: catch (System.Exception)
      IL_0027:  pop
      // sequence point: {
      IL_0028:  nop
      // sequence point: s = ""error"";
      IL_0029:  ldloca.s   V_0
      IL_002b:  ldstr      ""error""
      IL_0030:  dup
      IL_0031:  stloc.1
      IL_0032:  ldc.i4.1
      IL_0033:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0038:  nop
      // sequence point: }
      IL_0039:  nop
      IL_003a:  leave.s    IL_003c
    }
    // sequence point: }
    IL_003c:  leave.s    IL_0047
  }
  finally
  {
    // sequence point: <hidden>
    IL_003e:  ldloca.s   V_0
    IL_0040:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0045:  nop
    IL_0046:  endfinally
  }
  // sequence point: }
  IL_0047:  ret
}");
        }

        [Fact]
        public void ExceptionHandler_CatchTypeWithVariable()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        string s;
        try
        {
            s = args[0];
        }
        catch (System.Exception e)
        {
            s = ""error"";
        }
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: P'args'[0] = System.String[]
Main: L2 = System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at C.Main(String[] args)
Main: L1 = error
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                string V_1, //s
                System.Exception V_2) //e
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    .try
    {
      // sequence point: {
      IL_0015:  nop
      // sequence point: s = args[0];
      IL_0016:  ldloca.s   V_0
      IL_0018:  ldarg.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.ref
      IL_001b:  dup
      IL_001c:  stloc.1
      IL_001d:  ldc.i4.1
      IL_001e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0023:  nop
      // sequence point: }
      IL_0024:  nop
      IL_0025:  leave.s    IL_0046
    }
    catch System.Exception
    {
      // sequence point: catch (System.Exception e)
      IL_0027:  stloc.2
      IL_0028:  ldloca.s   V_0
      IL_002a:  ldloc.2
      IL_002b:  ldc.i4.2
      IL_002c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
      IL_0031:  nop
      // sequence point: {
      IL_0032:  nop
      // sequence point: s = ""error"";
      IL_0033:  ldloca.s   V_0
      IL_0035:  ldstr      ""error""
      IL_003a:  dup
      IL_003b:  stloc.1
      IL_003c:  ldc.i4.1
      IL_003d:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0042:  nop
      // sequence point: }
      IL_0043:  nop
      IL_0044:  leave.s    IL_0046
    }
    // sequence point: }
    IL_0046:  leave.s    IL_0051
  }
  finally
  {
    // sequence point: <hidden>
    IL_0048:  ldloca.s   V_0
    IL_004a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_004f:  nop
    IL_0050:  endfinally
  }
  // sequence point: }
  IL_0051:  ret
}
");
        }

        [Fact]
        public void ExceptionHandler_CatchTypeWithVariableAndLocalsInCatchBlock()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        string s;
        try
        {
            s = args[0];
        }
        catch (System.Exception e)
        {
            int a = 1;
            int b = 2;
        }
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: P'args'[0] = System.String[]
Main: L2 = System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at C.Main(String[] args)
Main: L3 = 1
Main: L4 = 2
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size       91 (0x5b)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                string V_1, //s
                System.Exception V_2, //e
                int V_3, //a
                int V_4) //b
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    .try
    {
      // sequence point: {
      IL_0015:  nop
      // sequence point: s = args[0];
      IL_0016:  ldloca.s   V_0
      IL_0018:  ldarg.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.ref
      IL_001b:  dup
      IL_001c:  stloc.1
      IL_001d:  ldc.i4.1
      IL_001e:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0023:  nop
      // sequence point: }
      IL_0024:  nop
      IL_0025:  leave.s    IL_004f
    }
    catch System.Exception
    {
      // sequence point: catch (System.Exception e)
      IL_0027:  stloc.2
      IL_0028:  ldloca.s   V_0
      IL_002a:  ldloc.2
      IL_002b:  ldc.i4.2
      IL_002c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
      IL_0031:  nop
      // sequence point: {
      IL_0032:  nop
      // sequence point: int a = 1;
      IL_0033:  ldloca.s   V_0
      IL_0035:  ldc.i4.1
      IL_0036:  dup
      IL_0037:  stloc.3
      IL_0038:  ldc.i4.3
      IL_0039:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_003e:  nop
      // sequence point: int b = 2;
      IL_003f:  ldloca.s   V_0
      IL_0041:  ldc.i4.2
      IL_0042:  dup
      IL_0043:  stloc.s    V_4
      IL_0045:  ldc.i4.4
      IL_0046:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(uint, int)""
      IL_004b:  nop
      // sequence point: }
      IL_004c:  nop
      IL_004d:  leave.s    IL_004f
    }
    // sequence point: }
    IL_004f:  leave.s    IL_005a
  }
  finally
  {
    // sequence point: <hidden>
    IL_0051:  ldloca.s   V_0
    IL_0053:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0058:  nop
    IL_0059:  endfinally
  }
  // sequence point: }
  IL_005a:  ret
}
");
        }

        [Fact]
        public void ExceptionHandler_CatchTypeWithFilter()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        bool b = false;
        string s;
        try
        {
            s = args[0];
        }
        catch (System.Exception e) when (b = true)
        {
            s = ""error"";
        }
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: P'args'[0] = System.String[]
Main: L1 = False
Main: L3 = System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at C.Main(String[] args)
Main: L1 = True
Main: L2 = error
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      129 (0x81)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                bool V_1, //b
                string V_2, //s
                System.Exception V_3, //e
                bool V_4)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    // sequence point: bool b = false;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldc.i4.0
    IL_0018:  dup
    IL_0019:  stloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(bool, int)""
    IL_0020:  nop
    .try
    {
      // sequence point: {
      IL_0021:  nop
      // sequence point: s = args[0];
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldarg.0
      IL_0025:  ldc.i4.0
      IL_0026:  ldelem.ref
      IL_0027:  dup
      IL_0028:  stloc.2
      IL_0029:  ldc.i4.2
      IL_002a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_002f:  nop
      // sequence point: }
      IL_0030:  nop
      IL_0031:  leave.s    IL_0075
    }
    filter
    {
      // sequence point: <hidden>
      IL_0033:  isinst     ""System.Exception""
      IL_0038:  dup
      IL_0039:  brtrue.s   IL_003f
      IL_003b:  pop
      IL_003c:  ldc.i4.0
      IL_003d:  br.s       IL_005e
      IL_003f:  stloc.3
      IL_0040:  ldloca.s   V_0
      IL_0042:  ldloc.3
      IL_0043:  ldc.i4.3
      IL_0044:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(object, int)""
      IL_0049:  nop
      // sequence point: when (b = true)
      IL_004a:  ldloca.s   V_0
      IL_004c:  ldc.i4.1
      IL_004d:  dup
      IL_004e:  stloc.1
      IL_004f:  ldc.i4.1
      IL_0050:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(bool, int)""
      IL_0055:  nop
      IL_0056:  ldloc.1
      IL_0057:  stloc.s    V_4
      // sequence point: <hidden>
      IL_0059:  ldloc.s    V_4
      IL_005b:  ldc.i4.0
      IL_005c:  cgt.un
      IL_005e:  endfilter
    }  // end filter
    {  // handler
      // sequence point: <hidden>
      IL_0060:  pop
      // sequence point: {
      IL_0061:  nop
      // sequence point: s = ""error"";
      IL_0062:  ldloca.s   V_0
      IL_0064:  ldstr      ""error""
      IL_0069:  dup
      IL_006a:  stloc.2
      IL_006b:  ldc.i4.2
      IL_006c:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0071:  nop
      // sequence point: }
      IL_0072:  nop
      IL_0073:  leave.s    IL_0075
    }
    // sequence point: }
    IL_0075:  leave.s    IL_0080
  }
  finally
  {
    // sequence point: <hidden>
    IL_0077:  ldloca.s   V_0
    IL_0079:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_007e:  nop
    IL_007f:  endfinally
  }
  // sequence point: }
  IL_0080:  ret
}
");
        }

        [Fact]
        public void ExceptionHandler_CatchAllWithFilter()
        {
            var source = WithHelpers(@"
class C
{
    static void Main(string[] args)
    {
        bool b = false;
        string s;
        try
        {
            s = args[0];
        }
        catch when (b = true)
        {
            s = ""error"";
        }
    }
}
");
            var verifier = CompileAndVerify(source, expectedOutput: @"
Main: Entered
Main: P'args'[0] = System.String[]
Main: L1 = False
Main: L1 = True
Main: L2 = error
Main: Returned
");

            verifier.VerifyMethodBody("C.Main", @"
{
  // Code size      105 (0x69)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0,
                bool V_1, //b
                string V_2, //s
                bool V_3)
  // sequence point: <hidden>
  IL_0000:  ldtoken    ""void C.Main(string[])""
  IL_0005:  call       ""Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogMethodEntry(int)""
  IL_000a:  stloc.0
  .try
  {
    // sequence point: {
    IL_000b:  ldloca.s   V_0
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.0
    IL_000f:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(object, int)""
    IL_0014:  nop
    // sequence point: bool b = false;
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldc.i4.0
    IL_0018:  dup
    IL_0019:  stloc.1
    IL_001a:  ldc.i4.1
    IL_001b:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(bool, int)""
    IL_0020:  nop
    .try
    {
      // sequence point: {
      IL_0021:  nop
      // sequence point: s = args[0];
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldarg.0
      IL_0025:  ldc.i4.0
      IL_0026:  ldelem.ref
      IL_0027:  dup
      IL_0028:  stloc.2
      IL_0029:  ldc.i4.2
      IL_002a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_002f:  nop
      // sequence point: }
      IL_0030:  nop
      IL_0031:  leave.s    IL_005d
    }
    filter
    {
      // sequence point: <hidden>
      IL_0033:  pop
      // sequence point: when (b = true)
      IL_0034:  ldloca.s   V_0
      IL_0036:  ldc.i4.1
      IL_0037:  dup
      IL_0038:  stloc.1
      IL_0039:  ldc.i4.1
      IL_003a:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(bool, int)""
      IL_003f:  nop
      IL_0040:  ldloc.1
      IL_0041:  stloc.3
      // sequence point: <hidden>
      IL_0042:  ldloc.3
      IL_0043:  ldc.i4.0
      IL_0044:  cgt.un
      IL_0046:  endfilter
    }  // end filter
    {  // handler
      // sequence point: <hidden>
      IL_0048:  pop
      // sequence point: {
      IL_0049:  nop
      // sequence point: s = ""error"";
      IL_004a:  ldloca.s   V_0
      IL_004c:  ldstr      ""error""
      IL_0051:  dup
      IL_0052:  stloc.2
      IL_0053:  ldc.i4.2
      IL_0054:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLocalStore(string, int)""
      IL_0059:  nop
      // sequence point: }
      IL_005a:  nop
      IL_005b:  leave.s    IL_005d
    }
    // sequence point: }
    IL_005d:  leave.s    IL_0068
  }
  finally
  {
    // sequence point: <hidden>
    IL_005f:  ldloca.s   V_0
    IL_0061:  call       ""void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()""
    IL_0066:  nop
    IL_0067:  endfinally
  }
  // sequence point: }
  IL_0068:  ret
}");
        }

        [Fact]
        public void Recursion()
        {
            var source = WithHelpers(@"
class C
{
    static void M(int depth)
    {
        if (depth == 4)
        {
            return;
        }

        int a = depth;
        int b = depth;

        M(depth + 1);

        a += 10;
        if (depth % 2 == 0) b += 10;
    }

    static void Main() => M(0);
}
");
            CompileAndVerify(source, expectedOutput: @"
Main: Entered
M: Entered
M: P'depth'[0] = 0
M: L1 = 0
M: L2 = 0
M: Entered
M: P'depth'[0] = 1
M: L1 = 1
M: L2 = 1
M: Entered
M: P'depth'[0] = 2
M: L1 = 2
M: L2 = 2
M: Entered
M: P'depth'[0] = 3
M: L1 = 3
M: L2 = 3
M: Entered
M: P'depth'[0] = 4
M: Returned
M: L1 = 13
M: Returned
M: L1 = 12
M: L2 = 12
M: Returned
M: L1 = 11
M: Returned
M: L1 = 10
M: L2 = 10
M: Returned
Main: Returned
");
        }

        [Fact]
        public void Discards()
        {
            var source = WithHelpers(@"
using System;

_ = G(y: out var a, x: out _, z: out _);
F((_, _) => _ = 1);

static int G(out int x, out int y, out int z) => x = y = z = 1;
static int F(Func<int, int, int> f) => f(1, 2);
");
            CompileAndVerify(source, expectedOutput: @"
<Main>$: Entered
<Main>$: P'args'[0] = System.String[]
<Main>$: Entered lambda '<<Main>$>g__G|0_1'
<<Main>$>g__G|0_1: P'z'[2] = 1
<<Main>$>g__G|0_1: P'y'[1] = 1
<<Main>$>g__G|0_1: P'x'[0] = 1
<<Main>$>g__G|0_1: Returned
<Main>$: L1 = 1
<Main>$: Entered lambda '<<Main>$>g__F|0_2'
<<Main>$>g__F|0_2: P'f'[0] = System.Func`3[System.Int32,System.Int32,System.Int32]
<Main>$: Entered lambda '<<Main>$>b__0_0'
<<Main>$>b__0_0: Returned
<<Main>$>g__F|0_2: Returned
<Main>$: Returned
");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_01()
        {
            // non-static extension method
            var source = WithHelpers("""
42.M(43);

static class E
{
    extension(int i1)
    {
        public void M(int i2) { }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: P'i2'[1] = 43
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_02()
        {
            // non-static extension method with ref parameters and assignments
            var source = WithHelpers("""
int x1 = 42;
int x2 = 43;
x1.M(ref x2);

static class E
{
    extension(ref int i1)
    {
        public void M(ref int i2)
        {
            i1 = 52;
            i2 = 53;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
Program.<Main>$: L1 = 42
Program.<Main>$: L2 = 43
E.M: Entered
E.M: P'i1'[0] = 42
E.M: P'i2'[1] = 43
E.M: P'i1'[0] = 52
E.M: P'i2'[1] = 53
E.M: Returned
Program.<Main>$: L1 = 52
Program.<Main>$: L2 = 53
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_03()
        {
            // assignment to parameter, static extension method
            var source = WithHelpers("""
int i = 0;
int.M(ref i);

static class E
{
    extension(int)
    {
        public static void M(ref int i2)
        {
            i2 = 42;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
Program.<Main>$: L1 = 0
E.M: Entered
E.M: P'i2'[0] = 0
E.M: P'i2'[0] = 42
E.M: Returned
Program.<Main>$: L1 = 42
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_04()
        {
            // only receiver parameter has ref kind
            var source = WithHelpers("""
int i = 42;
i.M(43);

static class E
{
    extension(ref int i1)
    {
        public void M(int i2)
        {
            i1 = 52;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
Program.<Main>$: L1 = 42
E.M: Entered
E.M: P'i1'[0] = 42
E.M: P'i2'[1] = 43
E.M: P'i1'[0] = 52
E.M: Returned
Program.<Main>$: L1 = 52
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_05()
        {
            // non-static extension property
            var source = WithHelpers("""
_ = 42.P;

int i = 43;
i.P = 44;

static class E
{
    extension(int i1)
    {
        public int P { get => 0; set { } }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.get_P: Entered
E.get_P: P'i1'[0] = 42
E.get_P: Returned
Program.<Main>$: L1 = 43
E.set_P: Entered
E.set_P: P'i1'[0] = 43
E.set_P: P'value'[1] = 44
E.set_P: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_06()
        {
            // static extension property
            var source = WithHelpers("""
_ = int.P;
int.P = 42;

static class E
{
    extension(int)
    {
        public static int P { get => 0; set { } }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.get_P: Entered
E.get_P: Returned
E.set_P: Entered
E.set_P: P'value'[0] = 42
E.set_P: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_07()
        {
            // local function in extension
            var source = WithHelpers("""
42.M();

static class E
{
    extension(int i1)
    {
        public void M()
        {
            local(i1);

            void local(int i2) { }
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: Entered lambda 'E.<M>g__local|1_0'
E.<M>g__local|1_0: P'i2'[0] = 42
E.<M>g__local|1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_08()
        {
            // lambda in extension
            var source = WithHelpers("""
42.M();

static class E
{
    extension(int i1)
    {
        public void M()
        {
            var x = (int i2) => { };
            x(i1);
        }
    }
}
""", displayContainingType: true);

            var verifier = CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: L1 = System.Action`1[System.Int32]
E.M: Entered lambda 'E+<>c.<M>b__1_0'
E+<>c.<M>b__1_0: P'i2'[0] = 42
E+<>c.<M>b__1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");

            verifier.VerifyMethodBody("E.<>c.<M>b__1_0", """
{
  // Code size       38 (0x26)
  .maxstack  3
  .locals init (Microsoft.CodeAnalysis.Runtime.LocalStoreTracker V_0)
  // sequence point: <hidden>
  IL_0000:  ldtoken    "void E.M(int)"
  IL_0005:  ldtoken    "void E.<>c.<M>b__1_0(int)"
  IL_000a:  call       "Microsoft.CodeAnalysis.Runtime.LocalStoreTracker Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogLambdaEntry(int, int)"
  IL_000f:  stloc.0
  .try
  {
    // sequence point: {
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogParameterStore(uint, int)"
    IL_0019:  nop
    // sequence point: }
    IL_001a:  leave.s    IL_0025
  }
  finally
  {
    // sequence point: <hidden>
    IL_001c:  ldloca.s   V_0
    IL_001e:  call       "void Microsoft.CodeAnalysis.Runtime.LocalStoreTracker.LogReturn()"
    IL_0023:  nop
    IL_0024:  endfinally
  }
  // sequence point: }
  IL_0025:  ret
}
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_09()
        {
            // nested lambda in extension
            var source = WithHelpers("""
42.M();

static class E
{
    extension(int i1)
    {
        public void M()
        {
            var f1 = (int i2) =>
            {
                var f2 = (int i3) => { };
                f2(i2);
            };

            f1(i1);
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: L1 = System.Action`1[System.Int32]
E.M: Entered lambda 'E+<>c.<M>b__1_0'
E+<>c.<M>b__1_0: P'i2'[0] = 42
E+<>c.<M>b__1_0: L1 = System.Action`1[System.Int32]
E.M: Entered lambda 'E+<>c.<M>b__1_1'
E+<>c.<M>b__1_1: P'i3'[0] = 42
E+<>c.<M>b__1_1: Returned
E+<>c.<M>b__1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_10()
        {
            // local function parameter uses type parameter from extension block
            var source = WithHelpers("""
42.M();

static class E
{
    extension<T>(T t)
    {
        public void M()
        {
            local(t);

            void local(T t) { }
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P't'[0] = 42
E.M: Entered lambda 'E.<M>g__local|1_0'
E.<M>g__local|1_0: P't'[0] = 42
E.<M>g__local|1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_11()
        {
            // extension method local uses type parameter from extension block
            var source = WithHelpers("""
42.M();

static class E
{
    extension<T>(T t)
    {
        public void M()
        {
            T t2 = t;
            t2.ToString();
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P't'[0] = 42
E.M: L1 = 42
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_12()
        {
            // anonymous function in extension
            var source = WithHelpers("""
42.M();

static class E
{
    extension(int i1)
    {
        public void M()
        {
            System.Action<int> x = delegate { };
            x(i1);
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: L1 = System.Action`1[System.Int32]
E.M: Entered lambda 'E+<>c.<M>b__1_0'
E+<>c.<M>b__1_0: P'<p0>'[0] = 42
E+<>c.<M>b__1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_13()
        {
            // iterator in extension
            var source = WithHelpers("""
foreach (var i in 42.M())
{
}

static class E
{
    extension(int i)
    {
        public System.Collections.Generic.IEnumerable<int> M()
        {
            yield return i;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered state machine #1
E.M: P'i'[0] = 42
E.M: Returned
Program.<Main>$: L2 = 42
E.M: Entered state machine #1
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_14()
        {
            // iterator local function in extension
            var source = WithHelpers("""
42.M();

static class E
{
    extension(int i)
    {
        public void M()
        {
            foreach (var j in local())
            {
                System.Console.WriteLine(j);
            }
            return;

            System.Collections.Generic.IEnumerable<int> local()
            {
                yield return i;
            }
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i'[0] = 42
E.M: Entered lambda 'E+<>c__DisplayClass1_0.<M>g__local|0' state machine #1
E+<>c__DisplayClass1_0.<M>g__local|0: Returned
E.M: L3 = 42
42
E.M: Entered lambda 'E+<>c__DisplayClass1_0.<M>g__local|0' state machine #1
E+<>c__DisplayClass1_0.<M>g__local|0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_15()
        {
            // async lambda in extension
            var source = WithHelpers("""
await 42.M();

static class E
{
    extension(int i)
    {
        public async System.Threading.Tasks.Task M()
        {
            var f = async () =>
            {
                await System.Threading.Tasks.Task.FromResult(0);
            };

            await f();
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered state machine #1
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered state machine #2
E.M: P'i'[0] = 42
E.M: L'f' = System.Func`1[System.Threading.Tasks.Task]
E.M: Entered lambda 'E+<>c.<M>b__1_0' state machine #3
E+<>c.<M>b__1_0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_16()
        {
            // extension operator
            var source = WithHelpers("""
_ = new C() + 10;

static class E
{
    extension(C)
    {
        public static C operator +(C c, int i)
        {
            return new C();
        }
    }
}

class C { }
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
C..ctor: Entered
C..ctor: Returned
E.op_Addition: Entered
E.op_Addition: P'c'[0] = C
E.op_Addition: P'i'[1] = 10
C..ctor: Entered
C..ctor: Returned
E.op_Addition: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_17()
        {
            // ref assignment from parameter
            var source = WithHelpers("""
42.M(43);

static class E
{
    extension(int i1)
    {
        public void M(int i2)
        {
            ref int x1 = ref i1;
            ref int x2 = ref i2;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: P'i2'[1] = 43
M: L1 -> P'i1'[0]
M: L2 -> P'i2'[1]
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_18()
        {
            // ref assignment from hoisted parameter
            var source = WithHelpers("""
42.M(43);

static class E
{
    extension(int i1)
    {
        public void M(int i2)
        {
            var f = () =>
            {
                ref int x1 = ref i1;
                ref int x2 = ref i2;
            };

            f();
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
E.M: P'i2'[1] = 43
E.M: L2 = System.Action
E.M: Entered lambda 'E+<>c__DisplayClass1_0.<M>b__0'
<M>b__0: L1 -> P'i1'
<M>b__0: L2 -> P'i2'
E+<>c__DisplayClass1_0.<M>b__0: Returned
E.M: Returned
Program.<Main>$: Returned
""");
        }

        [Fact, CompilerTrait(CompilerFeature.Extensions)]
        public void Extensions_19()
        {
            // ref readonly extension parameter
            var source = WithHelpers("""
42.M();

static class E
{
    extension(ref readonly int i1)
    {
        public void M()
        {
            ref readonly int x1 = ref i1;
        }
    }
}
""", displayContainingType: true);

            CompileAndVerify(source, expectedOutput: """
Program.<Main>$: Entered
Program.<Main>$: P'args'[0] = System.String[]
E.M: Entered
E.M: P'i1'[0] = 42
M: L1 -> P'i1'[0]
E.M: Returned
Program.<Main>$: Returned
""");
        }
    }
}
#endif
