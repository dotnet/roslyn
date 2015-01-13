#pragma warning disable CS0618 // MetadataFileReference to be removed

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MetadataFileReferenceCompilationTests : CSharpTestBase
    {
        [Fact]
        public void AssemblyFileReferenceNotFound()
        {
            CSharpCompilation comp = CSharpCompilation.Create(
                assemblyName: "Compilation",
                options: TestOptions.ReleaseDll,
                references: new[] { new MetadataFileReference(@"c:\file_that_does_not_exist.bbb") });

            comp.VerifyDiagnostics(
                // error CS0006: Metadata file 'c:\file_that_does_not_exist.bbb' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile).WithArguments(@"c:\file_that_does_not_exist.bbb"));
        }

        [Fact]
        public void ModuleFileReferenceNotFound()
        {
            CSharpCompilation comp = CSharpCompilation.Create(
                assemblyName: "Compilation",
                options: TestOptions.ReleaseDll,
                references: new[] { new MetadataFileReference(@"c:\file_that_does_not_exist.bbb", MetadataImageKind.Module) });

            comp.VerifyDiagnostics(
                // error CS0006: Metadata file 'c:\file_that_does_not_exist.bbb' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile).WithArguments(@"c:\file_that_does_not_exist.bbb"));
        }

        [Fact, WorkItem(545062, "DevDiv")]
        public void ExternAliasToSameDll()
        {
            var systemDllPath = typeof(Uri).Assembly.Location;
            var alias1 = new MetadataFileReference(systemDllPath, aliases: ImmutableArray.Create("Alias1"));
            var alias2 = new MetadataFileReference(systemDllPath, aliases: ImmutableArray.Create("Alias2"));

            var text = @"
extern alias Alias1;
extern alias Alias2;

class A { }
";
            var comp = CreateCompilationWithMscorlib(text, references: new MetadataReference[] { alias1, alias2 });
            Assert.Equal(3, comp.References.Count());
            Assert.Equal("Alias2", comp.References.Last().Properties.Aliases.Single());
            comp.VerifyDiagnostics(
                // (2,1): info CS8020: Unused extern alias.
                // extern alias Alias1;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Alias1;"),
                // (3,1): info CS8020: Unused extern alias.
                // extern alias Alias2;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Alias2;"));
        }

        [Fact]
        public void CS1009FTL_MetadataCantOpenFileModule()
        {
            //CSC /TARGET:library /addmodule:class1.netmodule text.CS
            var text = @"class Test
{
    public static int Main()
    {
        return 1;
    }
}";
            var refFile = Temp.CreateFile();
            var reference = new MetadataFileReference(refFile.Path, MetadataImageKind.Module);

            CreateCompilationWithMscorlib(text, new[] { reference }).VerifyDiagnostics(
                 // error CS0009: Metadata file '...' could not be opened -- Image is too small.
                 Diagnostic(ErrorCode.FTL_MetadataCantOpenFile).WithArguments(refFile.Path, "Image is too small."));
        }

        [Fact]
        public void CS1009FTL_MetadataCantOpenFileAssembly()
        {
            //CSC /TARGET:library /reference:class1.netmodule text.CS
            var text = @"class Test
{
    public static int Main()
    {
        return 1;
    }
}";

            var refFile = Temp.CreateFile();
            var reference = new MetadataFileReference(refFile.Path);

            CreateCompilationWithMscorlib(text, new[] { reference }).VerifyDiagnostics(
                // error CS0009: Metadata file '...' could not be opened -- Image is too small.
                Diagnostic(ErrorCode.FTL_MetadataCantOpenFile).WithArguments(refFile.Path, "Image is too small."));
        }

        /// <summary>
        /// Compilation A depends on C Version 1.0.0.0, C Version 2.0.0.0, and B. B depends on C Version 2.0.0.0.
        /// Checks that the ReferenceManager compares the identities correctly and doesn't throw "Two metadata references found with the same identity" exception.
        /// </summary>
        [Fact]
        public void ReferencesVersioning()
        {
            using (MetadataCache.LockAndClean())
            {
                var dir1 = Temp.CreateDirectory();
                var dir2 = Temp.CreateDirectory();
                var dir3 = Temp.CreateDirectory();
                var file1 = dir1.CreateFile("C.dll").WriteAllBytes(TestResources.SymbolsTests.General.C1);
                var file2 = dir2.CreateFile("C.dll").WriteAllBytes(TestResources.SymbolsTests.General.C2);
                var file3 = dir3.CreateFile("main.dll");

                var b = CreateCompilationWithMscorlib(
                    @"public class B { public static int Main() { return C.Main(); } }",
                    assemblyName: "b",
                    references: new[] { MetadataReference.CreateFromImage(TestResources.SymbolsTests.General.C2) },
                    options: TestOptions.ReleaseDll);

                using (MemoryStream output = new MemoryStream())
                {
                    var emitResult = b.Emit(output);
                    Assert.True(emitResult.Success);
                    file3.WriteAllBytes(output.ToArray());
                }

                var a = CreateCompilationWithMscorlib(
                    @"class A { public static void Main() { B.Main(); } }",
                    assemblyName: "a",
                    references: new[] { new MetadataFileReference(file1.Path), new MetadataFileReference(file2.Path), new MetadataFileReference(file3.Path) },
                    options: TestOptions.ReleaseDll);

                using (var stream = new MemoryStream())
                {
                    a.Emit(stream);
                }
            }
        }
    }
}