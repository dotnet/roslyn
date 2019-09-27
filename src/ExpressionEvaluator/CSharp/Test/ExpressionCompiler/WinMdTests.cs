// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Resources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class WinMdTests : ExpressionCompilerTestBase
    {
        /// <summary>
        /// Handle runtime assemblies rather than Windows.winmd
        /// (compile-time assembly) since those are the assemblies
        /// loaded in the debuggee.
        /// </summary>
        [WorkItem(981104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981104")]
        [ConditionalFact(typeof(OSVersionWin8))]
        public void Win8RuntimeAssemblies()
        {
            var source =
@"class C
{
    static void M(Windows.Storage.StorageFolder f, Windows.Foundation.Collections.PropertySet p)
    {
    }
}";
            var compilation0 = CreateEmptyCompilation(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: WinRtRefs);

            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage", "Windows.Foundation.Collections");
            Assert.True(runtimeAssemblies.Length >= 2);

            // no reference to Windows.winmd
            WithRuntimeInstance(compilation0, new[] { MscorlibRef }.Concat(runtimeAssemblies), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("(p == null) ? f : null", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldnull
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  ret
}");
            });
        }

        [ConditionalFact(typeof(OSVersionWin8))]
        public void Win8RuntimeAssemblies_ExternAlias()
        {
            var source =
@"extern alias X;
class C
{
    static void M(X::Windows.Storage.StorageFolder f)
    {
    }
}";
            var compilation0 = CreateEmptyCompilation(
                source,
                options: TestOptions.DebugDll,
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: WinRtRefs.Select(r => r.Display == "Windows" ? r.WithAliases(new[] { "X" }) : r));

            var runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage");
            Assert.True(runtimeAssemblies.Length >= 1);

            // no reference to Windows.winmd
            WithRuntimeInstance(compilation0, new[] { MscorlibRef }.Concat(runtimeAssemblies), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("X::Windows.Storage.FileProperties.PhotoOrientation.Unspecified", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}");
            });
        }

        [Fact]
        public void Win8OnWin8()
        {
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows.Storage");
        }

        [Fact]
        public void Win8OnWin10()
        {
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows.Storage");
        }

        [WorkItem(1108135, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108135")]
        [Fact]
        public void Win10OnWin10()
        {
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows");
        }

        private void CompileTimeAndRuntimeAssemblies(
            ImmutableArray<MetadataReference> compileReferences,
            ImmutableArray<MetadataReference> runtimeReferences,
            string storageAssemblyName)
        {
            var source =
@"class C
{
    static void M(LibraryA.A a, LibraryB.B b, Windows.Data.Text.TextSegment t, Windows.Storage.StorageFolder f)
    {
    }
}";
            var compilation0 = CreateEmptyCompilation(source, compileReferences, TestOptions.DebugDll);
            WithRuntimeInstance(compilation0, runtimeReferences, runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("(object)a ?? (object)b ?? (object)t ?? f", out error, testData);
                Assert.Null(error);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0010
  IL_0004:  pop
  IL_0005:  ldarg.1
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0010
  IL_0009:  pop
  IL_000a:  ldarg.2
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ldarg.3
  IL_0010:  ret
}");
                testData = new CompilationTestData();
                var result = context.CompileExpression("default(Windows.Storage.StorageFolder)", out error, testData);
                Assert.Null(error);
                var methodData = testData.GetMethodData("<>x.<>m0");
                methodData.VerifyIL(
@"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}");
                // Check return type is from runtime assembly.
                var assemblyReference = AssemblyMetadata.CreateFromImage(result.Assembly).GetReference();
                var compilation = CSharpCompilation.Create(
                    assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                    references: runtimeReferences.Concat(ImmutableArray.Create<MetadataReference>(assemblyReference)));
                var assembly = ImmutableArray.CreateRange(result.Assembly);
                using (var metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(assembly)))
                {
                    var reader = metadata.MetadataReader;
                    var typeDef = reader.GetTypeDef("<>x");
                    var methodHandle = reader.GetMethodDefHandle(typeDef, "<>m0");
                    var module = (PEModuleSymbol)compilation.GetMember("<>x").ContainingModule;
                    var metadataDecoder = new MetadataDecoder(module);
                    SignatureHeader signatureHeader;
                    BadImageFormatException metadataException;
                    var parameters = metadataDecoder.GetSignatureForMethod(methodHandle, out signatureHeader, out metadataException);
                    Assert.Equal(5, parameters.Length);
                    var actualReturnType = parameters[0].Type;
                    Assert.Equal(TypeKind.Class, actualReturnType.TypeKind); // not error
                    var expectedReturnType = compilation.GetMember("Windows.Storage.StorageFolder");
                    Assert.Equal(expectedReturnType, actualReturnType);
                    Assert.Equal(storageAssemblyName, actualReturnType.ContainingAssembly.Name);
                }
            });
        }

        /// <summary>
        /// Assembly-qualified name containing "ContentType=WindowsRuntime",
        /// and referencing runtime assembly.
        /// </summary>
        [WorkItem(1116143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1116143")]
        [ConditionalFact(typeof(OSVersionWin8), typeof(IsRelease))] // https://github.com/dotnet/roslyn/issues/25702
        public void AssemblyQualifiedName()
        {
            var source =
@"class C
{
    static void M(Windows.Storage.StorageFolder f, Windows.Foundation.Collections.PropertySet p)
    {
    }
}";
            var compilation = CreateEmptyCompilation(source, WinRtRefs, TestOptions.DebugDll);
            WithRuntimeInstance(compilation, new[] { MscorlibRef }.Concat(ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage", "Windows.Foundation.Collections")), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");

                var aliases = ImmutableArray.Create(
                    VariableAlias("s", "Windows.Storage.StorageFolder, Windows.Storage, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                    VariableAlias("d", "Windows.Foundation.DateTime, Windows.Foundation, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"));

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression(
                    "(object)s.Attributes ?? d.UniversalTime",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    out error,
                    testData);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_000a:  castclass  ""Windows.Storage.StorageFolder""
  IL_000f:  callvirt   ""Windows.Storage.FileAttributes Windows.Storage.StorageFolder.Attributes.get""
  IL_0014:  box        ""Windows.Storage.FileAttributes""
  IL_0019:  dup
  IL_001a:  brtrue.s   IL_0036
  IL_001c:  pop
  IL_001d:  ldstr      ""d""
  IL_0022:  call       ""object Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(string)""
  IL_0027:  unbox.any  ""Windows.Foundation.DateTime""
  IL_002c:  ldfld      ""long Windows.Foundation.DateTime.UniversalTime""
  IL_0031:  box        ""long""
  IL_0036:  ret
}");
            });
        }

        [WorkItem(1117084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1117084")]
        [Fact]
        public void OtherFrameworkAssembly()
        {
            var source =
@"class C
{
    static void M(Windows.UI.Xaml.FrameworkElement f)
    {
    }
}";
            var compilation = CreateEmptyCompilation(source, WinRtRefs, TestOptions.DebugDll);
            WithRuntimeInstance(compilation, new[] { MscorlibRef }.Concat(ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Foundation", "Windows.UI", "Windows.UI.Xaml")), runtime =>
            {
                var context = CreateMethodContext(runtime, "C.M");
                string error;
                ResultProperties resultProperties;
                ImmutableArray<AssemblyIdentity> missingAssemblyIdentities;
                var testData = new CompilationTestData();
                var result = context.CompileExpression(
                    "f.RenderSize",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    DebuggerDiagnosticFormatter.Instance,
                    out resultProperties,
                    out error,
                    out missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData);
                var expectedAssemblyIdentity = WinRtRefs.Single(r => r.Display == "System.Runtime.WindowsRuntime.dll").GetAssemblyIdentity();
                Assert.Equal(expectedAssemblyIdentity, missingAssemblyIdentities.Single());
            });
        }

        [WorkItem(1154988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154988")]
        [ConditionalFact(typeof(OSVersionWin8))]
        public void WinMdAssemblyReferenceRequiresRedirect()
        {
            var source =
@"class C : Windows.UI.Xaml.Controls.UserControl
{
    static void M(C c)
    {
    }
}";
            var compilation = CreateEmptyCompilation(source, WinRtRefs, TestOptions.DebugDll);
            WithRuntimeInstance(compilation, new[] { MscorlibRef }.Concat(ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI", "Windows.UI.Xaml")), runtime =>
            {
                string errorMessage;
                var testData = new CompilationTestData();
                ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                    runtime.Modules.SelectAsArray(m => m.MetadataBlock),
                    "c.Dispatcher",
                    ImmutableArray<Alias>.Empty,
                    (metadataBlocks, _) =>
                    {
                        return CreateMethodContext(runtime, "C.M");
                    },
                    (AssemblyIdentity assembly, out uint size) =>
                    {
                        // Compilation should succeed without retry if we redirect assembly refs correctly.
                        // Throwing so that we don't loop forever (as we did before fix)...
                        throw ExceptionUtilities.Unreachable;
                    },
                    out errorMessage,
                    out testData);
                Assert.Null(errorMessage);
                testData.GetMethodData("<>x.<>m0").VerifyIL(
    @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""Windows.UI.Core.CoreDispatcher Windows.UI.Xaml.DependencyObject.Dispatcher.get""
  IL_0006:  ret
}");
            });
        }

        private static byte[] ToVersion1_3(byte[] bytes)
        {
            return ExpressionCompilerTestHelpers.ToVersion1_3(bytes);
        }

        private static byte[] ToVersion1_4(byte[] bytes)
        {
            return ExpressionCompilerTestHelpers.ToVersion1_4(bytes);
        }
    }
}
