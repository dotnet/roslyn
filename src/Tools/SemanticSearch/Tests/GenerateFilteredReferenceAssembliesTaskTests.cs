// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Tools.UnitTests;

public sealed class GenerateFilteredReferenceAssembliesTaskTests : CSharpTestBase
{
    [Theory]
    [InlineData("")]
    [InlineData("\t")]
    [InlineData("\t#\t")]
    [InlineData("#")]
    [InlineData("#+")]
    [InlineData("#abc")]
    [InlineData("#𫚭鿯龻蝌灋齅ㄥ﹫䶱ན།ىي꓂")] // GB18030
    public void ParseApiPatterns_Empty(string value)
    {
        var errors = new List<(string message, int line)>();
        var patterns = new List<ApiPattern>();
        GenerateFilteredReferenceAssembliesTask.ParseApiPatterns([value], errors, patterns);
        Assert.Empty(errors);
        Assert.Empty(patterns);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("-#")]
    public void ParseApiPatterns_Error_ExpectedMetadataName(string value)
    {
        var errors = new List<(string message, int line)>();
        var patterns = new List<ApiPattern>();
        GenerateFilteredReferenceAssembliesTask.ParseApiPatterns([value], errors, patterns);
        AssertEx.Equal(new[] { ("expected metadata name", 1) }, errors);
        Assert.Empty(patterns);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("P")]
    [InlineData("N")]
    [InlineData("X")]
    internal void ParseApiPatterns_Error_UnexpectedSymbolKind(string kind)
    {
        var errors = new List<(string message, int line)>();
        var patterns = new List<ApiPattern>();
        GenerateFilteredReferenceAssembliesTask.ParseApiPatterns([kind + ":*"], errors, patterns);
        AssertEx.Equal(new[] { ($"unexpected symbol kind: '{kind}'", 1) }, errors);
        Assert.Empty(patterns);
    }

    [Theory]
    [InlineData("*", true, SymbolKindFlags.NamedType, @".*")]
    [InlineData("?", true, SymbolKindFlags.NamedType, @"\?")]
    [InlineData("%", true, SymbolKindFlags.NamedType, @"%")]
    [InlineData("<", true, SymbolKindFlags.NamedType, @"<")]
    [InlineData("a b c", true, SymbolKindFlags.NamedType, @"a\ b\ c")]
    [InlineData("a b c#", true, SymbolKindFlags.NamedType, @"a\ b\ c")]
    [InlineData(" a b #c", true, SymbolKindFlags.NamedType, @"a\ b")]
    [InlineData(" + System.IO", true, SymbolKindFlags.NamedType, @"System\.IO")]
    [InlineData("+System.IO.*", true, SymbolKindFlags.NamedType, @"System\.IO\..*")]
    [InlineData(" -System.IO.**", false, SymbolKindFlags.NamedType, @"System\.IO\..*.*")]
    [InlineData("- System.IO.* *", false, SymbolKindFlags.NamedType, @"System\.IO\..*\ .*")]
    [InlineData("𫚭鿯龻蝌灋齅ㄥ﹫䶱ན།ىي꓂", true, SymbolKindFlags.NamedType, @"𫚭鿯龻蝌灋齅ㄥ﹫䶱ན།ىي꓂")] // GB18030
    [InlineData("M:*", true, SymbolKindFlags.Method, @".*")]
    [InlineData("M:?", true, SymbolKindFlags.Method, @"\?")]
    [InlineData("M: a b #c", true, SymbolKindFlags.Method, @"a\ b")]
    [InlineData("+M: System.IO", true, SymbolKindFlags.Method, @"System\.IO")]
    [InlineData("+M: System.IO.Path.F(*)", true, SymbolKindFlags.Method, @"System\.IO\.Path\.F\(.*\)")]
    internal void ParseApiPatterns(string value, bool isIncluded, SymbolKindFlags symbolKinds, string pattern)
    {
        var errors = new List<(string message, int line)>();
        var patterns = new List<ApiPattern>();
        GenerateFilteredReferenceAssembliesTask.ParseApiPatterns([value], errors, patterns);
        Assert.Empty(errors);

        AssertEx.Equal(new[] { (symbolKinds, $"^{pattern}$", isIncluded) }, patterns.Select(p => (p.SymbolKinds, p.MetadataNamePattern.ToString(), p.IsIncluded)));
    }

