// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            // (1,12): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // file class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("file types").WithLocation(1, 12));

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
            // (3,16): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("file types").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
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
            // (1,12): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // file class Outer
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Outer").WithArguments("file types").WithLocation(1, 12));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
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

        // PROTOTYPE(ft): determine whether an inner file class within a file class is an error or if it's just fine.
        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
        comp.VerifyDiagnostics(
            // (1,12): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // file class Outer
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "Outer").WithArguments("file types").WithLocation(1, 12),
            // (3,16): error CS8652: The feature 'file types' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     file class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("file types").WithLocation(3, 16));

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
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
            // (8,10): error CS9300: File type 'Outer.C' cannot be used in a member signature in non-file type 'D'.
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
                    void M(Outer outer) { } // ok
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
    public void SameFileUse()
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

        var verifier = CompileAndVerify(source, expectedOutput: "1");
        verifier.VerifyDiagnostics();
        // PROTOTYPE(ft): check metadata names
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

    [Theory]
    [InlineData("file", "file")]
    [InlineData("file", "")]
    [InlineData("", "file")]
    public void Duplication_01(string firstFileModifier, string secondFileModifier)
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
                }
            }
            """;

        // PROTOTYPE(ft): execute and check expectedOutput once name mangling is done
        // expectedOutput: "1"
        var comp = CreateCompilation(new[] { source1 + main, source2 });
        verify();

        // expectedOutput: "2"
        comp = CreateCompilation(new[] { source1, source2 + main });
        verify();

        void verify()
        {
            comp.VerifyDiagnostics();

            var cs = comp.GetMembers("C");
            Assert.Equal(2, cs.Length);
            Assert.Equal(comp.SyntaxTrees[0], cs[0].DeclaringSyntaxReferences.Single().SyntaxTree);
            Assert.Equal(comp.SyntaxTrees[1], cs[1].DeclaringSyntaxReferences.Single().SyntaxTree);
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

        var comp = CreateCompilation(new[] { source1, source2, main }); // expectedOutput: 2
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

        var comp = CreateCompilation(new[] { source1, main }); // expectedOutput: 2
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

        var comp = CreateCompilation(new[] { source1, main }); // expectedOutput: 2
        comp.VerifyDiagnostics();

        var cs = comp.GetMembers("C");
        Assert.Equal(2, cs.Length);

        var c0 = cs[0];
        Assert.Equal(firstClassIsFile, ((SourceMemberContainerTypeSymbol)c0).IsFile);
        Assert.Equal(comp.SyntaxTrees[0], c0.DeclaringSyntaxReferences.Single().SyntaxTree);

        var c1 = cs[1];
        Assert.True(c1 is SourceMemberContainerTypeSymbol { IsFile: true });
        Assert.Equal(comp.SyntaxTrees[1], c1.DeclaringSyntaxReferences.Single().SyntaxTree);
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
        // PROTOTYPE(ft): should this diagnostic be more specific?
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
        // PROTOTYPE(ft): is it a problem that we consider this symbol a file type in this scenario?
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
        compilation.VerifyDiagnostics();

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
            // (7,17): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public void M1(C c) { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 17),
            // (8,18): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
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
            // (7,14): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M1() => new C(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M1").WithArguments("C", "D").WithLocation(7, 14),
            // (8,15): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     private C M2() => new C(); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M2").WithArguments("C", "D").WithLocation(8, 15));
    }

    // PROTOTYPE(ft): report error on indexer parameters
    // ensure that all cases where 'BadVis' errors are reported also check for 'file' mismatch
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
            // (8,7): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     C field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C", "E").WithLocation(8, 7),
            // (8,7): warning CS0169: The field 'E.field' is never used
            //     C field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(8, 7),
            // (9,7): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     C property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C", "E").WithLocation(9, 7),
            // (10,12): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'E'.
            //     object this[C c] { get => c; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C", "E").WithLocation(10, 12),
            // (11,13): error CS9300: File type 'D' cannot be used in a member signature in non-file type 'E'.
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
            // (9,13): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E").WithLocation(9, 13),
            // (9,13): warning CS0169: The field 'E.field' is never used
            //     C.Inner field; // 1
            Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("E.field").WithLocation(9, 13),
            // (10,13): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E").WithLocation(10, 13),
            // (11,12): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E'.
            //     object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E").WithLocation(11, 12),
            // (12,27): error CS9300: File type 'C.InnerDelegate' cannot be used in a member signature in non-file type 'E'.
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
            // (24,17): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         C.Inner field; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "field").WithArguments("C.Inner", "E.Inner").WithLocation(24, 17),
            // (25,17): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         C.Inner property { get; set; } // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "property").WithArguments("C.Inner", "E.Inner").WithLocation(25, 17),
            // (26,16): error CS9300: File type 'C.Inner' cannot be used in a member signature in non-file type 'E.Inner'.
            //         object this[C.Inner inner] { get => inner; set { } } // 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "this").WithArguments("C.Inner", "E.Inner").WithLocation(26, 16),
            // (27,31): error CS9300: File type 'C.InnerDelegate' cannot be used in a member signature in non-file type 'E.Inner'.
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
            // (5,15): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'Del1'.
            // delegate void Del1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "Del1").WithArguments("C", "Del1").WithLocation(5, 15),
            // (6,12): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'Del2'.
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
            // (7,30): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public static D operator +(D d, C c) => d; // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "+").WithArguments("C", "D").WithLocation(7, 30),
            // (8,30): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
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
            // (7,12): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
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
            // (7,14): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
            //     public C M(C c1, C c2) => c1; // 1, 2, 3
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("C", "D").WithLocation(7, 14),
            // (7,14): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'D'.
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
            // (1,19): error CS9301: File type 'C' cannot use accessibility modifiers.
            // public file class C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "C").WithArguments("C").WithLocation(1, 19),
            // (2,21): error CS9301: File type 'D' cannot use accessibility modifiers.
            // file internal class D { } // 2
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "D").WithArguments("D").WithLocation(2, 21),
            // (3,20): error CS9301: File type 'E' cannot use accessibility modifiers.
            // private file class E { } // 3, 4
            Diagnostic(ErrorCode.ERR_FileTypeNoExplicitAccessibility, "E").WithArguments("E").WithLocation(3, 20),
            // (3,20): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
            // private file class E { } // 3, 4
            Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "E").WithLocation(3, 20));
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
            // (2,7): error CS9301: File type 'Base' cannot be used as a base type of non-file type 'Derived1'.
            // class Derived1 : Base { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived1").WithArguments("Base", "Derived1").WithLocation(2, 7),
            // (3,14): error CS0060: Inconsistent accessibility: base class 'Base' is less accessible than class 'Derived2'
            // public class Derived2 : Base { } // 2, 3
            Diagnostic(ErrorCode.ERR_BadVisBaseClass, "Derived2").WithArguments("Derived2", "Base").WithLocation(3, 14),
            // (3,14): error CS9301: File type 'Base' cannot be used as a base type of non-file type 'Derived2'.
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
            // (6,11): error CS9301: File type 'Interface' cannot be used as a base type of non-file type 'Derived3'.
            // interface Derived3 : Interface { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeBase, "Derived3").WithArguments("Interface", "Derived3").WithLocation(6, 11));
    }

    [Fact]
    public void TypeArguments_01()
    {
        var source = """
            file class C { }
            class Container<T> { }
            class Program
            {
                Container<C> M() => new Container<C>(); // 1
            }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyDiagnostics(
            // (5,18): error CS9300: File type 'Container<C>' cannot be used in a member signature in non-file type 'Program'.
            //     Container<C> M() => new Container<C>(); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "M").WithArguments("Container<C>", "Program").WithLocation(5, 18));
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
            // (10,30): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'E.M<T>(T)'.
            //     void M<T>(T t) where T : C { } // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "C").WithArguments("C", "E.M<T>(T)").WithLocation(10, 30));
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
            // (3,8): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (3,8): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'R1'.
            // record R1(C c); // 1
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R1").WithArguments("C", "R1").WithLocation(3, 8),
            // (4,15): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'R2'.
            // record struct R2(C c); // 2
            Diagnostic(ErrorCode.ERR_FileTypeDisallowedInSignature, "R2").WithArguments("C", "R2").WithLocation(4, 15),
            // (4,15): error CS9300: File type 'C' cannot be used in a member signature in non-file type 'R2'.
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

        var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
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

        var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });
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

        var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, expectedOutput: "1");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void AccessThroughNamespace_02()
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
            Diagnostic(ErrorCode.ERR_NoSuchMember, "C").WithArguments("Outer", "C").WithLocation(5, 15));
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
            Diagnostic(ErrorCode.ERR_NoSuchMember, "C").WithArguments("Outer", "C").WithLocation(5, 15));
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

        // PROTOTYPE(ft): it should probably be an error to reference a file type in a global using static
        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics();
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

        var compilation = CompileAndVerify(source, expectedOutput: "1");
        compilation.VerifyDiagnostics();
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

        // 'Derived.C' is not actually accessible from 'Program', so we just bind to 'Base.C' and things work.
        var compilation = CompileAndVerify(new[] { source, main }, expectedOutput: "1");
        compilation.VerifyDiagnostics();
    }

    [Fact]
    public void SemanticModel_01()
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
        Assert.Equal(compilation.GetMember("C.M").GetPublicSymbol(), info.Symbol);

        var classC = compilation.GetMember("C").GetPublicSymbol();
        var symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, name: "C");
        Assert.Equal(new[] { classC }, symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition);
        Assert.Contains(classC, symbols);
    }

    [Fact]
    public void SemanticModel_02()
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
                    C.M(); // 1
                }
            }
            """;

        var compilation = CreateCompilation(new[] { source, main });
        compilation.VerifyDiagnostics(
            // (5,9): error CS0103: The name 'C' does not exist in the current context
            //         C.M();
            Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(5, 9)
            );

        var tree = compilation.SyntaxTrees[1];
        var body = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last().Body!;

        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

        var info = model.GetSymbolInfo(((ExpressionStatementSyntax)body.Statements.First()).Expression);
        Assert.Null(info.Symbol);
        Assert.Empty(info.CandidateSymbols);
        Assert.Equal(CandidateReason.None, info.CandidateReason);

        var symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition, name: "C");
        Assert.Empty(symbols);

        symbols = model.LookupSymbols(body.OpenBraceToken.EndPosition);
        Assert.DoesNotContain(compilation.GetMember("C").GetPublicSymbol(), symbols);
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

    // PROTOTYPE(ft): public API (INamedTypeSymbol.IsFile?)
}
