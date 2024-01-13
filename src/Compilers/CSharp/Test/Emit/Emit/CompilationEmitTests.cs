// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public partial class CompilationEmitTests : EmitMetadataTestBase
    {
        [Fact]
        public void CompilationEmitDiagnostics()
        {
            // Check that Compilation.Emit actually produces compilation errors.

            string source = @"
class X
{
    public void Main()
    {
        const int x = 5;
        x = x; // error; assigning to const.
    }
}";
            var compilation = CreateCompilation(source);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = compilation.Emit(output, pdbStream: null, xmlDocumentationStream: null, win32Resources: null);
            }

            emitResult.Diagnostics.Verify(
                // (7,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x"));
        }

        [Fact]
        public void CompilationEmitWithQuotedMainType()
        {
            // Check that compilation with quoted main switch argument produce diagnostic.
            // MSBuild can return quoted main argument value which is removed from the command line arguments or by parsing
            // command line arguments, but we DO NOT unquote arguments which are provided by 
            // the WithMainTypeName function - (was originally exposed through using 
            // a Cyrillic Namespace And building Using MSBuild.)

            string source = @"
namespace abc
{
public class X
{
    public static void Main()
    {
  
    }
}
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("abc.X"));
            compilation.VerifyDiagnostics();

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("\"abc.X\""));
            compilation.VerifyDiagnostics(// error CS1555: Could not find '"abc.X"' specified for Main method
                                          Diagnostic(ErrorCode.ERR_MainClassNotFound).WithArguments("\"abc.X\""));

            // Verify use of Cyrillic namespace results in same behavior
            source = @"
namespace решения
{
public class X
{
    public static void Main()
    {
  
    }
}
}";
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("решения.X"));
            compilation.VerifyDiagnostics();

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("\"решения.X\""));
            compilation.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_MainClassNotFound).WithArguments("\"решения.X\""));
        }

        [Fact]
        public void CompilationGetDiagnostics()
        {
            // Check that Compilation.GetDiagnostics and Compilation.GetDeclarationDiagnostics work as expected.

            string source = @"
class X
{
    private Blah q;
    public void Main()
    {
        const int x = 5;
        x = x; // error; assigning to const.
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (4,13): error CS0246: The type or namespace name 'Blah' could not be found (are you missing a using directive or an assembly reference?)
                //     private Blah q;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Blah").WithArguments("Blah"),
                // (8,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x = x; // error; assigning to const.
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x"),
                // (4,18): warning CS0169: The field 'X.q' is never used
                //     private Blah q;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "q").WithArguments("X.q"));
        }

        // Check that Emit produces syntax, declaration, and method body errors.
        [Fact]
        public void EmitDiagnostics()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace N {
     class X {
        public Blah field;
        private static readonly int ro;
        public static void Main()
        {
            ro = 4;
        }
    }
}

namespace N.;
");

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = comp.Emit(output, pdbStream: null, xmlDocumentationStream: null, win32Resources: null);
            }

            Assert.False(emitResult.Success);

            emitResult.Diagnostics.Verify(
                    // (13,13): error CS1001: Identifier expected
                    // namespace N.;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(13, 13),
                    // (13,11): error CS8942: File-scoped namespace must precede all other members in a file.
                    // namespace N.;
                    Diagnostic(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, "N.").WithLocation(13, 11),
                    // (4,16): error CS0246: The type or namespace name 'Blah' could not be found (are you missing a using directive or an assembly reference?)
                    //         public Blah field;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Blah").WithArguments("Blah").WithLocation(4, 16),
                    // (8,13): error CS0198: A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
                    //             ro = 4;
                    Diagnostic(ErrorCode.ERR_AssgReadonlyStatic, "ro").WithLocation(8, 13),
                    // (4,21): warning CS0649: Field 'X.field' is never assigned to, and will always have its default value null
                    //         public Blah field;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("N.X.field", "null").WithLocation(4, 21));
        }

        [Fact]
        public void EmitMetadataOnly()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
                                   
        public int x;
        private int y;
         
        public Test1()
        {
            x = 17;
        }

        public string goo(int a)
        {
            return a.ToString();
        }
    }  
}     
");

            EmitResult emitResult;
            byte[] mdOnlyImage;

            using (var output = new MemoryStream())
            {
                emitResult = comp.Emit(output, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = output.ToArray();
            }

            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();
            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");

            var srcUsing = @"
using System;
using Goo.Bar;

class Test2
{
    public static void Main()
    {
        Test1.SayHello();
        Console.WriteLine(new Test1().x);
    }
}  
";
            CSharpCompilation compUsing = CreateCompilation(srcUsing, new[] { MetadataReference.CreateFromImage(mdOnlyImage.AsImmutableOrNull()) });

            using (var output = new MemoryStream())
            {
                emitResult = compUsing.Emit(output);

                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();
                Assert.True(output.ToArray().Length > 0, "no metadata emitted");
            }
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_NoDocMode_Success()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary>This should be emitted</summary>
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();
            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_NoDocMode_SyntaxWarning()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary>This should still emit
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
                @"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_DiagnoseDocMode_SyntaxWarning()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary>This should still emit
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            // This should not fail the emit (as it's a warning).
            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify(
                // (5,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
                //     public class Test1
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary").WithLocation(5, 1),
                // (7,28): warning CS1591: Missing XML comment for publicly visible type or member 'Test1.SayHello()'
                //         public static void SayHello()
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "SayHello").WithArguments("Goo.Bar.Test1.SayHello()").WithLocation(7, 28));

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:Goo.Bar.Test1"" -->
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_DiagnoseDocMode_SemanticWarning()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary><see cref=""T""/></summary>
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            // This should not fail the emit (as it's a warning).
            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify(
                // (4,29): warning CS1574: XML comment has cref attribute 'T' that could not be resolved
                //     /// <summary><see cref="T"/></summary>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "T").WithArguments("T").WithLocation(4, 29),
                // (7,28): warning CS1591: Missing XML comment for publicly visible type or member 'Test1.SayHello()'
                //         public static void SayHello()
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "SayHello").WithArguments("Goo.Bar.Test1.SayHello()").WithLocation(7, 28));

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
        <member name=""T:Goo.Bar.Test1"">
            <summary><see cref=""!:T""/></summary>
        </member>
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_DiagnoseDocMode_Success()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary>This should emit</summary>
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            // This should not fail the emit (as it's a warning).
            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify(
                // (7,28): warning CS1591: Missing XML comment for publicly visible type or member 'Test1.SayHello()'
                //         public static void SayHello()
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "SayHello").WithArguments("Goo.Bar.Test1.SayHello()").WithLocation(7, 28));

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
        <member name=""T:Goo.Bar.Test1"">
            <summary>This should emit</summary>
        </member>
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitMetadataOnly_XmlDocs_ParseDocMode_Success()
        {
            CSharpCompilation comp = CreateCompilation(@"
namespace Goo.Bar
{
    /// <summary>This should emit</summary>
    public class Test1
    {
        public static void SayHello()
        {
            Console.WriteLine(""hello"");
        }
    }  
}     
", assemblyName: "test", parseOptions: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse));

            EmitResult emitResult;
            byte[] mdOnlyImage;
            byte[] xmlDocBytes;

            using (var peStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                emitResult = comp.Emit(peStream, xmlDocumentationStream: xmlStream, options: new EmitOptions(metadataOnly: true));
                mdOnlyImage = peStream.ToArray();
                xmlDocBytes = xmlStream.ToArray();
            }

            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();

            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted");
            Assert.Equal(
                @"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>test</name>
    </assembly>
    <members>
        <member name=""T:Goo.Bar.Test1"">
            <summary>This should emit</summary>
        </member>
    </members>
</doc>
",
                Encoding.UTF8.GetString(xmlDocBytes));
        }

        [Fact]
        public void EmitRefAssembly_PrivateMain()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    internal static void Main()
    {
        System.Console.WriteLine(""hello"");
    }
}
", options: TestOptions.DebugExe);

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                // Previously, this would crash when trying to get the entry point for the ref assembly
                // (but the Main method is not emitted in the ref assembly...)
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();

                verifyEntryPoint(output, expectZero: false);
                VerifyMethods(output, "C", new[] { "void C.Main()", "C..ctor()" });
                VerifyMvid(output, hasMvidSection: false);

                verifyEntryPoint(metadataOutput, expectZero: true);
                VerifyMethods(metadataOutput, "C", new[] { "C..ctor()" });
                VerifyMvid(metadataOutput, hasMvidSection: true);
            }

            void verifyEntryPoint(MemoryStream stream, bool expectZero)
            {
                stream.Position = 0;
                int entryPoint = new PEHeaders(stream).CorHeader.EntryPointTokenOrRelativeVirtualAddress;
                Assert.Equal(expectZero, entryPoint == 0);
            }
        }

        private class TestResourceSectionBuilder : ResourceSectionBuilder
        {
            public TestResourceSectionBuilder()
            {
            }

            protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                builder.WriteInt32(0x12345678);
                builder.WriteInt32(location.PointerToRawData);
                builder.WriteInt32(location.RelativeVirtualAddress);
            }
        }

        private class TestPEBuilder : ManagedPEBuilder
        {
            public static readonly Guid s_mvid = Guid.Parse("a78fa2c3-854e-42bf-8b8d-75a450a6dc18");

            public TestPEBuilder(PEHeaderBuilder header,
                MetadataRootBuilder metadataRootBuilder,
                BlobBuilder ilStream,
                ResourceSectionBuilder nativeResources)
                : base(header, metadataRootBuilder, ilStream, nativeResources: nativeResources)
            {
            }

            protected override ImmutableArray<Section> CreateSections()
            {
                return base.CreateSections().Add(
                     new Section(".mvid", SectionCharacteristics.MemRead |
                        SectionCharacteristics.ContainsInitializedData |
                        SectionCharacteristics.MemDiscardable));
            }

            protected override BlobBuilder SerializeSection(string name, SectionLocation location)
            {
                if (name.Equals(".mvid", StringComparison.Ordinal))
                {
                    var sectionBuilder = new BlobBuilder();
                    sectionBuilder.WriteGuid(s_mvid);
                    return sectionBuilder;
                }

                return base.SerializeSection(name, location);
            }
        }

        [Fact]
        public void MvidSectionNotFirst()
        {
            var ilBuilder = new BlobBuilder();
            var metadataBuilder = new MetadataBuilder();

            var peBuilder = new TestPEBuilder(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                nativeResources: new TestResourceSectionBuilder());

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);

            var peStream = new MemoryStream();
            peBlob.WriteContentTo(peStream);

            peStream.Position = 0;
            using (var peReader = new PEReader(peStream))
            {
                AssertEx.Equal(new[] { ".text", ".rsrc", ".reloc", ".mvid" },
                    peReader.PEHeaders.SectionHeaders.Select(h => h.Name));

                peStream.Position = 0;
                var mvid = BuildTasks.MvidReader.ReadAssemblyMvidOrEmpty(peStream);
                Assert.Equal(TestPEBuilder.s_mvid, mvid);
            }
        }

        /// <summary>
        /// Extract the MVID using two different methods (PEReader and MvidReader) and compare them. 
        /// We only expect an .mvid section in ref assemblies.
        /// </summary>
        private void VerifyMvid(MemoryStream stream, bool hasMvidSection)
        {
            stream.Position = 0;
            using (var reader = new PEReader(stream))
            {
                var metadataReader = reader.GetMetadataReader();
                Guid mvidFromModuleDefinition = metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);

                stream.Position = 0;
                var mvidFromMvidReader = BuildTasks.MvidReader.ReadAssemblyMvidOrEmpty(stream);

                Assert.NotEqual(Guid.Empty, mvidFromModuleDefinition);
                if (hasMvidSection)
                {
                    Assert.Equal(mvidFromModuleDefinition, mvidFromMvidReader);
                }
                else
                {
                    Assert.Equal(Guid.Empty, mvidFromMvidReader);
                }
            }
        }

        [Fact]
        public void EmitRefAssembly_PrivatePropertySetter()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public int PrivateSetter { get; private set; }
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();

                VerifyMethods(output, "C", new[] { "System.Int32 C.<PrivateSetter>k__BackingField", "System.Int32 C.PrivateSetter.get", "void C.PrivateSetter.set",
                    "C..ctor()", "System.Int32 C.PrivateSetter { get; private set; }" });
                VerifyMethods(metadataOutput, "C", new[] { "System.Int32 C.PrivateSetter.get", "C..ctor()", "System.Int32 C.PrivateSetter { get; }" });
                VerifyMvid(output, hasMvidSection: false);
                VerifyMvid(metadataOutput, hasMvidSection: true);
            }
        }

        [Fact]
        public void EmitRefAssembly_PrivatePropertyGetter()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public int PrivateGetter { private get; set; }
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();

                VerifyMethods(output, "C", new[] { "System.Int32 C.<PrivateGetter>k__BackingField", "System.Int32 C.PrivateGetter.get", "void C.PrivateGetter.set",
                    "C..ctor()", "System.Int32 C.PrivateGetter { private get; set; }" });
                VerifyMethods(metadataOutput, "C", new[] { "void C.PrivateGetter.set", "C..ctor()", "System.Int32 C.PrivateGetter { set; }" });
            }
        }

        [Fact]
        public void EmitRefAssembly_PrivateIndexerGetter()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public int this[int i] { private get { return 0; } set { } }
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();

                VerifyMethods(output, "C", new[] { "System.Int32 C.this[System.Int32 i].get", "void C.this[System.Int32 i].set",
                    "C..ctor()", "System.Int32 C.this[System.Int32 i] { private get; set; }" });
                VerifyMethods(metadataOutput, "C", new[] { "void C.this[System.Int32 i].set", "C..ctor()",
                    "System.Int32 C.this[System.Int32 i] { set; }" });
            }
        }

        [Fact]
        public void EmitRefAssembly_SealedPropertyWithInternalInheritedGetter()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class Base
{
    public virtual int Property { internal get { return 0; } set { } }
}
public class C : Base
{
    public sealed override int Property { set { } }
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                emitResult.Diagnostics.Verify();
                Assert.True(emitResult.Success);

