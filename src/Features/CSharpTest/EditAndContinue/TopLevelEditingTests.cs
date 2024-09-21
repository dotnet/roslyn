// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
#pragma warning disable IDE0055 // Collection expression formatting

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

[UseExportProvider]
public class TopLevelEditingTests : EditingTestBase
{
    private static readonly string s_attributeSource = @"
[System.AttributeUsage(System.AttributeTargets.All)]class A : System.Attribute { public A() {} public A(int x) { } }
";
    #region Usings

    [Fact]
    public void Using_Global_Insert1()
    {
        var src1 = @"
using System.Collections.Generic;
";
        var src2 = @"
global using D = System.Diagnostics;
global using System.Collections;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [global using D = System.Diagnostics;]@2",
            "Insert [global using System.Collections;]@40");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Global_Insert2()
    {
        var src1 = @"
using unsafe D3 = int*;
";
        var src2 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [global using D1 = int;]@2",
            "Insert [using D2 = (int, int);]@26");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Delete1()
    {
        var src1 = @"
using System.Diagnostics;
";
        var src2 = @"";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits("Delete [using System.Diagnostics;]@2");
        Assert.IsType<UsingDirectiveSyntax>(edits.Edits.First().OldNode);
        Assert.Null(edits.Edits.First().NewNode);
    }

    [Fact]
    public void Using_Delete2()
    {
        var src1 = @"
using D = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
        var src2 = @"
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [using D = System.Diagnostics;]@2",
            "Delete [using System.Collections;]@33");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Delete3()
    {
        var src1 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
";
        var src2 = @"
using D2 = (int, int);
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [global using D1 = int;]@2",
            "Delete [using unsafe D3 = int*;]@50");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Insert1()
    {
        var src1 = @"
using System.Collections.Generic;
";
        var src2 = @"
using D = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [using D = System.Diagnostics;]@2",
            "Insert [using System.Collections;]@33");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Insert2()
    {
        var src1 = @"
using System.Collections.Generic;
";
        var src2 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [global using D1 = int;]@2",
            "Insert [using D2 = (int, int);]@26",
            "Insert [using unsafe D3 = int*;]@50");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Update1()
    {
        var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
        var src2 = @"
using System.Diagnostics;
using X = System.Collections;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [using System.Collections;]@29 -> [using X = System.Collections;]@29");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Update2()
    {
        var src1 = @"
using System.Diagnostics;
using X1 = System.Collections;
using System.Collections.Generic;
";
        var src2 = @"
using System.Diagnostics;
using X2 = System.Collections;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [using X1 = System.Collections;]@29 -> [using X2 = System.Collections;]@29");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Update3()
    {
        var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
        var src2 = @"
using System;
using System.Collections;
using System.Collections.Generic;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [using System.Diagnostics;]@2 -> [using System;]@2");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Update4()
    {
        var src1 = @"
using X = int;
using Y = int;
using Z = int;
";
        var src2 = @"
using X = string;
using unsafe Y = int*;
global using Z = int;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [using X = int;]@2 -> [using X = string;]@2",
            "Update [using Y = int;]@18 -> [using unsafe Y = int*;]@21",
            "Update [using Z = int;]@34 -> [global using Z = int;]@45");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Using_Reorder1()
    {
        var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
        var src2 = @"
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [using System.Diagnostics;]@2 -> @64");
    }

    [Fact]
    public void Using_Reorder2()
    {
        var src1 = @"
using X = int;
using Y = string;
";
        var src2 = @"
using Y = string;
using X = int;
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [using Y = string;]@18 -> @2");
    }

    [Fact]
    public void Using_InsertDelete1()
    {
        var src1 = @"
namespace N
{
    using System.Collections;
}

namespace M
{
}
";
        var src2 = @"
namespace N
{
}

namespace M
{
    using System.Collections;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [using System.Collections;]@43",
            "Delete [using System.Collections;]@22");
    }

    [Fact]
    public void Using_InsertDelete2()
    {
        var src1 = @"
namespace N
{
    using System.Collections;
}
";
        var src2 = @"
using System.Collections;

namespace N
{
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [using System.Collections;]@2",
            "Delete [using System.Collections;]@22");
    }

    [Fact]
    public void Using_Delete_ChangesCodeMeaning()
    {
        // This test specifically validates the scenario we _don't_ support, namely when inserting or deleting
        // a using directive, if existing code changes in meaning as a result, we don't issue edits for that code.
        // If this ever regresses then please buy a lottery ticket because the feature has magically fixed itself.
        var src1 = @"
using System.IO;
using DirectoryInfo = N.C;

namespace N
{
    public class C
    {
        public C(string a) { }
        public FileAttributes Attributes { get; set; }
    }

    public class D
    {
        public void M()
        {
            var d = new DirectoryInfo(""aa"");
            var x = directoryInfo.Attributes;
        }
    }
}";
        var src2 = @"
using System.IO;

namespace N
{
    public class C
    {
        public C(string a) { }
        public FileAttributes Attributes { get; set; }
    }

    public class D
    {
        public void M()
        {
            var d = new DirectoryInfo(""aa"");
            var x = directoryInfo.Attributes;
        }
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [using DirectoryInfo = N.C;]@20");

        edits.VerifySemantics();
    }

    [Fact]
    public void Using_Insert_ForNewCode()
    {
        // As distinct from the above, this test validates a real world scenario of inserting a using directive
        // and changing code that utilizes the new directive to some effect.
        var src1 = @"
namespace N
{
    class Program
    {
        static void F()
        {
        }
    }
}";
        var src2 = @"
using System;

namespace N
{
    class Program
    {
        static void F()
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.Program.F")));
    }

    [Fact]
    public void Using_Delete_ForOldCode()
    {
        var src1 = @"
using System;

namespace N
{
    class Program
    {
        static void F()
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";
        var src2 = @"
namespace N
{
    class Program
    {
        static void F()
        {
        }
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.Program.F")));
    }

    [Fact]
    public void Using_Insert_CreatesAmbiguousCode()
    {
        // This test validates that we still issue edits for changed valid code, even when unchanged
        // code has ambiguities after adding a using.
        var src1 = @"
using System.Threading;

namespace N
{
    class C
    {
        void M()
        {
            // Timer exists in System.Threading and System.Timers
            var t = new Timer(s => System.Console.WriteLine(s));
        }
    }
}";
        var src2 = @"
using System.Threading;
using System.Timers;

namespace N
{
    class C
    {
        void M()
        {
            // Timer exists in System.Threading and System.Timers
            var t = new Timer(s => System.Console.WriteLine(s));
        }

        void M2()
        {
             // TimersDescriptionAttribute only exists in System.Timers
            System.Console.WriteLine(new TimersDescriptionAttribute(""""));
        }
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.C.M2"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    #endregion

    #region Extern Alias

    [Fact]
    public void ExternAliasUpdate()
    {
        var src1 = "extern alias X;";
        var src2 = "extern alias Y;";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [extern alias X;]@0 -> [extern alias Y;]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Update, "extern alias Y;", CSharpFeaturesResources.extern_alias));
    }

    [Fact]
    public void ExternAliasInsert()
    {
        var src1 = "";
        var src2 = "extern alias Y;";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [extern alias Y;]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "extern alias Y;", CSharpFeaturesResources.extern_alias));
    }

    [Fact]
    public void ExternAliasDelete()
    {
        var src1 = "extern alias Y;";
        var src2 = "";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [extern alias Y;]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, null, CSharpFeaturesResources.extern_alias));
    }

    #endregion

    #region Assembly/Module Attributes

    [Fact]
    public void Insert_TopLevelAttribute()
    {
        var src1 = "";
        var src2 = "[assembly: System.Obsolete(\"2\")]";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [[assembly: System.Obsolete(\"2\")]]@0",
            "Insert [System.Obsolete(\"2\")]@11");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "[assembly: System.Obsolete(\"2\")]", FeaturesResources.attribute));
    }

    [Fact]
    public void Delete_TopLevelAttribute()
    {
        var src1 = "[assembly: System.Obsolete(\"2\")]";
        var src2 = "";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [[assembly: System.Obsolete(\"2\")]]@0",
            "Delete [System.Obsolete(\"2\")]@11");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, null, FeaturesResources.attribute));
    }

    [Fact]
    public void Update_TopLevelAttribute()
    {
        var src1 = "[assembly: System.Obsolete(\"1\")]";
        var src2 = "[assembly: System.Obsolete(\"2\")]";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[assembly: System.Obsolete(\"1\")]]@0 -> [[assembly: System.Obsolete(\"2\")]]@0",
            "Update [System.Obsolete(\"1\")]@11 -> [System.Obsolete(\"2\")]@11");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Update, "System.Obsolete(\"2\")", FeaturesResources.attribute));
    }

    [Fact]
    public void Reorder_TopLevelAttribute()
    {
        var src1 = "[assembly: System.Obsolete(\"1\")][assembly: System.Obsolete(\"2\")]";
        var src2 = "[assembly: System.Obsolete(\"2\")][assembly: System.Obsolete(\"1\")]";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [[assembly: System.Obsolete(\"2\")]]@32 -> @0");

        edits.VerifySemanticDiagnostics();
    }

    #endregion

    #region Types

    [Theory]
    [InlineData("class", "struct")]
    [InlineData("class", "record")] // TODO: Allow this conversion: https://github.com/dotnet/roslyn/issues/51874
    [InlineData("class", "record struct")]
    [InlineData("class", "interface")]
    [InlineData("struct", "record struct")] // TODO: Allow this conversion: https://github.com/dotnet/roslyn/issues/51874
    public void Type_Update_Kind(string oldKeyword, string newKeyword)
    {
        var src1 = oldKeyword + " C { }";
        var src2 = newKeyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [" + oldKeyword + " C { }]@0 -> [" + newKeyword + " C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeKindUpdate, newKeyword + " C"));
    }

    [Theory]
    [InlineData("class", "struct")]
    [InlineData("class", "record")]
    [InlineData("class", "record struct")]
    [InlineData("class", "interface")]
    [InlineData("struct", "record struct")]
    public void Type_Update_Kind_Reloadable(string oldKeyword, string newKeyword)
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]" + oldKeyword + " C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]" + newKeyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[CreateNewOnMetadataUpdate]" + oldKeyword + " C { }]@145 -> [[CreateNewOnMetadataUpdate]" + newKeyword + " C { }]@145");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Update_Modifiers_Static_Remove()
    {
        var src1 = "public static class C { }";
        var src2 = "public class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public static class C { }]@0 -> [public class C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public class C", FeaturesResources.class_));
    }

    [Theory]
    [InlineData("public")]
    public void Type_Update_Modifiers_Accessibility_Significant(string accessibility)
    {
        var src1 = accessibility + " class C { }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [" + accessibility + " class C { }]@0 -> [class C { }]@0");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"))]);
    }

    [Fact]
    public void Type_Update_Modifiers_Accessibility_Insignificant()
    {
        var src1 = "internal interface C { }";
        var src2 = "interface C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [internal interface C { }]@0 -> [interface C { }]@0");

        edits.VerifySemantics();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Type_Update_Modifiers_Accessibility_Nested_Significant(string accessibility)
    {
        var src1 = "class D { " + accessibility + " class C { } }";
        var src2 = "class D { class C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.C"))]);
    }

    [Fact]
    public void Type_Update_Modifiers_Accessibility_Nested_Insignificant()
    {
        var src1 = "class D { private class C { } }";
        var src2 = "class D { class C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics();
    }

    [Theory]
    [InlineData("public", "public")]
    [InlineData("internal", "internal")]
    [InlineData("", "internal")]
    [InlineData("internal", "")]
    [InlineData("protected", "protected")]
    [InlineData("private", "private")]
    [InlineData("private protected", "private protected")]
    [InlineData("internal protected", "internal protected")]
    public void Type_Update_Modifiers_Accessibility_Partial(string accessibilityA, string accessibilityB)
    {
        var srcA1 = accessibilityA + " partial class C { }";
        var srcB1 = "partial class C { }";
        var srcA2 = "partial class C { }";
        var srcB2 = accessibilityB + " partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Type_Update_Modifiers_Accessibility_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[CreateNewOnMetadataUpdate]public class C { }]@145 -> [[CreateNewOnMetadataUpdate]internal class C { }]@145");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Type_Update_Modifiers_NestedPrivateInInterface_Remove(string keyword)
    {
        var src1 = "interface C { private " + keyword + " S { } }";
        var src2 = "interface C { " + keyword + " S { } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.S"))]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Type_Update_Modifiers_NestedPrivateInClass_Add(string keyword)
    {
        var src1 = "class C { " + keyword + " S { } }";
        var src2 = "class C { private " + keyword + " S { } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics();
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Type_Update_Modifiers_NestedPublicInInterface_Add(string keyword)
    {
        var src1 = "interface C { " + keyword + " S { } }";
        var src2 = "interface C { public " + keyword + " S { } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
    public void Type_Update_Modifiers_Unsafe_Add()
    {
        var src1 = "public class C { }";
        var src2 = "public unsafe class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public class C { }]@0 -> [public unsafe class C { }]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
    public void Type_Update_Modifiers_Unsafe_Remove()
    {
        var src1 = @"
using System;
unsafe delegate void D();
class C
{
    unsafe class N { }
    public unsafe event Action<int> A { add { } remove { } }
    unsafe int F() => 0;
    unsafe int X;
    unsafe int Y { get; }
    unsafe C() {}
    unsafe ~C() {}
}
";
        var src2 = @"
using System;
delegate void D();
class C
{
    class N { }
    public event Action<int> A { add { } remove { } }
    int F() => 0;
    int X;
    int Y { get; }
    C() {}
    ~C() {}
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [unsafe delegate void D();]@17 -> [delegate void D();]@17",
            "Update [unsafe class N { }]@60 -> [class N { }]@53",
            "Update [public unsafe event Action<int> A { add { } remove { } }]@84 -> [public event Action<int> A { add { } remove { } }]@70",
            "Update [unsafe int F() => 0;]@146 -> [int F() => 0;]@125",
            "Update [unsafe int X;]@172 -> [int X;]@144",
            "Update [unsafe int Y { get; }]@191 -> [int Y { get; }]@156",
            "Update [unsafe C() {}]@218 -> [C() {}]@176",
            "Update [unsafe ~C() {}]@237 -> [~C() {}]@188");

        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
    public void Type_Update_Modifiers_Unsafe_DeleteInsert()
    {
        var srcA1 = "partial class C { unsafe void F() { } }";
        var srcB1 = "partial class C { }";
        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F"))
                ]),
            ]);
    }

    [Fact]
    public void Type_Update_Modifiers_Ref_Add()
    {
        var src1 = "public struct C { }";
        var src2 = "public ref struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public struct C { }]@0 -> [public ref struct C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public ref struct C", CSharpFeaturesResources.struct_));
    }

    [Fact]
    public void Type_Update_Modifiers_Ref_Remove()
    {
        var src1 = "public ref struct C { }";
        var src2 = "public struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public ref struct C { }]@0 -> [public struct C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
    }

    [Fact]
    public void Type_Update_Modifiers_ReadOnly_Add()
    {
        var src1 = "public struct C { }";
        var src2 = "public readonly struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public struct C { }]@0 -> [public readonly struct C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly struct C", CSharpFeaturesResources.struct_));
    }

    [Fact]
    public void Type_Update_Modifiers_ReadOnly_Remove()
    {
        var src1 = "public readonly struct C { }";
        var src2 = "public struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public readonly struct C { }]@0 -> [public struct C { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
    }

    [Theory]
    [InlineData("[System.CLSCompliantAttribute]", "CLSCompliantAttribute")]
    [InlineData("[System.Diagnostics.CodeAnalysis.AllowNullAttribute]", "AllowNullAttribute")]
    [InlineData("[System.Diagnostics.CodeAnalysis.DisallowNullAttribute]", "DisallowNullAttribute")]
    [InlineData("[System.Diagnostics.CodeAnalysis.MaybeNullAttribute]", "MaybeNullAttribute")]
    [InlineData("[System.Diagnostics.CodeAnalysis.NotNullAttribute]", "NotNullAttribute")]
    [InlineData("[System.NonSerializedAttribute]", "NonSerializedAttribute")]
    [InlineData("[System.Reflection.AssemblyAlgorithmIdAttribute]", "AssemblyAlgorithmIdAttribute")]
    [InlineData("[System.Reflection.AssemblyCultureAttribute]", "AssemblyCultureAttribute")]
    [InlineData("[System.Reflection.AssemblyFlagsAttribute]", "AssemblyFlagsAttribute")]
    [InlineData("[System.Reflection.AssemblyVersionAttribute]", "AssemblyVersionAttribute")]
    [InlineData("[System.Runtime.CompilerServices.DllImportAttribute]", "DllImportAttribute")]
    [InlineData("[System.Runtime.CompilerServices.IndexerNameAttribute]", "IndexerNameAttribute")]
    [InlineData("[System.Runtime.CompilerServices.MethodImplAttribute]", "MethodImplAttribute")]
    [InlineData("[System.Runtime.CompilerServices.SpecialNameAttribute]", "SpecialNameAttribute")]
    [InlineData("[System.Runtime.CompilerServices.TypeForwardedToAttribute]", "TypeForwardedToAttribute")]
    [InlineData("[System.Runtime.InteropServices.ComImportAttribute]", "ComImportAttribute")]
    [InlineData("[System.Runtime.InteropServices.DefaultParameterValueAttribute]", "DefaultParameterValueAttribute")]
    [InlineData("[System.Runtime.InteropServices.FieldOffsetAttribute]", "FieldOffsetAttribute")]
    [InlineData("[System.Runtime.InteropServices.InAttribute]", "InAttribute")]
    [InlineData("[System.Runtime.InteropServices.MarshalAsAttribute]", "MarshalAsAttribute")]
    [InlineData("[System.Runtime.InteropServices.OptionalAttribute]", "OptionalAttribute")]
    [InlineData("[System.Runtime.InteropServices.OutAttribute]", "OutAttribute")]
    [InlineData("[System.Runtime.InteropServices.PreserveSigAttribute]", "PreserveSigAttribute")]
    [InlineData("[System.Runtime.InteropServices.StructLayoutAttribute]", "StructLayoutAttribute")]
    [InlineData("[System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute]", "WindowsRuntimeImportAttribute")]
    [InlineData("[System.Security.DynamicSecurityMethodAttribute]", "DynamicSecurityMethodAttribute")]
    [InlineData("[System.SerializableAttribute]", "SerializableAttribute")]
    [InlineData("[System.Runtime.CompilerServices.AsyncMethodBuilderAttribute]", "AsyncMethodBuilderAttribute")]
    public void Type_Attribute_Insert_SupportedByRuntime_NonCustomAttribute(string attributeType, string attributeName)
    {
        var src1 = @"class C { public void M(int a) {} }";
        var src2 = attributeType + @"class C { public void M(int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [class C { public void M(int a) {} }]@0 -> [" + attributeType + "class C { public void M(int a) {} }]@0");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "class C", attributeName, FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void Type_Attribute_Insert_InlineArray(string keyword)
    {
        var attribute = "namespace System.Runtime.CompilerServices { public class InlineArrayAttribute : Attribute { public InlineArrayAttribute(int n) { } } } ";

        var src1 = attribute + keyword + " C { int a; }";
        var src2 = attribute + "[System.Runtime.CompilerServices.InlineArray(1)]" + keyword + " C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttribute, keyword + " C", "InlineArrayAttribute")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void Type_Attribute_Update_InlineArray(string keyword)
    {
        var attribute = "namespace System.Runtime.CompilerServices { public class InlineArrayAttribute : Attribute { public InlineArrayAttribute(int n) { } } } ";

        var src1 = attribute + "[System.Runtime.CompilerServices.InlineArray(1)]" + keyword + " C { int a; }";
        var src2 = attribute + "[System.Runtime.CompilerServices.InlineArray(2)]" + keyword + " C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttribute, keyword + " C", "InlineArrayAttribute")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void Type_Attribute_Update_InlineArray_Reloadable(string keyword)
    {
        var attribute = ReloadableAttributeSrc + "namespace System.Runtime.CompilerServices { public class InlineArrayAttribute : Attribute { public InlineArrayAttribute(int n) { } } } ";

        var src1 = attribute + "[CreateNewOnMetadataUpdate, InlineArray(1)]" + keyword + " C { int a; }";
        var src2 = attribute + "[CreateNewOnMetadataUpdate, InlineArray(2)]" + keyword + " C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Attribute_Update_NotSupportedByRuntime1()
    {
        var attribute = "public class A1Attribute : System.Attribute { }\n\n" +
                        "public class A2Attribute : System.Attribute { }\n\n";

        var src1 = attribute + "[A1]class C { }";
        var src2 = attribute + "[A2]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A1]class C { }]@98 -> [[A2]class C { }]@98");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Attribute_Update_NotSupportedByRuntime2()
    {
        var src1 = "[System.Obsolete(\"1\")]class C { }";
        var src2 = "[System.Obsolete(\"2\")]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete(\"1\")]class C { }]@0 -> [[System.Obsolete(\"2\")]class C { }]@0");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Attribute_Delete_NotSupportedByRuntime1()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "[A, B]class C { }";
        var src2 = attribute + "[A]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A, B]class C { }]@96 -> [[A]class C { }]@96");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Attribute_Delete_NotSupportedByRuntime2()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "[B, A]class C { }";
        var src2 = attribute + "[A]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[B, A]class C { }]@96 -> [[A]class C { }]@96");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void Type_Attribute_Delete_InlineArray(string keyword)
    {
        var attribute = "namespace System.Runtime.CompilerServices { public class InlineArrayAttribute : Attribute { public InlineArrayAttribute(int n) { } } } ";

        var src1 = attribute + "[System.Runtime.CompilerServices.InlineArray(1)]" + keyword + " C { int a; }";
        var src2 = attribute + keyword + " C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttribute, keyword + " C", "InlineArrayAttribute")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1831006")]
    public void Type_Attribute_Update_Null()
    {
        var attribute = @"
using System;
public class A : Attribute { public A1(int[] array, Type type, Type[] types) {} }
";

        var src1 = attribute + "[A(null, null, new Type[] { typeof(C) })]class C { }";
        var src2 = attribute + "[A(null, null, null)]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_Attribute_Change_Reloadable()
    {
        var attributeSrc = @"
public class A1 : System.Attribute { }
public class A2 : System.Attribute { }
public class A3 : System.Attribute { }
";

        var src1 = ReloadableAttributeSrc + attributeSrc + "[CreateNewOnMetadataUpdate, A1, A2]class C { }";
        var src2 = ReloadableAttributeSrc + attributeSrc + "[CreateNewOnMetadataUpdate, A2, A3]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[CreateNewOnMetadataUpdate, A1, A2]class C { }]@267 -> [[CreateNewOnMetadataUpdate, A2, A3]class C { }]@267");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Attribute_ReloadableRemove()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
        var src2 = ReloadableAttributeSrc + "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Attribute_ReloadableAdd()
    {
        var src1 = ReloadableAttributeSrc + "class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_Attribute_ReloadableBase()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class B { } class C : B { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class B { } class C : B { void F() {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Update_Attribute_Insert()
    {
        var attributes =
            """
            class A : System.Attribute { }
            class B : System.Attribute { }
            """;

        var src1 = attributes + "[A]class C { }";
        var src2 = attributes + "[A, B]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]class C { }]@62 -> [[A, B]class C { }]@62");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_Update_Attribute_Insert_Reloadable()
    {
        var attributes = ReloadableAttributeSrc +
            """
            class A : System.Attribute { }
            class B : System.Attribute { }
            """;

        var srcA1 = attributes + "[CreateNewOnMetadataUpdate]partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = attributes + "[CreateNewOnMetadataUpdate][A]partial class C { }";
        var srcB2 = "[B]partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Update_Attribute_Insert_NotSupportedByRuntime1()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "[A]class C { }";
        var src2 = attribute + "[A, B]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]class C { }]@96 -> [[A, B]class C { }]@96");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Update_Attribute_Insert_NotSupportedByRuntime2()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "class C { }";
        var src2 = attribute + "[A]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [class C { }]@48 -> [[A]class C { }]@48");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Update_Attribute_Reorder1()
    {
        var src1 = "[A(1), B(2), C(3)]class C { }";
        var src2 = "[C(3), A(1), B(2)]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A(1), B(2), C(3)]class C { }]@0 -> [[C(3), A(1), B(2)]class C { }]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Type_Update_Attribute_Reorder2()
    {
        var src1 = "[A, B, C]class C { }";
        var src2 = "[B, C, A]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A, B, C]class C { }]@0 -> [[B, C, A]class C { }]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Type_Attribute_ReorderAndUpdate_NotSupportedByRuntime()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "[System.Obsolete(\"1\"), A, B]class C { }";
        var src2 = attribute + "[A, B, System.Obsolete(\"2\")]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete(\"1\"), A, B]class C { }]@96 -> [[A, B, System.Obsolete(\"2\")]class C { }]@96");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Type_Rename(string keyword)
    {
        var src1 = keyword + " C { }";
        var src2 = keyword + " D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [" + keyword + " C { }]@0 -> [" + keyword + " D { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, keyword + " D", GetResource(keyword, "C")));
    }

    [Fact]
    public void Type_Rename_AddAndDeleteMember()
    {
        var src1 = "class C { int x = 1; }";
        var src2 = "class D { void F() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [class C { int x = 1; }]@0 -> [class D { void F() { } }]@0",
            "Insert [void F() { }]@10",
            "Insert [()]@16",
            "Delete [int x = 1;]@10",
            "Delete [int x = 1]@10",
            "Delete [x = 1]@14");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "class D", GetResource("class", "C")));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54886")]
    public void Type_Rename_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[CreateNewOnMetadataUpdate]class C { }]@145 -> [[CreateNewOnMetadataUpdate]class D { }]@145");

        // TODO: expected: Replace edit of D
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "class D", GetResource("class", "C")));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54886")]
    public void Type_Rename_Reloadable_AddAndDeleteMember()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { int x = 1; }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class D { void F() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[CreateNewOnMetadataUpdate]class C { int x = 1; }]@145 -> [[CreateNewOnMetadataUpdate]class D { void F() { } }]@145",
            "Insert [void F() { }]@182",
            "Insert [()]@188",
            "Delete [int x = 1;]@182",
            "Delete [int x = 1]@182",
            "Delete [x = 1]@186");

        // TODO: expected: Replace edit of D
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "class D", GetResource("class", "C")));
    }

    [Fact]
    public void Interface_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "interface C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Interface_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { } ";
        var src2 = "namespace N { interface C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Interface_NoModifiers_IntoType_Insert()
    {
        var src1 = "interface N { }";
        var src2 = "interface N { interface C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Class_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Class_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { }";
        var src2 = "namespace N { class C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Class_NoModifiers_IntoType_Insert()
    {
        var src1 = "struct N { }";
        var src2 = "struct N { class C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Struct_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Struct_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { }";
        var src2 = "namespace N { struct C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Struct_NoModifiers_IntoType_Insert()
    {
        var src1 = "struct N { }";
        var src2 = "struct N { struct C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Type_BaseType_Insert_Unchanged()
    {
        var src1 = "class C { }";
        var src2 = "class C : object { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: object]@8");

        edits.VerifySemantics();
    }

    [Fact]
    public void Type_BaseType_Insert_Changed()
    {
        var src1 = "class C { }";
        var src2 = "class C : D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: D]@8");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "D", FeaturesResources.class_));
    }

    [Fact]
    public void Type_BaseType_Insert_WithPrimaryInitializer()
    {
        var src1 = "class C() { }";
        var src2 = "class C() : D() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: D()]@10",
            "Insert [D()]@12");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "D()", FeaturesResources.class_));
    }

    [Fact]
    public void Type_BaseType_Delete_WithPrimaryInitializer()
    {
        var src1 = "class C() : D() { }";
        var src2 = "class C() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [: D()]@10",
            "Delete [D()]@12");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("string[]", "string[]?")]
    [InlineData("object", "dynamic")]
    [InlineData("dynamic?", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Type_BaseType_Update_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "class C : System.Collections.Generic.List<" + oldType + "> {}";
        var src2 = "class C : System.Collections.Generic.List<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        // We don't require a runtime capability to update attributes.
        // All runtimes support changing the attributes in metadata, some just don't reflect the changes in the Reflection model.
        // Having compiler-generated attributes visible via Reflaction API is not that important.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")));
    }

    [Theory]
    [InlineData("int", "string")]
    [InlineData("int", "int?")]
    [InlineData("(int a, int b)", "(int a, double b)")]
    public void Type_BaseType_Update_RuntimeTypeChanged(string oldType, string newType)
    {
        var src1 = "class C : System.Collections.Generic.List<" + oldType + "> {}";
        var src2 = "class C : System.Collections.Generic.List<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "System.Collections.Generic.List<" + newType + ">", FeaturesResources.class_));
    }

    [Fact]
    public void Type_BaseType_Update_CompileTimeTypeUnchanged()
    {
        var src1 = "using A = System.Int32; using B = System.Int32; class C : System.Collections.Generic.List<A> {}";
        var src2 = "using A = System.Int32; using B = System.Int32; class C : System.Collections.Generic.List<B> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics();
    }

    [Fact]
    public void Type_BaseInterface_Add()
    {
        var src1 = "class C { }";
        var src2 = "class C : IDisposable { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: IDisposable]@8");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "IDisposable", FeaturesResources.class_));
    }

    [Fact]
    public void Type_BaseInterface_Delete_Inherited()
    {
        var src1 = @"
interface B {}
interface A : B {}

class C : A, B {}
";
        var src2 = @"
interface B {}
interface A : B {}

class C : A {}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics();
    }

    [Fact]
    public void Type_BaseInterface_Reorder()
    {
        var src1 = "class C : IGoo, IBar { }";
        var src2 = "class C : IBar, IGoo { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: IGoo, IBar]@8 -> [: IBar, IGoo]@8");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "IBar, IGoo", FeaturesResources.class_));
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("object", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Type_BaseInterface_Update_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "class C : System.Collections.Generic.IEnumerable<" + oldType + "> {}";
        var src2 = "class C : System.Collections.Generic.IEnumerable<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")));
    }

    [Theory]
    [InlineData("int", "string")]
    [InlineData("int", "int?")]
    [InlineData("(int a, int b)", "(int a, double b)")]
    public void Type_BaseInterface_Update_RuntimeTypeChanged(string oldType, string newType)
    {
        var src1 = "class C : System.Collections.Generic.IEnumerable<" + oldType + "> {}";
        var src2 = "class C : System.Collections.Generic.IEnumerable<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "System.Collections.Generic.IEnumerable<" + newType + ">", FeaturesResources.class_));
    }

    [Fact]
    public void Type_Base_Partial()
    {
        var srcA1 = "partial class C : B, I { }";
        var srcB1 = "partial class C : J { }";
        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C : B, I, J { }";

        var srcC = @"
class B {}
interface I {}
interface J {}";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC, srcC)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults()
            ]);
    }

    [Fact]
    public void Type_Base_Partial_InsertDeleteAndUpdate()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "";
        var srcC1 = "partial class C { }";

        var srcA2 = "";
        var srcB2 = "partial class C : D { }";
        var srcC2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "partial class C", FeaturesResources.class_)]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Type_Base_InsertDelete()
    {
        var srcA1 = "";
        var srcB1 = "class C : B, I { }";
        var srcA2 = "class C : B, I { }";
        var srcB2 = "";

        var srcC = @"
class B {}
interface I {}
interface J {}";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC, srcC)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults()
            ]);
    }

    [Fact]
    public void Type_Reloadable_NotSupportedByRuntime()
    {
        var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
public class C
{
    void F() { System.Console.WriteLine(1); }
}";
        var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
public class C
{
    void F() { System.Console.WriteLine(2); }
}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, "void F()", "CreateNewOnMetadataUpdateAttribute")],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Insert_AbstractVirtualOverride()
    {
        var src1 = "";
        var src2 = @"
public abstract class C<T>
{ 
    public abstract void F(); 
    public virtual void G() {}
    public override string ToString() => null;
}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Insert_NotSupportedByRuntime()
    {
        var src1 = @"
public class C
{
    void F()
    {
    }
}";
        var src2 = @"
public class C
{
    void F()
    {
    }
}

public class D
{
    void M()
    {
    }
}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "public class D", FeaturesResources.class_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Insert_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void InterfaceInsert()
    {
        var src1 = "";
        var src2 = @"
public interface I 
{ 
    void F(); 
    static void G() {}
}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void RefStructInsert()
    {
        var src1 = "";
        var src2 = "ref struct X { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [ref struct X { }]@0");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Struct_ReadOnly_Insert()
    {
        var src1 = "";
        var src2 = "readonly struct X { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [readonly struct X { }]@0");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Struct_RefModifier_Add()
    {
        var src1 = "struct X { }";
        var src2 = "ref struct X { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [struct X { }]@0 -> [ref struct X { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "ref struct X", CSharpFeaturesResources.struct_));
    }

    [Fact]
    public void Struct_ReadonlyModifier_Add()
    {
        var src1 = "struct X { }";
        var src2 = "readonly struct X { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [struct X { }]@0 -> [readonly struct X { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "readonly struct X", SyntaxFacts.GetText(SyntaxKind.StructKeyword)));
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("readonly")]
    public void Struct_Modifiers_Partial_InsertDelete(string modifier)
    {
        var srcA1 = modifier + " partial struct S { }";
        var srcB1 = "partial struct S { }";
        var srcA2 = "partial struct S { }";
        var srcB2 = modifier + " partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults()
            ]);
    }

    [Fact]
    public void Class_ImplementingInterface_Add_Implicit_NonVirtual()
    {
        var src1 = """
            interface I
            {
                void F();
            }
            """;

        var src2 = """
            interface I
            {
                void F();
            }

            class C : I
            {
                public void F() {}
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Class_ImplementingInterface_Add_Implicit_Virtual()
    {
        var src1 = """
            interface I
            {
                void F();
            }
            """;

        var src2 = """
            interface I
            {
                void F();
            }

            class C : I
            {
                public virtual void F() {}
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Class_ImplementingInterface_Add_Implicit_Override()
    {
        var src1 = """
            interface I
            {
                void F();
            }

            class C : I
            {
                public virtual void F() {}
            }
            """;

        var src2 = """
            interface I
            {
                void F();
            }

            class C : I
            {
                public virtual void F() {}
            }

            class D : C
            {
                public override void F() {}
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("D"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition | EditAndContinueCapabilities.AddExplicitInterfaceImplementation);
    }

    [Theory]
    [InlineData("void F();", "void I.F() {}")]
    [InlineData("int F { get; }", "int I.F { get; }")]
    [InlineData("event System.Action F;", "event System.Action I.F { add {} remove {} }")]
    public void Class_ImplementingInterface_Add_Explicit_NonVirtual(string memberDef, string explicitImpl)
    {
        var src1 = $$"""
            interface I
            {
                {{memberDef}}
            }
            """;

        var src2 = $$"""
            interface I
            {
                {{memberDef}}
            }

            class C<T> : I
            {
                {{explicitImpl}}
            }
            """;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition | EditAndContinueCapabilities.AddExplicitInterfaceImplementation);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "class C<T>", GetResource("class"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37128")]
    public void Interface_InsertMembers()
    {
        var src1 = @"
using System;
interface I
{
}
";
        var src2 = @"
using System;
interface I
{
    static int StaticField = 10;

    static void StaticMethod() { }
    void VirtualMethod1() { }
    virtual void VirtualMethod2() { }
    abstract void AbstractMethod();
    sealed void NonVirtualMethod() { }

    public static int operator +(I a, I b) => 1;

    static int StaticProperty1 { get => 1; set { } }
    static int StaticProperty2 => 1;
    virtual int VirtualProperty1 { get => 1; set { } }
    virtual int VirtualProperty2 { get => 1; }
    int VirtualProperty3 { get => 1; set { } }
    int VirtualProperty4 { get => 1; }
    abstract int AbstractProperty1 { get; set; }
    abstract int AbstractProperty2 { get; }
    sealed int NonVirtualProperty => 1;

    int this[byte virtualIndexer] => 1;
    int this[sbyte virtualIndexer] { get => 1; }
    virtual int this[ushort virtualIndexer] { get => 1; set {} }
    virtual int this[short virtualIndexer] { get => 1; set {} }
    abstract int this[uint abstractIndexer] { get; set; }
    abstract int this[int abstractIndexer] { get; }
    sealed int this[ulong nonVirtualIndexer] { get => 1; set {} }
    sealed int this[long nonVirtualIndexer] { get => 1; set {} }
    
    static event Action StaticEvent;
    static event Action StaticEvent2 { add { } remove { } }

    event Action VirtualEvent { add { } remove { } }
    abstract event Action AbstractEvent;
    sealed event Action NonVirtualEvent { add { } remove { } }

    abstract class C { }
    interface J { }
    enum E { }
    delegate void D();
}
";
        var edits = GetTopEdits(src1, src2);

        // TODO: InsertIntoInterface errors are reported due to https://github.com/dotnet/roslyn/issues/37128.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoInterface, "static void StaticMethod()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertVirtual, "void VirtualMethod1()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertVirtual, "virtual void VirtualMethod2()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertVirtual, "abstract void AbstractMethod()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertIntoInterface, "sealed void NonVirtualMethod()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertOperator, "public static int operator +(I a, I b)", FeaturesResources.operator_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty1", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty2", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty1", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty2", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty3", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty4", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty1", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty2", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int NonVirtualProperty", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertVirtual, "int this[byte virtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertVirtual, "int this[sbyte virtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[ushort virtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[short virtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[uint abstractIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[int abstractIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[ulong nonVirtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[long nonVirtualIndexer]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "static event Action StaticEvent2", FeaturesResources.event_),
            Diagnostic(RudeEditKind.InsertVirtual, "event Action VirtualEvent", FeaturesResources.event_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "sealed event Action NonVirtualEvent", FeaturesResources.event_),
            Diagnostic(RudeEditKind.InsertIntoInterface, "StaticField = 10", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertIntoInterface, "StaticEvent", CSharpFeaturesResources.event_field),
            Diagnostic(RudeEditKind.InsertVirtual, "AbstractEvent", CSharpFeaturesResources.event_field));
    }

    [Fact]
    public void Interface_InsertDelete()
    {
        var srcA1 = @"
interface I
{
    static void M() { }
}
";
        var srcB1 = @"
";

        var srcA2 = @"
";
        var srcB2 = @"
interface I
{
    static void M() { }
}
";
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("M"))
                    ]),
            ]);
    }

    [Fact]
    public void Type_Generic_Insert_StatelessMembers()
    {
        var src1 = @"
using System;
class C<T>
{
    int P1 { get => 1; }
    int this[string s] { set {} }
}
";
        var src2 = @"
using System;
class C<T>
{
    C(int x) {}

    void M() {}
    void G<S>() {}
    int P1 { get => 1; set {} }
    int P2 { get => 1; set {} }
    int this[int i] { set {} get => 1; }
    int this[string s] { set {} get => 1; }
    event Action E { add {} remove {} }

    enum E {}
    interface I {} 
    interface I<S> {} 
    class D {}
    class D<S> {}
    delegate void Del();
    delegate void Del<S>();
}
";
        var edits = GetTopEdits(src1, src2);

        var diagnostics = new[]
        {
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "C(int x)", FeaturesResources.constructor),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "void M()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "void G<S>()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P2", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int this[int i]", FeaturesResources.indexer_),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "event Action E", FeaturesResources.event_),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "set", CSharpFeaturesResources.property_setter),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "get", CSharpFeaturesResources.indexer_getter),
        };

        edits.VerifySemanticDiagnostics(diagnostics, capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        edits.VerifySemanticDiagnostics(diagnostics, capabilities: EditAndContinueCapabilities.GenericAddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType);
    }

    [Fact]
    public void Type_Generic_Insert_DataMembers()
    {
        var src1 = @"
using System;
class C<T>
{
}
";
        var src2 = @"
using System;
class C<T>
{
    int P { get; set; }
    event Action EF;
    int F1, F2;
    static int SF;
}
";
        var edits = GetTopEdits(src1, src2);

        var nonGenericCapabilities =
            EditAndContinueCapabilities.AddInstanceFieldToExistingType |
            EditAndContinueCapabilities.AddStaticFieldToExistingType |
            EditAndContinueCapabilities.AddMethodToExistingType;

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", FeaturesResources.auto_property),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "EF", CSharpFeaturesResources.event_field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "F1", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "F2", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "SF", FeaturesResources.field),
        ], capabilities: nonGenericCapabilities);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", GetResource("auto-property")),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "F1", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "F2", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "SF", FeaturesResources.field),
        ], capabilities: nonGenericCapabilities | EditAndContinueCapabilities.GenericAddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", FeaturesResources.auto_property),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "EF", CSharpFeaturesResources.event_field)
        ], capabilities: nonGenericCapabilities | EditAndContinueCapabilities.GenericAddFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            capabilities: nonGenericCapabilities | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericAddFieldToExistingType);
    }

    [Fact]
    public void Type_Generic_Insert_IntoNestedType()
    {
        var src1 = @"
class C<T>
{
    class D
    {
    }
}
";
        var src2 = @"
class C<T>
{
    class D
    {
        void F() {}
        int X;
        static int Y;
    }
}
";
        var edits = GetTopEdits(src1, src2);

        var nonGenericCapabilities =
            EditAndContinueCapabilities.AddMethodToExistingType |
            EditAndContinueCapabilities.AddInstanceFieldToExistingType |
            EditAndContinueCapabilities.AddStaticFieldToExistingType;

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "void F()", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "X", FeaturesResources.field),
            Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "Y", FeaturesResources.field)
        ], capabilities: nonGenericCapabilities);

        edits.VerifySemanticDiagnostics(capabilities:
            nonGenericCapabilities |
            EditAndContinueCapabilities.GenericAddMethodToExistingType |
            EditAndContinueCapabilities.GenericAddFieldToExistingType);
    }

    [Fact]
    public void Type_Generic_InsertMembers_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + @"
interface IExplicit
{
    void F() {}
}

[CreateNewOnMetadataUpdate]
class C<T> : IExplicit
{
    void IExplicit.F() {}
}
";
        var src2 = ReloadableAttributeSrc + @"
interface IExplicit
{
    void F() {}
}

[CreateNewOnMetadataUpdate]
class C<T> : IExplicit
{
    void IExplicit.F() {}

    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    event System.Action E { add {} remove {} }
    event System.Action EF;
    int F1, F2;

    enum E {}
    interface I {} 
    class D {}
}
";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition | EditAndContinueCapabilities.AddExplicitInterfaceImplementation);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, "void M()", "CreateNewOnMetadataUpdateAttribute")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Generic_DeleteInsert()
    {
        var srcA1 = @"
class C<T> { void F() {} }
struct S<T> { void F() {} }
interface I<T> { void F() {} }
";
        var srcB1 = "";

        var srcA2 = srcB1;
        var srcB2 = srcA1;

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("I")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("I.F"))
                    ])
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F()", GetResource("method")),
                        Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F()", GetResource("method")),
                        Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F()", GetResource("method"))
                    ])
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54881")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/54881")]
    public void Type_TypeParameter_Insert_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public class C<T> { void F() { } }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal class C<T, S> { int x = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")));
    }

    [Fact]
    public void Type_Delete()
    {
        var src1 = @"
class C { void F() {} }
struct S { void F() {} }
interface I { void F() {} }
";
        var src2 = "";

        GetTopEdits(src1, src2).VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.class_, "C")),
            Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(CSharpFeaturesResources.struct_, "S")),
            Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.interface_, "I")));
    }

    [Fact]
    public void Type_Delete_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";
        var src2 = ReloadableAttributeSrc;

        GetTopEdits(src1, src2).VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "using System.Runtime.CompilerServices;", GetResource("class", "C")));
    }

    [Fact]
    public void Type_Partial_DeleteDeclaration()
    {
        var srcA1 = "partial class C { void F() {} void M() { } }";
        var srcB1 = "partial class C { void G() {} }";
        var srcA2 = "";
        var srcB2 = "partial class C { void G() {} void M() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("M")),
                    ])
            ]);
    }

    [Fact]
    public void Type_Partial_InsertFirstDeclaration()
    {
        var src1 = "";
        var src2 = "partial class C { void F() {}  }";

        GetTopEdits(src1, src2).VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C"), preserveLocalVariables: false)],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Partial_InsertSecondDeclaration()
    {
        var srcA1 = "partial class C { void F() {} }";
        var srcB1 = "";
        var srcA2 = "partial class C { void F() {} }";
        var srcB2 = "partial class C { void G() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("G"), preserveLocalVariables: false)
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Type_Partial_Reloadable()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { void F() {} }";
        var srcB1 = "";
        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { void F() {} }";
        var srcB2 = "partial class C { void G() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_DeleteInsert()
    {
        var srcA1 = @"
class C { void F() {} }
struct S { void F() {} }
interface I { void F() {} }
";
        var srcB1 = "";

        var srcA2 = srcB1;
        var srcB2 = srcA1;

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember("F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                    ])
            ]);
    }

    [Fact]
    public void Type_DeleteInsert_Reloadable()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";
        var srcB1 = "";

        var srcA2 = ReloadableAttributeSrc;
        var srcB2 = "[CreateNewOnMetadataUpdate]class C { void F() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")),
                    ])
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_NonInsertableMembers_DeleteInsert()
    {
        var srcA1 = @"
abstract class C
{
    public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    void G();
    void F() {}
}
";
        var srcB1 = "";

        var srcA2 = srcB1;
        var srcB2 = srcA1;

        // TODO: The methods without bodies do not need to be updated.
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("AbstractMethod")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("VirtualMethod")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("ToString")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("I.G")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("G")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                    ])
            ]);
    }

    [Fact]
    public void Type_Attribute_NonInsertableMembers_DeleteInsert()
    {
        var srcA1 = @"
abstract class C
{
    public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    void G();
    void F() {}
}
";
        var srcB1 = "";

        var srcA2 = "";
        var srcB2 = @"
abstract class C
{
    [System.Obsolete]public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    [System.Obsolete]void G();
    void F() {}
}";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("AbstractMethod")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("VirtualMethod")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("ToString")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("I.G")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("G")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                    ])
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_DeleteInsert_DataMembers()
    {
        var srcA1 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
    public event System.Action E = new System.Action(null);
}
";
        var srcB1 = "";

        var srcA2 = "";
        var srcB2 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
    public event System.Action E = new System.Action(null);
}
";
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
                    ])
            ]);
    }

    [Fact]
    public void Type_DeleteInsert_DataMembers_PartialSplit()
    {
        var srcA1 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
}
";
        var srcB1 = "";

        var srcA2 = @"
