// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class MissingAssemblyTests : ExpressionCompilerTestBase
    {
        [Fact]
        public void ErrorsWithAssemblyIdentityArguments()
        {
            var identity = new AssemblyIdentity(GetUniqueName());
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_NoTypeDef, identity));
        }

        [Fact]
        public void ErrorsWithAssemblySymbolArguments()
        {
            var assembly = CreateEmptyCompilation("").Assembly;
            var identity = assembly.Identity;
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd, assembly));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd, assembly));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_SingleTypeNameNotFoundFwd, assembly));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_NameNotInContextPossibleMissingReference, assembly));
        }

        [Fact]
        public void ErrorsRequiringSystemCore()
        {
            var identity = EvaluationContextBase.SystemCoreIdentity;
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_NoSuchMemberOrExtension));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_DynamicAttributeMissing));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_DynamicRequiredTypesMissing));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_QueryNoProviderStandard));
            Assert.Same(identity, GetMissingAssemblyIdentity(ErrorCode.ERR_ExtensionAttrNotFound));
        }

        [Fact]
        public void MultipleAssemblyArguments()
        {
            var identity1 = new AssemblyIdentity(GetUniqueName());
            var identity2 = new AssemblyIdentity(GetUniqueName());
            Assert.Equal(identity1, GetMissingAssemblyIdentity(ErrorCode.ERR_NoTypeDef, identity1, identity2));
            Assert.Equal(identity2, GetMissingAssemblyIdentity(ErrorCode.ERR_NoTypeDef, identity2, identity1));
        }

        [Fact]
        public void NoAssemblyArguments()
        {
            Assert.Null(GetMissingAssemblyIdentity(ErrorCode.ERR_NoTypeDef));
            Assert.Null(GetMissingAssemblyIdentity(ErrorCode.ERR_NoTypeDef, "Not an assembly"));
        }

        [Fact]
        public void ERR_NoTypeDef()
        {
            var libSource = @"
public class Missing { }
";

            var source = @"
public class C
{
    public void M(Missing parameter)
    {
    }
}
";
            var libRef = CreateCompilation(libSource, assemblyName: "Lib").EmitToImageReference();
            var comp = CreateCompilation(source, new[] { libRef }, TestOptions.DebugDll);

            WithRuntimeInstance(comp, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedError = "error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.";
                var expectedMissingAssemblyIdentity = new AssemblyIdentity("Lib");

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                context.CompileExpression(
                    "parameter",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(expectedError, actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [Fact]
        public void ERR_QueryNoProviderStandard()
        {
            var source = @"
public class C
{
    public void M(int[] array)
    {
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            WithRuntimeInstance(comp, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedError = "error CS1935: Could not find an implementation of the query pattern for source type 'int[]'.  'Select' not found.  Are you missing required assembly references or a using directive for 'System.Linq'?";
                var expectedMissingAssemblyIdentity = EvaluationContextBase.SystemCoreIdentity;

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                context.CompileExpression(
                    "from i in array select i",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(expectedError, actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1151888")]
        public void ERR_NoSuchMemberOrExtension_CompilationReferencesSystemCore()
        {
            var source = @"
using System.Linq;

public class C
{
    public void M(int[] array)
    {
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedErrorTemplate = "error CS1061: 'int[]' does not contain a definition for '{0}' and no accessible extension method '{0}' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)";
                var expectedMissingAssemblyIdentity = EvaluationContextBase.SystemCoreIdentity;

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                context.CompileExpression(
                    "array.Count()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(string.Format(expectedErrorTemplate, "Count"), actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());

                context.CompileExpression(
                    "array.NoSuchMethod()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(string.Format(expectedErrorTemplate, "NoSuchMethod"), actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        /// <remarks>
        /// The fact that the compilation does not reference System.Core has no effect since
        /// this test only covers our ability to identify an assembly to attempt to load, not
        /// our ability to actually load or consume it.
        /// </remarks>
        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1151888")]
        public void ERR_NoSuchMemberOrExtension_CompilationDoesNotReferenceSystemCore()
        {
            var source = @"
using System.Linq;

public class C
{
    public void M(int[] array)
    {
    }
}

namespace System.Linq
{
    public class Dummy
    {
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedErrorTemplate = "error CS1061: 'int[]' does not contain a definition for '{0}' and no accessible extension method '{0}' accepting a first argument of type 'int[]' could be found (are you missing a using directive or an assembly reference?)";
                var expectedMissingAssemblyIdentity = EvaluationContextBase.SystemCoreIdentity;

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                context.CompileExpression(
                    "array.Count()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(string.Format(expectedErrorTemplate, "Count"), actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());

                context.CompileExpression(
                    "array.NoSuchMethod()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(string.Format(expectedErrorTemplate, "NoSuchMethod"), actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [Fact]
        public void ForwardingErrors()
        {
            var il = @"
.assembly extern mscorlib { }
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Forwarded
{
  .assembly extern pe2
}

.class extern forwarder NS.Forwarded
{
  .assembly extern pe2
}

.class public auto ansi beforefieldinit Dummy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var csharp = @"
class C
{
    static void M(Dummy d)
    {
    }
}
";
            var ilRef = CompileIL(il, prependDefaultHeader: false);
            var comp = CreateCompilation(csharp, new[] { ilRef });
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var expectedMissingAssemblyIdentity = new AssemblyIdentity("pe2");

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

                context.CompileExpression(
                    "new global::Forwarded()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(
                    "error CS1068: The type name 'Forwarded' could not be found in the global namespace. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.",
                    actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());

                context.CompileExpression(
                    "new Forwarded()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(
                    "error CS1070: The type name 'Forwarded' could not be found. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider adding a reference to that assembly.",
                    actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());

                context.CompileExpression(
                    "new NS.Forwarded()",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(
                    "error CS1069: The type name 'Forwarded' could not be found in the namespace 'NS'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.",
                    actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [Fact]
        public unsafe void ShouldTryAgain_Success()
        {
            var comp = CreateCompilation("public class C { }");
            using (var pinned = new PinnedMetadata(GetMetadataBytes(comp)))
            {
                IntPtr gmdbpf(AssemblyIdentity assemblyIdentity, out uint uSize)
                {
                    uSize = (uint)pinned.Size;
                    return pinned.Pointer;
                }

                var references = ImmutableArray<MetadataBlock>.Empty;
                var missingAssemblyIdentity = new AssemblyIdentity("A");
                var missingAssemblyIdentities = ImmutableArray.Create(missingAssemblyIdentity);
                Assert.True(ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));

                var newReference = references.Single();
                Assert.Equal(pinned.Pointer, newReference.Pointer);
                Assert.Equal(pinned.Size, newReference.Size);
            }
        }

        [Fact]
        public unsafe void ShouldTryAgain_Mixed()
        {
            var comp1 = CreateCompilation("public class C { }", assemblyName: GetUniqueName());
            var comp2 = CreateCompilation("public class D { }", assemblyName: GetUniqueName());
            using (PinnedMetadata pinned1 = new PinnedMetadata(GetMetadataBytes(comp1)),
                pinned2 = new PinnedMetadata(GetMetadataBytes(comp2)))
            {
                var assemblyIdentity1 = comp1.Assembly.Identity;
                var assemblyIdentity2 = comp2.Assembly.Identity;
                Assert.NotEqual(assemblyIdentity1, assemblyIdentity2);

                IntPtr gmdbpf(AssemblyIdentity assemblyIdentity, out uint uSize)
                {
                    if (assemblyIdentity == assemblyIdentity1)
                    {
                        uSize = (uint)pinned1.Size;
                        return pinned1.Pointer;
                    }
                    else if (assemblyIdentity == assemblyIdentity2)
                    {
                        uSize = (uint)pinned2.Size;
                        return pinned2.Pointer;
                    }
                    else
                    {
                        Marshal.ThrowExceptionForHR(DkmExceptionUtilities.CORDBG_E_MISSING_METADATA);
                        throw ExceptionUtilities.Unreachable();
                    }
                }

                var references = ImmutableArray.Create(default(MetadataBlock));
                var unknownAssemblyIdentity = new AssemblyIdentity(GetUniqueName());
                var missingAssemblyIdentities = ImmutableArray.Create(assemblyIdentity1, unknownAssemblyIdentity, assemblyIdentity2);
                Assert.True(ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
                Assert.Equal(3, references.Length);

                Assert.Equal(default(MetadataBlock), references[0]);

                Assert.Equal(pinned1.Pointer, references[1].Pointer);
                Assert.Equal(pinned1.Size, references[1].Size);

                Assert.Equal(pinned2.Pointer, references[2].Pointer);
                Assert.Equal(pinned2.Size, references[2].Size);
            }
        }

        [Fact]
        public void ShouldTryAgain_CORDBG_E_MISSING_METADATA()
        {
            ShouldTryAgain_False(
                (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                {
                    Marshal.ThrowExceptionForHR(DkmExceptionUtilities.CORDBG_E_MISSING_METADATA);
                    throw ExceptionUtilities.Unreachable();
                });
        }

        [Fact]
        public void ShouldTryAgain_COR_E_BADIMAGEFORMAT()
        {
            ShouldTryAgain_False(
                (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                {
                    Marshal.ThrowExceptionForHR(DkmExceptionUtilities.COR_E_BADIMAGEFORMAT);
                    throw ExceptionUtilities.Unreachable();
                });
        }

        [Fact]
        public void ShouldTryAgain_ObjectDisposedException()
        {
            ShouldTryAgain_False(
                (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                {
                    throw new ObjectDisposedException("obj");
                });
        }

        [Fact]
        public void ShouldTryAgain_RPC_E_DISCONNECTED()
        {
            static IntPtr gmdbpf(AssemblyIdentity assemblyIdentity, out uint uSize)
            {
                Marshal.ThrowExceptionForHR(unchecked((int)0x80010108));
                throw ExceptionUtilities.Unreachable();
            }

            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.Throws<COMException>(() => ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
        }

        [Fact]
        public void ShouldTryAgain_Exception()
        {
            static IntPtr gmdbpf(AssemblyIdentity assemblyIdentity, out uint uSize)
            {
                throw new Exception();
            }

            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.Throws<Exception>(() => ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
        }

        private static void ShouldTryAgain_False(DkmUtilities.GetMetadataBytesPtrFunction gmdbpf)
        {
            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.False(ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
            Assert.Empty(references);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1124725")]
        public void PseudoVariableType()
        {
            var source =
@"class C
{
    static void M()
    {
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);
            WithRuntimeInstance(comp, new[] { CSharpRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                const string expectedError = "error CS0012: The type 'Exception' is defined in an assembly that is not referenced. You must add a reference to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.";
                var expectedMissingAssemblyIdentity = comp.Assembly.CorLibrary.Identity;

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
                context.CompileExpression(
                    "$stowedexception",
                    DkmEvaluationFlags.TreatAsExpression,
                    ImmutableArray.Create(ExceptionAlias("Microsoft.CSharp.RuntimeBinder.RuntimeBinderException, Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", stowed: true)),
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(expectedError, actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")]
        [ConditionalFact(typeof(OSVersionWin8))]
        public void NotYetLoadedWinMds()
        {
            var source =
@"class C
{
    static void M(Windows.Storage.StorageFolder f)
    {
    }
}";
            var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.DebugDll);
            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage");
            Assert.True(runtimeAssemblies.Any());

            WithRuntimeInstance(comp, new[] { MscorlibRef }.Concat(runtimeAssemblies), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                const string expectedError = "error CS0234: The type or namespace name 'UI' does not exist in the namespace 'Windows' (are you missing an assembly reference?)";
                var expectedMissingAssemblyIdentity = new AssemblyIdentity("Windows.UI", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
                context.CompileExpression(
                    "typeof(@Windows.UI.Colors)",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(expectedError, actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        /// <remarks>
        /// Windows.UI.Xaml is the only (win8) winmd with more than two parts.
        /// </remarks>
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")]
        [ConditionalFact(typeof(OSVersionWin8))]
        public void NotYetLoadedWinMds_MultipleParts()
        {
            var source =
@"class C
{
    static void M(Windows.UI.Colors c)
    {
    }
}";
            var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.DebugDll);
            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI");
            Assert.True(runtimeAssemblies.Any());

            WithRuntimeInstance(comp, new[] { MscorlibRef }.Concat(runtimeAssemblies), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                const string expectedError = "error CS0234: The type or namespace name 'Xaml' does not exist in the namespace 'Windows.UI' (are you missing an assembly reference?)";
                var expectedMissingAssemblyIdentity = new AssemblyIdentity("Windows.UI.Xaml", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);

                ResultProperties resultProperties;
                string actualError;
                ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
                context.CompileExpression(
                    "typeof(Windows.@UI.Xaml.Application)",
                    DkmEvaluationFlags.None,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out actualError,
                    out actualMissingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData: null);
                Assert.Equal(expectedError, actualError);
                Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154988")]
        public void CompileWithRetrySameErrorReported()
        {
            var source = @" 
class C 
{ 
    void M() 
    { 
    } 
}";
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var missingModule = runtime.Modules.First();
                var missingIdentity = missingModule.GetMetadataReader().ReadAssemblyIdentityOrThrow();

                var numRetries = 0;
                string errorMessage;
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                    runtime.Modules.Select(m => m.MetadataBlock).ToImmutableArray(),
                    context,
                    (_, diagnostics) =>
                    {
                        numRetries++;
                        Assert.InRange(numRetries, 0, 2); // We don't want to loop forever... 
                        diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_NoTypeDef, "MissingType", missingIdentity), Location.None));
                        return null;
                    },
                    (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                    {
                        uSize = (uint)missingModule.MetadataLength;
                        return missingModule.MetadataAddress;
                    },
                    out errorMessage);

                Assert.Equal(2, numRetries); // Ensure that we actually retried and that we bailed out on the second retry if the same identity was seen in the diagnostics.
                Assert.Equal($"error CS0012: {string.Format(CSharpResources.ERR_NoTypeDef, "MissingType", missingIdentity)}", errorMessage);
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1151888")]
        public void SucceedOnRetry()
        {
            var source = @" 
class C 
{ 
    void M() 
    { 
    } 
}";
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var missingModule = runtime.Modules.First();
                var missingIdentity = missingModule.GetMetadataReader().ReadAssemblyIdentityOrThrow();

                var shouldSucceed = false;
                string errorMessage;
                var compileResult = ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                    runtime.Modules.Select(m => m.MetadataBlock).ToImmutableArray(),
                    context,
                    (_, diagnostics) =>
                    {
                        if (shouldSucceed)
                        {
                            return TestCompileResult.Instance;
                        }
                        else
                        {
                            shouldSucceed = true;
                            diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_NoTypeDef, "MissingType", missingIdentity), Location.None));
                            return null;
                        }
                    },
                    (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                    {
                        uSize = (uint)missingModule.MetadataLength;
                        return missingModule.MetadataAddress;
                    },
                    out errorMessage);

                Assert.Same(TestCompileResult.Instance, compileResult);
                Assert.Null(errorMessage);
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2547")]
        public void TryDifferentLinqLibraryOnRetry()
        {
            var source = @"
using System.Linq;
class C 
{ 
    void M(string[] args) 
    {
    } 
}
class UseLinq
{
    bool b = Enumerable.Any<int>(null);
}";

            var compilation = CreateEmptyCompilation(source, new[] { MscorlibRef, SystemCoreRef });
            WithRuntimeInstance(compilation, new[] { MscorlibRef }, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var systemCore = SystemCoreRef.ToModuleInstance();
                var fakeSystemLinq = CreateCompilationWithMscorlib461("", assemblyName: "System.Linq").
                    EmitToImageReference().ToModuleInstance();

                string errorMessage;
                CompilationTestData testData;
                int retryCount = 0;
                var compileResult = ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                    runtime.Modules.Select(m => m.MetadataBlock).ToImmutableArray(),
                    "args.Where(a => a.Length > 0)",
                    ImmutableArray<Alias>.Empty,
                    (_1, _2) => context, // ignore new blocks and just keep using the same failed context...
                    (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                    {
                        retryCount++;
                        MetadataBlock block;
                        switch (retryCount)
                        {
                            case 1:
                                Assert.Equal(EvaluationContextBase.SystemLinqIdentity, assemblyIdentity);
                                block = fakeSystemLinq.MetadataBlock;
                                break;
                            case 2:
                                Assert.Equal(EvaluationContextBase.SystemCoreIdentity, assemblyIdentity);
                                block = systemCore.MetadataBlock;
                                break;
                            default:
                                throw ExceptionUtilities.Unreachable();
                        }
                        uSize = (uint)block.Size;
                        return block.Pointer;
                    },
                    errorMessage: out errorMessage,
                    testData: out testData);

                Assert.Equal(2, retryCount);
            });
        }

        [Fact]
        public void TupleNoSystemRuntime()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = 1;
        var y = (x, 2);
        var z = (3, 4, (5, 6));
    }
}";
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "y",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                System.ValueTuple<int, int> V_1, //y
                System.ValueTuple<int, int, System.ValueTuple<int, int>> V_2) //z
  IL_0000:  ldloc.1
  IL_0001:  ret
}");
        }

        [Fact]
        public void TupleNoSystemRuntimeWithCSharp7_1()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = 1;
        var y = (x, 2);
        var z = (3, 4, (5, 6));
    }
}";
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "y",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                System.ValueTuple<int, int> V_1, //y
                System.ValueTuple<int, int, System.ValueTuple<int, int>> V_2) //z
  IL_0000:  ldloc.1
  IL_0001:  ret
}",
LanguageVersion.CSharp7_1);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16879")]
        public void NonTupleNoSystemRuntime()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = 1;
        var y = (x, 2);
        var z = (3, 4, (5, 6));
    }
}";
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "x",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                System.ValueTuple<int, int> V_1, //y
                System.ValueTuple<int, int, System.ValueTuple<int, int>> V_2) //z
  IL_0000:  ldloc.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void NonTupleNoSystemRuntimeWithCSharp7_1()
        {
            var source =
@"class C
{
    static void M()
    {
        var x = 1;
        var y = (x, 2);
        var z = (3, 4, (5, 6));
    }
}";
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "x",
@"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (int V_0, //x
                System.ValueTuple<int, int> V_1, //y
                System.ValueTuple<int, int, System.ValueTuple<int, int>> V_2) //z
  IL_0000:  ldloc.0
  IL_0001:  ret
}",
LanguageVersion.CSharp7_1);
        }

        private static void TupleContextNoSystemRuntime(string source, string methodName, string expression, string expectedIL,
            LanguageVersion languageVersion = LanguageVersion.CSharp7)
        {
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion),
                references: new[] { SystemRuntimeFacadeRef, ValueTupleRef }, options: TestOptions.DebugDll);
            using (var systemRuntime = SystemRuntimeFacadeRef.ToModuleInstance())
            {
                WithRuntimeInstance(comp, new[] { MscorlibRef, ValueTupleRef }, runtime =>
                {
                    ImmutableArray<MetadataBlock> blocks;
                    Guid moduleVersionId;
                    ISymUnmanagedReader symReader;
                    int methodToken;
                    int localSignatureToken;
                    GetContextState(runtime, methodName, out blocks, out moduleVersionId, out symReader, out methodToken, out localSignatureToken);
                    string errorMessage;
                    CompilationTestData testData;
                    int retryCount = 0;
                    var compileResult = ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                        runtime.Modules.Select(m => m.MetadataBlock).ToImmutableArray(),
                        expression,
                        ImmutableArray<Alias>.Empty,
                        (b, u) => EvaluationContext.CreateMethodContext(
                            b.ToCompilation(default(Guid), MakeAssemblyReferencesKind.AllAssemblies),
                            symReader,
                            moduleVersionId,
                            methodToken,
                            methodVersion: 1,
                            ilOffset: 0,
                            localSignatureToken: localSignatureToken),
                        (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                        {
                            retryCount++;
                            Assert.Equal("System.Runtime", assemblyIdentity.Name);
                            var block = systemRuntime.MetadataBlock;
                            uSize = (uint)block.Size;
                            return block.Pointer;
                        },
                        errorMessage: out errorMessage,
                        testData: out testData);
                    Assert.Equal(1, retryCount);
                    testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL);
                });
            }
        }

        private sealed class TestCompileResult : CompileResult
        {
            public static readonly CompileResult Instance = new TestCompileResult();

            private TestCompileResult()
                : base(null, null, null, null)
            {
            }

            public override Guid GetCustomTypeInfo(out ReadOnlyCollection<byte> payload)
            {
                throw new NotImplementedException();
            }
        }

        private static AssemblyIdentity GetMissingAssemblyIdentity(ErrorCode code, params object[] arguments)
        {
            var missingAssemblyIdentities = EvaluationContext.GetMissingAssemblyIdentitiesHelper(code, arguments, EvaluationContextBase.SystemCoreIdentity);
            return missingAssemblyIdentities.IsDefault ? null : missingAssemblyIdentities.Single();
        }

        private static ImmutableArray<byte> GetMetadataBytes(Compilation comp)
        {
            var imageReference = (MetadataImageReference)comp.EmitToImageReference();
            var assemblyMetadata = (AssemblyMetadata)imageReference.GetMetadataNoCopy();
            var moduleMetadata = assemblyMetadata.GetModules()[0];
            return moduleMetadata.Module.PEReaderOpt.GetMetadata().GetContent();
        }
    }
}