                VerifyMethods(output, "C", new[] { "void C.Property.set", "C..ctor()", "System.Int32 C.Property.get", "System.Int32 C.Property { internal get; set; }" });
                // A getter is synthesized on C.Property so that it can be marked as sealed. It is emitted despite being internal because it is virtual.
                VerifyMethods(metadataOutput, "C", new[] { "void C.Property.set", "C..ctor()", "System.Int32 C.Property.get", "System.Int32 C.Property { internal get; set; }" });
            }
        }

        [Fact]
        public void EmitRefAssembly_PrivateAccessorOnEvent()
        {
            CSharpCompilation comp = CreateCompilation(@"
public class C
{
    public event System.Action PrivateAdder { private add { } remove { } }
    public event System.Action PrivateRemover { add { } private remove { } }
}
");
            comp.VerifyDiagnostics(
                // (4,47): error CS1609: Modifiers cannot be placed on event accessor declarations
                //     public event System.Action PrivateAdder { private add { } remove { } }
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private").WithLocation(4, 47),
                // (5,57): error CS1609: Modifiers cannot be placed on event accessor declarations
                //     public event System.Action PrivateRemover { add { } private remove { } }
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private").WithLocation(5, 57)
                );
        }

        [Fact]
        [WorkItem(38444, "https://github.com/dotnet/roslyn/issues/38444")]
        public void EmitRefAssembly_InternalAttributeConstructor()
        {
            CSharpCompilation comp = CreateCompilation(@"
using System;
internal class SomeAttribute : Attribute
{
    internal SomeAttribute()
    {
    }
}
[Some]
public class C
{
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                emitResult.Diagnostics.Verify();
                Assert.True(emitResult.Success);

                VerifyMethods(output, "C", new[] { "C..ctor()" });
                VerifyMethods(metadataOutput, "C", new[] { "C..ctor()" });
                VerifyMethods(output, "SomeAttribute", new[] { "SomeAttribute..ctor()" });
                VerifyMethods(metadataOutput, "SomeAttribute", new[] { "SomeAttribute..ctor()" });
            }
        }

        [Fact]
        [WorkItem(38444, "https://github.com/dotnet/roslyn/issues/38444")]
        public void EmitRefAssembly_InternalAttributeConstructor_DoesntIncludeMethodsOrStaticConstructors()
        {
            CSharpCompilation comp = CreateCompilation(@"
using System;
internal class SomeAttribute : Attribute
{
    internal SomeAttribute()
    {
    }

    static SomeAttribute()
    {
    }

    internal void F()
    {
    }
}
[Some]
public class C
{
}
");

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                EmitResult emitResult = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: new EmitOptions(includePrivateMembers: false));
                emitResult.Diagnostics.Verify();
                Assert.True(emitResult.Success);

                VerifyMethods(output, "C", new[] { "C..ctor()" });
                VerifyMethods(metadataOutput, "C", new[] { "C..ctor()" });
                VerifyMethods(output, "SomeAttribute", new[] { "SomeAttribute..ctor()", "SomeAttribute..cctor()", "void SomeAttribute.F()" });
                VerifyMethods(metadataOutput, "SomeAttribute", new[] { "SomeAttribute..ctor()" });
            }
        }

        private static void VerifyMethods(MemoryStream stream, string containingType, string[] expectedMethods)
        {
            stream.Position = 0;
            var metadataRef = AssemblyMetadata.CreateFromImage(stream.ToArray()).GetReference();

            var compWithMetadata = CreateEmptyCompilation("", references: new[] { MscorlibRef, metadataRef },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            AssertEx.Equal(
                expectedMethods,
                compWithMetadata.GetMember<NamedTypeSymbol>(containingType).GetMembers().Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void RefAssembly_HasReferenceAssemblyAttribute()
        {
            var emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);

            Action<PEAssembly> assemblyValidator = assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var attributes = reader.GetAssemblyDefinition().GetCustomAttributes();
                AssertEx.Equal(new[] {
                        "MemberReference:Void System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor(Int32)",
                        "MemberReference:Void System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor()",
                        "MemberReference:Void System.Diagnostics.DebuggableAttribute..ctor(DebuggingModes)",
                        "MemberReference:Void System.Runtime.CompilerServices.ReferenceAssemblyAttribute..ctor()"
                    },
                    attributes.Select(a => MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)));
            };

            CompileAndVerify("", emitOptions: emitRefAssembly, assemblyValidator: assemblyValidator);
        }

        [Fact]
        public void RefAssembly_HandlesMissingReferenceAssemblyAttribute()
        {
            var emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);

            Action<PEAssembly> assemblyValidator = assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var attributes = reader.GetAssemblyDefinition().GetCustomAttributes();
                AssertEx.SetEqual(attributes.Select(a => MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)),
                    new string[] {
                        "MemberReference:Void System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor(Int32)",
                        "MemberReference:Void System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor()",
                        "MemberReference:Void System.Diagnostics.DebuggableAttribute..ctor(DebuggingModes)"
                    });
            };

            var comp = CreateCompilation("");
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_ReferenceAssemblyAttribute__ctor);
            CompileAndVerifyCommon(compilation: comp, emitOptions: emitRefAssembly, assemblyValidator: assemblyValidator);
        }

        [Fact]
        public void RefAssembly_ReferenceAssemblyAttributeAlsoInSource()
        {
            var emitRefAssembly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);

            Action<PEAssembly> assemblyValidator = assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var attributes = reader.GetAssemblyDefinition().GetCustomAttributes();
                AssertEx.Equal(new string[] {
                        "MemberReference:Void System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor(Int32)",
                        "MemberReference:Void System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor()",
                        "MemberReference:Void System.Diagnostics.DebuggableAttribute..ctor(DebuggingModes)",
                        "MemberReference:Void System.Runtime.CompilerServices.ReferenceAssemblyAttribute..ctor()"
                    },
                    attributes.Select(a => MetadataReaderUtils.Dump(reader, reader.GetCustomAttribute(a).Constructor)));
            };
            string source = @"[assembly:System.Runtime.CompilerServices.ReferenceAssembly()]";
            CompileAndVerify(source, emitOptions: emitRefAssembly, assemblyValidator: assemblyValidator);
        }

        [Theory]
        [InlineData("public int M() { return 1; }", "public int M() { return 2; }", Match.BothMetadataAndRefOut)]
        [InlineData("public int M() { return 1; }", "public int M() { error(); }", Match.BothMetadataAndRefOut)]
        [InlineData("private void M() { }", "", Match.RefOut)]
        [InlineData("internal void M() { }", "", Match.RefOut)]
        [InlineData("private protected void M() { }", "", Match.RefOut)]
        [InlineData("private void M() { dynamic x = 1; }", "", Match.RefOut)] // no reference added from method bodies
        [InlineData(@"private void M() { var x = new { id = 1 }; }", "", Match.RefOut)]
        [InlineData("private int P { get { Error(); } set { Error(); } }", "", Match.RefOut)] // errors in methods bodies don't matter
        [InlineData("public int P { get; set; }", "", Match.Different)]
        [InlineData("protected int P { get; set; }", "", Match.Different)]
        [InlineData("private int P { get; set; }", "", Match.RefOut)] // private auto-property and underlying field are removed
        [InlineData("internal int P { get; set; }", "", Match.RefOut)]
        [InlineData("private event Action E { add { Error(); } remove { Error(); } }", "", Match.RefOut)]
        [InlineData("internal event Action E { add { Error(); } remove { Error(); } }", "", Match.RefOut)]
        [InlineData("private class C2 { }", "", Match.Different)] // all types are included
        [InlineData("private struct S { }", "", Match.Different)]
        [InlineData("public struct S { private int i; }", "public struct S { }", Match.Different)]
        [InlineData("private int i;", "", Match.RefOut)]
        [InlineData("public C() { }", "", Match.BothMetadataAndRefOut)]
        public void RefAssembly_InvariantToSomeChanges(string left, string right, Match expectedMatch)
        {
            string sourceTemplate = @"
using System;
public class C
{
    CHANGE
}
";

            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers: true);
            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers: false);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void RefAssembly_NoPia()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S { public int field; }

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest1
{
    S M();
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public class D : ITest1
{
    public S M()
    {
        throw null;
    }
}
";
            var piaImageReference = pia.EmitToImageReference(embedInteropTypes: true);
            verifyRefOnly(piaImageReference);
            verifyRefOut(piaImageReference);

            var piaMetadataReference = pia.ToMetadataReference(embedInteropTypes: true);
            verifyRefOnly(piaMetadataReference);
            verifyRefOut(piaMetadataReference);

            void verifyRefOnly(MetadataReference reference)
            {
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll,
                                references: new MetadataReference[] { reference });
                var refOnlyImage = EmitRefOnly(comp);
                verifyNoPia(refOnlyImage);
            }

            void verifyRefOut(MetadataReference reference)
            {
                var comp = CreateCompilation(source, options: TestOptions.DebugDll,
                                references: new MetadataReference[] { reference });
                var (image, refImage) = EmitRefOut(comp);
                verifyNoPia(image);
                verifyNoPia(refImage);
            }

            void verifyNoPia(ImmutableArray<byte> image)
            {
                var reference = CompilationVerifier.LoadTestEmittedExecutableForSymbolValidation(image, OutputKind.DynamicallyLinkedLibrary);
                var comp = CreateCompilation("", references: new[] { reference });
                var referencedAssembly = comp.GetReferencedAssemblySymbol(reference);
                var module = (PEModuleSymbol)referencedAssembly.Modules[0];

                var itest1 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("ITest1");
                Assert.NotNull(itest1.GetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute"));

                var method = (PEMethodSymbol)itest1.GetMember("M");
                Assert.Equal("S ITest1.M()", method.ToTestDisplayString());

                var s = (NamedTypeSymbol)method.ReturnType;
                Assert.Equal("S", s.ToTestDisplayString());
                Assert.NotNull(s.GetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute"));

                var field = s.GetMember("field");
                Assert.Equal("System.Int32 S.field", field.ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        public void RefAssembly_NoPia_ReferenceFromMethodBody()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S { public int field; }

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest1
{
    S M();
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public class D
{
    public void M2()
    {
        ITest1 x = null;
        S s = x.M();
    }
}
";
            var piaImageReference = pia.EmitToImageReference(embedInteropTypes: true);
            verifyRefOnly(piaImageReference);
            verifyRefOut(piaImageReference);

            var piaMetadataReference = pia.ToMetadataReference(embedInteropTypes: true);
            verifyRefOnly(piaMetadataReference);
            verifyRefOut(piaMetadataReference);

            void verifyRefOnly(MetadataReference reference)
            {
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll,
                                references: new MetadataReference[] { reference });
                var refOnlyImage = EmitRefOnly(comp);
                verifyNoPia(refOnlyImage, expectMissing: true);
            }

            void verifyRefOut(MetadataReference reference)
            {
                var comp = CreateCompilation(source, options: TestOptions.ReleaseDll,
                                references: new MetadataReference[] { reference });
                var (image, refImage) = EmitRefOut(comp);
                verifyNoPia(image, expectMissing: false);
                verifyNoPia(refImage, expectMissing: false);
            }

            // The ref assembly produced by refout has more types than that produced by refonly,
            // because refout will bind the method bodies (and therefore populate more referenced types).
            // This will be refined in the future. Follow-up issue: https://github.com/dotnet/roslyn/issues/19403
            void verifyNoPia(ImmutableArray<byte> image, bool expectMissing)
            {
                var reference = CompilationVerifier.LoadTestEmittedExecutableForSymbolValidation(image, OutputKind.DynamicallyLinkedLibrary);
                var comp = CreateCompilation("", references: new[] { reference });
                var referencedAssembly = comp.GetReferencedAssemblySymbol(reference);
                var module = (PEModuleSymbol)referencedAssembly.Modules[0];

                var itest1 = module.GlobalNamespace.GetMember<NamedTypeSymbol>("ITest1");
                if (expectMissing)
                {
                    Assert.Null(itest1);
                    Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("S"));
                    return;
                }

                Assert.NotNull(itest1.GetAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute"));

                var method = (PEMethodSymbol)itest1.GetMember("M");
                Assert.Equal("S ITest1.M()", method.ToTestDisplayString());

                var s = (NamedTypeSymbol)method.ReturnType;
                Assert.Equal("S", s.ToTestDisplayString());

                var field = s.GetMember("field");
                Assert.Equal("System.Int32 S.field", field.ToTestDisplayString());
            }
        }

        [Theory]
        [InlineData("internal void M() { }", "", Match.Different)]
        [InlineData("private protected void M() { }", "", Match.Different)]
        public void RefAssembly_InvariantToSomeChangesWithInternalsVisibleTo(string left, string right, Match expectedMatch)
        {
            string sourceTemplate = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleToAttribute(""Friend"")]
public class C
{
    CHANGE
}
";

            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers: true);
            CompareAssemblies(sourceTemplate, left, right, expectedMatch, includePrivateMembers: false);
        }

        public enum Match
        {
            BothMetadataAndRefOut,
            RefOut,
            Different
        }

        /// <summary>
        /// Are the metadata-only assemblies identical with two source code modifications?
        /// Metadata-only assemblies can either include private/internal members or not.
        /// </summary>
        private static void CompareAssemblies(string sourceTemplate, string change1, string change2, Match expectedMatch, bool includePrivateMembers)
        {
            bool expectMatch = includePrivateMembers ?
                expectedMatch == Match.BothMetadataAndRefOut :
                (expectedMatch == Match.BothMetadataAndRefOut || expectedMatch == Match.RefOut);

            string name = GetUniqueName();
            string source1 = sourceTemplate.Replace("CHANGE", change1);
            CSharpCompilation comp1 = CreateCompilation(Parse(source1), options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);
            var image1 = comp1.EmitToStream(EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(includePrivateMembers));

            var source2 = sourceTemplate.Replace("CHANGE", change2);
            Compilation comp2 = CreateCompilation(Parse(source2), options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);
            var image2 = comp2.EmitToStream(EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(includePrivateMembers));

            if (expectMatch)
            {
                AssertEx.Equal(image1.GetBuffer(), image2.GetBuffer(), message: $"Expecting match for includePrivateMembers={includePrivateMembers} case, but differences were found.");
            }
            else
            {
                AssertEx.NotEqual(image1.GetBuffer(), image2.GetBuffer(), message: $"Expecting difference for includePrivateMembers={includePrivateMembers} case, but they matched.");
            }

            var mvid1 = BuildTasks.MvidReader.ReadAssemblyMvidOrEmpty(image1);
            var mvid2 = BuildTasks.MvidReader.ReadAssemblyMvidOrEmpty(image2);

            if (!includePrivateMembers)
            {
                Assert.NotEqual(Guid.Empty, mvid1);
                Assert.Equal(expectMatch, mvid1 == mvid2);
            }
            else
            {
                Assert.Equal(Guid.Empty, mvid1);
                Assert.Equal(Guid.Empty, mvid2);
            }
        }

#if NET472
        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(31197, "https://github.com/dotnet/roslyn/issues/31197")]
        public void RefAssembly_InvariantToResourceChanges()
        {
            var arrayOfEmbeddedData1 = new byte[] { 1, 2, 3, 4, 5 };
            var arrayOfEmbeddedData2 = new byte[] { 1, 2, 3, 4, 5, 6 };

            IEnumerable<ResourceDescription> manifestResources1 = new[] {
                new ResourceDescription(resourceName: "A", fileName: "x.goo", () => new MemoryStream(arrayOfEmbeddedData1), isPublic: true)};
            IEnumerable<ResourceDescription> manifestResources2 = new[] {
                new ResourceDescription(resourceName: "A", fileName: "x.goo", () => new MemoryStream(arrayOfEmbeddedData2), isPublic: true)};
            verify();

            manifestResources1 = new[] {
                new ResourceDescription(resourceName: "A", () => new MemoryStream(arrayOfEmbeddedData1), isPublic: true)}; // embedded
            manifestResources2 = new[] {
                new ResourceDescription(resourceName: "A", () => new MemoryStream(arrayOfEmbeddedData2), isPublic: true)}; // embedded
            verify();

            void verify()
            {
                // Verify refout
                string name = GetUniqueName();
                var (image1, metadataImage1) = emitRefOut(manifestResources1, name);
                var (image2, metadataImage2) = emitRefOut(manifestResources2, name);
                AssertEx.NotEqual(image1, image2, message: "Expecting different main assemblies produced by refout");
                AssertEx.Equal(metadataImage1, metadataImage2, message: "Expecting identical ref assemblies produced by refout");

                var refAssembly1 = Assembly.ReflectionOnlyLoad(metadataImage1.ToArray());
                Assert.DoesNotContain("A", refAssembly1.GetManifestResourceNames());

                // Verify refonly
                string name2 = GetUniqueName();
                var refOnlyMetadataImage1 = emitRefOnly(manifestResources1, name2);
                var refOnlyMetadataImage2 = emitRefOnly(manifestResources2, name2);
                AssertEx.Equal(refOnlyMetadataImage1, refOnlyMetadataImage2, message: "Expecting identical ref assemblies produced by refonly");

                var refOnlyAssembly1 = Assembly.ReflectionOnlyLoad(refOnlyMetadataImage1.ToArray());
                Assert.DoesNotContain("A", refOnlyAssembly1.GetManifestResourceNames());
            }

            (ImmutableArray<byte>, ImmutableArray<byte>) emitRefOut(IEnumerable<ResourceDescription> manifestResources, string name)
            {
                var source = Parse("");
                var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);
                comp.VerifyDiagnostics();

                var metadataPEStream = new MemoryStream();
                var refoutOptions = EmitOptions.Default.WithEmitMetadataOnly(false).WithIncludePrivateMembers(false);
                var peStream = comp.EmitToArray(refoutOptions, metadataPEStream: metadataPEStream, manifestResources: manifestResources);

                return (peStream, metadataPEStream.ToImmutable());
            }

            ImmutableArray<byte> emitRefOnly(IEnumerable<ResourceDescription> manifestResources, string name)
            {
                var source = Parse("");
                var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);
                comp.VerifyDiagnostics();

                var refonlyOptions = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
                return comp.EmitToArray(refonlyOptions, metadataPEStream: null, manifestResources: manifestResources);
            }
        }
#endif
        [Fact, WorkItem(31197, "https://github.com/dotnet/roslyn/issues/31197")]
        public void RefAssembly_CryptoHashFailedIsOnlyReportedOnce()
        {
            var hash_resources = new[] {new ResourceDescription("hash_resource", "snKey.snk",
                () => new MemoryStream(TestResources.General.snKey, writable: false),
                true)};

            CSharpCompilation moduleComp = CreateEmptyCompilation("",
                options: TestOptions.DebugDll.WithDeterministic(true).WithOutputKind(OutputKind.NetModule));

            var reference = ModuleMetadata.CreateFromImage(moduleComp.EmitToArray()).GetReference();

            CSharpCompilation compilation = CreateCompilation(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345)]