    [Fact]
    public void Types()
    {
        var libSource = CreateCompilation("""
            namespace N
            {
                public class C
                {
                    public class D;
                }
            }

            namespace M
            {
                public class E;
                public class E<T>;
                public class E<T1, T2>;
            }
            """);

        var dir = Temp.CreateDirectory();
        var libImage = libSource.EmitToArray(new EmitOptions(metadataOnly: true)).ToArray();

        var patterns = ImmutableArray.Create(
            new ApiPattern(SymbolKindFlags.NamedType, new Regex(@"M\.E.*"), IsIncluded: true),
            new ApiPattern(SymbolKindFlags.NamedType, new Regex(@"M\.E`1"), IsIncluded: false));

        GenerateFilteredReferenceAssembliesTask.Rewrite(libImage, patterns);

        var libRef = MetadataReference.CreateFromImage(libImage);

        var c = CreateCompilation("""
            N.C.D d = null;
            M.E e1 = null;
            M.E<int> e2 = null;
            M.E<int, int> e3 = null;
            """, references: [libRef], TestOptions.DebugExe);

        c.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify(
            // (1,3): error CS0122: 'C' is inaccessible due to its protection level
            // N.C.D d = null;
            Diagnostic(ErrorCode.ERR_BadAccess, "C").WithArguments("N.C").WithLocation(1, 3),
            // (3,3): error CS0122: 'E<T>' is inaccessible due to its protection level
            // M.E<int> e2 = null;
            Diagnostic(ErrorCode.ERR_BadAccess, "E<int>").WithArguments("M.E<T>").WithLocation(3, 3));
    }

    [Fact]
    public void Interface()
    {
        var libSource = CreateCompilation("""
            public interface I
            {
                public void M1();
                public void M2();
            }

            public class C : I
            {
                public C() => throw null;
                public void M1() => throw null;
                public void M2() => throw null;
            }
            """);

        var dir = Temp.CreateDirectory();
        var libImage = libSource.EmitToArray(new EmitOptions(metadataOnly: true)).ToArray();

        var patterns = ImmutableArray.Create(
            new ApiPattern(SymbolKindFlags.NamedType, new Regex(@".*"), IsIncluded: true),
            new ApiPattern(SymbolKindFlags.Method, new Regex(@"I.M1"), IsIncluded: false));

        GenerateFilteredReferenceAssembliesTask.Rewrite(libImage, patterns);

        var libRef = MetadataReference.CreateFromImage(libImage);

        var c = CreateCompilation("""
            I i = new C();
            i.M1();
            i.M2();

            C c = new C();
            c.M1();
            c.M2();
            """, references: [libRef], TestOptions.DebugExe);

        c.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify(
            // (2,3): error CS0122: 'I.M1()' is inaccessible due to its protection level
            // i.M1();
            Diagnostic(ErrorCode.ERR_BadAccess, "M1").WithArguments("I.M1()").WithLocation(2, 3));
    }

    [Fact]
    public void Property()
    {
        var libSource = CreateCompilation("""
            public class C
            {
                public int P1 { get; }
                public int P2 { get; set; }
                public int P3 { get; protected set; }
                public int P4 { get; private protected set; }
            }    
            """);

        var dir = Temp.CreateDirectory();
        var libImage = libSource.EmitToArray(new EmitOptions(metadataOnly: true)).ToArray();

        var patterns = ImmutableArray.Create(
            new ApiPattern(SymbolKindFlags.NamedType, new Regex(@".*"), IsIncluded: true),
            new ApiPattern(SymbolKindFlags.Method, new Regex(@"C\.get_.*"), IsIncluded: false),
            new ApiPattern(SymbolKindFlags.Method, new Regex(@"C\.set_.*"), IsIncluded: false),
            new ApiPattern(SymbolKindFlags.Method, new Regex(@"C\.get_P2"), IsIncluded: true));

        GenerateFilteredReferenceAssembliesTask.Rewrite(libImage, patterns);

        var libRef = MetadataReference.CreateFromImage(libImage);

        var c = CreateCompilation("""
            var d = new D();
            d.F();

            class D : C
            {
                public int F()
                {
                    P2 = 2;
                    P3 = 3;
                    P4 = 4;

                    return P1 + P2 + P3 + P4;
                }
            }
            """, references: [libRef], TestOptions.DebugExe);

        c.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify(
            // (8,9): error CS0200: Property or indexer 'C.P2' cannot be assigned to -- it is read only
            //         P2 = 2;
            Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "P2").WithArguments("C.P2").WithLocation(8, 9),
            // (9,9): error CS0103: The name 'P3' does not exist in the current context
            //         P3 = 3;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P3").WithArguments("P3").WithLocation(9, 9),
            // (10,9): error CS0103: The name 'P4' does not exist in the current context
            //         P4 = 4;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P4").WithArguments("P4").WithLocation(10, 9),
            // (12,16): error CS0103: The name 'P1' does not exist in the current context
            //         return P1 + P2 + P3 + P4;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P1").WithArguments("P1").WithLocation(12, 16),
            // (12,26): error CS0103: The name 'P3' does not exist in the current context
            //         return P1 + P2 + P3 + P4;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P3").WithArguments("P3").WithLocation(12, 26),
            // (12,31): error CS0103: The name 'P4' does not exist in the current context
            //         return P1 + P2 + P3 + P4;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "P4").WithArguments("P4").WithLocation(12, 31));
    }
}
