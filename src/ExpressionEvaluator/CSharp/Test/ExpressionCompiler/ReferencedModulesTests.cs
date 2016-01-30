// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;
using CommonResources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ReferencedModulesTests : ExpressionCompilerTestBase
    {
        /// <summary>
        /// MakeAssemblyReferences should drop unreferenced assemblies.
        /// </summary>
        [WorkItem(1141029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1141029")]
        [Fact]
        public void AssemblyDuplicateReferences()
        {
            var sourceA =
@"public class A
{
}";
            var sourceB =
@"public class B
{
    public A F = new A();
}";
            var sourceC =
@"class C
{
    private B F = new B();
    static void M()
    {
    }
}";
            // Assembly A, multiple versions, strong name.
            var assemblyNameA = ExpressionCompilerUtilities.GenerateUniqueName();
            var publicKeyA = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });
            var compilationAS1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(1, 1, 1, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceAS1 = compilationAS1.EmitToImageReference();
            var identityAS1 = referenceAS1.GetAssemblyIdentity();
            var compilationAS2 = CreateCompilation(
                new AssemblyIdentity(assemblyNameA, new Version(2, 1, 1, 1), cultureName: "", publicKeyOrToken: publicKeyA, hasPublicKey: true),
                new[] { sourceA },
                references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceAS2 = compilationAS2.EmitToImageReference();
            var identityAS2 = referenceAS2.GetAssemblyIdentity();

            // Assembly B, multiple versions, not strong name.
            var assemblyNameB = ExpressionCompilerUtilities.GenerateUniqueName();
            var compilationBN1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(1, 1, 1, 1)),
                new[] { sourceB },
                references: new[] { MscorlibRef, referenceAS1 },
                options: TestOptions.DebugDll);
            var referenceBN1 = compilationBN1.EmitToImageReference();
            var identityBN1 = referenceBN1.GetAssemblyIdentity();
            var compilationBN2 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(2, 2, 2, 1)),
                new[] { sourceB },
                references: new[] { MscorlibRef, referenceAS1 },
                options: TestOptions.DebugDll);
            var referenceBN2 = compilationBN2.EmitToImageReference();
            var identityBN2 = referenceBN2.GetAssemblyIdentity();

            // Assembly B, multiple versions, strong name.
            var publicKeyB = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x53, 0x52, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });
            var compilationBS1 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(1, 1, 1, 1), cultureName: "", publicKeyOrToken: publicKeyB, hasPublicKey: true),
                new[] { sourceB },
                references: new[] { MscorlibRef, referenceAS1 },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceBS1 = compilationBS1.EmitToImageReference();
            var identityBS1 = referenceBS1.GetAssemblyIdentity();
            var compilationBS2 = CreateCompilation(
                new AssemblyIdentity(assemblyNameB, new Version(2, 2, 2, 1), cultureName: "", publicKeyOrToken: publicKeyB, hasPublicKey: true),
                new[] { sourceB },
                references: new[] { MscorlibRef, referenceAS2 },
                options: TestOptions.DebugDll.WithDelaySign(true));
            var referenceBS2 = compilationBS2.EmitToImageReference();
            var identityBS2 = referenceBS2.GetAssemblyIdentity();

            var mscorlibIdentity = MscorlibRef.GetAssemblyIdentity();
            var mscorlib20Identity = MscorlibRef_v20.GetAssemblyIdentity();
            var systemRefIdentity = SystemRef.GetAssemblyIdentity();
            var systemRef20Identity = SystemRef_v20.GetAssemblyIdentity();

            // No duplicates.
            VerifyAssemblyReferences(
                referenceBN1,
                ImmutableArray.Create(MscorlibRef, referenceAS1, referenceBN1),
                ImmutableArray.Create(mscorlibIdentity, identityAS1, identityBN1));
            // No duplicates, extra references.
            VerifyAssemblyReferences(
                referenceAS1,
                ImmutableArray.Create(MscorlibRef, referenceBN1, referenceAS1, referenceBS2),
                ImmutableArray.Create(mscorlibIdentity, identityAS1));
            // Strong-named, non-strong-named, and framework duplicates, same version (no aliases).
            VerifyAssemblyReferences(
                referenceBN2,
                ImmutableArray.Create(MscorlibRef, referenceAS1, MscorlibRef, referenceBN2, referenceBN2, referenceAS1, referenceAS1),
                ImmutableArray.Create(mscorlibIdentity, identityAS1, identityBN2));
            // Strong-named, non-strong-named, and framework duplicates, different versions.
            VerifyAssemblyReferences(
                referenceBN1,
                ImmutableArray.Create(MscorlibRef, referenceAS1, MscorlibRef_v20, referenceAS2, referenceBN2, referenceBN1, referenceAS2, referenceAS1, referenceBN1),
                ImmutableArray.Create(mscorlibIdentity, identityAS2, identityBN2));
            VerifyAssemblyReferences(
                referenceBN2,
                ImmutableArray.Create(MscorlibRef, referenceAS1, MscorlibRef_v20, referenceAS2, referenceBN2, referenceBN1, referenceAS2, referenceAS1, referenceBN1),
                ImmutableArray.Create(mscorlibIdentity, identityAS2, identityBN2));
            // Strong-named, different versions.
            VerifyAssemblyReferences(
                referenceBS1,
                ImmutableArray.Create(MscorlibRef, referenceAS1, referenceAS2, referenceBS2, referenceBS1, referenceAS2, referenceAS1, referenceBS1),
                ImmutableArray.Create(mscorlibIdentity, identityAS2, identityBS2));
            VerifyAssemblyReferences(
                referenceBS2,
                ImmutableArray.Create(MscorlibRef, referenceAS1, referenceAS2, referenceBS2, referenceBS1, referenceAS2, referenceAS1, referenceBS1),
                ImmutableArray.Create(mscorlibIdentity, identityAS2, identityBS2));

            // Assembly C, multiple versions, not strong name.
            var compilationCN1 = CreateCompilation(
                new AssemblyIdentity("C", new Version(1, 1, 1, 1)),
                new[] { sourceC },
                references: new[] { MscorlibRef, referenceBS1 },
                options: TestOptions.DebugDll);

            // Duplicate assemblies, target module referencing BS1.
            WithRuntimeInstance(compilationCN1, new[] { MscorlibRef, referenceAS1, referenceAS2, referenceBS2, referenceBS1, referenceBS2 }, runtime =>
            {
                ImmutableArray<MetadataBlock> typeBlocks;
                ImmutableArray<MetadataBlock> methodBlocks;
                Guid moduleVersionId;
                ISymUnmanagedReader symReader;
                int typeToken;
                int methodToken;
                int localSignatureToken;
                GetContextState(runtime, "C", out typeBlocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
                GetContextState(runtime, "C.M", out methodBlocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
                uint ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader);

                // Compile expression with type context with all modules.
                var context = EvaluationContext.CreateTypeContext(
                    default(CSharpMetadataContext),
                    typeBlocks,
                    moduleVersionId,
                    typeToken);
                string error;
                // A is ambiguous.
                var testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.True(error.StartsWith("error CS0433: The type 'A' exists in both "));
                testData = new CompilationTestData();
                // B is ambiguous.
                context.CompileExpression("new B()", out error, testData);
                Assert.True(error.StartsWith("error CS0433: The type 'B' exists in both "));
                var previous = new CSharpMetadataContext(typeBlocks, context);

                // Compile expression with type context with referenced modules only.
                context = EvaluationContext.CreateTypeContext(
                    typeBlocks.ToCompilationReferencedModulesOnly(moduleVersionId),
                    moduleVersionId,
                    typeToken);
                // A is unrecognized since there were no direct references to AS1 or AS2.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Equal(error, "error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)");
                testData = new CompilationTestData();
                // B should be resolved to BS2.
                context.CompileExpression("new B()", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
    @"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityBS2.GetDisplayName());
                // B.F should result in missing assembly AS2 since there were no direct references to AS2.
                ResultProperties resultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                testData = new CompilationTestData();
                context.CompileExpression(
                    "(new B()).F",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                AssertEx.Equal(missingAssemblyIdentities, ImmutableArray.Create(identityAS2));

                // Compile expression with method context with all modules.
                context = EvaluationContext.CreateMethodContext(
                    previous,
                    methodBlocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken);
                Assert.Equal(previous.Compilation, context.Compilation); // re-use type context compilation
                testData = new CompilationTestData();
                // A is ambiguous.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.True(error.StartsWith("error CS0433: The type 'A' exists in both "));
                testData = new CompilationTestData();
                // B is ambiguous.
                context.CompileExpression("new B()", out error, testData);
                Assert.True(error.StartsWith("error CS0433: The type 'B' exists in both "));

                // Compile expression with method context with referenced modules only.
                context = EvaluationContext.CreateMethodContext(
                    methodBlocks.ToCompilationReferencedModulesOnly(moduleVersionId),
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken);
                // A is unrecognized since there were no direct references to AS1 or AS2.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Equal(error, "error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)");
                testData = new CompilationTestData();
                // B should be resolved to BS2.
                context.CompileExpression("new B()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
    @"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityBS2.GetDisplayName());
                // B.F should result in missing assembly AS2 since there were no direct references to AS2.
                testData = new CompilationTestData();
                context.CompileExpression(
                    "(new B()).F",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                AssertEx.Equal(missingAssemblyIdentities, ImmutableArray.Create(identityAS2));
            });
        }

        private static void VerifyAssemblyReferences(
            MetadataReference target,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<AssemblyIdentity> expectedIdentities)
        {
            Assert.True(references.Contains(target));
            var modules = references.SelectAsArray(r => r.ToModuleInstance());
            using (var runtime = new RuntimeInstance(modules))
            {
                var moduleVersionId = target.GetModuleVersionId();
                var blocks = runtime.Modules.SelectAsArray(m => m.MetadataBlock);
                var actualReferences = blocks.MakeAssemblyReferences(moduleVersionId, CompilationExtensions.IdentityComparer);
                // Verify identities.
                var actualIdentities = actualReferences.SelectAsArray(r => r.GetAssemblyIdentity());
                AssertEx.Equal(expectedIdentities, actualIdentities);
                // Verify identities are unique.
                var uniqueIdentities = actualIdentities.Distinct();
                Assert.Equal(actualIdentities.Length, uniqueIdentities.Length);
            }
        }

        [Fact]
        public void DuplicateTypesAndMethodsDifferentAssemblies()
        {
            var sourceA =
@"using N;
namespace N
{
    class C1 { }
    public static class E
    {
        public static A F(this A o) { return o; }
    }
}
class C2 { }
public class A
{
    public static void M()
    {
        var x = new A();
        var y = x.F();
    }
}";
            var sourceB =
@"using N;
namespace N
{
    class C1 { }
    public static class E
    {
        public static int F(this A o) { return 2; }
    }
}
class C2 { }
public class B
{
    static void M()
    {
        var x = new A();
    }
}";
            var compilationA = CreateCompilationWithMscorlibAndSystemCore(sourceA, options: TestOptions.DebugDll);
            var identityA = compilationA.Assembly.Identity;
            var moduleA = compilationA.ToModuleInstance();

            var compilationB = CreateCompilationWithMscorlibAndSystemCore(sourceB, options: TestOptions.DebugDll, references: new[] { moduleA.GetReference() });
            var moduleB = compilationB.ToModuleInstance();

            var runtime = CreateRuntimeInstance(new[] { MscorlibRef.ToModuleInstance(), SystemCoreRef.ToModuleInstance(), moduleA, moduleB });
            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int typeToken;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, "B", out blocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            string errorMessage;
            CompilationTestData testData;
            var contextFactory = CreateTypeContextFactory(moduleVersionId, typeToken);

            // Duplicate type in namespace, at type scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new N.C1()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.True(errorMessage.StartsWith("error CS0433: The type 'C1' exists in both "));

            GetContextState(runtime, "B.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
            contextFactory = CreateMethodContextFactory(moduleVersionId, symReader, methodToken, localSignatureToken);

            // Duplicate type in namespace, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new C1()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.True(errorMessage.StartsWith("error CS0433: The type 'C1' exists in both "));

            // Duplicate type in global namespace, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new C2()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.True(errorMessage.StartsWith("error CS0433: The type 'C2' exists in both "));

            // Duplicate extension method, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "x.F()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Equal(errorMessage, "error CS0121: The call is ambiguous between the following methods or properties: 'N.E.F(A)' and 'N.E.F(A)'");

            // Same tests as above but in library that does not directly reference duplicates.
            GetContextState(runtime, "A", out blocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            contextFactory = CreateTypeContextFactory(moduleVersionId, typeToken);

            // Duplicate type in namespace, at type scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new N.C1()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Null(errorMessage);
            var methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""N.C1..ctor()""
IL_0005:  ret
}");
            Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());

            GetContextState(runtime, "A.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
            contextFactory = CreateMethodContextFactory(moduleVersionId, symReader, methodToken, localSignatureToken);

            // Duplicate type in global namespace, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new C2()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Null(errorMessage);
            methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
.locals init (A V_0, //x
            A V_1) //y
IL_0000:  newobj     ""C2..ctor()""
IL_0005:  ret
}");
            Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());

            // Duplicate extension method, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "x.F()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Null(errorMessage);
            methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(
@"{
// Code size        7 (0x7)
.maxstack  1
.locals init (A V_0, //x
            A V_1) //y
IL_0000:  ldloc.0
IL_0001:  call       ""A N.E.F(A)""
IL_0006:  ret
}");
            Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());
        }

        /// <summary>
        /// mscorlib.dll is not directly referenced from an assembly
        /// compiled against portable framework assemblies.
        /// </summary>
        [WorkItem(1150981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1150981")]
        [Fact]
        public void MissingMscorlib()
        {
            var sourceA =
@"public class A
{
}
class B
{
}
class C
{
}";
            var sourceB =
@"public class B : A
{
}";
            var moduleA = CreateCompilation(
                sourceA,
                references: new[] { SystemRuntimePP7Ref },
                options: TestOptions.DebugDll).ToModuleInstance();

            var moduleB = CreateCompilation(
                sourceB,
                references: new[] { SystemRuntimePP7Ref, moduleA.GetReference() },
                options: TestOptions.DebugDll).ToModuleInstance();

            // Include an empty assembly to verify that not all assemblies
            // with no references are treated as mscorlib.
            var referenceC = AssemblyMetadata.CreateFromImage(CommonResources.Empty).GetReference();

            // At runtime System.Runtime.dll contract assembly is replaced
            // by mscorlib.dll and System.Runtime.dll facade assemblies.
            var runtime = CreateRuntimeInstance(new[] 
            {
                MscorlibFacadeRef.ToModuleInstance(),
                SystemRuntimeFacadeRef.ToModuleInstance(),
                moduleA,
                moduleB,
                referenceC.ToModuleInstance()
            });

            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int typeToken;
            int localSignatureToken;
            GetContextState(runtime, "C", out blocks, out moduleVersionId, out symReader, out typeToken, out localSignatureToken);
            string errorMessage;
            CompilationTestData testData;
            int attempts = 0;
            ExpressionCompiler.CreateContextDelegate contextFactory = (b, u) =>
            {
                attempts++;
                return EvaluationContext.CreateTypeContext(
                    ToCompilation(b, u, moduleVersionId),
                    moduleVersionId,
                    typeToken);
            };

            // Compile: [DebuggerDisplay("{new B()}")]
            const string expr = "new B()";
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, expr, ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Null(errorMessage);
            Assert.Equal(2, attempts);
            var methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
        }

        [WorkItem(1170032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1170032")]
        [Fact]
        public void DuplicateTypesInMscorlib()
        {
            var sourceConsole =
@"namespace System
{
    public class Console
    {
    }
}";
            var sourceObjectModel =
@"namespace System.Collections.ObjectModel
{
    public class ReadOnlyDictionary<K, V>
    {
    }
}";
            var source =
@"class C
{
    static void Main()
    {
        var t = typeof(System.Console);
        var o = (System.Collections.ObjectModel.ReadOnlyDictionary<object, object>)null;
    }
}";
            var systemConsoleComp = CreateCompilationWithMscorlib(sourceConsole, options: TestOptions.DebugDll, assemblyName: "System.Console");
            var systemConsoleRef = systemConsoleComp.EmitToImageReference();
            var systemObjectModelComp = CreateCompilationWithMscorlib(sourceObjectModel, options: TestOptions.DebugDll, assemblyName: "System.ObjectModel");
            var systemObjectModelRef = systemObjectModelComp.EmitToImageReference();
            var identityObjectModel = systemObjectModelRef.GetAssemblyIdentity();

            // At runtime System.Runtime.dll contract assembly is replaced
            // by mscorlib.dll and System.Runtime.dll facade assemblies;
            // System.Console.dll and System.ObjectModel.dll are not replaced.

            // Test different ordering of modules containing duplicates:
            // { System.Console, mscorlib } and { mscorlib, System.ObjectModel }.
            var contractReferences = ImmutableArray.Create(systemConsoleRef, SystemRuntimePP7Ref, systemObjectModelRef);
            var runtimeReferences = ImmutableArray.Create(systemConsoleRef, MscorlibFacadeRef, SystemRuntimeFacadeRef, systemObjectModelRef);

            // Verify the compiler reports duplicate types with facade assemblies.
            var compilation = CreateCompilation(
                source,
                references: runtimeReferences,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName());
            compilation.VerifyDiagnostics(
                // error CS0433: The type 'Console' exists in both 'System.Console, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
                //         var t = typeof(System.Console);
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "Console").WithArguments("System.Console, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Console", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").WithLocation(5, 31),
                // error CS0433: The type 'ReadOnlyDictionary<K, V>' exists in both 'System.ObjectModel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
                //         var o = (System.Collections.ObjectModel.ReadOnlyDictionary<object, object>)null;
                Diagnostic(ErrorCode.ERR_SameFullNameAggAgg, "ReadOnlyDictionary<object, object>").WithArguments("System.ObjectModel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Collections.ObjectModel.ReadOnlyDictionary<K, V>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").WithLocation(6, 49));

            // EE should not report duplicate type when the original source
            // is compiled with contract assemblies and the EE expression
            // is compiled with facade assemblies.
            compilation = CreateCompilation(
                source,
                references: contractReferences,
                options: TestOptions.DebugDll);

            WithRuntimeInstance(compilation, runtimeReferences, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.Main");
                string errorMessage;

                // { System.Console, mscorlib }
                var testData = new CompilationTestData();
                context.CompileExpression("typeof(System.Console)", out errorMessage, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (System.Type V_0, //t
                System.Collections.ObjectModel.ReadOnlyDictionary<object, object> V_1) //o
  IL_0000:  ldtoken    ""System.Console""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}");

                // { mscorlib, System.ObjectModel }
                testData = new CompilationTestData();
                context.CompileExpression("(System.Collections.ObjectModel.ReadOnlyDictionary<object, object>)null", out errorMessage, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(@"
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.Type V_0, //t
                System.Collections.ObjectModel.ReadOnlyDictionary<object, object> V_1) //o
  IL_0000:  ldnull
  IL_0001:  ret
}");
                Assert.Equal(methodData.Method.ReturnType.ContainingAssembly.ToDisplayString(), identityObjectModel.GetDisplayName());
            });
        }

        /// <summary>
        /// Intrinsic methods assembly should not be dropped.
        /// </summary>
        [WorkItem(4140, "https://github.com/dotnet/roslyn/issues/4140")]
        [Fact]
        public void IntrinsicMethods()
        {
            var sourceA =
@"public class A { }";
            var sourceB =
@"public class A { }
public class B
{
    static void M(A a)
    {
    }
}";
            var compilationA = CreateCompilationWithMscorlibAndSystemCore(sourceA, options: TestOptions.DebugDll);
            var moduleA = compilationA.ToModuleInstance();

            var compilationB = CreateCompilationWithMscorlibAndSystemCore(sourceB, options: TestOptions.DebugDll, references: new[] { moduleA.GetReference() });
            var moduleB = compilationB.ToModuleInstance();

            var runtime = CreateRuntimeInstance(new[] 
            {
                MscorlibRef.ToModuleInstance(),
                SystemCoreRef.ToModuleInstance(),
                moduleA,
                moduleB,
                ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance()
            });

            ImmutableArray<MetadataBlock> blocks;
            Guid moduleVersionId;
            ISymUnmanagedReader symReader;
            int methodToken;
            int localSignatureToken;
            GetContextState(runtime, "B.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);

            var aliases = ImmutableArray.Create(
                ExceptionAlias(typeof(ArgumentException)),
                ReturnValueAlias(2, typeof(string)),
                ObjectIdAlias(1, typeof(object)));

            int attempts = 0;
            ExpressionCompiler.CreateContextDelegate contextFactory = (b, u) =>
            {
                attempts++;
                return EvaluationContext.CreateMethodContext(
                    ToCompilation(b, u, moduleVersionId),
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion: 1,
                    ilOffset: 0,
                    localSignatureToken: localSignatureToken);
            };

            string errorMessage;
            CompilationTestData testData;
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                blocks,
                "(object)new A() ?? $exception ?? $1 ?? $ReturnValue2",
                aliases,
                contextFactory,
                getMetaDataBytesPtr: null,
                errorMessage: out errorMessage,
                testData: out testData);

            Assert.Null(errorMessage);
            Assert.Equal(2, attempts);
            var methodData = testData.GetMethodData("<>x.<>m0");
            methodData.VerifyIL(
@"{
// Code size       49 (0x31)
.maxstack  2
IL_0000:  newobj     ""A..ctor()""
IL_0005:  dup
IL_0006:  brtrue.s   IL_0030
IL_0008:  pop
IL_0009:  call       ""System.Exception Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException()""
IL_000e:  castclass  ""System.ArgumentException""
IL_0013:  dup
IL_0014:  brtrue.s   IL_0030
IL_0016:  pop
IL_0017:  ldstr      ""$1""
IL_001c:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
IL_0021:  dup
IL_0022:  brtrue.s   IL_0030
IL_0024:  pop
IL_0025:  ldc.i4.2
IL_0026:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(int)""
IL_002b:  castclass  ""string""
IL_0030:  ret
}");
        }

        private static ExpressionCompiler.CreateContextDelegate CreateTypeContextFactory(
            Guid moduleVersionId,
            int typeToken)
        {
            return (blocks, useReferencedModulesOnly) => EvaluationContext.CreateTypeContext(
                ToCompilation(blocks, useReferencedModulesOnly, moduleVersionId),
                moduleVersionId,
                typeToken);
        }

        private static ExpressionCompiler.CreateContextDelegate CreateMethodContextFactory(
            Guid moduleVersionId,
            ISymUnmanagedReader symReader,
            int methodToken,
            int localSignatureToken)
        {
            return (blocks, useReferencedModulesOnly) => EvaluationContext.CreateMethodContext(
                ToCompilation(blocks, useReferencedModulesOnly, moduleVersionId),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion: 1,
                ilOffset: 0,
                localSignatureToken: localSignatureToken);
        }

        private static CSharpCompilation ToCompilation(
            ImmutableArray<MetadataBlock> blocks,
            bool useReferencedModulesOnly,
            Guid moduleVersionId)
        {
            return useReferencedModulesOnly ? blocks.ToCompilationReferencedModulesOnly(moduleVersionId) : blocks.ToCompilation();
        }
    }
}
