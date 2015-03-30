// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Roslyn.Test.PdbUtilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
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
            var assembly = CreateCompilation("").Assembly;
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
            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Lib").EmitToImageReference();
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef }, TestOptions.DebugDll);
            var context = CreateMethodContextWithReferences(comp, "C.M", MscorlibRef);

            var expectedError = "error CS0012: The type 'Missing' is defined in an assembly that is not referenced. You must add a reference to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.";
            var expectedMissingAssemblyIdentity = new AssemblyIdentity("Lib");

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "parameter",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(expectedError, actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
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
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef }, TestOptions.DebugDll);
            var context = CreateMethodContextWithReferences(comp, "C.M", MscorlibRef);

            var expectedError = "(1,11): error CS1935: Could not find an implementation of the query pattern for source type 'int[]'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?";
            var expectedMissingAssemblyIdentity = EvaluationContextBase.SystemCoreIdentity;

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "from i in array select i",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(expectedError, actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
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
            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilationWithMscorlib(csharp, new[] { ilRef });
            var runtime = CreateRuntimeInstance(comp);
            var context = CreateMethodContext(runtime, "C.M");

            var expectedMissingAssemblyIdentity = new AssemblyIdentity("pe2");

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;

            context.CompileExpression(
                DefaultInspectionContext.Instance,
                "new global::Forwarded()",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
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
                DefaultInspectionContext.Instance,
                "new Forwarded()",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
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
                DefaultInspectionContext.Instance,
                "new NS.Forwarded()",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(
                "error CS1069: The type name 'Forwarded' could not be found in the namespace 'NS'. This type has been forwarded to assembly 'pe2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' Consider adding a reference to that assembly.",
                actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
        }

        [Fact]
        public unsafe void ShouldTryAgain_Success()
        {
            var comp = CreateCompilationWithMscorlib("public class C { }");
            using (var pinned = new PinnedMetadata(GetMetadataBytes(comp)))
            {
                DkmUtilities.GetMetadataBytesPtrFunction gmdbpf = (AssemblyIdentity assemblyIdentity, out uint uSize) =>
                {
                    uSize = (uint)pinned.Size;
                    return pinned.Pointer;
                };

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
            var comp1 = CreateCompilationWithMscorlib("public class C { }", assemblyName: GetUniqueName());
            var comp2 = CreateCompilationWithMscorlib("public class D { }", assemblyName: GetUniqueName());
            using (PinnedMetadata pinned1 = new PinnedMetadata(GetMetadataBytes(comp1)),
                pinned2 = new PinnedMetadata(GetMetadataBytes(comp2)))
            {
                var assemblyIdentity1 = comp1.Assembly.Identity;
                var assemblyIdentity2 = comp2.Assembly.Identity;
                Assert.NotEqual(assemblyIdentity1, assemblyIdentity2);

                DkmUtilities.GetMetadataBytesPtrFunction gmdbpf = (AssemblyIdentity assemblyIdentity, out uint uSize) =>
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
                        Marshal.ThrowExceptionForHR(unchecked((int)MetadataUtilities.CORDBG_E_MISSING_METADATA));
                        throw ExceptionUtilities.Unreachable;
                    }
                };

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
            DkmUtilities.GetMetadataBytesPtrFunction gmdbpf = (AssemblyIdentity assemblyIdentity, out uint uSize) =>
            {
                Marshal.ThrowExceptionForHR(unchecked((int)MetadataUtilities.CORDBG_E_MISSING_METADATA));
                throw ExceptionUtilities.Unreachable;
            };

            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.False(ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
            Assert.Empty(references);
        }

        [Fact]
        public void ShouldTryAgain_COR_E_BADIMAGEFORMAT()
        {
            DkmUtilities.GetMetadataBytesPtrFunction gmdbpf = (AssemblyIdentity assemblyIdentity, out uint uSize) =>
            {
                Marshal.ThrowExceptionForHR(unchecked((int)MetadataUtilities.COR_E_BADIMAGEFORMAT));
                throw ExceptionUtilities.Unreachable;
            };

            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.False(ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
            Assert.Empty(references);
        }

        [Fact]
        public void ShouldTryAgain_OtherException()
        {
            DkmUtilities.GetMetadataBytesPtrFunction gmdbpf = (AssemblyIdentity assemblyIdentity, out uint uSize) =>
            {
                throw new Exception();
            };

            var references = ImmutableArray<MetadataBlock>.Empty;
            var missingAssemblyIdentities = ImmutableArray.Create(new AssemblyIdentity("A"));
            Assert.Throws<Exception>(() => ExpressionCompiler.ShouldTryAgainWithMoreMetadataBlocks(gmdbpf, missingAssemblyIdentities, ref references));
        }

        [WorkItem(1124725, "DevDiv")]
        [Fact]
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
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.DebugDll);
            var context = CreateMethodContextWithReferences(comp, "C.M", CSharpRef, ExpressionCompilerTestHelpers.IntrinsicAssemblyReference);

            const string expectedError = "error CS0012: The type 'Exception' is defined in an assembly that is not referenced. You must add a reference to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.";
            var expectedMissingAssemblyIdentity = comp.Assembly.CorLibrary.Identity;

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
            var result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("$stowedexception", "Microsoft.CSharp.RuntimeBinder.RuntimeBinderException, Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"),
                "$stowedexception",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(expectedError, actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
        }

        [WorkItem(1114866)]
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
            var comp = CreateCompilationWithMscorlib(source, WinRtRefs, TestOptions.DebugDll);
            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage");
            Assert.True(runtimeAssemblies.Any());
            var context = CreateMethodContextWithReferences(comp, "C.M", ImmutableArray.Create(MscorlibRef).Concat(runtimeAssemblies));

            const string expectedError = "error CS0234: The type or namespace name 'UI' does not exist in the namespace 'Windows' (are you missing an assembly reference?)";
            var expectedMissingAssemblyIdentity = new AssemblyIdentity("Windows.UI", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "typeof(@Windows.UI.Colors)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(expectedError, actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
        }

        /// <remarks>
        /// Windows.UI.Xaml is the only (win8) winmd with more than two parts.
        /// </remarks>
        [WorkItem(1114866)]
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
            var comp = CreateCompilationWithMscorlib(source, WinRtRefs, TestOptions.DebugDll);
            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI");
            Assert.True(runtimeAssemblies.Any());
            var context = CreateMethodContextWithReferences(comp, "C.M", ImmutableArray.Create(MscorlibRef).Concat(runtimeAssemblies));

            const string expectedError = "error CS0234: The type or namespace name 'Xaml' does not exist in the namespace 'Windows.UI' (are you missing an assembly reference?)";
            var expectedMissingAssemblyIdentity = new AssemblyIdentity("Windows.UI.Xaml", contentType: System.Reflection.AssemblyContentType.WindowsRuntime);

            ResultProperties resultProperties;
            string actualError;
            ImmutableArray<AssemblyIdentity> actualMissingAssemblyIdentities;
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "typeof(Windows.@UI.Xaml.Application)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                out resultProperties,
                out actualError,
                out actualMissingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData: null);
            Assert.Equal(expectedError, actualError);
            Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single());
        }

        private EvaluationContext CreateMethodContextWithReferences(Compilation comp, string methodName, params MetadataReference[] references)
        {
            return CreateMethodContextWithReferences(comp, methodName, ImmutableArray.CreateRange(references));
        }

        private EvaluationContext CreateMethodContextWithReferences(Compilation comp, string methodName, ImmutableArray<MetadataReference> references)
        {
            byte[] exeBytes;
            byte[] pdbBytes;
            ImmutableArray<MetadataReference> unusedReferences;
            var result = comp.EmitAndGetReferences(out exeBytes, out pdbBytes, out unusedReferences);
            Assert.True(result);

            var runtime = CreateRuntimeInstance(GetUniqueName(), references, exeBytes, new SymReader(pdbBytes));
            return CreateMethodContext(runtime, methodName);
        }

        private static AssemblyIdentity GetMissingAssemblyIdentity(ErrorCode code, params object[] arguments)
        {
            var missingAssemblyIdentities = EvaluationContext.GetMissingAssemblyIdentitiesHelper(code, arguments);
            return missingAssemblyIdentities.IsDefault ? null : missingAssemblyIdentities.Single();
        }

        private static ImmutableArray<byte> GetMetadataBytes(Compilation comp)
        {
            var imageReference = (MetadataImageReference)comp.EmitToImageReference();
            var assemblyMetadata = (AssemblyMetadata)imageReference.GetMetadata();
            var moduleMetadata = assemblyMetadata.GetModules()[0];
            return moduleMetadata.Module.PEReaderOpt.GetMetadata().GetContent();
        }
    }
}