class Program
{
    void M() {}
}
", references: new[] { reference }, options: TestOptions.ReleaseDll);

            // refonly
            var refonlyOptions = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            var refonlyDiagnostics = compilation.Emit(new MemoryStream(), pdbStream: null,
                options: refonlyOptions, manifestResources: hash_resources).Diagnostics;

            refonlyDiagnostics.Verify(
                // error CS8013: Cryptographic failure while creating hashes.
                Diagnostic(ErrorCode.ERR_CryptoHashFailed));

            // refout
            var refoutOptions = EmitOptions.Default.WithEmitMetadataOnly(false).WithIncludePrivateMembers(false);
            var refoutDiagnostics = compilation.Emit(peStream: new MemoryStream(), metadataPEStream: new MemoryStream(), pdbStream: null,
                options: refoutOptions, manifestResources: hash_resources).Diagnostics;

            refoutDiagnostics.Verify(
                // error CS8013: Cryptographic failure while creating hashes.
                Diagnostic(ErrorCode.ERR_CryptoHashFailed));
        }

        [Fact]
        public void RefAssemblyClient_RefReadonlyParameters()
        {
            VerifyRefAssemblyClient(@"
public class C
{
    public void RR_input(in int x) => throw null;
    public ref readonly int RR_output() => throw null;
    public ref readonly int P => throw null;
    public ref readonly int this[in int i] => throw null;
    public delegate ref readonly int Delegate(in int i);
}
public static class Extensions
{
    public static void RR_extension(in this int x) => throw null;
    public static void R_extension(ref this int x) => throw null;
}",
@"class D
{
    void M(C c, in int y)
    {
        c.RR_input(y);
        VerifyRR(c.RR_output());
        VerifyRR(c.P);
        VerifyRR(c[y]);
        C.Delegate x = VerifyDelegate;
        y.RR_extension();
        1.RR_extension();
        y.R_extension(); // error 1
        1.R_extension(); // error 2
    }
    void VerifyRR(in int y) => throw null;
    ref readonly int VerifyDelegate(in int y) => throw null;
}",
comp => comp.VerifyDiagnostics(
                // (12,9): error CS8329: Cannot use variable 'y' as a ref or out value because it is a readonly variable
                //         y.R_extension(); // error 1
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "y").WithArguments("variable", "y").WithLocation(12, 9),
                // (13,9): error CS1510: A ref or out value must be an assignable variable
                //         1.R_extension(); // error 2
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "1").WithLocation(13, 9)
                ));
        }

        [Fact]
        public void RefAssemblyClient_StructWithPrivateReferenceTypeField()
        {
            VerifyRefAssemblyClient(@"
public struct S
{
    private object _field;
    public static S GetValue() => new S() { _field = new object() };
    public object GetField() => _field;
}",
@"class C
{
    void M()
    {
        unsafe
        {
            System.Console.WriteLine(sizeof(S*));
        }
    }
}",
comp => comp.VerifyDiagnostics(
                // (7,45): warning CS8500: This takes the address of, gets the size of, or declares a pointer to a managed type ('S')
                //             System.Console.WriteLine(sizeof(S*));
                Diagnostic(ErrorCode.WRN_ManagedAddr, "S*").WithArguments("S").WithLocation(7, 45)
                ));
        }

        [Fact]
        public void RefAssemblyClient_ExplicitPropertyImplementation()
        {
            VerifyRefAssemblyClient(@"
public interface I
{
    int P { get; set; }
}
public class Base : I
{
    int I.P { get { throw null; } set { throw null; } }
}",
@"
class Derived : Base, I
{
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitAllNestedTypes()
        {
            VerifyRefAssemblyClient(@"
public interface I1<T> { }
public interface I2 { }
public class A: I1<A.X>
{
    private class X: I2 { }
}",
@"class C
{
    I1<I2> M(A a)
    {
        return (I1<I2>)a;
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitTupleNames()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public (int first, int) field;
}",
@"class C
{
    void M(A a)
    {
        System.Console.Write(a.field.first);
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitDynamic()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public dynamic field;
}",
@"class C
{
    void M(A a)
    {
        System.Console.Write(a.field.DynamicMethod());
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitOut()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public void M(out int x) { x = 1; }
}",
@"class C
{
    void M(A a)
    {
        a.M(out int x);
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitVariance_OutError()
        {
            VerifyRefAssemblyClient(@"
public interface I<out T>
{
}",
@"
class Base { }
class Derived : Base
{
    I<Derived> M(I<Base> x)
    {
        return x;
    }
}",
comp => comp.VerifyDiagnostics(
                // (7,16): error CS0266: Cannot implicitly convert type 'I<Base>' to 'I<Derived>'. An explicit conversion exists (are you missing a cast?)
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<Base>", "I<Derived>").WithLocation(7, 16)
                ));
        }

        [Fact]
        public void RefAssemblyClient_EmitVariance_OutSuccess()
        {
            VerifyRefAssemblyClient(@"
public interface I<out T>
{
}",
@"
class Base { }
class Derived : Base
{
    I<Base> M(I<Derived> x)
    {
        return x;
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitVariance_InSuccess()
        {
            VerifyRefAssemblyClient(@"
public interface I<in T>
{
}",
@"
class Base { }
class Derived : Base
{
    I<Derived> M(I<Base> x)
    {
        return x;
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitVariance_InError()
        {
            VerifyRefAssemblyClient(@"
public interface I<in T>
{
}",
@"
class Base { }
class Derived : Base
{
    I<Base> M(I<Derived> x)
    {
        return x;
    }
}",
comp => comp.VerifyDiagnostics(
                // (7,16): error CS0266: Cannot implicitly convert type 'I<Derived>' to 'I<Base>'. An explicit conversion exists (are you missing a cast?)
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("I<Derived>", "I<Base>").WithLocation(7, 16)
                ));
        }

        [Fact]
        public void RefAssemblyClient_EmitOptionalArguments()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public void M(int x = 42) { }
}",
@"
class C
{
    void M2(A a)
    {
        a.M();
    }
}",
comp =>
{
    comp.VerifyDiagnostics();
    var verifier = CompileAndVerify(comp);
    verifier.VerifyIL("C.M2", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   42
  IL_0004:  callvirt   ""void A.M(int)""
  IL_0009:  nop
  IL_000a:  ret
}");
});
        }

        [Fact]
        public void RefAssemblyClient_EmitArgumentNames()
        {
            VerifyRefAssemblyClient(@"
public class Base
{
    public virtual void M(int x) { }
}
public class Derived : Base
{
    public override void M(int different) { }
}",
@"
class C
{
    void M2(Derived d)
    {
        d.M(different: 1);
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitEnum()
        {
            VerifyRefAssemblyClient(@"
public enum E
{
    Default,
    Other
}",
@"
class C
{
    void M2(E e)
    {
        System.Console.Write(E.Other);
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitConst()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public const int number = 42;
}",
@"
class C
{
    void M2()
    {
        System.Console.Write(A.number);
    }
}",
comp =>
{
    comp.VerifyDiagnostics();
    var verifier = CompileAndVerify(comp);
    verifier.VerifyIL("C.M2", @"
{
  // Code size       10 (0xa)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.s   42
  IL_0003:  call       ""void System.Console.Write(int)""
  IL_0008:  nop
  IL_0009:  ret
}");
});
        }

        [Fact]
        public void RefAssemblyClient_EmitParams()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public void M(params int[] x) { }
}",
@"
class C
{
    void M2(A a)
    {
        a.M(1, 2, 3);
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitExtension()
        {
            VerifyRefAssemblyClient(@"
public static class A
{
    public static void M(this string x) { }
}",
@"
class C
{
    void M2(string s)
    {
        s.M();
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitAllTypes()
        {
            VerifyRefAssemblyClient(@"
public interface I1<T> { }
public interface I2 { }
public class A: I1<X> { }
internal class X: I2 { }
",
@"class C
{
    I1<I2> M(A a)
    {
        return (I1<I2>)a;
    }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_EmitNestedTypes()
        {
            VerifyRefAssemblyClient(@"
public class A
{
    public class Nested { }
}
",
@"class C
{
    void M(A.Nested a) { }
}",
comp => comp.VerifyDiagnostics());
        }

        [Fact]
        public void RefAssemblyClient_StructWithPrivateGenericField()
        {
            VerifyRefAssemblyClient(@"
public struct Container<T>
{
    private T contained;
    public void SetField(T value) { contained = value; }
    public T GetField() => contained;
}",
@"public struct Usage
{
    public Container<Usage> x;
}",
comp => comp.VerifyDiagnostics(
                // (3,29): error CS0523: Struct member 'Usage.x' of type 'Container<Usage>' causes a cycle in the struct layout
                //     public Container<Usage> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("Usage.x", "Container<Usage>").WithLocation(3, 29)
                ));
        }

        [Fact]
        public void RefAssemblyClient_EmitAllVirtualMethods()
        {

            var comp1 = CreateCSharpCompilation("CS1",
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS2"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""CS3"")]
public abstract class C1
{
    internal abstract void M();
}",
                referencedAssemblies: new[] { MscorlibRef });
            comp1.VerifyDiagnostics();
            var image1 = comp1.EmitToImageReference(EmitOptions.Default);

            var comp2 = CreateCSharpCompilation("CS2",
@"public abstract class C2 : C1
{
    internal override void M() { }
}",
              referencedAssemblies: new[] { MscorlibRef, image1 });
            var image2 = comp2.EmitToImageReference(EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false));

            // If internal virtual methods were not included in ref assemblies, then C3 could not be concrete and would report
            // error CS0534: 'C3' does not implement inherited abstract member 'C1.M()'

            var comp3 = CreateCSharpCompilation("CS3",
@"public class C3 : C2
{
}",
                referencedAssemblies: new[] { MscorlibRef, image1, image2 });
            comp3.VerifyDiagnostics();
        }

        [Fact]
        public void RefAssemblyClient_StructWithPrivateIntField()
        {
            VerifyRefAssemblyClient(@"
public struct S
{
    private int i;
    private void M()
    {
        System.Console.Write(i++);
    }
}",
@"class C
{
    string M()
    {
        S s;
        return s.ToString();
    }
}",
comp => comp.VerifyDiagnostics(
                // (6,16): error CS0165: Use of unassigned local variable 's'
                //         return s.ToString();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(6, 16)
                ));
        }

        /// <summary>
        /// The client compilation should not be affected (except for some diagnostic differences)
        /// by the library assembly only having metadata, or not including private members.
        /// </summary>
        private void VerifyRefAssemblyClient(string lib_cs, string client_cs, Action<CSharpCompilation> validator, int debugFlag = -1)
        {
            // Whether the library is compiled in full, as metadata-only, or as a ref assembly should be transparent
            // to the client and the validator should be able to verify the same expectations.

            if (debugFlag == -1 || debugFlag == 0)
            {
                VerifyRefAssemblyClient(lib_cs, client_cs, validator,
                    EmitOptions.Default.WithEmitMetadataOnly(false));
            }

            if (debugFlag == -1 || debugFlag == 1)
            {
                VerifyRefAssemblyClient(lib_cs, client_cs, validator,
                    EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(true));
            }

            if (debugFlag == -1 || debugFlag == 2)
            {
                VerifyRefAssemblyClient(lib_cs, client_cs, validator,
                    EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false));
            }
        }

        private static void VerifyRefAssemblyClient(string lib_cs, string source, Action<CSharpCompilation> validator, EmitOptions emitOptions)
        {
            string name = GetUniqueName();
            var libComp = CreateCompilation(lib_cs,
                options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);
            libComp.VerifyDiagnostics();
            var libImage = libComp.EmitToImageReference(emitOptions);

            var comp = CreateCompilation(source, references: new[] { libImage },
                options: TestOptions.DebugDll.WithAllowUnsafe(true));
            validator(comp);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersion(""1"")]", false)]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")]", true)]
        public void RefAssembly_EmitAsDeterministic(string source, bool hasWildcard)
        {
            var name = GetUniqueName();
            var options = TestOptions.DebugDll.WithDeterministic(false);
            var comp1 = CreateCompilation(source, options: options, assemblyName: name);

            var (out1, refOut1) = EmitRefOut(comp1);
            var refOnly1 = EmitRefOnly(comp1);
            VerifyIdentitiesMatch(out1, refOut1);
            VerifyIdentitiesMatch(out1, refOnly1);
            AssertEx.Equal(refOut1, refOut1);

            // The resolution of the PE header time date stamp is seconds (divided by two), and we want to make sure that has an opportunity to change
            // between calls to Emit.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            // Re-using the same compilation results in the same time stamp
            var (out15, refOut15) = EmitRefOut(comp1);
            VerifyIdentitiesMatch(out1, out15);
            VerifyIdentitiesMatch(refOut1, refOut15);
            AssertEx.Equal(refOut1, refOut15);

            // Using a new compilation results in new time stamp
            var comp2 = CreateCompilation(source, options: options, assemblyName: name);
            var (out2, refOut2) = EmitRefOut(comp2);
            var refOnly2 = EmitRefOnly(comp2);
            VerifyIdentitiesMatch(out2, refOut2);
            VerifyIdentitiesMatch(out2, refOnly2);

            VerifyIdentitiesMatch(out1, out2, expectMatch: !hasWildcard);
            VerifyIdentitiesMatch(refOut1, refOut2, expectMatch: !hasWildcard);

            if (hasWildcard)
            {
                AssertEx.NotEqual(refOut1, refOut2);
                AssertEx.NotEqual(refOut1, refOnly2);
            }
            else
            {
                // If no wildcards, the binaries are emitted deterministically
                AssertEx.Equal(refOut1, refOut2);
                AssertEx.Equal(refOut1, refOnly2);
            }
        }

        private void VerifySigned(ImmutableArray<byte> image, bool expectSigned = true)
        {
            using (var reader = new PEReader(image))
            {
                var flags = reader.PEHeaders.CorHeader.Flags;
                Assert.Equal(expectSigned, flags.HasFlag(CorFlags.StrongNameSigned));
            }
        }

        private static void VerifyIdentitiesMatch(ImmutableArray<byte> firstImage, ImmutableArray<byte> secondImage,
            bool expectMatch = true, bool expectPublicKey = false)
        {
            var id1 = ModuleMetadata.CreateFromImage(firstImage).GetMetadataReader().ReadAssemblyIdentityOrThrow();
            var id2 = ModuleMetadata.CreateFromImage(secondImage).GetMetadataReader().ReadAssemblyIdentityOrThrow();
            Assert.Equal(expectMatch, id1 == id2);
            if (expectPublicKey)
            {
                Assert.True(id1.HasPublicKey);
                Assert.True(id2.HasPublicKey);
            }
        }

        private static (ImmutableArray<byte> image, ImmutableArray<byte> refImage) EmitRefOut(CSharpCompilation comp)
        {
            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                var options = EmitOptions.Default.WithIncludePrivateMembers(false);
                comp.VerifyEmitDiagnostics();
                var result = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: options);
                return (output.ToImmutable(), metadataOutput.ToImmutable());
            }
        }

        private static ImmutableArray<byte> EmitRefOnly(CSharpCompilation comp)
        {
            using (var output = new MemoryStream())
            {
                var options = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
                comp.VerifyEmitDiagnostics();
                var result = comp.Emit(output,
                    options: options);
                return output.ToImmutable();
            }
        }

        [Fact]
        public void RefAssembly_PublicSigning()
        {
            var snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);

            var comp = CreateCompilation("public class C{}",
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(snk.Path).WithPublicSign(true));

            comp.VerifyDiagnostics();
            var (image, refImage) = EmitRefOut(comp);
            var refOnlyImage = EmitRefOnly(comp);
            VerifySigned(image);
            VerifySigned(refImage);
            VerifySigned(refOnlyImage);
            VerifyIdentitiesMatch(image, refImage, expectPublicKey: true);
            VerifyIdentitiesMatch(image, refOnlyImage, expectPublicKey: true);
        }

        [Fact]
        public void RefAssembly_StrongNameProvider()
        {
            var signedDllOptions = TestOptions.SigningReleaseDll.
                 WithCryptoKeyFile(SigningTestHelpers.KeyPairFile);

            var comp = CreateCompilation("public class C{}", options: signedDllOptions);

            comp.VerifyDiagnostics();
            var (image, refImage) = EmitRefOut(comp);
            var refOnlyImage = EmitRefOnly(comp);
            VerifySigned(image);
            VerifySigned(refImage);
            VerifySigned(refOnlyImage);
            VerifyIdentitiesMatch(image, refImage, expectPublicKey: true);
            VerifyIdentitiesMatch(image, refOnlyImage, expectPublicKey: true);
        }

        [Fact]
        public void RefAssembly_StrongNameProvider_Arm64()
        {
            var signedDllOptions = TestOptions.SigningReleaseDll.
                 WithCryptoKeyFile(SigningTestHelpers.KeyPairFile).
                 WithPlatform(Platform.Arm64).
                 WithDeterministic(true);

            var comp = CreateCompilation("public class C{}", options: signedDllOptions);

            comp.VerifyDiagnostics();
            var (image, refImage) = EmitRefOut(comp);
            var refOnlyImage = EmitRefOnly(comp);
            VerifySigned(image);
            VerifySigned(refImage);
            VerifySigned(refOnlyImage);
            VerifyIdentitiesMatch(image, refImage, expectPublicKey: true);
            VerifyIdentitiesMatch(image, refOnlyImage, expectPublicKey: true);
        }

        [Fact]
        public void RefAssembly_StrongNameProviderAndDelaySign()
        {
            var signedDllOptions = TestOptions.SigningReleaseDll
                .WithCryptoKeyFile(SigningTestHelpers.KeyPairFile)
                .WithDelaySign(true);

            var comp = CreateCompilation("public class C{}", options: signedDllOptions);

            comp.VerifyDiagnostics();
            var (image, refImage) = EmitRefOut(comp);
            var refOnlyImage = EmitRefOnly(comp);
            VerifySigned(image, expectSigned: false);
            VerifySigned(refImage, expectSigned: false);
            VerifySigned(refOnlyImage, expectSigned: false);
            VerifyIdentitiesMatch(image, refImage, expectPublicKey: true);
            VerifyIdentitiesMatch(image, refOnlyImage, expectPublicKey: true);
        }

        [Theory]
        [InlineData("public int M() { error(); }", true)]
        [InlineData("public int M() { error() }", false)] // This may get relaxed. See follow-up issue https://github.com/dotnet/roslyn/issues/17612
        [InlineData("public int M();", true)]
        [InlineData("public int M() { int Local(); }", true)]
        [InlineData("public C();", true)]
        [InlineData("~ C();", true)]
        [InlineData("public Error M() { return null; }", false)] // This may get relaxed. See follow-up issue https://github.com/dotnet/roslyn/issues/17612
        [InlineData("public static explicit operator C(int i);", true)]
        [InlineData("public async Task M();", false)]
        [InlineData("partial void M(); partial void M();", false)] // This may get relaxed. See follow-up issue https://github.com/dotnet/roslyn/issues/17612
        public void RefAssembly_IgnoresSomeDiagnostics(string change, bool expectSuccess)
        {
            string sourceTemplate = @"
using System.Threading.Tasks;
public partial class C
{
    CHANGE
}
";
            verifyIgnoresDiagnostics(EmitOptions.Default.WithEmitMetadataOnly(false).WithTolerateErrors(false), success: false);
            verifyIgnoresDiagnostics(EmitOptions.Default.WithEmitMetadataOnly(true).WithTolerateErrors(false), success: expectSuccess);

            void verifyIgnoresDiagnostics(EmitOptions emitOptions, bool success)
            {
                string source = sourceTemplate.Replace("CHANGE", change);
                string name = GetUniqueName();
                CSharpCompilation comp = CreateCompilation(Parse(source),
                    options: TestOptions.DebugDll.WithDeterministic(true), assemblyName: name);

                using (var output = new MemoryStream())
                {
                    var emitResult = comp.Emit(output, options: emitOptions);
                    Assert.Equal(!success, emitResult.Diagnostics.HasAnyErrors());
                    Assert.Equal(success, emitResult.Success);
                }
            }
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembers()
        {
            string source = @"
public class PublicClass
{
    public void PublicMethod() { System.Console.Write(new { anonymous = 1 }); }
    private void PrivateMethod() { System.Console.Write(""Hello""); }
    protected void ProtectedMethod() { System.Console.Write(""Hello""); }
    internal void InternalMethod() { System.Console.Write(""Hello""); }
    protected internal void ProtectedInternalMethod() { }
    private protected void PrivateProtectedMethod() { }
    public event System.Action PublicEvent;
    internal event System.Action InternalEvent;
}
";
            CSharpCompilation comp = CreateEmptyCompilation(source, references: new[] { MscorlibRef },
                parseOptions: TestOptions.Regular7_2.WithNoRefSafetyRulesAttribute(),
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the regular assembly
            CompileAndVerify(comp, emitOptions: EmitOptions.Default, verify: Verification.Passes);

            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var realImage = comp.EmitToImageReference(EmitOptions.Default);
            var compWithReal = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, realImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "<>f__AnonymousType0<<anonymous>j__TPar>", "PublicClass" },
                compWithReal.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "void PublicClass.PublicMethod()", "void PublicClass.PrivateMethod()",
                    "void PublicClass.ProtectedMethod()", "void PublicClass.InternalMethod()",
                    "void PublicClass.ProtectedInternalMethod()", "void PublicClass.PrivateProtectedMethod()",
                    "void PublicClass.PublicEvent.add", "void PublicClass.PublicEvent.remove",
                    "void PublicClass.InternalEvent.add", "void PublicClass.InternalEvent.remove",
                    "PublicClass..ctor()",
                    "event System.Action PublicClass.PublicEvent", "event System.Action PublicClass.InternalEvent" },
                compWithReal.GetMember<NamedTypeSymbol>("PublicClass").GetMembers()
                    .Select(m => m.ToTestDisplayString()));

            AssertEx.Equal(
                new[] { "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute" },
                compWithReal.SourceModule.GetReferencedAssemblySymbols().Last().GetAttributes().Select(a => a.AttributeClass.ToTestDisplayString()));

            // Verify metadata (types, members, attributes) of the regular assembly with IncludePrivateMembers accidentally set to false.
            // Note this can happen because of binary clients compiled against old EmitOptions ctor which had IncludePrivateMembers=false by default.
            // In this case, IncludePrivateMembers is silently set to true when emitting
            // See https://github.com/dotnet/roslyn/issues/20873
            var emitRegularWithoutPrivateMembers = EmitOptions.Default.WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRegularWithoutPrivateMembers, verify: Verification.Passes);

            var realImage2 = comp.EmitToImageReference(emitRegularWithoutPrivateMembers);
            var compWithReal2 = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, realImage2 },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "<>f__AnonymousType0<<anonymous>j__TPar>", "PublicClass" },
                compWithReal2.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "void PublicClass.PublicMethod()", "void PublicClass.PrivateMethod()",
                    "void PublicClass.ProtectedMethod()", "void PublicClass.InternalMethod()",
                    "void PublicClass.ProtectedInternalMethod()", "void PublicClass.PrivateProtectedMethod()",
                    "void PublicClass.PublicEvent.add", "void PublicClass.PublicEvent.remove",
                    "void PublicClass.InternalEvent.add", "void PublicClass.InternalEvent.remove",
                    "PublicClass..ctor()",
                    "event System.Action PublicClass.PublicEvent", "event System.Action PublicClass.InternalEvent" },
                compWithReal2.GetMember<NamedTypeSymbol>("PublicClass").GetMembers()
                    .Select(m => m.ToTestDisplayString()));

            AssertEx.Equal(
                new[] { "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute" },
                compWithReal2.SourceModule.GetReferencedAssemblySymbols().Last().GetAttributes().Select(a => a.AttributeClass.ToTestDisplayString()));

            // verify metadata (types, members, attributes) of the metadata-only assembly
            var emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(true);
            CompileAndVerify(comp, emitOptions: emitMetadataOnly, verify: Verification.Passes);

            var metadataImage = comp.EmitToImageReference(emitMetadataOnly);
            var compWithMetadata = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, metadataImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "PublicClass" },
                compWithMetadata.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "void PublicClass.PublicMethod()", "void PublicClass.PrivateMethod()",
                    "void PublicClass.ProtectedMethod()", "void PublicClass.InternalMethod()",
                    "void PublicClass.ProtectedInternalMethod()", "void PublicClass.PrivateProtectedMethod()",
                    "void PublicClass.PublicEvent.add", "void PublicClass.PublicEvent.remove",
                    "void PublicClass.InternalEvent.add", "void PublicClass.InternalEvent.remove",
                    "PublicClass..ctor()",
                    "event System.Action PublicClass.PublicEvent", "event System.Action PublicClass.InternalEvent" },
                compWithMetadata.GetMember<NamedTypeSymbol>("PublicClass").GetMembers().Select(m => m.ToTestDisplayString()));

            AssertEx.Equal(
                new[] { "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute" },
                compWithMetadata.SourceModule.GetReferencedAssemblySymbols().Last().GetAttributes().Select(a => a.AttributeClass.ToTestDisplayString()));

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "PublicClass" },
                compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "void PublicClass.PublicMethod()", "void PublicClass.ProtectedMethod()",
                    "void PublicClass.ProtectedInternalMethod()",
                    "void PublicClass.PublicEvent.add", "void PublicClass.PublicEvent.remove",
                    "PublicClass..ctor()", "event System.Action PublicClass.PublicEvent"},
                compWithRef.GetMember<NamedTypeSymbol>("PublicClass").GetMembers().Select(m => m.ToTestDisplayString()));

            AssertEx.Equal(
                new[] {
                    "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                    "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                    "System.Diagnostics.DebuggableAttribute",
                    "System.Runtime.CompilerServices.ReferenceAssemblyAttribute" },
                compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GetAttributes().Select(a => a.AttributeClass.ToTestDisplayString()));

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly));
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembersOnExplicitlyImplementedProperty()
        {
            string source = @"
public interface I
{
    int P { get; set; }
}
public class C : I
{
    int I.P
    {
        get { throw null; }
        set { throw null; }
    }
}
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CSharpCompilation comp = CreateEmptyCompilation(source, parseOptions: parseOptions, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the regular assembly
            CompileAndVerify(comp, emitOptions: EmitOptions.Default, verify: Verification.Passes);

            var realImage = comp.EmitToImageReference(EmitOptions.Default);
            var compWithReal = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, realImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyPropertyWasEmitted(compWithReal);

            // verify metadata (types, members, attributes) of the metadata-only assembly
            var emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(true);
            CompileAndVerify(comp, emitOptions: emitMetadataOnly, verify: Verification.Passes);

            var metadataImage = comp.EmitToImageReference(emitMetadataOnly);
            var compWithMetadata = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, metadataImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyPropertyWasEmitted(compWithMetadata);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyPropertyWasEmitted(compWithRef);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly));

            void verifyPropertyWasEmitted(CSharpCompilation input)
            {
                AssertEx.Equal(
                    new[] { "<Module>", "I", "C" },
                    input.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

                AssertEx.Equal(
                    new[] { "System.Int32 C.I.P.get", "void C.I.P.set", "C..ctor()", "System.Int32 C.I.P { get; set; }" },
                    input.GetMember<NamedTypeSymbol>("C").GetMembers()
                        .Select(m => m.ToTestDisplayString()));
            }
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembersOnExplicitlyImplementedEvent()
        {
            string source = @"
public interface I
{
    event System.Action E;
}
public class C : I
{
    event System.Action I.E
    {
        add { throw null; }
        remove { throw null; }
    }
}
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CSharpCompilation comp = CreateEmptyCompilation(source, parseOptions: parseOptions, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the regular assembly
            CompileAndVerify(comp, emitOptions: EmitOptions.Default, verify: Verification.Passes);

            var realImage = comp.EmitToImageReference(EmitOptions.Default);
            var compWithReal = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, realImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyEventWasEmitted(compWithReal);

            // verify metadata (types, members, attributes) of the metadata-only assembly
            var emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(true);
            CompileAndVerify(comp, emitOptions: emitMetadataOnly, verify: Verification.Passes);

            var metadataImage = comp.EmitToImageReference(emitMetadataOnly);
            var compWithMetadata = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, metadataImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyEventWasEmitted(compWithMetadata);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyEventWasEmitted(compWithRef);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly));

            void verifyEventWasEmitted(CSharpCompilation input)
            {
                AssertEx.Equal(
                    new[] { "<Module>", "I", "C" },
                    input.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

                AssertEx.Equal(
                    new[] { "void C.I.E.add", "void C.I.E.remove", "C..ctor()", "event System.Action C.I.E" },
                    input.GetMember<NamedTypeSymbol>("C").GetMembers()
                        .Select(m => m.ToTestDisplayString()));
            }
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembersOnExplicitlyImplementedIndexer()
        {
            string source = @"
public interface I
{
    int this[int i] { get; set; }
}
public class C : I
{
    int I.this[int i]
    {
        get { throw null; }
        set { throw null; }
    }
}
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CSharpCompilation comp = CreateEmptyCompilation(source, parseOptions: parseOptions, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the regular assembly
            CompileAndVerify(comp, emitOptions: EmitOptions.Default, verify: Verification.Passes);

            var realImage = comp.EmitToImageReference(EmitOptions.Default);
            var compWithReal = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, realImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyIndexerWasEmitted(compWithReal);

            // verify metadata (types, members, attributes) of the metadata-only assembly
            var emitMetadataOnly = EmitOptions.Default.WithEmitMetadataOnly(true);
            CompileAndVerify(comp, emitOptions: emitMetadataOnly, verify: Verification.Passes);

            var metadataImage = comp.EmitToImageReference(emitMetadataOnly);
            var compWithMetadata = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, metadataImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyIndexerWasEmitted(compWithMetadata);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitMetadataOnly));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            verifyIndexerWasEmitted(compWithRef);

            MetadataReaderUtils.AssertEmptyOrThrowNull(comp.EmitToArray(emitRefOnly));

            void verifyIndexerWasEmitted(CSharpCompilation input)
            {
                AssertEx.Equal(
                    new[] { "<Module>", "I", "C" },
                    input.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

                AssertEx.Equal(
                    new[] {"System.Int32 C.I.get_Item(System.Int32 i)", "void C.I.set_Item(System.Int32 i, System.Int32 value)",
                        "C..ctor()", "System.Int32 C.I.Item[System.Int32 i] { get; set; }" },
                    input.GetMember<NamedTypeSymbol>("C").GetMembers()
                        .Select(m => m.ToTestDisplayString()));
            }
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembersOnStruct()
        {
            string source = @"
internal struct InternalStruct
{
    internal int P { get; set; }
}
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CSharpCompilation comp = CreateEmptyCompilation(source, parseOptions: parseOptions, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var globalNamespace = compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace;

            AssertEx.Equal(
                new[] { "<Module>", "InternalStruct", "Microsoft", "System" },
                globalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(new[] { "Microsoft.CodeAnalysis" }, globalNamespace.GetMember<NamespaceSymbol>("Microsoft").GetMembers().Select(m => m.ToDisplayString()));
            AssertEx.Equal(
                new[] { "Microsoft.CodeAnalysis.EmbeddedAttribute" },
                globalNamespace.GetMember<NamespaceSymbol>("Microsoft.CodeAnalysis").GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "System.Runtime.CompilerServices" },
                globalNamespace.GetMember<NamespaceSymbol>("System.Runtime").GetMembers().Select(m => m.ToDisplayString()));
            AssertEx.Equal(
                new[] { "System.Runtime.CompilerServices.IsReadOnlyAttribute" },
                globalNamespace.GetMember<NamespaceSymbol>("System.Runtime.CompilerServices").GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "System.Int32 InternalStruct.<P>k__BackingField", "InternalStruct..ctor()" },
                compWithRef.GetMember<NamedTypeSymbol>("InternalStruct").GetMembers().Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void RefAssembly_VerifyTypesAndMembersOnPrivateStruct()
        {
            string source = @"
struct S
{
    private class PrivateType { }
    private PrivateType field;
}
";
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            CSharpCompilation comp = CreateEmptyCompilation(source, parseOptions: parseOptions, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            // verify metadata (types, members, attributes) of the ref assembly
            var emitRefOnly = EmitOptions.Default.WithEmitMetadataOnly(true).WithIncludePrivateMembers(false);
            CompileAndVerify(comp, emitOptions: emitRefOnly, verify: Verification.Passes);

            var refImage = comp.EmitToImageReference(emitRefOnly);
            var compWithRef = CreateEmptyCompilation("", parseOptions: parseOptions, references: new[] { MscorlibRef, refImage },
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            AssertEx.Equal(
                new[] { "<Module>", "S" },
                compWithRef.SourceModule.GetReferencedAssemblySymbols().Last().GlobalNamespace.GetMembers().Select(m => m.ToDisplayString()));

            AssertEx.Equal(
                new[] { "S.PrivateType S.field", "S..ctor()", "S.PrivateType" },
                compWithRef.GetMember<NamedTypeSymbol>("S").GetMembers().Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void EmitMetadataOnly_DisallowPdbs()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            using (var output = new MemoryStream())
            using (var pdbOutput = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output, pdbOutput,
                    options: EmitOptions.Default.WithEmitMetadataOnly(true)));
            }
        }

        [Fact]
        public void EmitMetadataOnly_DisallowMetadataPeStream()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            using (var output = new MemoryStream())
            using (var metadataPeOutput = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output, metadataPEStream: metadataPeOutput,
                    options: EmitOptions.Default.WithEmitMetadataOnly(true)));
            }
        }

        [Fact]
        public void IncludePrivateMembers_DisallowMetadataPeStream()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            using (var output = new MemoryStream())
            using (var metadataPeOutput = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output, metadataPEStream: metadataPeOutput,
                    options: EmitOptions.Default.WithIncludePrivateMembers(true)));
            }
        }

        [Fact]
        [WorkItem(20873, "https://github.com/dotnet/roslyn/issues/20873")]
        public void IncludePrivateMembersSilentlyAssumedTrueWhenEmittingRegular()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            using (var output = new MemoryStream())
            {
                // no exception
                _ = comp.Emit(output, options: EmitOptions.Default.WithIncludePrivateMembers(false));
            }
        }

        [Fact]
        public void EmitMetadata_DisallowOutputtingNetModule()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true).WithOutputKind(OutputKind.NetModule));

            using (var output = new MemoryStream())
            using (var metadataPeOutput = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output, metadataPEStream: metadataPeOutput,
                    options: EmitOptions.Default));
            }
        }

        [Fact]
        public void EmitMetadataOnly_DisallowOutputtingNetModule()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true).WithOutputKind(OutputKind.NetModule));

            using (var output = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output,
                    options: EmitOptions.Default.WithEmitMetadataOnly(true)));
            }
        }

        [Fact]
        public void RefAssembly_AllowEmbeddingPdb()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll);

            using (var output = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                var result = comp.Emit(output, metadataPEStream: metadataOutput,
                    options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded).WithIncludePrivateMembers(false));

                verifyEmbeddedDebugInfo(output, new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.EmbeddedPortablePdb });
                verifyEmbeddedDebugInfo(metadataOutput, new DebugDirectoryEntryType[] { DebugDirectoryEntryType.Reproducible });
            }

            void verifyEmbeddedDebugInfo(MemoryStream stream, DebugDirectoryEntryType[] expected)
            {
                using (var peReader = new PEReader(stream.ToImmutable()))
                {
                    var entries = peReader.ReadDebugDirectory();
                    AssertEx.Equal(expected, entries.Select(e => e.Type));
                }
            }
        }

        [Fact]
        public void EmitMetadataOnly_DisallowEmbeddingPdb()
        {
            CSharpCompilation comp = CreateEmptyCompilation("", references: new[] { MscorlibRef },
                options: TestOptions.DebugDll);

            using (var output = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => comp.Emit(output,
                    options: EmitOptions.Default.WithEmitMetadataOnly(true)
                        .WithDebugInformationFormat(DebugInformationFormat.Embedded)));
            }
        }

        [Fact]
        public void EmitMetadata()
        {
            string source = @"
public abstract class PublicClass
{
    public void PublicMethod() { System.Console.Write(""Hello""); }
}
";
            CSharpCompilation comp = CreateEmptyCompilation(source, references: new[] { MscorlibRef },
                options: TestOptions.DebugDll.WithDeterministic(true));

            using (var output = new MemoryStream())
            using (var pdbOutput = new MemoryStream())
            using (var metadataOutput = new MemoryStream())
            {
                var result = comp.Emit(output, pdbOutput, metadataPEStream: metadataOutput);
                Assert.True(result.Success);
                Assert.NotEqual(0, output.Position);
                Assert.NotEqual(0, pdbOutput.Position);
                Assert.NotEqual(0, metadataOutput.Position);
                MetadataReaderUtils.AssertNotThrowNull(ImmutableArray.CreateRange(output.GetBuffer()));
                MetadataReaderUtils.AssertEmptyOrThrowNull(ImmutableArray.CreateRange(metadataOutput.GetBuffer()));
            }

            var peImage = comp.EmitToArray();
            MetadataReaderUtils.AssertNotThrowNull(peImage);
        }

        /// <summary>
        /// Check that when we emit metadata only, we include metadata for
        /// compiler generate methods (e.g. the ones for implicit interface
        /// implementation).
        /// </summary>
        [Fact]
        public void EmitMetadataOnly_SynthesizedExplicitImplementations()
        {
            var ilAssemblyReference = TestReferences.SymbolsTests.CustomModifiers.CppCli.dll;

            var libAssemblyName = "SynthesizedMethodMetadata";
            var exeAssemblyName = "CallSynthesizedMethod";

            // Setup: CppBase2 has methods that implement CppInterface1, but it doesn't declare
            // that it implements the interface.  Class1 does declare that it implements the
            // interface, but it's empty so it counts on CppBase2 to provide the implementations.
            // Since CppBase2 is not in the current source module, bridge methods are inserted
            // into Class1 to implement the interface methods by delegating to CppBase2.
            var libText = @"
public class Class1 : CppCli.CppBase2, CppCli.CppInterface1
{
}
";

            var libComp = CreateCompilation(
                source: libText,
                references: new MetadataReference[] { ilAssemblyReference },
                options: TestOptions.ReleaseDll,
                assemblyName: libAssemblyName);

            Assert.False(libComp.GetDiagnostics().Any());

            EmitResult emitResult;
            byte[] dllImage;
            using (var output = new MemoryStream())
            {
                emitResult = libComp.Emit(output, options: new EmitOptions(metadataOnly: true));
                dllImage = output.ToArray();
            }

            Assert.True(emitResult.Success);
            emitResult.Diagnostics.Verify();
            Assert.True(dllImage.Length > 0, "no metadata emitted");

            // NOTE: this DLL won't PEVerify because there are no method bodies.

            var class1 = libComp.GlobalNamespace.GetMember<SourceNamedTypeSymbol>("Class1");

            // We would prefer to check that the module used by Compiler.Emit does the right thing,
            // but we don't have access to that object, so we'll create our own and manipulate it
            // in the same way.
            var module = new PEAssemblyBuilder((SourceAssemblySymbol)class1.ContainingAssembly, EmitOptions.Default,
                OutputKind.DynamicallyLinkedLibrary, GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable<ResourceDescription>());
            SynthesizedMetadataCompiler.ProcessSynthesizedMembers(libComp, module, default(CancellationToken));

            var class1TypeDef = (Cci.ITypeDefinition)class1.GetCciAdapter();

            var symbolSynthesized = class1.GetSynthesizedExplicitImplementations(CancellationToken.None).ForwardingMethods;
            var context = new EmitContext(module, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);
            var cciExplicit = class1TypeDef.GetExplicitImplementationOverrides(context);
            var cciMethods = class1TypeDef.GetMethods(context).Where(m => ((MethodSymbol)m.GetInternalSymbol()).MethodKind != MethodKind.Constructor);

            context.Diagnostics.Verify();
            var symbolsSynthesizedCount = symbolSynthesized.Length;
            Assert.True(symbolsSynthesizedCount > 0, "Expected more than 0 synthesized method symbols.");
            Assert.Equal(symbolsSynthesizedCount, cciExplicit.Count());
            Assert.Equal(symbolsSynthesizedCount, cciMethods.Count());

            var libAssemblyReference = MetadataReference.CreateFromImage(dllImage.AsImmutableOrNull());

            var exeText = @"
class Class2
{
    public static void Main()
    {
        CppCli.CppInterface1 c = new Class1();
        c.Method1(1);
        c.Method2(2);
    }
}  
";

            var exeComp = CreateCompilation(
                source: exeText,
                references: new MetadataReference[] { ilAssemblyReference, libAssemblyReference },
                assemblyName: exeAssemblyName);

            Assert.False(exeComp.GetDiagnostics().Any());

            using (var output = new MemoryStream())
            {
                emitResult = exeComp.Emit(output);

                Assert.True(emitResult.Success);
                emitResult.Diagnostics.Verify();
                output.Flush();
                Assert.True(output.Length > 0, "no metadata emitted");
            }

            // NOTE: there's no point in trying to run the EXE since it depends on a DLL with no method bodies.
        }

        [WorkItem(539982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539982")]
        [Fact]
        public void EmitNestedLambdaWithAddPlusOperator()
        {
            CompileAndVerify(@"
public class C
{
    delegate int D(int i);
    delegate D E(int i);

    public static void Main()
    {
        D y = x => x + 1;
        E e = x => (y += (z => z + 1));
    }
}
");
        }

        [Fact, WorkItem(539983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539983")]
        public void EmitAlwaysFalseExpression()
        {
            CompileAndVerify(@"
class C
{
    static bool Goo(int i)
    {
        int y = 10;
        bool x = (y == null); // NYI: Implicit null conversion
        return x;
    }
}
");
        }

        [WorkItem(540146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540146")]
        [Fact]
        public void EmitLambdaInConstructorInitializer()
        {
            string source = @"
using System;
public class A
{
    public A(string x):this(()=>x) {}    
    public A(Func<string> x)
    {
        Console.WriteLine(x());
    }
    
    static void Main()
    {
        A a = new A(""Hello"");
    }
}";
            CompileAndVerify(source, expectedOutput: "Hello");
        }

        [WorkItem(540146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540146")]
        [Fact]
        public void EmitLambdaInConstructorBody()
        {
            string source = @"
using System;
public class A
{
    public string y = ""!"";

    public A(string x) {func(()=>x+y); }
    public A(Func<string> x)
    {
        Console.WriteLine(x());
    }
 
public void func(Func<string> x)
    {
        Console.WriteLine(x());
    }
    static void Main()
    {
        A a = new A(""Hello"");
    }
}";
            CompileAndVerify(source, expectedOutput: "Hello!");
        }

        [WorkItem(540146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540146")]
        [Fact]
        public void EmitLambdaInConstructorInitializerAndBody()
        {
            string source = @"
using System;
public class A
{
    public string y = ""!"";
    
    public A(string x):this(()=>x){func(()=>x+y);}    
    public A(Func<string> x)
    {
        Console.WriteLine(x());
    }
    public void func (Func<string> x)
    {
        Console.WriteLine(x());
    }
    static void Main()
    {
        A a = new A(""Hello"");
    }
}";
            CompileAndVerify(source, expectedOutput: @"
Hello
Hello!
");
        }

        [WorkItem(541786, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541786")]
        [Fact]
        public void EmitInvocationExprInIfStatementNestedInsideCatch()
        {
            string source = @"
static class Test
{
    static public void Main()
    {
        int i1 = 45;

        try
        {
        }
        catch
        {
            if (i1.ToString() == null)
            {
            }
        }
        System.Console.WriteLine(i1);
    }
}";
            CompileAndVerify(source, expectedOutput: "45");
        }

        [WorkItem(541822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541822")]
        [Fact]
        public void EmitSwitchOnByteType()
        {
            string source = @"
using System;
public class Test
{
    public static object TestSwitch(byte val)
    {
        switch (val)
        {
            case (byte)0: return 0;
            case (byte)1: return 1;
            case (byte)0x7F: return (byte)0x7F;
            case (byte)0xFE: return (byte)0xFE;
            case (byte)0xFF: return (byte)0xFF;
            default: return null;
        }
    }
    public static void Main()
    {
        Console.WriteLine(TestSwitch(0));
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(541823, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541823")]
        [Fact]
        public void EmitSwitchOnIntTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(int val)
    {
        switch (val)
        {
            case (int)int.MinValue:
            case (int)int.MinValue + 1:
            case (int)short.MinValue:
            case (int)short.MinValue + 1:
            case (int)sbyte.MinValue: return 0;
            case (int)-1: return -1;
            case (int)0: return 0;
            case (int)1: return 0;
            case (int)0x7F: return 0;
            case (int)0xFE: return 0;
            case (int)0xFF: return 0;
            case (int)0x7FFE: return 0;
            case (int)0xFFFE:
            case (int)0x7FFFFFFF: return 0;
            default: return null;
        }
    }
    public static void Main()
    {
        System.Console.WriteLine(TestSwitch(-1));
    }
}
";
            CompileAndVerify(source, expectedOutput: "-1");
        }

        [WorkItem(541824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541824")]
        [Fact]
        public void EmitSwitchOnLongTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(long val)
    {
        switch (val)
        {
            case (long)long.MinValue: return (long)long.MinValue;
            case (long)long.MinValue + 1: return (long)long.MinValue + 1;
            case (long)int.MinValue: return (long)int.MinValue;
            case (long)int.MinValue + 1: return (long)int.MinValue + 1;
            case (long)short.MinValue: return (long)short.MinValue;
            case (long)short.MinValue + 1: return (long)short.MinValue + 1;
            case (long)sbyte.MinValue: return (long)sbyte.MinValue;
            case (long)-1: return (long)-1;
            case (long)0: return (long)0;
            case (long)1: return (long)1;
            case (long)0x7F: return (long)0x7F;
            case (long)0xFE: return (long)0xFE;
            case (long)0xFF: return (long)0xFF;
            case (long)0x7FFE: return (long)0x7FFE;
            case (long)0x7FFF: return (long)0x7FFF;
            case (long)0xFFFE: return (long)0xFFFE;
            case (long)0xFFFF: return (long)0xFFFF;
            case (long)0x7FFFFFFE: return (long)0x7FFFFFFE;
            case (long)0x7FFFFFFF: return (long)0x7FFFFFFF;
            case (long)0xFFFFFFFE: return (long)0xFFFFFFFE;
            case (long)0xFFFFFFFF: return (long)0xFFFFFFFF;
            case (long)0x7FFFFFFFFFFFFFFE: return (long)0x7FFFFFFFFFFFFFFE;
            case (long)0x7FFFFFFFFFFFFFFF: return (long)0x7FFFFFFFFFFFFFFF;
            default: return null;
        }
    }
    public static void Main()
    {
        System.Console.WriteLine(TestSwitch(0));
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(541840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541840")]
        [Fact]
        public void EmitSwitchOnLongTypeBoundary2()
        {
            string source = @"
public class Test
{
    private static int DoLong()
    {
        int ret = 2;
        long l = 0x7fffffffffffffffL;

        switch (l)
        {
            case 1L:
            case 9223372036854775807L:
                ret--;
                break;
            case -1L:
                break;
            default:
                break;
        }

        switch (l)
        {
            case 1L:
            case -1L:
                break;
            default:
                ret--;
                break;
        }
        return (ret);
    }

    public static void Main(string[] args)
    {
        System.Console.WriteLine(DoLong());
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(541840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541840")]
        [Fact]
        public void EmitSwitchOnLongTypeBoundary3()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(long val)
    {
        switch (val)
        {
            case (long)long.MinValue: return (long)long.MinValue;
            case (long)long.MinValue + 1: return (long)long.MinValue + 1;
            case (long)int.MinValue: return (long)int.MinValue;
            case (long)int.MinValue + 1: return (long)int.MinValue + 1;
            case (long)short.MinValue: return (long)short.MinValue;
            case (long)short.MinValue + 1: return (long)short.MinValue + 1;
            case (long)sbyte.MinValue: return (long)sbyte.MinValue;
            case (long)-1: return (long)-1;
            case (long)0: return (long)0;
            case (long)1: return (long)1;
            case (long)0x7F: return (long)0x7F;
            case (long)0xFE: return (long)0xFE;
            case (long)0xFF: return (long)0xFF;
            case (long)0x7FFE: return (long)0x7FFE;
            case (long)0x7FFF: return (long)0x7FFF;
            case (long)0xFFFE: return (long)0xFFFE;
            case (long)0xFFFF: return (long)0xFFFF;
            case (long)0x7FFFFFFE: return (long)0x7FFFFFFE;
            case (long)0x7FFFFFFF: return (long)0x7FFFFFFF;
            case (long)0xFFFFFFFE: return (long)0xFFFFFFFE;
            case (long)0xFFFFFFFF: return (long)0xFFFFFFFF;
            case (long)0x7FFFFFFFFFFFFFFE: return (long)0x7FFFFFFFFFFFFFFE;
            case (long)0x7FFFFFFFFFFFFFFF: return (long)0x7FFFFFFFFFFFFFFF;
            default: return null;
        }
    }
    public static void Main()
    {
        bool b1 = true;

        b1 = b1 && (((long)long.MinValue).Equals(TestSwitch(long.MinValue)));
        b1 = b1 && (((long)long.MinValue + 1).Equals(TestSwitch(long.MinValue + 1)));
        b1 = b1 && (((long)int.MinValue).Equals(TestSwitch(int.MinValue)));
        b1 = b1 && (((long)int.MinValue + 1).Equals(TestSwitch(int.MinValue + 1)));
        b1 = b1 && (((long)short.MinValue).Equals(TestSwitch(short.MinValue)));
        b1 = b1 && (((long)short.MinValue + 1).Equals(TestSwitch(short.MinValue + 1)));
        b1 = b1 && (((long)sbyte.MinValue).Equals(TestSwitch(sbyte.MinValue)));
        b1 = b1 && (((long)-1).Equals(TestSwitch(-1)));
        b1 = b1 && (((long)0).Equals(TestSwitch(0)));
        b1 = b1 && (((long)1).Equals(TestSwitch(1)));
        b1 = b1 && (((long)0x7F).Equals(TestSwitch(0x7F)));
        b1 = b1 && (((long)0xFE).Equals(TestSwitch(0xFE)));
        b1 = b1 && (((long)0xFF).Equals(TestSwitch(0xFF)));
        b1 = b1 && (((long)0x7FFE).Equals(TestSwitch(0x7FFE)));
        b1 = b1 && (((long)0x7FFF).Equals(TestSwitch(0x7FFF)));
        b1 = b1 && (((long)0xFFFE).Equals(TestSwitch(0xFFFE)));
        b1 = b1 && (((long)0xFFFF).Equals(TestSwitch(0xFFFF)));
        b1 = b1 && (((long)0x7FFFFFFE).Equals(TestSwitch(0x7FFFFFFE)));
        b1 = b1 && (((long)0x7FFFFFFF).Equals(TestSwitch(0x7FFFFFFF)));
        b1 = b1 && (((long)0xFFFFFFFE).Equals(TestSwitch(0xFFFFFFFE)));
        b1 = b1 && (((long)0xFFFFFFFF).Equals(TestSwitch(0xFFFFFFFF)));
        b1 = b1 && (((long)0x7FFFFFFFFFFFFFFE).Equals(TestSwitch(0x7FFFFFFFFFFFFFFE)));
        b1 = b1 && (((long)0x7FFFFFFFFFFFFFFF).Equals(TestSwitch(0x7FFFFFFFFFFFFFFF)));

        System.Console.Write(b1);
    }
}
";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(541840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541840")]
        [Fact]
        public void EmitSwitchOnCharTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(char val)
    {
        switch (val)
        {
            case (char)0: return (char)0;
            case (char)1: return (char)1;
            case (char)0x7F: return (char)0x7F;
            case (char)0xFE: return (char)0xFE;
            case (char)0xFF: return (char)0xFF;
            case (char)0x7FFE: return (char)0x7FFE;
            case (char)0x7FFF: return (char)0x7FFF;
            case (char)0xFFFE: return (char)0xFFFE;
            case (char)0xFFFF: return (char)0xFFFF;
            default: return null;
        }
    }
    public static void Main()
    {
        bool b1 = true;

        b1 = b1 && (((char)0).Equals(TestSwitch((char)0)));
        b1 = b1 && (((char)1).Equals(TestSwitch((char)1)));
        b1 = b1 && (((char)0x7F).Equals(TestSwitch((char)0x7F)));
        b1 = b1 && (((char)0xFE).Equals(TestSwitch((char)0xFE)));
        b1 = b1 && (((char)0xFF).Equals(TestSwitch((char)0xFF)));
        b1 = b1 && (((char)0x7FFE).Equals(TestSwitch((char)0x7FFE)));
        b1 = b1 && (((char)0x7FFF).Equals(TestSwitch((char)0x7FFF)));
        b1 = b1 && (((char)0xFFFE).Equals(TestSwitch((char)0xFFFE)));
        b1 = b1 && (((char)0xFFFF).Equals(TestSwitch((char)0xFFFF)));

        System.Console.Write(b1);
    }
}
";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(541840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541840")]
        [Fact]
        public void EmitSwitchOnUIntTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(uint val)
    {
        switch (val)
        {
            case (uint)0: return (uint)0;
            case (uint)1: return (uint)1;
            case (uint)0x7F: return (uint)0x7F;
            case (uint)0xFE: return (uint)0xFE;
            case (uint)0xFF: return (uint)0xFF;
            case (uint)0x7FFE: return (uint)0x7FFE;
            case (uint)0x7FFF: return (uint)0x7FFF;
            case (uint)0xFFFE: return (uint)0xFFFE;
            case (uint)0xFFFF: return (uint)0xFFFF;
            case (uint)0x7FFFFFFE: return (uint)0x7FFFFFFE;
            case (uint)0x7FFFFFFF: return (uint)0x7FFFFFFF;
            case (uint)0xFFFFFFFE: return (uint)0xFFFFFFFE;
            case (uint)0xFFFFFFFF: return (uint)0xFFFFFFFF;
            default: return null;
        }
    }
    public static void Main()
    {
        bool b1 = true;

        b1 = b1 && (((uint)0).Equals(TestSwitch(0)));
        b1 = b1 && (((uint)1).Equals(TestSwitch(1)));
        b1 = b1 && (((uint)0x7F).Equals(TestSwitch(0x7F)));
        b1 = b1 && (((uint)0xFE).Equals(TestSwitch(0xFE)));
        b1 = b1 && (((uint)0xFF).Equals(TestSwitch(0xFF)));
        b1 = b1 && (((uint)0x7FFE).Equals(TestSwitch(0x7FFE)));
        b1 = b1 && (((uint)0x7FFF).Equals(TestSwitch(0x7FFF)));
        b1 = b1 && (((uint)0xFFFE).Equals(TestSwitch(0xFFFE)));
        b1 = b1 && (((uint)0xFFFF).Equals(TestSwitch(0xFFFF)));
        b1 = b1 && (((uint)0x7FFFFFFE).Equals(TestSwitch(0x7FFFFFFE)));
        b1 = b1 && (((uint)0x7FFFFFFF).Equals(TestSwitch(0x7FFFFFFF)));
        b1 = b1 && (((uint)0xFFFFFFFE).Equals(TestSwitch(0xFFFFFFFE)));
        b1 = b1 && (((uint)0xFFFFFFFF).Equals(TestSwitch(0xFFFFFFFF)));

        System.Console.Write(b1);
    }
}

";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(541824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541824")]
        [Fact]
        public void EmitSwitchOnUnsignedLongTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(ulong val)
    {
        switch (val)
        {
            case ulong.MinValue: return 0;
            case ulong.MaxValue: return 1;
            default: return 1;
        }
    }
    public static void Main()
    {
        System.Console.WriteLine(TestSwitch(0));
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(541847, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541847")]
        [Fact]
        public void EmitSwitchOnUnsignedLongTypeBoundary2()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(ulong val)
    {
        switch (val)
        {
            case (ulong)0: return (ulong)0;
            case (ulong)1: return (ulong)1;
            case (ulong)0x7F: return (ulong)0x7F;
            case (ulong)0xFE: return (ulong)0xFE;
            case (ulong)0xFF: return (ulong)0xFF;
            case (ulong)0x7FFE: return (ulong)0x7FFE;
            case (ulong)0x7FFF: return (ulong)0x7FFF;
            case (ulong)0xFFFE: return (ulong)0xFFFE;
            case (ulong)0xFFFF: return (ulong)0xFFFF;
            case (ulong)0x7FFFFFFE: return (ulong)0x7FFFFFFE;
            case (ulong)0x7FFFFFFF: return (ulong)0x7FFFFFFF;
            case (ulong)0xFFFFFFFE: return (ulong)0xFFFFFFFE;
            case (ulong)0xFFFFFFFF: return (ulong)0xFFFFFFFF;
            case (ulong)0x7FFFFFFFFFFFFFFE: return (ulong)0x7FFFFFFFFFFFFFFE;
            case (ulong)0x7FFFFFFFFFFFFFFF: return (ulong)0x7FFFFFFFFFFFFFFF;
            case (ulong)0xFFFFFFFFFFFFFFFE: return (ulong)0xFFFFFFFFFFFFFFFE;
            case (ulong)0xFFFFFFFFFFFFFFFF: return (ulong)0xFFFFFFFFFFFFFFFF;
            default: return null;
        }
    }
    public static void Main()
    {
        bool b1 = true;
        b1 = b1 && (((ulong)0).Equals(TestSwitch(0)));
        b1 = b1 && (((ulong)1).Equals(TestSwitch(1)));
        b1 = b1 && (((ulong)0x7F).Equals(TestSwitch(0x7F)));
        b1 = b1 && (((ulong)0xFE).Equals(TestSwitch(0xFE)));
        b1 = b1 && (((ulong)0xFF).Equals(TestSwitch(0xFF)));
        b1 = b1 && (((ulong)0x7FFE).Equals(TestSwitch(0x7FFE)));
        b1 = b1 && (((ulong)0x7FFF).Equals(TestSwitch(0x7FFF)));
        b1 = b1 && (((ulong)0xFFFE).Equals(TestSwitch(0xFFFE)));
        b1 = b1 && (((ulong)0xFFFF).Equals(TestSwitch(0xFFFF)));
        b1 = b1 && (((ulong)0x7FFFFFFE).Equals(TestSwitch(0x7FFFFFFE)));
        b1 = b1 && (((ulong)0x7FFFFFFF).Equals(TestSwitch(0x7FFFFFFF)));
        b1 = b1 && (((ulong)0xFFFFFFFE).Equals(TestSwitch(0xFFFFFFFE)));
        b1 = b1 && (((ulong)0xFFFFFFFF).Equals(TestSwitch(0xFFFFFFFF)));
        b1 = b1 && (((ulong)0x7FFFFFFFFFFFFFFE).Equals(TestSwitch(0x7FFFFFFFFFFFFFFE)));
        b1 = b1 && (((ulong)0x7FFFFFFFFFFFFFFF).Equals(TestSwitch(0x7FFFFFFFFFFFFFFF)));
        b1 = b1 && (((ulong)0xFFFFFFFFFFFFFFFE).Equals(TestSwitch(0xFFFFFFFFFFFFFFFE)));
        b1 = b1 && (((ulong)0xFFFFFFFFFFFFFFFF).Equals(TestSwitch(0xFFFFFFFFFFFFFFFF)));

        System.Console.Write(b1);
    }
}
";
            CompileAndVerify(source, expectedOutput: "True");
        }

        [WorkItem(541839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541839")]
        [Fact]
        public void EmitSwitchOnShortTypeBoundary()
        {
            string source = @"
public class Test
{
    public static object TestSwitch(short val)
    {
        switch (val)
        {
            case (short)short.MinValue: return (short)short.MinValue;
            case (short)short.MinValue + 1: return (short)short.MinValue + 1;
            case (short)sbyte.MinValue: return (short)sbyte.MinValue;
            case (short)-1: return (short)-1;
            case (short)0: return (short)0;
            case (short)1: return (short)1;
            case (short)0x7F: return (short)0x7F;
            case (short)0xFE: return (short)0xFE;
            case (short)0xFF: return (short)0xFF;
            case (short)0x7FFE: return (short)0x7FFE;
            case (short)0x7FFF: return (short)0x7FFF;
            default: return null;
        }
    }

    public static void Main()
    {
        System.Console.WriteLine(TestSwitch(1));
    }
}
";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [WorkItem(542563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542563")]
        [Fact]
        public void IncompleteIndexerDeclWithSyntaxErrors()
        {
            string source = @"
public class Test
{
    public sealed object this";

            var compilation = CreateCompilation(source);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = compilation.Emit(output, pdbStream: null, xmlDocumentationStream: null, win32Resources: null);
            }

            Assert.False(emitResult.Success);
            Assert.NotEmpty(emitResult.Diagnostics);
        }

        [WorkItem(541639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541639")]
        [Fact]
        public void VariableDeclInsideSwitchCaptureInLambdaExpr()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        switch (10)
        {
            default:
                int i = 10;
                Func<int> f1 = () => i;
                break;
        }
    }
}";

            var compilation = CreateCompilation(source);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = compilation.Emit(output, pdbStream: null, xmlDocumentationStream: null, win32Resources: null);
            }

            Assert.True(emitResult.Success);
        }

        [WorkItem(541639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541639")]
        [Fact]
        public void MultipleVariableDeclInsideSwitchCaptureInLambdaExpr()
        {
            string source = @"
using System;

class C
{
    public static void Main()
    {
        int i = 0;
        switch (i)
        {
            case 0:
                int j = 0;
                Func<int> f1 = () => i + j;
                break;

            default:
                int k = 0;
                Func<int> f2 = () => i + k;
                break;
        }
    }
}";

            var compilation = CreateCompilation(source);

            EmitResult emitResult;
            using (var output = new MemoryStream())
            {
                emitResult = compilation.Emit(output, pdbStream: null, xmlDocumentationStream: null, win32Resources: null);
            }

            Assert.True(emitResult.Success);
        }
        #region "PE and metadata bits"

        [Fact]
        public void CheckRuntimeMDVersion()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CSharpCompilation.Create(
                "v2Fx.exe",
                new[] { Parse(source) },
                new[] { Net20.mscorlib });

            //EDMAURER this is built with a 2.0 mscorlib. The runtimeMetadataVersion should be the same as the runtimeMetadataVersion stored in the assembly
            //that contains System.Object.
            var metadataReader = ModuleMetadata.CreateFromStream(compilation.EmitToStream()).MetadataReader;
            Assert.Equal("v2.0.50727", metadataReader.MetadataVersion);
        }

        [Fact]
        public void CheckCorflags()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            PEHeaders peHeaders;

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.AnyCpu));
            peHeaders = new PEHeaders(compilation.EmitToStream());
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X86));
            peHeaders = new PEHeaders(compilation.EmitToStream());
            Assert.Equal(CorFlags.ILOnly | CorFlags.Requires32Bit, peHeaders.CorHeader.Flags);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X64));
            peHeaders = new PEHeaders(compilation.EmitToStream());
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags);
            Assert.True(peHeaders.Requires64Bits());
            Assert.True(peHeaders.RequiresAmdInstructionSet());

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.AnyCpu32BitPreferred));
            peHeaders = new PEHeaders(compilation.EmitToStream());
            Assert.False(peHeaders.Requires64Bits());
            Assert.False(peHeaders.RequiresAmdInstructionSet());
            Assert.Equal(CorFlags.ILOnly | CorFlags.Requires32Bit | CorFlags.Prefers32Bit, peHeaders.CorHeader.Flags);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.Arm));
            peHeaders = new PEHeaders(compilation.EmitToStream());
            Assert.False(peHeaders.Requires64Bits());
            Assert.False(peHeaders.RequiresAmdInstructionSet());
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags);
        }

        [Fact]
        public void CheckCOFFAndPEOptionalHeaders32()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source,
                options: TestOptions.DebugDll.WithPlatform(Platform.X86));

            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.False(peHeaders.Requires64Bits());
            Assert.True(peHeaders.IsDll);
            Assert.False(peHeaders.IsExe);
            Assert.False(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware));
            //interesting Optional PE header bits
            //We will use a range beginning with 0x30 to identify the Roslyn compiler family.
            Assert.Equal(0x30, peHeaders.PEHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion);
            Assert.Equal(0x10000000u, peHeaders.PEHeader.ImageBase);
            Assert.Equal(0x200, peHeaders.PEHeader.FileAlignment);
            Assert.Equal(0x8540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE
            //Verify additional items 
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfStackReserve);
            Assert.Equal(0x1000u, peHeaders.PEHeader.SizeOfStackCommit);
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfHeapReserve);
            Assert.Equal(0x1000u, peHeaders.PEHeader.SizeOfHeapCommit);
        }

        [Fact]
        public void CheckCOFFAndPEOptionalHeaders64()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source,
                options: TestOptions.DebugDll.WithPlatform(Platform.X64));

            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.True(peHeaders.Requires64Bits());
            Assert.True(peHeaders.IsDll);
            Assert.False(peHeaders.IsExe);
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware));
            //interesting Optional PE header bits
            //We will use a range beginning with 0x30 to identify the Roslyn compiler family.
            Assert.Equal(0x30, peHeaders.PEHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion);
            // the default value is the same as the 32 bit default value
            Assert.Equal(0x0000000180000000u, peHeaders.PEHeader.ImageBase);
            Assert.Equal(0x00000200, peHeaders.PEHeader.FileAlignment);      //doesn't change based on architecture.
            Assert.Equal(0x8540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE
            //Verify additional items
            Assert.Equal(0x00400000u, peHeaders.PEHeader.SizeOfStackReserve);
            Assert.Equal(0x4000u, peHeaders.PEHeader.SizeOfStackCommit);
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfHeapReserve);
            Assert.Equal(0x2000u, peHeaders.PEHeader.SizeOfHeapCommit);
            Assert.Equal(0x8664, (ushort)peHeaders.CoffHeader.Machine);     //AMD64 (K8)

            //default for non-arm, non-appcontainer outputs. EDMAURER: This is an intentional change from Dev11.
            //Should we find that it is too disruptive. We will consider rolling back.
            //It turns out to be too disruptive. Rolling back to 4.0
            Assert.Equal(4, peHeaders.PEHeader.MajorSubsystemVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorSubsystemVersion);

            //The following ensure that the runtime startup stub was not emitted. It is not needed on modern operating systems.
            Assert.Equal(0, peHeaders.PEHeader.ImportAddressTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.ImportAddressTableDirectory.Size);
            Assert.Equal(0, peHeaders.PEHeader.ImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.ImportTableDirectory.Size);
            Assert.Equal(0, peHeaders.PEHeader.BaseRelocationTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.BaseRelocationTableDirectory.Size);
        }

        [Fact]
        public void CheckCOFFAndPEOptionalHeadersARM()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source,
                options: TestOptions.DebugDll.WithPlatform(Platform.Arm));

            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.False(peHeaders.Requires64Bits());
            Assert.True(peHeaders.IsDll);
            Assert.False(peHeaders.IsExe);
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware));
            //interesting Optional PE header bits
            //We will use a range beginning with 0x30 to identify the Roslyn compiler family.
            Assert.Equal(0x30, peHeaders.PEHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion);
            // the default value is the same as the 32 bit default value
            Assert.Equal(0x10000000u, peHeaders.PEHeader.ImageBase);
            Assert.Equal(0x200, peHeaders.PEHeader.FileAlignment);
            Assert.Equal(0x8540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE
            Assert.Equal(0x01c4, (ushort)peHeaders.CoffHeader.Machine);
            Assert.Equal(6, peHeaders.PEHeader.MajorSubsystemVersion);    //Arm targets only run on 6.2 and above
            Assert.Equal(2, peHeaders.PEHeader.MinorSubsystemVersion);
            //The following ensure that the runtime startup stub was not emitted. It is not needed on modern operating systems.
            Assert.Equal(0, peHeaders.PEHeader.ImportAddressTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.ImportAddressTableDirectory.Size);
            Assert.Equal(0, peHeaders.PEHeader.ImportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.ImportTableDirectory.Size);
            Assert.Equal(0, peHeaders.PEHeader.BaseRelocationTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, peHeaders.PEHeader.BaseRelocationTableDirectory.Size);
        }

        [Fact]
        public void CheckCOFFAndPEOptionalHeadersAnyCPUExe()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source,
                options: TestOptions.ReleaseExe.WithPlatform(Platform.AnyCpu));

            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.False(peHeaders.Requires64Bits());
            Assert.True(peHeaders.IsExe);
            Assert.False(peHeaders.IsDll);
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware));
            //interesting Optional PE header bits
            //We will use a range beginning with 0x30 to identify the Roslyn compiler family.
            Assert.Equal(0x30, peHeaders.PEHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion);
            Assert.Equal(0x00400000ul, peHeaders.PEHeader.ImageBase);
            Assert.Equal(0x00000200, peHeaders.PEHeader.FileAlignment);
            Assert.True(peHeaders.IsConsoleApplication); //should change if this is a windows app.
            Assert.Equal(0x8540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfStackReserve);
            Assert.Equal(0x1000u, peHeaders.PEHeader.SizeOfStackCommit);
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfHeapReserve);
            Assert.Equal(0x1000u, peHeaders.PEHeader.SizeOfHeapCommit);

            //The following ensure that the runtime startup stub was emitted. It is not needed on modern operating systems.
            Assert.NotEqual(0, peHeaders.PEHeader.ImportAddressTableDirectory.RelativeVirtualAddress);
            Assert.NotEqual(0, peHeaders.PEHeader.ImportAddressTableDirectory.Size);
            Assert.NotEqual(0, peHeaders.PEHeader.ImportTableDirectory.RelativeVirtualAddress);
            Assert.NotEqual(0, peHeaders.PEHeader.ImportTableDirectory.Size);
            Assert.NotEqual(0, peHeaders.PEHeader.BaseRelocationTableDirectory.RelativeVirtualAddress);
            Assert.NotEqual(0, peHeaders.PEHeader.BaseRelocationTableDirectory.Size);
        }

        [Fact]
        public void CheckCOFFAndPEOptionalHeaders64Exe()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X64));
            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.True(peHeaders.Requires64Bits());
            Assert.True(peHeaders.IsExe);
            Assert.False(peHeaders.IsDll);
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware));
            //interesting Optional PE header bits
            //We will use a range beginning with 0x30 to identify the Roslyn compiler family.
            Assert.Equal(0x30, peHeaders.PEHeader.MajorLinkerVersion);
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion);
            Assert.Equal(0x0000000140000000ul, peHeaders.PEHeader.ImageBase);
            Assert.Equal(0x200, peHeaders.PEHeader.FileAlignment);  //doesn't change based on architecture
            Assert.True(peHeaders.IsConsoleApplication); //should change if this is a windows app.
            Assert.Equal(0x8540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE
            Assert.Equal(0x00400000u, peHeaders.PEHeader.SizeOfStackReserve);
            Assert.Equal(0x4000u, peHeaders.PEHeader.SizeOfStackCommit);
            Assert.Equal(0x00100000u, peHeaders.PEHeader.SizeOfHeapReserve); //no sure why we don't bump this up relative to 32bit as well.
            Assert.Equal(0x2000u, peHeaders.PEHeader.SizeOfHeapCommit);
        }

        [Fact]
        public void CheckDllCharacteristicsHighEntropyVA()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source);
            var peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(highEntropyVirtualAddressSpace: true)));

            //interesting COFF bits
            Assert.Equal(0x8560u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | HIGH_ENTROPY_VA (0x20)
        }

        [WorkItem(764418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/764418")]
        [Fact]
        public void CheckDllCharacteristicsWinRtApp()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.CreateTestOptions(OutputKind.WindowsRuntimeApplication, OptimizationLevel.Debug));
            var peHeaders = new PEHeaders(compilation.EmitToStream());

            //interesting COFF bits
            Assert.Equal(0x9540u, (ushort)peHeaders.PEHeader.DllCharacteristics);  //DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | IMAGE_DLLCHARACTERISTICS_APPCONTAINER (0x1000)
        }

        [Fact]
        public void CheckBaseAddress()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            // last four hex digits get zero'ed
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0x0000000010111111)));
            Assert.Equal(0x10110000ul, peHeaders.PEHeader.ImageBase);

            // test rounding up of values
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0x8000)));
            Assert.Equal(0x10000ul, peHeaders.PEHeader.ImageBase);

            // values less than 0x8000 get default baseaddress
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0x7fff)));
            Assert.Equal(0x00400000u, peHeaders.PEHeader.ImageBase);

            // default for 32bit
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X86));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: EmitOptions.Default));
            Assert.Equal(0x00400000u, peHeaders.PEHeader.ImageBase);

            // max for 32bit
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X86));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0xffff7fff)));
            Assert.Equal(0xffff0000ul, peHeaders.PEHeader.ImageBase);

            // max+1 for 32bit
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X86));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0xffff8000)));
            Assert.Equal(0x00400000u, peHeaders.PEHeader.ImageBase);

            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X64));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: EmitOptions.Default));
            Assert.Equal(0x0000000140000000u, peHeaders.PEHeader.ImageBase);

            // max for 64bit
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X64));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0xffffffffffff7fff)));
            Assert.Equal(0xffffffffffff0000ul, peHeaders.PEHeader.ImageBase);

            // max+1 for 64bit
            compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithPlatform(Platform.X64));
            peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(baseAddress: 0xffffffffffff8000)));
            Assert.Equal(0x0000000140000000u, peHeaders.PEHeader.ImageBase);
        }

        [Fact]
        public void CheckFileAlignment()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(fileAlignment: 1024)));
            Assert.Equal(1024, peHeaders.PEHeader.FileAlignment);
        }

        #endregion

        [Fact]
        public void Bug10273()
        {
            string source = @"
using System;

    public struct C1
    {
        public int C;
        public static int B = 12;

        public void F(){}

        public int A;
    }

    public delegate void B();

    public class A1
    {
        public int C;
        public static int  B = 12;

        public void F(){}

        public int A;

        public int I {get; set;}

        public void E(){}

        public int H {get; set;}
        public int G {get; set;}

        public event Action L;
        public void D(){}

        public event Action K;
        public event Action J;

        public partial class O { }
        public partial class N { }
        public partial class M { }

        public partial class N{}
        public partial class M{}
        public partial class O{}

        public void F(int x){}
        public void E(int x){}
        public void D(int x){}
    }

    namespace F{}

    public class G {}

    namespace E{}
    namespace D{}
";

            CompileAndVerify(source,
                             sourceSymbolValidator: delegate (ModuleSymbol m)
                             {
                                 string[] expectedGlobalMembers = { "C1", "B", "A1", "F", "G", "E", "D" };
                                 var actualGlobalMembers = m.GlobalNamespace.GetMembers().Where(member => !member.IsImplicitlyDeclared).ToArray();
                                 for (int i = 0; i < System.Math.Max(expectedGlobalMembers.Length, actualGlobalMembers.Length); i++)
                                 {
                                     Assert.Equal(expectedGlobalMembers[i], actualGlobalMembers[i].Name);
                                 }

                                 string[] expectedAMembers = {
                                                        "C", "B", "F", "A",
                                                        "<I>k__BackingField", "I", "get_I", "set_I",
                                                        "E",
                                                        "<H>k__BackingField", "H", "get_H", "set_H",
                                                        "<G>k__BackingField", "G", "get_G", "set_G",
                                                        "add_L", "remove_L", "L",
                                                        "D",
                                                        "add_K", "remove_K", "K",
                                                        "add_J", "remove_J", "J",
                                                        "O", "N", "M",
                                                        "F", "E", "D",
                                                        ".ctor", ".cctor"
                                                };

                                 var actualAMembers = ((SourceModuleSymbol)m).GlobalNamespace.GetTypeMembers("A1").Single().GetMembers().ToArray();

                                 for (int i = 0; i < System.Math.Max(expectedAMembers.Length, actualAMembers.Length); i++)
                                 {
                                     Assert.Equal(expectedAMembers[i], actualAMembers[i].Name);
                                 }

                                 string[] expectedBMembers = { ".ctor", "Invoke", "BeginInvoke", "EndInvoke" };
                                 var actualBMembers = ((SourceModuleSymbol)m).GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray();

                                 for (int i = 0; i < System.Math.Max(expectedBMembers.Length, actualBMembers.Length); i++)
                                 {
                                     Assert.Equal(expectedBMembers[i], actualBMembers[i].Name);
                                 }

                                 string[] expectedCMembers = {".cctor",
                                                            "C", "B", "F", "A",
                                                            ".ctor"};
                                 var actualCMembers = ((SourceModuleSymbol)m).GlobalNamespace.GetTypeMembers("C1").Single().GetMembers().ToArray();

                                 AssertEx.SetEqual(expectedCMembers, actualCMembers.Select(s => s.Name));
                             },
                             symbolValidator: delegate (ModuleSymbol m)
                             {
                                 string[] expectedAMembers = {"C", "B", "A",
                                                        "F",
                                                        "get_I", "set_I",
                                                        "E",
                                                        "get_H", "set_H",
                                                        "get_G", "set_G",
                                                        "add_L", "remove_L",
                                                        "D",
                                                        "add_K", "remove_K",
                                                        "add_J", "remove_J",
                                                        "F", "E", "D",
                                                        ".ctor",
                                                        "I", "H", "G",
                                                        "L", "K", "J",
                                                        "O", "N", "M",
                                                        };

                                 var actualAMembers = m.GlobalNamespace.GetTypeMembers("A1").Single().GetMembers().ToArray();

                                 AssertEx.SetEqual(expectedAMembers, actualAMembers.Select(s => s.Name));

                                 string[] expectedBMembers = { ".ctor", "BeginInvoke", "EndInvoke", "Invoke" };
                                 var actualBMembers = m.GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray();

                                 AssertEx.SetEqual(expectedBMembers, actualBMembers.Select(s => s.Name));

                                 string[] expectedCMembers = { "C", "B", "A", ".ctor", "F" };
                                 var actualCMembers = m.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers().ToArray();

                                 AssertEx.SetEqual(expectedCMembers, actualCMembers.Select(s => s.Name));
                             }
                            );
        }

        [WorkItem(543763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543763")]
        [Fact()]
        public void OptionalParamTypeAsDecimal()
        {
            string source = @"
public class Test
{
    public static decimal Goo(decimal d = 0)
    {
        return d;
    }

    public static void Main()
    {
        System.Console.WriteLine(Goo());
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(543932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543932")]
        [Fact]
        public void BranchCodeGenOnConditionDebug()
        {
            string source = @"
public class Test
{
    public static void Main()
    {
        int a_int = 0;
        if ((a_int != 0) || (false))
        {
            System.Console.WriteLine(""CheckPoint-1"");
        }

        System.Console.WriteLine(""CheckPoint-2"");
    }
}";

            var compilation = CreateCompilation(source);

            CompileAndVerify(source, expectedOutput: "CheckPoint-2");
        }

        [Fact]
        public void EmitAssemblyWithGivenName()
        {
            var name = "a";
            var extension = ".dll";
            var nameWithExtension = name + extension;

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll, assemblyName: name);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal(name, assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(nameWithExtension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: nameWithExtension)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(name, peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(nameWithExtension, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        // a.netmodule to b.netmodule
        [Fact]
        public void EmitModuleWithDifferentName()
        {
            var name = "a";
            var extension = ".netmodule";
            var outputName = "b";

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseModule.WithModuleName(name + extension), assemblyName: null);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal("?", assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(name + extension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: outputName + extension)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.False(peReader.IsAssembly);

                Assert.Equal(module.Name, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        // a.dll to b.dll - expected use case
        [Fact]
        public void EmitAssemblyWithDifferentName1()
        {
            var name = "a";
            var extension = ".dll";
            var nameOverride = "b";

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll, assemblyName: name);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal(name, assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(name + extension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: nameOverride + extension)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(nameOverride, peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(module.Name, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        // a.dll to b - odd, but allowable
        [Fact]
        public void EmitAssemblyWithDifferentName2()
        {
            var name = "a";
            var extension = ".dll";
            var nameOverride = "b";

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll, assemblyName: name);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal(name, assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(name + extension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: nameOverride)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(nameOverride, peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(module.Name, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        // a to b.dll - odd, but allowable
        [Fact]
        public void EmitAssemblyWithDifferentName3()
        {
            var name = "a";
            var extension = ".dll";
            var nameOverride = "b";

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll, assemblyName: name);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal(name, assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(name + extension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: nameOverride + extension)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(nameOverride, peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(module.Name, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        // a to b - odd, but allowable
        [Fact]
        public void EmitAssemblyWithDifferentName4()
        {
            var name = "a";
            var extension = ".dll";
            var nameOverride = "b";

            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll, assemblyName: name);
            compilation.VerifyDiagnostics();

            var assembly = compilation.Assembly;
            Assert.Equal(name, assembly.Name);

            var module = assembly.Modules.Single();
            Assert.Equal(name + extension, module.Name);

            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream, options: new EmitOptions(outputNameOverride: nameOverride)).Success);

            using (ModuleMetadata metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable()))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(nameOverride, peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(module.Name, peReader.GetString(peReader.GetModuleDefinition().Name));
            }
        }

        [WorkItem(570975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570975")]
        [Fact]
        public void Bug570975()
        {
            var source = @"
public sealed class ContentType
{       
	public void M(System.Collections.Generic.Dictionary<object, object> p)
	{   
		foreach (object parameterKey in p.Keys)
		{
		}
	}
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseModule, assemblyName: "ContentType");
            compilation.VerifyDiagnostics();

            using (ModuleMetadata block = ModuleMetadata.CreateFromStream(compilation.EmitToStream()))
            {
                var reader = block.MetadataReader;
                foreach (var typeRef in reader.TypeReferences)
                {
                    EntityHandle scope = reader.GetTypeReference(typeRef).ResolutionScope;
                    if (scope.Kind == HandleKind.TypeReference)
                    {
                        Assert.InRange(reader.GetRowNumber(scope), 1, reader.GetRowNumber(typeRef) - 1);
                    }
                }
            }
        }

        [Fact]
        public void IllegalNameOverride()
        {
            var compilation = CreateCompilation("class A { }", options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();

            var result = compilation.Emit(new MemoryStream(), options: new EmitOptions(outputNameOverride: "x\0x"));
            result.Diagnostics.Verify(
                // error CS2041: Invalid output name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_InvalidOutputName).WithArguments("Name contains invalid characters.").WithLocation(1, 1));

            Assert.False(result.Success);
        }

        // Verify via MetadataReader - comp option
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes3()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            // Setting the CompilationOption.AllowUnsafe causes an entry to be inserted into the DeclSecurity table
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            CompileAndVerify(compilation, verify: Verification.Passes, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        // Verify via MetadataReader - comp option, module case
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes4()
        {
            string source = @"
class C
{
    public static void Main()
    {
    }
}";
            // Setting the CompilationOption.AllowUnsafe causes an entry to be inserted into the DeclSecurity table
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule));
            compilation.VerifyDiagnostics();

            CompileAndVerify(compilation, verify: Verification.Skipped, symbolValidator: module =>
            {
                //no assembly => no decl security row
                ValidateDeclSecurity(module);
            });
        }

        // Verify via MetadataReader - attr in source
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes5()
        {
            // Writing the attributes in the source should have the same effect as the compilation option.
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

class C
{
    public static void Main()
    {
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        // Verify via MetadataReader - two attrs in source, same action
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes6()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, RemotingConfiguration = true)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]
[module: UnverifiableCode]

class C
{
    public static void Main()
    {
    }
}";
            // The attributes have the SecurityAction, so they should be merged into a single permission set.
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, RemotingConfiguration = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (6,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0002" + // number of attributes (small enough to fit in 1 byte)

                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u001a" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0015" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "RemotingConfiguration" + // property name
                        "\u0001" + // argument value (true)

                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0012" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u000d" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "UnmanagedCode" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        // Verify via MetadataReader - two attrs in source, different actions
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes7()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]
[module: UnverifiableCode]

class C
{
    public static void Main()
    {
    }
}";
            // The attributes have different SecurityActions, so they should not be merged into a single permission set.
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                // (6,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestOptional,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u001a" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0015" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "RemotingConfiguration" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0012" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u000d" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "UnmanagedCode" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        // Verify via MetadataReader - one attr in source, one synthesized, same action
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes8()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, RemotingConfiguration = true)]
[module: UnverifiableCode]

class C
{
    public static void Main()
    {
    }
}";
            // The attributes have the SecurityAction, so they should be merged into a single permission set.
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestMinimum, RemotingConfiguration = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestMinimum").WithArguments("System.Security.Permissions.SecurityAction.RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, verify: Verification.Passes, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0002" + // number of attributes (small enough to fit in 1 byte)

                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u001a" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0015" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "RemotingConfiguration" + // property name
                        "\u0001" + // argument value (true)

                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        // Verify via MetadataReader - one attr in source, one synthesized, different actions
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsDesktopTypes)]
        public void CheckUnsafeAttributes9()
        {
            string source = @"
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
[module: UnverifiableCode]

class C
{
    public static void Main()
    {
    }
}";
            // The attributes have different SecurityActions, so they should not be merged into a single permission set.
            var compilation = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,31): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestOptional' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.'
                // [assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration = true)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SecurityAction.RequestOptional").WithArguments("System.Security.Permissions.SecurityAction.RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."));

            CompileAndVerify(compilation, verify: Verification.Passes, symbolValidator: module =>
            {
                ValidateDeclSecurity(module, new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestOptional,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u001a" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0015" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "RemotingConfiguration" + // property name
                        "\u0001", // argument value (true)
                },
                new DeclSecurityEntry
                {
                    ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                    ParentKind = SymbolKind.Assembly,
                    PermissionSet =
                        "." + // always start with a dot
                        "\u0001" + // number of attributes (small enough to fit in 1 byte)
                        "\u0080\u0084" + // length of UTF-8 string (0x80 indicates a 2-byte encoding)
                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" + // attr type name
                        "\u0015" + // number of bytes in the encoding of the named arguments
                        "\u0001" + // number of named arguments
                        "\u0054" + // property (vs field)
                        "\u0002" + // type bool
                        "\u0010" + // length of UTF-8 string (small enough to fit in 1 byte)
                        "SkipVerification" + // property name
                        "\u0001", // argument value (true)
                });
            });
        }

        [Fact]
        [WorkItem(545651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545651")]
        public void TestReferenceToNestedGenericType()
        {
            string p1 = @"public class Goo<T> { }";
            string p2 = @"using System;

public class Test
{
    public class C<T> {}
    public class J<T> : C<Goo<T>> { }
    
    public static void Main()
    {
        Console.WriteLine(typeof(J<int>).BaseType.Equals(typeof(C<Goo<int>>)) ? 0 : 1);
    }
}";
            var c1 = CreateCompilation(p1, options: TestOptions.ReleaseDll, assemblyName: Guid.NewGuid().ToString());
            CompileAndVerify(p2, new[] { MetadataReference.CreateFromStream(c1.EmitToStream()) }, expectedOutput: "0");
        }

        [WorkItem(546450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546450")]
        [Fact]
        public void EmitNetModuleWithReferencedNetModule()
        {
            string source1 = @"public class A {}";
            string source2 = @"public class B: A {}";
            var comp = CreateCompilation(source1, options: TestOptions.ReleaseModule);
            var metadataRef = ModuleMetadata.CreateFromStream(comp.EmitToStream()).GetReference();
            CompileAndVerify(source2, references: new[] { metadataRef }, options: TestOptions.ReleaseModule, verify: Verification.Fails);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        [WorkItem(530879, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530879")]
        public void TestCompilationEmitUsesDifferentStreamsForBinaryAndPdb()
        {
            string p1 = @"public class C1 { }";

            var c1 = CreateCompilation(p1);
            var tmpDir = Temp.CreateDirectory();

            var dllPath = Path.Combine(tmpDir.Path, "assemblyname.dll");
            var pdbPath = Path.Combine(tmpDir.Path, "assemblyname.pdb");

            var result = c1.Emit(dllPath, pdbPath);

            Assert.True(result.Success, "Compilation failed");
            Assert.Empty(result.Diagnostics);

            Assert.True(File.Exists(dllPath), "DLL does not exist");
            Assert.True(File.Exists(pdbPath), "PDB does not exist");
        }

        [Fact, WorkItem(540777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540777"), WorkItem(546354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546354")]
        public void CS0219WRN_UnreferencedVarAssg_ConditionalOperator()
        {
            var text = @"
class Program
{
    static void Main(string[] args)
    {
        bool b;
        int s = (b = false) ? 5 : 100; // Warning
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (7,18): warning CS0665: Assignment in conditional expression is always constant; did you mean to use == instead of = ?
                //         int s = (b = false) ? 5 : 100; 		// Warning
                Diagnostic(ErrorCode.WRN_IncorrectBooleanAssg, "b = false"),
                // (6,14): warning CS0219: The variable 'b' is assigned but its value is never used
                //         bool b;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "b").WithArguments("b"));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_01()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var compRef = new CSharpCompilationReference(refCompilation);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";
            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.X86));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.X86));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.X86));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.X86));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            // Confirm that suppressing the old alink warning 1607 shuts off WRN_ConflictingMachineAssembly
            var warnings = new System.Collections.Generic.Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn), ReportDiagnostic.Suppress);
            useCompilation = useCompilation.WithOptions(useCompilation.Options.WithSpecificDiagnosticOptions(warnings));
            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_02()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";
            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // error CS8010: Agnostic assembly cannot have a processor specific module 'PlatformMismatch.netmodule'.
                Diagnostic(ErrorCode.ERR_AgnosticToMachineModule).WithArguments("PlatformMismatch.netmodule"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.X86));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // error CS8011: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
                Diagnostic(ErrorCode.ERR_ConflictingMachineModule).WithArguments("PlatformMismatch.netmodule"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu));

            // no CS8010 when building a module and adding a module that has a conflict.
            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_03()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.X86), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var compRef = new CSharpCompilationReference(refCompilation);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // warning CS8012: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
                Diagnostic(ErrorCode.WRN_ConflictingMachineAssembly).WithArguments("PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_04()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.X86), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions,
                // error CS8011: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
                Diagnostic(ErrorCode.ERR_ConflictingMachineModule).WithArguments("PlatformMismatch.netmodule"));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_05()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var compRef = new CSharpCompilationReference(refCompilation);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_06()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_07()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);
            var compRef = new CSharpCompilationReference(refCompilation);
            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { compRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);

            useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30169")]
        public void PlatformMismatch_08()
        {
            var emitOptions = new EmitOptions(runtimeMetadataVersion: "v1234");
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();

            string refSource = @"
public interface ITestPlatform
{}
";
            var refCompilation = CreateEmptyCompilation(refSource, parseOptions: parseOptions, options: TestOptions.ReleaseModule.WithPlatform(Platform.Itanium), assemblyName: "PlatformMismatch");

            refCompilation.VerifyEmitDiagnostics(emitOptions);

            var imageRef = refCompilation.EmitToImageReference();

            string useSource = @"
public interface IUsePlatform
{
    ITestPlatform M();
}
";

            var useCompilation = CreateEmptyCompilation(useSource,
                new MetadataReference[] { imageRef },
                parseOptions: parseOptions, options: TestOptions.ReleaseDll.WithPlatform(Platform.Itanium));

            useCompilation.VerifyEmitDiagnostics(emitOptions);
        }

        [Fact, WorkItem(769741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769741")]
        public void Bug769741()
        {
            var comp = CreateEmptyCompilation("", new[] { TestReferences.SymbolsTests.netModule.x64COFF }, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            // modules not supported in ref emit
            // PEVerify: [HRESULT 0x8007000B] - An attempt was made to load a program with an incorrect format.
            // ILVerify: Internal.IL.VerifierException : No system module specified
            CompileAndVerify(comp, verify: Verification.Fails);
            Assert.NotSame(comp.Assembly.CorLibrary, comp.Assembly);
            comp.GetSpecialType(SpecialType.System_Int32);
        }

        [Fact]
        public void FoldMethods()
        {
            string source = @"
class Viewable
{
    static void Main()
    {
        var v = new Viewable();
        var x = v.P1;
        var y = x && v.P2;
    }

    bool P1 { get { return true; } } 
    bool P2 { get { return true; } }
}
";
            var compilation = CreateCompilation(source, null, TestOptions.ReleaseDll);
            var peReader = ModuleMetadata.CreateFromStream(compilation.EmitToStream()).Module.GetMetadataReader();

            int P1RVA = 0;
            int P2RVA = 0;

            foreach (var handle in peReader.TypeDefinitions)
            {
                var typeDef = peReader.GetTypeDefinition(handle);

                if (peReader.StringComparer.Equals(typeDef.Name, "Viewable"))
                {
                    foreach (var m in typeDef.GetMethods())
                    {
                        var method = peReader.GetMethodDefinition(m);
                        if (peReader.StringComparer.Equals(method.Name, "get_P1"))
                        {
                            P1RVA = method.RelativeVirtualAddress;
                        }
                        if (peReader.StringComparer.Equals(method.Name, "get_P2"))
                        {
                            P2RVA = method.RelativeVirtualAddress;
                        }
                    }
                }
            }

            Assert.NotEqual(0, P1RVA);
            Assert.Equal(P2RVA, P1RVA);
        }

        private static bool SequenceMatches(byte[] buffer, int startIndex, byte[] pattern)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (buffer[startIndex + i] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int IndexOfPattern(byte[] buffer, int startIndex, byte[] pattern)
        {
            // Naive linear search for target within buffer
            int end = buffer.Length - pattern.Length;
            for (int i = startIndex; i < end; i++)
            {
                if (SequenceMatches(buffer, i, pattern))
                {
                    return i;
                }
            }

            return -1;
        }

        [Fact, WorkItem(1669, "https://github.com/dotnet/roslyn/issues/1669")]
        public void FoldMethods2()
        {
            // Verifies that IL folding eliminates duplicate copies of small method bodies by
            // examining the emitted binary.
            string source = @"
class C
{
    ulong M() => 0x8675309ABCDE4225UL; 
    long P => -8758040459200282075L;
}
";

            var compilation = CreateCompilation(source, null, TestOptions.ReleaseDll);
            using (var stream = compilation.EmitToStream())
            {
                var bytes = new byte[stream.Length];
                Assert.Equal(bytes.Length, stream.Read(bytes, 0, bytes.Length));

                // The constant should appear exactly once
                byte[] pattern = new byte[] { 0x25, 0x42, 0xDE, 0xBC, 0x9A, 0x30, 0x75, 0x86 };
                int firstMatch = IndexOfPattern(bytes, 0, pattern);
                Assert.True(firstMatch >= 0, "Couldn't find the expected byte pattern in the output.");
                int secondMatch = IndexOfPattern(bytes, firstMatch + 1, pattern);
                Assert.True(secondMatch < 0, "Expected to find just one occurrence of the pattern in the output.");
            }
        }

        [Fact]
        public void BrokenOutStream()
        {
            //These tests ensure that users supplying a broken stream implementation via the emit API 
            //get exceptions enabling them to attribute the failure to their code and to debug.
            string source = @"class Goo {}";
            var compilation = CreateCompilation(source);

            var output = new BrokenStream();

            output.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;
            var result = compilation.Emit(output);
            result.Diagnostics.Verify(
                // error CS8104: An error occurred while writing the Portable Executable file.
                Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(output.ThrownException.ToString()).WithLocation(1, 1));

            // Stream.Position is not called:
            output.BreakHow = BrokenStream.BreakHowType.ThrowOnSetPosition;
            result = compilation.Emit(output);
            result.Diagnostics.Verify();

            // disposed stream is not writable
            var outReal = new MemoryStream();
            outReal.Dispose();
            Assert.Throws<ArgumentException>(() => compilation.Emit(outReal));
        }

        [Fact]
        public void BrokenPortablePdbStream()
        {
            string source = @"class Goo {}";
            var compilation = CreateCompilation(source);

            using (new EnsureEnglishUICulture())
            using (var output = new MemoryStream())
            {
                var pdbStream = new BrokenStream();
                pdbStream.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;
                var result = compilation.Emit(output, pdbStream, options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'I/O error occurred.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("I/O error occurred.").WithLocation(1, 1)
                    );
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/23760")]
        public void BrokenPDBStream()
        {
            string source = @"class Goo {}";
            var compilation = CreateCompilation(source, null, TestOptions.DebugDll);

            var output = new MemoryStream();
            var pdb = new BrokenStream();
            pdb.BreakHow = BrokenStream.BreakHowType.ThrowOnSetLength;
            var result = compilation.Emit(output, pdbStream: pdb);

            // error CS0041: Unexpected error writing debug information -- 'Exception from HRESULT: 0x806D0004'
            var err = result.Diagnostics.Single();

            Assert.Equal((int)ErrorCode.FTL_DebugEmitFailure, err.Code);
            Assert.Equal(1, err.Arguments.Count);
            var ioExceptionMessage = new IOException().Message;
            Assert.Equal(ioExceptionMessage, (string)err.Arguments[0]);

            pdb.Dispose();
            result = compilation.Emit(output, pdbStream: pdb);

            // error CS0041: Unexpected error writing debug information -- 'Exception from HRESULT: 0x806D0004'
            err = result.Diagnostics.Single();

            Assert.Equal((int)ErrorCode.FTL_DebugEmitFailure, err.Code);
            Assert.Equal(1, err.Arguments.Count);
            Assert.Equal(ioExceptionMessage, (string)err.Arguments[0]);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NetModulesNeedDesktop)]
        public void MultipleNetmodulesWithPrivateImplementationDetails()
        {
            var s1 = @"
public class A
{
    private static char[] contents = { 'H', 'e', 'l', 'l', 'o', ',', ' ' };
    public static string M1()
    {
        return new string(contents);
    }
}";
            var s2 = @"
public class B : A
{
    private static char[] contents = { 'w', 'o', 'r', 'l', 'd', '!' };
    public static string M2()
    {
        return new string(contents);
    }
}";
            var s3 = @"
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.Write(A.M1());
        System.Console.WriteLine(B.M2());
    }
}";
            var comp1 = CreateCompilation(s1, options: TestOptions.ReleaseModule);
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(s2, options: TestOptions.ReleaseModule, references: new[] { ref1 });
            comp2.VerifyDiagnostics();
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(s3, options: TestOptions.ReleaseExe, references: new[] { ref1, ref2 });
            // Before the bug was fixed, the PrivateImplementationDetails classes clashed, resulting in the commented-out error below.
            comp3.VerifyDiagnostics(
                ////// error CS0101: The namespace '<global namespace>' already contains a definition for '<PrivateImplementationDetails>'
                ////Diagnostic(ErrorCode.ERR_DuplicateNameInNS).WithArguments("<PrivateImplementationDetails>", "<global namespace>").WithLocation(1, 1)
                );
            CompileAndVerify(comp3, expectedOutput: "Hello, world!");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NetModulesNeedDesktop)]
        public void MultipleNetmodulesWithAnonymousTypes()
        {
            var s1 = @"
public class A
{
    internal object o1 = new { hello = 1, world = 2 };
    public static string M1()
    {
        return ""Hello, "";
    }
}";
            var s2 = @"
public class B : A
{
    internal object o2 = new { hello = 1, world = 2 };
    public static string M2()
    {
        return ""world!"";
    }
}";
            var s3 = @"
public class Program
{
    public static void Main(string[] args)
    {
        System.Console.Write(A.M1());
        System.Console.WriteLine(B.M2());
    }
}";
            var comp1 = CreateCompilation(s1, options: TestOptions.ReleaseModule.WithModuleName("A"));
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();

            var comp2 = CreateCompilation(s2, options: TestOptions.ReleaseModule.WithModuleName("B"), references: new[] { ref1 });
            comp2.VerifyDiagnostics();
            var ref2 = comp2.EmitToImageReference();

            var comp3 = CreateCompilation(s3, options: TestOptions.ReleaseExe.WithModuleName("C"), references: new[] { ref1, ref2 });
            comp3.VerifyDiagnostics();
            CompileAndVerify(comp3, expectedOutput: "Hello, world!");
        }

        /// <summary>
        /// Ordering of anonymous type definitions
        /// in metadata should be deterministic.
        /// </summary>
        [Fact]
        public void AnonymousTypeMetadataOrder()
        {
            var source =
@"class C1
{
    object F = new { A = 1, B = 2 };
}
class C2
{
    object F = new { a = 3, b = 4 };
}
class C3
{
    object F = new { AB = 3 };
}
class C4
{
    object F = new { a = 1, B = 2 };
}
class C5
{
    object F = new { a = 1, B = 2 };
}
class C6
{
    object F = new { Ab = 5 };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.ReleaseDll);
            var bytes = compilation.EmitToArray();
            using (var metadata = ModuleMetadata.CreateFromImage(bytes))
            {
                var reader = metadata.MetadataReader;
                var actualNames = reader.GetTypeDefNames().Select(h => reader.GetString(h));
                var expectedNames = new[]
                    {
                        "<Module>",
                        "<>f__AnonymousType0`2",
                        "<>f__AnonymousType1`2",
                        "<>f__AnonymousType2`1",
                        "<>f__AnonymousType3`2",
                        "<>f__AnonymousType4`1",
                        "C1",
                        "C2",
                        "C3",
                        "C4",
                        "C5",
                        "C6",
                    };
                AssertEx.Equal(expectedNames, actualNames);
            }
        }

        /// <summary>
        /// Ordering of synthesized delegates in
        /// metadata should be deterministic.
        /// </summary>
        [WorkItem(1440, "https://github.com/dotnet/roslyn/issues/1440")]
        [Fact]
        public void SynthesizedDelegateMetadataOrder()
        {
            var source =
@"class C1
{
    static void M(dynamic d, object x, int y)
    {
        d(1, ref x, out y);
    }
}
class C2
{
    static object M(dynamic d, object o)
    {
        return d(o, ref o);
    }
}
class C3
{
    static void M(dynamic d, object o)
    {
        d(ref o);
    }
}
class C4
{
    static int M(dynamic d, object o)
    {
        return d(ref o, 2);
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.ReleaseDll, references: new[] { CSharpRef });
            var bytes = compilation.EmitToArray();
            using (var metadata = ModuleMetadata.CreateFromImage(bytes))
            {
                var reader = metadata.MetadataReader;
                var actualNames = reader.GetTypeDefNames().Select(h => reader.GetString(h));
                var expectedNames = new[]
                    {
                        "<Module>",
                        "<>A{00000040}`3",
                        "<>A{00001200}`5",
                        "<>F{00000040}`5",
                        "<>F{00000200}`5",
                        "C1",
                        "C2",
                        "C3",
                        "C4",
                        "<>o__0",
                        "<>o__0",
                        "<>o__0",
                        "<>o__0",
                    };
                AssertEx.Equal(expectedNames, actualNames);
            }
        }

        [Fact]
        [WorkItem(3240, "https://github.com/dotnet/roslyn/pull/8227")]
        public void FailingEmitter()
        {
            string source = @"
public class X
{
    public static void Main()
    {
  
    }
}";
            var compilation = CreateCompilation(source);
            var broken = new BrokenStream();
            broken.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;
            var result = compilation.Emit(broken);
            Assert.False(result.Success);
            result.Diagnostics.Verify(
                // error CS8104: An error occurred while writing the Portable Executable file.
                Diagnostic(ErrorCode.ERR_PeWritingFailure).WithArguments(broken.ThrownException.ToString()).WithLocation(1, 1));
        }

        [Fact]
        public void BadPdbStreamWithPortablePdbEmit()
        {
            var comp = CreateCompilation("class C {}");
            var broken = new BrokenStream();
            broken.BreakHow = BrokenStream.BreakHowType.ThrowOnWrite;
            using (new EnsureEnglishUICulture())
            using (var peStream = new MemoryStream())
            {
                var portablePdbOptions = EmitOptions.Default
                    .WithDebugInformationFormat(DebugInformationFormat.PortablePdb);

                var result = comp.Emit(peStream,
                    pdbStream: broken,
                    options: portablePdbOptions);

                Assert.False(result.Success);
                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'I/O error occurred.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("I/O error occurred.").WithLocation(1, 1));

                // Allow for cancellation
                broken = new BrokenStream();
                broken.BreakHow = BrokenStream.BreakHowType.CancelOnWrite;
                Assert.Throws<OperationCanceledException>(() => comp.Emit(peStream,
                    pdbStream: broken,
                    options: portablePdbOptions));
            }
        }

        [Fact]
        [WorkItem(9308, "https://github.com/dotnet/roslyn/issues/9308")]
        public void FailingEmitterAllowsCancellationExceptionsThrough()
        {
            string source = @"
public class X
{
    public static void Main()
    {
  
    }
}";
            var compilation = CreateCompilation(source);
            var broken = new BrokenStream();
            broken.BreakHow = BrokenStream.BreakHowType.CancelOnWrite;

            Assert.Throws<OperationCanceledException>(() => compilation.Emit(broken));
        }

        [Fact]
        [WorkItem(11691, "https://github.com/dotnet/roslyn/issues/11691")]
        public void ObsoleteAttributeOverride()
        {
            string source = @"
using System;
public abstract class BaseClass<T>
{
    public abstract int Method(T input);
}

public class DerivingClass<T> : BaseClass<T>
{
    [Obsolete(""Deprecated"")]
    public override void Method(T input)
    {
        throw new NotImplementedException();
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (11,26): warning CS0809: Obsolete member 'DerivingClass<T>.Method(T)' overrides non-obsolete member 'BaseClass<T>.Method(T)'
                //     public override void Method(T input)
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "Method").WithArguments("DerivingClass<T>.Method(T)", "BaseClass<T>.Method(T)").WithLocation(11, 26),
                // (11,26): error CS0508: 'DerivingClass<T>.Method(T)': return type must be 'int' to match overridden member 'BaseClass<T>.Method(T)'
                //     public override void Method(T input)
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "Method").WithArguments("DerivingClass<T>.Method(T)", "BaseClass<T>.Method(T)", "int").WithLocation(11, 26));
        }

        [Fact]
        public void CompileAndVerifyModuleIncludesAllModules()
        {
            // Before this change, CompileAndVerify() didn't include other modules when testing a PEModule.
            // Verify that symbols from other modules are accessible as well.

            var modRef = CreateCompilation("public class A { }", options: TestOptions.ReleaseModule, assemblyName: "refMod").EmitToImageReference();
            var comp = CreateCompilation("public class B : A { }", references: new[] { modRef }, assemblyName: "sourceMod");

            // ILVerify: Assembly or module not found: refMod
            CompileAndVerify(comp, verify: Verification.FailsILVerify, symbolValidator: module =>
            {
                var b = module.GlobalNamespace.GetTypeMember("B");
                Assert.Equal("B", b.Name);
                Assert.False(b.IsErrorType());
                Assert.Equal("sourceMod.dll", b.ContainingModule.Name);

                var a = b.BaseType();
                Assert.Equal("A", a.Name);
                Assert.False(a.IsErrorType());
                Assert.Equal("refMod.netmodule", a.ContainingModule.Name);
            });
        }

        [Fact]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void WarnAsErrorDoesNotEmit_GeneralDiagnosticOption()
        {
            var options = TestOptions.DebugDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            TestWarnAsErrorDoesNotEmitCore(options);
        }

        [Fact]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void WarnAsErrorDoesNotEmit_SpecificDiagnosticOption()
        {
            var options = TestOptions.DebugDll.WithSpecificDiagnosticOptions("CS0169", ReportDiagnostic.Error);
            TestWarnAsErrorDoesNotEmitCore(options);
        }

        private void TestWarnAsErrorDoesNotEmitCore(CSharpCompilationOptions options)
        {
            string source = @"
class X
{
    int _f;
}";
            var compilation = CreateCompilation(source, options: options);

            using var output = new MemoryStream();
            using var pdbStream = new MemoryStream();
            using var xmlDocumentationStream = new MemoryStream();
            using var win32ResourcesStream = compilation.CreateDefaultWin32Resources(versionResource: true, noManifest: false, manifestContents: null, iconInIcoFormat: null);

            var emitResult = compilation.Emit(output, pdbStream, xmlDocumentationStream, win32ResourcesStream);
            Assert.False(emitResult.Success);

            Assert.Equal(0, output.Length);
            Assert.Equal(0, pdbStream.Length);

            // https://github.com/dotnet/roslyn/issues/37996 tracks revisiting the below behavior.
            Assert.True(xmlDocumentationStream.Length > 0);

            emitResult.Diagnostics.Verify(
                // (4,9): error CS0169: The field 'X._f' is never used
                //     int _f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_f").WithArguments("X._f").WithLocation(4, 9).WithWarningAsError(true));
        }

        [Fact]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void WarnAsErrorWithMetadataOnlyImageDoesEmit_GeneralDiagnosticOption()
        {
            var options = TestOptions.DebugDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            TestWarnAsErrorWithMetadataOnlyImageDoesEmitCore(options);
        }

        [Fact]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void WarnAsErrorWithMetadataOnlyImageDoesEmit_SpecificDiagnosticOptions()
        {
            var options = TestOptions.DebugDll.WithSpecificDiagnosticOptions("CS0612", ReportDiagnostic.Error);
            TestWarnAsErrorWithMetadataOnlyImageDoesEmitCore(options);
        }

        private void TestWarnAsErrorWithMetadataOnlyImageDoesEmitCore(CSharpCompilationOptions options)
        {
            string source = @"
public class X
{
    public void M(Y y)
    {
    }
}

[System.Obsolete]
public class Y { }
";
            var compilation = CreateCompilation(source, options: options);

            using var output = new MemoryStream();
            var emitOptions = new EmitOptions(metadataOnly: true);

            var emitResult = compilation.Emit(output, options: emitOptions);
            Assert.True(emitResult.Success);

            Assert.True(output.Length > 0);

            emitResult.Diagnostics.Verify(
                // (4,19): error CS0612: 'Y' is obsolete
                //     public void M(Y y)
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "Y").WithArguments("Y").WithLocation(4, 19).WithWarningAsError(true));
        }
    }
}
