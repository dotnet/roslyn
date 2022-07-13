﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class FileModifierTests : CSharpTestBase
{
    [Fact]
    public void LangVersion()
    {
        var source = """
            file class C { }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
        comp.VerifyDiagnostics(
            // (1,12): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
            // file class C { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "C").WithArguments("file types", "11.0").WithLocation(1, 12));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Nested_01()
    {
        var source = """
            class Outer
            {
                file class C { }
            }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
        comp.VerifyDiagnostics(
            // (3,16): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "C").WithArguments("file types", "11.0").WithLocation(3, 16),
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));
    }

    [Fact]
    public void Nested_02()
    {
        var source = """
            file class Outer
            {
                class C { }
            }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
        comp.VerifyDiagnostics(
            // (1,12): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
            // file class Outer
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "Outer").WithArguments("file types", "11.0").WithLocation(1, 12));
        verify();

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
        verify();

        void verify()
        {
            var outer = comp.GetMember<NamedTypeSymbol>("Outer");
            Assert.Equal(Accessibility.Internal, outer.DeclaredAccessibility);
            Assert.True(((SourceMemberContainerTypeSymbol)outer).IsFile);

            var classC = comp.GetMember<NamedTypeSymbol>("Outer.C");
            Assert.Equal(Accessibility.Private, classC.DeclaredAccessibility);
            Assert.False(((SourceMemberContainerTypeSymbol)classC).IsFile);
        }
    }

    [Fact]
    public void Nested_03()
    {
        var source = """
            file class Outer
            {
                file class C { }
            }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
        comp.VerifyDiagnostics(
            // (1,12): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
            // file class Outer
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "Outer").WithArguments("file types", "11.0").WithLocation(1, 12),
            // (3,16): error CS8936: Feature 'file types' is not available in C# 10.0. Please use language version 11.0 or greater.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "C").WithArguments("file types", "11.0").WithLocation(3, 16),
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));
    }

    [Fact]
    public void Nested_04()
    {
        var source = """
            file class Outer
            {
                public class C { }
            }

            class D
            {
                void M(Outer.C c) { } // 1
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (8,10): error CS9051: File type 'Outer.C' cannot be used in a member signature in non-file type 'D'.
            //     void M(Outer.C c) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("Outer.C", "D").WithLocation(8, 10));
    }

    [Fact]
    public void Nested_05()
    {
        var source = """
            file class Outer
            {
                public class C
                {
                    void M1(Outer outer) { } // ok
                    void M2(C outer) { } // ok
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Nested_06()
    {
        var source = """
            class A1
            {
                internal class A2 { }
            }
            file class B : A1
            {
            }
            class C : B.A2 // ok: base type is bound as A1.A2
            {
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void SameFileUse_01()
    {
        var source = """
            using System;

            file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>F0__C", symbol.MetadataName);

        // The qualified name here is based on `SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes`.
        // We don't actually look up based on the file-encoded name of the type.
        // This is similar to how generic types work (lookup based on 'C<T>' instead of 'C`1').
        verifier.VerifyIL("C@<tree 0>.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "<>F0__C", "Program" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F0__C");
            Assert.Equal(new[] { "M", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void SameFileUse_02()
    {
        var source = """
            using System;

            file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { "", source }, expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>F1__C", symbol.MetadataName);

        // The qualified name here is based on `SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes`.
        // We don't actually look up based on the file-encoded name of the type.
        // This is similar to how generic types work (lookup based on 'C<T>' instead of 'C`1').
        verifier.VerifyIL("C@<tree 1>.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "<>F1__C", "Program" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F1__C");
            Assert.Equal(new[] { "M", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void FileEnum_01()
    {
        var source = """
            using System;

            file enum E
            {
                E1, E2
            }

            class Program
            {
                static void Main()
                {
                    Console.Write(E.E2);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "E2", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("E");
        Assert.Equal("<>F0__E", symbol.MetadataName);

        verifier.VerifyIL("Program.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""E""
  IL_0006:  call       ""void System.Console.Write(object)""
  IL_000b:  ret
}");

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "<>F0__E", "Program" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F0__E");
            Assert.Equal(new[] { "value__", "E1", "E2", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void FileEnum_02()
    {
        var source = """
            using System;

            file enum E
            {
                E1, E2
            }

            file class Attr : Attribute
            {
                public Attr(E e) { }
            }

            [Attr(E.E2)]
            class Program
            {
                static void Main()
                {
                    var data = typeof(Program).GetCustomAttributesData();
                    Console.Write(data[0].ConstructorArguments[0]);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "(<>F0__E)1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("E");
        Assert.Equal("<>F0__E", symbol.MetadataName);

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "<>F0__E", "<>F0__Attr", "Program" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F0__E");
            Assert.Equal(new[] { "value__", "E1", "E2", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void FileEnum_03()
    {
        var source = """
            using System;

            file enum E
            {
                E1, E2
            }

            class Attr : Attribute
            {
                public Attr(E e) { } // 1
            }

            [Attr(E.E2)]
            class Program
            {
                static void Main()
                {
                    var data = typeof(Program).GetCustomAttributesData();
                    Console.Write(data[0].ConstructorArguments[0]);
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (10,12): error CS9051: File type 'E' cannot be used in a member signature in non-file type 'Attr'.
            //     public Attr(E e) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Attr").WithArguments("E", "Attr").WithLocation(10, 12));
    }

    [Fact]
    public void FileEnum_04()
    {
        var source = """
            using System;

            file enum E
            {
                E1, E2
            }

            class Attr : Attribute
            {
                public Attr(object obj) { }
            }

            [Attr(E.E2)]
            class Program
            {
                static void Main()
                {
                    var data = typeof(Program).GetCustomAttributesData();
                    Console.Write(data[0].ConstructorArguments[0]);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "(<>F0__E)1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("E");
        Assert.Equal("<>F0__E", symbol.MetadataName);

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "<>F0__E", "Attr", "Program" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("<>F0__E");
            Assert.Equal(new[] { "value__", "E1", "E2", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void OtherFileUse()
    {
        var source1 = """
            using System;

            file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var source2 = """
            class Program
            {
                static void Main()
                {
                    C.M(); // 1
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (5,9): error CS0103: The name 'C' does not exist in the current context
            //         C.M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(5, 9));
    }

    [Fact]
    public void Generic_01()
    {
        var source = """
        using System;

        C<int>.M(1);

        file class C<T>
        {
            public static void M(T t) { Console.Write(t); }
        }
        """;

        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/MyFile.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        verifier.VerifyIL("C<T>@MyFile.M(T)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        ""T""
  IL_0006:  call       ""void System.Console.Write(object)""
  IL_000b:  ret
}
");

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("<MyFile>F0__C`1", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "<MyFile>F0__C" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));

            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("<MyFile>F0__C");
            Assert.Equal("<MyFile>F0__C`1", classC.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact]
    public void BadFileNames_01()
    {
        var source = """
        using System;

        C.M();

        file class C
        {
            public static void M() { Console.Write(1); }
        }
        """;

        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/My<>File.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@My__File", c.ToTestDisplayString());
        Assert.Equal("<My__File>F0__C", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "<My__File>F0__C" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("<My__File>F0__C");
            Assert.Equal("<My__File>F0__C", expectedSymbol.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, expectedSymbol.MemberNames);
        }
    }

    [Fact]
    public void BadFileNames_02()
    {
        var source = """
        using System;

        C.M();

        file class C
        {
            public static void M() { Console.Write(1); }
        }
        """;

        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/MyGeneratedFile.g.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@MyGeneratedFile_g", c.ToTestDisplayString());
        Assert.Equal("<MyGeneratedFile_g>F0__C", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "<MyGeneratedFile_g>F0__C" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("<MyGeneratedFile_g>F0__C");
            Assert.Equal("<MyGeneratedFile_g>F0__C", expectedSymbol.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, expectedSymbol.MemberNames);
        }
    }

    [Fact]
    public void DuplicateFileNames_01()
    {
        var path = "path/to/file.cs";
        var source1 = SyntaxFactory.ParseSyntaxTree("""
            using System;

            C.M();

            file class C
            {
                public static void M() { Console.Write(1); }
            }
            """, options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);
        var source2 = SyntaxFactory.ParseSyntaxTree("""
            using System;

            file class C
            {
                public static void M() { Console.Write(2); }
            }
            """, options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);

        var verifier = CompileAndVerify(new[] { source1, source2 }, expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        // note that VerifyIL doesn't work in this specific scenario because the files have the same name.

        void symbolValidator(ModuleSymbol module)
        {
            Assert.NotNull(module.GlobalNamespace.GetMember("<file>F0__C"));
            Assert.NotNull(module.GlobalNamespace.GetMember("<file>F1__C"));
        }
    }

    // Data based on Lexer.ScanIdentifier_FastPath, excluding '/', '\', and ':' because those are path separators.
    [Theory]
    [InlineData('&')]
    [InlineData('\0')]
    [InlineData(' ')]
    [InlineData('\r')]
    [InlineData('\n')]
    [InlineData('\t')]
    [InlineData('!')]
    [InlineData('%')]
    [InlineData('(')]
    [InlineData(')')]
    [InlineData('*')]
    [InlineData('+')]
    [InlineData(',')]
    [InlineData('-')]
    [InlineData('.')]
    [InlineData(';')]
    [InlineData('<')]
    [InlineData('=')]
    [InlineData('>')]
    [InlineData('?')]
    [InlineData('[')]
    [InlineData(']')]
    [InlineData('^')]
    [InlineData('{')]
    [InlineData('|')]
    [InlineData('}')]
    [InlineData('~')]
    [InlineData('"')]
    [InlineData('\'')]
    [InlineData('`')]
    public void BadFileNames_03(char badChar)
    {
        var source = """
        using System;

        C.M();

        file class C
        {
            public static void M() { Console.Write(1); }
        }
        """;

        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: $"path/to/My{badChar}File.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@My_File", c.ToTestDisplayString());
        Assert.Equal("<My_File>F0__C", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "<My_File>F0__C" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("<My_File>F0__C");
            Assert.Equal("<My_File>F0__C", expectedSymbol.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, expectedSymbol.MemberNames);
        }
    }

    [Fact]
    public void Pdb_01()
    {
        var source = """
        using System;

        C.M();

        file class C
        {
            public static void M() { Console.Write(1); }
        }
        """;

        var expectedMetadataName = "<My_File>F0__C";
        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/My+File.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: validateSymbols);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@My_File", c.ToTestDisplayString());
        Assert.Equal(expectedMetadataName, c.MetadataName);

        void validateSymbols(ModuleSymbol module)
        {
            var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>(expectedMetadataName);
            Assert.NotNull(type);
            Assert.Equal(new[] { "M", ".ctor" }, type.MemberNames);
        }
    }

    [Theory]
    [InlineData("file", "file", "<>F0__C", "<>F1__C")]
    [InlineData("file", "", "<>F0__C", "C")]
    [InlineData("", "file", "C", "<>F1__C")]
    public void Duplication_01(string firstFileModifier, string secondFileModifier, string firstMetadataName, string secondMetadataName)
    {
        // A file type is allowed to have the same name as a non-file type from a different file.
        // When both a file type and non-file type with the same name are in scope, the file type is preferred, since it's "more local".
        var source1 = $$"""
            using System;

            {{firstFileModifier}} class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var source2 = $$"""
            using System;

            {{secondFileModifier}} class C
            {
                public static void M()
                {
                    Console.Write(2);
                }
            }
            """;

        var main = """

            class Program
            {
                static void Main()
                {
                    C.M();
                    global::C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1 + main, source2 }, expectedOutput: "11");
        var comp = (CSharpCompilation)verifier.Compilation;
        var cs = comp.GetMembers("C");
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        Assert.Equal(firstMetadataName, expectedSymbol.MetadataName);
        verify();

        verifier = CompileAndVerify(new[] { source1, source2 + main }, expectedOutput: "22");
        comp = (CSharpCompilation)verifier.Compilation;
        cs = comp.GetMembers("C");
        tree = comp.SyntaxTrees[1];
        expectedSymbol = cs[1];
        Assert.Equal(secondMetadataName, expectedSymbol.MetadataName);
        verify();

        void verify()
        {
            verifier.VerifyDiagnostics();
            Assert.Equal(2, cs.Length);
            Assert.Equal(comp.SyntaxTrees[0], cs[0].DeclaringSyntaxReferences.Single().SyntaxTree);
            Assert.Equal(comp.SyntaxTrees[1], cs[1].DeclaringSyntaxReferences.Single().SyntaxTree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
            var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last().Expression;
            var info = model.GetTypeInfo(cReference);
            Assert.Equal(expectedSymbol.GetPublicSymbol(), info.Type);
        }
    }

    [Fact]
    public void Duplication_02()
    {
        // As a sanity check, demonstrate that non-file classes with the same name across different files are disallowed.
        var source1 = """
            using System;

            class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var source2 = """
            using System;

            class C
            {
                public static void M()
                {
                    Console.Write(2);
                }
            }
            """;

        var main = """

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1 + main, source2 });
        verify();

        comp = CreateCompilation(new[] { source1, source2 + main });
        verify();

        void verify()
        {
            comp.VerifyDiagnostics(
                // (3,7): error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
                // class C
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>").WithLocation(3, 7),
                // (5,24): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                //     public static void M()
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(5, 24),
                // (14,11): error CS0121: The call is ambiguous between the following methods or properties: 'C.M()' and 'C.M()'
                //         C.M();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M()", "C.M()").WithLocation(14, 11));

            var cs = comp.GetMember("C");
            var syntaxReferences = cs.DeclaringSyntaxReferences;
            Assert.Equal(2, syntaxReferences.Length);
            Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
            Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);
        }
    }

    [Fact]
    public void Duplication_03()
    {
        var source1 = """
            using System;

            partial class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var source2 = """
            partial class C
            {
            }
            """;

        var main = """
            using System;

            file class C
            {
                public static void M()
                {
                    Console.Write(2);
                }
            }

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, source2, main }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFile: false });

        var syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[2], c1.DeclaringSyntaxReferences.Single().SyntaxTree);

        var tree = comp.SyntaxTrees[2];
        var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
        var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last().Expression;
        var info = model.GetTypeInfo(cReference);
        Assert.Equal(c1.GetPublicSymbol(), info.Type);
    }

    [Fact]
    public void Duplication_04()
    {
        var source1 = """
            using System;

            class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var main = """
            using System;

            file partial class C
            {
                public static void M()
                {
                    Console.Write(Number);
                }
            }

            file partial class C
            {
                private static int Number => 2;
            }

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, main }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFile: false });
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });

        var syntaxReferences = c1.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        var tree = comp.SyntaxTrees[1];
        var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
        var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last().Expression;
        var info = model.GetTypeInfo(cReference);
        Assert.Equal(c1.GetPublicSymbol(), info.Type);
    }

    [Theory]
    [CombinatorialData]
    public void Duplication_05(bool firstClassIsFile)
    {
        var source1 = $$"""
            using System;

            {{(firstClassIsFile ? "file " : "")}}partial class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var main = """
            using System;

            file partial class C
            {
                public static void M()
                {
                    Console.Write(2);
                }
            }

            class Program
            {
                static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, main }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.Equal(firstClassIsFile, ((SourceMemberContainerTypeSymbol)c0).IsFile);
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[1], c1.DeclaringSyntaxReferences.Single().SyntaxTree);

        var tree = comp.SyntaxTrees[1];
        var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
        var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last().Expression;
        var info = model.GetTypeInfo(cReference);
        Assert.Equal(c1.GetPublicSymbol(), info.Type);
    }

    [Fact]
    public void Duplication_06()
    {
        var source1 = """
            using System;

            partial class C
            {
                public static void M()
                {
                    Console.Write(Number);
                }
            }
            """;

        var source2 = """
            using System;

            partial class C
            {
                private static int Number => 1;
            }

            file class C
            {
                public static void M()
                {
                    Console.Write(2);
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        // https://github.com/dotnet/roslyn/issues/62333: should this diagnostic be more specific?
        // the issue more precisely is that a definition for 'C' already exists in the current file--not that it's already in this namespace.
        comp.VerifyDiagnostics(
            // (8,12): error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
            // file class C
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>").WithLocation(8, 12));

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFile: false });
        var syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[1], c1.DeclaringSyntaxReferences.Single().SyntaxTree);


        comp = CreateCompilation(new[] { source2, source1 });
        comp.VerifyDiagnostics(
            // (5,24): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
            //     public static void M()
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(5, 24),
            // (8,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(8, 12));

        var c = comp.GetMember("C");
        Assert.True(c is SourceMemberContainerTypeSymbol { IsFile: true });
        syntaxReferences = c.DeclaringSyntaxReferences;
        Assert.Equal(3, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[1].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[2].SyntaxTree);
    }

    [Fact]
    public void Duplication_07()
    {
        var source1 = """
            using System;

            file partial class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var source2 = """
            using System;

            file partial class C
            {
                public static void M()
                {
                    Console.Write(Number);
                }
            }

            file class C
            {
                private static int Number => 2;
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (11,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(11, 12));

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        var syntaxReferences = c1.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);


        comp = CreateCompilation(new[] { source2, source1 });
        comp.VerifyDiagnostics(
            // (11,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(11, 12));

        cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFile: true });
        syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[1].SyntaxTree);

        c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[1], c1.DeclaringSyntaxReferences.Single().SyntaxTree);
    }

    [Fact]
    public void Duplication_08()
    {
        var source1 = """
            partial class Outer
            {
                file class C
                {
                    public static void M() { }
                }
            }
            """;

        var source2 = """
            partial class Outer
            {
                file class C
                {
                    public static void M() { }
                }
            }
            """;

        var source3 = """
            partial class Outer
            {
                public class C
                {
                    public static void M() { }
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source1, source2, source3 });
        compilation.VerifyDiagnostics(
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16),
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        var classOuter = compilation.GetMember<NamedTypeSymbol>("Outer");
        var cs = classOuter.GetMembers("C");
        Assert.Equal(3, cs.Length);
        Assert.True(cs[0] is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.True(cs[1] is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.True(cs[2] is SourceMemberContainerTypeSymbol { IsFile: false });
    }

    [Fact]
    public void Duplication_09()
    {
        var source1 = """
            namespace NS
            {
                file class C
                {
                    public static void M() { }
                }
            }
            """;

        var source2 = """
            namespace NS
            {
                file class C
                {
                    public static void M() { }
                }
            }
            """;

        var source3 = """
            namespace NS
            {
                public class C
                {
                    public static void M() { }
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source1, source2, source3 });
        compilation.VerifyDiagnostics();

        var namespaceNS = compilation.GetMember<NamespaceSymbol>("NS");
        var cs = namespaceNS.GetMembers("C");
        Assert.Equal(3, cs.Length);
        Assert.True(cs[0] is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.True(cs[1] is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.True(cs[2] is SourceMemberContainerTypeSymbol { IsFile: false });
    }

    [Theory]
    [InlineData("file", "file")]
    [InlineData("file", "")]
    [InlineData("", "file")]
    public void Duplication_10(string firstFileModifier, string secondFileModifier)
    {
        var source1 = $$"""
            using System;

            partial class Program
            {
                {{firstFileModifier}} class C
                {
                    public static void M()
                    {
                        Console.Write(1);
                    }
                }
            }
            """;

        var source2 = $$"""
            using System;

            partial class Program
            {
                {{secondFileModifier}} class C
                {
                    public static void M()
                    {
                        Console.Write(2);
                    }
                }
            }
            """;

        var main = """
            partial class Program
            {
                static void Main()
                {
                    Program.C.M();
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1 + main, source2 });
        var cs = comp.GetMembers("Program.C");
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        verify();

        comp = CreateCompilation(new[] { source1, source2 + main });
        cs = comp.GetMembers("Program.C");
        tree = comp.SyntaxTrees[1];
        expectedSymbol = cs[1];
        verify();

        void verify()
        {
            comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_FileTypeNested).Verify();
            Assert.Equal(2, cs.Length);
            Assert.Equal(comp.SyntaxTrees[0], cs[0].DeclaringSyntaxReferences.Single().SyntaxTree);
            Assert.Equal(comp.SyntaxTrees[1], cs[1].DeclaringSyntaxReferences.Single().SyntaxTree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
            var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last();
            var info = model.GetTypeInfo(cReference);
            Assert.Equal(expectedSymbol.GetPublicSymbol(), info.Type);
        }
    }

    [Theory]
    [InlineData("file", "file")]
    [InlineData("file", "")]
    [InlineData("", "file")]
    public void Duplication_11(string firstFileModifier, string secondFileModifier)
    {
        var source1 = $$"""
            using System;

            {{firstFileModifier}} partial class Outer
            {
                internal class C
                {
                    public static void M()
                    {
                        Console.Write(1);
                    }
                }
            }
            """;

        var source2 = $$"""
            using System;

            {{secondFileModifier}} partial class Outer
            {
                internal class C
                {
                    public static void M()
                    {
                        Console.Write(2);
                    }
                }
            }
            """;

        var main = """
            class Program
            {
                static void Main()
                {
                    Outer.C.M();
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1 + main, source2 }, options: TestOptions.DebugExe);
        comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_FileTypeNested).Verify();
        var outers = comp.GetMembers("Outer");
        var cs = outers.Select(o => ((NamedTypeSymbol)o).GetMember("C")).ToArray();
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        verify();

        comp = CreateCompilation(new[] { source1, source2 + main }, options: TestOptions.DebugExe);
        comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_FileTypeNested).Verify();
        outers = comp.GetMembers("Outer");
        cs = outers.Select(o => ((NamedTypeSymbol)o).GetMember("C")).ToArray();
        tree = comp.SyntaxTrees[1];
        expectedSymbol = cs[1];
        verify();

        void verify()
        {
            Assert.Equal(2, cs.Length);
            Assert.Equal(comp.SyntaxTrees[0], cs[0].DeclaringSyntaxReferences.Single().SyntaxTree);
            Assert.Equal(comp.SyntaxTrees[1], cs[1].DeclaringSyntaxReferences.Single().SyntaxTree);

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);
            var cReference = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Last();
            var info = model.GetTypeInfo(cReference);
            Assert.Equal(expectedSymbol.GetPublicSymbol(), info.Type);
        }
    }

    [Theory]
    [InlineData("file ")]
    [InlineData("")]
    public void Duplication_13(string fileModifier)
    {
        var userCode = """
            using System;

            UserCode.Print();

            partial class UserCode
            {
                public static partial void Print();

                private class C
                {
                    public static void M() => Console.Write("Program.cs");
                }
            }
            """;

        // A source generator must assume that partial classes and namespaces may bring user-defined types into scope.
        // Therefore, generators should reference types they introduce with a `global::`-qualified name.
        var generatedCode = $$"""
            using System;

            partial class UserCode
            {
                public static partial void Print()
                {
                    global::C.M(); // binds to 'class C'/'file class C' from global namespace
                    C.M(); // binds to class 'UserCode.C'
                }
            }

            {{fileModifier}}class C
            {
                public static void M() => Console.Write("OtherFile.cs");
            }
            """;

        var verifier = CompileAndVerify(new[] { userCode, generatedCode }, expectedOutput: "OtherFile.csProgram.cs");
        verifier.VerifyDiagnostics();
    }

    [Theory]
    [InlineData("file ")]
    [InlineData("")]
    public void Duplication_14(string fileModifier)
    {
        var userCode = """
            using System;
            using UserNamespace;

            GeneratedClass.Print();

            namespace UserNamespace
            {
                class C
                {
                    public static void M() => Console.Write("Program.cs");
                }
            }
            """;

        // A source generator must assume that partial classes and namespaces may bring user-defined types into scope.
        // Therefore, generators should reference types they introduce with a `global::`-qualified name.
        var generatedCode = $$"""
            using System;

            namespace UserNamespace
            {
                class GeneratedClass
                {
                    public static void Print()
                    {
                        global::C.M(); // binds to 'class C'/'file class C' from global namespace
                        C.M(); // binds to class 'UserNamespace.C'
                    }
                }
            }

            {{fileModifier}}class C
            {
                public static void M() => Console.Write("OtherFile.cs");
            }
            """;

        var verifier = CompileAndVerify(new[] { userCode, generatedCode }, expectedOutput: "OtherFile.csProgram.cs");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void Duplication_15()
    {
        var userCode = """
            using System;
            using UserNamespace;

            GeneratedClass.Print();

            namespace UserNamespace
            {
                class C
                {
                    public static void M() => Console.Write("Program.cs");
                }
            }
            """;

        // Generators can also mitigate the "nearer scope" problem by ensuring no namespace or partial class scopes lie between the declaration and usage of a file type.
        var generatedCode = $$"""
            using System;

            namespace UserNamespace
            {
                class GeneratedClass
                {
                    public static void Print()
                    {
                        C.M(); // binds to 'UserNamespace.C@OtherFile'
                    }
                }

                file class C
                {
                    public static void M() => Console.Write("OtherFile.cs");
                }
            }

            """;

        var verifier = CompileAndVerify(new[] { userCode, generatedCode }, expectedOutput: "OtherFile.cs");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void SignatureUsage_01()
    {
        var source = """
            file class C
            {
            }

            class D
            {
                public void M1(C c) { } // 1
                private void M2(C c) { } // 2
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,17): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public void M1(C c) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 17),
            // (8,18): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     private void M2(C c) { } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("C", "D").WithLocation(8, 18));
    }

    [Fact]
    public void SignatureUsage_02()
    {
        var source = """
            file class C
            {
            }

            class D
            {
                public C M1() => new C(); // 1
                private C M2() => new C(); // 2
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,14): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M1() => new C(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 14),
            // (8,15): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     private C M2() => new C(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("C", "D").WithLocation(8, 15));
    }

    [Fact]
    public void SignatureUsage_03()
    {
        var source = """
            file class C
            {
            }
            file delegate void D();

            public class E
            {
                C field; // 1
                C property { get; set; } // 2
                object this[C c] { get => c; set { } } // 3
                event D @event; // 4
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (8,7): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     C field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C", "E").WithLocation(8, 7),
            // (8,7): warning CS0169: The field 'E.field' is never used
            //     C field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(8, 7),
            // (9,7): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     C property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C", "E").WithLocation(9, 7),
            // (10,12): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     object this[C c] { get => c; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C", "E").WithLocation(10, 12),
            // (11,13): error CS9051: File type 'D' cannot be used in a member signature in non-file type 'E'.
            //     event D @event; // 4
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "@event").WithArguments("D", "E").WithLocation(11, 13),
            // (11,13): warning CS0067: The event 'E.event' is never used
            //     event D @event; // 4
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "@event").WithArguments("E.event").WithLocation(11, 13));
    }

    [Fact]
    public void SignatureUsage_04()
    {
        var source = """
            file class C
            {
                public class Inner { }
                public delegate void InnerDelegate();
            }

            public class E
            {
                C.Inner field; // 1
                C.Inner property { get; set; } // 2
                object this[C.Inner inner] { get => inner; set { } } // 3
                event C.InnerDelegate @event; // 4
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (9,13): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E").WithLocation(9, 13),
            // (9,13): warning CS0169: The field 'E.field' is never used
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(9, 13),
            // (10,13): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E").WithLocation(10, 13),
            // (11,12): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E").WithLocation(11, 12),
            // (12,27): error CS9051: File type 'C.InnerDelegate' cannot be used in a member signature in non-file type 'E'.
            //     event C.InnerDelegate @event; // 4
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "@event").WithArguments("C.InnerDelegate", "E").WithLocation(12, 27),
            // (12,27): warning CS0067: The event 'E.event' is never used
            //     event C.InnerDelegate @event; // 4
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "@event").WithArguments("E.event").WithLocation(12, 27));
    }

    [Fact]
    public void SignatureUsage_05()
    {
        var source = """
            #pragma warning disable 67, 169 // unused event, field

            file class C
            {
                public class Inner { }
                public delegate void InnerDelegate();
            }

            file class D
            {
                public class Inner
                {
                    C.Inner field;
                    C.Inner property { get; set; }
                    object this[C.Inner inner] { get => inner; set { } }
                    event C.InnerDelegate @event;
                }
            }

            class E
            {
                public class Inner
                {
                    C.Inner field; // 1
                    C.Inner property { get; set; } // 2
                    object this[C.Inner inner] { get => inner; set { } } // 3
                    event C.InnerDelegate @event; // 4
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (24,17): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E.Inner").WithLocation(24, 17),
            // (25,17): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E.Inner").WithLocation(25, 17),
            // (26,16): error CS9051: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E.Inner").WithLocation(26, 16),
            // (27,31): error CS9051: File type 'C.InnerDelegate' cannot be used in a member signature in non-file type 'E.Inner'.
            //         event C.InnerDelegate @event; // 4
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "@event").WithArguments("C.InnerDelegate", "E.Inner").WithLocation(27, 31));
    }

    [Fact]
    public void SignatureUsage_06()
    {
        var source = """
            file class C
            {
            }

            delegate void Del1(C c); // 1
            delegate C Del2(); // 2
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'Del1'.
            // delegate void Del1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del1").WithArguments("C", "Del1").WithLocation(5, 15),
            // (6,12): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'Del2'.
            // delegate C Del2(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del2").WithArguments("C", "Del2").WithLocation(6, 12));
    }

    [Fact]
    public void SignatureUsage_07()
    {
        var source = """
            file class C
            {
            }

            class D
            {
                public static D operator +(D d, C c) => d; // 1
                public static C operator -(D d1, D d2) => new C(); // 2
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,30): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public static D operator +(D d, C c) => d; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("C", "D").WithLocation(7, 30),
            // (8,30): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public static C operator -(D d1, D d2) => new C(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "-").WithArguments("C", "D").WithLocation(8, 30));
    }

    [Fact]
    public void SignatureUsage_08()
    {
        var source = """
            file class C
            {
            }

            class D
            {
                public D(C c) { } // 1
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,12): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public D(C c) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "D").WithArguments("C", "D").WithLocation(7, 12));
    }

    [Fact]
    public void SignatureUsage_09()
    {
        var source = """
            file class C
            {
            }

            class D
            {
                public C M(C c1, C c2) => c1; // 1, 2, 3
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,14): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14));
    }

    [Fact]
    public void AccessModifiers_01()
    {
        var source = """
            public file class C { } // 1
            file internal class D { } // 2
            private file class E { } // 3, 4
            file class F { } // ok
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (1,19): error CS9052: File type 'C' cannot use accessibility modifiers.
            // public file class C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "C").WithArguments("C").WithLocation(1, 19),
            // (2,21): error CS9052: File type 'D' cannot use accessibility modifiers.
            // file internal class D { } // 2
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "D").WithArguments("D").WithLocation(2, 21),
            // (3,20): error CS9052: File type 'E' cannot use accessibility modifiers.
            // private file class E { } // 3, 4
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "E").WithArguments("E").WithLocation(3, 20),
            // (3,20): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // private file class E { } // 3, 4
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "E").WithLocation(3, 20));
    }

    [Fact]
    public void DuplicateModifiers_01()
    {
        var source = """
            file file class C { } // 1
            file readonly file struct D { } // 2
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (1,6): error CS1004: Duplicate 'file' modifier
            // file file class C { } // 1
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "file").WithArguments("file").WithLocation(1, 6),
            // (2,15): error CS1004: Duplicate 'file' modifier
            // file readonly file struct D { } // 2
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "file").WithArguments("file").WithLocation(2, 15));
    }

    [Fact]
    public void BaseClause_01()
    {
        var source = """
            file class Base { }
            class Derived1 : Base { } // 1
            public class Derived2 : Base { } // 2, 3
            file class Derived3 : Base { } // ok
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (2,7): error CS9053: File type 'Base' cannot be used as a base type of non-file type 'Derived1'.
            // class Derived1 : Base { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived1").WithArguments("Base", "Derived1").WithLocation(2, 7),
            // (3,14): error CS0060: Inconsistent accessibility: base class 'Base' is less accessible than class 'Derived2'
            // public class Derived2 : Base { } // 2, 3
            Diagnostic(ErrorCode.ERR_BadVisBaseClass, "Derived2").WithArguments("Derived2", "Base").WithLocation(3, 14),
            // (3,14): error CS9053: File type 'Base' cannot be used as a base type of non-file type 'Derived2'.
            // public class Derived2 : Base { } // 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived2").WithArguments("Base", "Derived2").WithLocation(3, 14));
    }

    [Fact]
    public void BaseClause_02()
    {
        var source = """
            file interface Interface { }

            class Derived1 : Interface { } // ok
            file class Derived2 : Interface { } // ok

            interface Derived3 : Interface { } // 1
            file interface Derived4 : Interface { } // ok
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (6,11): error CS9053: File type 'Interface' cannot be used as a base type of non-file type 'Derived3'.
            // interface Derived3 : Interface { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived3").WithArguments("Interface", "Derived3").WithLocation(6, 11));
    }

    [Fact]
    public void BaseClause_03()
    {
        var source1 = """
            using System;
            class Base
            {
                public static void M0()
                {
                    Console.Write(1);
                }
            }
            """;
        var source2 = """
            using System;

            file class Base
            {
                public static void M0()
                {
                    Console.Write(2);
                }
            }
            file class Program : Base
            {
                static void Main()
                {
                    M0();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, source2 }, expectedOutput: "2");
        verifier.VerifyDiagnostics();
        var comp = (CSharpCompilation)verifier.Compilation;

        var tree = comp.SyntaxTrees[1];
        var model = comp.GetSemanticModel(tree);

        var fileClassBase = (NamedTypeSymbol)comp.GetMembers("Base")[1];
        var expectedSymbol = fileClassBase.GetMember("M0");

        var node = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Last();
        var symbolInfo = model.GetSymbolInfo(node.Expression);
        Assert.Equal(expectedSymbol.GetPublicSymbol(), symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
    }

    [Fact]
    public void BaseClause_04()
    {
        var source1 = """
            using System;
            class Base
            {
                public static void M0()
                {
                    Console.Write(1);
                }
            }
            """;
        var source2 = """
            file class Program : Base
            {
                static void Main()
                {
                    M0();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, source2 }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
        var comp = (CSharpCompilation)verifier.Compilation;

        var tree = comp.SyntaxTrees[1];
        var model = comp.GetSemanticModel(tree);

        var expectedSymbol = comp.GetMember("Base.M0");

        var node = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Last();
        var symbolInfo = model.GetSymbolInfo(node.Expression);
        Assert.Equal(expectedSymbol.GetPublicSymbol(), symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
    }

    [Fact]
    public void BaseClause_05()
    {
        var source = """
            interface I2 { }
            file interface I1 { }
            partial interface Derived : I1 { } // 1
            partial interface Derived : I2 { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,19): error CS9053: File type 'I1' cannot be used as a base type of non-file type 'Derived'.
            // partial interface Derived : I1 { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived").WithArguments("I1", "Derived").WithLocation(3, 19));
    }

    [Fact]
    public void InterfaceImplementation_01()
    {
        var source = """
            file interface I
            {
                void F();
            }
            class C : I
            {
                public void F() { }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InterfaceImplementation_02()
    {
        var source = """
            file interface I
            {
                void F(I i);
            }
            class C : I
            {
                public void F(I i) { } // 1
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,17): error CS9051: File type 'I' cannot be used in a member signature in non-file type 'C'.
            //     public void F(I i) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "F").WithArguments("I", "C").WithLocation(7, 17));
    }

    [Fact]
    public void InterfaceImplementation_03()
    {
        var source = """
            file interface I
            {
                void F(I i);
            }
            class C : I
            {
                void I.F(I i) { }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,12): error CS9051: File type 'I' cannot be used in a member signature in non-file type 'C'.
            //     void I.F(I i) { }
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "F").WithArguments("I", "C").WithLocation(7, 12));
    }

    [Fact]
    public void InterfaceImplementation_04()
    {
        var source1 = """
            file interface I
            {
                void F();
            }
            partial class C : I
            {
            }
            """;

        var source2 = """
            partial class C
            {
                public void F() { }
            }
            """;

        // This is similar to how a base class may not have access to an interface (by being from another assembly, etc.),
        // but a derived class might add that interface to its list, and a base member implicitly implements an interface member.
        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InterfaceImplementation_05()
    {
        var source1 = """
            file interface I
            {
                void F();
            }
            partial class C : I // 1
            {
            }
            """;

        var source2 = """
            partial class C
            {
                void I.F() { } // 2, 3
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (3,10): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
            //     void I.F() { } // 2, 3
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(3, 10),
            // (3,10): error CS0538: 'I' in explicit interface declaration is not an interface
            //     void I.F() { } // 2, 3
            Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(3, 10),
            // (5,19): error CS0535: 'C' does not implement interface member 'I.F()'
            // partial class C : I // 1
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", "I.F()").WithLocation(5, 19));
    }

    [Fact]
    public void TypeArguments_01()
    {
        var source = """
            file struct S { public int X; }
            class Container<T> { }
            unsafe class Program
            {
                Container<S> M1() => new Container<S>(); // 1
                S[] M2() => new S[0]; // 2
                (S, S) M3() => (new S(), new S()); // 3
                S* M4() => null; // 4
                delegate*<S, void> M5() => null; // 5
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
        comp.VerifyDiagnostics(
                // (1,28): warning CS0649: Field 'S.X' is never assigned to, and will always have its default value 0
                // file struct S { public int X; }
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "X").WithArguments("S.X", "0").WithLocation(1, 28),
                // (5,18): error CS9051: File type 'Container<S>' cannot be used in a member signature in non-file type 'Program'.
                //     Container<S> M1() => new Container<S>(); // 1
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("Container<S>", "Program").WithLocation(5, 18),
                // (6,9): error CS9051: File type 'S[]' cannot be used in a member signature in non-file type 'Program'.
                //     S[] M2() => new S[0]; // 2
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("S[]", "Program").WithLocation(6, 9),
                // (7,12): error CS9051: File type '(S, S)' cannot be used in a member signature in non-file type 'Program'.
                //     (S, S) M3() => (new S(), new S()); // 3
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M3").WithArguments("(S, S)", "Program").WithLocation(7, 12),
                // (8,8): error CS9051: File type 'S*' cannot be used in a member signature in non-file type 'Program'.
                //     S* M4() => null; // 4
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M4").WithArguments("S*", "Program").WithLocation(8, 8),
                // (9,24): error CS9051: File type 'delegate*<S, void>' cannot be used in a member signature in non-file type 'Program'.
                //     delegate*<S, void> M5() => null; // 5
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M5").WithArguments("delegate*<S, void>", "Program").WithLocation(9, 24));
    }

    [Fact]
    public void Constraints_01()
    {
        var source = """
            file class C { }

            file class D
            {
                void M<T>(T t) where T : C { } // ok
            }

            class E
            {
                void M<T>(T t) where T : C { } // 1
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (10,30): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'E.M<T>(T)'.
            //     void M<T>(T t) where T : C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C").WithArguments("C", "E.M<T>(T)").WithLocation(10, 30));
    }

    [Theory, WorkItem(62435, "https://github.com/dotnet/roslyn/issues/62435")]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Constraints_02(string typeKind)
    {
        var source = $$"""
            file class C { }

            file {{typeKind}} D<T> where T : C // ok
            {
            }

            {{typeKind}} E<T> where T : C // 1
            {
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (7,{{17 + typeKind.Length}}): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'E<T>'.
            // {{typeKind}} E<T> where T : C // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C").WithArguments("C", "E<T>").WithLocation(7, 17 + typeKind.Length));
    }

    [Fact]
    public void Constraints_03()
    {
        var source = """
            file class C { }

            file class D
            {
                void M()
                {
                    local(new C());
                    void local<T>(T t) where T : C { } // ok
                }
            }

            class E
            {
                void M()
                {
                    local(new C());
                    void local<T>(T t) where T : C { } // ok
                }
            }
            """;

        // Local functions aren't members, so we don't give any diagnostics when their signatures contain file types.
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void Constraints_04()
    {
        var source = """
            file class C { }

            file delegate void D1<T>(T t) where T : C; // ok

            delegate void D2<T>(T t) where T : C; // 1
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,36): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'D2<T>'.
            // delegate void D2<T>(T t) where T : C; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C").WithArguments("C", "D2<T>").WithLocation(5, 36));
    }

    [Fact]
    public void PrimaryConstructor_01()
    {
        var source = """
            file class C { }

            record R1(C c); // 1
            record struct R2(C c); // 2

            file record R3(C c);
            file record struct R4(C c);
            """;

        var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
        comp.VerifyDiagnostics(
            // (3,8): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (3,8): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (4,15): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'R2'.
            // record struct R2(C c); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R2").WithArguments("C", "R2").WithLocation(4, 15),
            // (4,15): error CS9051: File type 'C' cannot be used in a member signature in non-file type 'R2'.
            // record struct R2(C c); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R2").WithArguments("C", "R2").WithLocation(4, 15)
            );
    }

    [Fact]
    public void Lambda_01()
    {
        var source = """
            file class C { }

            class Program
            {
                void M()
                {
                    var lambda = C (C c) => c; // ok
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void LocalFunction_01()
    {
        var source = """
            file class C { }

            class Program
            {
                void M()
                {
                    local(null!);
                    C local(C c) => c; // ok
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void AccessThroughNamespace_01()
    {
        var source = """
            using System;

            namespace NS
            {
                file class C
                {
                    public static void M() => Console.Write(1);
                }
            }

            class Program
            {
                public static void Main()
                {
                    NS.C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void AccessThroughNamespace_02()
    {
        var source1 = """
            using System;

            namespace NS
            {
                file class C
                {
                    public static void M()
                    {
                        Console.Write(1);
                    }
                }
            }
            """;

        var source2 = """
            class Program
            {
                static void Main()
                {
                    NS.C.M(); // 1
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (5,9): error CS0234: The type or namespace name 'C' does not exist in the namespace 'NS' (are you missing an assembly reference?)
            //         NS.C.M(); // 1
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "NS.C").WithArguments("C", "NS").WithLocation(5, 9));
    }

    [Fact]
    public void AccessThroughType_01()
    {
        var source = """
            using System;

            class Outer
            {
                file class C // 1
                {
                    public static void M() => Console.Write(1);
                }
            }

            class Program
            {
                public static void Main()
                {
                    Outer.C.M(); // 2
                }
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(5, 16),
            // (15,15): error CS0122: 'Outer.C' is inaccessible due to its protection level
            //         Outer.C.M(); // 2
            Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("Outer.C").WithLocation(15, 15));
    }

    [Fact]
    public void AccessThroughType_02()
    {
        var source1 = """
            using System;

            class Outer
            {
                file class C
                {
                    public static void M()
                    {
                        Console.Write(1);
                    }
                }
            }
            """;

        var source2 = """
            class Program
            {
                static void Main()
                {
                    Outer.C.M(); // 1
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (5,15): error CS0117: 'Outer' does not contain a definition for 'C'
            //         Outer.C.M(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMember, "C").WithArguments("Outer", "C").WithLocation(5, 15),
            // (5,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(5, 16));
    }

    [Fact]
    public void AccessThroughGlobalUsing_01()
    {
        var usings = """
            global using NS;
            """;

        var source = """
            using System;

            namespace NS
            {
                file class C
                {
                    public static void M() => Console.Write(1);
                }
            }

            class Program
            {
                public static void Main()
                {
                    C.M();
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { usings, source, IsExternalInitTypeDefinition }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Theory]
    [InlineData("file ")]
    [InlineData("")]
    public void AccessThroughGlobalUsing_02(string fileModifier)
    {
        var source = $$"""
            using System;

            namespace NS
            {
                {{fileModifier}}class C
                {
                    public static void M() => Console.Write(1);
                }
            }

            class Program
            {
                public static void Main()
                {
                    C.M(); // 1
                }
            }
            """;

        // note: 'Usings' is a legacy setting which only works in scripts.
        // https://github.com/dotnet/roslyn/issues/61502
        var compilation = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.DebugExe.WithUsings("NS"));
        compilation.VerifyDiagnostics(
            // (15,9): error CS0103: The name 'C' does not exist in the current context
            //         C.M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(15, 9));
    }

    [Fact]
    public void GlobalUsingStatic_01()
    {
        var source = """
            global using static C;

            file class C
            {
                public static void M() { }
            }
            """;

        var main = """
            class Program
            {
                public static void Main()
                {
                    M();
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // global using static C;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C;").WithLocation(1, 1),
                // (1,21): error CS9055: File type 'C' cannot be used in a 'global using static' directive.
                // global using static C;
                Diagnostic(ErrorCode.ERR_GlobalUsingStaticFileType, "C").WithArguments("C").WithLocation(1, 21),
                // (5,9): error CS0103: The name 'M' does not exist in the current context
                //         M();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(5, 9));
    }

    [Fact]
    public void GlobalUsingStatic_02()
    {
        var source = """
            global using static Container<C>;

            public class Container<T>
            {
            }

            file class C
            {
                public static void M() { }
            }
            """;

        var main = """
            class Program
            {
                public static void Main()
                {
                    M();
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // global using static Container<C>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static Container<C>;").WithLocation(1, 1),
                // (1,21): error CS9055: File type 'Container<C>' cannot be used in a 'global using static' directive.
                // global using static Container<C>;
                Diagnostic(ErrorCode.ERR_GlobalUsingStaticFileType, "Container<C>").WithArguments("Container<C>").WithLocation(1, 21),
                // (5,9): error CS0103: The name 'M' does not exist in the current context
                //         M();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(5, 9));
    }

    [Fact]
    public void UsingStatic_01()
    {
        var source = """
            using System;
            using static C;

            file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }

            class Program
            {
                public static void Main()
                {
                    M();
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void UsingStatic_02()
    {
        var source1 = """
            using System;
            using static C.D;

            M();

            file class C
            {
                public class D
                {
                    public static void M() { Console.Write(1); }
                }
            }
            """;

        var source2 = """
            using System;

            class C
            {
                public class D
                {
                    public static void M() { Console.Write(2); }
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, source2 }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
        var comp = (CSharpCompilation)verifier.Compilation;

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var members = comp.GetMembers("C");
        Assert.Equal(2, members.Length);
        var expectedMember = ((NamedTypeSymbol)members[0]).GetMember<MethodSymbol>("D.M");

        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbolInfo = model.GetSymbolInfo(invocation.Expression);
        Assert.Equal(expectedMember.GetPublicSymbol(), symbolInfo.Symbol);
        Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
    }

    [Theory]
    [InlineData("file ")]
    [InlineData("")]
    public void UsingStatic_03(string fileModifier)
    {
        // note: the top-level `class D` "wins" the lookup in this scenario.
        var source1 = $$"""
            using System;
            using static C;

            D.M();

            {{fileModifier}}class C
            {
                public class D
                {
                    public static void M() { Console.Write(1); }
                }
            }
            """;

        var source2 = """
            using System;

            class D
            {
                public static void M() { Console.Write(2); }
            }
            """;

        var verifier = CompileAndVerify(new[] { source1, source2 }, expectedOutput: "2");
        verifier.VerifyDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using static C;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static C;").WithLocation(2, 1));
        var comp = (CSharpCompilation)verifier.Compilation;

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var expectedMember = comp.GetMember("D.M");

        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbolInfo = model.GetSymbolInfo(invocation.Expression);
        Assert.Equal(expectedMember.GetPublicSymbol(), symbolInfo.Symbol);
        Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
    }

    [Fact]
    public void TypeShadowing()
    {
        var source = """
            using System;

            class Base
            {
                internal class C
                {
                    public static void M()
                    {
                        Console.Write(1);
                    }
                }
            }

            class Derived : Base
            {
                new file class C
                {
                }
            }
            """;

        var main = """
            class Program
            {
                public static void Main()
                {
                    Derived.C.M();
                }
            }
            """;

        // 'Derived.C' is not actually accessible from 'Program', so we just bind to 'Base.C'.
        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics(
            // (16,20): error CS9054: File type 'Derived.C' must be defined in a top level type; 'Derived.C' is a nested type.
            //     new file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Derived.C").WithLocation(16, 20));

        var expected = compilation.GetMember<MethodSymbol>("Base.C.M");

        var tree = compilation.SyntaxTrees[1];
        var model = compilation.GetSemanticModel(tree);
        var invoked = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
        var symbolInfo = model.GetSymbolInfo(invoked);
        Assert.Equal(expected, symbolInfo.Symbol.GetSymbol());
    }

    [Fact]
    public void SemanticModel_01()
    {
        var source = """
            namespace NS;

            file class C
            {
                public static void M() { }
            }

            class Program
            {
                public void M()
                {
                    C.M();
                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees[0];
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last().Body!;

        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        var info = model.GetSymbolInfo(((ExpressionStatementSyntax)body.Statements.First()).Expression);
        Assert.Equal("void NS.C@<tree 0>.M()", info.Symbol.ToTestDisplayString());

        var classC = compilation.GetMember("NS.C").GetPublicSymbol();
        Assert.Equal("NS.C@<tree 0>", classC.ToTestDisplayString());

        // lookup with no container
        var symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, name: "C");
        Assert.Equal(new[] { classC }, symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition);
        Assert.Contains(classC, symbols);

        // lookup with a correct container
        var nsSymbol = compilation.GetMember<NamespaceSymbol>("NS").GetPublicSymbol();
        Assert.Equal("NS", nsSymbol.ToTestDisplayString());

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol, name: "C");
        Assert.Equal(new[] { classC }, symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol);
        Assert.Contains(classC, symbols);

        // lookup with an incorrect container
        nsSymbol = compilation.GetMember<NamespaceSymbol>("System").GetPublicSymbol();
        Assert.Equal("System", nsSymbol.ToTestDisplayString());

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol, name: "C");
        Assert.Empty(symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol);
        Assert.DoesNotContain(classC, symbols);
    }

    [Fact]
    public void SemanticModel_02()
    {
        var source = """
            namespace NS;

            file class C
            {
                public static void M() { }
            }
            """;

        var main = """
            namespace NS;

            class Program
            {
                public void M()
                {
                    C.M(); // 1
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics(
            // (7,9): error CS0103: The name 'C' does not exist in the current context
            //         C.M(); // 1
            Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(7, 9)
            );

        var tree = compilation.SyntaxTrees[1];
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last().Body!;

        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        var info = model.GetSymbolInfo(((ExpressionStatementSyntax)body.Statements.First()).Expression);
        Assert.Null(info.Symbol);
        Assert.Empty(info.CandidateSymbols);
        Assert.Equal(CandidateReason.None, info.CandidateReason);

        var classC = compilation.GetMember("NS.C").GetPublicSymbol();
        Assert.Equal("NS.C@<tree 0>", classC.ToTestDisplayString());

        // lookup with no container
        var symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, name: "C");
        Assert.Empty(symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition);
        Assert.DoesNotContain(classC, symbols);

        // lookup with a correct container (still don't find the symbol due to lookup occurring in other file)
        var nsSymbol = compilation.GetMember<NamespaceSymbol>("NS").GetPublicSymbol();
        Assert.Equal("NS", nsSymbol.ToTestDisplayString());

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol, name: "C");
        Assert.Empty(symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol);
        Assert.DoesNotContain(classC, symbols);

        // lookup with an incorrect container
        nsSymbol = compilation.GetMember<NamespaceSymbol>("System").GetPublicSymbol();
        Assert.Equal("System", nsSymbol.ToTestDisplayString());

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol, name: "C");
        Assert.Empty(symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, container: nsSymbol);
        Assert.DoesNotContain(classC, symbols);
    }

    [Fact]
    public void Speculation_01()
    {
        var source = """
            file class C
            {
                public static void M() { }
            }

            class Program
            {
                public void M()
                {

                }
            }
            """;

        var compilation = CreateCompilation(source);
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees[0];
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last().Body!;

        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        var newBody = body.AddStatements(SyntaxFactory.ParseStatement("C.M();"));
        Assert.True(model.TryGetSpeculativeSemanticModel(position: body.OpenBraceToken.EndPosition, newBody, out var speculativeModel));
        var info = speculativeModel!.GetSymbolInfo(((ExpressionStatementSyntax)newBody.Statements.First()).Expression);
        Assert.Equal(compilation.GetMember("C.M").GetPublicSymbol(), info.Symbol);

        var classC = compilation.GetMember("C").GetPublicSymbol();
        var symbols = speculativeModel.LookupSymbols(newBody.OpenBraceToken.EndPosition, name: "C");
        Assert.Equal(new[] { classC }, symbols);

        symbols = speculativeModel.LookupSymbols(newBody.OpenBraceToken.EndPosition);
        Assert.Contains(classC, symbols);
    }

    [Fact]
    public void Speculation_02()
    {
        var source = """
            file class C
            {
                public static void M() { }
            }
            """;

        var main = """
            class Program
            {
                public void M()
                {

                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics();

        var tree = compilation.SyntaxTrees[1];
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last().Body!;

        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        var newBody = body.AddStatements(SyntaxFactory.ParseStatement("C.M();"));
        Assert.True(model.TryGetSpeculativeSemanticModel(position: body.OpenBraceToken.EndPosition, newBody, out var speculativeModel));
        var info = speculativeModel!.GetSymbolInfo(((ExpressionStatementSyntax)newBody.Statements.First()).Expression);
        Assert.Null(info.Symbol);
        Assert.Empty(info.CandidateSymbols);
        Assert.Equal(CandidateReason.None, info.CandidateReason);

        var symbols = speculativeModel.LookupSymbols(newBody.OpenBraceToken.EndPosition, name: "C");
        Assert.Empty(symbols);

        symbols = speculativeModel.LookupSymbols(newBody.OpenBraceToken.EndPosition);
        Assert.DoesNotContain(compilation.GetMember("C").GetPublicSymbol(), symbols);
    }

    [Fact]
    public void Cref_01()
    {
        var source = """
            file class C
            {
                public static void M() { }
            }

            class Program
            {
                /// <summary>
                /// In the same file as <see cref="C"/>.
                /// </summary>
                public static void M()
                {

                }
            }
            """;

        var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
        compilation.VerifyDiagnostics();
    }

    [Fact]
    public void Cref_02()
    {
        var source = """
            file class C
            {
                public static void M() { }
            }
            """;

        var main = """
            class Program
            {
                /// <summary>
                /// In a different file than <see cref="C"/>.
                /// </summary>
                public static void M()
                {

                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main }, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
        compilation.VerifyDiagnostics(
            // (4,45): warning CS1574: XML comment has cref attribute 'C' that could not be resolved
            //     /// In a different file than <see cref="C"/>.
            Diagnostic(ErrorCode.WRN_BadXMLRef, "C").WithArguments("C").WithLocation(4, 45)
            );
    }

    [Fact]
    public void TopLevelStatements()
    {
        var source = """
            using System;

            C.M();

            file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void StaticFileClass()
    {
        var source = """
            using System;

            C.M();

            static file class C
            {
                public static void M()
                {
                    Console.Write(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMethod_01()
    {
        var source = """
            using System;

            "a".M();

            static file class C
            {
                public static void M(this string s)
                {
                    Console.Write(1);
                }
            }
            """;

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ExtensionMethod_02()
    {
        var source1 = """
            "a".M(); // 1
            """;

        var source2 = """
            using System;

            static file class C
            {
                public static void M(this string s)
                {
                    Console.Write(1);
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (1,5): error CS1061: 'string' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
            // "a".M(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("string", "M").WithLocation(1, 5));
    }

    [Fact]
    public void ExtensionMethod_03()
    {
        var source1 = """
            "a".M(); // 1
            """;

        var source2 = """
            using System;

            file class C
            {
                static class D
                {
                    public static void M(this string s) // 2
                    {
                        Console.Write(1);
                    }
                }
            }
            """;

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics(
            // (1,5): error CS1061: 'string' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
            // "a".M(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("string", "M").WithLocation(1, 5),
            // (7,28): error CS1109: Extension methods must be defined in a top level static class; D is a nested class
            //         public static void M(this string s) // 2
            Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("D").WithLocation(7, 28));
    }

    [Fact]
    public void Alias_01()
    {
        var source = """
            namespace NS;
            using C1 = NS.C;

            file class C
            {
            }

            class D : C1 { } // 1
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (8,7): error CS9053: File type 'C' cannot be used as a base type of non-file type 'D'.
            // class D : C1 { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "D").WithArguments("NS.C", "NS.D").WithLocation(8, 7));
    }

    [Fact]
    public void SymbolDisplay()
    {
        var source1 = """
            file class C1
            {
                public static void M() { }
            }
            """;

        var source2 = """
            file class C2
            {
                public static void M() { }
            }
            """;

        var comp = CreateCompilation(new[]
        {
            SyntaxFactory.ParseSyntaxTree(source1, TestOptions.RegularPreview),
            SyntaxFactory.ParseSyntaxTree(source2, TestOptions.RegularPreview, path: "path/to/FileB.cs")
        });
        comp.VerifyDiagnostics();

        var c1 = comp.GetMember<NamedTypeSymbol>("C1");
        var c2 = comp.GetMember<NamedTypeSymbol>("C2");
        Assert.Equal("C1@<tree 0>", c1.ToTestDisplayString());
        Assert.Equal("C2@FileB", c2.ToTestDisplayString());

        Assert.Equal("void C1@<tree 0>.M()", c1.GetMember("M").ToTestDisplayString());
        Assert.Equal("void C2@FileB.M()", c2.GetMember("M").ToTestDisplayString());
    }

    [Fact]
    public void Script_01()
    {
        var source1 = """
            using System;

            C1.M("a");

            static file class C1
            {
                public static void M(this string s) { }
            }
            """;

        var comp = CreateSubmission(source1, parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.Preview));
        comp.VerifyDiagnostics(
            // (5,19): error CS9054: File type 'C1' must be defined in a top level type; 'C1' is a nested type.
            // static file class C1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C1").WithArguments("C1").WithLocation(5, 19),
            // (7,24): error CS1109: Extension methods must be defined in a top level static class; C1 is a nested class
            //     public static void M(this string s) { }
            Diagnostic(ErrorCode.ERR_ExtensionMethodsDecl, "M").WithArguments("C1").WithLocation(7, 24));
    }

    [Fact]
    public void SystemVoid_01()
    {
        var source1 = """
            using System;

            void M(Void v) { }

            namespace System
            {
                file class Void { }
            }
            """;

        // https://github.com/dotnet/roslyn/issues/62331
        // Ideally we would give an error about use of System.Void here.
        var comp = CreateCompilation(source1);
        comp.VerifyDiagnostics(
                // (3,6): warning CS8321: The local function 'M' is declared but never used
                // void M(Void v) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "M").WithArguments("M").WithLocation(3, 6));

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);

        var voidTypeSyntax = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single().Type!;
        var typeInfo = model.GetTypeInfo(voidTypeSyntax);
        Assert.Equal("System.Void@<tree 0>", typeInfo.Type!.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes)));
    }

    [Fact]
    public void GetTypeByMetadataName_01()
    {
        var source1 = """
            file class C { }
            """;

        // from source
        var comp = CreateCompilation(source1);
        comp.VerifyDiagnostics();
        var sourceMember = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>F0__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<>F0__C");
        Assert.Equal(sourceMember, sourceType);

        Assert.Null(comp.GetTypeByMetadataName("<>F0__D"));
        Assert.Null(comp.GetTypeByMetadataName("<>F1__C"));
        Assert.Null(comp.GetTypeByMetadataName("F0__C"));
        Assert.Null(comp.GetTypeByMetadataName("<file>F0__C"));

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();
        var metadataMember = comp2.GetMember<NamedTypeSymbol>("<>F0__C");
        Assert.Equal("<>F0__C", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<>F0__C");
        Assert.Equal(metadataMember, metadataType);
    }

    [Fact]
    public void GetTypeByMetadataName_02()
    {
        var source1 = """
            file class C<T> { }
            """;

        // from source
        var comp = CreateCompilation(source1);
        comp.VerifyDiagnostics();
        var sourceMember = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>F0__C`1", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<>F0__C`1");
        Assert.Equal(sourceMember, sourceType);
        Assert.Null(comp.GetTypeByMetadataName("<>F0__C"));

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var metadataMember = comp2.GetMember<NamedTypeSymbol>("<>F0__C");
        Assert.Equal("<>F0__C`1", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<>F0__C`1");
        Assert.Equal(metadataMember, metadataType);
    }

    [Fact]
    public void GetTypeByMetadataName_03()
    {
        var source1 = """
            class Outer
            {
                file class C { } // 1
            }
            """;

        // from source
        var comp = CreateCompilation(source1);
        comp.VerifyDiagnostics(
            // (3,16): error CS9054: File type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));
        var sourceMember = comp.GetMember<NamedTypeSymbol>("Outer.C");
        Assert.Equal("<>F0__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("Outer.<>F0__C");
        // Note: strictly speaking, it would be reasonable to return the (invalid) nested file type symbol here.
        // However, since we don't actually support nested file types, we don't think we need the API to do the additional lookup
        // when the requested type is nested, and so we end up giving a null here.
        Assert.Null(sourceType);
    }

    [Fact]
    public void GetTypeByMetadataName_04()
    {
        var source1 = """
            file class C { }
            """;

        var source2 = """
            class C { }
            """;

        // from source
        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyDiagnostics();
        var sourceMember = comp.GetMembers("C")[0];
        Assert.Equal("<>F0__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<>F0__C");
        Assert.Equal(sourceMember, sourceType);

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var metadataMember = comp2.GetMember<NamedTypeSymbol>("<>F0__C");
        Assert.Equal("<>F0__C", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<>F0__C");
        Assert.Equal(metadataMember, metadataType);
    }

    [CombinatorialData]
    [Theory]
    public void GetTypeByMetadataName_05(bool firstIsMetadataReference, bool secondIsMetadataReference)
    {
        var source1 = """
            file class C { }
            """;

        // Create two references containing identically-named file types
        var ref1 = CreateCompilation(source1, assemblyName: "ref1");
        var ref2 = CreateCompilation(source1, assemblyName: "ref2");

        var comp = CreateCompilation("", references: new[]
        {
            firstIsMetadataReference ? ref1.ToMetadataReference() : ref1.EmitToImageReference(),
            secondIsMetadataReference ? ref2.ToMetadataReference() : ref2.EmitToImageReference()
        });
        comp.VerifyDiagnostics();

        var sourceType = comp.GetTypeByMetadataName("<>F0__C");
        Assert.Null(sourceType);

        var types = comp.GetTypesByMetadataName("<>F0__C");
        Assert.Equal(2, types.Length);
        Assert.Equal(firstIsMetadataReference ? "C@<tree 0>" : "<>F0__C", types[0].ToTestDisplayString());
        Assert.Equal(secondIsMetadataReference ? "C@<tree 0>" : "<>F0__C", types[1].ToTestDisplayString());
        Assert.NotEqual(types[0], types[1]);
    }

    [Fact]
    public void GetTypeByMetadataName_06()
    {
        var source1 = """
            file class C { }
            file class C { }
            """;

        var comp = CreateCompilation(source1);
        comp.VerifyDiagnostics(
            // (2,12): error CS0101: The namespace '<global namespace>' already contains a definition for 'C'
            // file class C { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "<global namespace>").WithLocation(2, 12));

        var sourceType = ((Compilation)comp).GetTypeByMetadataName("<>F0__C");
        Assert.Equal("C@<tree 0>", sourceType.ToTestDisplayString());

        var types = comp.GetTypesByMetadataName("<>F0__C");
        Assert.Equal(1, types.Length);
        Assert.Same(sourceType, types[0]);
    }

    [Fact]
    public void GetTypeByMetadataName_07()
    {
        var source1 = """
            file class C { }
            """;

        var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreview, path: "path/to/SomeFile.cs"));
        comp.VerifyDiagnostics();

        Assert.Null(comp.GetTypeByMetadataName("<>F0__C"));
        Assert.Empty(comp.GetTypesByMetadataName("<>F0__C"));

        Assert.Null(comp.GetTypeByMetadataName("<WrongName>F0__C"));
        Assert.Empty(comp.GetTypesByMetadataName("<WrongName>F0__C"));

        var sourceType = ((Compilation)comp).GetTypeByMetadataName("<SomeFile>F0__C");
        Assert.Equal("C@SomeFile", sourceType.ToTestDisplayString());

        var types = comp.GetTypesByMetadataName("<SomeFile>F0__C");
        Assert.Equal(1, types.Length);
        Assert.Same(sourceType, types[0]);
    }

    [Fact]
    public void AssociatedSyntaxTree_01()
    {
        var source = """
            file class C
            {
                void M(C c)
                {
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        var type = (INamedTypeSymbol)model.GetTypeInfo(node.Type!).Type!;
        Assert.Equal("C@<tree 0>", type.ToTestDisplayString());
        Assert.Equal(tree, type.GetSymbol()!.AssociatedSyntaxTree);
        Assert.True(type.IsFile);

        var referencingMetadataComp = CreateCompilation("", new[] { comp.ToMetadataReference() });
        type = ((Compilation)referencingMetadataComp).GetTypeByMetadataName("<>F0__C")!;
        Assert.Equal("C@<tree 0>", type.ToTestDisplayString());
        Assert.Equal(tree, type.GetSymbol()!.AssociatedSyntaxTree);
        Assert.True(type.IsFile);

        var referencingImageComp = CreateCompilation("", new[] { comp.EmitToImageReference() });
        type = ((Compilation)referencingImageComp).GetTypeByMetadataName("<>F0__C")!;
        Assert.Equal("<>F0__C", type.ToTestDisplayString());
        Assert.Null(type.GetSymbol()!.AssociatedSyntaxTree);
        Assert.False(type.IsFile);
    }

    [Fact]
    public void AssociatedSyntaxTree_02()
    {
        var source = """
            class C
            {
                void M(C c)
                {
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        var type = (INamedTypeSymbol)model.GetTypeInfo(node.Type!).Type!;
        Assert.Equal("C", type.ToTestDisplayString());
        Assert.Null(type.GetSymbol()!.AssociatedSyntaxTree);
        Assert.False(type.IsFile);
    }

    [Fact]
    public void AssociatedSyntaxTree_03()
    {
        var source = """
            file class C<T>
            {
                void M(C<int> c)
                {
                }
            }
            """;
        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        var type = (INamedTypeSymbol)model.GetTypeInfo(node.Type!).Type!;
        Assert.Equal("C<System.Int32>@<tree 0>", type.ToTestDisplayString());
        Assert.Equal(tree, type.GetSymbol()!.AssociatedSyntaxTree);
        Assert.True(type.IsFile);
    }
}
