// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
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
            Assert.True(((SourceMemberContainerTypeSymbol)outer).IsFileLocal);

            var classC = comp.GetMember<NamedTypeSymbol>("Outer.C");
            Assert.Equal(Accessibility.Private, classC.DeclaredAccessibility);
            Assert.False(((SourceMemberContainerTypeSymbol)classC).IsFileLocal);
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
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
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
            // (8,10): error CS9051: File-local type 'Outer.C' cannot be used in a member signature in non-file-local type 'D'.
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
        AssertEx.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C", symbol.MetadataName);

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
            Assert.Equal(new[] { "<Module>", "C", "Program", "Microsoft", "System" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
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

        var verifier = CompileAndVerify(new[] { ("", "file1.cs"), (source, "file2.cs") }, expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("C");
        AssertEx.Equal("<file2>F66382B88D8E28FDD21CEADA0DE847F8B00DA1324042DD28F8FFC58C454BD6188__C", symbol.MetadataName);

        // The qualified name here is based on `SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes`.
        // We don't actually look up based on the file-encoded name of the type.
        // This is similar to how generic types work (lookup based on 'C<T>' instead of 'C`1').
        verifier.VerifyIL("C@file2.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "C", "Program", "Microsoft", "System" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
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
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__E", symbol.MetadataName);

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
            Assert.Equal(new[] { "<Module>", "E", "Program", "Microsoft", "System" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
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

        var verifier = CompileAndVerify(source, expectedOutput: "(<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__E)1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("E");
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__E", symbol.MetadataName);

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "E", "Attr", "Program", "Microsoft", "System" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
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
            // (10,12): error CS9051: File-local type 'E' cannot be used in a member signature in non-file-local type 'Attr'.
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

        var verifier = CompileAndVerify(source, expectedOutput: "(<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__E)1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var symbol = comp.GetMember<NamedTypeSymbol>("E");
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__E", symbol.MetadataName);

        void symbolValidator(ModuleSymbol symbol)
        {
            Assert.Equal(new[] { "<Module>", "E", "Attr", "Program", "Microsoft", "System" }, symbol.GlobalNamespace.GetMembers().Select(m => m.Name));
            var classC = symbol.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(new[] { "value__", "E1", "E2", ".ctor" }, classC.MemberNames);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void NonFileLocalClass_Duplicate()
    {
        var source = @"
public class D { }

public partial class C
{
    public class D { }
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
            // 1.cs(2,14): error CS0101: The namespace '<global namespace>' already contains a definition for 'D'
            // public class D { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "D").WithArguments("D", "<global namespace>").WithLocation(2, 14),
            // 1.cs(6,18): error CS0102: The type 'C' already contains a definition for 'D'
            //     public class D { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "D").WithArguments("C", "D").WithLocation(6, 18));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void FileLocalClass_Duplicate()
    {
        var source = @"
file class D { }

public partial class C
{
    file class D { } // 1
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
                    // 1.cs(6,16): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
                    //     file class D { } // 1
                    Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 16),
                    // 0.cs(6,16): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
                    //     file class D { } // 1
                    Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 16));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void NonFileLocalEnum_Duplicate()
    {
        var source = @"
public enum E { }

public partial class C
{
    public enum E { }
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
            // 1.cs(2,13): error CS0101: The namespace '<global namespace>' already contains a definition for 'E'
            // public enum E { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "E").WithArguments("E", "<global namespace>").WithLocation(2, 13),
            // 1.cs(6,17): error CS0102: The type 'C' already contains a definition for 'E'
            //     public enum E { }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "E").WithArguments("C", "E").WithLocation(6, 17));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void MixedFileLocalClass_Duplicate()
    {
        var source1 = @"
file class D { }

public partial class C
{
    file class D { } // 1
}
";

        var source2 = @"
public class D { }

public partial class C
{
    public class D { }
}
";

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,16): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file class D { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 16));

        comp = CreateCompilation(new[] { source2, source1 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,16): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file class D { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 16));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void FileLocalEnum_Duplicate()
    {
        var source = @"
file enum E { }

public partial class C
{
    file enum E { } // 1
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
            // 1.cs(6,15): error CS9054: File-local type 'C.E' must be defined in a top level type; 'C.E' is a nested type.
            //     file enum E { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "E").WithArguments("C.E").WithLocation(6, 15),
            // 0.cs(6,15): error CS9054: File-local type 'C.E' must be defined in a top level type; 'C.E' is a nested type.
            //     file enum E { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "E").WithArguments("C.E").WithLocation(6, 15));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void MixedFileLocalEnum_Duplicate()
    {
        var source1 = @"
file enum E { }

public partial class C
{
    file enum E { } // 1
}
";

        var source2 = @"
public enum E { }

public partial class C
{
    public enum E { }
}
";

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,15): error CS9054: File-local type 'C.E' must be defined in a top level type; 'C.E' is a nested type.
            //     file enum E { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "E").WithArguments("C.E").WithLocation(6, 15));

        comp = CreateCompilation(new[] { source2, source1 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,15): error CS9054: File-local type 'C.E' must be defined in a top level type; 'C.E' is a nested type.
            //     file enum E { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "E").WithArguments("C.E").WithLocation(6, 15));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void NonFileLocalDelegate_Duplicate()
    {
        var source = @"
public delegate void D();

public partial class C
{
    public delegate void D();
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
            // 1.cs(2,22): error CS0101: The namespace '<global namespace>' already contains a definition for 'D'
            // public delegate void D();
            Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "D").WithArguments("D", "<global namespace>").WithLocation(2, 22),
            // 1.cs(6,26): error CS0102: The type 'C' already contains a definition for 'D'
            //     public delegate void D();
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "D").WithArguments("C", "D").WithLocation(6, 26));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void MixedFileLocalDelegate_Duplicate()
    {
        var source1 = @"
file delegate void D();

public partial class C
{
    file delegate void D(); // 1
}
";

        var source2 = @"
public delegate void D();

public partial class C
{
    public delegate void D();
}
";

        var comp = CreateCompilation(new[] { source1, source2 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,24): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file delegate void D(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 24));

        comp = CreateCompilation(new[] { source2, source1 });
        comp.VerifyEmitDiagnostics(
            // 0.cs(6,24): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file delegate void D(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 24));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67258")]
    public void FileLocalDelegate_Duplicate()
    {
        var source = @"
file delegate void D();

public partial class C
{
    file delegate void D(); // 1
}
";
        var comp = CreateCompilation(new[] { source, source });
        comp.VerifyEmitDiagnostics(
            // 1.cs(6,24): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file delegate void D(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 24),
            // 0.cs(6,24): error CS9054: File-local type 'C.D' must be defined in a top level type; 'C.D' is a nested type.
            //     file delegate void D(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "D").WithArguments("C.D").WithLocation(6, 24));
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
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
        AssertEx.Equal("<MyFile>F5E7157F91336401EED4848664C7CEB8A5E156C0D713F4211A61BDB8932B19EF2__C`1", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "C", "Microsoft", "System" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));

            var classC = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.Equal("<MyFile>F5E7157F91336401EED4848664C7CEB8A5E156C0D713F4211A61BDB8932B19EF2__C`1", classC.MetadataName);
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

        const string expectedMetadataName = "<My__File>FCE8825365B7010B8DE2ACFBE270B3B795D1AB2633451F8A6C1A94FB1933D5E4E__C";
        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/My<>File.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@My__File", c.ToTestDisplayString());
        AssertEx.Equal(expectedMetadataName, c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "C", "Microsoft", "System" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.Equal(expectedMetadataName, expectedSymbol.MetadataName);
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
        AssertEx.Equal("<MyGeneratedFile_g>F18307E6C553D2E6465CEA162655C06E2BB2896889519559EB1EE5FA53513F0E8__C", c.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "C", "Microsoft", "System" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            AssertEx.Equal("<MyGeneratedFile_g>F18307E6C553D2E6465CEA162655C06E2BB2896889519559EB1EE5FA53513F0E8__C", expectedSymbol.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, expectedSymbol.MemberNames);
        }
    }

    [Theory]
    [InlineData("""
            file class Outer1 { }
            file class Outer2 { }
            """, "Outer2")]
    [InlineData("""
            file class Outer { }
            """, "Outer")]
    public void Determinism(string source, string fileTypeName)
    {
        var (root1, root2) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (@"q:\", @"j:\") : ("/q", "/j");
        var testSource1 = CSharpTestSource.Parse(source, Path.Combine(root1, "code.cs"));
        var testSource2 = CSharpTestSource.Parse(source, Path.Combine(root2, "code.cs"));
        var options = TestOptions.DebugDll.WithDeterministic(true);
        var comp1 = CreateCompilation(testSource1, options: options, assemblyName: "filetypes");
        var comp2 = CreateCompilation(testSource2, options: options, assemblyName: "filetypes");

        var resolver = new SourceFileResolver(
            ImmutableArray<string>.Empty,
            baseDirectory: null,
            ImmutableArray.Create(new KeyValuePair<string, string>(root2, root1)));
        var comp3 = CreateCompilation(testSource2, options: options.WithSourceReferenceResolver(resolver), assemblyName: "filetypes");

        var outer1 = comp1.GetMember<NamedTypeSymbol>(fileTypeName).AssociatedFileIdentifier;
        var outer2 = comp2.GetMember<NamedTypeSymbol>(fileTypeName).AssociatedFileIdentifier;
        var outer3 = comp3.GetMember<NamedTypeSymbol>(fileTypeName).AssociatedFileIdentifier;
        Assert.False(outer1.FilePathChecksumOpt.IsDefaultOrEmpty);
        Assert.False(outer2.FilePathChecksumOpt.IsDefaultOrEmpty);
        Assert.False(outer3.FilePathChecksumOpt.IsDefaultOrEmpty);
        Assert.False(outer1.FilePathChecksumOpt.SequenceEqual(outer2.FilePathChecksumOpt));
        Assert.True(outer1.FilePathChecksumOpt.SequenceEqual(outer3.FilePathChecksumOpt));

        var emitOptions = EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded);
        var bytes1 = comp1.EmitToArray(emitOptions);
        var bytes2 = comp2.EmitToArray(emitOptions);
        var bytes3 = comp3.EmitToArray(emitOptions);
        Assert.False(bytes1.SequenceEqual(bytes2));
        Assert.True(bytes1.SequenceEqual(bytes3));
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
        var source2 = SyntaxFactory.ParseSyntaxTree("", options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);

        var comp = CreateCompilation(new[] { source1, source2 }, assemblyName: "comp");
        verify();

        comp = CreateCompilation(new[] { source2, source1 }, assemblyName: "comp");
        verify();

        void verify()
        {
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // path/to/file.cs(5,12): error CS9067: File-local type 'C' must be declared in a file with a unique path. Path 'path/to/file.cs' is used in multiple files.
                // file class C
                Diagnostic(ErrorCode.ERR_FileTypeNonUniquePath, "C").WithArguments("C", "path/to/file.cs").WithLocation(5, 12));
            var classC = comp.GetMember("C");
            Assert.Equal(source1, classC.Locations[0].SourceTree);
            AssertEx.Equal("<file>F620949CDCC480533E3607E5DD92F88E866EC1D65C225D70509A32F831433D9A4__C", classC.MetadataName);
        }
    }

    [Fact]
    public void DuplicateFileNames_02()
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

            namespace NS;

            file class C
            {
                public static void M() { Console.Write(2); }
            }
            """, options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);

        var comp = CreateCompilation(new[] { source1, source2 }, assemblyName: "comp");
        verify();

        comp = CreateCompilation(new[] { source2, source1 }, assemblyName: "comp");
        verify();

        void verify()
        {
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // path/to/file.cs(5,12): error CS9067: File-local type 'C' must be declared in a file with a unique path. Path 'path/to/file.cs' is used in multiple files.
                // file class C
                Diagnostic(ErrorCode.ERR_FileTypeNonUniquePath, "C").WithArguments("NS.C", "path/to/file.cs").WithLocation(5, 12),
                // path/to/file.cs(5,12): error CS9067: File-local type 'C' must be declared in a file with a unique path. Path 'path/to/file.cs' is used in multiple files.
                // file class C
                Diagnostic(ErrorCode.ERR_FileTypeNonUniquePath, "C").WithArguments("C", "path/to/file.cs").WithLocation(5, 12));
            var member = comp.GetMember("C");
            Assert.Equal(source1, member.Locations[0].SourceTree);
            AssertEx.Equal("<file>F620949CDCC480533E3607E5DD92F88E866EC1D65C225D70509A32F831433D9A4__C", member.MetadataName);
        }
    }

    [Fact]
    public void DuplicateFileNames_03()
    {
        var path = "path/to/file.cs";
        var source1 = SyntaxFactory.ParseSyntaxTree("""
            using System;

            namespace NS1.NS2;

            file class C<T>
            {
                public static void M() { Console.Write(1); }
            }
            """, options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);
        var source2 = SyntaxFactory.ParseSyntaxTree("", options: TestOptions.RegularPreview, path: path, encoding: Encoding.Default);

        var comp = CreateCompilation(new[] { source1, source2 }, assemblyName: "comp");
        verify();

        comp = CreateCompilation(new[] { source2, source1 }, assemblyName: "comp");
        verify();

        void verify()
        {
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // path/to/file.cs(5,12): error CS9067: File-local type 'C<T>' must be declared in a file with a unique path. Path 'path/to/file.cs' is used in multiple files.
                // file class C<T>
                Diagnostic(ErrorCode.ERR_FileTypeNonUniquePath, "C").WithArguments("NS1.NS2.C<T>", "path/to/file.cs").WithLocation(5, 12));
            var classC = comp.GetMember("NS1.NS2.C");
            Assert.Equal(source1, classC.Locations[0].SourceTree);
            AssertEx.Equal("<file>F620949CDCC480533E3607E5DD92F88E866EC1D65C225D70509A32F831433D9A4__C`1", classC.MetadataName);
        }
    }

    [Fact]
    public void DuplicateFileNames_04()
    {
        var source1 = SyntaxFactory.ParseSyntaxTree("""
            using System;

            C.M();

            file class C
            {
                public static void M() { Console.Write(1); }
            }
            """, options: TestOptions.RegularPreview, path: "path/to/file.cs", encoding: Encoding.Default);
        var source2 = SyntaxFactory.ParseSyntaxTree("", options: TestOptions.RegularPreview, path: "path/to/File.cs", encoding: Encoding.Default);

        var comp = CreateCompilation(new[] { source1, source2 }, assemblyName: "comp");
        verify();

        comp = CreateCompilation(new[] { source2, source1 }, assemblyName: "comp");
        verify();

        void verify()
        {
            comp.VerifyEmitDiagnostics();
            var classC = comp.GetMember("C");
            Assert.Equal(source1, classC.Locations[0].SourceTree);
            AssertEx.Equal("<file>F620949CDCC480533E3607E5DD92F88E866EC1D65C225D70509A32F831433D9A4__C", classC.MetadataName);
        }
    }

    [Fact]
    public void DuplicateFileNames_05()
    {
        var source1 = """
            using System;

            file class C // 1
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
                    C.M();
                }
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file1.cs") });
        comp.VerifyDiagnostics();
        comp.VerifyEmitDiagnostics(
            // file1.cs(3,12): error CS9067: File-local type 'C' must be declared in a file with a unique path. Path 'file1.cs' is used in multiple files.
            // file class C // 1
            Diagnostic(ErrorCode.ERR_FileTypeNonUniquePath, "C").WithArguments("C", "file1.cs").WithLocation(3, 12));
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

        var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: $"path/to/My{badChar}File.cs", encoding: Encoding.Default));
        var sourceFileTypeSymbol = comp.GetMember("C");

        var verifier = CompileAndVerify(comp, expectedOutput: "1", symbolValidator: symbolValidator);
        verifier.VerifyDiagnostics();

        Assert.Equal("C@My_File", sourceFileTypeSymbol.ToTestDisplayString());
        Assert.Matches(expectedRegexPattern: @"<My_File>F[\w\d]{64}__C", sourceFileTypeSymbol.MetadataName);

        void symbolValidator(ModuleSymbol module)
        {
            Assert.Equal(new[] { "<Module>", "Program", "C", "Microsoft", "System" }, module.GlobalNamespace.GetMembers().Select(m => m.Name));
            var expectedSymbol = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(sourceFileTypeSymbol.MetadataName, expectedSymbol.MetadataName);
            Assert.Equal(new[] { "M", ".ctor" }, expectedSymbol.MemberNames);
        }
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public void BadFileNames_04()
    {
        var source1 = """
            new C(); // 1

            file class C { } // 2
            """;

        var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreview, path: "\uD800.cs"));
        comp.VerifyDiagnostics(
            // ?.cs(1,5): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
            // new C(); // 1
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C").WithLocation(1, 5),
            // ?.cs(3,12): error CS9068: File-local type 'C' cannot be used because the containing file path cannot be converted into the equivalent UTF-8 byte representation. Unable to translate Unicode character \\uD800 at index 0 to specified code page.      
            // file class C { } // 2
            Diagnostic(ErrorCode.ERR_FilePathCannotBeConvertedToUtf8, "C")
                .WithArguments(
                    "C",
                    ExecutionConditionUtil.IsCoreClr
                        ? @"Unable to translate Unicode character \\uD800 at index 0 to specified code page."
                        : @"Unable to translate Unicode character \uD800 at index 0 to specified code page.")
                .WithLocation(3, 12)
            );

        var classC = comp.GetMember("C");
        Assert.Equal("<_>F<no checksum>__C", classC.MetadataName);
        Assert.Null(comp.GetTypeByMetadataName("<_>F<no checksum>__C"));
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

        var verifier = CompileAndVerify(SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularPreview, path: "path/to/My+File.cs", encoding: Encoding.Default), expectedOutput: "1", symbolValidator: validateSymbols);
        verifier.VerifyDiagnostics();

        var comp = (CSharpCompilation)verifier.Compilation;
        var c = comp.GetMember("C");
        Assert.Equal("C@My_File", c.ToTestDisplayString());
        AssertEx.Equal("<My_File>FA818559F9E8E4AF40425A1819866C71357DE9017B4B7EFE1D34D9F48C0539B6E__C", c.MetadataName);

        void validateSymbols(ModuleSymbol module)
        {
            var type = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.NotNull(type);
            Assert.Equal(new[] { "M", ".ctor" }, type.MemberNames);
        }
    }

#pragma warning disable format
    [Theory]
    [InlineData("file", "file", "<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C", "<file2>F66382B88D8E28FDD21CEADA0DE847F8B00DA1324042DD28F8FFC58C454BD6188__C")]
    [InlineData("file", "",     "<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C", "C")]
    [InlineData("",     "file", "C",                                                                           "<file2>F66382B88D8E28FDD21CEADA0DE847F8B00DA1324042DD28F8FFC58C454BD6188__C")]
#pragma warning restore format
    public void Duplication_01(string firstFileModifier, string secondFileModifier, string firstMetadataName, string secondMetadataName)
    {
        // A file-local type is allowed to have the same name as a non-file-local type from a different file.
        // When both a file-local type and non-file-local type with the same name are in scope, the file-local type is preferred, since it's "more local".
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
                }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source1 + main, "file1.cs"), (source2, "file2.cs") }, expectedOutput: "1");
        var comp = (CSharpCompilation)verifier.Compilation;
        var cs = comp.GetMembers("C");
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        AssertEx.Equal(firstMetadataName, expectedSymbol.MetadataName);
        verify();

        verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2 + main, "file2.cs") }, expectedOutput: "2");
        comp = (CSharpCompilation)verifier.Compilation;
        cs = comp.GetMembers("C");
        tree = comp.SyntaxTrees[1];
        expectedSymbol = cs[1];
        AssertEx.Equal(secondMetadataName, expectedSymbol.MetadataName);
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
                // (14,11): error CS0121: The call is ambiguous between the following methods or properties: 'C.M() [0.cs(5)]' and 'C.M() [1.cs(5)]'
                //         C.M();
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M() [0.cs(5)]", "C.M() [1.cs(5)]").WithLocation(14, 11));

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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2, "file2.cs"), (main, "file3.cs") }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFileLocal: false });

        var syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (main, "file2.cs") }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFileLocal: false });
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });

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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (main, "file2.cs") }, expectedOutput: "2");
        var comp = (CSharpCompilation)verifier.Compilation;
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.Equal(firstClassIsFile, ((SourceMemberContainerTypeSymbol)c0).IsFileLocal);
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
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
        // note: we avoid `using System;` here because we don't want to attempt to bind to `System.Number`
        var source1 = """
            namespace NS;

            partial class C
            {
                public static void M()
                {
                    System.Console.Write(Number);
                }
            }
            """;

        var source2 = """
            namespace NS;

            partial class C
            {
                private static int Number => 1;
            }

            file class C
            {
                public static void M()
                {
                    System.Console.Write(2);
                }
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
        comp.VerifyDiagnostics(
            // file2.cs(8,12): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // file class C
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(8, 12)
            );

        var cs = comp.GetMembers("NS.C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFileLocal: false });
        var syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.Equal(comp.SyntaxTrees[1], c1.DeclaringSyntaxReferences.Single().SyntaxTree);

        comp = CreateCompilation(new[] { (source2, "file2.cs"), (source1, "file1.cs") });
        comp.VerifyDiagnostics(
            // file1.cs(5,24): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
            //     public static void M()
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "NS.C").WithLocation(5, 24),
            // file1.cs(7,30): error CS0103: The name 'Number' does not exist in the current context
            //         System.Console.Write(Number);
            Diagnostic(ErrorCode.ERR_NameNotInContext, "Number").WithArguments("Number").WithLocation(7, 30),
            // file2.cs(8,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(8, 12)
            );

        var c = comp.GetMember("NS.C");
        Assert.True(c is SourceMemberContainerTypeSymbol { IsFileLocal: true });
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
        comp.VerifyDiagnostics(
            // (11,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(11, 12));

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        var syntaxReferences = c1.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[1], syntaxReferences[1].SyntaxTree);

        comp = CreateCompilation(new[] { (source2, "file2.cs"), (source1, "file1.cs") });
        comp.VerifyDiagnostics(
            // (11,12): error CS0260: Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists
            // file class C
            Diagnostic(ErrorCode.ERR_MissingPartial, "C").WithArguments("C").WithLocation(11, 12));

        cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        c0 = cs[0];
        Assert.True(c0 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        syntaxReferences = c0.DeclaringSyntaxReferences;
        Assert.Equal(2, syntaxReferences.Length);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[0].SyntaxTree);
        Assert.Equal(comp.SyntaxTrees[0], syntaxReferences[1].SyntaxTree);

        c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFileLocal: true });
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

        var compilation = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs"), (source3, "file3.cs") });
        compilation.VerifyDiagnostics(
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16),
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));

        var classOuter = compilation.GetMember<NamedTypeSymbol>("Outer");
        var cs = classOuter.GetMembers("C");
        Assert.Equal(3, cs.Length);
        Assert.True(cs[0] is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.True(cs[1] is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.True(cs[2] is SourceMemberContainerTypeSymbol { IsFileLocal: false });
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

        var compilation = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs"), (source3, "file3.cs") });
        compilation.VerifyDiagnostics();

        var namespaceNS = compilation.GetMember<NamespaceSymbol>("NS");
        var cs = namespaceNS.GetMembers("C");
        Assert.Equal(3, cs.Length);
        Assert.True(cs[0] is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.True(cs[1] is SourceMemberContainerTypeSymbol { IsFileLocal: true });
        Assert.True(cs[2] is SourceMemberContainerTypeSymbol { IsFileLocal: false });
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

        var comp = CreateCompilation(new[] { (source1 + main, "file1.cs"), (source2, "file2.cs") });
        var cs = comp.GetMembers("Program.C");
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        verify();

        comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2 + main, "file2.cs") });
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

        var comp = CreateCompilation(new[] { (source1 + main, "file1.cs"), (source2, "file2.cs") }, options: TestOptions.DebugExe);
        comp.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_FileTypeNested).Verify();
        var outers = comp.GetMembers("Outer");
        var cs = outers.Select(o => ((NamedTypeSymbol)o).GetMember("C")).ToArray();
        var tree = comp.SyntaxTrees[0];
        var expectedSymbol = cs[0];
        verify();

        comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2 + main, "file2.cs") }, options: TestOptions.DebugExe);
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

        var verifier = CompileAndVerify(new[] { (userCode, "file1.cs"), (generatedCode, "file2.cs") }, expectedOutput: "OtherFile.csProgram.cs");
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

        var verifier = CompileAndVerify(new[] { (userCode, "file1.cs"), (generatedCode, "file2.cs") }, expectedOutput: "OtherFile.csProgram.cs");
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

        var verifier = CompileAndVerify(new[] { (userCode, "file1.cs"), (generatedCode, "file2.cs") }, expectedOutput: "OtherFile.cs");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void Duplication_16()
    {
        var source = """
            namespace NS;

            file class C { }
            class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,7): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(4, 7));
    }

    [Fact]
    public void Duplication_17()
    {
        var source = """
            namespace NS;

            class C { }
            file class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,12): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // file class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(4, 12));
    }

    [Fact]
    public void Duplication_18()
    {
        var source = """
            namespace NS;

            file class C { }
            class C { }
            class C { }
            """;

        var comp = CreateCompilation((source, "file1.cs"));
        comp.VerifyDiagnostics(
            // file1.cs(4,7): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(4, 7),
            // file1.cs(5,7): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(5, 7));
    }

    [Fact]
    public void Duplication_19()
    {
        var source = """
            namespace NS;

            class C { }
            file class C { }
            class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (4,12): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // file class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(4, 12),
            // (5,7): error CS9070: The namespace 'NS' already contains a definition for 'C' in this file.
            // class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "NS").WithLocation(5, 7));
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
            // (7,17): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public void M1(C c) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 17),
            // (8,18): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
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
            // (7,14): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public C M1() => new C(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 14),
            // (8,15): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
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
            // (8,7): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'E'.
            //     C field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C", "E").WithLocation(8, 7),
            // (8,7): warning CS0169: The field 'E.field' is never used
            //     C field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(8, 7),
            // (9,7): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'E'.
            //     C property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C", "E").WithLocation(9, 7),
            // (10,12): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'E'.
            //     object this[C c] { get => c; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C", "E").WithLocation(10, 12),
            // (11,13): error CS9051: File-local type 'D' cannot be used in a member signature in non-file-local type 'E'.
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
            // (9,13): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E'.
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E").WithLocation(9, 13),
            // (9,13): warning CS0169: The field 'E.field' is never used
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(9, 13),
            // (10,13): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E'.
            //     C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E").WithLocation(10, 13),
            // (11,12): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E'.
            //     object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E").WithLocation(11, 12),
            // (12,27): error CS9051: File-local type 'C.InnerDelegate' cannot be used in a member signature in non-file-local type 'E'.
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
            // (24,17): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E.Inner'.
            //         C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E.Inner").WithLocation(24, 17),
            // (25,17): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E.Inner'.
            //         C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E.Inner").WithLocation(25, 17),
            // (26,16): error CS9051: File-local type 'C.Inner' cannot be used in a member signature in non-file-local type 'E.Inner'.
            //         object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E.Inner").WithLocation(26, 16),
            // (27,31): error CS9051: File-local type 'C.InnerDelegate' cannot be used in a member signature in non-file-local type 'E.Inner'.
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
            // (5,15): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'Del1'.
            // delegate void Del1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del1").WithArguments("C", "Del1").WithLocation(5, 15),
            // (6,12): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'Del2'.
            // delegate C Del2(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del2").WithArguments("C", "Del2").WithLocation(6, 12));
    }

    [Fact]
    public void SignatureUsage_06_2()
    {
        var source = """
            file class C<T>
            {
            }

            delegate void Del1(C<int> c); // 1
            delegate C<int> Del2(); // 2

            file delegate void Del3(C<int> c); // ok
            file delegate C<int> Del4(); // ok
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,15): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'Del1'.
            // delegate void Del1(C<int> c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del1").WithArguments("C<int>", "Del1").WithLocation(5, 15),
            // (6,17): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'Del2'.
            // delegate C<int> Del2(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del2").WithArguments("C<int>", "Del2").WithLocation(6, 17));

        var del1 = comp.GetMember<NamedTypeSymbol>("Del1");
        var cInt = (ConstructedNamedTypeSymbol)del1.DelegateInvokeMethod.Parameters[0].Type;
        Assert.True(cInt.IsFileLocal);
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
            // (7,30): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public static D operator +(D d, C c) => d; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("C", "D").WithLocation(7, 30),
            // (8,30): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
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
            // (7,12): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
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
            // (7,14): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14));
    }

    [Fact]
    public void SignatureUsage_10()
    {
        var source = """
            #pragma warning disable 67, 169 // unused event, field

            file class C<T> { }
            file delegate void Del<T>(T input);

            class C1
            {
                private C<int> F; // 1
                private event Del<int> E; // 2
                private void M1(C<int> input) { } // 3
                private C<int> M2() => throw null!; // 4

                private C<int> P { get; set; } // 5
                private C<int> this[int i] => throw null!; // 6
            }

            file class FC
            {
                private C<int> F;
                private event Del<int> E;
                private void M1(C<int> input) { }
                private C<int> M2() => throw null!;

                private C<int> P { get; set; }
                private C<int> this[int i] => throw null!;
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (8,20): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private C<int> F; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "F").WithArguments("C<int>", "C1").WithLocation(8, 20),
            // (9,28): error CS9051: File-local type 'Del<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private event Del<int> E; // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "E").WithArguments("Del<int>", "C1").WithLocation(9, 28),
            // (10,18): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private void M1(C<int> input) { } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C<int>", "C1").WithLocation(10, 18),
            // (11,20): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private C<int> M2() => throw null!; // 4
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("C<int>", "C1").WithLocation(11, 20),
            // (13,20): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private C<int> P { get; set; } // 5
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "P").WithArguments("C<int>", "C1").WithLocation(13, 20),
            // (14,20): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'C1'.
            //     private C<int> this[int i] => throw null!; // 6
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C<int>", "C1").WithLocation(14, 20));

        verifyConstructedFileType(comp.GetMember<FieldSymbol>("C1.F").Type);
        verifyConstructedFileType(comp.GetMember<EventSymbol>("C1.E").Type);
        verifyConstructedFileType(comp.GetMember<MethodSymbol>("C1.M1").Parameters[0].Type);
        verifyConstructedFileType(comp.GetMember<MethodSymbol>("C1.M2").ReturnType);
        verifyConstructedFileType(comp.GetMember<PropertySymbol>("C1.P").Type);
        verifyConstructedFileType(comp.GetMember<PropertySymbol>("C1.this[]").Type);

        void verifyConstructedFileType(TypeSymbol type)
        {
            var cInt = (ConstructedNamedTypeSymbol)type;
            Assert.True(cInt.IsFileLocal);
        }
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
            // (1,19): error CS9052: File-local type 'C' cannot use accessibility modifiers.
            // public file class C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "C").WithArguments("C").WithLocation(1, 19),
            // (2,21): error CS9052: File-local type 'D' cannot use accessibility modifiers.
            // file internal class D { } // 2
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "D").WithArguments("D").WithLocation(2, 21),
            // (3,20): error CS9052: File-local type 'E' cannot use accessibility modifiers.
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
            // (2,7): error CS9053: File-local type 'Base' cannot be used as a base type of non-file-local type 'Derived1'.
            // class Derived1 : Base { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived1").WithArguments("Base", "Derived1").WithLocation(2, 7),
            // (3,14): error CS0060: Inconsistent accessibility: base class 'Base' is less accessible than class 'Derived2'
            // public class Derived2 : Base { } // 2, 3
            Diagnostic(ErrorCode.ERR_BadVisBaseClass, "Derived2").WithArguments("Derived2", "Base").WithLocation(3, 14),
            // (3,14): error CS9053: File-local type 'Base' cannot be used as a base type of non-file-local type 'Derived2'.
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
            // (6,11): error CS9053: File-local type 'Interface' cannot be used as a base type of non-file-local type 'Derived3'.
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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2, "file2.cs") }, expectedOutput: "2");
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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2, "file2.cs") }, expectedOutput: "1");
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
            // (3,19): error CS9053: File-local type 'I1' cannot be used as a base type of non-file-local type 'Derived'.
            // partial interface Derived : I1 { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived").WithArguments("I1", "Derived").WithLocation(3, 19));
    }

    [Fact]
    public void BaseClause_06()
    {
        var source = """
        file class C<T> { }

        class D : C<int> { } // 1
        file class E : C<int> { }

        file interface I<T> { }

        class F : I<int> { } // ok
        file class G : I<int> { }

        interface J : I<int> { } // 2
        file interface K : I<int> { }
        """;

        var comp = CreateCompilation((source, "Program.cs"));
        comp.VerifyEmitDiagnostics(
            // Program.cs(3,7): error CS9053: File-local type 'C<int>' cannot be used as a base type of non-file-local type 'D'.
            // class D : C<int>, I<int> { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "D").WithArguments("C<int>", "D").WithLocation(3, 7),
            // Program.cs(11,11): error CS9053: File-local type 'I<int>' cannot be used as a base type of non-file-local type 'J'.
            // interface J : I<int> { } // 2
            Diagnostic(ErrorCode.ERR_FileTypeBase, "J").WithArguments("I<int>", "J").WithLocation(11, 11));

        var cInt = (ConstructedNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("D").BaseTypeNoUseSiteDiagnostics;
        Assert.True(cInt.IsFileLocal);

        var iInt = (ConstructedNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("F").InterfacesNoUseSiteDiagnostics()[0];
        Assert.True(iInt.IsFileLocal);

        iInt = (ConstructedNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("J").InterfacesNoUseSiteDiagnostics()[0];
        Assert.True(iInt.IsFileLocal);
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
            // (7,17): error CS9051: File-local type 'I' cannot be used in a member signature in non-file-local type 'C'.
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
            // (7,12): error CS9051: File-local type 'I' cannot be used in a member signature in non-file-local type 'C'.
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
        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void InterfaceImplementation_06()
    {
        // Ensure that appropriate error is given for duplicate implementations which have a type difference which is insignificant to the runtime.
        var source1 = """
            file interface FI<T>
            {
                public T Prop { get; }
            }

            internal class C : FI<object>
            {
                object FI<object>.Prop { get; }
                dynamic FI<dynamic>.Prop { get; }
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // F1.cs(6,16): error CS8646: 'FI<object>.Prop' is explicitly implemented more than once.
            // internal class C : FI<object>
            Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("FI<object>.Prop").WithLocation(6, 16),
            // F1.cs(9,13): error CS0540: 'C.FI<dynamic>.Prop': containing type does not implement interface 'FI<dynamic>'
            //     dynamic FI<dynamic>.Prop { get; }
            Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "FI<dynamic>").WithArguments("C.FI<dynamic>.Prop", "FI<dynamic>").WithLocation(9, 13)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void InterfaceImplementation_07()
    {
        // Ensure that appropriate error is given for duplicate implementations which have a type difference which is insignificant to the runtime.
        var source1 = """
            using System;

            file interface FI<T>
            {
                public T Prop { get; }
            }

            internal class C : FI<nint>, FI<IntPtr>
            {
                nint FI<nint>.Prop { get; }
                IntPtr FI<IntPtr>.Prop { get; }
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // F1.cs(8,16): error CS8646: 'FI<nint>.Prop' is explicitly implemented more than once.
            // internal class C : FI<nint>, FI<IntPtr>
            Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("FI<nint>.Prop").WithLocation(8, 16),
            // F1.cs(8,30): error CS0528: 'FI<nint>' is already listed in interface list
            // internal class C : FI<nint>, FI<IntPtr>
            Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "FI<IntPtr>").WithArguments("FI<nint>").WithLocation(8, 30),
            // F1.cs(11,23): error CS0102: The type 'C' already contains a definition for '<F1>F2A62B10769F2595F65CAD631A41E2B54F5D1B3601B00884A41306FA9AD9BACDB__FI<nint>.Prop'
            //     IntPtr FI<IntPtr>.Prop { get; }
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Prop").WithArguments("C", "<F1>F2A62B10769F2595F65CAD631A41E2B54F5D1B3601B00884A41306FA9AD9BACDB__FI<nint>.Prop").WithLocation(11, 23)
            );
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
                // (5,18): error CS9051: File-local type 'Container<S>' cannot be used in a member signature in non-file-local type 'Program'.
                //     Container<S> M1() => new Container<S>(); // 1
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("Container<S>", "Program").WithLocation(5, 18),
                // (6,9): error CS9051: File-local type 'S[]' cannot be used in a member signature in non-file-local type 'Program'.
                //     S[] M2() => new S[0]; // 2
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("S[]", "Program").WithLocation(6, 9),
                // (7,12): error CS9051: File-local type '(S, S)' cannot be used in a member signature in non-file-local type 'Program'.
                //     (S, S) M3() => (new S(), new S()); // 3
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M3").WithArguments("(S, S)", "Program").WithLocation(7, 12),
                // (8,8): error CS9051: File-local type 'S*' cannot be used in a member signature in non-file-local type 'Program'.
                //     S* M4() => null; // 4
                Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M4").WithArguments("S*", "Program").WithLocation(8, 8),
                // (9,24): error CS9051: File-local type 'delegate*<S, void>' cannot be used in a member signature in non-file-local type 'Program'.
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
            // (10,30): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'E.M<T>(T)'.
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
            // (7,{{17 + typeKind.Length}}): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'E<T>'.
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
            // (5,36): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'D2<T>'.
            // delegate void D2<T>(T t) where T : C; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C").WithArguments("C", "D2<T>").WithLocation(5, 36));
    }

    [Fact]
    public void Constraints_05()
    {
        var source = """
            file class C<T> { }

            class D
            {
                private void M<T>(T t) where T : C<int> { } // 1
            }

            file class E
            {
                private void M<T>(T t) where T : C<int> { } // ok
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,38): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'D.M<T>(T)'.
            //     private void M<T>(T t) where T : C<int> { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C<int>").WithArguments("C<int>", "D.M<T>(T)").WithLocation(5, 38));

        var cInt = (ConstructedNamedTypeSymbol)comp.GetMember<MethodSymbol>("D.M").TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].Type;
        Assert.True(cInt.IsFileLocal);
    }

    [Fact]
    public void Constraints_06()
    {
        var source = """
            file class C<T> { }

            class D<T> where T : C<int> { } // 1

            file class E<T> where T : C<int> { } // ok
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (3,22): error CS9051: File-local type 'C<int>' cannot be used in a member signature in non-file-local type 'D<T>'.
            // class D<T> where T : C<int> { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C<int>").WithArguments("C<int>", "D<T>").WithLocation(3, 22));

        var cInt = (ConstructedNamedTypeSymbol)comp.GetMember<NamedTypeSymbol>("D").TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0].Type;
        Assert.True(cInt.IsFileLocal);
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

        var comp = CreateCompilation(new[] { (source, "file1.cs"), (IsExternalInitTypeDefinition, "file2.cs") });
        comp.VerifyDiagnostics(
            // (3,8): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (3,8): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (4,15): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'R2'.
            // record struct R2(C c); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R2").WithArguments("C", "R2").WithLocation(4, 15),
            // (4,15): error CS9051: File-local type 'C' cannot be used in a member signature in non-file-local type 'R2'.
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
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
            // (5,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
        comp.VerifyDiagnostics(
            // (5,15): error CS0117: 'Outer' does not contain a definition for 'C'
            //         Outer.C.M(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMember, "C").WithArguments("Outer", "C").WithLocation(5, 15),
            // (5,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
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

        var verifier = CompileAndVerify(new[] { (usings, "file1.cs"), (source, "file2.cs"), (IsExternalInitTypeDefinition, "file3.cs") }, expectedOutput: "1");
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
        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (IsExternalInitTypeDefinition, "file2.cs") }, options: TestOptions.DebugExe.WithUsings("NS"));
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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") });
        compilation.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // global using static C;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C;").WithLocation(1, 1),
                // (1,21): error CS9055: File-local type 'C' cannot be used in a 'global using static' directive.
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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") });
        compilation.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // global using static Container<C>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static Container<C>;").WithLocation(1, 1),
                // (1,21): error CS9055: File-local type 'Container<C>' cannot be used in a 'global using static' directive.
                // global using static Container<C>;
                Diagnostic(ErrorCode.ERR_GlobalUsingStaticFileType, "Container<C>").WithArguments("Container<C>").WithLocation(1, 21),
                // (5,9): error CS0103: The name 'M' does not exist in the current context
                //         M();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(5, 9));
    }

    [Fact]
    public void GlobalUsingStatic_03()
    {
        var source = """
            global using static C<int>;

            file class C<T>
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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") });
        compilation.VerifyDiagnostics(
            // file1.cs(1,1): hidden CS8019: Unnecessary using directive.
            // global using static C<int>;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "global using static C<int>;").WithLocation(1, 1),
            // file1.cs(1,21): error CS9055: File-local type 'C<int>' cannot be used in a 'global using static' directive.
            // global using static C<int>;
            Diagnostic(ErrorCode.ERR_GlobalUsingStaticFileType, "C<int>").WithArguments("C<int>").WithLocation(1, 21),
            // file2.cs(5,9): error CS0103: The name 'M' does not exist in the current context
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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2, "file2.cs") }, expectedOutput: "1");
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

        var verifier = CompileAndVerify(new[] { (source1, "file1.cs"), (source2, "file2.cs") }, expectedOutput: "2");
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
        var compilation = CreateCompilation(new[] { (source, "file.cs"), (main, "file2.cs") });
        compilation.VerifyDiagnostics(
            // (16,20): error CS9054: File-local type 'Derived.C' must be defined in a top level type; 'Derived.C' is a nested type.
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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") });
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
        Assert.Equal("NS.C@file1", classC.ToTestDisplayString());

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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") });
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

        var compilation = CreateCompilation(new[] { (source, "file1.cs"), (main, "file2.cs") }, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
        comp.VerifyDiagnostics(
            // (1,5): error CS1061: 'string' does not contain a definition for 'M' and no accessible extension method 'M' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
            // "a".M(); // 1
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "M").WithArguments("string", "M").WithLocation(1, 5));

        var tree = comp.SyntaxTrees[0];
        var methodNameSyntax = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Single();
        var model = comp.GetSemanticModel(tree);

        var symbolInfo = model.GetSymbolInfo(methodNameSyntax);
        Assert.Null(symbolInfo.Symbol);
        Assert.Empty(symbolInfo.CandidateSymbols);
        Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

        var aliasInfo = model.GetAliasInfo(methodNameSyntax);
        Assert.Null(aliasInfo);
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

        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
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
            // (8,7): error CS9053: File-local type 'C' cannot be used as a base type of non-file-local type 'D'.
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
            // (5,19): error CS9054: File-local type 'C1' must be defined in a top level type; 'C1' is a nested type.
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
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C");
        Assert.Equal(sourceMember, sourceType);

        Assert.Null(comp.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__D"));
        Assert.Null(comp.GetTypeByMetadataName("<>F1__C"));
        Assert.Null(comp.GetTypeByMetadataName("F0__C"));
        Assert.Null(comp.GetTypeByMetadataName("<file>F0__C"));
        Assert.Null(comp.GetTypeByMetadataName("C"));
        Assert.Null(comp.GetTypeByMetadataName("C`1"));

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();
        var metadataMember = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C");
        Assert.Equal(metadataMember, metadataType);

        Assert.Null(comp2.GetTypeByMetadataName("C"));
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
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C`1", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C`1");
        Assert.Equal(sourceMember, sourceType);
        Assert.Null(comp.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C"));
        Assert.Null(comp.GetTypeByMetadataName("C"));
        Assert.Null(comp.GetTypeByMetadataName("C`1"));

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var metadataMember = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C`1", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C`1");
        Assert.Equal(metadataMember, metadataType);

        Assert.Null(comp2.GetTypeByMetadataName("C`1"));
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
            // (3,16): error CS9054: File-local type 'Outer.C' must be defined in a top level type; 'Outer.C' is a nested type.
            //     file class C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNested, "C").WithArguments("Outer.C").WithLocation(3, 16));
        var sourceMember = comp.GetMember<NamedTypeSymbol>("Outer.C");
        Assert.Equal("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("Outer.<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C");
        // Note: strictly speaking, it would be reasonable to return the (invalid) nested file-local type symbol here.
        // However, since we don't actually support nested file types, we don't think we need the API to do the additional lookup
        // when the requested type is nested, and so we end up giving a null here.
        Assert.Null(sourceType);
        Assert.Null(comp.GetTypeByMetadataName("Outer.C"));
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
        var comp = CreateCompilation(new[] { (source1, "file1.cs"), (source2, "file2.cs") });
        comp.VerifyDiagnostics();
        var sourceMember = comp.GetMembers("C")[0];
        AssertEx.Equal("<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C", sourceMember.MetadataName);

        var sourceType = comp.GetTypeByMetadataName("<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C");
        Assert.Equal(sourceMember, sourceType);

        var sourceTypeCByMetadataName = comp.GetTypeByMetadataName("C");
        Assert.NotNull(sourceTypeCByMetadataName);
        Assert.Equal("C", sourceTypeCByMetadataName.MetadataName);
        Assert.False(sourceTypeCByMetadataName is SourceMemberContainerTypeSymbol { IsFileLocal: true });

        // from metadata
        var comp2 = CreateCompilation("", references: new[] { comp.EmitToImageReference() });
        comp2.VerifyDiagnostics();

        var metadataMember = comp2.GetMembers("C")[0];
        Assert.Equal("<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C", metadataMember.MetadataName);

        var metadataType = comp2.GetTypeByMetadataName("<file1>F96B1D9CB33A43D51528FE81EDAFE5AE31358FE749929AC76B76C64B60DEF129D__C");
        Assert.Equal(metadataMember, metadataType);

        var metadataTypeCByMetadataName = comp2.GetTypeByMetadataName("C");
        Assert.NotNull(metadataTypeCByMetadataName);
        Assert.Equal("C", metadataTypeCByMetadataName.MetadataName);
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

        const string metadataName = "<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C";
        var sourceType = comp.GetTypeByMetadataName(metadataName);
        Assert.Null(sourceType);
        Assert.Null(comp.GetTypeByMetadataName("C"));

        var types = comp.GetTypesByMetadataName(metadataName);
        Assert.Equal(2, types.Length);
        Assert.Equal(firstIsMetadataReference ? "C@<tree 0>" : "C@<unknown>", types[0].ToTestDisplayString());
        Assert.Equal(secondIsMetadataReference ? "C@<tree 0>" : "C@<unknown>", types[1].ToTestDisplayString());
        Assert.NotEqual(types[0], types[1]);

        Assert.Empty(comp.GetTypesByMetadataName("C"));
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
            // (2,12): error CS9070: The namespace '<global namespace>' already contains a definition for 'C' in this file.
            // file class C { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "C").WithArguments("C", "<global namespace>").WithLocation(2, 12));

        const string metadataName = "<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C";
        var sourceType = ((Compilation)comp).GetTypeByMetadataName(metadataName);
        Assert.Equal("C@<tree 0>", sourceType.ToTestDisplayString());

        Assert.Null(((Compilation)comp).GetTypeByMetadataName("C"));

        var types = comp.GetTypesByMetadataName(metadataName);
        Assert.Equal(1, types.Length);
        Assert.Same(sourceType, types[0]);

        Assert.Empty(comp.GetTypesByMetadataName("C"));
    }

    [Fact]
    public void GetTypeByMetadataName_07()
    {
        var source1 = """
            file class C { }
            """;

        var comp = CreateCompilation(SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreview, path: "path/to/SomeFile.cs"));
        comp.VerifyDiagnostics();

        const string checksum = "0146C6A4DC0D382DC3D534F34EF202BE3FAF72EE35E08C8382B730D5270B6585";
        Assert.Null(comp.GetTypeByMetadataName($"<>F{checksum}__C"));
        Assert.Empty(comp.GetTypesByMetadataName($"<>F{checksum}__C"));

        Assert.Null(comp.GetTypeByMetadataName($"<WrongName>F{checksum}__C"));
        Assert.Empty(comp.GetTypesByMetadataName($"<WrongName>F{checksum}__C"));

        var sourceType = ((Compilation)comp).GetTypeByMetadataName($"<SomeFile>F{checksum}__C");
        Assert.Equal("C@SomeFile", sourceType.ToTestDisplayString());

        var types = comp.GetTypesByMetadataName($"<SomeFile>F{checksum}__C");
        Assert.Equal(1, types.Length);
        Assert.Same(sourceType, types[0]);
    }

    [Fact]
    public void GetTypeByMetadataName_08()
    {
        var source1 = """
            file class C { public static void M() { } }
            """;

        var comp = CreateCompilation(source1, targetFramework: TargetFramework.Mscorlib40);
        comp.VerifyDiagnostics();

        const string metadataName = "<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C";

        var member = comp.GetMember<NamedTypeSymbol>("C");
        Assert.Equal(metadataName, member.MetadataName);

        Assert.Null(comp.GetTypeByMetadataName("C"));
        Assert.Equal(member, comp.GetTypeByMetadataName(metadataName));

        var source2 = """
            class C2
            {
                void M()
                {
                    C.M();
                }
            }
            """;

        var comp2 = CreateCompilation(source2, references: new[] { comp.ToMetadataReference() }, targetFramework: TargetFramework.Mscorlib461);
        comp2.VerifyDiagnostics(
        // (5,9): error CS0103: The name 'C' does not exist in the current context
        //         C.M();
        Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(5, 9)
        );

        Assert.NotEqual(comp.Assembly.CorLibrary, comp2.Assembly.CorLibrary);

        var retargeted = comp2.GetMember<NamedTypeSymbol>("C");
        Assert.IsType<RetargetingNamedTypeSymbol>(retargeted);
        Assert.Equal(metadataName, retargeted.MetadataName);

        Assert.Null(comp2.GetTypeByMetadataName("C"));
        Assert.Equal(retargeted, comp2.GetTypeByMetadataName(metadataName));
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

        var expectedChecksum = new byte[] { 0xE3, 0xB0, 0xC4, 0x42, 0x98, 0xFC, 0x1C, 0x14, 0x9A, 0xFB, 0xF4, 0xC8, 0x99, 0x6F, 0xB9, 0x24, 0x27, 0xAE, 0x41, 0xE4, 0x64, 0x9B, 0x93, 0x4C, 0xA4, 0x95, 0x99, 0x1B, 0x78, 0x52, 0xB8, 0x55 };
        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        var type = (INamedTypeSymbol)model.GetTypeInfo(node.Type!).Type!;
        Assert.Equal("C@<tree 0>", type.ToTestDisplayString());
        var identifier = type.GetSymbol()!.AssociatedFileIdentifier;
        Assert.NotNull(identifier);
        AssertEx.Equal(expectedChecksum, identifier.FilePathChecksumOpt);
        Assert.Empty(identifier.DisplayFilePath);
        Assert.True(type.IsFileLocal);

        var referencingMetadataComp = CreateCompilation("", new[] { comp.ToMetadataReference() });
        type = ((Compilation)referencingMetadataComp).GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C")!;
        Assert.Equal("C@<tree 0>", type.ToTestDisplayString());
        identifier = type.GetSymbol()!.AssociatedFileIdentifier;
        Assert.NotNull(identifier);
        AssertEx.Equal(expectedChecksum, identifier.FilePathChecksumOpt);
        Assert.Empty(identifier.DisplayFilePath);
        Assert.True(type.IsFileLocal);

        var referencingImageComp = CreateCompilation("", new[] { comp.EmitToImageReference() });
        type = ((Compilation)referencingImageComp).GetTypeByMetadataName("<>FE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855__C")!;
        Assert.Equal("C@<unknown>", type.ToTestDisplayString());
        identifier = type.GetSymbol()!.AssociatedFileIdentifier;
        Assert.NotNull(identifier);
        AssertEx.Equal(expectedChecksum, identifier.FilePathChecksumOpt);
        Assert.Empty(identifier.DisplayFilePath);
        Assert.False(type.IsFileLocal);
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
        Assert.Null(type.GetSymbol()!.AssociatedFileIdentifier);
        Assert.False(type.IsFileLocal);
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
        var identifier = type.GetSymbol()!.AssociatedFileIdentifier;
        Assert.NotNull(identifier);
        AssertEx.Equal(
            new byte[] { 0xE3, 0xB0, 0xC4, 0x42, 0x98, 0xFC, 0x1C, 0x14, 0x9A, 0xFB, 0xF4, 0xC8, 0x99, 0x6F, 0xB9, 0x24, 0x27, 0xAE, 0x41, 0xE4, 0x64, 0x9B, 0x93, 0x4C, 0xA4, 0x95, 0x99, 0x1B, 0x78, 0x52, 0xB8, 0x55 },
            identifier.FilePathChecksumOpt);
        Assert.Empty(identifier.DisplayFilePath);
        Assert.True(type.IsFileLocal);
    }

    [Theory]
    [CombinatorialData]
    public void CannotAccessFromMetadata_01(bool useMetadataReference)
    {
        // Compare to 'InternalsVisibleToAndStrongNameTests.IVTBasicMetadata'
        var fileTypeSource = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("HasIVTAccess")]
            file class C1 { public static void M1() {} }
            """;

        // reused across all compilations to try and trick the binder into thinking it's binding from the same file as 'file class C'.
        var filePath = "file1.cs";

        var comp0 = CreateCompilation((fileTypeSource, filePath), options: TestOptions.SigningReleaseDll);
        comp0.VerifyDiagnostics();

        var reference = useMetadataReference ? comp0.ToMetadataReference() : comp0.EmitToImageReference();

        var useFileTypeSource = """
            class C2
            {
                void M2()
                {
                    C1.M1();
                }
            }
            """;

        // Whether or not you have an IVT, the compiler won't bind to a file type from a different compilation.
        verify("DoesNotHaveIVTAccess");
        verify("HasIVTAccess");
        void verify(string assemblyName)
        {
            var comp1 = CreateCompilation(
                (useFileTypeSource, filePath),
                references: new[] { reference },
                assemblyName: assemblyName,
                options: TestOptions.SigningReleaseDll);
            comp1.VerifyDiagnostics(
                // file1.cs(5,9): error CS0103: The name 'C1' does not exist in the current context
                //         C1.M1();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "C1").WithArguments("C1").WithLocation(5, 9));
        }
    }

    [Fact]
    public void CannotAccessFromMetadata_02()
    {
        var fileTypeSource = """
            file class C1 { public static void M1() {} }
            """;

        // reused across all compilations to try and trick the binder into thinking it's binding from the same file
        var filePath = "file1.cs";

        var comp0 = CreateCompilation((fileTypeSource, filePath), options: TestOptions.SigningReleaseDll);
        comp0.VerifyDiagnostics();
        var classC1 = comp0.GetMember<NamedTypeSymbol>("C1");
        Assert.True(classC1.GetPublicSymbol().IsFileLocal);

        var reference = comp0.ToMetadataReference();

        var useFileTypeSource = """
            class C2
            {
                void M2()
                {
                    C1.M1();
                }
            }
            """;

        var comp1 = CreateCompilation(
            (useFileTypeSource, filePath),
            references: new[] { reference },
            targetFramework: TargetFramework.Mscorlib461,
            options: TestOptions.SigningReleaseDll);
        comp1.VerifyDiagnostics(
            // file1.cs(5,9): error CS0103: The name 'C1' does not exist in the current context
            //         C1.M1();
            Diagnostic(ErrorCode.ERR_NameNotInContext, "C1").WithArguments("C1").WithLocation(5, 9));
        var retargeted = comp1.GetMember<NamedTypeSymbol>("C1");
        Assert.IsType<RetargetingNamedTypeSymbol>(retargeted);
        Assert.False(retargeted.GetPublicSymbol().IsFileLocal);

        var originalFileIdentifier = classC1.AssociatedFileIdentifier!;
        var retargetedFileIdentifier = retargeted.AssociatedFileIdentifier!;
        Assert.Equal(originalFileIdentifier.DisplayFilePath, retargetedFileIdentifier.DisplayFilePath);
        Assert.Equal((IEnumerable<byte>)originalFileIdentifier.FilePathChecksumOpt, (IEnumerable<byte>)retargetedFileIdentifier.FilePathChecksumOpt);
        Assert.Equal(originalFileIdentifier.EncoderFallbackErrorMessage, retargetedFileIdentifier.EncoderFallbackErrorMessage);
    }

    [Fact]
    public void SyntaxTreeAlreadyPresent()
    {
        var tree = SyntaxFactory.ParseSyntaxTree("""
            partial file class C { }
            """,
            path: "file1.cs",
            encoding: Encoding.Default);

        var ex = Assert.Throws<ArgumentException>(() => CreateCompilation(new[] { tree, tree }));
        Assert.Equal("trees[1]", ex.ParamName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_01()
    {
        var source0 = """
            var c = new C();
            c.Use1();
            c.Use2();
            """;

        var source1 = """
            using System;

            file interface FI
            {
                void M();
            }

            partial class C : FI
            {
                void FI.M() { Console.Write(1); }

                public void Use1() { ((FI)this).M(); }
            }
            """;

        var source2 = """
            using System;

            file interface FI
            {
                void M();
            }

            partial class C : FI
            {
                void FI.M() { Console.Write(2); }

                public void Use2() { ((FI)this).M(); }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "F0.cs"), (source1, "F1.cs"), (source2, "F2.cs") }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_02()
    {
        var source1 = """
            file interface FI
            {
                void M();
            }

            partial class C : FI
            {
                void FI.M() => throw null!;
            }
            """;

        // Explicit implementation of 'FI.M()' in 'source1' does not implement 'FI.M()' in 'source2'.
        var source2 = """
            file interface FI
            {
                void M();
            }

            partial class C : FI
            {
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") });
        comp.VerifyDiagnostics(
            // F2.cs(6,19): error CS0535: 'C' does not implement interface member 'FI.M()'
            // partial class C : FI
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "FI").WithArguments("C", "FI.M()").WithLocation(6, 19));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_03()
    {
        var source1 = """
            using System.Collections.Generic;

            file interface I
            {
                IReadOnlyDictionary<int, string> P { get; }
            }

            internal partial class C : I
            {
                private readonly Dictionary<int, string> _p = new() { { 1, "one" }, { 2, "two" } };
                IReadOnlyDictionary<int, string> I.P => _p;
            }
            """;

        var source2 = """
            using System.Collections.Generic;

            file interface I
            {
                IReadOnlyDictionary<int, string> P { get; }
            }

            internal partial class C : I
            {
                IReadOnlyDictionary<int, string> I.P => _p;
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") });
        comp.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_04()
    {
        var source1 = """
            file interface I
            {
                int P { get; }
            }

            internal partial class C : I
            {
            }
            """;

        var source2 = """
            file interface I
            {
                int P { get; }
            }

            internal partial class C : I
            {
                int I.P => 1;
                int I.P => 2;
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") });
        comp.VerifyDiagnostics(
            // F1.cs(6,24): error CS8646: 'I.P' is explicitly implemented more than once.
            // internal partial class C : I
            Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.P").WithLocation(6, 24),
            // F1.cs(6,28): error CS0535: 'C' does not implement interface member 'I.P'
            // internal partial class C : I
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "I").WithArguments("C", "I.P").WithLocation(6, 28),
            // F2.cs(9,11): error CS0102: The type 'C' already contains a definition for '<F2>F141A34209AF0D3C8CA844A7D9A360C895EB14E557F17D27626C519D9BE96AF4A__I.P'
            //     int I.P => 2;
            Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "<F2>F141A34209AF0D3C8CA844A7D9A360C895EB14E557F17D27626C519D9BE96AF4A__I.P").WithLocation(9, 11));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_05()
    {
        var source0 = """
            var c = new C();
            c.Use1();
            c.Use2();
            """;

        var source1 = """
            using System;

            file interface FI
            {
                int Bar { get; }
            }

            internal partial class C : FI
            {
                int FI.Bar => 1;
                public void Use1() => Console.Write(((FI)this).Bar);
            }
            """;

        var source2 = """
            using System;

            file interface FI
            {
                int Bar { get; }
            }

            internal partial class C : FI
            {
                int FI.Bar => 2;
                public void Use2() => Console.Write(((FI)this).Bar);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "F0.cs"), (source1, "F1.cs"), (source2, "F2.cs") }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_06()
    {
        var source0 = """
            var c = new C();
            c.Use1();
            c.Use2();
            """;

        var source1 = """
            using System;

            file interface FI
            {
                event Action E;
            }

            internal partial class C : FI
            {
                event Action FI.E { add { Console.Write(1); } remove { } }
                public void Use1() => ((FI)this).E += () => { };
            }
            """;

        var source2 = """
            using System;

            file interface FI
            {
                event Action E;
            }

            internal partial class C : FI
            {
                event Action FI.E { add { Console.Write(2); } remove { } }
                public void Use2() => ((FI)this).E += () => { };
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "F0.cs"), (source1, "F1.cs"), (source2, "F2.cs") }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_07()
    {
        var source0 = """
            var c = new C();
            c.Use1();
            c.Use2();
            """;

        var source1 = """
            using System;

            file interface FI
            {
                int this[int i] { get; }
            }

            internal partial class C : FI
            {
                int FI.this[int i] => 1;
                public void Use1() => Console.Write(((FI)this)[0]);
            }
            """;

        var source2 = """
            using System;

            file interface FI
            {
                int this[int i] { get; }
            }

            internal partial class C : FI
            {
                int FI.this[int i] => 2;
                public void Use2() => Console.Write(((FI)this)[0]);
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "F0.cs"), (source1, "F1.cs"), (source2, "F2.cs") }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_08()
    {
        // Test explicit implementation of a file interface operator in a partial type multiple times across files.
        // File types can't be used in signatures of non-file types, so this scenario isn't allowed currently,
        // but we'd like to make sure that redundant/invalid duplicate member name diagnostics aren't given here.
        var source1 = """
            file interface FI
            {
                static abstract int operator +(FI fi, int i);
            }

            internal partial class C : FI
            {
                static int FI.operator +(FI fi, int i) => throw null!; // 1
            }
            """;

        var source2 = """
            file interface FI
            {
                static abstract int operator +(FI fi, int i);
            }

            internal partial class C : FI
            {
                static int FI.operator +(FI fi, int i) => throw null!; // 2
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // F2.cs(8,28): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.operator +(FI fi, int i) => throw null!; // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("FI", "C").WithLocation(8, 28),
            // F1.cs(8,28): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.operator +(FI fi, int i) => throw null!; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("FI", "C").WithLocation(8, 28)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_09()
    {
        // Similar to PartialExplicitImplementation_08, except only one of the files contains duplicate operator implementations.
        var source1 = """
            file interface FI
            {
                static abstract int operator +(FI fi, int i);
            }

            internal partial class C : FI // 1, 2
            {
            }
            """;

        var source2 = """
            file interface FI
            {
                static abstract int operator +(FI fi, int i);
            }

            internal partial class C : FI
            {
                static int FI.operator +(FI fi, int i) => throw null!; // 3
                static int FI.operator +(FI fi, int i) => throw null!; // 4, 5
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // F1.cs(6,24): error CS8646: 'FI.operator +(FI, int)' is explicitly implemented more than once.
            // internal partial class C : FI // 1, 2
            Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("FI.operator +(FI, int)").WithLocation(6, 24),
            // F1.cs(6,28): error CS0535: 'C' does not implement interface member 'FI.operator +(FI, int)'
            // internal partial class C : FI // 1, 2
            Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "FI").WithArguments("C", "FI.operator +(FI, int)").WithLocation(6, 28),
            // F2.cs(8,28): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.operator +(FI fi, int i) => throw null!; // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("FI", "C").WithLocation(8, 28),
            // F2.cs(9,28): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.operator +(FI fi, int i) => throw null!; // 4, 5
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("FI", "C").WithLocation(9, 28),
            // F2.cs(9,28): error CS0111: Type 'C' already defines a member called '<F2>F141A34209AF0D3C8CA844A7D9A360C895EB14E557F17D27626C519D9BE96AF4A__FI.op_Addition' with the same parameter types
            //     static int FI.operator +(FI fi, int i) => throw null!; // 4, 5
            Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "+").WithArguments("<F2>F141A34209AF0D3C8CA844A7D9A360C895EB14E557F17D27626C519D9BE96AF4A__FI.op_Addition", "C").WithLocation(9, 28)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_10()
    {
        // Test explicit implementation of a file interface operator in a partial type.
        // In another file, implement a member in a type with the same source name, but with member name using the metadata name of the same operator.
        // File types can't be used in signatures of non-file types, so this scenario isn't allowed currently,
        // but we'd like to make sure that redundant/invalid duplicate member name diagnostics aren't given here.
        var source1 = """
            file interface FI
            {
                static abstract int operator +(FI fi, int i);
            }

            internal partial class C : FI
            {
                static int FI.operator +(FI fi, int i) => throw null!;
            }
            """;

        var source2 = """
            file interface FI
            {
                static abstract int op_Addition(FI fi, int i);
            }

            internal partial class C : FI
            {
                static int FI.op_Addition(FI fi, int i) => throw null!;
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "F1.cs"), (source2, "F2.cs") }, targetFramework: TargetFramework.Net70);
        comp.VerifyDiagnostics(
            // F2.cs(8,19): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.op_Addition(FI fi, int i) => throw null!;
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "op_Addition").WithArguments("FI", "C").WithLocation(8, 19),
            // F1.cs(8,28): error CS9051: File-local type 'FI' cannot be used in a member signature in non-file-local type 'C'.
            //     static int FI.operator +(FI fi, int i) => throw null!;
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("FI", "C").WithLocation(8, 28)
            );
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68219")]
    public void PartialExplicitImplementation_11()
    {
        var source0 = """
            var c = new C();
            c.Use1();
            c.Use2();

            interface I<T>
            {
                void M();
            }
            """;

        var source1 = """
            using System;

            file interface FI { }

            partial class C : I<FI>
            {
                void I<FI>.M() { Console.Write(1); }

                public void Use1() { ((I<FI>)this).M(); }
            }
            """;

        var source2 = """
            using System;

            file interface FI { }

            partial class C : I<FI>
            {
                void I<FI>.M() { Console.Write(2); }

                public void Use2() { ((I<FI>)this).M(); }
            }
            """;

        var verifier = CompileAndVerify(new[] { (source0, "F0.cs"), (source1, "F1.cs"), (source2, "F2.cs") }, expectedOutput: "12");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void ShadowNamespace_01()
    {
        var source1 = """
            namespace App.Widget
            {
                class Inner { }
            }

            """;

        var source2 = """
            namespace App
            {
                file class Widget { }
            }

            """;

        var comp = CreateCompilation(new[] { (source1, "File1.cs"), (source2, "File2.cs") });
        comp.VerifyDiagnostics();

        comp = CreateCompilation(source1 + source2);
        comp.VerifyDiagnostics(
            // (7,16): error CS9071: The namespace 'App' already contains a definition for 'Widget' in this file.
            //     file class Widget { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "Widget").WithArguments("Widget", "App").WithLocation(7, 16));

        comp = CreateCompilation(source2 + source1);
        comp.VerifyDiagnostics(
            // (3,16): error CS9071: The namespace 'App' already contains a definition for 'Widget' in this file.
            //     file class Widget { }
            Diagnostic(ErrorCode.ERR_FileLocalDuplicateNameInNS, "Widget").WithArguments("Widget", "App").WithLocation(3, 16));
    }

    [Theory, CombinatorialData]
    public void ShadowNamespace_02(bool useMetadataReference)
    {
        var source1 = """
            namespace App.Widget
            {
                public class Inner { }
            }

            """;

        var source2 = """
            namespace App
            {
                file class Widget { }
            }

            """;

        var comp1 = CreateCompilation(new[] { (source1, "File1.cs") });
        comp1.VerifyEmitDiagnostics();

        var comp2 = CreateCompilation(new[] { (source2, "File2.cs") }, references: new[] { useMetadataReference ? comp1.ToMetadataReference() : comp1.EmitToImageReference() });
        comp2.VerifyEmitDiagnostics();

        comp2 = CreateCompilation(new[] { (source2, "File2.cs") });
        comp2.VerifyEmitDiagnostics();

        comp1 = CreateCompilation(new[] { (source1, "File1.cs") }, references: new[] { useMetadataReference ? comp2.ToMetadataReference() : comp2.EmitToImageReference() });
        comp1.VerifyEmitDiagnostics();
    }

    [Fact]
    public void ShadowNamespace_03()
    {
        var source1 = """
            namespace App.Widget
            {
                class Inner { }
            }

            class C1
            {
                static void M1()
                {
                    new App.Widget(); // 1
                    new App.Widget.Inner();
                }
            }
            """;

        var source2 = """
            namespace App
            {
                file class Widget { }
            }

            class C2
            {
                static void M2()
                {
                    new App.Widget();
                    new App.Widget.Inner(); // 2
                }
            }
            """;

        var comp = CreateCompilation(new[] { (source1, "File1.cs"), (source2, "File2.cs") });
        comp.VerifyDiagnostics(
            // File1.cs(10,13): error CS0118: 'App.Widget' is a namespace but is used like a type
            //         new App.Widget(); // 1
            Diagnostic(ErrorCode.ERR_BadSKknown, "App.Widget").WithArguments("App.Widget", "namespace", "type").WithLocation(10, 13),
            // File2.cs(11,24): error CS0426: The type name 'Inner' does not exist in the type 'Widget'
            //         new App.Widget.Inner(); // 2
            Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Inner").WithArguments("Inner", "App.Widget").WithLocation(11, 24));
    }
}