partial class C
{
    public int x = 1;
    public int y = 2;
}
";
        var srcB2 = @"
partial class C
{
    public int P { get; set; } = 3;
}
";
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true),
                    ])
            ]);
    }

    [Fact]
    public void Type_DeleteInsert_DataMembers_PartialMerge()
    {
        var srcA1 = @"
partial class C
{
    public int x = 1;
    public int y = 2;
}
";
        var srcB1 = @"
partial class C
{
    public int P { get; set; } = 3;
}";

        var srcA2 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
}
";

        var srcB2 = @"
";
        // note that accessors are not updated since they do not have bodies
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true),
                    ]),

                DocumentResults()
            ]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Type_Move_NamespaceChange(string keyword)
    {
        var declaration = keyword + " C {}";
        var src1 = $"namespace N {{{declaration,-20}}} namespace M {{             }}";
        var src2 = $"namespace N {{                 }} namespace M {{{declaration}}}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [" + declaration + "]@13 -> @45");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, keyword + " C", GetResource(keyword), "N", "M"));
    }

    [Fact]
    public void Type_Move_NamespaceChange_Delegate()
    {
        var src1 = @"namespace N { delegate void F(); } namespace M {                    }";
        var src2 = @"namespace N {                    } namespace M { delegate void F(); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [delegate void F();]@14 -> @49");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "delegate void F()", GetResource("delegate"), "N", "M"));
    }

    [Fact]
    public void Type_Move_NamespaceChange_Subnamespace()
    {
        var src1 = @"namespace N { class C {} } namespace M { namespace O {            } }";
        var src2 = @"namespace N {            } namespace M { namespace O { class C {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [class C {}]@14 -> @55");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M.O"));
    }

    [Fact]
    public void Type_Move_SameEffectiveNamespace()
    {
        var src1 = @"namespace N.M { class C {} } namespace N { namespace M {            } }";
        var src2 = @"namespace N.M {            } namespace N { namespace M { class C {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [class C {}]@16 -> @57");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Type_Move_MultiFile()
    {
        var srcA1 = @"namespace N { class C {} } namespace M {            }";
        var srcB1 = @"namespace N {            } namespace M { class C {} }";
        var srcA2 = @"namespace N {            } namespace M { class C {} }";
        var srcB2 = @"namespace N { class C {} } namespace M {            }";

        var editsA = GetTopEdits(srcA1, srcA2);
        editsA.VerifyEdits(
            "Move [class C {}]@14 -> @41");

        var editsB = GetTopEdits(srcB1, srcB2);
        editsB.VerifyEdits(
            "Move [class C {}]@41 -> @14");

        EditAndContinueValidation.VerifySemantics(
            [editsA, editsB],
            [
                DocumentResults(),
                DocumentResults(),
            ]);
    }

    #endregion

    #region Records

    [Fact]
    public void Record_Insert()
    {
        var src1 = "";
        var src2 = "record C;";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [record C;]@0");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Record_Insert_WithParameters()
    {
        var src1 = "";
        var src2 = "record C(int A);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Record_Name_Update()
    {
        var src1 = "record C { }";
        var src2 = "record D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [record C { }]@0 -> [record D { }]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "record D", GetResource("record", "C")));
    }

    [Fact]
    public void RecordStruct_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "record struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void RecordStruct_AddField()
    {
        var src1 = @"
record struct C(int X)
{
}";
        var src2 = @"
record struct C(int X)
{
    private int _y = 0;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InsertIntoStruct, "_y = 0", FeaturesResources.field, CSharpFeaturesResources.record_struct));
    }

    [Fact]
    public void RecordStruct_AddProperty()
    {
        var src1 = @"
record struct C(int X)
{
}";
        var src2 = @"
record struct C(int X)
{
    public int Y { get; set; } = 0;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InsertIntoStruct, "public int Y", GetResource("auto-property"), GetResource("record struct")));
    }

    [Fact]
    public void Record_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "record C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Record_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { }";
        var src2 = "namespace N { record C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Record_NoModifiers_IntoType_Insert()
    {
        var src1 = "struct N { }";
        var src2 = "struct N { record C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Record_BaseType_Update1()
    {
        var src1 = "record C { }";
        var src2 = "record C : D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: D]@9");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "D", CSharpFeaturesResources.record_));
    }

    [Fact]
    public void Record_BaseType_Update2()
    {
        var src1 = "record C : D1 { }";
        var src2 = "record C : D2 { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: D1]@9 -> [: D2]@9");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "D2", CSharpFeaturesResources.record_));
    }

    [Fact]
    public void Record_BaseInterface_Update1()
    {
        var src1 = "record C { }";
        var src2 = "record C : IDisposable { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [: IDisposable]@9");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "IDisposable", CSharpFeaturesResources.record_));
    }

    [Fact]
    public void Record_BaseInterface_Update2()
    {
        var src1 = "record C : IGoo, IBar { }";
        var src2 = "record C : IGoo { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: IGoo, IBar]@9 -> [: IGoo]@9");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "IGoo", CSharpFeaturesResources.record_));
    }

    [Fact]
    public void Record_BaseInterface_Update3()
    {
        var src1 = "record C : IGoo, IBar { }";
        var src2 = "record C : IBar, IGoo { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: IGoo, IBar]@9 -> [: IBar, IGoo]@9");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "IBar, IGoo", CSharpFeaturesResources.record_));
    }

    [Fact]
    public void Record_Method_Insert_AbstractVirtualOverride()
    {
        var src1 = "";
        var src2 = @"
public abstract record C<T>
{ 
    public abstract void F(); 
    public virtual void G() {}
    public override void H() {}
}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("Equals", "public virtual bool Equals(C rhs) => true;", "C rhs")]
    [InlineData("PrintMembers", "protected virtual bool PrintMembers(System.Text.StringBuilder sb) => true;", "System.Text.StringBuilder sb")]
    [InlineData("Deconstruct", "public void Deconstruct(out int Y) { Y = 1; }", "out int Y")]
    [InlineData(".ctor", "protected C(C other) {}", "C other")]
    public void Record_Method_Insert_ReplacingSynthesizedWithCustom_ParameterNameChanges(string methodName, string methodImpl, string parameterDecl)
    {
        var src1 = "record C(int X) { }";
        var src2 = "record C(int X) { " + methodImpl + " }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, parameterDecl, FeaturesResources.parameter),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => methodName switch { ".ctor" => c.GetCopyConstructor("C"), "Equals" => c.GetSpecializedEqualsOverload("C"), _ => c.GetMember("C." + methodName) }),
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);
    }

    [Fact]
    public void Record_Method_Insert_ReplacingSynthesizedWithCustom_SemanticError()
    {
        var src1 = "record C { }";
        var src2 = @"record C
{
    protected virtual bool PrintMembers(System.Text.StringBuilder sb) => false;
    protected virtual bool PrintMembers(System.Text.StringBuilder sb) => false;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "System.Text.StringBuilder sb", GetResource("parameter")),
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "System.Text.StringBuilder sb", GetResource("parameter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMembers("C.PrintMembers").First().ISymbol),
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("ToString", "public override string ToString() => null;")]
    [InlineData("GetHashCode", "public override int GetHashCode() => 1;")]
    [InlineData("Equals", "public virtual bool Equals(C other) => true;")]
    [InlineData("PrintMembers", "protected virtual bool PrintMembers(System.Text.StringBuilder builder) => true;")]
    [InlineData("Deconstruct", "public void Deconstruct(out int X) { X = 1; }")]
    [InlineData(".ctor", "protected C(C original) {}")]
    public void Record_Method_Insert_ReplacingSynthesizedWithCustom(string methodName, string methodImpl)
    {
        var src1 = "record C(int X) { }";
        var src2 = "record C(int X) { " + methodImpl + " }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => methodName switch { ".ctor" => c.GetCopyConstructor("C"), "Equals" => c.GetSpecializedEqualsOverload("C"), _ => c.GetMember("C." + methodName) }));
    }

    [Theory]
    [InlineData("ToString", "public override string ToString() => null;")]
    [InlineData("GetHashCode", "public override int GetHashCode() => 1;")]
    [InlineData("Equals", "public virtual bool Equals(C other) => true;")]
    [InlineData("PrintMembers", "protected virtual bool PrintMembers(System.Text.StringBuilder builder) => true;")]
    [InlineData("Deconstruct", "public void Deconstruct(out int X) { X = 1; }")]
    [InlineData(".ctor", "protected C(C original) {}")]
    public void Record_Method_Delete_ReplacingCustomWithSynthesized(string methodName, string methodImpl)
    {
        var src1 = "record C(int X) { " + methodImpl + " }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => methodName switch { ".ctor" => c.GetCopyConstructor("C"), "Equals" => c.GetSpecializedEqualsOverload("C"), _ => c.GetMember("C." + methodName) }));

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("Equals", "public virtual bool Equals(C rhs) => true;", "Equals(C rhs)", "rhs")]
    [InlineData("PrintMembers", "protected virtual bool PrintMembers(System.Text.StringBuilder sb) => true;", "PrintMembers(StringBuilder sb)", "sb")]
    [InlineData("Deconstruct", "public void Deconstruct(out int Y) { Y = 1; }", "Deconstruct(out int Y)", "Y")]
    [InlineData(".ctor", "protected C(C other) {}", "C(C other)", "other")]
    public void Record_Method_Delete_ReplacingSynthesizedWithCustom_ParameterNameChanges(string methodName, string methodImpl, string methodDisplay, string parameterDisplay)
    {
        var src1 = "record C(int X) { " + methodImpl + " }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(
                    RudeEditKind.RenamingNotSupportedByRuntime,
                    "record C",
                    GetResource("parameter", parameterDisplay, methodName switch { ".ctor" => "constructor", _ => "method" }, methodDisplay)),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => methodName switch { ".ctor" => c.GetCopyConstructor("C"), "Equals" => c.GetSpecializedEqualsOverload("C"), _ => c.GetMember("C." + methodName) }),
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);
    }

    [Theory]
    [InlineData("method", "ToString()", "public override string ToString() => G(stackalloc int[1]).ToString();")]
    [InlineData("method", "GetHashCode()", "public override int GetHashCode() => G(stackalloc int[1]) ? 0 : 1;")]
    [InlineData("method", "Equals(C other)", "public virtual bool Equals(C other) => G(stackalloc int[1]);")]
    [InlineData("method", "PrintMembers(StringBuilder builder)", "protected virtual bool PrintMembers(System.Text.StringBuilder builder) => G(stackalloc int[1]);")]
    [InlineData("method", "Deconstruct(out int X)", "public void Deconstruct(out int X)  => G(stackalloc int[X = 1]);")]
    [InlineData("constructor", "C(C original)", "protected C(C original) => G(stackalloc int[1]);")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69493")]
    public void Record_Method_Delete_ReplacingCustomWithSynthesized_StackAlloc(string kind, string methodDisplay, string methodImpl)
    {
        var src1 = "record C(int X) { bool G(Span<int> s) => true; " + methodImpl + " }";
        var src2 = "record C(int X) { bool G(Span<int> s) => true; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "record C", GetResource(kind, methodDisplay)));
    }

    [Fact]
    public void Record_Field_Insert()
    {
        var src1 = "record C(int X) { }";
        var src2 = "record C(int X) { private int _y; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Field_Insert_WithExplicitMembers()
    {
        var src1 = @"
record C(int X)
{
    public C(C other)
    {
    }
}";
        var src2 = @"
record C(int X)
{
    private int _y;
    
    public C(C other)
    {
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Field_Insert_WithInitializer()
    {
        var src1 = "record C(int X) { }";
        var src2 = "record C(int X) { private int _y = 1; }";
        var syntaxMap = GetSyntaxMap(src1, src2);

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Field_Insert_WithExistingInitializer()
    {
        var src1 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; }";
        var src2 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; private int _z; }";

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._z")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Field_Insert_WithInitializerAndExistingInitializer()
    {
        var src1 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; }";
        var src2 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; private int _z = 1; }";

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._z")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), syntaxMap[0]),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Field_Delete()
    {
        var src1 = "record C(int X) { private int _y; }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(Diagnostic(RudeEditKind.Delete, "record C", DeletedSymbolDisplay(FeaturesResources.field, "_y")));
    }

    [Fact]
    public void Record_Property_Update_Initializer_NotPrimary()
    {
        var src1 = "record C { int X { get; } = 0; }";
        var src2 = "record C { int X { get; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Length == 0), preserveLocalVariables: true));
    }

    [Fact]
    public void Record_Property_Update_Initializer_Primary()
    {
        var src1 = "record C(int X) { int X { get; } = 0; }";
        var src2 = "record C(int X) { int X { get; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));
    }

    [Theory]
    [InlineData("set")]
    [InlineData("init")]
    public void Record_Property_Delete_Writable(string setter)
    {
        var src1 = "record C(int X) { public int P { get; " + setter + "; } }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("get;")]
    [InlineData("get => 1;")]
    public void Record_Property_Delete_ReadOnly(string getter)
    {
        var src1 = "record C(int X) { public int P { " + getter + " } }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_ReadOnly_ReplacingCustomWithSynthesized()
    {
        var src1 = "record C(int X) { public int X { get => 1; } }";
        var src2 = "record C(int X);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "record C", GetResource("property getter", "X.get"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_ReadOnly_ReplacingCustomWithSynthesized_Generic()
    {
        var src1 = "record C<T>(int X) { public int X { get => 1; } }";
        var src2 = "record C<T>(int X);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities:
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.GenericAddFieldToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "int X", GetResource("property")),
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "record C<T>", GetResource("property getter", "X.get"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_ReadOnly_ReplacingCustomWithSynthesized_AutoProp()
    {
        var src1 = "record C(int X) { public int X { get; } }";
        var src2 = "record C(int X);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Record_Property_Delete_WriteOnly()
    {
        var src1 = "record C(int X) { public int P { set { } } }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_Static()
    {
        var src1 = "record C(int X) { public static int P { get; set; } }";
        var src2 = "record C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_WithInitializer()
    {
        var src1 = @"
record C(int X)
{
    public int Y { get; set; } = 1;

    public C(bool b) : this(1) { }
}";
        var src2 = @"
record C(int X)
{
    public C(bool b) : this(1) { }
}";

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" = 1;")]
    [InlineData(" = X;")]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_Auto(string initializer)
    {
        var src1 = "record C(int X) { public int X { get; init; }" + initializer + " }";
        var src2 = "record C(int X);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_Struct()
    {
        var src1 = "record struct C(int X) { public int X { get; init; } }";
        var src2 = "record struct C(int X);";

        var edits = GetTopEdits(src1, src2);

        // synthesized setter is writable in non-readonly struct
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.AccessorKindUpdate, "record struct C"));
    }

    [Fact]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_ReadOnlyStruct()
    {
        var src1 = "readonly record struct C(int X) { public int X { get; init; } }";
        var src2 = "readonly record struct C(int X);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" = 1;")]
    [InlineData(" = X;")]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_Auto_Partial(string initializer)
    {
        var srcA1 = "partial record C(int X);";
        var srcB1 = "partial record C { public int X { get; init; }" + initializer + " }";

        var srcA2 = "partial record C(int X);";
        var srcB2 = "partial record C;";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ])
            ]);
    }

    [Theory]
    [InlineData("get => 4; init => throw null;")]
    [InlineData("get { return 4; } init { }")]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_WithBody(string body)
    {
        var src1 = "record C(int X) { public int X { " + body + " } }";
        var src2 = "record C(int X);";

        var edits = GetTopEdits(src1, src2);

        // The property changes from custom property to field-backed auto-prop.
        // Methods using backing field must be updated, unless they are explicitly declared.

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Theory]
    [InlineData("PrintMembers", "protected virtual bool PrintMembers(System.Text.StringBuilder builder) => true;")]
    [InlineData("GetHashCode", "public override int GetHashCode() => 1;")]
    [InlineData(".ctor", "protected C(C original) {}")]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_WithBodyAndMethod(string methodName, string methodImpl)
    {
        var src1 = "record C(int X) { " + methodImpl + " public int X { get => 4; init => throw null; } }";
        var src2 = "record C(int X) { " + methodImpl + " }";

        var edits = GetTopEdits(src1, src2);

        // The property changes from custom property to field-backed auto-prop.
        // Methods using backing field must be updated, unless they are explicitly declared.

        var expectedEdits = new List<SemanticEditDescription>();

        if (methodName != "PrintMembers")
        {
            expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")));
        }

        expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")));

        if (methodName != "GetHashCode")
        {
            expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")));
        }

        if (methodName != ".ctor")
        {
            expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")));
        }

        expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod));
        expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod));
        expectedEdits.Add(SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));

        edits.VerifySemantics(
            [.. expectedEdits],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Theory]
    [InlineData("get => 4; init => throw null;")]
    [InlineData("get { return 4; } init { }")]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_WithBody_Partial(string body)
    {
        var srcA1 = "partial record C(int X);";
        var srcB1 = "partial record C { public int X { " + body + " } }";

        var srcA2 = "partial record C(int X);";
        var srcB2 = "partial record C;";

        // The property changes from custom property to field-backed auto-prop.
        // Methods using backing field must be updated, unless they are explicitly declared.

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType : "C", preserveLocalVariables: true),
                    ])
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_WithAttribute()
    {
        var src1 = "record C([property: System.Obsolete]int P) { public int P { get; init; } = P; }";
        var src2 = "record C([property: System.Obsolete]int P) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_WithAttributeOnAccessor()
    {
        var src1 = "record C(int P) { public int P { get; [System.Obsolete] init; } = P; }";
        var src2 = "record C(int P) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "record C", GetResource("property setter", "P.init"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Delete_ReplacingCustomWithSynthesized_TypeLayoutChange()
    {
        var src1 = "record struct C(int P) { public int P { readonly get => 1; set {} } }";
        var src2 = "record struct C(int P);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoStruct, "int P", GetResource("auto-property"), GetResource("record struct"))],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("get; set;")]
    [InlineData("get; init;")]
    [InlineData("get {} set {}")]
    [InlineData("get;")]
    public void Record_Property_Insert(string accessors)
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public int Y { " + accessors + " } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Record_Property_Insert_WriteOnly()
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public int Y { set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Record_Property_Insert_Static()
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public static int Y { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y"))
            ],
            capabilities: EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Record_Property_Insert_WithInitializer()
    {
        var src1 = @"
record C(int X)
{
    public C(bool b) : this(1) { }
}";
        var src2 = @"
record C(int X)
{
    public int Y { get; set; } = 1;

    public C(bool b) : this(1) { }
}";

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("record")]
    [InlineData("readonly record struct")]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithSetter(string keyword)
    {
        var src1 = keyword + " C(int P);";
        var src2 = keyword + " C(int P) { public int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.AccessorKindUpdate, "set"));
    }

    [Fact]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithSetter_WritableStruct()
    {
        var src1 = "record struct C(int P);";
        var src2 = "record struct C(int P) { public int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
        ]);
    }

    [Fact]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_ReadOnly()
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public int X { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_X"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_TypeLayoutChange()
    {
        var src1 = "record struct C(int P);";
        var src2 = "record struct C(int P) { public int P { readonly get => 1; set {} } }";

        var edits = GetTopEdits(src1, src2);

        // Note: We do not report rude edits when a synthesized auto-property is replaced by an explicit one.
        // The synthesized property accessors are updated to throw and the backing field remains in the type.
        // The deleted field will remain unused since adding the primary property back is a rude edit.
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void Record_Property_Insert_NotPrimary_WithExplicitMembers()
    {
        var src1 = @"
record C(int X)
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return false;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public virtual bool Equals(C other)
    {
        return false;
    }

    public C(C original)
    {
    }
}";
        var src2 = @"
record C(int X)
{
    public int Y { get; set; }

    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return false;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public virtual bool Equals(C other)
    {
        return false;
    }

    public C(C original)
    {
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" = 1;")]
    [InlineData(" = X;")]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_Auto(string initializer)
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public int X { get; init; } " + initializer + " }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" = 1;")]
    [InlineData(" = X;")]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_Auto_Partial(string initializer)
    {
        var srcA1 = "partial record C(int X);";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C(int X);";
        var srcB2 = "partial record C { public int X { get; init; }" + initializer + " }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ])
            ]);
    }

    [Theory]
    [InlineData("get => 4; init => throw null;")]
    [InlineData("get { return 4; } init { }")]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithBody(string body)
    {
        var src1 = "record C(int X);";
        var src2 = "record C(int X) { public int X { " + body + " } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("get => 4; init => throw null;")]
    [InlineData("get { return 4; } init { }")]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithBody_Partial(string body)
    {
        var srcA1 = "partial record C(int X);";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C(int X);";
        var srcB2 = "partial record C { public int X { " + body + " } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_X")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ])
            ]);
    }

    [Fact]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithAttribute()
    {
        var src1 = "record C([property: System.Obsolete]int P) { }";
        var src2 = "record C([property: System.Obsolete]int P) { public int P { get; init; } = P; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_Insert_ReplacingSynthesizedWithCustom_WithAttributeOnAccessor()
    {
        var src1 = "record C(int P) { }";
        var src2 = "record C(int P) { public int P { get; [System.Obsolete] init; } = P; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "init", GetResource("property setter"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Record_Property_DeleteInsert()
    {
        var srcA1 = "partial record C(int X) { public int Y { get; init; } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C(int X);";
        var srcB2 = "partial record C { public int Y { get; init; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.Y").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.Y").SetMethod)
                    ]),
            ]);
    }

    #endregion

    #region Enums

    [Fact]
    public void Enum_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "enum C { A }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Enum_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { }";
        var src2 = "namespace N { enum C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Enum_NoModifiers_IntoType_Insert()
    {
        var src1 = "struct N { }";
        var src2 = "struct N { enum C { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Enum_Attribute_Insert()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "enum E { }";
        var src2 = attribute + "[A]enum E { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum E { }]@48 -> [[A]enum E { }]@48");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "enum E", FeaturesResources.enum_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Enum_Member_Attribute_Delete()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "enum E { [A]X }";
        var src2 = attribute + "enum E { X }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]X]@57 -> [X]@57");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "X", FeaturesResources.enum_value)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Enum_Member_Attribute_Insert()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "enum E { X }";
        var src2 = attribute + "enum E { [A]X }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [X]@57 -> [[A]X]@57");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "[A]X", FeaturesResources.enum_value)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Enum_Member_Attribute_Update()
    {
        var attribute = "public class A1Attribute : System.Attribute { }\n\n" +
                        "public class A2Attribute : System.Attribute { }\n\n";

        var src1 = attribute + "enum E { [A1]X }";
        var src2 = attribute + "enum E { [A2]X }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A1]X]@107 -> [[A2]X]@107");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "[A2]X", FeaturesResources.enum_value)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Enum_Member_Attribute_InsertDeleteAndUpdate()
    {
        var srcA1 = "";
        var srcB1 = "enum N { A = 1 }";
        var srcA2 = "enum N { [System.Obsolete]A = 1 }";
        var srcB2 = "";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.A"))
                ]),
                DocumentResults()
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Enum_Rename()
    {
        var src1 = "enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Colors { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Colors { Red = 1, Blue = 2, }]@0");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Renamed, "enum Colors", GetResource("enum", "Color")));
    }

    [Fact]
    public void Enum_BaseType_Add()
    {
        var src1 = "enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color : ushort { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [: ushort]@11");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "ushort", FeaturesResources.enum_));
    }

    [Fact]
    public void Enum_BaseType_Add_Unchanged()
    {
        var src1 = "enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color : int { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [: int]@11");

        edits.VerifySemantics();
    }

    [Fact]
    public void Enum_BaseType_Update()
    {
        var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
        var src2 = "enum Color : long { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [: ushort]@11 -> [: long]@11");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "long", FeaturesResources.enum_));
    }

    [Fact]
    public void Enum_BaseType_Delete_Unchanged()
    {
        var src1 = "enum Color : int { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [: int]@11");

        edits.VerifySemantics();
    }

    [Fact]
    public void Enum_BaseType_Delete_Changed()
    {
        var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [: ushort]@11");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
    }

    [Fact]
    public void EnumAccessibilityChange()
    {
        var src1 = "public enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

        edits.VerifySemantics([SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Color"))]);
    }

    [Fact]
    public void EnumAccessibilityNoChange()
    {
        var src1 = "internal enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1, Blue = 2, }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics();
    }

    [Fact]
    public void EnumInitializerUpdate()
    {
        var src1 = "enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1, Blue = 3, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Blue = 2]@22 -> [Blue = 3]@22");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 3", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumInitializerUpdate2()
    {
        var src1 = "enum Color { Red = 1, Blue = 2, }";
        var src2 = "enum Color { Red = 1 << 0, Blue = 2 << 1, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [Red = 1]@13 -> [Red = 1 << 0]@13",
                          "Update [Blue = 2]@22 -> [Blue = 2 << 1]@27");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 2 << 1", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumInitializerUpdate3()
    {
        var src1 = "enum Color { Red = int.MinValue }";
        var src2 = "enum Color { Red = int.MaxValue }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [Red = int.MinValue]@13 -> [Red = int.MaxValue]@13");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InitializerUpdate, "Red = int.MaxValue", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumInitializerUpdate_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]enum Color { Red = 1 }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]enum Color { Red = 2 }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [Red = 1]@185 -> [Red = 2]@185");

        edits.VerifySemantics(
             [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("Color"))],
             capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void EnumInitializerAdd()
    {
        var src1 = "enum Color { Red, }";
        var src2 = "enum Color { Red = 1, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Red]@13 -> [Red = 1]@13");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumInitializerDelete()
    {
        var src1 = "enum Color { Red = 1, }";
        var src2 = "enum Color { Red, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Red = 1]@13 -> [Red]@13");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InitializerUpdate, "Red", FeaturesResources.enum_value));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
    public void EnumMemberAdd()
    {
        var src1 = "enum Color { Red }";
        var src2 = "enum Color { Red, Blue}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum Color { Red }]@0 -> [enum Color { Red, Blue}]@0",
            "Insert [Blue]@18");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumMemberAdd2()
    {
        var src1 = "enum Color { Red, }";
        var src2 = "enum Color { Red, Blue}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [Blue]@18");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
    public void EnumMemberAdd3()
    {
        var src1 = "enum Color { Red, }";
        var src2 = "enum Color { Red, Blue,}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red, Blue,}]@0",
                          "Insert [Blue]@18");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
    }

    [Fact]
    public void EnumMemberUpdate()
    {
        var src1 = "enum Color { Red }";
        var src2 = "enum Color { Orange }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Red]@13 -> [Orange]@13");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Renamed, "Orange", GetResource("enum value", "Red")));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
    public void EnumMemberDelete()
    {
        var src1 = "enum Color { Red, Blue}";
        var src2 = "enum Color { Red }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum Color { Red, Blue}]@0 -> [enum Color { Red }]@0",
            "Delete [Blue]@18");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Delete, "enum Color", DeletedSymbolDisplay(FeaturesResources.enum_value, "Blue")));
    }

    [Fact]
    public void EnumMemberDelete2()
    {
        var src1 = "enum Color { Red, Blue}";
        var src2 = "enum Color { Red, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [Blue]@18");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Delete, "enum Color", DeletedSymbolDisplay(FeaturesResources.enum_value, "Blue")));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
    public void EnumTrailingCommaAdd()
    {
        var src1 = "enum Color { Red }";
        var src2 = "enum Color { Red, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum Color { Red }]@0 -> [enum Color { Red, }]@0");

        edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
    public void EnumTrailingCommaAdd_WithInitializer()
    {
        var src1 = "enum Color { Red = 1 }";
        var src2 = "enum Color { Red = 1, }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [enum Color { Red = 1 }]@0 -> [enum Color { Red = 1, }]@0");

        edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
    public void EnumTrailingCommaDelete()
    {
        var src1 = "enum Color { Red, }";
        var src2 = "enum Color { Red }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red }]@0");

        edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
    public void EnumTrailingCommaDelete_WithInitializer()
    {
        var src1 = "enum Color { Red = 1, }";
        var src2 = "enum Color { Red = 1 }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [enum Color { Red = 1, }]@0 -> [enum Color { Red = 1 }]@0");

        edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
    }

    #endregion

    #region Delegates

    [Fact]
    public void Delegate_NoModifiers_Insert()
    {
        var src1 = "";
        var src2 = "delegate void D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_NoModifiers_IntoNamespace_Insert()
    {
        var src1 = "namespace N { }";
        var src2 = "namespace N { delegate void D(); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_NoModifiers_IntoType_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { delegate void D(); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_Public_IntoType_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { public delegate void D(); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [public delegate void D();]@10",
            "Insert [()]@32");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_Generic_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { private delegate void D<T>(T a); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [private delegate void D<T>(T a);]@10",
            "Insert [<T>]@33",
            "Insert [(T a)]@36",
            "Insert [T]@34",
            "Insert [T a]@37");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_Delete()
    {
        var src1 = "class C { private delegate void D(); }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [private delegate void D();]@10",
            "Delete [()]@33");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.delegate_, "D")));
    }

    [Fact]
    public void Delegate_Rename()
    {
        var src1 = "public delegate void D();";
        var src2 = "public delegate void Z();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate void D();]@0 -> [public delegate void Z();]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "public delegate void Z()", GetResource("delegate", "D")));
    }

    [Fact]
    public void Delegate_Accessibility_Update()
    {
        var src1 = "public delegate void D();";
        var src2 = "private delegate void D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate void D();]@0 -> [private delegate void D();]@0");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D"))]);
    }

    [Fact]
    public void Delegate_ReturnType_Update_RuntimeTypeChanged()
    {
        var src1 = "public delegate int D();";
        var src2 = "public delegate void D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D();]@0 -> [public delegate void D();]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "public delegate void D()", FeaturesResources.delegate_));
    }

    [Fact]
    public void Delegate_ReturnType_Update_RuntimeTypeUnchanged()
    {
        var src1 = "public delegate object D();";
        var src2 = "public delegate dynamic D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.EndInvoke"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Delegate_ReturnType_AddAttribute()
    {
        var attribute = "public class A : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D(int a);";
        var src2 = attribute + "[return: A]public delegate int D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D(int a);]@39 -> [[return: A]public delegate int D(int a);]@39");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.EndInvoke"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Delegate_Parameter_Insert()
    {
        var src1 = "public delegate int D();";
        var src2 = "public delegate int D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [int a]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "int a", GetResource("delegate")));
    }

    [Fact]
    public void Delegate_Parameter_Insert_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public delegate int D();";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal delegate bool D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("D"))],
             capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_Parameter_Delete()
    {
        var src1 = "public delegate int D(int a);";
        var src2 = "public delegate int D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [int a]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "public delegate int D()", GetResource("delegate")));
    }

    [Fact]
    public void Delegate_Parameter_Rename()
    {
        var src1 = "public delegate int D(int a);";
        var src2 = "public delegate int D(int b);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@22 -> [int b]@22");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", GetResource("parameter"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                 SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                 SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);
    }

    [Fact]
    public void Delegate_Parameter_Update_Type_RuntimeTypeChanged()
    {
        var src1 = "public delegate int D(int a);";
        var src2 = "public delegate int D(byte a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@22 -> [byte a]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "byte a", GetResource("delegate")));
    }

    [Fact]
    public void Delegate_Parameter_Update_Type_RuntimeTypeUnchanged()
    {
        var src1 = "public delegate int D(object a);";
        var src2 = "public delegate int D(dynamic a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")));
    }

    [Fact]
    public void Delegate_Parameter_Update_Attribute()
    {
        var attribute = "public class A : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D(int a);";
        var src2 = attribute + "public delegate int D([A]int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@61 -> [[A]int a]@61");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", GetResource("parameter"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Delegate_TypeParameter_Insert()
    {
        var src1 = "public delegate int D();";
        var src2 = "public delegate int D<T>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [<T>]@21",
            "Insert [T]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "T", FeaturesResources.type_parameter));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54881")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/54881")]
    public void Delegate_TypeParameter_Insert_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public delegate int D<out T>();";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal delegate bool D<in T, out S>(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("D")));
    }

    [Fact]
    public void Delegate_TypeParameter_Delete()
    {
        var src1 = "public delegate int D<T>();";
        var src2 = "public delegate int D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [<T>]@21",
            "Delete [T]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "public delegate int D()", DeletedSymbolDisplay(FeaturesResources.type_parameter, "T")));
    }

    [Fact]
    public void Delegate_TypeParameter_Rename()
    {
        var src1 = "public delegate int D<T>();";
        var src2 = "public delegate int D<S>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@22 -> [S]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "S", GetResource("type parameter", "T")));
    }

    [Fact]
    public void Delegate_TypeParameter_Variance1()
    {
        var src1 = "public delegate int D<T>();";
        var src2 = "public delegate int D<in T>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@22 -> [in T]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
    }

    [Fact]
    public void Delegate_TypeParameter_Variance2()
    {
        var src1 = "public delegate int D<out T>();";
        var src2 = "public delegate int D<T>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [out T]@22 -> [T]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
    }

    [Fact]
    public void Delegate_TypeParameter_Variance3()
    {
        var src1 = "public delegate int D<out T>();";
        var src2 = "public delegate int D<in T>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [out T]@22 -> [in T]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
    }

    [Fact]
    public void Delegate_TypeParameter_AddAttribute()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D<T>();";
        var src2 = attribute + "public delegate int D<[A]T>();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@70 -> [[A]T]@70");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericTypeUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Delegate_Attribute_Add_NotSupportedByRuntime()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D(int a);";
        var src2 = attribute + "[A]public delegate int D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D(int a);]@48 -> [[A]public delegate int D(int a);]@48");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public delegate int D(int a)", FeaturesResources.delegate_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Delegate_Attribute_Add()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D(int a);";
        var src2 = attribute + "[A]public delegate int D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D(int a);]@48 -> [[A]public delegate int D(int a);]@48");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Delegate_Attribute_Add_WithReturnTypeAttribute()
    {
        var attribute = "public class A : System.Attribute { }\n\n";

        var src1 = attribute + "public delegate int D(int a);";
        var src2 = attribute + "[return: A][A]public delegate int D(int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D(int a);]@39 -> [[return: A][A]public delegate int D(int a);]@39");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.EndInvoke")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Delegate_ReadOnlyRef_Parameter_InsertWhole()
    {
        var src1 = "";
        var src2 = "public delegate int D(in int b);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [public delegate int D(in int b);]@0",
            "Insert [(in int b)]@21",
            "Insert [in int b]@22");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_ReadOnlyRef_Parameter_InsertParameter()
    {
        var src1 = "public delegate int D();";
        var src2 = "public delegate int D(in int b);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [in int b]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "in int b", GetResource("delegate")));
    }

    [Fact]
    public void Delegate_ReadOnlyRef_Parameter_Update()
    {
        var src1 = "public delegate int D(int b);";
        var src2 = "public delegate int D(in int b);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int b]@22 -> [in int b]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "in int b", GetResource("delegate")));
    }

    [Fact]
    public void Delegate_ReadOnlyRef_ReturnType_Insert()
    {
        var src1 = "";
        var src2 = "public delegate ref readonly int D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [public delegate ref readonly int D();]@0",
            "Insert [()]@34");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Delegate_ReadOnlyRef_ReturnType_Update()
    {
        var src1 = "public delegate int D();";
        var src2 = "public delegate ref readonly int D();";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public delegate int D();]@0 -> [public delegate ref readonly int D();]@0");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "public delegate ref readonly int D()", FeaturesResources.delegate_));
    }

    #endregion

    #region Nested Types

    [Fact]
    public void NestedType_Replace_WithUpdateInNestedType_Partial_DifferentDocument()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int N() => 1; }";
        var srcB1 = "partial class C { class D { int M() => 1; } }";
        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int N() => 2; }";
        var srcB2 = "partial class C { class D { int M() => 2; } }";

        var editsA = GetTopEdits(srcA1, srcA2);
        var editsB = GetTopEdits(srcB1, srcB2);

        EditAndContinueValidation.VerifySemantics(
            [editsA, editsB],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.D.M"))
                ])
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Replace_WithUpdateInNestedType_Partial_SameDocument()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int N() => 1; } partial class C { class D { int M() => 1; } }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int N() => 2; } partial class C { class D { int M() => 2; } }";

        var edits = GetTopEdits(src1, src2);

        EditAndContinueValidation.VerifySemantics(
            edits,
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.D.M"))
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Replace_WithUpdateInNestedType()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { int N() => 1; class D { int M() => 1; } }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { int N() => 2; class D { int M() => 2; } }";

        var edits = GetTopEdits(src1, src2);

        EditAndContinueValidation.VerifySemantics(
            edits,
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.D.M"))
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Update_WithUpdateInNestedType()
    {
        var src1 = @"[System.Obsolete(""A"")]class C { int N() => 1; class D { int M() => 1; } }";
        var src2 = @"[System.Obsolete(""B"")]class C { int N() => 1; class D { int M() => 2; } }";

        var edits = GetTopEdits(src1, src2);

        EditAndContinueValidation.VerifySemantics(
            edits,
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.D.M"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void NestedType_Move_Sideways()
    {
        var src1 = @"class N { class C {} } class M {            }";
        var src2 = @"class N {            } class M { class C {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Move [class C {}]@10 -> @33");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "class C", GetResource("class")));
    }

    [Fact]
    public void NestedType_Move_Outside()
    {
        var src1 = @"class C { class D { } }";
        var src2 = @"class C { } class D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [class D { }]@10 -> @12");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
    }

    [Fact]
    public void NestedType_Move_Insert()
    {
        var src1 = @"class C { class D { } }";
        var src2 = @"class C { class E { class D { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [class E { class D { } }]@10",
            "Move [class D { }]@10 -> @20");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
    }

    [Fact]
    public void NestedType_MoveAndNamespaceChange()
    {
        var src1 = @"namespace N { class C { class D { } } } namespace M { }";
        var src2 = @"namespace N { class C { } } namespace M { class D { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [class D { }]@24 -> @42");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
    }

    [Fact]
    public void NestedType_Move_MultiFile()
    {
        var srcA1 = @"partial class N { class C {} } partial class M {            }";
        var srcB1 = @"partial class N {            } partial class M { class C {} }";
        var srcA2 = @"partial class N {            } partial class M { class C {} }";
        var srcB2 = @"partial class N { class C {} } partial class M {            }";

        var editsA = GetTopEdits(srcA1, srcA2);
        editsA.VerifyEdits(
            "Move [class C {}]@18 -> @49");

        var editsB = GetTopEdits(srcB1, srcB2);
        editsB.VerifyEdits(
            "Move [class C {}]@49 -> @18");

        EditAndContinueValidation.VerifySemantics(
            [editsA, editsB],
            [
                DocumentResults(),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void NestedType_Move_PartialTypesInSameFile()
    {
        var src1 = @"partial class N { class C {} class D {} } partial class N { }";
        var src2 = @"partial class N { class C {}            } partial class N { class D {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Move [class D {}]@29 -> @60");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_Move_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "class N { [CreateNewOnMetadataUpdate]class C {} } class M { }";
        var src2 = ReloadableAttributeSrc + "class N { } class M { [CreateNewOnMetadataUpdate]class C {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "class C", GetResource("class")));
    }

    [Fact]
    public void NestedType_Insert1()
    {
        var src1 = @"class C {  }";
        var src2 = @"class C { class D { class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [class D { class E { } }]@10",
            "Insert [class E { }]@20");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_Insert2()
    {
        var src1 = @"class C {  }";
        var src2 = @"class C { protected class D { public class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [protected class D { public class E { } }]@10",
            "Insert [public class E { }]@30");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_Insert3()
    {
        var src1 = @"class C {  }";
        var src2 = @"class C { private class D { public class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [private class D { public class E { } }]@10",
            "Insert [public class E { }]@28");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_Insert4()
    {
        var src1 = @"class C {  }";
        var src2 = @"class C { private class D { public D(int a, int b) { } public int P { get; set; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [private class D { public D(int a, int b) { } public int P { get; set; } }]@10",
            "Insert [public D(int a, int b) { }]@28",
            "Insert [public int P { get; set; }]@55",
            "Insert [(int a, int b)]@36",
            "Insert [{ get; set; }]@68",
            "Insert [int a]@37",
            "Insert [int b]@44",
            "Insert [get;]@70",
            "Insert [set;]@75");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_Insert_ReloadableIntoReloadable1()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { [CreateNewOnMetadataUpdate]class D { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
             capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Insert_ReloadableIntoReloadable2()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { [CreateNewOnMetadataUpdate]class D { [CreateNewOnMetadataUpdate]class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
             capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Insert_ReloadableIntoReloadable3()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { class D { [CreateNewOnMetadataUpdate]class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
             capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_Insert_ReloadableIntoReloadable4()
    {
        var src1 = ReloadableAttributeSrc + "class C { }";
        var src2 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { [CreateNewOnMetadataUpdate]class E { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
             SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")));
    }

    [Fact]
    public void NestedType_Insert_Member_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { } }";
        var src2 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { int x; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C.D"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedType_InsertMemberWithInitializer1()
    {
        var src1 = @"
class C
{
}";
        var src2 = @"
class C
{
    private class D
    {
        public int P = 1;
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D"), preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void NestedType_Insert_PInvoke()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    abstract class D 
    {
        public extern D();

        public static extern int P { [DllImport(""msvcrt.dll"")]get; [DllImport(""msvcrt.dll"")]set; }

        [DllImport(""msvcrt.dll"")]
        public static extern int puts(string c);

        [DllImport(""msvcrt.dll"")]
        public static extern int operator +(D d, D g);

        [DllImport(""msvcrt.dll"")]
        public static extern explicit operator int (D d);
    }
}
";
        var edits = GetTopEdits(src1, src2);

        // Adding P/Invoke is not supported by the CLR.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertExtern, "public extern D()", FeaturesResources.constructor),
            Diagnostic(RudeEditKind.InsertExtern, "public static extern int P", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertExtern, "public static extern int puts(string c)", FeaturesResources.method),
            Diagnostic(RudeEditKind.InsertExtern, "public static extern int operator +(D d, D g)", FeaturesResources.operator_),
            Diagnostic(RudeEditKind.InsertExtern, "public static extern explicit operator int (D d)", CSharpFeaturesResources.conversion_operator));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void NestedType_Insert_VirtualAbstract()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    abstract class D 
    {
        public abstract int P { get; }
        public abstract int this[int i] { get; }
        public abstract int puts(string c);

        public virtual event Action E { add { } remove { } }
        public virtual int Q { get { return 1; } }
        public virtual int this[string i] { get { return 1; } }
        public virtual int M(string c) { return 1; }
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_TypeReorder1()
    {
        var src1 = @"class C { struct E { } class F { } delegate void D(); interface I {} }";
        var src2 = @"class C { class F { } interface I {} delegate void D(); struct E { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [struct E { }]@10 -> @56",
            "Reorder [interface I {}]@54 -> @22");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void NestedType_MethodDeleteInsert()
    {
        var src1 = @"public class C { public void goo() {} }";
        var src2 = @"public class C { private class D { public void goo() {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [private class D { public void goo() {} }]@17",
            "Insert [public void goo() {}]@35",
            "Insert [()]@50",
            "Delete [public void goo() {}]@17",
            "Delete [()]@32");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void NestedType_ClassDeleteInsert()
    {
        var src1 = @"public class C { public class X {} }";
        var src2 = @"public class C { public class D { public class X {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [public class D { public class X {} }]@17",
            "Move [public class X {}]@17 -> @34");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.Move, "public class X", FeaturesResources.class_));
    }

    /// <summary>
    /// A new generic type can be added whether it's nested and inherits generic parameters from the containing type, or top-level.
    /// </summary>
    [Fact]
    public void NestedClassGeneric_Insert()
    {
        var src1 = @"
using System;
class C<T>
{
}
";
        var src2 = @"
using System;
class C<T>
{
    class D {}
    struct S {}
    enum N {}
    interface I {}
    delegate void D();
}

class D<T>
{
    
}
";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void NestedEnum_InsertMember()
    {
        var src1 = "struct S { enum N { A = 1 } }";
        var src2 = "struct S { enum N { A = 1, B = 2 } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [enum N { A = 1 }]@11 -> [enum N { A = 1, B = 2 }]@11",
            "Insert [B = 2]@27");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
    public void NestedEnumInPartialType_InsertDelete()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { enum N { A = 1 } }";
        var srcA2 = "partial struct S { enum N { A = 1 } }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults()
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
    public void NestedEnumInPartialType_InsertDeleteAndUpdateMember()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { enum N { A = 1 } }";
        var srcA2 = "partial struct S { enum N { A = 2 } }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.InitializerUpdate, "A = 2", FeaturesResources.enum_value),
                    ]),

                DocumentResults()
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
    public void NestedEnumInPartialType_InsertDeleteAndUpdateBase()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { enum N : uint { A = 1 } }";
        var srcA2 = "partial struct S { enum N : int { A = 1 } }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum N", FeaturesResources.enum_),
                    ]),

                DocumentResults()
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
    public void NestedEnumInPartialType_InsertDeleteAndInsertMember()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { enum N { A = 1 } }";
        var srcA2 = "partial struct S { enum N { A = 1, B = 2 } }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value)]),

                DocumentResults()
            ]);
    }

    [Fact]
    public void NestedDelegateInPartialType_InsertDelete()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { delegate void D(); }";
        var srcA2 = "partial struct S { delegate void D(); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    // delegate does not have any user-defined method body and this does not need a PDB update
                    semanticEdits: NoSemanticEdits),

                DocumentResults()
            ]);
    }

    [Fact]
    public void NestedDelegateInPartialType_InsertDeleteAndChangeParameters()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { delegate void D(); }";
        var srcA2 = "partial struct S { delegate void D(int x); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.TypeUpdate, "delegate void D(int x)", GetResource("delegate"))]),

                DocumentResults()
            ]);
    }

    [Fact]
    public void NestedDelegateInPartialType_InsertDeleteAndChangeReturnType()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { delegate ref int D(); }";
        var srcA2 = "partial struct S { delegate ref readonly int D(); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.TypeUpdate, "delegate ref readonly int D()", FeaturesResources.delegate_)
                    ]),

                DocumentResults()
            ]);
    }

    [Fact]
    public void NestedDelegateInPartialType_InsertDeleteAndChangeOptionalParameterValue()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { delegate void D(int x = 1); }";
        var srcA2 = "partial struct S { delegate void D(int x = 2); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", GetResource("parameter"))
                    ]),

                DocumentResults()
            ]);
    }

    [Fact]
    public void NestedPartialTypeInPartialType_InsertDeleteAndChange()
    {
        var srcA1 = "partial struct S { partial class C { void F1() {} } }";
        var srcB1 = "partial struct S { partial class C { void F2(byte x) {} } }";
        var srcC1 = "partial struct S { }";

        var srcA2 = "partial struct S { partial class C { void F1() {} } }";
        var srcB2 = "partial struct S { }";
        var srcC2 = "partial struct S { partial class C { void F2(int x) {} } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c =>  c.GetMembers("S.C.F2").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Byte))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S.C"))
                    ]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.C.F2").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)])
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Type_Insert_Partial_Multiple()
    {
        var srcA1 = "";
        var srcB1 = "";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"), partialType: "C")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Delete_Partial_Multiple()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = "";
        var srcB2 = "";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(diagnostics: [Diagnostic(RudeEditKind.Delete, null, GetResource("class", "C"))]),
                DocumentResults(diagnostics: [Diagnostic(RudeEditKind.Delete, null, GetResource("class", "C"))]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Type_Partial_InsertDeleteAndChange_Attribute()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "";
        var srcC1 = "partial class C { }";

        var srcA2 = "";
        var srcB2 = "[A]partial class C { }";
        var srcC2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_Partial_InsertDeleteAndChange_TypeParameterAttribute()
    {
        var srcA1 = "partial class C<T> { }";
        var srcB1 = "";
        var srcC1 = "partial class C<T> { }";

        var srcA2 = "";
        var srcB2 = "partial class C<[A]T> { }";
        var srcC2 = "partial class C<T> { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                    ]),

                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    ]),

                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Type_Partial_InsertDeleteAndChange_Constraint()
    {
        var srcA1 = "partial class C<T> { }";
        var srcB1 = "";
        var srcC1 = "partial class C<T> { }";

        var srcA2 = "";
        var srcB2 = "partial class C<T> where T : new() { }";
        var srcC2 = "partial class C<T> { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.ChangingConstraints, "where T : new()", GetResource("type parameter"))
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Type_Partial_InsertDeleteRefactor()
    {
        var srcA1 = "partial class C : I { void F() { } }";
        var srcB1 = "[A][B]partial class C : J { void G() { } }";
        var srcC1 = "";
        var srcD1 = "";

        var srcA2 = "";
        var srcB2 = "";
        var srcC2 = "[A]partial class C : I, J { void F() { } }";
        var srcD2 = "[B]partial class C { void G() { } }";

        var srcE = "interface I {} interface J {}";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2), GetTopEdits(srcE, srcE)],
            [
                DocumentResults(),
                DocumentResults(),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F"))]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("G"))]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Type_Partial_Attribute_AddMultiple()
    {
        var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

        var srcA1 = "partial class C { }" + attributes;
        var srcB1 = "partial class C { }";

        var srcA2 = "[A]partial class C { }" + attributes;
        var srcB2 = "[B]partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Type_Partial_InsertDeleteRefactor_AttributeListSplitting()
    {
        var srcA1 = "partial class C { void F() { } }";
        var srcB1 = "[A,B]partial class C { void G() { } }";
        var srcC1 = "";
        var srcD1 = "";

        var srcA2 = "";
        var srcB2 = "";
        var srcC2 = "[A]partial class C { void F() { } }";
        var srcD2 = "[B]partial class C { void G() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G"))
                ]),
            ]);
    }

    [Fact]
    public void Type_Partial_InsertDeleteChangeMember()
    {
        var srcA1 = "partial class C { void F(int y = 1) { } }";
        var srcB1 = "partial class C { void G(int x = 1) { } }";
        var srcC1 = "";

        var srcA2 = "";
        var srcB2 = "partial class C { void G(int x = 2) { } }";
        var srcC2 = "partial class C { void F(int y = 2) { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(diagnostics: [Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", FeaturesResources.parameter)]),
                DocumentResults(diagnostics: [Diagnostic(RudeEditKind.InitializerUpdate, "int y = 2", FeaturesResources.parameter)]),
            ]);
    }

    [Fact]
    public void NestedPartialTypeInPartialType_InsertDeleteAndInsertVirtual()
    {
        var srcA1 = "partial interface I { partial class C { virtual void F1() {} } }";
        var srcB1 = "partial interface I { partial class C { virtual void F2() {} } }";
        var srcC1 = "partial interface I { partial class C { } }";
        var srcD1 = "partial interface I { partial class C { } }";
        var srcE1 = "partial interface I { }";
        var srcF1 = "partial interface I { }";

        var srcA2 = "partial interface I { partial class C { } }";
        var srcB2 = "";
        var srcC2 = "partial interface I { partial class C { virtual void F1() {} } }"; // move existing virtual into existing partial decl
        var srcD2 = "partial interface I { partial class C { virtual void N1() {} } }"; // insert new virtual into existing partial decl
        var srcE2 = "partial interface I { partial class C { virtual void F2() {} } }"; // move existing virtual into a new partial decl
        var srcF2 = "partial interface I { partial class C { virtual void N2() {} } }"; // insert new virtual into new partial decl

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2), GetTopEdits(srcE1, srcE2), GetTopEdits(srcF1, srcF2)],
            [
                // A
                DocumentResults(),

                // B
                DocumentResults(),

                // C
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember<INamedTypeSymbol>("C").GetMember("F1"))]),

                // D
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.InsertVirtual, "virtual void N1()", FeaturesResources.method)]),

                // E
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember<INamedTypeSymbol>("C").GetMember("F2"))]),

                // F
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.InsertVirtual, "virtual void N2()", FeaturesResources.method)]),
            ]);
    }

    #endregion

    #region Namespaces

    [Fact]
    public void Namespace_Empty_Insert()
    {
        var src1 = @"";
        var src2 = @"namespace C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [namespace C { }]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_InsertNested()
    {
        var src1 = @"namespace C { }";
        var src2 = @"namespace C { namespace D { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [namespace D { }]@14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_DeleteNested()
    {
        var src1 = @"namespace C { namespace D { } }";
        var src2 = @"namespace C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [namespace D { }]@14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_Move()
    {
        var src1 = @"namespace C { namespace D { } }";
        var src2 = @"namespace C { } namespace D { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [namespace D { }]@14 -> @16");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_Reorder1()
    {
        var src1 = @"namespace C { namespace D { } class T { } namespace E { } }";
        var src2 = @"namespace C { namespace E { } class T { } namespace D { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [class T { }]@30 -> @30",
            "Reorder [namespace E { }]@42 -> @14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_Reorder2()
    {
        var src1 = @"namespace C { namespace D1 { } namespace D2 { } namespace D3 { } class T { } namespace E { } }";
        var src2 = @"namespace C { namespace E { }                                    class T { } namespace D1 { } namespace D2 { } namespace D3 { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [class T { }]@65 -> @65",
            "Reorder [namespace E { }]@77 -> @14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_FileScoped_Insert()
    {
        var src1 = @"";
        var src2 = @"namespace N;";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [namespace N;]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Empty_FileScoped_Delete()
    {
        var src1 = @"namespace N;";
        var src2 = @"";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [namespace N;]@0");

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("namespace N; class C {}")]
    [InlineData("namespace N { class C {} }")]
    public void Namespace_Insert_NewType(string src2)
    {
        var src1 = @"";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "class C", GetResource("class"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("namespace N.M { class C {} }")]
    [InlineData("namespace N.M; class C {}")]
    public void Namespace_Insert_NewType_Qualified(string src2)
    {
        var src1 = "";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "class C", GetResource("class"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.M.C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Namespace_Insert(string keyword)
    {
        var declaration = keyword + " X {}";
        var src1 = declaration;
        var src2 = "namespace N { " + declaration + " }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "<global namespace>", "N")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_Delegate()
    {
        var declaration = "delegate void X();";
        var src1 = declaration;
        var src2 = "namespace N { " + declaration + " }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "<global namespace>", "N")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_MultipleDeclarations()
    {
        var src1 = @"class C {} class D {}";
        var src2 = "namespace N { class C {} class D { } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N"),
                Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "<global namespace>", "N")
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_FileScoped()
    {
        var src1 = @"class C {}";
        var src2 = @"namespace N; class C {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_Nested()
    {
        var src1 = @"namespace N { class C {} }";
        var src2 = @"namespace N { namespace M { class C {} } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "N.M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_Qualified()
    {
        var src1 = @"class C {}";
        var src2 = @"namespace N.M { class C {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N.M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Insert_Qualified_FileScoped()
    {
        var src1 = @"class C {}";
        var src2 = @"namespace N.M; class C {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N.M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Namespace_Delete(string keyword)
    {
        var declaration = keyword + " X {}";
        var src1 = "namespace N { " + declaration + " }";
        var src2 = declaration;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "N", "<global namespace>")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Delete_Delegate()
    {
        var declaration = "delegate void X();";
        var src1 = "namespace N { " + declaration + " }";
        var src2 = declaration;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "N", "<global namespace>")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Delete_MultipleDeclarations()
    {
        var src1 = @"namespace N { class C {} class D { } }";
        var src2 = @"class C {} class D {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "<global namespace>"),
                Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "N", "<global namespace>")
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Theory]
    [InlineData("namespace N.M { class C {} }")]
    [InlineData("namespace N.M; class C {}")]
    public void Namespace_Delete_Qualified(string src1)
    {
        var src2 = @"class C {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "<global namespace>")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Qualified_ToFileScoped()
    {
        var src1 = @"namespace N.M { class C {} }";
        var src2 = @"namespace N.M; class C {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Namespace_Update(string keyword)
    {
        var declaration = keyword + " X {}";
        var src1 = "namespace N { " + declaration + " }";
        var src2 = "namespace M { " + declaration + " }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "N", "M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_Delegate()
    {
        var declaration = "delegate void X();";
        var src1 = "namespace N { " + declaration + " }";
        var src2 = "namespace M { " + declaration + " }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "N", "M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_Multiple()
    {
        var src1 = @"namespace N { class C {} class D {} }";
        var src2 = @"namespace M { class C {} class D {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M"),
                Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "N", "M"),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_Qualified1()
    {
        var src1 = @"namespace N.M { class C {} }";
        var src2 = @"namespace N.M.O { class C {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "N.M.O")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_Qualified2()
    {
        var src1 = @"namespace N.M { class C {} }";
        var src2 = @"namespace N { class C {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "N")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_Qualified3()
    {
        var src1 = @"namespace N.M1.O { class C {} }";
        var src2 = @"namespace N.M2.O { class C {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M1.O", "N.M2.O")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_FileScoped()
    {
        var src1 = @"namespace N; class C {}";
        var src2 = @"namespace M; class C {}";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M")],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_MultiplePartials1()
    {
        var srcA1 = @"namespace N { partial class/*1*/C {} } namespace N { partial class/*2*/C {} }";
        var srcB1 = @"namespace N { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";
        var srcA2 = @"namespace N { partial class/*1*/C {} } namespace M { partial class/*2*/C {} }";
        var srcB2 = @"namespace M { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("M.C"), partialType: "M.C"),
                    ]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("M.C"), partialType: "M.C"),
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_MultiplePartials2()
    {
        var srcA1 = @"namespace N { partial class/*1*/C {} } namespace N { partial class/*2*/C {} }";
        var srcB1 = @"namespace N { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";
        var srcA2 = @"namespace M { partial class/*1*/C {} } namespace M { partial class/*2*/C {} }";
        var srcB2 = @"namespace M { partial class/*3*/C {} } namespace M { partial class/*4*/C {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.ChangingNamespace, "partial class/*1*/C", GetResource("class"), "N", "M")
                ]),
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.ChangingNamespace, "partial class/*3*/C", GetResource("class"), "N", "M")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Namespace_Update_MultiplePartials_MergeInNewNamspace()
    {
        var src1 = @"namespace N { partial class C {} } namespace M { partial class C {} }";
        var src2 = @"namespace X { partial class C {} } namespace X { partial class C {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "partial class C", GetResource("class"), "M", "X"),
            Diagnostic(RudeEditKind.Delete, "partial class C", DeletedSymbolDisplay(GetResource("class"), "C")));
    }

    [Fact]
    public void Namespace_Update_MultipleTypesWithSameNameAndArity()
    {
        var src1 = @"namespace N1 { class C {} } namespace N2 { class C {} } namespace O { class C {} }";
        var src2 = @"namespace M1 { class C {} } namespace M2 { class C {} } namespace O { class C {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N2", "M2"),
            Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N1", "M1"));
    }

    [Fact]
    public void Namespace_UpdateAndInsert()
    {
        var src1 = @"namespace N.M { class C {} }";
        var src2 = @"namespace N { namespace M { class C {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_UpdateAndDelete()
    {
        var src1 = @"namespace N { namespace M { class C {} } }";
        var src2 = @"namespace N.M { class C {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Namespace_Move1()
    {
        var src1 = @"namespace N { namespace M { class C {} class C<T> {} } class D {} }";
        var src2 = @"namespace N { class D {} } namespace M { class C<T> {} class C {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [namespace M { class C {} class C<T> {} }]@14 -> @27",
            "Reorder [class C<T> {}]@39 -> @41");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "M"),
            Diagnostic(RudeEditKind.ChangingNamespace, "class C<T>", GetResource("class"), "N.M", "M"));
    }

    [Fact]
    public void Namespace_Move2()
    {
        var src1 = @"namespace N1 { namespace M { class C {} } namespace N2 { } }";
        var src2 = @"namespace N1 { } namespace N2 { namespace M { class C {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Move [namespace N2 { }]@42 -> @17",
            "Move [namespace M { class C {} }]@15 -> @32");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingNamespace, "class C", "class", "N1.M", "N2.M"));
    }

    #endregion

    #region Members

    [Fact]
    public void PartialMember_DeleteInsert_SingleDocument()
    {
        var src1 = @"
using System;

partial class C
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    int this[byte i] { get => 1; set {} }
    event Action E { add {} remove {} }
    event Action EF;
    int F1;
    int F2;
}

partial class C
{
}
";
        var src2 = @"
using System;

partial class C
{
}

partial class C
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    int this[byte i] { get => 1; set {} }
    event Action E { add {} remove {} }
    event Action EF;
    int F1, F2;
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [void M() {}]@68",
            "Insert [int P1 { get; set; }]@85",
            "Insert [int P2 { get => 1; set {} }]@111",
            "Insert [int this[int i] { get => 1; set {} }]@144",
            "Insert [int this[byte i] { get => 1; set {} }]@186",
            "Insert [event Action E { add {} remove {} }]@229",
            "Insert [event Action EF;]@270",
            "Insert [int F1, F2;]@292",
            "Insert [()]@74",
            "Insert [{ get; set; }]@92",
            "Insert [{ get => 1; set {} }]@118",
            "Insert [[int i]]@152",
            "Insert [{ get => 1; set {} }]@160",
            "Insert [[byte i]]@194",
            "Insert [{ get => 1; set {} }]@203",
            "Insert [{ add {} remove {} }]@244",
            "Insert [Action EF]@276",
            "Insert [int F1, F2]@292",
            "Insert [get;]@94",
            "Insert [set;]@99",
            "Insert [get => 1;]@120",
            "Insert [set {}]@130",
            "Insert [int i]@153",
            "Insert [get => 1;]@162",
            "Insert [set {}]@172",
            "Insert [byte i]@195",
            "Insert [get => 1;]@205",
            "Insert [set {}]@215",
            "Insert [add {}]@246",
            "Insert [remove {}]@253",
            "Insert [EF]@283",
            "Insert [F1]@296",
            "Insert [F2]@300",
            "Delete [void M() {}]@43",
            "Delete [()]@49",
            "Delete [int P1 { get; set; }]@60",
            "Delete [{ get; set; }]@67",
            "Delete [get;]@69",
            "Delete [set;]@74",
            "Delete [int P2 { get => 1; set {} }]@86",
            "Delete [{ get => 1; set {} }]@93",
            "Delete [get => 1;]@95",
            "Delete [set {}]@105",
            "Delete [int this[int i] { get => 1; set {} }]@119",
            "Delete [[int i]]@127",
            "Delete [int i]@128",
            "Delete [{ get => 1; set {} }]@135",
            "Delete [get => 1;]@137",
            "Delete [set {}]@147",
            "Delete [int this[byte i] { get => 1; set {} }]@161",
            "Delete [[byte i]]@169",
            "Delete [byte i]@170",
            "Delete [{ get => 1; set {} }]@178",
            "Delete [get => 1;]@180",
            "Delete [set {}]@190",
            "Delete [event Action E { add {} remove {} }]@204",
            "Delete [{ add {} remove {} }]@219",
            "Delete [add {}]@221",
            "Delete [remove {}]@228",
            "Delete [event Action EF;]@245",
            "Delete [Action EF]@251",
            "Delete [EF]@258",
            "Delete [int F1;]@267",
            "Delete [int F1]@267",
            "Delete [F1]@271",
            "Delete [int F2;]@280",
            "Delete [int F2]@280",
            "Delete [F2]@284");

        EditAndContinueValidation.VerifySemantics(
            [edits],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.M")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P1").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P1").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P2").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P2").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Int32").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Int32").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Int32")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Byte").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Byte").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters[0].Type.Name == "Byte")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").AddMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.E").RemoveMethod),
                    ])
            ]);
    }

    [Fact]
    public void PartialMember_InsertDelete_MultipleDocuments()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { void F() {} }";
        var srcA2 = "partial class C { void F() {} }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F"), preserveLocalVariables: false)
                    ]),

                DocumentResults()
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_MultipleDocuments()
    {
        var srcA1 = "partial class C { void F() {} }";
        var srcB1 = "partial class C { }";
        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { void F() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                    ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_GenericMethod()
    {
        var srcA1 = "partial class C { void F<T>() {} }";
        var srcB1 = "partial class C { }";
        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { void F<T>() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                ])
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F<T>()", GetResource("method"))
                ])
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void PartialMember_DeleteInsert_GenericType()
    {
        var srcA1 = "partial class C<T> { void F() {} }";
        var srcB1 = "partial class C<T> { }";
        var srcA2 = "partial class C<T> { }";
        var srcB2 = "partial class C<T> { void F() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                ])
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F()", GetResource("method"))
                ])
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void PartialMember_DeleteInsert_Destructor()
    {
        var srcA1 = "partial class C { ~C() {} }";
        var srcB1 = "partial class C { }";
        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { ~C() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("Finalize"), preserveLocalVariables: false),
                    ])
            ]);
    }

    [Fact]
    public void PartialNestedType_InsertDeleteAndChange()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { class D { void M() {} } interface I { } }";

        var srcA2 = "partial class C { class D : I { void M() {} } interface I { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class D", FeaturesResources.class_),
                    ]),

                DocumentResults()
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
    public void PartialMember_RenameInsertDelete()
    {
        var srcA1 = "partial class C { void F1() {} }";
        var srcB1 = "partial class C { void F2() {} }";
        var srcA2 = "partial class C { void F2() {} }";
        var srcB2 = "partial class C { void F1() {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F2")),
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F1")),
                    ])
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
    public void PartialMember_RenameInsertDelete_SameFile()
    {
        var src1 = """
            partial class C { void F1(int a) {} void F4(int d) {} }
            partial class C { void F3(int c) {} void F2(int b) {} }
            partial class C { }
            """;
        var src2 = """
            partial class C { void F2(int b) {} void F4(int d) {} }
            partial class C { void F1(int a) {} }
            partial class C { void F3(int c) {} }
            """;

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [void F1(int a) {}]@18 -> [void F2(int b) {}]@18",
            "Update [void F3(int c) {}]@75 -> [void F1(int a) {}]@75",
            "Insert [void F3(int c) {}]@114",
            "Insert [(int c)]@121",
            "Update [int a]@26 -> [int b]@26",
            "Update [int c]@83 -> [int a]@83",
            "Insert [int c]@122",
            "Delete [void F2(int b) {}]@93",
            "Delete [(int b)]@100",
            "Delete [int b]@101");

        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F2")),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
    public void PartialMember_SignatureChangeInsertDelete()
    {
        var srcA1 = "partial class C { void F(byte x) {} }";
        var srcB1 = "partial class C { void F(char x) {} }";
        var srcA2 = "partial class C { void F(char x) {} }";
        var srcB2 = "partial class C { void F(byte x) {} }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Char }]))]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.F").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }]))]),
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
    public void PartialMember_SignatureChangeInsertDelete_Indexer()
    {
        var srcA1 = "partial class C { int this[byte x] { get => 1; set {} } }";
        var srcB1 = "partial class C { int this[char x] { get => 1; set {} } }";
        var srcA2 = "partial class C { int this[char x] { get => 1; set {} } }";
        var srcB2 = "partial class C { int this[byte y] { get => 1; set {} } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Char }])),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Char }]).GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Char }]).SetMethod),
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }])),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }]).GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("C.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }]).SetMethod),
                    ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_UpdateMethodBodyError()
    {
        var srcA1 = @"
using System.Collections.Generic;

partial class C
{
    IEnumerable<int> F() { yield return 1; }
}
";
        var srcB1 = @"
using System.Collections.Generic;

partial class C
{
}
";

        var srcA2 = @"
using System.Collections.Generic;

partial class C
{
}
";
        var srcB2 = @"
using System.Collections.Generic;

partial class C
{
    IEnumerable<int> F() { yield return 1; yield return 2; }
}
";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                ])
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void PartialMember_DeleteInsert_UpdatePropertyAccessors()
    {
        var srcA1 = "partial class C { int P { get => 1; set { Console.WriteLine(1); } } }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int P { get => 2; set { Console.WriteLine(2); } } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod)
                ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_UpdateAutoProperty()
    {
        var srcA1 = "partial class C { int P => 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int P => 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod)
                ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_AddFieldInitializer()
    {
        var srcA1 = "partial class C { int f; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int f = 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_RemoveFieldInitializer()
    {
        var srcA1 = "partial class C { int f = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int f; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_ConstructorWithInitializers()
    {
        var srcA1 = "partial class C { int f = 1; C(int x) { f = x; } }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { int f = 1; }";
        var srcB2 = "partial class C { C(int x) { f = x + 1; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                ])
            ]);
    }

    [Fact]
    public void PartialMember_DeleteInsert_MethodAddParameter()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { void F() {} }";
        var srcA2 = "partial struct S { void F(int x) {} }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void PartialMember_DeleteInsert_UpdateMethodParameterType()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { void F(int x); }";
        var srcA2 = "partial struct S { void F(byte x); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Byte))?.ISymbol)
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void PartialMember_DeleteInsert_MethodAddTypeParameter()
    {
        var srcA1 = "partial struct S { }";
        var srcB1 = "partial struct S { void F(); }";
        var srcA2 = "partial struct S { void F<T>(); }";
        var srcB2 = "partial struct S { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetArity() == 1)?.ISymbol)
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetArity() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType);
    }

    #endregion

    #region Methods

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Method_Update_Modifiers_Accessibility_Significant(string accessibility)
    {
        var src1 = $$"""
            class C
            {
                {{accessibility}}
                int F() => 0; 
            }
            """;

        var src2 = """
            class C
            {
                
                int F() => 0; 
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
            ]);
    }

    [Fact]
    public void Method_Update_Modifiers_Accessibility_Insignificant()
    {
        var src1 = "class C { private int F() => 0; }";
        var src2 = "class C {         int F() => 0; }";

        var edits = GetTopEdits(src1, src2);

        // the update is not necessary and can be eliminated:
        edits.VerifySemantics([SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))]);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("sealed override", "override")]
    public void Method_Update_Modifiers_Update(string oldModifiers, string newModifiers = "")
    {
        if (oldModifiers != "")
        {
            oldModifiers += " ";
        }

        if (newModifiers != "")
        {
            newModifiers += " ";
        }

        var src1 = "class C { " + oldModifiers + "int F() => 0; }";
        var src2 = "class C { " + newModifiers + "int F() => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [" + oldModifiers + "int F() => 0;]@10 -> [" + newModifiers + "int F() => 0;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F()", FeaturesResources.method));
    }

    [Fact]
    public void Method_Update_Modifiers_New_Add()
    {
        var src1 = "class C { int F() => 0; }";
        var src2 = "class C { new int F() => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [int F() => 0;]@10 -> [new int F() => 0;]@10");

        // Currently, an edit is produced eventhough there is no metadata/IL change. Consider improving.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")));
    }

    [Fact]
    public void Method_Update_Modifiers_New_Remove()
    {
        var src1 = "class C { new int F() => 0; }";
        var src2 = "class C { int F() => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [new int F() => 0;]@10 -> [int F() => 0;]@10");

        // Currently, an edit is produced eventhough there is no metadata/IL change. Consider improving.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")));
    }

    [Fact]
    public void Method_Update_Modifiers_ReadOnly_Add_InMutableStruct()
    {
        var src1 = @"
struct S
{
    public int M() => 1;
}";
        var src2 = @"
struct S
{
    public readonly int M() => 1;
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.M")));
    }

    [Fact]
    public void Method_Update_Modifiers_ReadOnly_Add_InReadOnlyStruct1()
    {
        var src1 = @"
readonly struct S
{
    public int M()
        => 1;
}";
        var src2 = @"
readonly struct S
{
    public readonly int M()
        => 1;
}";

        var edits = GetTopEdits(src1, src2);

        // Currently, an edit is produced eventhough the body nor IsReadOnly attribute have changed. Consider improving.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IMethodSymbol>("M")));
    }

    [Fact]
    public void Method_Update_Modifiers_ReadOnly_Add_InReadOnlyStruct2()
    {
        var src1 = @"
readonly struct S
{
    public int M() => 1;
}";
        var src2 = @"
struct S
{
    public readonly int M() => 1;
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "struct S", "struct"));
    }

    [Fact]
    public void Method_Update_Modifiers_Async_Remove()
    {
        var src1 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        return 1;
    }
}";
        var src2 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return Task.FromResult(1);
    }
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "public Task<int> WaitAsync()", FeaturesResources.method));
    }

    [Fact]
    public void Method_Update_Modifiers_Async_Add()
    {
        var src1 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return 1;
    }
}";
        var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition | EditAndContinueCapabilities.AddExplicitInterfaceImplementation);

        VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
    }

    [Fact]
    public void Method_Update_Modifiers_Async_Add_NotSupported()
    {
        var src1 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return 1;
    }
}";
        var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.MakeMethodAsyncNotSupportedByRuntime, "public async Task<int> WaitAsync()")],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("object", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Method_Update_ReturnType_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "class C { " + oldType + " M() => default; }";
        var src2 = "class C { " + newType + " M() => default; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")));
    }

    [Theory]
    [InlineData("int", "string")]
    [InlineData("int", "int?")]
    [InlineData("(int a, int b)", "(int a, double b)")]
    public void Method_Update_ReturnType_RuntimeTypeChanged(string oldType, string newType)
    {
        var src1 = "class C { " + oldType + " M() => default; }";
        var src2 = "class C { " + newType + " M() => default; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, newType + " M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_ReturnType_WithBodyChange()
    {
        var src1 = "class C { int M() => 1; }";
        var src2 = "class C { char M() => 'a'; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "char M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_ReturnType_Tuple()
    {
        var src1 = "class C { (int, int) M() { throw new System.Exception(); } }";
        var src2 = "class C { (string, int) M() { throw new System.Exception(); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [(int, int) M() { throw new System.Exception(); }]@10 -> [(string, int) M() { throw new System.Exception(); }]@10");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "(string, int) M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_ReturnType_Tuple_ElementDelete()
    {
        var src1 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";
        var src2 = "class C { (int, int) M() { return (1, 2); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [(int, int, int a) M() { return (1, 2, 3); }]@10 -> [(int, int) M() { return (1, 2); }]@10");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "(int, int) M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_ReturnType_Tuple_ElementAdd()
    {
        var src1 = "class C { (int, int) M() { return (1, 2); } }";
        var src2 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [(int, int) M() { return (1, 2); }]@10 -> [(int, int, int a) M() { return (1, 2, 3); }]@10");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "(int, int, int a) M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update()
    {
        var src1 = @"
class C
{
    static void F()
    {
        int a = 1;
        int b = 2;
        System.Console.WriteLine(a + b);
    }
}
";
        var src2 = @"
class C
{
    static void F()
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Update [static void F()
    {
        int a = 1;
        int b = 2;
        System.Console.WriteLine(a + b);
    }]@18 -> [static void F()
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }]@18");

        edits.VerifySemanticDiagnostics();

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)]);
    }

    [Fact]
    public void MethodWithExpressionBody_Update()
    {
        var src1 = @"
class C
{
    static int M() => F(1);
    static int F(int a) => 1;
}
";
        var src2 = @"
class C
{
    static int M() => F(2);
    static int F(int a) => 1;
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Update [static int M() => F(1);]@18 -> [static int M() => F(2);]@18");

        edits.VerifySemanticDiagnostics();

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"), preserveLocalVariables: false)]);
    }

    [Fact]
    public void MethodWithExpressionBody_ToBlockBody()
    {
        var src1 = "class C { static int F(int a) => 1; }";
        var src2 = "class C { static int F(int a) { return 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [static int F(int a) => 1;]@10 -> [static int F(int a) { return 2; }]@10");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void MethodWithBlockBody_ToExpressionBody()
    {
        var src1 = "class C { static int F(int a) { return 2; } }";
        var src2 = "class C { static int F(int a) => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [static int F(int a) { return 2; }]@10 -> [static int F(int a) => 1;]@10");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void MethodWithLambda_Update()
    {
        var src1 = @"
using System;

class C
{
    static void F()
    {
        Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
        Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };
    }
}
";
        var src2 = @"
using System;

class C
{
    static void F()
    {
        Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
        Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

        Console.WriteLine(1);
    }
}";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), syntaxMap[0])]);
    }

    [Fact]
    public void MethodUpdate_LocalVariableDeclaration()
    {
        var src1 = @"
class C
{
    static void F()
    {
        int x = 1;
        Console.WriteLine(x);
    }
}
";
        var src2 = @"
class C
{
    static void F()
    {
        int x = 2;
        Console.WriteLine(x);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
@"Update [static void F()
    {
        int x = 1;
        Console.WriteLine(x);
    }]@18 -> [static void F()
    {
        int x = 2;
        Console.WriteLine(x);
    }]@18");
    }

    [Fact]
    public void Method_Delete()
    {
        var src1 = @"
class C
{
    void goo() { }
}
";
        var src2 = @"
class C
{
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [void goo() { }]@18",
            "Delete [()]@26");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    public void Method_Delete_Modifiers(string modifier)
    {
        /* TODO: https://github.com/dotnet/roslyn/issues/59264

           This should be a supported edit. Consider the following inheritance chain:

            public class C { public virtual void M() => Console.WriteLine("C"); } 
            public class D : C { public override void M() { base.M(); Console.WriteLine("D"); } } 
            public class E : D { public override void M() { base.M(); Console.WriteLine("E"); } } 

            If D.M is deleted we expect E.M to print "C E" and not throw.

        */
        var src1 = $$"""
            class C
            {
                {{modifier}} void goo() { }
            }
            """;
        var src2 = """
            class C
            {
            }
            """;
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            $"Delete [{modifier} void goo() {{ }}]@16",
            $"Delete [()]@{25 + modifier.Length}");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.method, "goo()")));
    }

    [Fact]
    public void MethodWithExpressionBody_Delete()
    {
        var src1 = @"
class C
{
    int goo() => 1;
}
";
        var src2 = @"
class C
{
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [int goo() => 1;]@18",
            "Delete [()]@25");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
    public void MethodDelete_WithParameterAndAttribute()
    {
        var src1 = @"
class C
{
    [Obsolete]
    void goo(int a) { }
}
";
        var src2 = @"
class C
{
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Delete [[Obsolete]
    void goo(int a) { }]@18",
            "Delete [(int a)]@42",
            "Delete [int a]@43");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
    public void MethodDelete_PInvoke()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);
}
";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Delete [[DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);]@74",
             "Delete [(string c)]@134",
             "Delete [string c]@135");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.method, "puts(string c)")));
    }

    [Fact]
    public void MethodInsert_NotSupportedByRuntime()
    {
        var src1 = "class C {  }";
        var src2 = "class C { void goo() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "void goo()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void PrivateMethodInsert()
    {
        var src1 = @"
class C
{
    static void F()
    {
        Console.ReadLine();
    }
}";
        var src2 = @"
class C
{
    void goo() { }

    static void F()
    {
        Console.ReadLine();
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [void goo() { }]@18",
            "Insert [()]@26");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
    public void PrivateMethodInsert_WithParameters()
    {
        var src1 = @"
using System;

class C
{
    static void F()
    {
        Console.ReadLine();
    }
}";
        var src2 = @"
using System;

class C
{
    void goo(int a) { }

    static void F()
    {
        Console.ReadLine();
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [void goo(int a) { }]@35",
            "Insert [(int a)]@43",
            "Insert [int a]@44");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.goo"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
    public void PrivateMethodInsert_WithAttribute()
    {
        var src1 = @"
class C
{
    static void F()
    {
        Console.ReadLine();
    }
}";
        var src2 = @"
class C
{
    [System.Obsolete]
    void goo(int a) { }

    static void F()
    {
        Console.ReadLine();
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Insert [[System.Obsolete]
    void goo(int a) { }]@18",
            "Insert [(int a)]@49",
            "Insert [int a]@50");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void MethodInsert_Virtual()
    {
        var src1 = @"
class C
{
}";
        var src2 = @"
class C
{
    public virtual void F() {}
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertVirtual, "public virtual void F()", FeaturesResources.method));
    }

    [Fact]
    public void MethodInsert_Abstract()
    {
        var src1 = @"
abstract class C
{
}";
        var src2 = @"
abstract class C
{
    public abstract void F();
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertVirtual, "public abstract void F()", FeaturesResources.method));
    }

    [Fact]
    public void MethodInsert_Override()
    {
        var src1 = @"
class C
{
}";
        var src2 = @"
class C
{
    public override void F() { }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertVirtual, "public override void F()", FeaturesResources.method));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void ExternMethod_Insert()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            @"Insert [[DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);]@74",
            "Insert [(string c)]@135",
            "Insert [string c]@136");

        // CLR doesn't support methods without a body
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertExtern, "private static extern int puts(string c)", FeaturesResources.method));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void ExternMethod_DeleteInsert()
    {
        var srcA1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}";
        var srcA2 = @"
using System;
using System.Runtime.InteropServices;
";

        var srcB1 = @"
using System;
using System.Runtime.InteropServices;
";
        var srcB2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}
";
        // TODO: The method does not need to be updated since there are no sequence points generated for it.
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.puts")),
                ])
            ]);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void ExternMethod_Attribute_DeleteInsert()
    {
        var srcA1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}";
        var srcA2 = @"
using System;
using System.Runtime.InteropServices;
";

        var srcB1 = @"
using System;
using System.Runtime.InteropServices;
";
        var srcB2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    [Obsolete]
    private static extern int puts(string c);
}
";
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.puts")),
                ])
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void MethodReorder1()
    {
        var src1 = "class C { void f(int a, int b) { a = b; } void g() { } }";
        var src2 = "class C { void g() { } void f(int a, int b) { a = b; } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits("Reorder [void g() { }]@42 -> @10");
    }

    [Fact]
    public void MethodInsertDelete1()
    {
        var src1 = "class C { class D { } void f(int a, int b) { a = b; } }";
        var src2 = "class C { class D { void f(int a, int b) { a = b; } } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Insert [void f(int a, int b) { a = b; }]@20",
            "Insert [(int a, int b)]@26",
            "Insert [int a]@27",
            "Insert [int b]@34",
            "Delete [void f(int a, int b) { a = b; }]@22",
            "Delete [(int a, int b)]@28",
            "Delete [int a]@29",
            "Delete [int b]@36");
    }

    [Fact]
    public void Method_Update_Parameter_Insert()
    {
        var src1 = @"
class C
{
    static void F()
    {
        
    }
}";
        var src2 = @"
class C
{
    static void F(int a)
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [int a]@32");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int a", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Parameter_Insert_Multiple()
    {
        var src1 = @"
class C
{
    void M(int a)
    {
    }
}";
        var src2 = @"
class C
{
    void M(int a, int b, int c)
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int b", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Parameter_Insert_Partial()
    {
        var src1 = @"
class C
{
    partial void M(int a);

    partial void M(int a)
    {
    }
}";
        var src2 = @"
class C
{
    partial void M(int a, int/*1*/b, int c);

    partial void M(int a, int/*2*/b, int c)
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.M").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.M").PartialImplementationPart, partialType: "C")
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int/*1*/b", GetResource("method")),
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int/*2*/b", GetResource("method"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Type()
    {
        var src1 = @"
class C
{
    static void Main(bool x)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void Main(int x)
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [bool x]@35 -> [int x]@35");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Boolean))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int x", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Parameter_Type_WithRename()
    {
        var src1 = @"
class C
{
    static void Main(bool someBool)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void Main(int someInt)
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [bool someBool]@35 -> [int someInt]@35");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Boolean))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Method_Update_Parameter_Delete()
    {
        var src1 = @"
class C
{
    static void F(int a)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void F()
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [int a]@32");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "static void F()", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Parameter_Delete_Multiple()
    {
        var src1 = @"
class C
{
    void M(int a, int b, int c)
    {
    }
}";
        var src2 = @"
class C
{
    void M(int a)
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "void M(int a)", GetResource("method"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Update_Parameter_Rename()
    {
        var src1 = @"
class C
{
    static void F(int a)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void F(int b)
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@32 -> [int b]@32");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);
    }

    [Fact]
    public void Method_Update_Parameter_Rename_WithBodyUpdate()
    {
        var src1 = @"
class C
{
    static void F(int a)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void F(int b)
    {
        System.Console.Write(1);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);
    }

    [Fact]
    public void Method_Rename()
    {
        var src1 = @"
class C
{
    static void F(int a)
    {
        
    }
}";
        var src2 = @"
class C
{
    static void G(int a)
    {
        
    }
}";
        var edits = GetTopEdits(src1, src2);

        var expectedEdit = @"Update [static void F(int a)
    {
        
    }]@18 -> [static void G(int a)
    {
        
    }]@18";

        edits.VerifyEdits(expectedEdit);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.G"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void G(int a)", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_Rename_GenericType()
    {
        var src1 = @"
class C<T>
{
    static void F()
    {
    }
}";
        var src2 = @"
class C<T>
{
    static void G()
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void G()", FeaturesResources.method)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void G()", FeaturesResources.method)
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.G"))
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void Method_Rename_GenericMethod()
    {
        var src1 = @"
class C
{
    static void F<T>()
    {
    }
}";
        var src2 = @"
class C
{
    static void G<T>()
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void G<T>()", GetResource("method"))
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void G<T>()", GetResource("method"))
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.G"))
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Theory]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    public void Method_Rename_Modifiers(string modifier)
    {
        /* TODO: https://github.com/dotnet/roslyn/issues/59264

           This should be a supported edit. Consider the following inheritance chain:

            public class C { public virtual void M() => Console.WriteLine("C"); } 
            public class D : C { public override void M() { base.M(); Console.WriteLine("D"); } } 
            public class E : D { public override void M() { base.M(); Console.WriteLine("E"); } } 

            If D.M is deleted we expect E.M to print "C E" and not throw.

        */
        var src1 = $$"""
            class C
            {
                {{modifier}} void goo() { }
            }
            """;
        var src2 = $$"""
            class C
            {
                {{modifier}} void boo() { }
            }
            """;
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            $"Update [{modifier} void goo() {{ }}]@16 -> [{modifier} void boo() {{ }}]@16");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, $"{modifier} void boo()", GetResource("method", "goo()")));
    }

    [Fact]
    public void MethodUpdate_AsyncMethod0()
    {
        var src1 = @"
class C
{
    public async Task F()
    {
        await Task.Delay(1000);
    }
}";
        var src2 = @"
class C
{
    public async Task F()
    {
        await Task.Delay(500);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, "public async Task F()")],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AsyncMethod1()
    {
        var src1 = """
        class Test
        {
            static void F()
            {
                Test f = new Test();
                string result = f.WaitAsync().Result;
            }

            public async Task<string> WaitAsync()
            {
                await Task.Delay(1000);
                return "Done";
            }
        }
        """;

        var src2 = """
        class Test
        {
            static void F()
            {
                Test f = new Test();
                string result = f.WaitAsync().Result;
            }

            public async Task<string> WaitAsync()
            {
                await Task.Delay(1000);
                return "Not Done";
            }
        }
        """;

        var edits = GetTopEdits(src1, src2);
        var expectedEdit = """
        Update [public async Task<string> WaitAsync()
            {
                await Task.Delay(1000);
                return "Done";
            }]@133 -> [public async Task<string> WaitAsync()
            {
                await Task.Delay(1000);
                return "Not Done";
            }]@133
        """;

        edits.VerifyEdits(expectedEdit);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void MethodUpdate_AsyncMethod_Generic()
    {
        var src1 = @"
class C
{
    public async Task F<T>()
    {
        await Task.FromResult(1);
    }
}";
        var src2 = @"
class C
{
    public async Task F<T>()
    {
        await Task.FromResult(2);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities:
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod |
                EditAndContinueCapabilities.GenericAddFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, "public async Task F<T>()"),
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "public async Task F<T>()", GetResource("method")),
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "public async Task F<T>()", GetResource("method"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AddReturnTypeAttribute()
    {
        var src1 = @"
using System;

class Test
{
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [return: Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [static void F()
    {
        System.Console.Write(5);
    }]@38 -> [[return: Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }]@38");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AddAttribute()
    {
        var src1 = @"
using System;

class Test
{
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [static void F()
    {
        System.Console.Write(5);
    }]@38 -> [[Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }]@38");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AttributeWithTypeAtConstructor()
    {
        var src1 = "using System; [AttributeUsage(AttributeTargets.All)] public class AAttribute : Attribute { public AAttribute(Type t) { } } class C { [A(typeof(C))] public void M() { Console.WriteLine(\"2\"); } }";
        var src2 = "using System; [AttributeUsage(AttributeTargets.All)] public class AAttribute : Attribute { public AAttribute(Type t) { } } class C { [A(typeof(C))] public void M() { Console.WriteLine(\"1\"); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [[A(typeof(C))] public void M() { Console.WriteLine(""2""); }]@133 -> [[A(typeof(C))] public void M() { Console.WriteLine(""1""); }]@133");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_AttributeWithTypeAtConstructor2()
    {
        var src1 = "using System; [AttributeUsage(AttributeTargets.All)] public class AAttribute : Attribute { public AAttribute(Type t) { } } class C { [A(typeof(object))] public void M() { Console.WriteLine(\"2\"); } }";
        var src2 = "using System; [AttributeUsage(AttributeTargets.All)] public class AAttribute : Attribute { public AAttribute(Type t) { } } class C { [A(typeof(dynamic))] public void M() { Console.WriteLine(\"1\"); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [[A(typeof(object))] public void M() { Console.WriteLine(""2""); }]@133 -> [[A(typeof(dynamic))] public void M() { Console.WriteLine(""1""); }]@133");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_AddAttribute_SupportedByRuntime()
    {
        var src1 = """
        using System;

        class Test
        {
            static void F()
            {
                System.Console.Write(5);
            }
        }
        """;

        var src2 = """
        using System;

        class Test
        {
            [Obsolete]
            static void F()
            {
                System.Console.Write(5);
            }
        }
        """;

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("""
        Update [static void F()
            {
                System.Console.Write(5);
            }]@36 -> [[Obsolete]
            static void F()
            {
                System.Console.Write(5);
            }]@36
        """);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Test.F"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void MethodUpdate_Attribute_ArrayParameter()
    {
        var src1 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
    }
}";
        var src2 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 4, 5, 6})]
    void M()
    {
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void MethodUpdate_Attribute_ArrayParameter_NoChange()
    {
        var src1 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
        var x = 1;
    }
}";
        var src2 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
        var x = 2;
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))]);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_AddAttribute2()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AddAttribute3()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete]
    [STAThread]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_AddAttribute4()
    {
        var src1 = @"
using System;

class Test
{
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_UpdateAttribute()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete("""")]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
    public void MethodUpdate_DeleteAttribute()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_DeleteAttribute2()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_DeleteAttribute3()
    {
        var src1 = @"
using System;

class Test
{
    [Obsolete]
    [STAThread]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void F()
    {
        System.Console.Write(5);
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void F()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_ExplicitlyImplemented1()
    {
        var src1 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(2); }
    void J.Goo() { Console.WriteLine(1); }
}";
        var src2 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [void I.Goo() { Console.WriteLine(2); }]@25 -> [void I.Goo() { Console.WriteLine(1); }]@25",
            "Update [void J.Goo() { Console.WriteLine(1); }]@69 -> [void J.Goo() { Console.WriteLine(2); }]@69");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_ExplicitlyImplemented2()
    {
        var interfaces = @"
interface I { void Goo(); }
interface J { void Goo(); }
";

        var src1 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}
" + interfaces;

        var src2 = @"
class C : I, J
{
    void Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}
" + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [void I.Goo() { Console.WriteLine(1); }]@25 -> [void Goo() { Console.WriteLine(1); }]@25");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "void Goo()", GetResource("method", "I.Goo()")));
    }

    [Fact]
    public void MethodUpdate_StackAlloc_Update()
    {
        var src1 = @"
class C
{
    static void Main() 
    { 
        int i = 1;
        unsafe
        {
            char* buffer = stackalloc char[16];
            int* px2 = &i;
        }
    }
}";
        var src2 = @"
class C
{
    static void Main() 
    { 
        int i = 2;
        unsafe
        {
            char* buffer = stackalloc char[16];
            int* px2 = &i;
        }
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc char[16]", FeaturesResources.method),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "static void Main()", GetResource("method"))
        ]);
    }

    [Fact]
    public void MethodUpdate_StackAlloc_Insert()
    {
        var src1 = @"
class C
{
    static void F() 
    { 
        int i = 10;
        unsafe
        {
            int* px2 = &i;
        }
    }
}";
        var src2 = @"
class C
{
    static void F() 
    { 
        int i = 10;
        unsafe
        {
            char* buffer = stackalloc char[16];
            int* px2 = &i;
        }
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc char[16]", FeaturesResources.method));
    }

    [Fact]
    public void MethodUpdate_StackAlloc_Delete()
    {
        var src1 = @"
class C
{
    static void F() 
    { 
        int i = 10;
        unsafe
        {
            char* buffer = stackalloc char[16];
            int* px2 = &i;
        }
    }
}";
        var src2 = @"
class C
{
    static void F() 
    { 
        int i = 10;
        unsafe
        {
            int* px2 = &i;
        }
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "static void F()", FeaturesResources.method));
    }

    [Fact]
    public void MethodUpdate_SwitchExpressionInLambda1()
    {
        var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
        var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_SwitchExpressionInLambda2()
    {
        var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
        var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_SwitchExpressionInAnonymousMethod()
    {
        var src1 = "class C { void M() { F(1, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";
        var src2 = "class C { void M() { F(2, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_SwitchExpressionInLocalFunction()
    {
        var src1 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(1); } }";
        var src2 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(2); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_SwitchExpressionInQuery()
    {
        var src1 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 1; } }";
        var src2 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_UpdateAnonymousMethod()
    {
        var src1 = "class C { void M() { F(1, delegate(int a) { return a; }); } }";
        var src2 = "class C { void M() { F(2, delegate(int a) { return a; }); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodWithExpressionBody_Update_UpdateAnonymousMethod()
    {
        var src1 = "class C { void M() => F(1, delegate(int a) { return a; }); }";
        var src2 = "class C { void M() => F(2, delegate(int a) { return a; }); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_AnonymousType()
    {
        var src1 = "class C { void M() { F(1, new { A = 1, B = 2 }); } }";
        var src2 = "class C { void M() { F(2, new { A = 1, B = 2 }); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodWithExpressionBody_Update_AnonymousType()
    {
        var src1 = "class C { void M() => F(new { A = 1, B = 2 }); }";
        var src2 = "class C { void M() => F(new { A = 10, B = 20 }); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_Iterator_YieldReturn()
    {
        var src1 = "class C { IEnumerable<int> M() { yield return 1; } }";
        var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
    }

    [Fact]
    public void MethodUpdate_AddYieldReturn()
    {
        var src1 = "class C { IEnumerable<int> M() { return new[] { 1, 2, 3}; } }";
        var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.NewTypeDefinition | EditAndContinueCapabilities.AddExplicitInterfaceImplementation);

        VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
    }

    [Fact]
    public void MethodUpdate_AddYieldReturn_NotSupported()
    {
        var src1 = "class C { IEnumerable<int> M() { return new[] { 1, 2, 3}; } }";
        var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.MakeMethodIteratorNotSupportedByRuntime, "IEnumerable<int> M()")],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodUpdate_Iterator_YieldBreak()
    {
        var src1 = "class C { IEnumerable<int> M() { F(); yield break; } }";
        var src2 = "class C { IEnumerable<int> M() { G(); yield break; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087305")]
    public void MethodUpdate_LabeledStatement()
    {
        var src1 = @"
class C
{
    static void F()
    {
        goto Label1;
 
    Label1:
        {
            Console.WriteLine(1);
        }
    }
}";
        var src2 = @"
class C
{
    static void F()
    {
        goto Label1;
 
    Label1:
        {
            Console.WriteLine(2);
        }
    }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MethodUpdate_LocalFunctionsParameterRefnessInBody()
    {
        var src1 = @"class C { public void M(int a) { void f(ref int b) => b = 1; } }";
        var src2 = @"class C { public void M(int a) { void f(out int b) => b = 1; } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M(int a) { void f(ref int b) => b = 1; }]@10 -> [public void M(int a) { void f(out int b) => b = 1; }]@10");
    }

    [Fact]
    public void MethodUpdate_LambdaParameterRefnessInBody()
    {
        var src1 = @"class C { public void M(int a) { f((ref int b) => b = 1); } }";
        var src2 = @"class C { public void M(int a) { f((out int b) => b = 1); } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M(int a) { f((ref int b) => b = 1); }]@10 -> [public void M(int a) { f((out int b) => b = 1); }]@10");
    }

    [Fact]
    public void Method_ReadOnlyRef_Parameter_InsertWhole()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { int M(in int b) => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [int M(in int b) => throw null;]@13",
            "Insert [(in int b)]@18",
            "Insert [in int b]@19");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Method_ReadOnlyRef_Parameter_InsertParameter()
    {
        var src1 = "class C { int M() => throw null; }";
        var src2 = "class C { int M(in int b) => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [in int b]@16");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "in int b", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_ReadOnlyRef_Parameter_Update()
    {
        var src1 = "class C { int M(int b) => throw null; }";
        var src2 = "class C { int M(in int b) => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int b]@16 -> [in int b]@16");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Method_ReadOnlyRef_ReturnType_Insert()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { ref readonly int M() => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [ref readonly int M() => throw null;]@13",
            "Insert [()]@31");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Method_ReadOnlyRef_ReturnType_Update()
    {
        var src1 = "class Test { int M() => throw null; }";
        var src2 = "class Test { ref readonly int M() => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int M() => throw null;]@13 -> [ref readonly int M() => throw null;]@13");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.M"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "ref readonly int M()", FeaturesResources.method)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Method_ImplementingInterface_Add()
    {
        var src1 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}

public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;
}
";
        var src2 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}

public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;

    string IConflict.Get() => String.Empty;
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [string IConflict.Get() => String.Empty;]@325",
            "Insert [()]@345");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, "string IConflict.Get()", FeaturesResources.method));
    }

    [Fact]
    public void Method_Partial_DeleteInsert_DefinitionPart()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";
        var srcC1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { partial void F() { } }";
        var srcC2 = "partial class C { partial void F(); }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
            ]);
    }

    [Fact]
    public void Method_Partial_DeleteInsert_ImplementationPart()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";
        var srcC1 = "partial class C { }";

        var srcA2 = "partial class C { partial void F(); }";
        var srcB2 = "partial class C { }";
        var srcC2 = "partial class C { partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
    public void Method_Partial_Swap_ImplementationAndDefinitionParts()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";

        var srcA2 = "partial class C { partial void F() { } }";
        var srcB2 = "partial class C { partial void F(); }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
            ]);
    }

    [Fact]
    public void Method_Partial_DeleteImplementation()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";

        var srcA2 = "partial class C { partial void F(); }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C")
                    ]),
            ]);
    }

    [Fact]
    public void Method_Partial_DeleteBoth()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C")]),
            ]);
    }

    [Fact]
    public void Method_Partial_DeleteInsertBoth()
    {
        var srcA1 = "partial class C { partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";
        var srcC1 = "partial class C { }";
        var srcD1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";
        var srcC2 = "partial class C { partial void F(); }";
        var srcD2 = "partial class C { partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")])
            ]);
    }

    [Fact]
    public void Method_Partial_Insert()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { partial void F(); }";
        var srcB2 = "partial class C { partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart)]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Method_Partial_Insert_Reloadable()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { partial void F(); }";
        var srcB2 = "partial class C { partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Method_Partial_Update_Attribute_Definition()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { [A(1)]partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";

        var srcA2 = attribute +
                    "partial class C { [A(2)]partial void F(); }";
        var srcB2 = "partial class C { partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Method_Partial_Update_Attribute_Implementation()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { partial void F(); }";
        var srcB1 = "partial class C { [A(1)]partial void F() { } }";

        var srcA2 = attribute +
                    "partial class C { partial void F(); }";
        var srcB2 = "partial class C { [A(2)]partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Method_Partial_Update_Attribute_DefinitionAndImplementation()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { [A(1)]partial void F(); }";
        var srcB1 = "partial class C { [A(1)]partial void F() { } }";

        var srcA2 = attribute +
                    "partial class C { [A(2)]partial void F(); }";
        var srcB2 = "partial class C { [A(2)]partial void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Method_Partial_DeleteInsert_DefinitionWithAttributeChange()
    {
        var attribute = """
            public class A : System.Attribute {}
            """;

        var srcA1 = attribute +
                    "partial class C { [A]partial void F(); }";
        var srcB1 = "partial class C { partial void F() { } }";

        var srcA2 = attribute +
                    "partial class C { }";
        var srcB2 = "partial class C { partial void F() { } partial void F(); }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Method_Partial_Parameter_TypeChange()
    {
        var srcA1 = "partial class C { partial void F(long x); }";
        var srcB1 = "partial class C { partial void F(long x) { } }";

        var srcA2 = "partial class C { partial void F(byte x); }";
        var srcB2 = "partial class C { partial void F(byte x) { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")
                    ]),
                DocumentResults(
                   semanticEdits:
                   [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                       SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.F").PartialImplementationPart, partialType: "C")
                   ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "byte x", GetResource("method"))]),
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "byte x", GetResource("method"))]),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    #endregion

    #region Operators

    [Theory]
    [InlineData("implicit", "explicit")]
    [InlineData("explicit", "implicit")]
    public void Operator_Modifiers_Update(string oldModifiers, string newModifiers)
    {
        var src1 = "class C { public static " + oldModifiers + " operator int (C c) => 0; }";
        var src2 = "class C { public static " + newModifiers + " operator int (C c) => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [public static " + oldModifiers + " operator int (C c) => 0;]@10 -> [public static " + newModifiers + " operator int (C c) => 0;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public static " + newModifiers + " operator int (C c)", CSharpFeaturesResources.conversion_operator));
    }

    [Fact]
    public void Operator_Modifiers_Update_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { public static implicit operator int (C c) => 0; }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { public static explicit operator int (C c) => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Operator_Conversion_ExternModifiers_Add()
    {
        var src1 = "class C { public static implicit operator bool (C c) => default; }";
        var src2 = "class C { extern public static implicit operator bool (C c); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "extern public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator));
    }

    [Fact]
    public void Operator_Conversion_ExternModifiers_Remove()
    {
        var src1 = "class C { extern public static implicit operator bool (C c); }";
        var src2 = "class C { public static implicit operator bool (C c) => default; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator));
    }

    [Fact]
    public void OperatorInsert()
    {
        var src1 = @"
class C
{
}
";
        var src2 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertOperator, "public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator),
            Diagnostic(RudeEditKind.InsertOperator, "public static C operator +(C c, C d)", FeaturesResources.operator_));
    }

    [Fact]
    public void OperatorDelete()
    {
        var src1 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}
