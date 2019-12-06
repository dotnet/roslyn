// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CommonResources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ReferencedModulesTests : ExpressionCompilerTestBase
    {
        /// <summary>
        /// MakeAssemblyReferences should drop unreferenced assemblies.
        /// </summary>
        [Fact]
        public void UnreferencedAssemblies()
        {
            var (identityMscorlib, moduleMscorlib) = (MscorlibRef.GetAssemblyIdentity(), MscorlibRef.ToModuleInstance());
            var (identityIntrinsic, moduleIntrinsic) = (ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.GetAssemblyIdentity(), ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance());
            var (identityA1, moduleA1, refA1) = Compile("A1", "public class A1 { static void M() { } }");
            var (identityA2, moduleA2, refA2) = Compile("A2", "public class A2 { static void M() { } }");
            var (identityB1, moduleB1, refB1) = Compile("B1", "public class B1 : A1 { static void M() { } }", refA1);
            var (identityB2, moduleB2, refB2) = Compile("B2", "public class B2 : A1 { static void M() { } }", refA1);
            var (identityC, moduleC, refC) = Compile("C", "public class C1 : B2 { static void M() { } } public class C2 : A2 { }", refA1, refA2, refB2);

            using (var runtime = CreateRuntimeInstance(new[] { moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC }))
            {
                var stateB2 = GetContextState(runtime, "B2.M");

                // B2.M with missing A1.
                var context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock),
                    stateB2);
                ResultProperties resultProperties;
                string error;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                context.CompileExpression("new B2()", DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal("error CS0012: The type 'A1' is defined in an assembly that is not referenced. You must add a reference to assembly 'A1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.", error);
                AssertEx.Equal(new[] { identityA1 }, missingAssemblyIdentities);
                VerifyResolutionRequests(context, (identityA1, null, 1));

                // B2.M with all assemblies.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock),
                    stateB2);
                var testData = new CompilationTestData();
                context.CompileExpression("new B2()", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B2..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));

                // B2.M with all assemblies in reverse order.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleC, moduleB2, moduleB1, moduleA2, moduleA1, moduleMscorlib, moduleIntrinsic).SelectAsArray(m => m.MetadataBlock),
                    stateB2);
                testData = new CompilationTestData();
                context.CompileExpression("new B2()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B2..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));

                // A1.M with all assemblies.
                var stateA1 = GetContextState(runtime, "A1.M");
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock),
                    stateA1);
                testData = new CompilationTestData();
                context.CompileExpression("new A1()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""A1..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context);

                // B1.M with all assemblies.
                var stateB1 = GetContextState(runtime, "B1.M");
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock),
                    stateB1);
                testData = new CompilationTestData();
                context.CompileExpression("new B1()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B1..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));

                // C1.M with all assemblies.
                var stateC = GetContextState(runtime, "C1.M");
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock),
                    stateC);
                testData = new CompilationTestData();
                context.CompileExpression("new C1()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""C1..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityB2, identityB2, 1), (identityA1, identityA1, 1), (identityA2, identityA2, 1));

                // Other EvaluationContext.CreateMethodContext overload.
                // A1.M with all assemblies.
                var allBlocks = ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA2, moduleB1, moduleB2, moduleC).SelectAsArray(m => m.MetadataBlock);
                context = EvaluationContext.CreateMethodContext(
                    new CSharpMetadataContext(),
                    allBlocks,
                    stateA1.SymReader,
                    stateA1.ModuleVersionId,
                    stateA1.MethodToken,
                    methodVersion: 1,
                    stateA1.ILOffset,
                    stateA1.LocalSignatureToken);
                testData = new CompilationTestData();
                context.CompileExpression("new B1()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B1..ctor()""
IL_0005:  ret
}");

                // Other EvaluationContext.CreateMethodContext overload.
                // A1.M with all assemblies, offset outside of IL.
                context = EvaluationContext.CreateMethodContext(
                    new CSharpMetadataContext(),
                    allBlocks,
                    stateA1.SymReader,
                    stateA1.ModuleVersionId,
                    stateA1.MethodToken,
                    methodVersion: 1,
                    uint.MaxValue,
                    stateA1.LocalSignatureToken);
                testData = new CompilationTestData();
                context.CompileExpression("new C1()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""C1..ctor()""
IL_0005:  ret
}");
            }
        }

        [Fact]
        public void DifferentAssemblyVersions()
        {
            var publicKeyA = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });
            var options = TestOptions.DebugDll.WithDelaySign(true);
            var (identityMscorlib, moduleMscorlib) = (MscorlibRef.GetAssemblyIdentity(), MscorlibRef.ToModuleInstance());
            var (identityA1, moduleA1, refA1) = Compile(new AssemblyIdentity("A", new Version(1, 1, 1, 1), publicKeyOrToken: publicKeyA, hasPublicKey: true), "public class A { }", options, MscorlibRef);
            var (identityA2, moduleA2, refA2) = Compile(new AssemblyIdentity("A", new Version(2, 2, 2, 2), publicKeyOrToken: publicKeyA, hasPublicKey: true), "public class A { }", options, MscorlibRef);
            var (identityA3, moduleA3, refA3) = Compile(new AssemblyIdentity("a", new Version(3, 3, 3, 3), publicKeyOrToken: publicKeyA, hasPublicKey: true), "public class A { }", options, MscorlibRef);
            var (identityB1, moduleB1, refB1) = Compile(new AssemblyIdentity("B", new Version(1, 1, 1, 1)), "public class B : A { static void M() { } }", TestOptions.DebugDll, refA2, MscorlibRef);

            using (var runtime = CreateRuntimeInstance(new[] { moduleMscorlib, moduleA1, moduleA2, moduleA3, moduleB1 }))
            {
                var stateB = GetContextState(runtime, "B.M");

                // Expected version of A.
                var context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA2, moduleB1).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA2, identityA2, 1));

                // Higher version of A.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA3, moduleB1).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA2, identityA3, 1));

                // Lower version of A.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleB1).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                Assert.Equal("error CS1705: Assembly 'B' with identity 'B, Version=1.1.1.1, Culture=neutral, PublicKeyToken=null' uses 'A, Version=2.2.2.2, Culture=neutral, PublicKeyToken=1f8a32457d187bf3' which has a higher version than referenced assembly 'A' with identity 'A, Version=1.1.1.1, Culture=neutral, PublicKeyToken=1f8a32457d187bf3'", error);
                VerifyResolutionRequests(context, (identityA2, identityA1, 1));

                // Multiple versions of A.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA1, moduleA3, moduleA2, moduleB1).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA2, identityA2, 1));

                // Duplicate versions of A.
                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleA3, moduleA1, moduleA3, moduleA1, moduleB1).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA2, identityA3, 1));
            }
        }

        // Not handling duplicate corlib when using referenced assemblies
        // only (bug #...).
        [Fact(Skip = "TODO")]
        public void DuplicateNamedCorLib()
        {
            var publicKeyOther = ImmutableArray.CreateRange(new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xED, 0xD3, 0x22, 0xCB, 0x6B, 0xF8, 0xD4, 0xA2, 0xFC, 0xCC, 0x87, 0x37, 0x04, 0x06, 0x04, 0xCE, 0xE7, 0xB2, 0xA6, 0xF8, 0x4A, 0xEE, 0xF3, 0x19, 0xDF, 0x5B, 0x95, 0xE3, 0x7A, 0x6A, 0x28, 0x24, 0xA4, 0x0A, 0x83, 0x83, 0xBD, 0xBA, 0xF2, 0xF2, 0x52, 0x20, 0xE9, 0xAA, 0x3B, 0xD1, 0xDD, 0xE4, 0x9A, 0x9A, 0x9C, 0xC0, 0x30, 0x8F, 0x01, 0x40, 0x06, 0xE0, 0x2B, 0x95, 0x62, 0x89, 0x2A, 0x34, 0x75, 0x22, 0x68, 0x64, 0x6E, 0x7C, 0x2E, 0x83, 0x50, 0x5A, 0xCE, 0x7B, 0x0B, 0xE8, 0xF8, 0x71, 0xE6, 0xF7, 0x73, 0x8E, 0xEB, 0x84, 0xD2, 0x73, 0x5D, 0x9D, 0xBE, 0x5E, 0xF5, 0x90, 0xF9, 0xAB, 0x0A, 0x10, 0x7E, 0x23, 0x48, 0xF4, 0xAD, 0x70, 0x2E, 0xF7, 0xD4, 0x51, 0xD5, 0x8B, 0x3A, 0xF7, 0xCA, 0x90, 0x4C, 0xDC, 0x80, 0x19, 0x26, 0x65, 0xC9, 0x37, 0xBD, 0x52, 0x81, 0xF1, 0x8B, 0xCD });
            var options = TestOptions.DebugDll.WithDelaySign(true);
            var (identityMscorlib, moduleMscorlib) = (MscorlibRef.GetAssemblyIdentity(), MscorlibRef.ToModuleInstance());
            var (identityOther, moduleOther, refOther) = Compile(new AssemblyIdentity(identityMscorlib.Name, new Version(1, 1, 1, 1), publicKeyOrToken: publicKeyOther, hasPublicKey: true), "class Other { }", options, MscorlibRef);
            var (identityA, moduleA, refA) = Compile(new AssemblyIdentity("A", new Version(1, 1, 1, 1)), "public class A { }", TestOptions.DebugDll, refOther, MscorlibRef);
            var (identityB, moduleB, refB) = Compile(new AssemblyIdentity("B", new Version(1, 1, 1, 1)), "public class B : A { static void M() { } }", TestOptions.DebugDll, refA, refOther, MscorlibRef);

            using (var runtime = CreateRuntimeInstance(new[] { moduleMscorlib, moduleA, moduleB }))
            {
                var stateB = GetContextState(runtime, "B.M");
                var (moduleVersionId, symReader, methodToken, localSignatureToken, ilOffset) = GetContextState(runtime, "B.M");

                var context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleMscorlib, moduleOther, moduleA, moduleB).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA, identityA, 1));

                context = CreateMethodContext(
                    new AppDomain(),
                    ImmutableArray.Create(moduleB, moduleA, moduleOther, moduleMscorlib).SelectAsArray(m => m.MetadataBlock),
                    stateB);
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
// Code size        6 (0x6)
.maxstack  1
IL_0000:  newobj     ""B..ctor()""
IL_0005:  ret
}");
                VerifyResolutionRequests(context, (identityA, identityA, 1));
            }
        }

        /// <summary>
        /// Reuse compilation across evaluations unless current assembly changes.
        /// </summary>
        [Fact]
        public void ReuseCompilation()
        {
            var (identityMscorlib, moduleMscorlib) = (MscorlibRef.GetAssemblyIdentity(), MscorlibRef.ToModuleInstance());
            var (identityA1, moduleA1, refA1) = Compile("A1", "public class A1 { static void M() { } }");
            var (identityA2, moduleA2, refA2) = Compile("A2", "public class A2 { static void M() { } } public class A3 { static void M() { } }");
            var (identityB1, moduleB1, refB1) = Compile("B1", "public class B1 : A1 { static void M() { } } public class B2 { static void M() { } }", refA1);

            using (var runtime = CreateRuntimeInstance(new[] { moduleMscorlib, moduleA1, moduleA2, moduleB1 }))
            {
                var blocks = runtime.Modules.SelectAsArray(m => m.MetadataBlock);
                var stateA1 = GetContextState(runtime, "A1.M");
                var stateA2 = GetContextState(runtime, "A2.M");
                var stateA3 = GetContextState(runtime, "A3.M");
                var stateB1 = GetContextState(runtime, "B1.M");
                var stateB2 = GetContextState(runtime, "B2.M");

                var mvidA1 = stateA1.ModuleVersionId;
                var mvidA2 = stateA2.ModuleVersionId;
                var mvidB1 = stateB1.ModuleVersionId;
                Assert.Equal(mvidB1, stateB2.ModuleVersionId);

                EvaluationContext context;
                MetadataContext<CSharpMetadataContext> previous;

                // B1 -> B2 -> A1 -> A2 -> A3
                // B1.M:
                var appDomain = new AppDomain();
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateB1);
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));
                VerifyAppDomainMetadataContext(appDomain, mvidB1);
                // B2.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateB2);
                Assert.NotSame(context, GetMetadataContext(previous, mvidB1).EvaluationContext);
                Assert.Same(context.Compilation, GetMetadataContext(previous, mvidB1).Compilation);
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));
                VerifyAppDomainMetadataContext(appDomain, mvidB1);
                // A1.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateA1);
                Assert.NotSame(context, GetMetadataContext(previous, mvidB1).EvaluationContext);
                Assert.NotSame(context.Compilation, GetMetadataContext(previous, mvidB1).Compilation);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidB1, mvidA1);
                // A2.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateA2);
                Assert.NotSame(context, GetMetadataContext(previous, mvidA1).EvaluationContext);
                Assert.NotSame(context.Compilation, GetMetadataContext(previous, mvidA1).Compilation);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidB1, mvidA1, mvidA2);
                // A3.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateA3);
                Assert.NotSame(context, GetMetadataContext(previous, mvidA2).EvaluationContext);
                Assert.Same(context.Compilation, GetMetadataContext(previous, mvidA2).Compilation);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidB1, mvidA1, mvidA2);

                // A1 -> A2 -> A3 -> B1 -> B2
                // A1.M:
                appDomain = new AppDomain();
                context = CreateMethodContext(appDomain, blocks, stateA1);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidA1);
                // A2.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateA2);
                Assert.NotSame(context, GetMetadataContext(previous, mvidA1).EvaluationContext);
                Assert.NotSame(context.Compilation, GetMetadataContext(previous, mvidA1).Compilation);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidA1, mvidA2);
                // A3.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateA3);
                Assert.NotSame(context, GetMetadataContext(previous, mvidA2).EvaluationContext);
                Assert.Same(context.Compilation, GetMetadataContext(previous, mvidA2).Compilation);
                VerifyResolutionRequests(context);
                VerifyAppDomainMetadataContext(appDomain, mvidA1, mvidA2);
                // B1.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateB1);
                Assert.NotSame(context, GetMetadataContext(previous, mvidA2).EvaluationContext);
                Assert.NotSame(context.Compilation, GetMetadataContext(previous, mvidA2).Compilation);
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));
                VerifyAppDomainMetadataContext(appDomain, mvidA1, mvidA2, mvidB1);
                // B2.M:
                previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(appDomain, blocks, stateB2);
                Assert.NotSame(context, GetMetadataContext(previous, mvidB1).EvaluationContext);
                Assert.Same(context.Compilation, GetMetadataContext(previous, mvidB1).Compilation);
                VerifyResolutionRequests(context, (identityA1, identityA1, 1));
                VerifyAppDomainMetadataContext(appDomain, mvidA1, mvidA2, mvidB1);
            }
        }

        private static void VerifyAppDomainMetadataContext(AppDomain appDomain, params Guid[] moduleVersionIds)
        {
            ExpressionCompilerTestHelpers.VerifyAppDomainMetadataContext(appDomain.GetMetadataContext(), moduleVersionIds);
        }

        [WorkItem(26159, "https://github.com/dotnet/roslyn/issues/26159")]
        [Fact]
        public void TypeOutsideAssemblyReferences()
        {
            var sourceA =
@"public class A
{
    void M()
    {
    }
}";
            var sourceB =
@"#pragma warning disable 169
class B : A
{
    object F;
}";
            var (identityMscorlib, moduleMscorlib) = (MscorlibRef.GetAssemblyIdentity(), MscorlibRef.ToModuleInstance());
            var (identityA, moduleA, refA) = Compile("A", sourceA);
            var (identityB, moduleB, refB) = Compile("B", sourceB, refA);

            using (var runtime = CreateRuntimeInstance(new[] { moduleMscorlib, moduleA, moduleB }))
            {
                var blocks = runtime.Modules.SelectAsArray(m => m.MetadataBlock);
                var stateA = GetContextState(runtime, "A.M");
                const string expr = "((B)this).F";

                // A.M, all assemblies
                var context = CreateMethodContext(new AppDomain(), blocks, stateA, MakeAssemblyReferencesKind.AllAssemblies);
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(expr, out error, testData);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  castclass  ""B""
  IL_0006:  ldfld      ""object B.F""
  IL_000b:  ret
}");

                // A.M, all referenced assemblies
                context = CreateMethodContext(new AppDomain(), blocks, stateA, MakeAssemblyReferencesKind.AllReferences);
                testData = new CompilationTestData();
                context.CompileExpression(expr, out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)", error);
            }
        }

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
                var appDomain = new AppDomain();
                var context = CreateTypeContext(
                    appDomain,
                    typeBlocks,
                    moduleVersionId,
                    typeToken,
                    MakeAssemblyReferencesKind.AllAssemblies);

                Assert.Equal(identityAS2, context.Compilation.GlobalNamespace.GetMembers("A").OfType<NamedTypeSymbol>().Single().ContainingAssembly.Identity);
                Assert.Equal(identityBS2, context.Compilation.GlobalNamespace.GetMembers("B").OfType<NamedTypeSymbol>().Single().ContainingAssembly.Identity);

                string error;
                // A could be ambiguous, but the ambiguity is resolved in favor of the newer assembly.
                var testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Null(error);
                // B could be ambiguous, but the ambiguity is resolved in favor of the newer assembly.
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                Assert.Null(error);
                appDomain.SetMetadataContext(
                    SetMetadataContext(
                        new MetadataContext<CSharpMetadataContext>(typeBlocks, ImmutableDictionary<MetadataContextId, CSharpMetadataContext>.Empty),
                        default(Guid),
                        new CSharpMetadataContext(context.Compilation)));

                // Compile expression with type context with referenced modules only.
                context = CreateTypeContext(
                    appDomain,
                    typeBlocks,
                    moduleVersionId,
                    typeToken,
                    MakeAssemblyReferencesKind.DirectReferencesOnly);
                // A is unrecognized since there were no direct references to AS1 or AS2.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)", error);
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
                Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityBS2.GetDisplayName());
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
                var previous = appDomain.GetMetadataContext();
                context = CreateMethodContext(
                    appDomain,
                    methodBlocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken,
                    MakeAssemblyReferencesKind.AllAssemblies);
                Assert.NotSame(GetMetadataContext(previous).EvaluationContext, context);
                Assert.Same(GetMetadataContext(previous).Compilation, context.Compilation); // re-use type context compilation
                testData = new CompilationTestData();
                // A could be ambiguous, but the ambiguity is resolved in favor of the newer assembly.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Null(error);
                // B could be ambiguous, but the ambiguity is resolved in favor of the newer assembly.
                testData = new CompilationTestData();
                context.CompileExpression("new B()", out error, testData);
                Assert.Null(error);

                // Compile expression with method context with referenced modules only.
                context = CreateMethodContext(
                    appDomain,
                    methodBlocks,
                    symReader,
                    moduleVersionId,
                    methodToken: methodToken,
                    methodVersion: 1,
                    ilOffset: ilOffset,
                    localSignatureToken: localSignatureToken,
                    MakeAssemblyReferencesKind.DirectReferencesOnly);
                // A is unrecognized since there were no direct references to AS1 or AS2.
                testData = new CompilationTestData();
                context.CompileExpression("new A()", out error, testData);
                Assert.Equal("error CS0246: The type or namespace name 'A' could not be found (are you missing a using directive or an assembly reference?)", error);
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
                Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityBS2.GetDisplayName());
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

        private static (AssemblyIdentity Identity, ModuleInstance Module, MetadataReference Reference) Compile(string assemblyName, string source, params MetadataReference[] references)
        {
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: references, assemblyName: assemblyName);
            compilation.VerifyDiagnostics();
            var module = compilation.ToModuleInstance();
            return (compilation.Assembly.Identity, module, module.GetReference());
        }

        private static (AssemblyIdentity Identity, ModuleInstance Module, MetadataReference Reference) Compile(AssemblyIdentity identity, string source, CSharpCompilationOptions options, params MetadataReference[] references)
        {
            var compilation = CreateCompilation(identity, new[] { source }, references: references, options: options);
            compilation.VerifyDiagnostics();
            var module = compilation.ToModuleInstance();
            return (compilation.Assembly.Identity, module, module.GetReference());
        }

        private static void VerifyAssemblyReferences(
            MetadataReference target,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<AssemblyIdentity> expectedIdentities)
        {
            Assert.True(references.Contains(target));
            var modules = references.SelectAsArray(r => r.ToModuleInstance());
            using (var runtime = new RuntimeInstance(modules, DebugInformationFormat.Pdb))
            {
                var moduleVersionId = target.GetModuleVersionId();
                var blocks = runtime.Modules.SelectAsArray(m => m.MetadataBlock);

                IReadOnlyDictionary<string, ImmutableArray<(AssemblyIdentity, MetadataReference)>> referencesBySimpleName;
                var actualReferences = blocks.MakeAssemblyReferences(moduleVersionId, CompilationExtensions.IdentityComparer, MakeAssemblyReferencesKind.DirectReferencesOnly, out referencesBySimpleName);
                Assert.Null(referencesBySimpleName);
                // Verify identities.
                var actualIdentities = actualReferences.SelectAsArray(r => r.GetAssemblyIdentity());
                AssertEx.Equal(expectedIdentities, actualIdentities);
                // Verify identities are unique.
                var uniqueIdentities = actualIdentities.Distinct();
                Assert.Equal(actualIdentities.Length, uniqueIdentities.Length);

                actualReferences = blocks.MakeAssemblyReferences(moduleVersionId, CompilationExtensions.IdentityComparer, MakeAssemblyReferencesKind.AllReferences, out referencesBySimpleName);
                Assert.Equal(2, actualReferences.Length);
                Assert.Equal(moduleVersionId, actualReferences[1].GetModuleVersionId());
                foreach (var reference in references)
                {
                    var identity = reference.GetAssemblyIdentity();
                    var pairs = referencesBySimpleName[identity.Name];
                    var other = pairs.FirstOrDefault(p => identity.Equals(p.Item1));
                    Assert.Equal(identity, other.Item1);
                }
            }
        }

        private static void VerifyResolutionRequests(EvaluationContext context, params (AssemblyIdentity, AssemblyIdentity, int)[] expectedRequests)
        {
            ExpressionCompilerTestHelpers.VerifyResolutionRequests(
                (EEMetadataReferenceResolver)context.Compilation.Options.MetadataReferenceResolver,
                expectedRequests);
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
            var compilationA = CreateCompilationWithMscorlib40AndSystemCore(sourceA, options: TestOptions.DebugDll);
            var identityA = compilationA.Assembly.Identity;
            var moduleA = compilationA.ToModuleInstance();

            var compilationB = CreateCompilationWithMscorlib40AndSystemCore(sourceB, options: TestOptions.DebugDll, references: new[] { moduleA.GetReference() });
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

            IEnumerable<string> CS0433Messages(string type)
            {
                yield return "error CS0433: " + string.Format(CSharpResources.ERR_SameFullNameAggAgg, compilationA.Assembly.Identity, type, compilationB.Assembly.Identity);
                yield return "error CS0433: " + string.Format(CSharpResources.ERR_SameFullNameAggAgg, compilationB.Assembly.Identity, type, compilationA.Assembly.Identity);
            }
            Assert.Contains(errorMessage, CS0433Messages("C1"));

            GetContextState(runtime, "B.M", out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
            contextFactory = CreateMethodContextFactory(moduleVersionId, symReader, methodToken, localSignatureToken);

            // Duplicate type in namespace, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new C1()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Contains(errorMessage, CS0433Messages("C1"));

            // Duplicate type in global namespace, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "new C2()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Contains(errorMessage, CS0433Messages("C2"));

            // Duplicate extension method, at method scope.
            ExpressionCompilerTestHelpers.CompileExpressionWithRetry(blocks, "x.F()", ImmutableArray<Alias>.Empty, contextFactory, getMetaDataBytesPtr: null, errorMessage: out errorMessage, testData: out testData);
            Assert.Equal($"error CS0121: { string.Format(CSharpResources.ERR_AmbigCall, "N.E.F(A)", "N.E.F(A)") }", errorMessage);

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
            Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());

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
            Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());

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
            Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityA.GetDisplayName());
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
            var moduleA = CreateEmptyCompilation(
                sourceA,
                references: new[] { SystemRuntimePP7Ref },
                options: TestOptions.DebugDll).ToModuleInstance();

            var moduleB = CreateEmptyCompilation(
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
            EvaluationContextBase contextFactory(ImmutableArray<MetadataBlock> b, bool u)
            {
                attempts++;
                return EvaluationContext.CreateTypeContext(
                    ToCompilation(b, u, moduleVersionId),
                    moduleVersionId,
                    typeToken);
            }

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
            var systemConsoleComp = CreateCompilation(sourceConsole, options: TestOptions.DebugDll, assemblyName: "System.Console");
            var systemConsoleRef = systemConsoleComp.EmitToImageReference();
            var systemObjectModelComp = CreateCompilation(sourceObjectModel, options: TestOptions.DebugDll, assemblyName: "System.ObjectModel");
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
            var compilation = CreateEmptyCompilation(
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
            compilation = CreateEmptyCompilation(
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
                Assert.Equal(((MethodSymbol)methodData.Method).ReturnType.ContainingAssembly.ToDisplayString(), identityObjectModel.GetDisplayName());
            });
        }

        /// <summary>
        /// Intrinsic methods assembly should not be dropped.
        /// </summary>
        [WorkItem(4140, "https://github.com/dotnet/roslyn/issues/4140")]
        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
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
            var compilationA = CreateCompilationWithMscorlib40AndSystemCore(sourceA, options: TestOptions.DebugDll);
            var moduleA = compilationA.ToModuleInstance();

            var compilationB = CreateCompilationWithMscorlib40AndSystemCore(sourceB, options: TestOptions.DebugDll, references: new[] { moduleA.GetReference() });
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
            EvaluationContextBase contextFactory(ImmutableArray<MetadataBlock> b, bool u)
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
            }

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

        private const string CorLibAssemblyName = "System.Private.CoreLib";

        // An assembly with the expected corlib name and with System.Object should
        // be considered the corlib, even with references to external assemblies.
        [WorkItem(13275, "https://github.com/dotnet/roslyn/issues/13275")]
        [WorkItem(30030, "https://github.com/dotnet/roslyn/issues/30030")]
        [Fact]
        public void CorLibWithAssemblyReferences()
        {
            string sourceLib =
@"public class Private1
{
}
public class Private2
{
}";
            var compLib = CreateCompilation(sourceLib, assemblyName: "System.Private.Library");
            compLib.VerifyDiagnostics();
            var refLib = compLib.EmitToImageReference();

            string sourceCorLib =
@"using System.Runtime.CompilerServices;
[assembly: TypeForwardedTo(typeof(Private2))]
namespace System
{
    public class Object
    {
        public Private1 F() => null;
    }
#pragma warning disable 0436
    public class Void : Object { }
#pragma warning restore 0436
}";
            // Create a custom corlib with a reference to compilation
            // above and a reference to the actual mscorlib.
            var compCorLib = CreateEmptyCompilation(sourceCorLib, assemblyName: CorLibAssemblyName, references: new[] { MscorlibRef, refLib });
            compCorLib.VerifyDiagnostics();
            var objectType = compCorLib.SourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Object");
            Assert.NotNull(objectType.BaseType());

            ImmutableArray<byte> peBytes;
            ImmutableArray<byte> pdbBytes;
            ExpressionCompilerTestHelpers.EmitCorLibWithAssemblyReferences(
                compCorLib,
                null,
                (moduleBuilder, emitOptions) => new PEAssemblyBuilderWithAdditionalReferences(moduleBuilder, emitOptions, objectType),
                out peBytes,
                out pdbBytes);

            using (var reader = new PEReader(peBytes))
            {
                var metadata = reader.GetMetadata();
                var module = metadata.ToModuleMetadata(ignoreAssemblyRefs: true);
                var metadataReader = metadata.ToMetadataReader();
                var moduleInstance = ModuleInstance.Create(metadata, metadataReader.GetModuleVersionIdOrThrow());

                // Verify the module declares System.Object.
                Assert.True(metadataReader.DeclaresTheObjectClass());
                // Verify the PEModule has no assembly references.
                Assert.Equal(0, module.Module.ReferencedAssemblies.Length);
                // Verify the underlying metadata has the expected assembly references.
                var actualReferences = metadataReader.AssemblyReferences.Select(r => metadataReader.GetString(metadataReader.GetAssemblyReference(r).Name)).ToImmutableArray();
                AssertEx.Equal(new[] { "mscorlib", "System.Private.Library" }, actualReferences);

                var source =
@"class C
{
    static void M()
    {
    }
}";
                var comp = CreateEmptyCompilation(source, options: TestOptions.DebugDll, references: new[] { refLib, AssemblyMetadata.Create(module).GetReference() });
                comp.VerifyDiagnostics();

                using (var runtime = RuntimeInstance.Create(new[] { comp.ToModuleInstance(), moduleInstance }))
                {
                    string error;
                    var context = CreateMethodContext(runtime, "C.M");

                    // Valid expression.
                    var testData = new CompilationTestData();
                    context.CompileExpression(
                        "new object()",
                        out error,
                        testData);
                    Assert.Null(error);
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""object..ctor()""
  IL_0005:  ret
}");

                    // Invalid expression: System.Int32 is not defined in corlib above.
                    testData = new CompilationTestData();
                    context.CompileExpression(
                        "1",
                        out error,
                        testData);
                    Assert.Equal("error CS0518: Predefined type 'System.Int32' is not defined or imported", error);

                    // Invalid expression: type in method signature from missing referenced assembly.
                    testData = new CompilationTestData();
                    context.CompileExpression(
                        "(new object()).F()",
                        out error,
                        testData);
                    Assert.Equal("error CS0570: 'object.F()' is not supported by the language", error);

                    // Invalid expression: type forwarded to missing referenced assembly.
                    testData = new CompilationTestData();
                    context.CompileExpression(
                        "new Private2()",
                        out error,
                        testData);
                    Assert.Equal("error CS0246: The type or namespace name 'Private2' could not be found (are you missing a using directive or an assembly reference?)", error);
                }
            }
        }

        // References to missing assembly from PDB custom debug info.
        [WorkItem(13275, "https://github.com/dotnet/roslyn/issues/13275")]
        [Theory]
        [MemberData(nameof(NonNullTypesTrueAndFalseReleaseDll))]
        public void CorLibWithAssemblyReferences_Pdb(CSharpCompilationOptions options)
        {
            string sourceLib =
@"namespace Namespace
{
    public class Private { }
}";
            var compLib = CreateCompilation(sourceLib, assemblyName: "System.Private.Library");
            compLib.VerifyDiagnostics();
            var refLib = compLib.EmitToImageReference(aliases: ImmutableArray.Create("A"));

            string sourceCorLib =
@"extern alias A;
#pragma warning disable 8019
using N = A::Namespace;
namespace System
{
    public class Object
    {
        public void F()
        {
        }
    }
#pragma warning disable 0436
    public class Void : Object { }
#pragma warning restore 0436
}";
            // Create a custom corlib with a reference to compilation
            // above and a reference to the actual mscorlib.
            var compCorLib = CreateEmptyCompilation(sourceCorLib, assemblyName: CorLibAssemblyName, references: new[] { MscorlibRef, refLib }, options: options);
            compCorLib.VerifyDiagnostics();
            var objectType = compCorLib.SourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Object");
            Assert.NotNull(objectType.BaseType());

            var pdbPath = Temp.CreateDirectory().Path;
            ImmutableArray<byte> peBytes;
            ImmutableArray<byte> pdbBytes;
            ExpressionCompilerTestHelpers.EmitCorLibWithAssemblyReferences(
                compCorLib,
                pdbPath,
                (moduleBuilder, emitOptions) => new PEAssemblyBuilderWithAdditionalReferences(moduleBuilder, emitOptions, objectType),
                out peBytes,
                out pdbBytes);
            var symReader = SymReaderFactory.CreateReader(pdbBytes);

            using (var reader = new PEReader(peBytes))
            {
                var metadata = reader.GetMetadata();
                var module = metadata.ToModuleMetadata(ignoreAssemblyRefs: true);
                var metadataReader = metadata.ToMetadataReader();
                var moduleInstance = ModuleInstance.Create(metadata, metadataReader.GetModuleVersionIdOrThrow(), symReader);

                // Verify the module declares System.Object.
                Assert.True(metadataReader.DeclaresTheObjectClass());
                // Verify the PEModule has no assembly references.
                Assert.Equal(0, module.Module.ReferencedAssemblies.Length);
                // Verify the underlying metadata has the expected assembly references.
                var actualReferences = metadataReader.AssemblyReferences.Select(r => metadataReader.GetString(metadataReader.GetAssemblyReference(r).Name)).ToImmutableArray();
                AssertEx.Equal(new[] { "mscorlib", "System.Private.Library" }, actualReferences);

                using (var runtime = RuntimeInstance.Create(new[] { moduleInstance }))
                {
                    string error;
                    var context = CreateMethodContext(runtime, "System.Object.F");
                    var testData = new CompilationTestData();
                    // Invalid import: "using N = A::Namespace;".
                    context.CompileExpression(
                        "new N.Private()",
                        out error,
                        testData);
                    Assert.Equal("error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)", error);
                }
            }
        }

        // An assembly with the expected corlib name but without
        // System.Object should not be considered the corlib.
        [Fact]
        public void CorLibWithAssemblyReferencesNoSystemObject()
        {
            // Assembly with expected corlib name but without System.Object declared.
            string sourceLib =
@"class Private
{
}";
            var compLib = CreateCompilation(sourceLib, assemblyName: CorLibAssemblyName);
            compLib.VerifyDiagnostics();
            var refLib = compLib.EmitToImageReference();

            var source =
@"class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();

            using (var runtime = RuntimeInstance.Create(new[] { comp.ToModuleInstance(), refLib.ToModuleInstance(), MscorlibRef.ToModuleInstance() }))
            {
                string error;
                var context = CreateMethodContext(runtime, "C.M");
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "1.GetType()",
                    out error,
                    testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  call       ""System.Type object.GetType()""
  IL_000b:  ret
}");
            }
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
            return blocks.ToCompilation(moduleVersionId, useReferencedModulesOnly ? MakeAssemblyReferencesKind.DirectReferencesOnly : MakeAssemblyReferencesKind.AllAssemblies);
        }

        private sealed class PEAssemblyBuilderWithAdditionalReferences : PEModuleBuilder, IAssemblyReference
        {
            private readonly CommonPEModuleBuilder _builder;
            private readonly NamespaceTypeDefinitionNoBase _objectType;

            internal PEAssemblyBuilderWithAdditionalReferences(CommonPEModuleBuilder builder, EmitOptions emitOptions, INamespaceTypeDefinition objectType) :
                base((SourceModuleSymbol)builder.CommonSourceModule, emitOptions, builder.OutputKind, builder.SerializationProperties, builder.ManifestResources)
            {
                _builder = builder;
                _objectType = new NamespaceTypeDefinitionNoBase(objectType);
            }

            public override IEnumerable<INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
            {
                foreach (var type in base.GetTopLevelSourceTypeDefinitions(context))
                {
                    yield return (type == _objectType.UnderlyingType) ? _objectType : type;
                }
            }

            public override int CurrentGenerationOrdinal => _builder.CurrentGenerationOrdinal;

            public override ISourceAssemblySymbolInternal SourceAssemblyOpt => _builder.SourceAssemblyOpt;

            public override IEnumerable<IFileReference> GetFiles(EmitContext context) => _builder.GetFiles(context);

            protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<ManagedResource> builder, DiagnosticBag diagnostics)
            {
            }

            internal override SynthesizedAttributeData SynthesizeEmbeddedAttribute()
            {
                throw new NotImplementedException();
            }

            AssemblyIdentity IAssemblyReference.Identity => ((IAssemblyReference)_builder).Identity;

            Version IAssemblyReference.AssemblyVersionPattern => ((IAssemblyReference)_builder).AssemblyVersionPattern;
        }
    }
}