";
        var src2 = @"
class C
{
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Implicit"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Addition"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void OperatorInsertDelete()
    {
        var srcA1 = @"
partial class C
{
    public static implicit operator bool (C c)  => false;
}
";
        var srcB1 = @"
partial class C
{
    public static C operator +(C c, C d) => c;
}
";

        var srcA2 = srcB1;
        var srcB2 = srcA1;

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("op_Addition"))
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("op_Implicit"))
                    ]),
            ]);
    }

    [Fact]
    public void OperatorUpdate()
    {
        var src1 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}
";
        var src2 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return true;
    }

    public static C operator +(C c, C d) 
    {
        return d;
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
        ]);
    }

    [Fact]
    public void OperatorWithExpressionBody_Update()
    {
        var src1 = @"
class C
{
    public static implicit operator bool (C c) => false;
    public static C operator +(C c, C d) => c;
}
";
        var src2 = @"
class C
{
    public static implicit operator bool (C c) => true;
    public static C operator +(C c, C d) => d;
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
        ]);
    }

    [Fact]
    public void OperatorWithExpressionBody_ToBlockBody()
    {
        var src1 = "class C { public static C operator +(C c, C d) => d; }";
        var src2 = "class C { public static C operator +(C c, C d) { return c; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [public static C operator +(C c, C d) => d;]@10 -> [public static C operator +(C c, C d) { return c; }]@10");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
        ]);
    }

    [Fact]
    public void OperatorWithBlockBody_ToExpressionBody()
    {
        var src1 = "class C { public static C operator +(C c, C d) { return c; } }";
        var src2 = "class C { public static C operator +(C c, C d) => d;  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [public static C operator +(C c, C d) { return c; }]@10 -> [public static C operator +(C c, C d) => d;]@10");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
        ]);
    }

    [Fact]
    public void Operator_Rename()
    {
        var src1 = @"
class C
{
    public static C operator +(C c, C d) { return c; }
}
";
        var src2 = @"
class C
{
    public static C operator -(C c, C d) { return d; }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Addition"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.op_Subtraction"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void OperatorReorder1()
    {
        var src1 = @"
class C
{
    public static implicit operator bool (C c) { return false; }
    public static implicit operator int (C c) { return 1; }
}
";
        var src2 = @"
class C
{
    public static implicit operator int (C c) { return 1; }
    public static implicit operator bool (C c) { return false; }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [public static implicit operator int (C c) { return 1; }]@84 -> @18");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void OperatorReorder2()
    {
        var src1 = @"
class C
{
    public static C operator +(C c, C d) { return c; }
    public static C operator -(C c, C d) { return d; }
}
";
        var src2 = @"
class C
{
    public static C operator -(C c, C d) { return d; }
    public static C operator +(C c, C d) { return c; }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [public static C operator -(C c, C d) { return d; }]@74 -> @18");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Operator_ReadOnlyRef_Parameter_InsertWhole()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { public static bool operator !(in Test b) => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [public static bool operator !(in Test b) => throw null;]@13",
            "Insert [(in Test b)]@42",
            "Insert [in Test b]@43");

        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.InsertOperator, "public static bool operator !(in Test b)", FeaturesResources.operator_));
    }

    [Fact]
    public void Operator_Delete()
    {
        var src1 = "class C { public static bool operator !(C b) => true; }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_LogicalNot"), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    #endregion

    #region Constructor

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Constructor_Update_Modifiers_Accessibility_Significant(string accessibility)
    {
        var src1 = $$"""
            class C
            {
                {{accessibility}}
                C()
                {
                }
            }
            """;
        var src2 = """
            class C
            {
                
                C()
                {
                }
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
        ]);
    }

    [Fact]
    public void Constructor_Update_Modifiers_Accessibility_Insignificant()
    {
        var src1 = "class C { private C() {} }";
        var src2 = "class C { C() {} }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
        [
            // the update is not necessary and can be eliminated:
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
        ]);
    }

    [Fact]
    public void Constructor_Parameter_AddAttribute()
    {
        var src1 = "class C { public C(int a) { } }";
        var src2 = "class C { public C([System.Obsolete]int a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_AddAttribute_Primary()
    {
        var src1 = "class C(int a);";
        var src2 = "class C([System.Obsolete] int a);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_Update_Attribute_Record_ParamTarget()
    {
        var src1 = "record C([param: A(1)] int P);" + s_attributeSource;
        var src2 = "record C([param: A(2)] int P);" + s_attributeSource;

        var edits = GetTopEdits(src1, src2);

        // Attribute is only applied to the parameter.
        // Currently we don't filter the property update out.
        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_Update_Attribute_Record_PropertyTarget()
    {
        var src1 = "record C([property: A(1)] int P);" + s_attributeSource;
        var src2 = "record C([property: A(2)] int P);" + s_attributeSource;

        var edits = GetTopEdits(src1, src2);

        // Attribute is only applied to the parameter.
        // Currently we don't filter the constructor update out.
        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void Constructor_Parameter_DefaultValue_Primary(string keyword)
    {
        var src1 = keyword + " C(int X = 1) : D {  }";
        var src2 = keyword + " C(int X = 2) : D {  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InitializerUpdate, "int X = 2", GetResource("parameter")));
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/68458")]
    [InlineData("field")]
    [InlineData("property")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/68458")]
    public void Constructor_Parameter_AddAttribute_Record_NonParamTargets(string target)
    {
        var src1 = "record C(int P);" + s_attributeSource;
        var src2 = "record C([" + target + ": A]int P);" + s_attributeSource;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68458")]
    public void Constructor_Parameter_AddAttribute_Record_ReplacingSynthesizedWithCustomProperty()
    {
        var src1 = "record C(int P) { }" + s_attributeSource;
        var src2 = "record C([property: A][field: A][param: A]int P) { public int P { get; init; } }" + s_attributeSource;

        var edits = GetTopEdits(src1, src2);

        // We update more members than strictly necessary to avoid more complex analysis.
        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
                // TOOD: Should include update of P: https://github.com/dotnet/roslyn/issues/68458
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_AddAttribute_Record_ReplacingCustomPropertyWithSynthesized()
    {
        var src1 = "record C(int P) { public int P { get; init; } }" + s_attributeSource;
        var src2 = "record C([property: A][field: A][param: A]int P) {} " + s_attributeSource;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory, CombinatorialData]
    public void Constructor_Parameter_Update_TypeOrRefKind_RuntimeTypeChanged(
        [CombinatorialValues("int", "in byte", "ref byte", "out byte", "ref readonly byte")] string type,
        bool direction)
    {
        var (oldType, newType) = direction ? (type, "byte") : ("byte", type);

        var src1 = "class C { C(" + oldType + " a) => throw null!; }";
        var src2 = "class C { C(" + newType + " a) => throw null!; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single())
           ],
           capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, newType + " a", GetResource("constructor"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Parameter_Update_Type_Primary()
    {
        var src1 = @"class C(bool x);";
        var src2 = @"class C(int x);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int x", GetResource("constructor"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Parameter_Update_Type_Primary_PartialMove()
    {
        var srcA1 = "partial class C(bool a);";
        var srcB1 = "partial class C;";

        var srcA2 = "partial class C;";
        var srcB2 = "partial class C(int a);";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"), partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Update_Type_Record()
    {
        var src1 = @"record C(bool x);";
        var src2 = @"record C(int x);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.x"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_x"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_x"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.x")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_x")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_x")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int x", GetResource("auto-property")),
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int x", GetResource("constructor"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Parameter_Update_Type_Record_TypeLayout()
    {
        var src1 = @"record struct C(bool x);";
        var src2 = @"record struct C(int x);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoStruct, "int x", GetResource("auto-property"), GetResource("record struct"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Update_Type_ReplacingClassWithRecord()
    {
        var src1 = @"class C(bool x);";
        var src2 = @"record C(int x);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.TypeKindUpdate, "record C")
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete()
    {
        var src1 = "class C { C(int x, int y) { } }";
        var src2 = "class C { C(int x) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single()),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary()
    {
        var src1 = "class C(int x, int y) { }";
        var src2 = "class C(int x) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record()
    {
        var src1 = "record C(int X, int Y);";
        var src2 = "record C(int X);";
        var edits = GetTopEdits(src1, src2);

        // Note: We do not report rude edits when deleting auto-properties of a type with a sequential or explicit layout.
        // The properties are updated to throw and the backing field remains in the type.
        // The deleted field will remain unused since adding the property back is a rude edit.
        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_LayoutClass()
    {
        var src1 = @"
using System.Runtime.InteropServices;
[StructLayoutAttribute(LayoutKind.Sequential)]
record C(int X, int Y);
";
        var src2 = @"
using System.Runtime.InteropServices;
[StructLayoutAttribute(LayoutKind.Sequential)]
record C(int X);
";
        var edits = GetTopEdits(src1, src2);

        // Note: We do not report rude edits when deleting auto-properties of a type with a sequential or explicit layout.
        // The properties are updated to throw and the backing field remains in the type.
        // The deleted field will remain unused since adding the property back is a rude edit.
        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_Struct()
    {
        var src1 = "record struct C(int X, int Y);";
        var src2 = "record struct C(int X);";
        var edits = GetTopEdits(src1, src2);

        // Note: We do not report rude edits when deleting auto-properties of a type with a sequential or explicit layout.
        // The properties are updated to throw and the backing field remains in the type.
        // The deleted field will remain unused since adding the property back is a rude edit.
        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_ReplacingSynthesizedWithCustomProperty()
    {
        var src1 = "record C(int X, int Y) { }";
        var src2 = "record C(int X) { public int Y { get; init; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Y")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Y")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomProperty_WithUpdate()
    {
        var src1 = "record C(int X, int Y) {           public int Y { get; init; } }";
        var src2 = "record C(int X       ) { [Obsolete]public int Y { get; init; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomProperty_WithAccessorUpdate()
    {
        var src1 = "record C(int X, int Y) { public int Y { get => new System.Func<int>(<N:0.0>() => 1</N:0.0>).Invoke(); init { } } }";
        var src2 = "record C(int X       ) { public int Y { get => new System.Func<int>(<N:0.0>() => 2</N:0.0>).Invoke(); init { } } }";

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Y"), syntaxMap: syntaxMap[0]),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomField_WithUpdate()
    {
        var src1 = "record C(int X, int Y) {           public int Y; }";
        var src2 = "record C(int X       ) { [Obsolete]public int Y; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomPropertyDelete()
    {
        var src1 = "record C(int X, int Y) { public int Y { get => 0; init {} } }";
        var src2 = "record C(int X       ) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomDeconstructor_NoUpdate()
    {
        var src1 = "record C(int X, int Y) { public void Deconstruct(out int X) => X = 1; }";
        var src2 = "record C(int X       ) { public void Deconstruct(out int X) => X = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomDeconstructor_Update()
    {
        var src1 = "record C(int X, int Y) { public void Deconstruct(out int X) => X = 1; }";
        var src2 = "record C(int X       ) { public void Deconstruct(out int X) => X = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.Deconstruct").Single(m => m.Parameters is [_])),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomDeconstructor_Insert()
    {
        var src1 = "record C(int X, int Y) { }";
        var src2 = "record C(int X       ) { public void Deconstruct(out int X) => X = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_Record_WithCustomDeconstructor_Delete()
    {
        var src1 = "record C(int X, int Y) { public void Deconstruct(out int X, out int Y) => X = Y = 1; }";
        var src2 = "record C(int X       ) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_LayoutClass_NotCaptured()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y) 
{ 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x)
{ 
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Delete_Primary_LayoutClass_Captured()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y) 
{
    public int M() => x + y;
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x)
{
    public int M() => x;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.NotCapturingPrimaryConstructorParameter, "M", GetResource("class with explicit or sequential layout"), "y"));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743552")]
    public void Constructor_Parameter_Insert()
    {
        var src1 = "class C { public C(int a) { } }";
        var src2 = "class C { public C(int a, int b) { } }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [(int a)]@18 -> [(int a, int b)]@18",
            "Insert [int b]@26");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single())
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int b", GetResource("constructor"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("struct")]
    public void Constructor_Parameter_Insert_Primary_Uncaptured(string keyword)
    {
        var src1 = keyword + " C(int a) { }";
        var src2 = keyword + " C(int a, int b) { }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [(int a)]@8 -> [(int a, int b)]@8",
            "Insert [int b]@16");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "(int a, int b)", GetResource("constructor"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69159")]
    public void Constructor_Parameter_Insert_Primary_Captured_Class()
    {
        var src1 = "class C(int a) { int X => a; }";
        var src2 = "class C(int a, int b) { int X => a + b; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_X")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        // TODO: should report rude edit https://github.com/dotnet/roslyn/issues/69159
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Captured_Struct()
    {
        var src1 = "struct C(int a) { int X => a; }";
        var src2 = "struct C(int a, int b) { int X => a + b; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.CapturingPrimaryConstructorParameter, "b", GetResource("struct"), "b"),
                Diagnostic(RudeEditKind.InsertIntoStruct, "int b", GetResource("parameter"), GetResource("struct"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record()
    {
        var src1 = "record C(int X) { }";
        var src2 = "record C(int X, int Y, int Z, int U) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Z")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.U")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_ClassWithLayout()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
record C(int X) { }
";

        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
record C(int X, int Y) { }
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "int Y", GetResource("auto-property"), GetResource("record")));
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_Struct()
    {
        var src1 = @"
record struct C(int x) 
{
}
";
        var src2 = @"
record struct C(int x, int y)
{
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoStruct, "int y", GetResource("auto-property"), GetResource("record struct")));
    }

    [Fact]
    public void Constructor_Parameter_Insert_In()
    {
        var src1 = "class C { C() => throw null; }";
        var src2 = "class C { C(in int b) => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [in int b]@12");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single())
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "in int b", GetResource("constructor"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_NoUpdate1()
    {
        var src1 = "record C(int X       ) { public void Deconstruct(out int X) => X = 1; }";
        var src2 = "record C(int X, int Y) { public void Deconstruct(out int X) => X = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_NoUpdate2()
    {
        var src1 = "record C(int X       ) { public void Deconstruct(out int X, out int Y) => X = Y = 1; }";
        var src2 = "record C(int X, int Y) { public void Deconstruct(out int X, out int Y) => X = Y = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_Update()
    {
        var src1 = "record C(int X       ) { public void Deconstruct(out int X, out int Y) => X = Y = 1; }";
        var src2 = "record C(int X, int Y) { public void Deconstruct(out int X, out int Y) => X = Y = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IMethodSymbol>("C.Deconstruct").Single(m => m.Parameters is [_, _])),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_Insert()
    {
        var src1 = "record C(int X       ) { }";
        var src2 = "record C(int X, int Y) { public void Deconstruct(out int X, out int Y) => X = Y = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_Delete()
    {
        var src1 = "record C(int X       ) { public void Deconstruct(out int X) => X = 1; }";
        var src2 = "record C(int X, int Y) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_Record_WithCustomDeconstructor_Delete_Partial()
    {
        var srcA1 = "partial record C(int X       ) { public partial void Deconstruct(out int X) => X = 1; }";
        var srcB1 = "partial record C               { public partial void Deconstruct(out int X); }";

        var srcA2 = "partial record C(int X, int Y);";
        var srcB2 = "partial record C;";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
                    ]),
                DocumentResults(
                   semanticEdits:
                   [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                   ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_IntoLayoutClass_NotLifted()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x) 
{ 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y)
{ 
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_IntoLayoutClass_Lifted()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x) 
{
    public int M() => x;
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y)
{
    public int M() => x + y;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.CapturingPrimaryConstructorParameter, "y", GetResource("class with explicit or sequential layout"), "y"),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "int y", GetResource("parameter"), GetResource("class"))
        ]);
    }

    [Fact]
    public void Constructor_Parameter_Insert_Primary_IntoLayoutClass_LiftedInLambda()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x) 
{
    public Func<int> M() => () => x;
}
";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y)
{
    public Func<int> M() => () => x + y;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.CapturingPrimaryConstructorParameter, "y", GetResource("class with explicit or sequential layout"), "y"),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "int y", GetResource("parameter"), GetResource("class"))
        ]);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("class")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/68708")]
    public void Constructor_Parameter_Reorder_Primary_NotLifted(string keyword)
    {
        var src1 = keyword + " C(int x, byte y) { }";
        var src2 = keyword + " C(byte y, int x) { }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68708")]
    public void Constructor_Parameter_Reorder_Primary_NotLifted_Record_Struct()
    {
        var src1 = "record struct C(int x, byte y) { }";
        var src2 = "record struct C(byte y, int x) { }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoStruct, "byte y", GetResource("auto-property"), GetResource("record struct"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68708")]
    public void Constructor_Parameter_Reorder_Primary_Lifted_Struct()
    {
        var src1 = "struct C(int x, byte y) { int M() => x + y; }";
        var src2 = "struct C(byte y, int x) { int M() => x + y; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoStruct, "byte y", GetResource("parameter"), GetResource("struct"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68708")]
    public void Constructor_Parameter_Reorder_Primary_Lifted_Class()
    {
        var src1 = "class C(int x, byte y) { int M() => x + y; }";
        var src2 = "class C(byte y, int x) { int M() => x + y; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C"))
        ],
        capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68708")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69894")]
    public void Constructor_Parameter_Reorder_Primary_Lifted_Record()
    {
        var src1 = "record C(int x, byte y) { int M() => x + y; }";
        var src2 = "record C(byte y, int x) { int M() => x + y; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
            SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Deconstruct"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Deconstruct")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.x")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_x")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_x")),
            // TODO: y should also be updated (to update sequence points to the new location)
            // https://github.com/dotnet/roslyn/issues/69894
            // SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.y")),
            // SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_y")),
            // SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_y")),

        ],
        capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_Capture_Primary_Class()
    {
        var src1 = @"
class C(int x) 
{
    public int M() => 1;
}
";
        var src2 = @"
class C(int x)
{
    public int M() => x;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")));
    }

    [Fact]
    public void Constructor_Parameter_Capture_Primary_Struct()
    {
        var src1 = @"
struct C(int x, int y) 
{
    public int M1() => 1;
    public int M2() => y;
}
";
        var src2 = @"
struct C(int x, int y)
{
    public int M1() => y;
    public int M2() => x;
}
";
        var edits = GetTopEdits(src1, src2);

        // note: 'y' is not reported since it is still captured
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.CapturingPrimaryConstructorParameter, "x", GetResource("struct"), "x"));
    }

    [Fact]
    public void Constructor_Parameter_Capture_Primary_ClassWithLayout()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y) 
{
    public int M1() => 1;
    public int M2() => y;
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y)
{
    public int M1() => y;
    public int M2() => x;
}
";
        var edits = GetTopEdits(src1, src2);

        // note: 'y' is not reported since it is still captured
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.CapturingPrimaryConstructorParameter, "x", GetResource("class with explicit or sequential layout"), "x"));
    }

    [Fact]
    public void Constructor_Parameter_CeaseCapturing_Primary_Struct()
    {
        var src1 = @"
struct C(int x, int y) 
{
    public int M1() => 1;
    public int M2() => x;
    public int M3() => y;
}
";
        var src2 = @"
struct C(int x, int y)
{
    public int M1() => y;
    public int M2() => 1;
    public int M3() => 2;
}
";
        var edits = GetTopEdits(src1, src2);

        // note: 'y' is not reported since it is still captured
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.NotCapturingPrimaryConstructorParameter, "M2", GetResource("struct"), "x"));
    }

    [Fact]
    public void Constructor_Parameter_CeaseCapturing_Primary_ClassWithLayout()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y) 
{
    public int M1() => 1;
    public int M2() => x;
    public int M3() => y;
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C(int x, int y)
{
    public int M1() => y;
    public int M2() => 1;
    public int M3() => 2;
}
";
        var edits = GetTopEdits(src1, src2);

        // note: 'y' is not reported since it is still captured
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.NotCapturingPrimaryConstructorParameter, "M2", GetResource("class with explicit or sequential layout"), "x"));
    }

    [Theory]
    [InlineData("partial class")]
    [InlineData("partial struct")]
    [InlineData("readonly partial struct")]
    public void Constructor_Parameter_DeleteInsert_Primary(string keywords)
    {
        var srcA1 = keywords + " C(int P);";
        var srcB1 = keywords + " C;";

        var srcA2 = keywords + " C;";
        var srcB2 = keywords + " C(int P);";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Theory]
    [InlineData("partial record")]
    [InlineData("partial record struct")]
    [InlineData("readonly partial record struct")]
    public void Constructor_Parameter_DeleteInsert_Primary_Record(string keywords)
    {
        var srcA1 = keywords + " C(int P);";
        var srcB1 = keywords + " C;";

        var srcA2 = keywords + " C;";
        var srcB2 = keywords + " C(int P);";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void Constructor_Parameter_DeleteInsert_ReplacingPrimaryWithNonPrimary(string keyword)
    {
        var srcA1 = "partial " + keyword + " C(int a);";
        var srcB1 = "partial " + keyword + " C;";

        var srcA2 = "partial " + keyword + " C;";
        var srcB2 = "partial " + keyword + " C { public C(int a) { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "a"}]), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_ReplacingPrimaryWithNonPrimary_Record()
    {
        var srcA1 = "partial record C(int P);";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C { public int P { get; init; } public C(int P) { } public void Deconstruct(out int P) { P = this.P; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Deconstruct")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "P"}]), partialType: "C", preserveLocalVariables: true),
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_ReplacingNonPrimaryWithPrimary_Record()
    {
        var srcA1 = "partial record C { public int P { get; init; } public C(int P) { } public void Deconstruct(out int P) { P = this.P; } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C(int P);";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "P" }]), partialType: "C", preserveLocalVariables: true),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Deconstruct")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_ReplacingNonPrimaryWithPrimary_WithExplicitPropertyAdded_Record()
    {
        var srcA1 = "partial record C        { public int P { get; init; } public C(int P) { } public void Deconstruct(out int P) { P = this.P; } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C(int P) { public int P { get; init; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "P" }]), partialType: "C", preserveLocalVariables: true),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Deconstruct")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_ReplacingNonPrimaryWithPrimary_WithExplicitFieldAdded_Record()
    {
        var srcA1 = "partial record C { public int P { get; init; } public C(int P) { } public void Deconstruct(out int P) { P = this.P; } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C(int P) { public int P; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters is [{ Name: "P" }]), partialType: "C", preserveLocalVariables: true),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Deconstruct")),
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_SwappingNonPrimaryWithPrimary_Record()
    {
        var srcA1 = "partial record C(int P) { public C() : this(1) { } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C() { public C(int P) : this() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    ]
                ),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters is [{ Name: "P" }])),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters is []), partialType: "C", preserveLocalVariables: true),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Parameter_DeleteInsert_ReplacingPropertyWithField_Record()
    {
        var srcA1 = "partial record C(int P) { public int P { get; init; } }";
        var srcB1 = "partial record C;";

        var srcA2 = "partial record C;";
        var srcB2 = "partial record C(int P) { public int P; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), partialType: "C", preserveLocalVariables: true),
                    ]),
            ], capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/68458")]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/68458")]
    public void Constructor_Instance_Update_Primary_Attributes(
        [CombinatorialValues("class", "struct", "record", "record struct")] string keyword)
    {
        var src1 = keyword + " C() { }";
        var src2 = "[method: System.Obsolete] " + keyword + " C() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Update_Initializer_Update()
    {
        var src1 = @"
class C
{
    public C(int a) : base(a) { }
}";
        var src2 = @"
class C
{
    public C(int a) : base(a + 1) { }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public C(int a) : base(a) { }]@18 -> [public C(int a) : base(a + 1) { }]@18");

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Update_Initializer_Update_Generic()
    {
        var src1 = @"
class C<T>
{
    public C(int a) : base(a) { }
}";
        var src2 = @"
class C<T>
{
    public C(int a) : base(a + 1) { }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public C(int a) : base(a) { }]@21 -> [public C(int a) : base(a + 1) { }]@21");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "public C(int a)", GetResource("constructor"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public void Constructor_Instance_Update_Initializer_Update_Primary(string keyword)
    {
        var src1 = keyword + " C(int a) : D(a);";
        var src2 = keyword + " C(int a) : D(a + 1);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public void Constructor_Instance_Update_Initializer_Update_Primary_WithInterface(string keyword)
    {
        var src1 = keyword + " C(int a) : D(a), I;";
        var src2 = keyword + " C(int a) : D(a + 1), I;";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Update_Initializer_Delete()
    {
        var src1 = @"
class C<T>
{
    public C(int a) : base(a) { }
}";
        var src2 = @"
class C<T>
{
    public C(int a) { }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public C(int a) : base(a) { }]@21 -> [public C(int a) { }]@21");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "public C(int a)", GetResource("constructor"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("record")]
    public void Constructor_Instance_Update_Initializer_Delete_Primary(string keyword)
    {
        var src1 = keyword + " C(int a) : D(a);";
        var src2 = keyword + " C(int a) : D;";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: D(a)]@16 -> [: D]@16",
            "Delete [D(a)]@18");

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Update_Initializer_Insert()
    {
        var src1 = @"
class C
{
    public C(int a) { }
}";
        var src2 = @"
class C
{
    public C(int a) : base(a) { }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public C(int a) { }]@18 -> [public C(int a) : base(a) { }]@18");

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("record")]
    public void Constructor_Instance_Update_Initializer_Insert_Primary(string keyword)
    {
        var src1 = keyword + " C(int a) : D;";
        var src2 = keyword + " C(int a) : D(a);";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [: D]@16 -> [: D(a)]@16",
            "Insert [D(a)]@18");

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68731")]
    public void Constructor_Instance_Update_Initializer_StackAlloc_Update()
    {
        var src1 = "class C { C() : this(stackalloc int[1]) {} C(Span<int> span) {} }";
        var src2 = "class C { C() : this(stackalloc int[2]) {} C(Span<int> span) {} }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[2]", FeaturesResources.constructor));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68731")]
    public void Constructor_Instance_Update_Initializer_StackAlloc_Delete()
    {
        var src1 = "class C { C() : this(stackalloc int[1]) {} C(Span<int> span) {} }";
        var src2 = "class C { C() : this(default) {} C(Span<int> span) {} }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "C()", FeaturesResources.constructor));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68731")]
    public void Constructor_Instance_Update_Initializer_StackAlloc_Insert()
    {
        var src1 = "class C { C() : this(default) {} C(Span<int> span) {} }";
        var src2 = "class C { C() : this(stackalloc int[1]) {} C(Span<int> span) {} }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", FeaturesResources.constructor));
    }

    [Fact]
    public void Constructor_Instance_Update_AnonymousTypeInFieldInitializer()
    {
        var src1 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 1; } }";
        var src2 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("record")]
    public void Constructor_Instance_Update_AnonymousTypeInMemberInitializer_Field_Primary(string keyword)
    {
        var src1 = keyword + " C() : D(1) { int a = F(new { A = 1, B = 2 }); }";
        var src2 = keyword + " C() : D(2) { int a = F(new { A = 1, B = 2 }); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [D(1)]@13 -> [D(2)]@13");

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Constructor_Instance_Update_BlockBodyToExpressionBody()
    {
        var src1 = @"
public class C
{
    private int _value;

    public C(int value) { _value = value; }
}
";
        var src2 = @"
public class C
{
    private int _value;

    public C(int value) => _value = value;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [public C(int value) { _value = value; }]@52 -> [public C(int value) => _value = value;]@52");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Constructor_Instance_Update_BlockBodyToExpressionBody_WithInitializer()
    {
        var src1 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) { _value = value; }
}
";
        var src2 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) => _value = value;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [public C(int value) : base(value) { _value = value; }]@90 -> [public C(int value) : base(value) => _value = value;]@90");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Constructor_Instance_Update_ExpressionBodyToBlockBody()
    {
        var src1 = @"
public class C
{
    private int _value;

    public C(int value) => _value = value;
}
";
        var src2 = @"
public class C
{
    private int _value;

    public C(int value) { _value = value; }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [public C(int value) => _value = value;]@52 -> [public C(int value) { _value = value; }]@52");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Constructor_Instance_Update_ExpressionBodyToBlockBody_WithInitializer()
    {
        var src1 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) => _value = value;
}
";
        var src2 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) { _value = value; }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(@"Update [public C(int value) : base(value) => _value = value;]@90 -> [public C(int value) : base(value) { _value = value; }]@90");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void Constructor_Instance_Update_SemanticError_Partial()
    {
        var src1 = @"
partial class C
{
    partial void C(int x);
}

partial class C
{
    partial void C(int x)
    {
        System.Console.WriteLine(1);
    }
}
";
        var src2 = @"
partial class C
{
    partial void C(int x);
}

partial class C
{
    partial void C(int x)
    {
        System.Console.WriteLine(2);
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("C").PartialImplementationPart, partialType: "C"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2068")]
    public void Constructor_Instance_Update_Modifier_Extern_Add()
    {
        var src1 = "class C { }";
        var src2 = "class C { public extern C(); }";

        var edits = GetTopEdits(src1, src2);

        // This can be allowed as the compiler generates an empty constructor, but it's not worth the complexity.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, "public extern C()", GetResource("constructor")));
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    [InlineData("readonly struct")]
    [InlineData("readonly record struct")]
    public void Constructor_Instance_Insert_Struct(string keyword)
    {
        var src1 = keyword + " C { }";
        var src2 = keyword + " C { public C(int X) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X" }]))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_Struct_Primary()
    {
        var src1 = "struct C { }";
        var src2 = "struct C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X" }]))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_Struct_Primary_Record()
    {
        var src1 = "record struct C { }";
        var src2 = "record struct C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.InsertIntoStruct, "int X", GetResource("auto-property"), GetResource("record struct"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record class")]
    public void Constructor_Instance_Insert_ReplacingDefault_Class(string keyword)
    {
        var src1 = keyword + " C { }";
        var src2 = keyword + " C { public C(int X) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X"}])),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_Class_Primary()
    {
        var src1 = "class C { }";
        var src2 = "class C(int X) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_Class_Primary_Record()
    {
        var src1 = "record C { }";
        var src2 = "record C(int P) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_Class_Primary_Record_Generic()
    {
        var src1 = "record C<T> { }";
        var src2 = "record C<T>(int P) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryConstructor("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetPrimaryDeconstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.GenericAddFieldToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "(int P)", GetResource("constructor")),
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", GetResource("parameter"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("struct")]
    public void Constructor_Instance_Insert_ReplacingDefault_Class_WithMemberInitializers(string typeKind)
    {
        var src1 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;
}
";
        var src2 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;

    public C() { b = 3; }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [public C() { b = 3; }]@66", "Insert [()]@74");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_WithStackAllocInMemberInitializer()
    {
        var src1 = "class C { int a = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; public C() {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "public C()", GetResource("constructor")));
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_WithStackAllocInMemberInitializer_Static()
    {
        var src1 = "class C { static int a = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { static int a = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; public C() {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingDefault_WithStackAllocInMemberInitializer_Partial()
    {
        var srcA1 = "partial class C { int a = G(stackalloc int[10]); }";
        var srcB1 = "partial class C { static int G(System.Span<int> span) => 1; }";

        var srcA2 = "partial class C { int a = G(stackalloc int[10]); }";
        var srcB2 = "partial class C { static int G(System.Span<int> span) => 1; public C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.StackAllocUpdate, "public C()", GetResource("constructor"))
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Delete_ReplacingDefault_Partial()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { public C(int a) { } }";
        var srcB2 = "partial class C { public C(int a, int b) { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), partialType: "C", deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters.Length == 1), partialType: "C")
                    ]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), partialType: "C", deletedSymbolContainerProvider: c => c.GetMember("C")),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters.Length == 2), partialType: "C")
                    ])
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingExplicitWithDefault_WithStackAllocInMemberInitializer_Partial()
    {
        var srcA1 = "partial class C { int a = G(stackalloc int[10]); }";
        var srcB1 = "partial class C { static int G(System.Span<int> span) => 1; public C() { } }";

        var srcA2 = "partial class C { int a = G(stackalloc int[10]); }";
        var srcB2 = "partial class C { static int G(System.Span<int> span) => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.StackAllocUpdate, "partial class C", GetResource("constructor", "C()"))
                    ]),
            ]);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_UpdatingImplicit(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "protected")] string accessibility)
    {
        if (accessibility == "protected")
            keyword = "abstract " + keyword;

        var src1 = keyword + " C { }";
        var src2 = keyword + " C { [System.Obsolete] " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_UpdatingImplicit_Partial(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "protected")] string accessibility)
    {
        if (accessibility == "protected")
            keyword = "abstract " + keyword;

        var srcA1 = "partial " + keyword + " C { }";
        var srcB1 = "partial " + keyword + " C { }";

        var srcA2 = "partial " + keyword + " C { }";
        var srcB2 = "partial " + keyword + " C { " + accessibility + " C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // no change in document A
                DocumentResults(),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)]),
            ]);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_AddingParameterless(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "internal", "private", "protected", "private protected", "internal protected")] string accessibility)
    {
        var src1 = keyword + " C { C(int a) { } }";
        var src2 = keyword + " C { C(int a) { } " + accessibility + " C() : this(1) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetParameterlessConstructor("C"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_AddingParameterless_Primary(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "internal", "private", "protected", "private protected", "internal protected")] string accessibility)
    {
        var src1 = keyword + " C(int a) { }";
        var src2 = keyword + " C(int a) { " + accessibility + " C() : this(1) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetParameterlessConstructor("C"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_AddingParameterless_Partial(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "internal", "private", "protected", "private protected", "internal protected")] string accessibility)
    {
        var srcA1 = "partial " + keyword + " C { }";
        var srcB1 = "partial " + keyword + " C { public C(int a) { } }";

        var srcA2 = "partial " + keyword + " C { " + accessibility + " C() { } }";
        var srcB2 = "partial " + keyword + " C { public C(int a) { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetParameterlessConstructor("C"), partialType: "C")
                    ]),

                // no change in document B
                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_AddingParameterless_Partial_Primary(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "internal", "private", "protected", "private protected", "internal protected")] string accessibility)
    {
        var srcA1 = "partial " + keyword + " C { }";
        var srcB1 = "partial " + keyword + " C(int a) { }";

        var srcA2 = "partial " + keyword + " C { " + accessibility + " C() { } }";
        var srcB2 = "partial " + keyword + " C(int a) { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetParameterlessConstructor("C"))
                    ]),

                // no change in document B
                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_ReplacingSynthesizedWithCustom_ChangingAccessibilty(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("", "private", "protected", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = keyword + " C { }";
        var src2 = keyword + " C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Insert_ReplacingSynthesizedWithCustom_ChangingAccessibilty_AbstractType(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("", "private", "public", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "abstract " + keyword + " C { }";
        var src2 = "abstract " + keyword + " C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingSynthesizedWithCustom_Primary()
    {
        var src1 = "class C { }";
        var src2 = "class C() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Insert_ReplacingSynthesizedWithCustom_Primary_Record()
    {
        var src1 = "record C { }";
        var src2 = "record C() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")));
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("record struct")]
    [InlineData("readonly struct")]
    [InlineData("readonly record struct")]
    public void Constructor_Instance_Delete_Struct(string keyword)
    {
        var src1 = keyword + " C { public C(int X) {} }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X" }]), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Delete_Struct_Primary()
    {
        var src1 = "struct C(int X) { }";
        var src2 = "struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X" }]), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Delete_Struct_Primary_Record()
    {
        var src1 = "record struct C(int P) { }";
        var src2 = "record struct C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record class")]
    public void Constructor_Instance_Delete_ReplacingWithDefault_Class(string keyword)
    {
        var src1 = keyword + " C { public C(int X) {} }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                // The compiler emits default constructor automatically
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "X"}]), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Delete_ReplacingWithDefault_Class_Primary()
    {
        var src1 = "class C(int X) { }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                // The compiler emits default constructor automatically
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Delete_ReplacingWithDefault_Class_Primary_Record()
    {
        var src1 = "record C(int P) { }";
        var src2 = "record C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                // The compiler emits default constructor automatically
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Delete_SemanticError()
    {
        var src1 = "class C { D() {} }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        // The compiler interprets D() as a constructor declaration.
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_UpdatingImplicit(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("public", "protected")] string accessibility)
    {
        if (accessibility == "protected")
            keyword = "abstract " + keyword;

        var src1 = keyword + " C { [System.Obsolete] " + accessibility + " C() { } }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Constructor_Instance_Delete_Parameterless()
    {
        var src1 = @"
class C
{
    private int a = 10;
    private int b;

    public C() { b = 3; }
}
";
        var src2 = @"
class C
{
    private int a = 10;
    private int b;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [public C() { b = 3; }]@65",
            "Delete [()]@73");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ]);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_WithParameters([CombinatorialValues("record", "class")] string keyword)
    {
        var src1 = keyword + " C { public C(int x) { } }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "x" }]), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_Primary_WithParameters([CombinatorialValues("record", "class")] string keyword)
    {
        var src1 = keyword + " C(int a) { public C(bool b) { } }";
        var src2 = keyword + " C(int a) { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [{ Name: "b" }]), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_Delete_Primary_Class()
    {
        var src1 = "class C(int a) { }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 1), deletedSymbolContainerProvider: c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_Delete_Primary_Record()
    {
        var src1 = "record C(int P) { }";
        var src2 = "record C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryDeconstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetPrimaryConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_Delete_Primary_Struct_CeasingCapture()
    {
        var src1 = "class C(int a) { int A => a; }";
        var src2 = "class C { int A => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Constructor_Instance_Delete_Public_PartialWithInitializerUpdate()
    {
        var srcA1 = "partial class C { public C() { } }";
        var srcB1 = "partial class C { int x = 1; }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int x = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)])
            ]);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_ReplacingCustomWithSynthesized(
        [CombinatorialValues("record", "class")] string keyword)
    {
        var src1 = keyword + " C { public C() { } }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_ReplacingCustomWithSynthesized_ChangingAccessibility(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("", "private", "protected", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = keyword + " C { " + accessibility + " C() { } }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_ReplacingCustomWithSynthesized_AbstractType(
        [CombinatorialValues("record", "class")] string keyword)
    {
        var src1 = "abstract " + keyword + " C { protected C() { } }";
        var src2 = "abstract " + keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_ReplacingCustomWithSynthesized_AbstractType_ChangingAccessibility(
        [CombinatorialValues("record", "class")] string keyword,
        [CombinatorialValues("", "private", "public", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "abstract " + keyword + " C { " + accessibility + " C() { } }";
        var src2 = "abstract " + keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_Delete_ReplacingCustomWithSynthesized_Partial()
    {
        var srcA1 = "partial class C { public C(int a) { } }";
        var srcB1 = "partial class C { public C(int a, int b) { } }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters.Length == 1), deletedSymbolContainerProvider: c => c.GetMember("C"))]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.Parameters.Length == 2), deletedSymbolContainerProvider: c => c.GetMember("C"))])
            ]);
    }

    [Fact]
    public void Constructor_Instance_Delete_Primary_ReplacingWithSynthesized()
    {
        var src1 = "class C() { }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Delete_Primary_ReplacingWithSynthesized_Record()
    {
        var src1 = "record C() { }";
        var src2 = "record C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")));
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_Primary_ReplacingWithRegular(
        [CombinatorialValues("", "private", "protected", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "class C() { }";
        var src2 = "class C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics([SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_Primary_ReplacingWithRegular_Record(
    [CombinatorialValues("", "private", "protected", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "record C() { }";
        var src2 = "record C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_Primary_ReplacingWithRegular_AbstractType(
        [CombinatorialValues("", "private", "public", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "abstract class C() { }";
        var src2 = "abstract class C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory, CombinatorialData]
    public void Constructor_Instance_Delete_Primary_ReplacingWithRegular_AbstractType_Record(
        [CombinatorialValues("", "private", "public", "internal", "private protected", "internal protected")] string accessibility)
    {
        var src1 = "abstract record C() { }";
        var src2 = "abstract record C { " + accessibility + " C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetCopyConstructor("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetSpecializedEqualsOverload("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Constructor_Instance_InsertDelete_Primary_Partial_Class()
    {
        var src1 = @"
partial class C { }
partial class C(int P);
";
        var src2 = @"
partial class C(int P) { }
partial class C;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_InsertDelete_Primary_Partial_Record()
    {
        var src1 = @"
partial record C { }
partial record C(int P);
";
        var src2 = @"
partial record C(int P) { }
partial record C;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void Constructor_Instance_Partial_DeletePrivateInsertPrivate()
    {
        var srcA1 = "partial class C { C() { } }";
        var srcB1 = "partial class C {  }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_DeletePublicInsertPublic()
    {
        var srcA1 = "partial class C { public C() { } }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { public C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_DeletePrivateInsertPublic()
    {
        var srcA1 = "partial class C { C() { } }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { public C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: NoSemanticEdits),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_InsertPublicDeletePublic()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { public C() { } }";

        var srcA2 = "partial class C { public C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_InsertPrivateDeletePrivate()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { private C() { } }";

        var srcA2 = "partial class C { private C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_DeleteInternalInsertInternal()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { internal C() { } }";

        var srcA2 = "partial class C { internal C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_InsertInternalDeleteInternal_WithBody()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { internal C() { } }";

        var srcA2 = "partial class C { internal C() { Console.WriteLine(1); } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_InsertPublicDeletePrivate()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { private C() { } }";

        var srcA2 = "partial class C { public C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_InsertInternalDeletePrivate()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { private C() { } }";

        var srcA2 = "partial class C { internal C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_Update_LambdaInInitializer1()
    {
        var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    <N:0.3>public C()
    {
        F(<N:0.2>c => c + 1</N:0.2>);
    }</N:0.3>
}
";
        var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    <N:0.3>public C()
    {
        F(<N:0.2>c => c + 2</N:0.2>);
    }</N:0.3>
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void Constructor_Instance_Partial_Update_LambdaInInitializer_Trivia1()
    {
        var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C() { F(<N:0.2>c => c + 1</N:0.2>); }
}
";
        var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    /*new trivia*/public C() { F(<N:0.2>c => c + 1</N:0.2>); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void Constructor_Instance_Partial_Update_LambdaInInitializer_ExplicitInterfaceImpl1()
    {
        var src1 = @"
using System;

public interface I { int B { get; } }
public interface J { int B { get; } }

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C : I, J
{
    int I.B { get; } = F(<N:0.1>ib => ib + 1</N:0.1>);
    int J.B { get; } = F(<N:0.2>jb => jb + 1</N:0.2>);

    public C()
    {
        F(<N:0.3>c => c + 1</N:0.3>);
    }
}
";
        var src2 = @"
using System;

public interface I { int B { get; } }
public interface J { int B { get; } }

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C : I, J
{
    int I.B { get; } = F(<N:0.1>ib => ib + 1</N:0.1>);
    int J.B { get; } = F(<N:0.2>jb => jb + 1</N:0.2>);

    public C()
    {
        F(<N:0.3>c => c + 2</N:0.3>);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void Constructor_Instance_Partial_Insert_Parameterless_LambdaInInitializer1()
    {
        var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);
}
";
        var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C()   // new ctor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);

        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])],
            capabilities: EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.NewTypeDefinition);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "c", GetResource("lambda"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
    public void Constructor_Instance_Partial_Insert_WithParameters_LambdaInInitializer1()
    {
        var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);
}
";
        var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int x)                                 // new ctor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        _ = GetSyntaxMap(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C(int x)"));

        // TODO: bug https://github.com/dotnet/roslyn/issues/2504
        //edits.VerifySemantics(
        //    ActiveStatementsDescription.Empty,
        //    new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
    }

    [Fact]
    public void Constructor_Instance_Partial_Explicit_Update()
    {
        var srcA1 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(bool arg) => Console.WriteLine(1);
}
";
        var srcB1 = @"
using System;

partial class C
{
    int a <N:0.0>= 1</N:0.0>;

    C(uint arg) => Console.WriteLine(2);
}
";

        var srcA2 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(bool arg) => Console.WriteLine(1);
}
";
        var srcB2 = @"
using System;

partial class C
{
    int a <N:0.0>= 2</N:0.0>;             // updated field initializer

    C(uint arg) => Console.WriteLine(2);
    C(byte arg) => Console.WriteLine(3);  // new ctor
}
";
        var syntaxMapB = GetSyntaxMap(srcB1, srcB2)[0];

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // No changes in document A
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                       SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Int32"), partialType: "C", syntaxMap: syntaxMapB),
                       SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Boolean"), partialType: "C", syntaxMap: syntaxMapB),
                       SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "UInt32"), partialType: "C", syntaxMap: syntaxMapB),
                       SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Byte"), partialType: "C", syntaxMap: null),
                    ])
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Instance_Partial_Explicit_Update_SemanticError()
    {
        var srcA1 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(int arg) => Console.WriteLine(1);
}
";
        var srcB1 = @"
using System;

partial class C
{
    int a = 1;
}
";

        var srcA2 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(int arg) => Console.WriteLine(1);
}
";
        var srcB2 = @"
using System;

partial class C
{
    int a = 2;

    C(int arg) => Console.WriteLine(2);
}
";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // No changes in document A
                DocumentResults(),

                // The actual edits do not matter since there are semantic errors in the compilation.
                // We just should not crash.
                DocumentResults(diagnostics: [])
            ]);
    }

    [Fact]
    public void Constructor_Instance_Partial_Implicit_Update()
    {
        var srcA1 = "partial class C { int F = 1; }";
        var srcB1 = "partial class C { int G = 1; }";

        var srcA2 = "partial class C { int F = 2; }";
        var srcB2 = "partial class C { int G = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void PartialDeclaration_Delete()
    {
        var srcA1 = "partial class C { public C() { } void F() { } }";
        var srcB1 = "partial class C { int x = 1; }";

        var srcA2 = "";
        var srcB2 = "partial class C { int x = 2; void F() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)]),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void PartialDeclaration_Insert()
    {
        var srcA1 = "";
        var srcB1 = "partial class C { int x = 1; void F() { } }";

        var srcA2 = "partial class C { public C() { } void F() { } }";
        var srcB2 = "partial class C { int x = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)]),
            ]);
    }

    [Fact]
    public void PartialDeclaration_Insert_Reloadable()
    {
        var srcA1 = "";
        var srcB1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 1; void F() { } }";

        var srcA2 = "partial class C { public C() { } void F() { } }";
        var srcB2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                    ]),

                DocumentResults(semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2068")]
    public void Constructor_Static_Update_Modifier_Extern_Add()
    {
        var src1 = "class C { }";
        var src2 = "class C { static extern C(); }";

        var edits = GetTopEdits(src1, src2);

        // This can be allowed as the compiler generates an empty constructor, but it's not worth the complexity.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertExtern, "static extern C()", GetResource("static constructor")));
    }

    [Fact]
    public void Constructor_Static_Delete()
    {
        var src1 = "class C { static C() { } }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.static_constructor, "C()")));
    }

    [Fact]
    public void Constructor_Static_Delete_Reloadable()
    {
        var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { static C() { } }";
        var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Constructor_Static_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { static C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single())],
            EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Constructor_Static_Partial_DeleteInsert()
    {
        var srcA1 = "partial class C { static C() { } }";
        var srcB1 = "partial class C {  }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { static C() { } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void Constructor_Static_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { static C() { } }";

        var srcA2 = "partial class C { static C() { } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                    ]),

                // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                DocumentResults(),
            ]);
    }

    #endregion

    #region Destructors

    [Fact]
    public void DestructorDelete()
    {
        var src1 = @"class B { ~B() { } }";
        var src2 = @"class B { }";

        var expectedEdit1 = @"Delete [~B() { }]@10";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(expectedEdit1);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class B", DeletedSymbolDisplay(CSharpFeaturesResources.destructor, "~B()")));
    }

    [Fact]
    public void DestructorDelete_InsertConstructor()
    {
        var src1 = @"class B { ~B() { } }";
        var src2 = @"class B { B() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [B() { }]@10",
            "Insert [()]@11",
            "Delete [~B() { }]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class B", DeletedSymbolDisplay(CSharpFeaturesResources.destructor, "~B()")));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Destructor_BlockBodyToExpressionBody()
    {
        var src1 = @"
public class C
{
    ~C() { Console.WriteLine(0); }
}
";
        var src2 = @"
public class C
{
    ~C() => Console.WriteLine(0);
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [~C() { Console.WriteLine(0); }]@25 -> [~C() => Console.WriteLine(0);]@25");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
            ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Destructor_ExpressionBodyToBlockBody()
    {
        var src1 = @"
public class C
{
    ~C() => Console.WriteLine(0);
}
";
        var src2 = @"
public class C
{
    ~C() { Console.WriteLine(0); }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [~C() => Console.WriteLine(0);]@25 -> [~C() { Console.WriteLine(0); }]@25");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
            ]);
    }

    #endregion

    #region Members with Initializers

    [Fact]
    public void MemberInitializer_Update_Field()
    {
        var src1 = "class C { int a = 0; }";
        var src2 = "class C { int a = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 0]@14 -> [a = 1]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_Event()
    {
        var src1 = "class C { event System.Action a = F(0); static System.Action F(int a) => null; }";
        var src2 = "class C { event System.Action a = F(1); static System.Action F(int a) => null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Property()
    {
        var src1 = "class C { int a { get; } = 0; }";
        var src2 = "class C { int a { get; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a { get; } = 0;]@10 -> [int a { get; } = 1;]@10");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Remove_Field()
    {
        var src1 = "class C { int a = 0; }";
        var src2 = "class C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 0]@14 -> [a]@14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_Remove_Partial_Field()
    {
        var srcA1 = "partial class C { int F = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C {  }";
        var srcB2 = "partial class C { int F ; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Remove_Partial_Property()
    {
        var srcA1 = "partial class C { int F { get; } = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C {  }";
        var srcB2 = "partial class C { int F { get; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_F"))
                    ]),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_DeleteInsert_Field()
    {
        var srcA1 = "partial class C { int F = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int F = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true),
                    ]),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_DeleteInsert_Property()
    {
        var srcA1 = "partial class C { int F { get; } = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { int F { get; } = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_F")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true),
                    ]),
            ]);
    }

    [Fact]
    public void MemberInitializer_PropertyUpdate2()
    {
        var src1 = "class C { int a { get; } = 0; }";
        var src2 = "class C { int a { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a { get; } = 0;]@10 -> [int a { get { return 1; } }]@10",
            "Update [get;]@18 -> [get { return 1; }]@18");

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true));
    }

    [Fact]
    public void MemberInitializer_PropertyInsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int a { get; } = 1; }";

        var srcA2 = "partial class C { int a { get { return 1; } } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), partialType: "C", preserveLocalVariables: true)
                    ]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void MemberInitializer_Field_Update3()
    {
        var src1 = "class C { int a; }";
        var src2 = "class C { int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@14 -> [a = 0]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_PropertyUpdate3()
    {
        var src1 = "class C { int a { get { return 1; } } }";
        var src2 = "class C { int a { get; } = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a { get { return 1; } }]@10 -> [int a { get; } = 0;]@10",
            "Update [get { return 1; }]@18 -> [get;]@18");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void FieldInitializer_Update_AccessPrimaryConstructorParameter(string keyword)
    {
        var src1 = keyword + " C(int x) { public int F = 1; }";
        var src2 = keyword + " C(int x) { public int F = 1 + x; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void MemberInitializer_Field_Delete()
    {
        var src1 = "class C { int a = 1; }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("public C() { }")]
    [InlineData("public C(int x) { }")]
    public void MemberInitializer_PropertyDelete(string ctor)
    {
        var src1 = "class C { " + ctor + " int a { get; set; } = 1; }";
        var src2 = "class C { " + ctor + " }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.a"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_a"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_a"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_StaticCtorUpdate1()
    {
        var src1 = "class C { static int a; static C() { } }";
        var src2 = "class C { static int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@21 -> [a = 0]@21",
            "Delete [static C() { }]@24",
            "Delete [()]@32");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_StaticCtorUpdate1()
    {
        var src1 = "class C { static int a { get; } = 1; static C() { } }";
        var src2 = "class C { static int a { get; } = 2;}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate_Private()
    {
        var src1 = "class C { int a; [System.Obsolete]C() { } }";
        var src2 = "class C { int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, $"class C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MemberInitializer_Update_Property_InstanceCtorUpdate_Private()
    {
        var src1 = "class C { int a { get; } = 1; C() { } }";
        var src2 = "class C { int a { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate_Public()
    {
        var src1 = "class C { int a; public C() { } }";
        var src2 = "class C { int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_InstanceCtorUpdate_Public()
    {
        var src1 = "class C { int a { get; } = 1; public C() { } }";
        var src2 = "class C { int a { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_StaticCtorUpdate2()
    {
        var src1 = "class C { static int a; static C() { } }";
        var src2 = "class C { static int a = 0; static C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@21 -> [a = 0]@21");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_StaticCtorUpdate2()
    {
        var src1 = "class C { static int a { get; } = 1; static C() { } }";
        var src2 = "class C { static int a { get; } = 2; static C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)]);
    }

    [Theory]
    [InlineData("class ")]
    [InlineData("struct")]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate2(string typeKind)
    {
        var src1 = typeKind + " C { int a; public C() { } }";
        var src2 = typeKind + " C { int a = 0; public C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@15 -> [a = 0]@15");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("readonly struct")]
    public void MemberInitializer_Update_Property_InstanceCtorUpdate2(string typeKind)
    {
        var src1 = typeKind + " C { int a { get; } = 1; public C() { } }";
        var src2 = typeKind + " C { int a { get; } = 2; public C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate3()
    {
        var src1 = "class C { int a; }";
        var src2 = "class C { int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@14 -> [a = 0]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_InstanceCtorUpdate3()
    {
        var src1 = "class C { int a { get; } = 1; }";
        var src2 = "class C { int a { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate4()
    {
        var src1 = "class C { int a = 0; }";
        var src2 = "class C { int a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 0]@14 -> [a]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate_Class()
    {
        var src1 = "class C { int a;     private C(int a) { } private C(bool a) : this() { } private C() : this(1) { } private C(string a) : base() { } }";
        var src2 = "class C { int a = 1; private C(int a) { } private C(bool a) : this() { } private C() : this(1) { } private C(string a) : base() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@14 -> [a = 1]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(string)"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate_Struct()
    {
        var src1 = "struct C { int a;     private C(int a) { } private C(bool a) : this() { } private C(char a) : this(1) { } }";
        var src2 = "struct C { int a = 1; private C(int a) { } private C(bool a) : this() { } private C(char a) : this(1) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@15 -> [a = 1]@15");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_InstanceCtorUpdate5()
    {
        var src1 = "class C { int a { get; } = 1;     private C(int a) { }    private C(bool a) { } }";
        var src2 = "class C { int a { get; } = 10000; private C(int a) { } private C(bool a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Property_Struct_InstanceCtorUpdate5()
    {
        var src1 = "struct C { int a { get; } = 1;     private C(int a) { } private C(bool a) { } }";
        var src2 = "struct C { int a { get; } = 10000; private C(int a) { } private C(bool a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_InstanceCtorUpdate6()
    {
        var src1 = "class C { int a;     private C(int a) : this(true) { } private C(bool a) { } }";
        var src2 = "class C { int a = 0; private C(int a) : this(true) { } private C(bool a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@14 -> [a = 0]@14");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_StaticCtorInsertImplicit()
    {
        var src1 = "class C { static int a; }";
        var src2 = "class C { static int a = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@21 -> [a = 0]@21");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single())]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_StaticCtorInsertExplicit()
    {
        var src1 = "class C { static int a; }";
        var src2 = "class C { static int a = 0; static C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [static C() { }]@28",
            "Insert [()]@36",
            "Update [a]@21 -> [a = 0]@21");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single())],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("readonly struct")]
    public void MemberInitializer_Update_Field_Constructor_Instance_InsertExplicit(string typeKind)
    {
        var src1 = typeKind + " C { int a; }";
        var src2 = typeKind + " C { int a = 0; public C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("readonly struct")]
    public void MemberInitializer_Update_Property_Constructor_Instance_InsertExplicit(string typeKind)
    {
        var src1 = typeKind + " C { int a { get; } = 1; }";
        var src2 = typeKind + " C { int a { get; } = 2; public C() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)]);
    }

    [Fact]
    public void MemberInitializer_Update_Field_GenericType()
    {
        var src1 = "class C<T> { int a = 1; }";
        var src2 = "class C<T> { int a = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@17 -> [a = 2]@17");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "a = 2", GetResource("field")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MemberInitializer_Update_Property_GenericType()
    {
        var src1 = "class C<T> { int a { get; } = 1; }";
        var src2 = "class C<T> { int a { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "int a", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MemberInitializer_Update_StackAllocInConstructor()
    {
        var src1 = "unsafe class C { int a = 1; public C() { int* a = stackalloc int[10]; } }";
        var src2 = "unsafe class C { int a = 2; public C() { int* a = stackalloc int[10]; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@21 -> [a = 2]@21");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[10]", FeaturesResources.constructor));
    }

    [Fact]
    public void MemberInitializer_Update_StackAllocInConstructor_ThisInitializer()
    {
        var src1 = "class C { int a = 1; C() : this(stackalloc int[1]) { } C(System.Span<int> s) { } }";
        var src2 = "class C { int a = 2; C() : this(stackalloc int[1]) { } C(System.Span<int> s) { } }";

        var edits = GetTopEdits(src1, src2);

        // no rude edits for constructors that field initializers are not emitted to
        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68731")]
    public void MemberInitializer_Update_StackAllocInConstructor_Initializer_Field()
    {
        var src1 = "class C : B { int a = 1; C() : base(stackalloc int[1]) { } } class B(System.Span<int> s);";
        var src2 = "class C : B { int a = 2; C() : base(stackalloc int[1]) { } } class B(System.Span<int> s);";

        var edits = GetTopEdits(src1, src2);

        // TODO: allow https://github.com/dotnet/roslyn/issues/68731
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("constructor")));
    }

    [Fact]
    public void FieldInitializerUpdate_StackAllocInConstructor_PrimaryBaseInitializer()
    {
        var src1 = "class C : B(stackalloc int[1]) { int a = 1; } class B(System.Span<int> s);";
        var src2 = "class C : B(stackalloc int[1]) { int a = 2; } class B(System.Span<int> s);";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67307")]
    public void MemberInitializer_Update_StackAllocInOtherInitializer()
    {
        var src1 = "class C { int a = 1; int b = G(stackalloc int[10]); static int G(Span<int> span) => 1; }";
        var src2 = "class C { int a = 2; int b = G(stackalloc int[10]); static int G(Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        // TODO: allow https://github.com/dotnet/roslyn/issues/67307
        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.StackAllocUpdate, "class C", GetResource("constructor", "C()")));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
    public void MemberInitializer_Update_SwitchExpressionInConstructor()
    {
        var src1 = "class C { int a = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
        var src2 = "class C { int a = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_StackAlloc_Update()
    {
        var src1 = "class C { int a { get; } = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a { get; } = G(stackalloc int[20]); static int G(System.Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        // Note: One edit is for the initializer and the other for implicit constructor.
        // We don't attempt to avoid duplicates reported for different members.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[20]", GetResource("auto-property")),
            Diagnostic(RudeEditKind.StackAllocUpdate, "class C", GetResource("constructor", "C()")));
    }

    [Fact]
    public void MemberInitializer_Update_StackAlloc_Delete()
    {
        var src1 = "class C { int a { get; } = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a { get; } = G(default); static int G(System.Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        // Note: One edit is for the initializer and the other for implicit constructor.
        // We don't attempt to avoid duplicates reported for different memebers.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "int a", GetResource("auto-property")),
            Diagnostic(RudeEditKind.StackAllocUpdate, "class C", GetResource("constructor", "C()")));
    }

    [Fact]
    public void MemberInitializer_Update_StackAlloc_Insert()
    {
        var src1 = "class C { int a { get; } = G(default); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a { get; } = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[10]", GetResource("auto-property")));
    }

    [Fact]
    public void PropertyInitializerUpdate_StackAlloc_Delete()
    {
        var src1 = "class C { int a { get; } = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a { get; } = G(default); static int G(System.Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        // Note: One edit is for the initializer and the other for implicit constructor.
        // We don't attempt to avoid duplicates reported for different memebers.
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "int a", GetResource("auto-property")),
            Diagnostic(RudeEditKind.StackAllocUpdate, "class C", GetResource("constructor", "C()")));
    }

    [Fact]
    public void PropertyInitializerUpdate_StackAlloc_Insert()
    {
        var src1 = "class C { int a { get; } = G(default); static int G(System.Span<int> span) => 1; }";
        var src2 = "class C { int a { get; } = G(stackalloc int[10]); static int G(System.Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[10]", GetResource("auto-property")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_StackAlloc(string accessor)
    {
        var src1 = "unsafe class C { int a " + accessor + " = G(stackalloc int[10]); public G(Span<int> span) => 1; }";
        var src2 = "unsafe class C { int a " + accessor + " = G(stackalloc int[20]); public G(Span<int> span) => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[20]", GetResource(accessor == "" ? "field" : "auto-property")),
            Diagnostic(RudeEditKind.StackAllocUpdate, "public G(Span<int> span)", GetResource("constructor")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_StackAlloc_InConstructorWithInitializers1(string accessor)
    {
        var src1 = "unsafe class C { int a " + accessor + " = 1; public C() { int* a = stackalloc int[10]; } }";
        var src2 = "unsafe class C { int a " + accessor + " = 2; public C() { int* a = stackalloc int[10]; } }";

        var edits = GetTopEdits(src1, src2);

        // TODO (tomat): diagnostic should point to the property initializer
        edits.VerifySemanticDiagnostics(
             Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[10]", GetResource("constructor")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_StackAlloc_InConstructorWithInitializers2(string accessor)
    {
        var src1 = "unsafe class C { int a " + accessor + " = 1; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";
        var src2 = "unsafe class C { int a " + accessor + " = 2; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";

        var edits = GetTopEdits(src1, src2);

        // TODO (tomat): diagnostic should point to the property initializer
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[10]", FeaturesResources.constructor));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_StackAlloc_InConstructorWithoutInitializers(string accessor)
    {
        var src1 = "unsafe class C { int a " + accessor + " = 1; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";
        var src2 = "unsafe class C { int a " + accessor + " = 2; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
    public void MemberInitializer_Update_SwitchExpression_InConstructorWithInitializers(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
        var src2 = "class C { int a " + accessor + " = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
    public void MemberInitializer_Update_SwitchExpression_InConstructorWithInitializers2(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = 1; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";
        var src2 = "class C { int a " + accessor + " = 2; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
    public void MemberInitializer_Update_SwitchExpression_InConstructorWithoutInitializers(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = 1; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";
        var src2 = "class C { int a " + accessor + " = 2; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_LambdaInConstructor_Field()
    {
        var src1 = "class C { int a = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
        var src2 = "class C { int a = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@14 -> [a = 2]@14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_LambdaInConstructor_Property()
    {
        var src1 = "class C { int a { get; } = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
        var src2 = "class C { int a { get; } = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_QueryInConstructor_Field()
    {
        var src1 = "using System.Linq; class C { int a = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
        var src2 = "using System.Linq; class C { int a = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@33 -> [a = 2]@33");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_QueryInConstructor_Property()
    {
        var src1 = "using System.Linq; class C { int a { get; } = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
        var src2 = "using System.Linq; class C { int a { get; } = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_AnonymousTypeInConstructor_Field()
    {
        var src1 = "class C { int a = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
        var src2 = "class C { int a = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_AnonymousTypeInConstructor_Property()
    {
        var src1 = "class C { int a { get; } = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
        var src2 = "class C { int a { get; } = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_PartialTypeWithSingleDeclaration_Field()
    {
        var src1 = "partial class C { int a = 1; }";
        var src2 = "partial class C { int a = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@22 -> [a = 2]@22");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_PartialTypeWithSingleDeclaration_Property()
    {
        var src1 = "partial class C { int a { get; } = 1; }";
        var src2 = "partial class C { int a { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_PartialTypeWithMultipleDeclarations_Field()
    {
        var src1 = "partial class C { int a = 1; } partial class C { }";
        var src2 = "partial class C { int a = 2; } partial class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 1]@22 -> [a = 2]@22");

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_PartialTypeWithMultipleDeclarations_Property()
    {
        var src1 = "partial class C { int a { get; } = 1; } partial class C { }";
        var src2 = "partial class C { int a { get; } = 2; } partial class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
            ]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_ParenthesizedLambda(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = F(1, (x, y) => x + y); }";
        var src2 = "class C { int a " + accessor + " = F(2, (x, y) => x + y); }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_AnonymousType(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = F(1, new { A = 1, B = 2 }); }";
        var src2 = "class C { int a " + accessor + " = F(2, new { A = 1, B = 2 }); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_Query_Field(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = F(1, from goo in bar select baz); }";
        var src2 = "class C { int a " + accessor + " = F(2, from goo in bar select baz); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ get; }")]
    public void MemberInitializer_Update_Lambda(string accessor)
    {
        var src1 = "class C { int a " + accessor + " = F(1, x => x); }";
        var src2 = "class C { int a " + accessor + " = F(2, x => x); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_ImplicitCtor_EditInitializerWithLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_ImplicitCtor_EditInitializerWithoutLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 1;
    int B = F(<N:0.0>b => b + 1</N:0.0>);
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 2;
    int B = F(<N:0.0>b => b + 1</N:0.0>);
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_CtorIncludingInitializers_EditInitializerWithLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    <N:0.2>public C() {}</N:0.2>
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    <N:0.2>public C() {}</N:0.2>
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_CtorIncludingInitializers_EditInitializerWithoutLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 1;
    int B = F(<N:0.0>b => b + 1</N:0.0>);

    public C() {}
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 2;
    int B = F(<N:0.0>b => b + 1</N:0.0>);

    public C() {}
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializers_EditInitializerWithLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) {}
    public C(bool b) {}
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C(int a) {}
    public C(bool b) {}
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda_Trivia1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B =   F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 2</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda_Trivia1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

        public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithoutLambda1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(2); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_EditConstructorNotIncludingInitializers()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(2); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_RemoveCtorInitializer1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    unsafe public C(int a) { char* buffer = stackalloc char[16]; F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    unsafe public C(int a) { char* buffer = stackalloc char[16]; F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_AddCtorInitializer1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_UpdateBaseCtorInitializerWithLambdas1()
    {
        var src1 = @"
using System;

class B
{
    public B(int a) { }
}

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(bool b)
      : base(F(<N:0.2>c => c + 1</N:0.2>))
    { 
        F(<N:0.3>d => d + 1</N:0.3>);
    }
}
";
        var src2 = @"
using System;

class B
{
    public B(int a) { }
}

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(bool b)
      : base(F(<N:0.2>c => c + 2</N:0.2>))
    {
        F(<N:0.3>d => d + 1</N:0.3>);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
            ]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" : base()")]
    public void MemberInitializer_Update_Lambda_ConstructorWithMemberInitializers_ReplacingCustomWithSynthesized(string initializer)
    {
        var src1 = $$"""
using System;

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C() {{initializer}}
    {
    }
}
""";
        var src2 = @"
using System;

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            ]);
    }

    [Theory, CombinatorialData]
    public void MemberInitializer_Update_Lambda_ConstructorWithMemberInitializers_ReplacingCustomWithSynthesized_Primary(
        [CombinatorialValues("", "()")] string initializer, bool isInsert)
    {
        var src1 = $$"""
using System;

class C() : B{{initializer}}
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
""";
        var src2 = @"
using System;

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
        if (isInsert)
        {
            (src1, src2) = (src2, src1);
        }

        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Lambda_PartialDeclarationDelete_SingleDocument()
    {
        var src1 = @"
partial class C
{
    int x = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int y = F(<N:0.1>a => a + 10</N:0.1>);
}

partial class C
{
    public C() { }
    static int F(Func<int, int> x) => 1;
}
";

        var src2 = @"
partial class C
{
    int x = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int y = F(<N:0.1>a => a + 10</N:0.1>);

    static int F(Func<int, int> x) => 1;
}
";
        var edits = GetTopEdits(src1, src2);

        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), syntaxMap[0]),
            ]);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void FieldInitializerUpdate_Lambdas_InsertPrimaryConstructorParameterUse(string keyword)
    {
        var src1 = keyword + " C(int x, int y) { public System.Func<int> F = new(() => x); }";
        var src2 = keyword + " C(int x, int y) { public System.Func<int> F = new(() => x + y); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters is [_, _]), preserveLocalVariables: true),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_ActiveStatements1()
    {
        var src1 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0>;</AS:0>
    int B = 1;

    public C(int a) { Console.WriteLine(1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
        var src2 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0>;</AS:0>
    int B = 2;

    public C(int a) { Console.WriteLine(1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);
        var activeStatements = GetActiveStatements(src1, src2);

        edits.VerifySemantics(
            activeStatements,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0]),
            ]);
    }

    [Fact]
    public void MemberInitializer_Update_Partial_SemanticError()
    {
        var src1 = @"
partial class C
{
    partial int P => 1;
}

partial class C
{
    partial int P => 1;
}
";
        var src2 = @"
partial class C
{
    partial int P => 1;
}

partial class C
{
    partial int P => 2;

    public C() { }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => ((IPropertySymbol)c.GetMember<INamedTypeSymbol>("C").GetMembers("P").First()).GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true));
    }

    [Fact]
    public void MemberInitializer_Rename_Property()
    {
        var src1 = "class C { int A { get; } = 1; }";
        var src2 = "class C { int B { get; } = 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.A"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_A"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_B")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void MemberInitializer_Update_Reloadable_Partial()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 1; }";
        var srcB1 = "partial class C { int y = 1; }";

        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 2; }";
        var srcB2 = "partial class C { int y = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    #endregion

    #region Fields

    [Fact]
    public void Field_Rename()
    {
        var src1 = "class C { int a = 0; }";
        var src2 = "class C { int b = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a = 0]@14 -> [b = 0]@14");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "b = 0", GetResource("field", "a")));
    }

    [Fact]
    public void Field_Kind_Update()
    {
        var src1 = "class C { Action a; }";
        var src2 = "class C { event Action a; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Action a;]@10 -> [event Action a;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.FieldKindUpdate, "event Action a", GetResource("field")));
    }

    [Theory]
    [InlineData("static")]
    [InlineData("volatile")]
    [InlineData("const")]
    public void Field_Modifiers_Update(string oldModifiers, string newModifiers = "")
    {
        if (oldModifiers != "")
        {
            oldModifiers += " ";
        }

        if (newModifiers != "")
        {
            newModifiers += " ";
        }

        var src1 = "class C { " + oldModifiers + "int F = 0; }";
        var src2 = "class C { " + newModifiers + "int F = 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [" + oldModifiers + "int F = 0;]@10 -> [" + newModifiers + "int F = 0;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F = 0", GetResource(oldModifiers.Contains("const") ? "const field" : "field")));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Field_Modifiers_Accessibility_Update_Significant(string accessibility)
    {
        var src1 = "class C { " + accessibility + " int F; }";
        var src2 = "class C { int F; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
            ]);
    }

    [Fact]
    public void Field_Modifiers_Accessibility_Update_Insignificant()
    {
        var src1 = "class C { private int F; }";
        var src2 = "class C { int F; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics();
    }

    [Fact]
    public void Field_Modifier_Add_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int F; }";

        var srcA2 = "partial class C { static int F; }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.ModifiersUpdate, "F", FeaturesResources.field)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Field_Attribute_Add_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int F; }";

        var srcA2 = "partial class C { [System.Obsolete]int F; }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                    ]),

                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Field_FixedSize_Update()
    {
        var src1 = "struct S { public unsafe fixed byte a[1], b[2]; }";
        var src2 = "struct S { public unsafe fixed byte a[2], b[3]; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a[1]]@36 -> [a[2]]@36",
            "Update [b[2]]@42 -> [b[3]]@42");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.FixedSizeFieldUpdate, "a[2]", FeaturesResources.field),
            Diagnostic(RudeEditKind.FixedSizeFieldUpdate, "b[3]", FeaturesResources.field));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1120407")]
    public void Field_Const_Update()
    {
        var src1 = "class C { const int x = 0; }";
        var src2 = "class C { const int x = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [x = 0]@20 -> [x = 1]@20");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InitializerUpdate, "x = 1", FeaturesResources.const_field));
    }

    [Fact]
    public void Field_Event_VariableDeclarator_Update()
    {
        var src1 = "class C { event Action a; }";
        var src2 = "class C { event Action a = () => { }; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [a]@23 -> [a = () => { }]@23");

        edits.VerifySemanticDiagnostics(capabilities:
            EditAndContinueCapabilities.AddMethodToExistingType |
            EditAndContinueCapabilities.AddStaticFieldToExistingType |
            EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Field_Reorder()
    {
        var src1 = "class C { int a = 0; int b = 1; int c = 2; }";
        var src2 = "class C { int c = 2; int a = 0; int b = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [int c = 2;]@32 -> @10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "int c = 2", FeaturesResources.field));
    }

    [Fact]
    public void Field_Insert()
    {
        var src1 = "class C {  }";
        var src2 = "class C { int a = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [int a = 1;]@10",
            "Insert [int a = 1]@10",
            "Insert [a = 1]@14");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.a")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Field_Insert_IntoStruct()
    {
        var src1 = @"
struct S 
{ 
    public int a; 

    public S(int z) { this = default(S); a = z; }
}
";
        var src2 = @"
struct S 
{ 
    public int a; 

    private int b; 
    private static int c; 
    private static int f = 1;
    private event System.Action d; 

    public S(int z) { this = default(S); a = z; }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoStruct, "b", FeaturesResources.field, CSharpFeaturesResources.struct_),
            Diagnostic(RudeEditKind.InsertIntoStruct, "c", FeaturesResources.field, CSharpFeaturesResources.struct_),
            Diagnostic(RudeEditKind.InsertIntoStruct, "f = 1", FeaturesResources.field, CSharpFeaturesResources.struct_),
            Diagnostic(RudeEditKind.InsertIntoStruct, "d", CSharpFeaturesResources.event_field, CSharpFeaturesResources.struct_));
    }

    [Fact]
    public void Field_Insert_IntoLayoutClass_Auto()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Auto)]
class C 
{ 
    private int a; 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Auto)]
class C 
{ 
    private int a; 
    private int b; 
    private int c; 
    private static int d; 
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.b")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.c")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.d")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType);
    }

    [Fact]
    public void Field_Insert_IntoLayoutClass_Explicit()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C 
{ 
    [FieldOffset(0)]
    private int a; 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C 
{ 
    [FieldOffset(0)]
    private int a; 

    [FieldOffset(0)]
    private int b; 

    [FieldOffset(4)]
    private int c; 

    private static int d; 
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", FeaturesResources.field, FeaturesResources.class_),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", FeaturesResources.field, FeaturesResources.class_),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", FeaturesResources.field, FeaturesResources.class_));
    }

    [Fact]
    public void Field_Insert_IntoLayoutClass_Sequential()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
    private int b; 
    private int c; 
    private static int d; 
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", FeaturesResources.field, FeaturesResources.class_),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", FeaturesResources.field, FeaturesResources.class_),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", FeaturesResources.field, FeaturesResources.class_));
    }

    [Fact]
    public void Field_Insert_WithInitializersAndLambdas1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C()
    {
        F(<N:0.1>c => c + 1</N:0.1>);
    }
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C()
    {
        F(<N:0.1>c => c + 1</N:0.1>);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Field_Insert_ConstructorReplacingImplicitConstructor_WithInitializersAndLambdas()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C()                                // new ctor replacing existing implicit constructor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            ],
            capabilities:
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.AddStaticFieldToExistingType |
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.NewTypeDefinition);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "c", GetResource("lambda")),
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "B = F(b => b + 1)", GetResource("field")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
    public void Field_Insert_ParameterlessConstructorInsert_WithInitializersAndLambdas()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C(int x) {}
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C(int x) {}

    public C()                                // new ctor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C()")],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        // TODO (bug https://github.com/dotnet/roslyn/issues/2504):
        //edits.VerifySemantics(
        //    ActiveStatementsDescription.Empty,
        //    new[]
        //    {
        //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
        //    });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
    public void Field_Insert_ConstructorInsert_WithInitializersAndLambdas1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C(int x)                           // new ctor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        _ = GetSyntaxMap(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C(int x)"));

        // TODO (bug https://github.com/dotnet/roslyn/issues/2504):
        //edits.VerifySemantics(
        //    ActiveStatementsDescription.Empty,
        //    new[]
        //    {
        //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
        //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
        //    });
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
    public void Field_Insert_ConstructorInsert_WithInitializersButNoExistingLambdas1()
    {
        var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(null);
}
";
        var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(null);
    int B = F(b => b + 1);                    // new field

    public C(int x)                           // new ctor
    {
        F(c => c + 1);
    }
}
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single()),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetParameterlessConstructor("C"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Field_Insert_NotSupportedByRuntime()
    {
        var src1 = "class C {  }";
        var src2 = "class C { public int a = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "a = 1", FeaturesResources.field)],
            capabilities: EditAndContinueCapabilities.AddStaticFieldToExistingType);
    }

    [Fact]
    public void Field_Insert_Static_NotSupportedByRuntime()
    {
        var src1 = "class C {  }";
        var src2 = "class C { public static int a = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "a = 1", FeaturesResources.field)],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Field_Attribute_Add_NotSupportedByRuntime()
    {
        var src1 = @"
class C
{
    public int a = 1, x = 1;
}";
        var src2 = @"
class C
{
    [System.Obsolete]public int a = 1, x = 1;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [public int a = 1, x = 1;]@18 -> [[System.Obsolete]public int a = 1, x = 1;]@18");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public int a = 1, x = 1", FeaturesResources.field),
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public int a = 1, x = 1", FeaturesResources.field),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Field_Attribute_Add()
    {
        var src1 = @"
class C
{
    public int a, b;
}";
        var src2 = @"
class C
{
    [System.Obsolete]public int a, b;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.b"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Field_Attribute_Add_WithInitializer()
    {
        var src1 = @"
class C
{
    int a;
}";
        var src2 = @"
class C
{
    [System.Obsolete]int a = 0;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            ActiveStatementsDescription.Empty,
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Field_Attribute_DeleteInsertUpdate_WithInitializer()
    {
        var srcA1 = "partial class C { int a = 1; }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { [System.Obsolete]int a = 2; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Field_Delete1()
    {
        var src1 = "class C { int a = 1; }";
        var src2 = "class C {  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [int a = 1;]@10",
            "Delete [int a = 1]@10",
            "Delete [a = 1]@14");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
    }

    [Fact]
    public void Field_UnsafeModifier_Update()
    {
        var src1 = "struct Node { unsafe Node* left; }";
        var src2 = "struct Node { Node* left; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [unsafe Node* left;]@14 -> [Node* left;]@14");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Field_ModifierAndType_Update()
    {
        var src1 = "struct Node { unsafe Node* left; }";
        var src2 = "struct Node { Node left; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [unsafe Node* left;]@14 -> [Node left;]@14",
            "Update [Node* left]@21 -> [Node left]@14");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, "Node left", FeaturesResources.field));
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("object", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Field_Type_Update_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "class C { " + oldType + " F, G; }";
        var src2 = "class C { " + newType + " F, G; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G")));
    }

    [Theory]
    [InlineData("int", "string")]
    [InlineData("int", "int?")]
    [InlineData("(int a, int b)", "(int a, double b)")]
    public void Field_Type_Update_RuntimeTypeChanged(string oldType, string newType)
    {
        var src1 = "class C { " + oldType + " F, G; }";
        var src2 = "class C { " + newType + " F, G; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.TypeUpdate, newType + " F, G", GetResource("field")),
            Diagnostic(RudeEditKind.TypeUpdate, newType + " F, G", GetResource("field")));
    }

    [Fact]
    public void Field_Type_Update_ReorderRemoveAdd()
    {
        var src1 = "class C { int F, G, H; bool U; }";
        var src2 = "class C { string G, F; double V, U; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int F, G, H]@10 -> [string G, F]@10",
            "Reorder [G]@17 -> @17",
            "Update [bool U]@23 -> [double V, U]@23",
            "Insert [V]@30",
            "Delete [H]@20");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "G", FeaturesResources.field),
            Diagnostic(RudeEditKind.TypeUpdate, "string G, F", FeaturesResources.field),
            Diagnostic(RudeEditKind.TypeUpdate, "string G, F", FeaturesResources.field),
            Diagnostic(RudeEditKind.TypeUpdate, "double V, U", FeaturesResources.field),
            Diagnostic(RudeEditKind.Delete, "string G, F", DeletedSymbolDisplay(FeaturesResources.field, "H")));
    }

    #endregion

    #region Properties

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Property_Update_Modifiers_Accessibility_ExpressionBody_Significant(string accessibility)
    {
        var src1 = $$"""
            class C
            {
                {{accessibility}}
                int P => 1;
            }
            """;

        var src2 = """
            class C
            {
                
                int P => 1;
            }
            """;

        var edits = GetTopEdits(src1, src2);

        // update of the property itself is not necessary and could be eliminated:
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"))
            ]);
    }

    [Fact]
    public void Property_Update_Modifiers_Accessibility_ExpressionBody_Insignificant()
    {
        var src1 = "class C { private int P => 1; }";
        var src2 = "class C {         int P => 1; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"))
            ]);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Property_Update_Modifiers_Accessibility_ReadOnly_Significant(string accessibility)
    {
        var src1 = $$"""
            class C
            {
                {{accessibility}}
                int P { get; }
            }
            """;

        var src2 = """
            class C
            {

                int P { get; }
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"))
            ]);
    }

    [Fact]
    public void Property_Update_Modifiers_Accessibility_Mix()
    {
        var src1 = """
            class C
            {
                public
                int P
                {
                    protected
                    get;
                    set;
                }
            }
            """;

        var src2 = """
            class C
            {
                protected
                int P
                {
                    
                    get;
                    set;
                }
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
                // The update is not necessary and could be eliminated:
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"))
            ]);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Property_Update_Modifiers_Accessibility_Writable_Significant(string accessibility)
    {
        var src1 = $$"""
            class C
            {
                {{accessibility}}
                int P { get; set; }
            }
            """;

        var src2 = """
            class C
            {

                int P { get; set; }
            }
            """;

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P"))
            ]);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("sealed override", "override")]
    public void Property_Modifiers_Update(string oldModifiers, string newModifiers = "")
    {
        if (oldModifiers != "")
        {
            oldModifiers += " ";
        }

        if (newModifiers != "")
        {
            newModifiers += " ";
        }

        var src1 = "class C { " + oldModifiers + "int F => 0; }";
        var src2 = "class C { " + newModifiers + "int F => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [" + oldModifiers + "int F => 0;]@10 -> [" + newModifiers + "int F => 0;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F", FeaturesResources.property_));
    }

    [Fact]
    public void Property_ExpressionBody_Rename()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { int Q => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_ExpressionBody_Update()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { int P => 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P => 1;]@10 -> [int P => 2;]@10",
            "Update [=> 1]@16 -> [=> 2]@16");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
    public void Property_ExpressionBody_ModifierUpdate()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { unsafe int P => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [int P => 1;]@10 -> [unsafe int P => 1;]@10");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Property_ExpressionBodyToBlockBody1()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { int P { get { return 2; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P => 1;]@10 -> [int P { get { return 2; } }]@10",
            "Insert [{ get { return 2; } }]@16",
            "Insert [get { return 2; }]@18",
            "Delete [=> 1]@16");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void Property_ExpressionBodyToBlockBody2()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { int P { get { return 2; } set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P => 1;]@10 -> [int P { get { return 2; } set { } }]@10",
            "Insert [{ get { return 2; } set { } }]@16",
            "Insert [get { return 2; }]@18",
            "Insert [set { }]@36",
            "Delete [=> 1]@16");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_BlockBodyToExpressionBody1()
    {
        var src1 = "class C { int P { get { return 2; } } }";
        var src2 = "class C { int P => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P { get { return 2; } }]@10 -> [int P => 1;]@10",
            "Insert [=> 1]@16",
            "Delete [{ get { return 2; } }]@16",
            "Delete [get { return 2; }]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void Property_BlockBodyToExpressionBody2()
    {
        var src1 = "class C { int P { get { return 2; } set { } } }";
        var src2 = "class C { int P => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P { get { return 2; } set { } }]@10 -> [int P => 1;]@10",
            "Insert [=> 1]@16",
            "Delete [{ get { return 2; } set { } }]@16",
            "Delete [get { return 2; }]@18",
            "Delete [set { }]@36");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_ExpressionBodyToGetterExpressionBody()
    {
        var src1 = "class C { int P => 1; }";
        var src2 = "class C { int P { get => 2; } }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P => 1;]@10 -> [int P { get => 2; }]@10",
            "Insert [{ get => 2; }]@16",
            "Insert [get => 2;]@18",
            "Delete [=> 1]@16");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_GetterExpressionBodyToExpressionBody()
    {
        var src1 = "class C { int P { get => 2; } }";
        var src2 = "class C { int P => 1; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [int P { get => 2; }]@10 -> [int P => 1;]@10",
            "Insert [=> 1]@16",
            "Delete [{ get => 2; }]@16",
            "Delete [get => 2;]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_GetterBlockBodyToGetterExpressionBody()
    {
        var src1 = "class C { int P { get { return 2; } } }";
        var src2 = "class C { int P { get => 2; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_SetterBlockBodyToSetterExpressionBody()
    {
        var src1 = "class C { int P { set { } } }";
        var src2 = "class C { int P { set => F(); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [set { }]@18 -> [set => F();]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_InitBlockBodyToInitExpressionBody()
    {
        var src1 = "class C { int P { init { } } }";
        var src2 = "class C { int P { init => F(); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [init { }]@18 -> [init => F();]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod, preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_GetterExpressionBodyToGetterBlockBody()
    {
        var src1 = "class C { int P { get => 2; } }";
        var src2 = "class C { int P { get { return 2; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_GetterBlockBodyWithSetterToGetterExpressionBodyWithSetter()
    {
        var src1 = "class C { int P { get => 2;         set { Console.WriteLine(0); } } }";
        var src2 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Property_GetterExpressionBodyWithSetterToGetterBlockBodyWithSetter()
    {
        var src1 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";
        var src2 = "class C { int P { get => 2; set { Console.WriteLine(0); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void Property_Rename1()
    {
        var src1 = "class C { int P { get { return 1; } } }";
        var src2 = "class C { int Q { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int Q", FeaturesResources.property_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Rename2()
    {
        var interfaces = """
            interface I
            {
                int P { get; }
            }
            
            interface J
            {
                int P { get; }
            }
            """;
        var src1 = "class C { int I.P { get { return 1; } } } " + interfaces;
        var src2 = "class C { int J.P { get { return 1; } } } " + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "int J.P", GetResource("property", "I.P")));
    }

    [Fact]
    public void Property_Rename3()
    {
        var src1 = "class C { int P { get { return 1; } set { } } }";
        var src2 = "class C { int Q { get { return 1; } set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Rename4()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { int Q { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Rename_SemanticError_NoAccessors()
    {
        var src1 = "class C { System.Action E { } }";
        var src2 = "class C { System.Action F { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_RenameAndUpdate()
    {
        var src1 = "class C { int P { get { return 1; } } }";
        var src2 = "class C { int Q { get { return 2; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Q")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Rename_Stackalloc()
    {
        var src1 = "class C { int G(Span<char> s) => 0; int P { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";
        var src2 = "class C { int G(Span<char> s) => 0; int Q { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("property getter")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("property setter"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void Property_Delete(string keyword)
    {
        var src1 = keyword + " C { int P { get { return 1; } set { } } }";
        var src2 = keyword + " C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Delete_GetOnly()
    {
        var src1 = "class C { int P { get { return 1; } } }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Delete_Auto_Class()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Delete_Auto_Struct()
    {
        var src1 = "struct C { int P { get; set; } }";
        var src2 = "struct C { }";

        var edits = GetTopEdits(src1, src2);

        // We do not report rude edits when deleting auto-properties/events of a type with a sequential or explicit layout.
        // The properties are updated to throw and the backing field remains in the type.
        // The deleted field will remain unused since adding the property/event back is a rude edit.
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void PropertyAccessorDelete1()
    {
        var src1 = "class C { int P { get { return 1; } set { } } }";
        var src2 = "class C { int P { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void PropertyAccessorDelete2()
    {
        var src1 = "class C { int P { set { } get { return 1; } } }";
        var src2 = "class C { int P { set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Reorder1()
    {
        var src1 = "class C { int P { get { return 1; } } int Q { get { return 1; } }  }";
        var src2 = "class C { int Q { get { return 1; } } int P { get { return 1; } }  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [int Q { get { return 1; } }]@38 -> @10");

        // TODO: we can allow the move since the property doesn't have a backing field
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.property_));
    }

    [Fact]
    public void Property_Reorder_Auto_Class()
    {
        var src1 = "class C { int P { get; set; } int Q { get; set; }  }";
        var src2 = "class C { int Q { get; set; } int P { get; set; }  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [int Q { get; set; }]@30 -> @10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.auto_property));
    }

    [Fact]
    public void Property_Reorder_Auto_Struct()
    {
        var src1 = "struct C { int P { get; set; } byte Q { get; set; }  }";
        var src2 = "struct C { byte Q { get; set; } int P { get; set; }  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [byte Q { get; set; }]@31 -> @11");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "byte Q", FeaturesResources.auto_property));
    }

    [Fact]
    public void PropertyAccessorReorder_GetSet()
    {
        var src1 = "class C { int P { get { return 1; } set { } } }";
        var src2 = "class C { int P { set { } get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [set { }]@36 -> @18");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void PropertyAccessorReorder_GetInit()
    {
        var src1 = "class C { int P { get { return 1; } init { } } }";
        var src2 = "class C { int P { init { } get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [init { }]@36 -> @18");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Property_Update_Type()
    {
        var src1 = "class C { byte P { get; set; } }";
        var src2 = "class C { char P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "char P", GetResource("auto-property"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Update_Type_Stackalloc()
    {
        // only type is changed, no changes to the accessors (not even whitespace)
        var src1 = "class C { byte G(Span<char> s) => 0; byte P { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";
        var src2 = "class C { byte G(Span<char> s) => 0; long P { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("property getter")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("property setter"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Update_Type_WithBodies()
    {
        var src1 = "class C { int P { get { return 1; } set { } } }";
        var src2 = "class C { char P { get { return 'a'; } set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "char P", FeaturesResources.property_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Update_Type_TypeLayout()
    {
        var src1 = "struct C { byte P { get; } }";
        var src2 = "struct C { long P { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoStruct, "long P", GetResource("auto-property"), GetResource("struct"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Update_AddAttribute()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { [System.Obsolete]int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/68458")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/68458")]
    public void Property_Update_AddAttribute_FieldTarget()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { [field: System.Obsolete]int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int P", FeaturesResources.property_)],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void PropertyAccessorUpdate_AddAttribute()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { int P { [System.Obsolete]get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "get", CSharpFeaturesResources.property_getter)],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void PropertyAccessorUpdate_AddAttribute2()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { int P { get; [System.Obsolete]set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "set", CSharpFeaturesResources.property_setter)],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { int P { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Insert_Static()
    {
        var src1 = "class C { }";
        var src2 = "class C { static int P { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void PropertyInsert_NotSupportedByRuntime()
    {
        var src1 = "class C { }";
        var src2 = "class C { int P { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", FeaturesResources.property_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992578")]
    public void Property_Insert_Incomplete()
    {
        var src1 = "class C { }";
        var src2 = "class C { public int P { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [public int P { }]@10", "Insert [{ }]@23");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
    public void Property_Insert_PInvoke()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    private static extern int P1 { [DllImport(""x.dll"")]get; }
    private static extern int P2 { [DllImport(""x.dll"")]set; }
    private static extern int P3 { [DllImport(""x.dll"")]get; [DllImport(""x.dll"")]set; }
}
";
        var edits = GetTopEdits(src1, src2);

        // CLR doesn't support methods without a body
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertExtern, "private static extern int P1", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertExtern, "private static extern int P2", FeaturesResources.property_),
            Diagnostic(RudeEditKind.InsertExtern, "private static extern int P3", FeaturesResources.property_));
    }

    [Fact]
    public void Property_Insert_IntoStruct()
    {
        var src1 = @"
struct S 
{ 
    public int a; 
    
    public S(int z) { a = z; } 
}
";
        var src2 = @"
struct S 
{ 
    public int a; 
    private static int c { get; set; } 
    private static int e { get { return 0; } set { } } 
    private static int g { get; } = 1;
    private static int i { get; set; } = 1;
    private static int k => 1;
    private static int l { get => 1; set {} }
    private static int m { get => 1; set => k; }
    public S(int z) { a = z; }
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoStruct, "private static int c", GetResource("auto-property"), GetResource("struct")),
            Diagnostic(RudeEditKind.InsertIntoStruct, "private static int g", GetResource("auto-property"), GetResource("struct")),
            Diagnostic(RudeEditKind.InsertIntoStruct, "private static int i", GetResource("auto-property"), GetResource("struct")));
    }

    [Fact]
    public void Property_Insert_IntoLayoutClass_Sequential()
    {
        var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
}
";
        var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
    private int b { get; set; }
    private static int c { get; set; } 
    private int d { get { return 0; } set { } }
    private static int e { get { return 0; } set { } } 
    private int f { get; } = 1;
    private static int g { get; } = 1;
    private int h { get; set; } = 1;
    private static int i { get; set; } = 1;
    private int j => 1;
    private static int k => 1;
    private int l { get => 1; set { } }
    private static int m { get => 1; set { } }
    private int n { get { return 1; } set => a; }
    private static int o { get { return 1; } set => a; }
}    
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int b", GetResource("auto-property"), GetResource("class")),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int c", GetResource("auto-property"), GetResource("class")),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int f", GetResource("auto-property"), GetResource("class")),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int g", GetResource("auto-property"), GetResource("class")),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int h", GetResource("auto-property"), GetResource("class")),
            Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int i", GetResource("auto-property"), GetResource("class")));
    }

    [Fact]
    public void Property_Insert_Auto()
    {
        var src1 = "class C { }";
        var src2 = "class C { int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Insert_Auto_Static()
    {
        var src1 = "class C { }";
        var src2 = "class C { static int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType);
    }

    [Fact]
    public void Property_Insert_Auto_GenericType()
    {
        var src1 = "class C<T> { }";
        var src2 = "class C<T> { int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Insert_Auto_GenericType_Static()
    {
        var src1 = "class C<T> { }";
        var src2 = "class C<T> { static int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P"))],
            capabilities:
                EditAndContinueCapabilities.AddMethodToExistingType |
                EditAndContinueCapabilities.AddStaticFieldToExistingType |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericAddFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "static int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType);
    }

    // Design: Adding private accessors should also be allowed since we now allow adding private methods
    // and adding public properties and/or public accessors are not allowed.
    [Fact]
    public void Property_Private_AccessorAdd()
    {
        var src1 = "class C { int _p; int P { get { return 1; } } }";
        var src2 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [set { _p = value; }]@44");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
    public void Property_Private_AccessorDelete()
    {
        var src1 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";
        var src2 = "class C { int _p; int P { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [set { _p = value; }]@44");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd1()
    {
        var src1 = "class C { int P { get; } }";
        var src2 = "class C { int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [set;]@23");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd2()
    {
        var src1 = "class C { public int P { get; } }";
        var src2 = "class C { public int P { get; private set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [private set;]@30");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd4()
    {
        var src1 = "class C { public int P { get; } }";
        var src2 = "class C { public int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [set;]@30");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd5()
    {
        var src1 = "class C { public int P { get; } }";
        var src2 = "class C { public int P { get; internal set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [internal set;]@30");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd6()
    {
        var src1 = "class C { int P { get; } = 1; }";
        var src2 = "class C { int P { get; set; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [set;]@23");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Auto_Private_AccessorAdd_Init()
    {
        var src1 = "class C { int P { get; } = 1; }";
        var src2 = "class C { int P { get; init; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [init;]@23");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
    public void Property_Auto_Private_AccessorDelete_Get()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { int P { set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [get;]@18");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_SetToInit()
    {
        var src1 = "class C { int P { get; set; } }";
        var src2 = "class C { int P { get; init; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [set;]@23 -> [init;]@23");

        // not allowed since it changes the backing field readonly-ness and the signature of the setter (modreq)
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.AccessorKindUpdate, "init"));
    }

    [Fact]
    public void Property_Auto_Accessor_InitToSet()
    {
        var src1 = "class C { int P { get; init; } }";
        var src2 = "class C { int P { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [init;]@23 -> [set;]@23");

        // not allowed since it changes the backing field readonly-ness and the signature of the setter (modreq)
        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.AccessorKindUpdate, "set"));
    }

    [Fact]
    public void Propert_Auto_Private_AccessorDelete_Set()
    {
        var src1 = "class C { int P { get; set; } = 1; }";
        var src2 = "class C { int P { get; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [set;]@23");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Propert_Auto_Private_AccessorDelete_Init()
    {
        var src1 = "class C { int P { get; init; } = 1; }";
        var src2 = "class C { int P { get; } = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [init;]@23");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update()
    {
        var src1 = "class C { int P { get; } }";
        var src2 = "class C { int P { set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get;]@18 -> [set;]@18");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.AccessorKindUpdate, "set"));
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingExplicitWithImplicit()
    {
        var src1 = "class C { int P { get => 1; } }";
        var src2 = "class C { int P { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "get", GetResource("property getter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingExplicitWithImplicit_GenericType()
    {
        var src1 = "class C<T> { int P { get => 1; } }";
        var src2 = "class C<T> { int P { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities:
                EditAndContinueCapabilities.AddInstanceFieldToExistingType |
                EditAndContinueCapabilities.GenericAddFieldToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "get", GetResource("property getter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingImplicitWithExplicit()
    {
        var src1 = "class C { int P { get; } }";
        var src2 = "class C { int P { get => 1; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingImplicitWithExplicit_GenericType()
    {
        var src1 = "class C<T> { int P { get; } }";
        var src2 = "class C<T> { int P { get => 1; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "get", GetResource("property getter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingImplicitWithExpressionBodiedProperty()
    {
        var src1 = "class C { int P { get; } }";
        var src2 = "class C { int P => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Auto_Accessor_Update_ReplacingImplicitWithExpressionBodiedProperty_GenericType()
    {
        var src1 = "class C<T> { int P { get; } }";
        var src2 = "class C<T> { int P => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "int P", GetResource("auto-property")),
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "int P", GetResource("property getter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_ReadOnlyRef_Insert()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { ref readonly int P { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [ref readonly int P { get; }]@13",
            "Insert [{ get; }]@32",
            "Insert [get;]@34");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_ReadOnlyRef_Update()
    {
        var src1 = "class Test { int P { get; } }";
        var src2 = "class Test { ref readonly int P { get; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int P { get; }]@13 -> [ref readonly int P { get; }]@13");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.P"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.get_P"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.P")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.get_P")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "ref readonly int P", GetResource("auto-property"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Property_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int P { get => 1; set { } } }";

        var srcA2 = "partial class C { int P { get => 1; set { } } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void PropertyInit_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int Q { get => 1; init { } }}";

        var srcA2 = "partial class C { int Q { get => 1; init { } }}";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").SetMethod)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Property_Auto_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int P { get; set; } int Q { get; init; } }";

        var srcA2 = "partial class C { int P { get; set; } int Q { get; init; } }";
        var srcB2 = "partial class C { }";

        // Accessors need to be updated even though they do not have an explicit body. 
        // There is still a sequence point generated for them whose location needs to be updated.
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").SetMethod),
                    ]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Property_AutoWithInitializer_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int P { get; set; } = 1; }";

        var srcA2 = "partial class C { int P { get; set; } = 1; }";
        var srcB2 = "partial class C { }";

        // Accessors need to be updated even though they do not have an explicit body. 
        // There is still a sequence point generated for them whose location needs to be updated.
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Property_WithExpressionBody_Partial_InsertDeleteUpdate()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int P => 1; }";

        var srcA2 = "partial class C { int P => 2; }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod)]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Property_Auto_ReadOnly_Add()
    {
        var src1 = @"
struct S
{
    int P { get; }
}";
        var src2 = @"
struct S
{
    readonly int P { get; }
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Property_InMutableStruct_ReadOnly_Add()
    {
        var src1 = @"
struct S
{
     int P1 { get => 1; }
     int P2 { get => 1; set {}}
     int P3 { get => 1; set {}}
     int P4 { get => 1; set {}}
}";
        var src2 = @"
struct S
{
     readonly int P1 { get => 1; }
     int P2 { readonly get => 1; set {}}
     int P3 { get => 1; readonly set {}}
     readonly int P4 { get => 1; set {}}
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.get_P1")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.get_P2")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.get_P4")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.set_P2")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.set_P3")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.set_P4")),
            ]);
    }

    [Fact]
    public void Property_InReadOnlyStruct_ReadOnly_Add()
    {
        // indent to align accessor bodies and avoid updates caused by sequence point location changes

        var src1 = @"
readonly struct S
{
              int P1 { get => 1; }
     int P2 {          get => 1; set {}}
     int P3 { get => 1;          set {}}
              int P4 { get => 1; set {}}
}";
        var src2 = @"
readonly struct S
{
     readonly int P1 { get => 1; }
     int P2 { readonly get => 1; set {}}
     int P3 { get => 1; readonly set {}}
     readonly int P4 { get => 1; set {}}
}";
        var edits = GetTopEdits(src1, src2);

        // updates only for accessors whose modifiers were explicitly updated
        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IPropertySymbol>("P2").GetMethod, preserveLocalVariables: false),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IPropertySymbol>("P3").SetMethod, preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69317")]
    public void Property_Rename_ShadowingPrimaryParameter()
    {
        var src1 = @"
class C(int A, int B)
{
    public int B { get; init; }

    public int F() => B;
}
";
        var src2 = @"
class C(int A, int B)
{
    public int D { get; init; }

    public int F() => B;
}
";
        var edits = GetTopEdits(src1, src2);

        // TODO: https://github.com/dotnet/roslyn/issues/69317
        // Update D getter/setter to use deleted B property

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_D")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_D")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69317")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69216")]
    public void Property_Rename_ShadowingPrimaryParameter_WithInitializer()
    {
        var src1 = @"
class C(int A, int B)
{
    public int B { get; init; } = B;

    public int F() => B;
}
";
        var src2 = @"
class C(int A, int B)
{
    public int D { get; init; } = B;

    public int F() => B;
}
";
        var edits = GetTopEdits(src1, src2);

        // TODO: https://github.com/dotnet/roslyn/issues/69317
        // Update D getter/setter to use deleted B property

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetPrimaryConstructor("C"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_B"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_D")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_D")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Property_Partial_DeleteInsert_DefinitionPart()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";
        var srcC1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { partial int P => 1; }";
        var srcC2 = "partial class C { partial int P { get; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")]),
            ]);
    }

    [Fact]
    public void Property_Partial_DeleteInsert_ImplementationPart()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";
        var srcC1 = "partial class C { }";

        var srcA2 = "partial class C { partial int P { get; } }";
        var srcB2 = "partial class C { }";
        var srcC2 = "partial class C { partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")]),
            ]);
    }

    [Fact]
    public void Property_Partial_Swap_ImplementationAndDefinitionParts()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";

        var srcA2 = "partial class C { partial int P => 1; }";
        var srcB2 = "partial class C { partial int P { get; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Property_Partial_DeleteBoth()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C")
                    ]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C")
                    ]),
            ]);
    }

    [Fact]
    public void Property_Partial_DeleteInsertBoth()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";
        var srcC1 = "partial class C { }";
        var srcD1 = "partial class C { }";

        var srcA2 = "partial class C { }";
        var srcB2 = "partial class C { }";
        var srcC2 = "partial class C { partial int P { get; } }";
        var srcD2 = "partial class C { partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2)],
            [
                DocumentResults(),
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")])
            ]);
    }

    [Fact]
    public void Property_Partial_Insert()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = "partial class C { partial int P { get; } }";
        var srcB2 = "partial class C { partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart)]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Property_Partial_Insert_Reloadable()
    {
        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { }";
        var srcB1 = "partial class C { }";

        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { partial int P { get; } }";
        var srcB2 = "partial class C { partial int P { get => 1; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")]),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void Property_Partial_Update_Attribute_Definition()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { [A(1)]partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";

        var srcA2 = attribute +
                    "partial class C { [A(2)]partial int P { get; } }";
        var srcB2 = "partial class C { partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C")]),
                DocumentResults(),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Property_Partial_Update_Attribute_Implementation()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { [A(1)]partial int P => 1; }";

        var srcA2 = attribute +
                    "partial class C { partial int P { get; } }";
        var srcB2 = "partial class C { [A(2)]partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C"),

                        // Updating the accessor is superfluous.
                        // It is added since we see an update to an expression bodied property. We don't distinguish between update to the body and update to an attribute.
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Property_Partial_Update_Attribute_DefinitionAndImplementation()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var srcA1 = attribute +
                    "partial class C { [A(1)]partial int P { get; } }";
        var srcB1 = "partial class C { [A(1)]partial int P => 1; }";

        var srcA2 = attribute +
                    "partial class C { [A(2)]partial int P { get; } }";
        var srcB2 = "partial class C { [A(2)]partial int P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C")]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Property_Partial_DeleteInsert_DefinitionWithAttributeChange()
    {
        var attribute = """
            public class A : System.Attribute {}
            """;

        var srcA1 = attribute +
                    "partial class C { [A]partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";

        var srcA2 = attribute +
                    "partial class C { }";
        var srcB2 = "partial class C { partial int P => 1; partial int P { get; } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),

                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C")
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Property_Partial_Parameter_TypeChange()
    {
        var srcA1 = "partial class C { partial int P { get; } }";
        var srcB1 = "partial class C { partial int P => 1; }";

        var srcA2 = "partial class C { partial long P { get; } }";
        var srcB2 = "partial class C { partial long P => 1; }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C"),
                    ]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.get_P").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.P").PartialImplementationPart, partialType: "C"),
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "partial long P", GetResource("property"))]),
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "partial long P", GetResource("property"))]),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    #endregion

    #region Indexers

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Indexer_Update_Modifiers_Accessibility_ExpressionBody_Significant(string accessibility)
    {
        var src1 = "class C { " + accessibility + " int this[int index] => 1; }";
        var src2 = "class C { int this[int index] => 1; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
            ]);
    }

    [Fact]
    public void Indexer_Update_Modifiers_Accessibility_ExpressionBody_Insignificant()
    {
        var src1 = "class C { private int this[int index] => 1; }";
        var src2 = "class C { int this[int index] => 1; }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
            ]);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Indexer_Update_Modifiers_Accessibility_ReadOnly_Significant(string accessibility)
    {
        var src1 = "class C { " + accessibility + " int this[int index] { get; } }";
        var src2 = "class C { int this[int index] { get; } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
            ]);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Indexer_Update_Modifiers_Accessibility_Writable_Significant(string accessibility)
    {
        var src1 = "class C { " + accessibility + " int this[int index] { get; set; } }";
        var src2 = "class C { int this[int index] { get; set; } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"))
            ]);
    }

    [Theory]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("sealed override", "override")]
    public void Indexer_Update_Modifiers(string oldModifiers, string newModifiers = "")
    {
        if (oldModifiers != "")
        {
            oldModifiers += " ";
        }

        if (newModifiers != "")
        {
            newModifiers += " ";
        }

        var src1 = "class C { " + oldModifiers + "int this[int a] => 0; }";
        var src2 = "class C { " + newModifiers + "int this[int a] => 0; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [" + oldModifiers + "int this[int a] => 0;]@10 -> [" + newModifiers + "int this[int a] => 0;]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int this[int a]", FeaturesResources.indexer_));
    }

    [Fact]
    public void Indexer_Getter()
    {
        var src1 = "class C { int this[int a] { get { return 1; } } }";
        var src2 = "class C { int this[int a] { get { return 2; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [get { return 1; }]@28 -> [get { return 2; }]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void Indexer_SetterUpdate()
    {
        var src1 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value); } } }";
        var src2 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value + 1); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [set { System.Console.WriteLine(value); }]@46 -> [set { System.Console.WriteLine(value + 1); }]@46");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void Indexer_InitUpdate()
    {
        var src1 = "class C { int this[int a] { get { return 1; } init { System.Console.WriteLine(value); } } }";
        var src2 = "class C { int this[int a] { get { return 1; } init { System.Console.WriteLine(value + 1); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [init { System.Console.WriteLine(value); }]@46 -> [init { System.Console.WriteLine(value + 1); }]@46");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
        ]);
    }

    [Fact]
    public void IndexerWithExpressionBody_Update()
    {
        var src1 = "class C { int this[int a] => 1; }";
        var src2 = "class C { int this[int a] => 2; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] => 1;]@10 -> [int this[int a] => 2;]@10",
            "Update [=> 1]@26 -> [=> 2]@26");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_ExpressionBodyToBlockBody()
    {
        var src1 = "class C { int this[int a] => 1; }";
        var src2 = "class C { int this[int a] { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } }]@10",
            "Insert [{ get { return 1; } }]@26",
            "Insert [get { return 1; }]@28",
            "Delete [=> 1]@26");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_BlockBodyToExpressionBody()
    {
        var src1 = "class C { int this[int a] { get { return 1; } } }";
        var src2 = "class C { int this[int a] => 1; } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] { get { return 1; } }]@10 -> [int this[int a] => 1;]@10",
            "Insert [=> 1]@26",
            "Delete [{ get { return 1; } }]@26",
            "Delete [get { return 1; }]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_GetterExpressionBodyToBlockBody()
    {
        var src1 = "class C { int this[int a] { get => 1; } }";
        var src2 = "class C { int this[int a] { get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_BlockBodyToGetterExpressionBody()
    {
        var src1 = "class C { int this[int a] { get { return 1; } } }";
        var src2 = "class C { int this[int a] { get => 1; } }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");
        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_GetterExpressionBodyToExpressionBody()
    {
        var src1 = "class C { int this[int a] { get => 1; } }";
        var src2 = "class C { int this[int a] => 1; } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] { get => 1; }]@10 -> [int this[int a] => 1;]@10",
            "Insert [=> 1]@26",
            "Delete [{ get => 1; }]@26",
            "Delete [get => 1;]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_ExpressionBodyToGetterExpressionBody()
    {
        var src1 = "class C { int this[int a] => 1; }";
        var src2 = "class C { int this[int a] { get => 1; } }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] => 1;]@10 -> [int this[int a] { get => 1; }]@10",
            "Insert [{ get => 1; }]@26",
            "Insert [get => 1;]@28",
            "Delete [=> 1]@26");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"))
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_GetterBlockBodyToGetterExpressionBody()
    {
        var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
        var src2 = "class C { int this[int a] { get => 1;         set { Console.WriteLine(0); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_SetterBlockBodyToSetterExpressionBody()
    {
        var src1 = "class C { int this[int a] { set { } } void F() { } }";
        var src2 = "class C { int this[int a] { set => F(); } void F() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [set { }]@28 -> [set => F();]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_InitBlockBodyToInitExpressionBody()
    {
        var src1 = "class C { int this[int a] { init { } } void F() { } }";
        var src2 = "class C { int this[int a] { init => F(); } void F() { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [init { }]@28 -> [init => F();]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_GetterExpressionBodyToGetterBlockBody()
    {
        var src1 = "class C { int this[int a] { get => 1; set { Console.WriteLine(0); } } }";
        var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

        edits.VerifySemantics(ActiveStatementsDescription.Empty,
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
        ]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_GetterAndSetterBlockBodiesToExpressionBody()
    {
        var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
        var src2 = "class C { int this[int a] => 1; }";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10 -> [int this[int a] => 1;]@10",
            "Insert [=> 1]@26",
            "Delete [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
            "Delete [get { return 1; }]@28",
            "Delete [set { Console.WriteLine(0); }]@46");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Indexer_ExpressionBodyToGetterAndSetterBlockBodies()
    {
        var src1 = "class C { int this[int a] => 1; }";
        var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10",
            "Insert [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
            "Insert [get { return 1; }]@28",
            "Insert [set { Console.WriteLine(0); }]@46",
            "Delete [=> 1]@26");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Rename()
    {
        var interfaces = """
            interface I
            {
                int this[int a] { get; }
            }
            
            interface J
            {
                int this[int a] { get; }
            }
            """;
        var src1 = "class C { int I.this[int a] { get { return 1; } } } " + interfaces;
        var src2 = "class C { int J.this[int a] { get { return 1; } } } " + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "int J.this[int a]", GetResource("indexer", "I.this[int a]")));
    }

    [Fact]
    public void Indexer_Rename_ExpressionBody()
    {
        var interfaces = """
            interface I
            {
                int this[int a] { get; }
            }
            
            interface J
            {
                int this[int a] { get; }
            }
            """;
        var src1 = "class C { int I.this[int a] => 1; } " + interfaces;
        var src2 = "class C { int J.this[int a] => 1; } " + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "int J.this[int a]", GetResource("indexer", "I.this[int a]")));
    }

    [Fact]
    public void Indexer_Rename_Stackalloc()
    {
        var interfaces = """
            interface I
            {
                int this[int a] { get; }
            }
            
            interface J
            {
                int this[int a] { get; }
            }
            """;

        // only type is changed, no changes to the accessors (not even whitespace)
        var src1 = "class C { byte G(Span<char> s) => 0; byte I.this[int a] { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }" + interfaces;
        var src2 = "class C { byte G(Span<char> s) => 0; long J.this[int a] { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }" + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer setter"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Indexer_Rename_Stackalloc_ExpressionBody()
    {
        var interfaces = """
            interface I
            {
                int this[int a] { get; }
            }
            
            interface J
            {
                int this[int a] { get; }
            }
            """;

        // only type is changed, no changes to the body (not even whitespace)
        var src1 = "class C { byte G(Span<char> s) => 0; byte I.this[int a] => G(stackalloc int[1]); } " + interfaces;
        var src2 = "class C { byte G(Span<char> s) => 0; long J.this[int a] => G(stackalloc int[1]); } " + interfaces;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Indexer_Reorder1()
    {
        var src1 = "class C { int this[int a] { get { return 1; } } int this[string a] { get { return 1; } }  }";
        var src2 = "class C { int this[string a] { get { return 1; } } int this[int a] { get { return 1; } }  }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [int this[string a] { get { return 1; } }]@48 -> @10");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Indexer_AccessorReorder()
    {
        var src1 = "class C { int this[int a] { get { return 1; } set { } } }";
        var src2 = "class C { int this[int a] { set { } get { return 1; } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [set { }]@46 -> @28");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Indexer_Update_Attribute()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var src1 = attribute + "class C { [A(1)]int this[int a] { get; set; } }";
        var src2 = attribute + "class C { [A(2)]int this[int a] { get; set; } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int this[int a]", GetResource("indexer"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Update_Type()
    {
        var src1 = "class C { byte this[int a] { get => 1; set {} } }";
        var src2 = "class C { long this[int a] { get => 1; set {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "long this[int a]", CSharpFeaturesResources.indexer)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Update_Type_WithExpressionBody()
    {
        var src1 = "class C { byte this[int a] => 1; }";
        var src2 = "class C { long this[int a] => 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "long this[int a]", CSharpFeaturesResources.indexer)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Update_Type_Stackalloc()
    {
        // only type is changed, no changes to the accessors (not even whitespace)
        var src1 = "class C { byte G(Span<char> s) => 0; byte this[int x] { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";
        var src2 = "class C { byte G(Span<char> s) => 0; long this[int x] { get => G(stackalloc int[1]); set => G(stackalloc int[1]); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer setter"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Indexer_Update_Type_Stackalloc_WithExpressionBody()
    {
        // only type is changed, no changes to the body (not even whitespace)
        var src1 = "class C { byte G(Span<char> s) => 0; byte this[int x] => G(stackalloc int[1]); }";
        var src2 = "class C { byte G(Span<char> s) => 0; long this[int x] => G(stackalloc int[1]); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Indexer_Parameter_TypeChange()
    {
        var src1 = "class C { int this[byte a] { get => 1; set { } } }";
        var src2 = "class C { int this[long a] { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Partial_Parameter_TypeChange()
    {
        var srcA1 = "partial class C { partial int this[long x] { get; set; } }";
        var srcB1 = "partial class C { partial int this[long x] { get => 1; set { } } }";

        var srcA2 = "partial class C { partial int this[byte x] { get; set; } }";
        var srcB2 = "partial class C { partial int this[byte x] { get => 1; set { } } }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, partialType: "C"),
                    ]),
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, partialType: "C"),
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, partialType: "C"),
                    ]),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "byte x", GetResource("indexer"))]),
                DocumentResults(
                    diagnostics: [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "byte x", GetResource("indexer"))]),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Parameter_Rename()
    {
        var src1 = "class C { int this[int a] { get => 1; set { } } }";
        var src2 = "class C { int this[int b] { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]")),
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", GetResource("parameter"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Parameter_Update_Attribute()
    {
        var attribute = """
            public class A : System.Attribute { public A(int x) {} }
            """;

        var src1 = attribute + "class C { int this[[A(1)]int a] { get => 1; set { } } }";
        var src2 = attribute + "class C { int this[[A(2)]int a] { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.this[]"))
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", GetResource("parameter"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Parameter_Insert()
    {
        var src1 = "class C { int this[int a] { get => 1; set { } } }";
        var src2 = "class C { int this[int a, string b] { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Parameter_Insert_Partial()
    {
        var src1 = """
            class C
            {
                partial int this[int a] { get; set; }
                partial int this[int a] { get => 1; set { } }
            }
            """;

        var src2 = """
            class C
            {
                partial int this[int a, int/*1*/b, int c] { get; set; }
                partial int this[int a, int/*2*/b, int c] { get => 1; set { } }
            }
            """;

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"), partialType: "C"),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IPropertySymbol>("C.this[]").PartialImplementationPart, partialType: "C"),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.get_Item").PartialImplementationPart, partialType: "C"),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<IMethodSymbol>("C.set_Item").PartialImplementationPart, partialType: "C"),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int/*1*/b", GetResource("indexer")),
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "int/*2*/b", GetResource("indexer"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Parameter_Delete()
    {
        var src1 = "class C { int this[int a, string b] { get => 1; set { } } }";
        var src2 = "class C { int this[int a] { get => 1; set { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Parameter_Reorder_Stackalloc()
    {
        var src1 = @"
using System;

class C
{
    int this[int a, byte b] { get { return stackalloc int[1].Length; } }
}
";
        var src2 = @"
using System;

class C
{
    int this[byte b, int a] { get { return stackalloc int[1].Length; } }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Parameter_Reorder_Stackalloc_WithGetter_WithExpressionBody()
    {
        var src1 = @"
using System;

class C
{
    int this[int a, byte b] { get => stackalloc int[1].Length; }
}
";
        var src2 = @"
using System;

class C
{
    int this[byte b, int a] { get => stackalloc int[1].Length; }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Parameter_Reorder_Stackalloc_WithExpressionBody()
    {
        var src1 = @"
using System;

class C
{
    int this[int a, byte b] => stackalloc int[1].Length;
}
";
        var src2 = @"
using System;

class C
{
    int this[byte b, int a] => stackalloc int[1].Length;
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("indexer getter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_AddSetAccessor()
    {
        var src1 = @"
class C
{
    public int this[int i] { get { return default; } }
}";
        var src2 = @"
class C
{
    public int this[int i] { get { return default; } set { } }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [set { }]@67");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").SetMethod)],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_Delete()
    {
        var src1 = @"
class C<T>
{
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
        var src2 = @"
class C<T>
{
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750109")]
    public void Indexer_DeleteGetAccessor()
    {
        var src1 = @"
class C<T>
{
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
        var src2 = @"
class C<T>
{
    public T this[int i]
    {
        set { arr[i] = value; }
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_DeleteSetAccessor()
    {
        var src1 = @"
class C
{
    public int this[int i] { get { return 0; } set { } }
}";
        var src2 = @"
class C
{
    public int this[int i] { get { return 0; } }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174850")]
    public void Indexer_Insert()
    {
        var src1 = "struct C { }";
        var src2 = "struct C { public int this[int x, int y] { get { return x + y; } } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_ReadOnlyRef_Parameter_InsertWhole()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { int this[in int i] => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [int this[in int i] => throw null;]@13",
            "Insert [[in int i]]@21",
            "Insert [=> throw null]@32",
            "Insert [in int i]@22");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_ReadOnlyRef_Parameter_Update()
    {
        var src1 = "class C { int this[int i] => throw null; }";
        var src2 = "class C { int this[in int i] => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int i]@19 -> [in int i]@19");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_ReadOnlyRef_ReturnType_Insert()
    {
        var src1 = "class Test { }";
        var src2 = "class Test { ref readonly int this[int i] => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [ref readonly int this[int i] => throw null;]@13",
            "Insert [[int i]]@34",
            "Insert [=> throw null]@42",
            "Insert [int i]@35");

        edits.VerifySemanticDiagnostics(
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Indexer_ReadOnlyRef_ReturnType_Update()
    {
        var src1 = "class C { int this[int i] => throw null; }";
        var src2 = "class C { ref readonly int this[int i] => throw null; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int this[int i] => throw null;]@10 -> [ref readonly int this[int i] => throw null;]@10");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.this[]"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.get_Item")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "ref readonly int this[int i]", FeaturesResources.indexer_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int this[int x] { get => 1; set { } } }";

        var srcA2 = "partial class C { int this[int x] { get => 1; set { } } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").SetMethod)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void IndexerInit_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int this[int x] { get => 1; init { } }}";

        var srcA2 = "partial class C { int this[int x] { get => 1; init { } }}";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").SetMethod)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void AutoIndexer_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { int this[int x] { get; set; } }";

        var srcA2 = "partial class C { int this[int x] { get; set; } }";
        var srcB2 = "partial class C { }";

        // Accessors need to be updated even though they do not have an explicit body. 
        // There is still a sequence point generated for them whose location needs to be updated.
        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]")),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.this[]").SetMethod),
                    ]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void AutoIndexer_ReadOnly_Add()
    {
        var src1 = @"
struct S
{
    int this[int x] { get; }
}";
        var src2 = @"
struct S
{
    readonly int this[int x] { get; }
}";
        var edits = GetTopEdits(src1, src2);

        // Compiler generated attribute changed, we do not require runtime capability for custom attribute changes.
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("S.this[]")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("S.this[]").GetMethod)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Indexer_InMutableStruct_ReadOnly_Add()
    {
        var src1 = @"
struct S
{
     int this[int x] { get => 1; }
     int this[uint x] { get => 1; set {}}
     int this[byte x] { get => 1; set {}}
     int this[sbyte x] { get => 1; set {}}
}";
        var src2 = @"
struct S
{
     readonly int this[int x] { get => 1; }
     int this[uint x] { readonly get => 1; set {}}
     int this[byte x] { get => 1; readonly set {}}
     readonly int this[sbyte x] { get => 1; set {}}
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32 }])),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_SByte }])),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32 }]).GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_SByte }]).GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_SByte }]).SetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_UInt32 }]).GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }]).SetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_UInt32 }]).SetMethod));
    }

    [Fact]
    public void Indexer_InReadOnlyStruct_ReadOnly_Add()
    {
        var src1 = @"
readonly struct S
{
              int this[int x] { get => 1; }
     int this[uint x] {          get => 1; set {}}
     int this[byte x] { get => 1;          set {}}
              int this[sbyte x] { get => 1; set {}}
}";
        var src2 = @"
readonly struct S
{
     readonly int this[int x] { get => 1; }
     int this[uint x] { readonly get => 1; set {}}
     int this[byte x] { get => 1; readonly set {}}
     readonly int this[sbyte x] { get => 1; set {}}
}";
        var edits = GetTopEdits(src1, src2);

        // Updates only for accessors whose modifiers were explicitly updated.
        // Indexers themselves are only updated when their modifiers change. The update is not necessary and could be eliminated.
        edits.VerifySemantics(
        [
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_UInt32 }]).GetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Byte }]).SetMethod),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32 }])),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMembers<IPropertySymbol>("S.this[]").Single(m => m.Parameters is [{ Type.SpecialType: SpecialType.System_SByte }])),
        ]);
    }

    #endregion

    #region Events

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("private protected")]
    [InlineData("internal protected")]
    public void Event_Update_Modifiers_Accessibility(string accessibility)
    {
        var src1 = $$"""
            using System;
            class C
            {
                {{accessibility}}
                event Action E { add {} remove {} }
            }
            """;

        var src2 = """
            using System;
            class C
            {

                event Action E { add {} remove {} }
            }
            """;

        // update of the event itself is not necessary and could be eliminated:
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.E")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.add_E")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.remove_E"))
            ]);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("override")]
    [InlineData("sealed override", "override")]
    public void Event_Update_Modifiers(string oldModifiers, string newModifiers = "")
    {
        if (oldModifiers != "")
        {
            oldModifiers += " ";
        }

        if (newModifiers != "")
        {
            newModifiers += " ";
        }

        var src1 = "class C { " + oldModifiers + "event Action F { add {} remove {} } }";
        var src2 = "class C { " + newModifiers + "event Action F { add {} remove {} } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [" + oldModifiers + "event Action F { add {} remove {} }]@10 -> [" + newModifiers + "event Action F { add {} remove {} }]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "event Action F", FeaturesResources.event_));
    }

    [Fact]
    public void Event_Accessor_Reorder1()
    {
        var src1 = "class C { event int E { add { } remove { } } }";
        var src2 = "class C { event int E { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [remove { }]@32 -> @24");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Event_Accessor_Reorder2()
    {
        var src1 = "class C { event int E1 { add { } remove { } }    event int E1 { add { } remove { } } }";
        var src2 = "class C { event int E2 { remove { } add { } }    event int E2 { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [event int E1 { add { } remove { } }]@10 -> [event int E2 { remove { } add { } }]@10",
            "Update [event int E1 { add { } remove { } }]@49 -> [event int E2 { remove { } add { } }]@49",
            "Reorder [remove { }]@33 -> @25",
            "Reorder [remove { }]@72 -> @64");
    }

    [Fact]
    public void Event_Accessor_Reorder3()
    {
        var src1 = "class C { event int E1 { add { } remove { } }    event int E2 { add { } remove { } } }";
        var src2 = "class C { event int E2 { remove { } add { } }    event int E1 { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [event int E2 { add { } remove { } }]@49 -> @10",
            "Reorder [remove { }]@72 -> @25",
            "Reorder [remove { }]@33 -> @64");
    }

    [Fact]
    public void Event_Insert()
    {
        var src1 = "class C { }";
        var src2 = "class C { event int E { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("E"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Event_Delete_CustomAccessors()
    {
        var src1 = "class C { event int E { remove { } add { } } }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Event_Delete_SynthesizedAccessors()
    {
        var src1 = "class C { event System.Action E; }";
        var src2 = "class C { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.Delete, "class C", "event field 'E'")],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Event_Insert_TypeLayout_CustomAccessors()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
}
";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private event Action c { add { } remove { } } 
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Event_Insert_TypeLayout_SynthesizedAccessors()
    {
        var src1 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
}
";
        var src2 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private event Action c;
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", GetResource("event field"), GetResource("class"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Event_ExpressionBodyToBlockBody()
    {
        var src1 = @"
using System;
public class C
{
    event Action E { add => F(); remove => F(); }
}
";
        var src2 = @"
using System;
public class C
{
   event Action E { add { F(); } remove { } }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [add => F();]@57 -> [add { F(); }]@56",
            "Update [remove => F();]@69 -> [remove { }]@69"
            );

        edits.VerifySemanticDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
    public void Event_BlockBodyToExpressionBody()
    {
        var src1 = @"
using System;
public class C
{
   event Action E { add { F(); } remove { } }
}
";
        var src2 = @"
using System;
public class C
{
    event Action E { add => F(); remove => F(); }
}
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [add { F(); }]@56 -> [add => F();]@57",
            "Update [remove { }]@69 -> [remove => F();]@69"
            );

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Event_Partial_InsertDelete()
    {
        var srcA1 = "partial class C { }";
        var srcB1 = "partial class C { event int E { add { } remove { } } }";

        var srcA2 = "partial class C { event int E { add { } remove { } } }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").AddMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").RemoveMethod)
                    ]),

                DocumentResults(),
            ]);
    }

    [Fact]
    public void Event_InMutableStruct_ReadOnly_Add()
    {
        var src1 = @"
struct S
{
    public event Action E
    {
        add {} remove {}
    }
}";
        var src2 = @"
struct S
{
    public readonly event Action E
    {
        add {} remove {}
    }
}";
        var edits = GetTopEdits(src1, src2);
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.E")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.add_E")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("S.remove_E")));
    }

    [Fact]
    public void Event_InReadOnlyStruct_ReadOnly_Add1()
    {
        var src1 = @"
readonly struct S
{
    public event Action E
    {
        add {} remove {}
    }
}";
        var src2 = @"
readonly struct S
{
    public readonly event Action E
    {
        add {} remove {}
    }
}";
        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics();
    }

    [Fact]
    public void Event_Attribute_Add_SynthesizedAccessors()
    {
        var src1 = @"
class C
{
    event Action F;
}";
        var src2 = @"
class C
{
    [System.Obsolete]event Action F;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [event Action F;]@18 -> [[System.Obsolete]event Action F;]@18");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", GetResource("event field"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
           [
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F"))
           ],
           capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Event_Attribute_Add_CustomAccessors()
    {
        var src1 = @"
class C
{
    event Action F { add {} remove {} }
}";
        var src2 = @"
class C
{
    [System.Obsolete]event Action F { add {} remove {} }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [event Action F { add {} remove {} }]@18 -> [[System.Obsolete]event Action F { add {} remove {} }]@18");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
           [
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F")),
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").AddMethod),
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
           ],
           capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Event_Accessor_Attribute_Add()
    {
        var src1 = @"
class C
{
    event Action F { add {} remove {} }
}";
        var src2 = @"
class C
{
    event Action F { add {} [System.Obsolete]remove {} }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [remove {}]@42 -> [[System.Obsolete]remove {}]@42");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "remove", FeaturesResources.event_accessor)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Field_Event_Attribute_Delete()
    {
        var src1 = @"
class C
{
    [System.Obsolete]event Action F;
}";
        var src2 = @"
class C
{
    event Action F;
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete]event Action F;]@18 -> [event Action F;]@18");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", GetResource("event field"))],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Event_Attribute_Delete()
    {
        var src1 = @"
class C
{
    [System.Obsolete]event Action F { add {} remove {} }
}";
        var src2 = @"
class C
{
    event Action F { add {} remove {} }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete]event Action F { add {} remove {} }]@18 -> [event Action F { add {} remove {} }]@18");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
           [
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F")),
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").AddMethod),
               SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
           ],
           capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Event_Accessor_Attribute_Delete()
    {
        var src1 = @"
class C
{
    event Action F { add {} [System.Obsolete]remove {} }
}";
        var src2 = @"
class C
{
    event Action F { add {} remove {} }
}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete]remove {}]@42 -> [remove {}]@42");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "remove", FeaturesResources.event_accessor)],
            capabilities: EditAndContinueCapabilities.Baseline);

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
           ],
           capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Event_Rename_Stackalloc()
    {
        // only name is changed, no changes to the accessors (not even whitespace)
        var src1 = "class C { void G(Span<char> s) {} event System.Action E { add => G(stackalloc int[1]); remove => G(stackalloc int[1]); } }";
        var src2 = "class C { void G(Span<char> s) {} event System.Action F { add => G(stackalloc int[1]); remove => G(stackalloc int[1]); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("event accessor")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("event accessor")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Event_Update_Type_Stackalloc()
    {
        // only type is changed, no changes to the accessors (not even whitespace)
        var src1 = "class C { void G(Span<char> s) {} event System.Action<byte> E { add => G(stackalloc int[1]); remove => G(stackalloc int[1]); } }";
        var src2 = "class C { void G(Span<char> s) {} event System.Action<long> E { add => G(stackalloc int[1]); remove => G(stackalloc int[1]); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("event accessor")),
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[1]", GetResource("event accessor"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
    }

    [Fact]
    public void Event_Rename_CustomAcccessors()
    {
        var src1 = "class C { event System.Action E { remove { } add { } } }";
        var src2 = "class C { event System.Action F { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.add_F")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.remove_F")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "event System.Action F", FeaturesResources.event_)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Event_Rename_SynthesizedAccessors()
    {
        var src1 = "class C { event System.Action E; }";
        var src2 = "class C { event System.Action F; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.Renamed, "F", GetResource("event field", "E"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Event_Update_Type_CustomAcccessors()
    {
        var src1 = "class C { event System.Action<long> E { remove { } add { } } }";
        var src2 = "class C { event System.Action<byte> E { remove { } add { } } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.E")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.add_E")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.remove_E")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "event System.Action<byte> E", GetResource("event"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Event_Update_Type_SynthesizedAcccessors_RuntimeTypeChanged()
    {
        var src1 = "class C { event System.Action<long> E; }";
        var src2 = "class C { event System.Action<byte> E; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.TypeUpdate, "event System.Action<byte> E", GetResource("event field"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("object", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Event_Update_Type_SynthesizedAcccessors_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "class C { event System.Action<" + oldType + "> F, G; }";
        var src2 = "class C { event System.Action<" + newType + "> F, G; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G")));
    }

    [Fact]
    public void Event_Reorder_SynthesizedAccessors()
    {
        var src1 = "class C { int a = 0; int b = 1; event System.Action c = 2; }";
        var src2 = "class C { event System.Action c = 2; int a = 0; int b = 1; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [event System.Action c = 2;]@32 -> @10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "event System.Action c = 2", CSharpFeaturesResources.event_field));
    }

    [Fact]
    public void Event_Partial_InsertDelete_SynthesizedAccessors()
    {
        var srcA1 = "partial class C { static void F() {} }";
        var srcB1 = "partial class C { event System.Action E = F; }";

        var srcA2 = "partial class C { static void F() {} event System.Action E = F; }";
        var srcB2 = "partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    semanticEdits:
                    [
                        SemanticEdit(SemanticEditKind.Update, c => c.GetParameterlessConstructor("C"), partialType: "C", preserveLocalVariables: true)
                    ]),

                DocumentResults(),
            ]);
    }

    #endregion

    #region Parameter

    [Fact]
    public void Parameter_Rename_Method()
    {
        var src1 = @"class C { public void M(int a) { } }";
        var src2 = @"class C { public void M(int b) { } }";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [int a]@24 -> [int b]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Rename_Constructor()
    {
        var src1 = @"class C { public C(int a) {} }";
        var src2 = @"class C { public C(int b) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [int a]@19 -> [int b]@19");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Rename_Operator1()
    {
        var src1 = @"class C { public static implicit operator int(C a) {} }";
        var src2 = @"class C { public static implicit operator int(C b) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [C a]@46 -> [C b]@46");
    }

    [Fact]
    public void Parameter_Rename_Operator2()
    {
        var src1 = @"class C { public static int operator +(C a, C b) { return 0; } }";
        var src2 = @"class C { public static int operator +(C a, C x) { return 0; } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [C b]@44 -> [C x]@44");
    }

    [Fact]
    public void Parameter_Insert()
    {
        var src1 = @"class C { public void M() {} }";
        var src2 = @"class C { public void M(int a) { a.ToString(); } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M() {}]@10 -> [public void M(int a) { a.ToString(); }]@10",
            "Insert [int a]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Parameter_Insert_Ref()
    {
        var src1 = @"class C { public void M(int a) {} }";
        var src2 = @"class C { public void M(int a, ref int b) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [(int a)]@23 -> [(int a, ref int b)]@23",
            "Insert [ref int b]@31");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Parameter_Insert_WithUpdatedReturnType()
    {
        var src1 = @"class C { public void M() {} }";
        var src2 = @"class C { public int M(int a) { return a; } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M() {}]@10 -> [public int M(int a) { return a; }]@10",
            "Insert [int a]@23");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Parameter_Delete1()
    {
        var src1 = @"class C { public void M(int a) {} }";
        var src2 = @"class C { public void M() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Delete [int a]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Parameter_Delete2()
    {
        var src1 = @"class C { public void M(int a, int b) {} }";
        var src2 = @"class C { public void M(int b) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [(int a, int b)]@23 -> [(int b)]@23",
            "Delete [int a]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
    }

    [Fact]
    public void Parameter_Reorder()
    {
        var src1 = @"class C { public void M(int a, int b) {} }";
        var src2 = @"class C { public void M(int b, int a) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Reorder [int b]@31 -> @24");

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
           ],
           capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact, WorkItem(67268, "https://github.com/dotnet/roslyn/issues/67268")]
    public void Parameter_Reorder_Constructor()
    {
        var src1 = @"class C { C(int a, int b) {} }";
        var src2 = @"class C { C(int b, int a) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Reorder [int b]@19 -> @12");

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
           ],
           capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Reorder_UpdateBody()
    {
        var src1 = @"class C { public void M(int a, int b) { a.ToString(); } }";
        var src2 = @"class C { public void M(int b, int a) { b.ToString(); } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M(int a, int b) { a.ToString(); }]@10 -> [public void M(int b, int a) { b.ToString(); }]@10",
            "Reorder [int b]@31 -> @24");

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
           ],
           capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Reorder_DifferentTypes()
    {
        var src1 = @"class C { public void M(string a, int b) { a.ToString(); } }";
        var src2 = @"class C { public void M(int b, string a) { b.ToString(); } } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [public void M(string a, int b) { a.ToString(); }]@10 -> [public void M(int b, string a) { b.ToString(); }]@10",
            "Reorder [int b]@34 -> @24");

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
           ],
           capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "public void M(int b, string a)", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Reorder_Rename()
    {
        var src1 = @"class C { public void M(int a, int b) {} }";
        var src2 = @"class C { public void M(int b, int c) {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Reorder [int b]@31 -> @24",
            "Update [int a]@24 -> [int c]@31");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
            ],
            capabilities: EditAndContinueCapabilities.UpdateParameters);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter),
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int c", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Reorder_Rename_Generic()
    {
        var src1 = @"class C<T> { public void M(int a, int b) {} }";
        var src2 = @"class C<T> { public void M(int b, int c) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
            ],
            capabilities:
                EditAndContinueCapabilities.UpdateParameters |
                EditAndContinueCapabilities.GenericAddMethodToExistingType |
                EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "int b", GetResource("method")),
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", GetResource("parameter")),
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int c", GetResource("parameter"))
            ],
            capabilities: EditAndContinueCapabilities.GenericAddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter),
                Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int c", FeaturesResources.parameter)
            ],
            capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("in byte")]
    [InlineData("ref byte")]
    [InlineData("out byte")]
    [InlineData("ref readonly byte")]
    public void Parameter_Update_TypeOrRefKind_RuntimeTypeChanged(string oldType)
    {
        var src1 = "class C { int F(" + oldType + " a) => throw null!; }";
        var src2 = "class C { int F(byte a) => throw null!; }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
           [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F"))
           ],
           capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "byte a", GetResource("method"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Theory]
    [InlineData("int", "this int")]
    [InlineData("string[]", "params string[]")]
    [InlineData("string", "string?")]
    [InlineData("object", "dynamic")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void Parameter_Update_Type_RuntimeTypeUnchanged(string oldType, string newType)
    {
        var src1 = "static class C { static void M(" + oldType + " a) {} }";
        var src2 = "static class C { static void M(" + newType + " a) {} }";

        var edits = GetTopEdits(src1, src2);

        // We don't require a runtime capability to update attributes.
        // All runtimes support changing the attributes in metadata, some just don't reflect the changes in the Reflection model.
        // Having compiler-generated attributes visible via Reflaction API is not that important.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")));
    }

    [Fact]
    public void Parameter_Update_Type_Nullable()
    {
        var src1 = @"
#nullable enable
class C { static void M(string a) { } }
";
        var src2 = @"
#nullable disable
class C { static void M(string a) { } }
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics();
    }

    [Theory]
    [InlineData("this")]
    [InlineData("params")]
    public void Parameter_Modifier_Remove(string modifier)
    {
        var src1 = @"static class C { static void F(" + modifier + " int[] a) { } }";
        var src2 = @"static class C { static void F(int[] a) { } }";

        var edits = GetTopEdits(src1, src2);

        // We don't require a runtime capability to update attributes.
        // All runtimes support changing the attributes in metadata, some just don't reflect the changes in the Reflection model.
        // Having compiler-generated attributes visible via Reflaction API is not that important.
        edits.VerifySemantics(
            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")));
    }

    [Theory]
    [InlineData("int a = 1", "int a = 2")]
    [InlineData("int a = 1", "int a")]
    [InlineData("int a", "int a = 2")]
    [InlineData("object a = null", "object a")]
    [InlineData("object a", "object a = null")]
    [InlineData("double a = double.NaN", "double a = 1.2")]
    public void Parameter_Initializer_Update(string oldParameter, string newParameter)
    {
        var src1 = @"static class C { static void F(" + oldParameter + ") { } }";
        var src2 = @"static class C { static void F(" + newParameter + ") { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.InitializerUpdate, newParameter, FeaturesResources.parameter));
    }

    [Fact]
    public void Parameter_Initializer_NaN()
    {
        var src1 = @"static class C { static void F(double a = System.Double.NaN) { } }";
        var src2 = @"static class C { static void F(double a = double.NaN) { } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void Parameter_Initializer_InsertDeleteUpdate()
    {
        var srcA1 = @"partial class C { }";
        var srcB1 = @"partial class C { public static void F(int x = 1) {} }";

        var srcA2 = @"partial class C { public static void F(int x = 2) {} }";
        var srcB2 = @"partial class C { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(
                    diagnostics:
                    [
                        Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", FeaturesResources.parameter)
                    ]),
                DocumentResults(),
            ]);
    }

    [Fact]
    public void Parameter_Attribute_Insert()
    {
        var attribute = "public class A : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M(int a)    {} }";
        var src2 = attribute + @"class C { public void M([A]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@63 -> [[A]int a]@63");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Parameter_Attribute_Insert_SupportedByRuntime_SecurityAttribute1()
    {
        var attribute = "public class AAttribute : System.Security.Permissions.SecurityAttribute { }\n\n";

        var src1 = attribute + @"class C { public void M(int a) {} }";
        var src2 = attribute + @"class C { public void M([A]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@101 -> [[A]int a]@101");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "int a", "AAttribute", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Parameter_Attribute_Insert_SupportedByRuntime_SecurityAttribute2()
    {
        var attribute = "public class BAttribute : System.Security.Permissions.SecurityAttribute { }\n\n" +
                        "public class AAttribute : BAttribute { }\n\n";

        var src1 = attribute + @"class C { public void M(int a) {} }";
        var src2 = attribute + @"class C { public void M([A]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@143 -> [[A]int a]@143");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "int a", "AAttribute", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Parameter_Attribute_Insert_NotSupportedByRuntime1()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M(int a) {} }";
        var src2 = attribute + @"class C { public void M([A]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [int a]@72 -> [[A]int a]@72");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Attribute_Insert_NotSupportedByRuntime2()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M([A]int a) {} }";
        var src2 = attribute + @"class C { public void M([A, B]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]int a]@120 -> [[A, B]int a]@120");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Attribute_Delete_NotSupportedByRuntime()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M([A]int a) {} }";
        var src2 = attribute + @"class C { public void M(int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]int a]@72 -> [int a]@72");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Attribute_Update_NotSupportedByRuntime()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M([System.Obsolete(""1""), B]int a) {} }";
        var src2 = attribute + @"class C { public void M([System.Obsolete(""2""), A]int a) {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete(\"1\"), B]int a]@120 -> [[System.Obsolete(\"2\"), A]int a]@120");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter)],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void Parameter_Attribute_Update()
    {
        var attribute = "class A : System.Attribute { public A(int x) {} } ";

        var src1 = attribute + "class C { void F([A(0)]int a) {} }";
        var src2 = attribute + "class C { void F([A(1)]int a) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A(0)]int a]@67 -> [[A(1)]int a]@67");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void Parameter_Attribute_Update_WithBodyUpdate()
    {
        var attribute = "class A : System.Attribute { public A(int x) {} } ";

        var src1 = attribute + "class C { void F([A(0)]int a) { F(0); } }";
        var src2 = attribute + "class C { void F([A(1)]int a) { F(1); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [void F([A(0)]int a) { F(0); }]@60 -> [void F([A(1)]int a) { F(1); }]@60",
            "Update [[A(0)]int a]@67 -> [[A(1)]int a]@67");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    #endregion

    #region Method Type Parameter

    [Fact]
    public void MethodTypeParameterInsert1()
    {
        var src1 = @"class C { public void M() {} }";
        var src2 = @"class C { public void M<A>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Insert [<A>]@23",
            "Insert [A]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "A", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodTypeParameterInsert2()
    {
        var src1 = @"class C { public void M<A>() {} }";
        var src2 = @"class C { public void M<A,B>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [<A>]@23 -> [<A,B>]@23",
            "Insert [B]@26");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "B", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodTypeParameterDelete1()
    {
        var src1 = @"class C { public void M<A>() {} }";
        var src2 = @"class C { public void M() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Delete [<A>]@23",
            "Delete [A]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "public void M()", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodTypeParameterDelete2()
    {
        var src1 = @"class C { public void M<A,B>() {} }";
        var src2 = @"class C { public void M<B>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [<A,B>]@23 -> [<B>]@23",
            "Delete [A]@24");

        edits.VerifySemantics(
            [
                SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M")),
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.ChangingSignatureNotSupportedByRuntime, "public void M<B>()", GetResource("method"))],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodTypeParameterUpdate()
    {
        var src1 = @"class C { public void M<A>() {} }";
        var src2 = @"class C { public void M<B>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Update [A]@24 -> [B]@24");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.Renamed, "B", GetResource("type parameter", "A"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "B", GetResource("method")),
                Diagnostic(RudeEditKind.Renamed, "B", GetResource("type parameter", "A"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void MethodTypeParameterReorder()
    {
        var src1 = @"class C { public void M<A,B>() {} }";
        var src2 = @"class C { public void M<B,A>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Reorder [B]@26 -> @24");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.Move, "B", GetResource("type parameter"))],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameterReorderAndUpdate()
    {
        var src1 = @"class C { public void M<A,B>() {} }";
        var src2 = @"class C { public void M<B,C>() {} } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Reorder [B]@26 -> @24",
            "Update [A]@24 -> [C]@26");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.Move, "B", GetResource("type parameter")),
                Diagnostic(RudeEditKind.Renamed, "C", GetResource("type parameter", "A"))
            ],
            capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.GenericAddMethodToExistingType | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameter_Attribute_Insert1()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M<   T>() {} }";
        var src2 = attribute + @"class C { public void M<[A]T>() {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@75 -> [[A]T]@72");

        // Updating attributes of type parameters not supported:
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericMethodUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameter_Attribute_Insert2()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M<[A   ]T>() {} }";
        var src2 = attribute + @"class C { public void M<[A, B]T>() {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A   ]T]@120 -> [[A, B]T]@120");

        // Updating attributes of type parameters not supported:
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericMethodUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameter_Attribute_Delete()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C { public void M<[A]T>() {} }";
        var src2 = attribute + @"class C { public void M<   T>() {} } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]T]@72 -> [T]@75");

        // Updating attributes of type parameters not supported:
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericMethodUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameter_Attribute_Update()
    {
        var attribute = "class A : System.Attribute { public A(int x) {} } ";

        var src1 = attribute + "class C { void F<[A(0)]T>(T a) {} }";
        var src2 = attribute + "class C { void F<[A(1)]T>(T a) {} }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A(0)]T]@67 -> [[A(1)]T]@67");

        // Updating attributes of type parameters not supported:
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericMethodUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes | EditAndContinueCapabilities.GenericUpdateMethod);
    }

    [Fact]
    public void MethodTypeParameter_Attribute_Update_WithBodyUpdate()
    {
        var attribute = "class A : System.Attribute { public A(int x) {} } ";

        var src1 = attribute + "class C { void F<[A(0)]T>(T a) { F(0); } }";
        var src2 = attribute + "class C { void F<[A(1)]T>(T a) { F(1); } }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [void F<[A(0)]T>(T a) { F(0); }]@60 -> [void F<[A(1)]T>(T a) { F(1); }]@60",
            "Update [[A(0)]T]@67 -> [[A(1)]T]@67");

        // Updating attributes of type parameters not supported:
        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericMethodUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes | EditAndContinueCapabilities.GenericUpdateMethod);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "void F<[A(1)]T>(T a)", GetResource("method")),
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", GetResource("type parameter"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    #endregion

    #region Type Type Parameter

    [Fact]
    public void TypeTypeParameterInsert1()
    {
        var src1 = @"class C {}";
        var src2 = @"class C<A> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [<A>]@7",
            "Insert [A]@8");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "A", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeTypeParameterInsert2()
    {
        var src1 = @"class C<A> {}";
        var src2 = @"class C<A,B> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [<A>]@7 -> [<A,B>]@7",
            "Insert [B]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeTypeParameterDelete1()
    {
        var src1 = @"using System; class C<A> { }";
        var src2 = @"using System; class C { } ";

        var edits = GetTopEdits(src1, src2);
        edits.VerifyEdits(
            "Delete [<A>]@21",
            "Delete [A]@22");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C", GetResource("type parameter", "A")));
    }

    [Fact]
    public void TypeTypeParameterDelete2()
    {
        var src1 = @"class C<A,B> {}";
        var src2 = @"class C<B> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [<A,B>]@7 -> [<B>]@7",
            "Delete [A]@8");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C<B>", GetResource("type parameter", "A")));
    }

    [Fact]
    public void TypeTypeParameterUpdate()
    {
        var src1 = @"class C<A> {}";
        var src2 = @"class C<B> {} ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [A]@8 -> [B]@8");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Renamed, "B", GetResource("type parameter", "A")));
    }

    [Fact]
    public void TypeTypeParameterReorder()
    {
        var src1 = @"class C<A,B> { }";
        var src2 = @"class C<B,A> { } ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [B]@10 -> @8");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeTypeParameterReorderAndUpdate()
    {
        var src1 = @"class C<A,B> {}";
        var src2 = @"class C<B,C> {} ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [B]@10 -> @8",
            "Update [A]@8 -> [C]@10");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter),
            Diagnostic(RudeEditKind.Renamed, "C", GetResource("type parameter", "A")));
    }

    [Fact]
    public void TypeTypeParameterAttributeInsert1()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C<T> {}";
        var src2 = attribute + @"class C<[A]T> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@56 -> [[A]T]@56");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TypeTypeParameterAttributeInsert2()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C<[A]T> {}";
        var src2 = attribute + @"class C<[A, B]T> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]T]@104 -> [[A, B]T]@104");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TypeTypeParameterAttributeInsert_SupportedByRuntime()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C<T> {}";
        var src2 = attribute + @"class C<[A]T> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [T]@56 -> [[A]T]@56");

        edits.VerifySemanticDiagnostics(
            [Diagnostic(RudeEditKind.GenericTypeUpdate, "T")],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void TypeTypeParameterAttributeDelete()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C<[A]T> {}";
        var src2 = attribute + @"class C<T> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[A]T]@56 -> [T]@56");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TypeTypeParameterAttributeUpdate()
    {
        var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                        "public class BAttribute : System.Attribute { }\n\n";

        var src1 = attribute + @"class C<[System.Obsolete(""1""), B]T> {}";
        var src2 = attribute + @"class C<[System.Obsolete(""2""), A]T> {} ";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [[System.Obsolete(\"1\"), B]T]@104 -> [[System.Obsolete(\"2\"), A]T]@104");

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter)
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TypeTypeParameter_Partial_Attribute_AddMultiple()
    {
        var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

        var srcA1 = "partial class C<T> { }" + attributes;
        var srcB1 = "partial class C<T> { }";

        var srcA2 = "partial class C<[A]T> { }" + attributes;
        var srcB2 = "partial class C<[B]T> { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                ]),
                DocumentResults(diagnostics:
                [
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                ]),
            ],
            capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
    }

    [Fact]
    public void TypeTypeParameter_Partial_Attribute_AddMultiple_Reloadable()
    {
        var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

        var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C<T> { }" + attributes;
        var srcB1 = "partial class C<T> { }";

        var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C<[A]T> { }" + attributes;
        var srcB2 = "partial class C<[B]T> { }";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
                DocumentResults(semanticEdits:
                [
                    SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                ]),
            ],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    #endregion

    #region Type Parameter Constraints

    [Theory]
    [InlineData("nonnull")]
    [InlineData("struct")]
    [InlineData("class")]
    [InlineData("new()")]
    [InlineData("unmanaged")]
    [InlineData("System.IDisposable")]
    [InlineData("System.Delegate")]
    [InlineData("allows ref struct")]
    public void TypeConstraint_Insert(string newConstraint)
    {
        var src1 = "class C<S,T> { }";
        var src2 = "class C<S,T> where T : " + newConstraint + " { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [where T : " + newConstraint + "]@13");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingConstraints, "where T : " + newConstraint, FeaturesResources.type_parameter));
    }

    [Theory]
    [InlineData("nonnull")]
    [InlineData("struct")]
    [InlineData("class")]
    [InlineData("new()")]
    [InlineData("unmanaged")]
    [InlineData("System.IDisposable")]
    [InlineData("System.Delegate")]
    [InlineData("allows ref struct")]
    public void TypeConstraint_Delete(string oldConstraint)
    {
        var src1 = "class C<S,T> where T : " + oldConstraint + " { }";
        var src2 = "class C<S,T> { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [where T : " + oldConstraint + "]@13");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingConstraints, "T", FeaturesResources.type_parameter));
    }

    [Theory]
    [InlineData("string", "string?")]
    [InlineData("(int a, int b)", "(int a, int c)")]
    public void TypeConstraint_Update_RuntimeTypeUnchanged(string oldType, string newType)
    {
        // note: dynamic is not allowed in constraints
        var src1 = "class C<T> where T : System.Collections.Generic.List<" + oldType + "> {}";
        var src2 = "class C<T> where T : System.Collections.Generic.List<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            semanticEdits:
            [
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"))
            ]);
    }

    [Theory]
    [InlineData("int", "string")]
    [InlineData("int", "int?")]
    [InlineData("(int a, int b)", "(int a, double b)")]
    public void TypeConstraint_Update_RuntimeTypeChanged(string oldType, string newType)
    {
        var src1 = "class C<T> where T : System.Collections.Generic.List<" + oldType + "> {}";
        var src2 = "class C<T> where T : System.Collections.Generic.List<" + newType + "> {}";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingConstraints, "where T : System.Collections.Generic.List<" + newType + ">", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeConstraint_Delete_WithParameter()
    {
        var src1 = "class C<S,T> where S : new() where T : class  { }";
        var src2 = "class C<S> where S : new() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "class C<S>", GetResource("type parameter", "T")));
    }

    [Fact]
    public void TypeConstraint_MultipleClauses_Insert()
    {
        var src1 = "class C<S,T> where T : class { }";
        var src2 = "class C<S,T> where S : unmanaged where T : class { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Insert [where S : unmanaged]@13");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingConstraints, "where S : unmanaged", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeConstraint_MultipleClauses_Delete()
    {
        var src1 = "class C<S,T> where S : new() where T : class  { }";
        var src2 = "class C<S,T> where T : class { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Delete [where S : new()]@13");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangingConstraints, "S", FeaturesResources.type_parameter));
    }

    [Fact]
    public void TypeConstraint_MultipleClauses_Reorder()
    {
        var src1 = "class C<S,T> where S : struct where T : class  { }";
        var src2 = "class C<S,T> where T : class where S : struct { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [where T : class]@30 -> @13");

        edits.VerifySemanticDiagnostics();
    }

    [Fact]
    public void TypeConstraint_MultipleClauses_UpdateAndReorder()
    {
        var src1 = "class C<S,T> where S : new() where T : class  { }";
        var src2 = "class C<T,S> where T : class, I where S : class, new() { }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Reorder [where T : class]@29 -> @13",
            "Reorder [T]@10 -> @8",
            "Update [where T : class]@29 -> [where T : class, I]@13",
            "Update [where S : new()]@13 -> [where S : class, new()]@32");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Move, "T", FeaturesResources.type_parameter),
            Diagnostic(RudeEditKind.ChangingConstraints, "where T : class, I", FeaturesResources.type_parameter),
            Diagnostic(RudeEditKind.ChangingConstraints, "where S : class, new()", FeaturesResources.type_parameter));
    }

    #endregion

    #region Top Level Statements

    [Fact]
    public void TopLevelStatements_Update()
    {
        var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
        var src2 = @"
using System;

Console.WriteLine(""Hello World"");
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Update [Console.WriteLine(\"Hello\");]@19 -> [Console.WriteLine(\"Hello World\");]@19");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, @"Console.WriteLine(""Hello World"");", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_InsertAndUpdate()
    {
        var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
        var src2 = @"
using System;

Console.WriteLine(""Hello World"");
Console.WriteLine(""What is your name?"");
var name = Console.ReadLine();
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits(
            "Update [Console.WriteLine(\"Hello\");]@19 -> [Console.WriteLine(\"Hello World\");]@19",
            "Insert [Console.WriteLine(\"What is your name?\");]@54",
            "Insert [var name = Console.ReadLine();]@96");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, @"Console.WriteLine(""Hello World"");", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_Insert_NoImplicitMain()
    {
        var src1 = @"
using System;
";
        var src2 = @"
using System;

Console.WriteLine(""Hello World"");
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [Console.WriteLine(\"Hello World\");]@19");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Program.<Main>$"))],
            capabilities: EditAndContinueCapabilities.NewTypeDefinition);
    }

    [Fact]
    public void TopLevelStatements_Insert_ImplicitMain()
    {
        var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
        var src2 = @"
using System;

Console.WriteLine(""Hello"");
Console.WriteLine(""World"");
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Insert [Console.WriteLine(\"World\");]@48");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, @"Console.WriteLine(""Hello"");", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_Delete_NoImplicitMain()
    {
        var src1 = @"
using System;

Console.WriteLine(""Hello World"");
";
        var src2 = @"
using System;

";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [Console.WriteLine(\"Hello World\");]@19");

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.Delete, "using System;", GetResource("top-level statement")));
    }

    [Fact]
    public void TopLevelStatements_Delete_ImplicitMain()
    {
        var src1 = @"
using System;

Console.WriteLine(""Hello"");
Console.WriteLine(""World"");
";
        var src2 = @"
using System;

Console.WriteLine(""Hello"");
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Delete [Console.WriteLine(\"World\");]@48");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, @"Console.WriteLine(""Hello"");", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_StackAlloc()
    {
        var src1 = @"Span<int> = stackalloc int[1];";
        var src2 = @"Span<int> = stackalloc int[2];";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[2]", GetResource("top-level code")),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Span<int> = stackalloc int[2];", GetResource("top-level code"))
        ]);
    }

    [Fact]
    public void TopLevelStatements_StackAllocInUnsafeBlock()
    {
        var src1 = @"unsafe { var x = stackalloc int[3]; System.Console.Write(1); }";
        var src2 = @"unsafe { var x = stackalloc int[3]; System.Console.Write(2); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[3]", GetResource("top-level code")),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "unsafe { var x = stackalloc int[3]; System.Console.Write(2); }", GetResource("top-level code"))
        ]);
    }

    [Fact]
    public void TopLevelStatements_StackAllocInTopBlock()
    {
        var src1 = @"{ var x = stackalloc int[3]; System.Console.Write(1); }";
        var src2 = @"{ var x = stackalloc int[3]; System.Console.Write(2); }";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc int[3]", GetResource("top-level code")),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "{ var x = stackalloc int[3]; System.Console.Write(2); }", GetResource("top-level code"))
        ]);
    }

    [Fact]
    public void TopLevelStatements_VoidToInt1()
    {
        var src1 = @"
using System;

Console.Write(1);
";
        var src2 = @"
using System;

Console.Write(1);
return 1;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_VoidToInt2()
    {
        var src1 = @"
using System;

Console.Write(1);

return;
";
        var src2 = @"
using System;

Console.Write(1);
return 1;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_VoidToInt3()
    {
        var src1 = @"
using System;

Console.Write(1);

int Goo()
{
    return 1;
}
";
        var src2 = @"
using System;

Console.Write(1);
return 1;

int Goo()
{
    return 1;
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_Await_Insert_First()
    {
        var src1 = @"
using System.Threading.Tasks;

return 1;
";
        var src2 = @"
using System.Threading.Tasks;

await Task.Delay(200);
return 1;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
        [
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "await Task.Delay(200);"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(200);", GetResource("top-level code"))
        ]);
    }

    [Fact]
    public void TopLevelStatements_Await_Insert_Second()
    {
        var src1 = @"
using System.Threading.Tasks;

await Task.Delay(100);
";
        var src2 = @"
using System.Threading.Tasks;

await Task.Delay(100);
await Task.Delay(200);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true)],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code"))],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, "await Task.Delay(100);"),
                Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TopLevelStatements_Await_Delete_Last()
    {
        var src1 = @"
using System.Threading.Tasks;

await Task.Delay(100);
return 1;
";
        var src2 = @"
using System.Threading.Tasks;

return 1;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"),
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "return 1;", GetResource("top-level code")),
                Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "return 1;", GetResource("top-level code"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TopLevelStatements_Await_Delete_Second()
    {
        var src1 = @"
using System.Threading.Tasks;

await Task.Delay(100);
await Task.Delay(200);
";
        var src2 = @"
using System.Threading.Tasks;

await Task.Delay(100);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true)],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code"))],
            capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);

        edits.VerifySemanticDiagnostics(
            [
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodNotSupportedByRuntime, "await Task.Delay(100);"),
                Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code"))
            ],
            capabilities: EditAndContinueCapabilities.Baseline);
    }

    [Fact]
    public void TopLevelStatements_VoidToTask()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";
        var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "await Task.Delay(100);"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_TaskToTaskInt()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";
        var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
return 1;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_VoidToTaskInt()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";
        var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
return await GetInt();

Task<int> GetInt()
{
    return Task.FromResult(1);
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return await GetInt();"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_IntToVoid1()
    {
        var src1 = @"
using System;

Console.Write(1);

return 1;
";
        var src2 = @"
using System;

Console.Write(1);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
           Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"),
           Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_IntToVoid2()
    {
        var src1 = @"
using System;

Console.Write(1);

return 1;
";
        var src2 = @"
using System;

Console.Write(1);
return;
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return;"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_IntToVoid3()
    {
        var src1 = @"
using System;

Console.Write(1);
return 1;

int Goo()
{
    return 1;
}
";
        var src2 = @"
using System;

Console.Write(1);

int Goo()
{
    return 1;
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, """
                int Goo()
                {
                    return 1;
                }
                """),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_IntToVoid4()
    {
        var src1 = @"
using System;

Console.Write(1);
return 1;

public class C
{
    public int Goo()
    {
        return 1;
    }
}
";
        var src2 = @"
using System;

Console.Write(1);

public class C
{
    public int Goo()
    {
        return 1;
    }
}
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_TaskToVoid()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";
        var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"),
            Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "Console.Write(1);", GetResource("top-level code")),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_TaskIntToTask()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
return 1;
";
        var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "await Task.Delay(100);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_TaskIntToVoid()
    {
        var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
return await GetInt();

Task<int> GetInt()
{
    return Task.FromResult(1);
}
";
        var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";

        var edits = GetTopEdits(src1, src2);

        edits.VerifySemanticDiagnostics(
            Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"),
            Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "Console.Write(1);", GetResource("top-level code")),
            Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.Write(1);", GetResource("top-level code")));
    }

    [Fact]
    public void TopLevelStatements_WithLambda_Insert()
    {
        var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };
";
        var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0])],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Func<int> a = () => {        return 1;         };", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_WithLambda_Update()
    {
        var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);

public class C { }
";
        var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(2);

public class C { }
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0])],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Func<int> a = () => {        return 1;         };", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_WithLambda_Delete()
    {
        var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);

public class C { }
";
        var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

public class C { }
";
        var edits = GetTopEdits(src1, src2);
        var syntaxMap = GetSyntaxMap(src1, src2);

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0])],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Func<int> a = () => {        return 1;         };", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_UpdateMultiple()
    {
        var src1 = @"
using System;

Console.WriteLine(1);
Console.WriteLine(2);

public class C { }
";
        var src2 = @"
using System;

Console.WriteLine(3);
Console.WriteLine(4);

public class C { }
";
        var edits = GetTopEdits(src1, src2);

        // Since each individual statement is a separate update to a separate node, this just validates we correctly
        // only analyze the things once
        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.WriteLine(3);", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_MoveToOtherFile()
    {
        var srcA1 = @"
using System;

Console.WriteLine(1);

public class A
{
}";
        var srcB1 = @"
using System;

public class B
{
}";

        var srcA2 = @"
using System;

public class A
{
}";
        var srcB2 = @"
using System;

Console.WriteLine(2);

public class B
{
}";

        EditAndContinueValidation.VerifySemantics(
            [GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2)],
            [
                DocumentResults(),
                DocumentResults(
                    semanticEdits: [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
                    diagnostics: [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "Console.WriteLine(2);", GetResource("top-level code"))]),
            ]);
    }

    [Fact]
    public void TopLevelStatements_BlockReorder()
    {
        var src1 = @"
{ int a; }
{ int b; }
";
        var src2 = @"
{ int b; }
{ int a; }
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Reorder [{ int b; }]@14 -> @2");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "{ int b; }", GetResource("top-level code"))]);
    }

    [Fact]
    public void TopLevelStatements_Reorder()
    {
        var src1 = @"
System.Console.Write(1);
System.Console.Write(2);
";
        var src2 = @"
System.Console.Write(2);
System.Console.Write(1);
";
        var edits = GetTopEdits(src1, src2);

        edits.VerifyEdits("Reorder [System.Console.Write(2);]@28 -> @2");

        edits.VerifySemantics(
            [SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"))],
            [Diagnostic(RudeEditKind.UpdateMightNotHaveAnyEffect, "System.Console.Write(2);", GetResource("top-level code"))]);
    }

    #endregion
}
