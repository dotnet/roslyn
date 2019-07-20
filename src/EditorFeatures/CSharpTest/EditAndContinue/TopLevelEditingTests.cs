// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class TopLevelEditingTests : EditingTestBase
    {
        #region Usings

        [Fact]
        public void UsingDelete1()
        {
            var src1 = @"
using System.Diagnostics;
";
            var src2 = @"";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits("Delete [using System.Diagnostics;]@2");
            Assert.IsType<UsingDirectiveSyntax>(edits.Edits.First().OldNode);
            Assert.Equal(edits.Edits.First().NewNode, null);
        }

        [Fact]
        public void UsingDelete2()
        {
            var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System.Diagnostics;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [using System.Collections;]@29");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, CSharpFeaturesResources.using_directive));
        }

        [Fact]
        public void UsingInsert()
        {
            var src1 = @"
using System.Diagnostics;
using System.Collections.Generic;
";
            var src2 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [using System.Collections;]@29");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "using System.Collections;", CSharpFeaturesResources.using_directive));
        }

        [Fact]
        public void UsingUpdate1()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "using X = System.Collections;", CSharpFeaturesResources.using_directive));
        }

        [Fact]
        public void UsingUpdate2()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "using X2 = System.Collections;", CSharpFeaturesResources.using_directive));
        }

        [Fact]
        public void UsingUpdate3()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "using System;", CSharpFeaturesResources.using_directive));
        }

        [Fact]
        public void UsingReorder1()
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
        public void UsingInsertDelete1()
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
        public void UsingInsertDelete2()
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

        #endregion

        #region Attributes

        [Fact]
        public void UpdateAttributes1()
        {
            var src1 = "[A1]class C { }";
            var src2 = "[A2]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A1]@1 -> [A2]@1");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A2", FeaturesResources.attribute));
        }

        [Fact]
        public void UpdateAttributes2()
        {
            var src1 = "[A(1)]class C { }";
            var src2 = "[A(2)]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A(1)]@1 -> [A(2)]@1");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute));
        }

        [Fact]
        public void DeleteAttributes()
        {
            var src1 = "[A, B]class C { }";
            var src2 = "[A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A, B]]@0 -> [[A]]@0",
                "Delete [B]@4");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void InsertAttributes1()
        {
            var src1 = "[A]class C { }";
            var src2 = "[A, B]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]]@0 -> [[A, B]]@0",
                "Insert [B]@4");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.attribute));
        }

        [Fact]
        public void InsertAttributes2()
        {
            var src1 = "class C { }";
            var src2 = "[A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@0",
                "Insert [A]@1");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void ReorderAttributes1()
        {
            var src1 = "[A(1), B(2), C(3)]class C { }";
            var src2 = "[C(3), A(1), B(2)]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [C(3)]@13 -> @1");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void ReorderAttributes2()
        {
            var src1 = "[A, B, C]class C { }";
            var src2 = "[B, C, A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [A]@1 -> @7");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void ReorderAndUpdateAttributes()
        {
            var src1 = "[A(1), B, C]class C { }";
            var src2 = "[B, C, A(2)]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [A(1)]@1 -> @7",
                "Update [A(1)]@1 -> [A(2)]@7");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute));
        }

        #endregion

        #region Classes, Structs, Interfaces

        [Fact]
        public void TypeKindUpdate()
        {
            var src1 = "class C { }";
            var src2 = "struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeKindUpdate, "struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Class_Modifiers_Update()
        {
            var src1 = "public static class C { }";
            var src2 = "public class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public static class C { }]@0 -> [public class C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public class C", FeaturesResources.class_));
        }

        [Fact]
        public void Struct_Modifiers_Ref_Update1()
        {
            var src1 = "public struct C { }";
            var src2 = "public ref struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public struct C { }]@0 -> [public ref struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public ref struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Struct_Modifiers_Ref_Update2()
        {
            var src1 = "public ref struct C { }";
            var src2 = "public struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public ref struct C { }]@0 -> [public struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Struct_Modifiers_Readonly_Update1()
        {
            var src1 = "public struct C { }";
            var src2 = "public readonly struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public struct C { }]@0 -> [public readonly struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Struct_Modifiers_Readonly_Update2()
        {
            var src1 = "public readonly struct C { }";
            var src2 = "public struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public readonly struct C { }]@0 -> [public struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Interface_Modifiers_Update()
        {
            var src1 = "public interface C { }";
            var src2 = "interface C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public interface C { }]@0 -> [interface C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "interface C", FeaturesResources.interface_));
        }

        [Fact]
        public void Struct_Modifiers_Update()
        {
            var src1 = "struct C { }";
            var src2 = "public struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [struct C { }]@0 -> [public struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Struct_UnsafeModifier_Update()
        {
            var src1 = "unsafe struct C { }";
            var src2 = "struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [unsafe struct C { }]@0 -> [struct C { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Class_Name_Update1()
        {
            var src1 = "class C { }";
            var src2 = "class D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class D { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void Class_Name_Update2()
        {
            var src1 = "class LongerName { }";
            var src2 = "class LongerMame { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class LongerName { }]@0 -> [class LongerMame { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class LongerMame", FeaturesResources.class_));
        }

        [Fact]
        public void Interface_Name_Update()
        {
            var src1 = "interface C { }";
            var src2 = "interface D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [interface C { }]@0 -> [interface D { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "interface D", FeaturesResources.interface_));
        }

        [Fact]
        public void Struct_Name_Update()
        {
            var src1 = "struct C { }";
            var src2 = "struct D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "struct D", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Interface_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "interface C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Interface_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { } ";
            var src2 = "namespace N { interface C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Interface_NoModifiers_IntoType_Insert()
        {
            var src1 = "interface N { }";
            var src2 = "interface N { interface C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Class_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Class_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { class C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Class_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { class C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Struct_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Struct_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { struct C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Struct_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { struct C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void BaseTypeUpdate1()
        {
            var src1 = "class C { }";
            var src2 = "class C : D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class C : D { }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void BaseTypeUpdate2()
        {
            var src1 = "class C : D1 { }";
            var src2 = "class C : D2 { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C : D1 { }]@0 -> [class C : D2 { }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void BaseInterfaceUpdate1()
        {
            var src1 = "class C { }";
            var src2 = "class C : IDisposable { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class C : IDisposable { }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void BaseInterfaceUpdate2()
        {
            var src1 = "class C : IGoo, IBar { }";
            var src2 = "class C : IGoo { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C : IGoo, IBar { }]@0 -> [class C : IGoo { }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void BaseInterfaceUpdate3()
        {
            var src1 = "class C : IGoo, IBar { }";
            var src2 = "class C : IBar, IGoo { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C : IGoo, IBar { }]@0 -> [class C : IBar, IGoo { }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void ClassInsert_AbstractVirtualOverride()
        {
            var src1 = "";
            var src2 = @"
public abstract class C<T>
{ 
    public abstract void F(); 
    public virtual void G() {}
    public override void H() {}
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void InterfaceInsert()
        {
            var src1 = "";
            var src2 = @"
public interface I 
{ 
    void F(); 
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void RefStructInsert()
        {
            var src1 = "";
            var src2 = "ref struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref struct X { }]@0");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void ReadOnlyStructInsert()
        {
            var src1 = "";
            var src2 = "readonly struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [readonly struct X { }]@0");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void RefStructUpdate()
        {
            var src1 = "struct X { }";
            var src2 = "ref struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [struct X { }]@0 -> [ref struct X { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "ref struct X", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void ReadOnlyStructUpdate()
        {
            var src1 = "struct X { }";
            var src2 = "readonly struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [struct X { }]@0 -> [readonly struct X { }]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly struct X", SyntaxFacts.GetText(SyntaxKind.StructKeyword)));
        }

        [Fact]
        public void Class_ImplementingInterface_Add()
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
                @"Insert [public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;

    string IConflict.Get() => String.Empty;
}]@219",
                "Insert [public override string Get() => string.Empty;]@272",
                "Insert [string IConflict.Get() => String.Empty;]@325",
                "Insert [()]@298",
                "Insert [()]@345");

            // Here we add a class implementing an interface and a method inside it with explicit interface specifier.
            // We want to be sure that adding the method will not tirgger a rude edit as it happens if adding a single method with explicit interface specifier.
            edits.VerifyRudeDiagnostics();
        }

        [WorkItem(37128, "https://github.com/dotnet/roslyn/issues/37128")]
        [Fact]
        public void Interface_AddMembersWithImplementation()
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

    interface J { }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "void VirtualMethod1()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual void VirtualMethod2()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract void AbstractMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertOperator, "public static int operator +(I a, I b)", FeaturesResources.operator_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty1", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty2", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty3", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty4", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty1", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "int this[byte virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "int this[sbyte virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[ushort virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[short virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[uint abstractIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[int abstractIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "event Action VirtualEvent", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract event Action AbstractEvent", CSharpFeaturesResources.event_field),
                // TODO: The following errors are reported due to https://github.com/dotnet/roslyn/issues/37128.
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticField = 10", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static void StaticMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed void NonVirtualMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty1", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int NonVirtualProperty", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[ulong nonVirtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[long nonVirtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static event Action StaticEvent", CSharpFeaturesResources.event_field),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static event Action StaticEvent2", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed event Action NonVirtualEvent", FeaturesResources.event_));
        }

        #endregion

        #region Enums

        [Fact]
        public void Enum_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "enum C { A }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Enum_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { enum C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Enum_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { enum C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void EnumAttributeInsert()
        {
            var src1 = "enum E { }";
            var src2 = "[A]enum E { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@0",
                "Insert [A]@1");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void EnumMemberAttributeDelete()
        {
            var src1 = "enum E { [A]X }";
            var src2 = "enum E { X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [[A]]@9",
                "Delete [A]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "X", FeaturesResources.attribute));
        }

        [Fact]
        public void EnumMemberAttributeInsert()
        {
            var src1 = "enum E { X }";
            var src2 = "enum E { [A]X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@9",
                "Insert [A]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void EnumMemberAttributeUpdate()
        {
            var src1 = "enum E { [A1]X }";
            var src2 = "enum E { [A2]X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A1]@10 -> [A2]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A2", FeaturesResources.attribute));
        }

        [Fact]
        public void EnumNameUpdate()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Colors { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Colors { Red = 1, Blue = 2, }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Renamed, "enum Colors", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumBaseTypeAdd()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color : ushort { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color : ushort { Red = 1, Blue = 2, }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumBaseTypeUpdate()
        {
            var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
            var src2 = "enum Color : long { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color : ushort { Red = 1, Blue = 2, }]@0 -> [enum Color : long { Red = 1, Blue = 2, }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumBaseTypeDelete()
        {
            var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color : ushort { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumModifierUpdate()
        {
            var src1 = "public enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.ModifiersUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumInitializerUpdate()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 3, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Blue = 2]@22 -> [Blue = 3]@22");

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1 << 0", FeaturesResources.enum_value),
                 Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 2 << 1", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerUpdate3()
        {
            var src1 = "enum Color { Red = int.MinValue }";
            var src2 = "enum Color { Red = int.MaxValue }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [Red = int.MinValue]@13 -> [Red = int.MaxValue]@13");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red = int.MaxValue", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerAdd()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red = 1, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Red]@13 -> [Red = 1]@13");

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red", FeaturesResources.enum_value));
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        [Fact]
        public void EnumMemberAdd()
        {
            var src1 = "enum Color { Red }";
            var src2 = "enum Color { Red, Blue}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red }]@0 -> [enum Color { Red, Blue}]@0",
                "Insert [Blue]@18");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumMemberAdd2()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red, Blue}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [Blue]@18");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        [Fact]
        public void EnumMemberAdd3()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red, Blue,}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red, Blue,}]@0",
                              "Insert [Blue]@18");

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Renamed, "Orange", FeaturesResources.enum_value));
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        [Fact]
        public void EnumMemberDelete()
        {
            var src1 = "enum Color { Red, Blue}";
            var src2 = "enum Color { Red }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red, Blue}]@0 -> [enum Color { Red }]@0",
                "Delete [Blue]@18");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "enum Color", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumMemberDelete2()
        {
            var src1 = "enum Color { Red, Blue}";
            var src2 = "enum Color { Red, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [Blue]@18");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "enum Color", FeaturesResources.enum_value));
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem(793197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        [Fact]
        public void EnumTrailingCommaAdd()
        {
            var src1 = "enum Color { Red }";
            var src2 = "enum Color { Red, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red }]@0 -> [enum Color { Red, }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem(793197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        [Fact]
        public void EnumTrailingCommaAdd_WithInitializer()
        {
            var src1 = "enum Color { Red = 1 }";
            var src2 = "enum Color { Red = 1, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red = 1 }]@0 -> [enum Color { Red = 1, }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem(793197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        [Fact]
        public void EnumTrailingCommaDelete()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [WorkItem(754916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem(793197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        [Fact]
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
        public void Delegates_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_NoModifiers_IntoType_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_Public_IntoType_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { public delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate void D();]@10",
                "Insert [()]@32");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_Generic_Insert()
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_Delete()
        {
            var src1 = "class C { private delegate void D(); }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [private delegate void D();]@10",
                "Delete [()]@33");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegates_Rename()
        {
            var src1 = "public delegate void D();";
            var src2 = "public delegate void Z();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate void D();]@0 -> [public delegate void Z();]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "public delegate void Z()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegates_Update_Modifiers()
        {
            var src1 = "public delegate void D();";
            var src2 = "private delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate void D();]@0 -> [private delegate void D();]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "private delegate void D()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegates_Update_ReturnType()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D();]@0 -> [public delegate void D();]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "public delegate void D()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegates_Parameter_Insert()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int a]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int a", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_Parameter_Delete()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int a]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public delegate int D()", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_Parameter_Rename()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D(int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@22 -> [int b]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_Parameter_Update()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D(byte a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@22 -> [byte a]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "byte a", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_Parameter_UpdateModifier()
        {
            var src1 = "public delegate int D(int[] a);";
            var src2 = "public delegate int D(params int[] a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int[] a]@22 -> [params int[] a]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "params int[] a", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_Parameter_AddAttribute()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D([A]int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@22",
                "Insert [A]@23");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void Delegates_TypeParameter_Insert()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D<T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [<T>]@21",
                "Insert [T]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<T>", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_Delete()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [<T>]@21",
                "Delete [T]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public delegate int D()", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_Rename()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D<S>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@22 -> [S]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "S", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_Variance1()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D<in T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@22 -> [in T]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_Variance2()
        {
            var src1 = "public delegate int D<out T>();";
            var src2 = "public delegate int D<T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [out T]@22 -> [T]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_Variance3()
        {
            var src1 = "public delegate int D<out T>();";
            var src2 = "public delegate int D<in T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [out T]@22 -> [in T]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter));
        }

        [Fact]
        public void Delegates_TypeParameter_AddAttribute()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D<[A]T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@22",
                "Insert [A]@23");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void Delegates_AddAttribute()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "[return:A]public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[return:A]]@0",
                "Insert [A]@8");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[return:A]", FeaturesResources.attribute));
        }

        [Fact]
        public void Delegates_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate int D(in int b);]@0",
                "Insert [(in int b)]@21",
                "Insert [in int b]@22");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "public delegate int D(int b);";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@22 -> [in int b]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegates_ReadOnlyRef_ReturnType_Insert()
        {
            var src1 = "";
            var src2 = "public delegate ref readonly int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate ref readonly int D();]@0",
                "Insert [()]@34");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Delegates_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate ref readonly int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D();]@0 -> [public delegate ref readonly int D();]@0");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "public delegate ref readonly int D()", FeaturesResources.delegate_));
        }

        #endregion

        #region Nested Types

        [Fact]
        public void NestedClass_ClassMove1()
        {
            var src1 = @"class C { class D { } }";
            var src2 = @"class C { } class D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class D { }]@10 -> @12");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void NestedClass_ClassMove2()
        {
            var src1 = @"class C { class D { }  class E { }  class F { } }";
            var src2 = @"class C { class D { }  class F { } } class E { }  ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class E { }]@23 -> @37");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "class E", FeaturesResources.class_));
        }

        [Fact]
        public void NestedClass_ClassInsertMove1()
        {
            var src1 = @"class C { class D { } }";
            var src2 = @"class C { class E { class D { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [class E { class D { } }]@10",
                "Move [class D { }]@10 -> @20");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void NestedClass_Insert1()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { class D { class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [class D { class E { } }]@10",
                "Insert [class E { }]@20");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_Insert2()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { protected class D { public class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [protected class D { public class E { } }]@10",
                "Insert [public class E { }]@30");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_Insert3()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { private class D { public class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private class D { public class E { } }]@10",
                "Insert [public class E { }]@28");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_Insert4()
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_InsertMemberWithInitializer1()
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

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D"), preserveLocalVariables: false)
            });
        }

        [WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        [Fact]
        public void NestedClass_Insert_PInvoke()
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

        public static extern int P { [DllImport(""msvcrt.dll"")]get; }

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
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "public extern D()", FeaturesResources.constructor),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int P", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int puts(string c)", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int operator +(D d, D g)", FeaturesResources.operator_),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern explicit operator int (D d)", CSharpFeaturesResources.conversion_operator));
        }

        [WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        [Fact]
        public void NestedClass_Insert_VirtualAbstract()
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_TypeReorder1()
        {
            var src1 = @"class C { struct E { } class F { } delegate void D(); interface I {} }";
            var src2 = @"class C { class F { } interface I {} delegate void D(); struct E { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [struct E { }]@10 -> @56",
                "Reorder [interface I {}]@54 -> @22");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NestedClass_MethodDeleteInsert()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public class C", FeaturesResources.method));
        }

        [Fact]
        public void NestedClass_ClassDeleteInsert()
        {
            var src1 = @"public class C { public class X {} }";
            var src2 = @"public class C { public class D { public class X {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public class D { public class X {} }]@17",
                "Move [public class X {}]@17 -> @34");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Move, "public class X", FeaturesResources.class_));
        }

        #endregion

        #region Namespaces

        [Fact]
        public void NamespaceMove1()
        {
            var src1 = @"namespace C { namespace D { } }";
            var src2 = @"namespace C { } namespace D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [namespace D { }]@14 -> @16");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "namespace D", FeaturesResources.namespace_));
        }

        [Fact]
        public void NamespaceReorder1()
        {
            var src1 = @"namespace C { namespace D { } class T { } namespace E { } }";
            var src2 = @"namespace C { namespace E { } class T { } namespace D { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [class T { }]@30 -> @30",
                "Reorder [namespace E { }]@42 -> @14");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void NamespaceReorder2()
        {
            var src1 = @"namespace C { namespace D1 { } namespace D2 { } namespace D3 { } class T { } namespace E { } }";
            var src2 = @"namespace C { namespace E { }                                    class T { } namespace D1 { } namespace D2 { } namespace D3 { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [class T { }]@65 -> @65",
                "Reorder [namespace E { }]@77 -> @14");

            edits.VerifyRudeDiagnostics();
        }

        #endregion

        #region Members

        [Fact]
        public void MemberUpdate_Modifier_ReadOnly_Remove()
        {
            var src1 = @"
using System;

struct S
{
    // methods
    public readonly int M() => 1;

    // properties
    public readonly int P => 1;
    public readonly int Q { get; }
    public int R { readonly get; readonly set; }

    // events
    public readonly event Action E { add {} remove {} }
    public event Action F { readonly add {} readonly remove {} }
}";
            var src2 = @"
using System;
struct S
{
    // methods
    public int M() => 1;

    // properties
    public int P => 1;
    public int Q { get; }
    public int R { get; set; }

    // events
    public event Action E { add {} remove {} }
    public event Action F { add {} remove {} }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public int M()", FeaturesResources.method),
                Diagnostic(RudeEditKind.ModifiersUpdate, "public int P", FeaturesResources.property_),
                Diagnostic(RudeEditKind.ModifiersUpdate, "public int Q", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.ModifiersUpdate, "get", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "set", CSharpFeaturesResources.property_setter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "add", FeaturesResources.event_accessor),
                Diagnostic(RudeEditKind.ModifiersUpdate, "remove", FeaturesResources.event_accessor));
        }

        [Fact]
        public void MemberUpdate_Modifier_ReadOnly_Add()
        {
            var src1 = @"
using System;

struct S
{
    // methods
    public int M() => 1;

    // properties
    public int P => 1;
    public int Q { get; }
    public int R { get; set; }

    // events
    public event Action E { add {} remove {} }
    public event Action F { add {} remove {} }
}";
            var src2 = @"
using System;

struct S
{
    // methods
    public readonly int M() => 1;

    // properties
    public readonly int P => 1;
    public readonly int Q { get; }
    public int R { readonly get; readonly set; }

    // events
    public readonly event Action E { add {} remove {} }
    public event Action F { readonly add {} readonly remove {} }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly int M()", FeaturesResources.method),
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly int P", FeaturesResources.property_),
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly int Q", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly get", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly set", CSharpFeaturesResources.property_setter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly add", FeaturesResources.event_accessor),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly remove", FeaturesResources.event_accessor));
        }

        #endregion

        #region Methods

        [Fact]
        public void Method_Update()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
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
    static void Main(string[] args)
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Update [static void Main(string[] args)
    {
        int a = 1;
        int b = 2;
        System.Console.WriteLine(a + b);
    }]@18 -> [static void Main(string[] args)
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }]@18");

            edits.VerifyRudeDiagnostics();

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), preserveLocalVariables: false) });
        }

        [Fact]
        public void MethodWithExpressionBody_Update()
        {
            var src1 = @"
class C
{
    static int Main(string[] args) => F(1);
    static int F(int a) => 1;
}
";
            var src2 = @"
class C
{
    static int Main(string[] args) => F(2);
    static int F(int a) => 1;
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Update [static int Main(string[] args) => F(1);]@18 -> [static int Main(string[] args) => F(2);]@18");

            edits.VerifyRudeDiagnostics();

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), preserveLocalVariables: false) });
        }

        [Fact]
        public void MethodWithExpressionBody_ToBlockBody()
        {
            var src1 = "class C { static int F(int a) => 1; }";
            var src2 = "class C { static int F(int a) { return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [static int F(int a) => 1;]@10 -> [static int F(int a) { return 2; }]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void MethodWithBlockBody_ToExpressionBody()
        {
            var src1 = "class C { static int F(int a) { return 2; } }";
            var src2 = "class C { static int F(int a) => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [static int F(int a) { return 2; }]@10 -> [static int F(int a) => 1;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
            });
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), syntaxMap[0]) });
        }

        [Fact]
        public void MethodUpdate_LocalVariableDeclaration()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        int x = 1;
        Console.WriteLine(x);
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(x);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
@"Update [static void Main(string[] args)
    {
        int x = 1;
        Console.WriteLine(x);
    }]@18 -> [static void Main(string[] args)
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

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [void goo() { }]@18",
                "Delete [()]@26");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.method));
        }

        [Fact]
        public void MethodWithExpressionBody_Delete()
        {
            var src1 = @"
class C
{
    int goo() => 1;

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int goo() => 1;]@18",
                "Delete [()]@25");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.method));
        }

        [Fact]
        public void MethodDelete_WithParameters()
        {
            var src1 = @"
class C
{
    void goo(int a) { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [void goo(int a) { }]@18",
                "Delete [(int a)]@26",
                "Delete [int a]@27");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.method));
        }

        [WorkItem(754853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        [Fact]
        public void MethodDelete_WithAttribute()
        {
            var src1 = @"
class C
{
    [Obsolete]
    void goo(int a) { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Delete [[Obsolete]
    void goo(int a) { }]@18",
                "Delete [[Obsolete]]@18",
                "Delete [Obsolete]@19",
                "Delete [(int a)]@42",
                "Delete [int a]@43");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.method));
        }

        [WorkItem(754853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        [Fact]
        public void MethodDelete_PInvoke()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Delete [[DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);]@74",
                @"Delete [[DllImport(""msvcrt.dll"")]]@74",
                @"Delete [DllImport(""msvcrt.dll"")]@75",
                 "Delete [(string c)]@134",
                 "Delete [string c]@135");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.method));
        }

        [Fact]
        public void PrivateMethodInsert()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
class C
{
    void goo() { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [void goo() { }]@18",
                "Insert [()]@26");

            edits.VerifyRudeDiagnostics();
        }

        [WorkItem(755784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
        [Fact]
        public void PrivateMethodInsert_WithParameters()
        {
            var src1 = @"
using System;

class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
using System;

class C
{
    void goo(int a) { }

    static void Main(string[] args)
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
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.goo")) });
        }

        [WorkItem(755784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
        [Fact]
        public void PrivateMethodInsert_WithAttribute()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
class C
{
    [Obsolete]
    void goo(int a) { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Insert [[Obsolete]
    void goo(int a) { }]@18",
                "Insert [[Obsolete]]@18",
                "Insert [(int a)]@42",
                "Insert [Obsolete]@19",
                "Insert [int a]@43");

            edits.VerifyRudeDiagnostics();
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

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "public override void F()", FeaturesResources.method));
        }

        [WorkItem(755784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        [Fact]
        public void PrivateMethodInsert_PInvoke1()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Insert [[DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);]@74",
                @"Insert [[DllImport(""msvcrt.dll"")]]@74",
                "Insert [(string c)]@135",
                @"Insert [DllImport(""msvcrt.dll"")]@75",
                 "Insert [string c]@136");

            // CLR doesn't support methods without a body
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int puts(string c)", FeaturesResources.method));
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
        public void MethodUpdate_AddParameter()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [string[] args]@35");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "string[] args", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_UpdateParameter()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] b)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [string[] args]@35 -> [string[] b]@35");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "string[] b", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_UpdateParameterToNullable()
        {
            string src1 = @"
class C
{
    static void M(string s)
    {
    }
}";
            string src2 = @"
class C
{
    static void M(string? s)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [string s]@32 -> [string? s]@32");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "string? s", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_UpdateParameterToNonNullable()
        {
            string src1 = @"
class C
{
    static void M(string? s)
    {
        
    }
}";
            string src2 = @"
class C
{
    static void M(string s)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [string? s]@32 -> [string s]@32");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "string s", FeaturesResources.parameter));
        }


        [Fact]
        public void MethodUpdate_RenameMethodName()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void EntryPoint(string[] args)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            var expectedEdit = @"Update [static void Main(string[] args)
    {
        
    }]@18 -> [static void EntryPoint(string[] args)
    {
        
    }]@18";

            edits.VerifyEdits(expectedEdit);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "static void EntryPoint(string[] args)", FeaturesResources.method));
        }

        [Fact]
        public void MethodUpdate_ReorderParameter()
        {
            var src1 = @"
class C
{
    static void Main(int a, char c)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(char c, int a)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [char c]@42 -> @35");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "char c", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_DeleteParameter()
        {
            var src1 = @"
class C
{
    static void Main(string[] args) { }
}";
            var src2 = @"
class C
{
    static void Main() { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [string[] args]@35");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "static void Main()", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_Modifier_Async_Remove()
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
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "public Task<int> WaitAsync()", FeaturesResources.method));
        }

        [Fact]
        public void MethodUpdate_Modifier_Async_Add()
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
            edits.VerifyRudeDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
        }

        [Fact]
        public void MethodUpdate_AsyncMethod0()
        {
            var src1 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
            var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(500);
        return 1;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
        }

        [Fact]
        public void MethodUpdate_AsyncMethod1()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        string result = f.WaitAsync().Result;
    }

    public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Done"";
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        string result = f.WaitAsync().Result;
    }

    public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Not Done"";
    }
}";
            var edits = GetTopEdits(src1, src2);
            var expectedEdit = @"Update [public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Done"";
    }]@151 -> [public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Not Done"";
    }]@151";

            edits.VerifyEdits(expectedEdit);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_DeleteParameterModifierThis()
        {
            var src1 = @"
class C
{
    static void Main(string[] args) 
    { 
        var s = ""1"";
        s.ToInt32();
    }
}
public static class Extensions
{
    public static int ToInt32(this string s)
    {
        return Int32.Parse(s);
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args) 
    { 
        var s = ""1"";
        s.ToInt32();
    }
}
public static class Extensions
{
    public static int ToInt32(string s)
    {
        return Int32.Parse(s);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [this string s]@179 -> [string s]@179");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "string s", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_DeleteParameterModifiersRefAndOut()
        {
            var src1 = @"
class C
{
    static void Main(string[] args) 
    { 
        int a;
        C c = new C();
        c.Method(out a);
        c.Method2(ref a);
    }
    void Method(out int a)
    {
        a = 1;
    }
    void Method2(ref int b)
    {
        b = 45;
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args) 
    { 
        int a;
        C c = new C();
        c.Method(out a);
        c.Method2(ref a);
    }
    void Method(int a)
    {
        a = 1;
    }
    void Method2(int b)
    {
        b = 45;
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [out int a]@176 -> [int a]@176",
                "Update [ref int b]@235 -> [int b]@231");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "int a", FeaturesResources.parameter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "int b", FeaturesResources.parameter));
        }

        [Fact]
        public void MethodUpdate_AddAttribute()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [[Obsolete]]@21", "Insert [Obsolete]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[Obsolete]", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_AddAttribute2()
        {
            var src1 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete, Serializable]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [[Obsolete]]@21 -> [[Obsolete, Serializable]]@21",
                               "Insert [Serializable]@32");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "Serializable", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_AddAttribute3()
        {
            var src1 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete]
    [Serializable]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [[Serializable]]@37",
                              "Insert [Serializable]@38");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[Serializable]", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_AddAttribute4()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete, Serializable]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[Obsolete, Serializable]]@21",
                "Insert [Obsolete]@22",
                "Insert [Serializable]@32");

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "[Obsolete, Serializable]", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_UpdateAttribute()
        {
            var src1 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete("""")]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [Obsolete]@22 -> [Obsolete("""")]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, @"Obsolete("""")", FeaturesResources.attribute));
        }

        [WorkItem(754853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        [Fact]
        public void MethodUpdate_DeleteAttribute()
        {
            var src1 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [[Obsolete]]@21",
                "Delete [Obsolete]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "static void Main(string[] args)", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_DeleteAttribute2()
        {
            var src1 = @"
class Test
{
    [Obsolete, Serializable]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [[Obsolete, Serializable]]@21 -> [[Obsolete]]@21",
                              "Delete [Serializable]@32");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "[Obsolete]", FeaturesResources.attribute));
        }

        [Fact]
        public void MethodUpdate_DeleteAttribute3()
        {
            var src1 = @"
class Test
{
    [Obsolete]
    [Serializable]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [[Serializable]]@37",
                              "Delete [Serializable]@38");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "static void Main(string[] args)", FeaturesResources.attribute));
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_ExplicitlyImplemented2()
        {
            var src1 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
            var src2 = @"
class C : I, J
{
    void Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [void I.Goo() { Console.WriteLine(1); }]@25 -> [void Goo() { Console.WriteLine(1); }]@25");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "void Goo()", FeaturesResources.method));
        }

        [WorkItem(754255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754255")]
        [Fact]
        public void MethodUpdate_UpdateStackAlloc()
        {
            var src1 = @"
class C
{
    static void Main(string[] args) 
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
    static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                char* buffer = stackalloc char[16];
                int* px2 = &i;
            }
    }
}";
            var expectedEdit = @"Update [static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                int* px2 = &i;
            }
    }]@18 -> [static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                char* buffer = stackalloc char[16];
                int* px2 = &i;
            }
    }]@18";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(expectedEdit);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.method));
        }

        [Theory]
        [InlineData("stackalloc int[3]")]
        [InlineData("stackalloc int[3] { 1, 2, 3 }")]
        [InlineData("stackalloc int[] { 1, 2, 3 }")]
        [InlineData("stackalloc[] { 1, 2, 3 }")]
        public void MethodUpdate_UpdateStackAlloc2(string stackallocDecl)
        {
            var src1 = @"unsafe class C { static int F() { var x = " + stackallocDecl + "; return 1; } }";
            var src2 = @"unsafe class C { static int F() { var x = " + stackallocDecl + "; return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.method));
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void MethodUpdate_UpdateSwitchExpression()
        {
            var src1 = @"
class C
{
    static int F(int a) => a switch { 0 => 0, _ => 1 };
}";
            var src2 = @"
class C
{
    static int F(int a) => a switch { 0 => 0, _ => 2 };
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.SwitchExpressionUpdate, "switch", FeaturesResources.method));
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLambda1()
        {
            var src1 = "unsafe class C { void M() { F(1, () => { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, () => { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLambda2()
        {
            var src1 = "unsafe class C { void M() { F(1, x => { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, x => { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInAnonymousMethod()
        {
            var src1 = "unsafe class C { void M() { F(1, delegate(int x) { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, delegate(int x) { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLocalFunction()
        {
            var src1 = "class C { void M() { unsafe void f(int x) { int* a = stackalloc int[10]; } f(1); } }";
            var src2 = "class C { void M() { unsafe void f(int x) { int* a = stackalloc int[10]; } f(2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLambda1()
        {
            var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
            var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLambda2()
        {
            var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
            var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInAnonymousMethod()
        {
            var src1 = "class C { void M() { F(1, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";
            var src2 = "class C { void M() { F(2, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLocalFunction()
        {
            var src1 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(1); } }";
            var src2 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInQuery()
        {
            var src1 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 1; } }";
            var src2 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateAnonymousMethod()
        {
            var src1 = "class C { void M() { F(1, delegate(int a) { return a; }); } }";
            var src2 = "class C { void M() { F(2, delegate(int a) { return a; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_UpdateAnonymousMethod()
        {
            var src1 = "class C { void M() => F(1, delegate(int a) { return a; }); }";
            var src2 = "class C { void M() => F(2, delegate(int a) { return a; }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_Query()
        {
            var src1 = "class C { void M() { F(1, from goo in bar select baz); } }";
            var src2 = "class C { void M() { F(2, from goo in bar select baz); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_Query()
        {
            var src1 = "class C { void M() => F(1, from goo in bar select baz); }";
            var src2 = "class C { void M() => F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_AnonymousType()
        {
            var src1 = "class C { void M() { F(1, new { A = 1, B = 2 }); } }";
            var src2 = "class C { void M() { F(2, new { A = 1, B = 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_AnonymousType()
        {
            var src1 = "class C { void M() => F(new { A = 1, B = 2 }); }";
            var src2 = "class C { void M() => F(new { A = 10, B = 20 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void MethodUpdate_Iterator_YieldReturn()
        {
            var src1 = "class C { IEnumerable<int> M() { yield return 1; } }";
            var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
        }

        [Fact]
        public void MethodUpdate_AddYieldReturn()
        {
            var src1 = "class C { IEnumerable<int> M() { return new[] { 1, 2, 3}; } }";
            var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
        }

        [Fact]
        public void MethodUpdate_Iterator_YieldBreak()
        {
            var src1 = "class C { IEnumerable<int> M() { F(); yield break; } }";
            var src2 = "class C { IEnumerable<int> M() { G(); yield break; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
        }

        [WorkItem(1087305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087305")]
        [Fact]
        public void MethodUpdate_LabeledStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
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
    static void Main(string[] args)
    {
        goto Label1;
 
    Label1:
        {
            Console.WriteLine(2);
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Method_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "class Test { int M() => throw null; }";
            var src2 = "class Test { int M(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@19");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Method_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { int M(int b) => throw null; }";
            var src2 = "class Test { int M(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@19 -> [in int b]@19");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Method_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "class Test { int M() => throw null; }";
            var src2 = "class Test { ref readonly int M() => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int M() => throw null;]@13 -> [ref readonly int M() => throw null;]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "ref readonly int M()", FeaturesResources.method));
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, "string IConflict.Get()", FeaturesResources.method));
        }

        #endregion

        #region Operators

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

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", CSharpFeaturesResources.conversion_operator),
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.operator_));
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

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
            });
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

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
            });
        }

        [Fact]
        public void OperatorWithExpressionBody_ToBlockBody()
        {
            var src1 = "class C { public static C operator +(C c, C d) => d; }";
            var src2 = "class C { public static C operator +(C c, C d) { return c; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public static C operator +(C c, C d) => d;]@10 -> [public static C operator +(C c, C d) { return c; }]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
            });
        }

        [Fact]
        public void OperatorWithBlockBody_ToExpressionBody()
        {
            var src1 = "class C { public static C operator +(C c, C d) { return c; } }";
            var src2 = "class C { public static C operator +(C c, C d) => d;  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public static C operator +(C c, C d) { return c; }]@10 -> [public static C operator +(C c, C d) => d;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
            });
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

            edits.VerifyRudeDiagnostics();
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

            edits.VerifyRudeDiagnostics();
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

            edits.VerifyRudeDiagnostics(
                 Diagnostic(RudeEditKind.InsertOperator, "public static bool operator !(in Test b)", FeaturesResources.operator_));
        }

        [Fact]
        public void Operator_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { public static bool operator !(Test b) => throw null; }";
            var src2 = "class Test { public static bool operator !(in Test b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Test b]@43 -> [in Test b]@43");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in Test b", FeaturesResources.parameter));
        }

        #endregion

        #region Constructor, Destructor

        [Fact]
        public void ConstructorInitializer_Update1()
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void ConstructorInitializer_Update2()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "public C(int a)", FeaturesResources.constructor));
        }

        [Fact]
        public void ConstructorInitializer_Update3()
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

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void ConstructorInitializer_Update4()
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "public C(int a)", FeaturesResources.constructor));
        }

        [WorkItem(743552, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743552")]
        [Fact]
        public void ConstructorUpdate_AddParameter()
        {
            var src1 = @"
class C
{
    public C(int a) { }

    static void Main(string[] args)
    {
        C c = new C(5);        
    }
}";
            var src2 = @"
class C
{
    public C(int a, int b) { }

    static void Main(string[] args)
    {
        C c = new C(5);                
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int a)]@26 -> [(int a, int b)]@26",
                "Insert [int b]@34");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int b", FeaturesResources.parameter));
        }

        [Fact]
        public void DestructorDelete()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        B b = new B();
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
class B
{
    ~B()
    {
        Console.WriteLine(""B's destructor"");
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        B b = new B();
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
class B
{

}";

            var expectedEdit1 = @"Delete [~B()
    {
        Console.WriteLine(""B's destructor"");
    }]@190";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(expectedEdit1);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class B", CSharpFeaturesResources.destructor));
        }

        [Fact]
        public void DestructorDelete_InsertConstructor()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        B b = new B();
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
class B
{
    ~B()
    {
        Console.WriteLine(""B's destructor"");
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        B b = new B();
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
class B
{
    B()
    {
        Console.WriteLine(""B's destructor"");
    }
}";
            var expectedEdit1 = @"Insert [B()
    {
        Console.WriteLine(""B's destructor"");
    }]@190";

            var expectedEdit2 = @"Delete [~B()
    {
        Console.WriteLine(""B's destructor"");
    }]@190";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(expectedEdit1, "Insert [()]@191", expectedEdit2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class B", CSharpFeaturesResources.destructor));
        }

        [Fact]
        [WorkItem(789577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789577")]
        public void ConstructorUpdate_AnonymousTypeInFieldInitializer()
        {
            var src1 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 1; } }";
            var src2 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void StaticCtorDelete()
        {
            var src1 = "class C { static C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorDelete_Public()
        {
            var src1 = "class C { public C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single()) });
        }

        [Fact]
        public void InstanceCtorDelete_Private1()
        {
            var src1 = "class C { C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorDelete_Private2()
        {
            var src1 = "class C { private C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorDelete_Protected()
        {
            var src1 = "class C { protected C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorDelete_Internal()
        {
            var src1 = "class C { internal C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorDelete_ProtectedInternal()
        {
            var src1 = "class C { protected internal C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void StaticCtorInsert()
        {
            var src1 = "class C { }";
            var src2 = "class C { static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void InstanceCtorInsert_Public_Implicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void InstanceCtorInsert_Public_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void InstanceCtorInsert_Private_Implicit1()
        {
            var src1 = "class C { }";
            var src2 = "class C { private C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "private C()"));
        }

        [Fact]
        public void InstanceCtorInsert_Private_Implicit2()
        {
            var src1 = "class C { }";
            var src2 = "class C { C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "C()"));
        }

        [Fact]
        public void InstanceCtorInsert_Protected_PublicImplicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "protected C()"));
        }

        [Fact]
        public void InstanceCtorInsert_Internal_PublicImplicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "internal C()"));
        }

        [Fact]
        public void InstanceCtorInsert_Internal_ProtectedImplicit()
        {
            var src1 = "abstract class C { }";
            var src2 = "abstract class C { internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstructorVisibility, "internal C()"));
        }

        [Fact]
        public void InstanceCtorUpdate_ProtectedImplicit()
        {
            var src1 = "abstract class C { }";
            var src2 = "abstract class C { protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void InstanceCtorInsert_Private_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } private C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C")
                        .InstanceConstructors.Single(ctor => ctor.DeclaredAccessibility == Accessibility.Private))
                });
        }

        [Fact]
        public void InstanceCtorInsert_Internal_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void InstanceCtorInsert_Protected_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void InstanceCtorInsert_InternalProtected_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } internal protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void StaticCtor_Partial_Delete()
        {
            var srcA1 = "partial class C { static C() { } }";
            var srcB1 = "partial class C {  }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { static C() { } }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single())
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePrivate()
        {
            var srcA1 = "partial class C { C() { } }";
            var srcB1 = "partial class C {  }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { C() { } }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single())
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePublic()
        {
            var srcA1 = "partial class C { public C() { } }";
            var srcB1 = "partial class C {  }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { public C() { } }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single())
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePrivateToPublic()
        {
            var srcA1 = "partial class C { C() { } }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { public C() { } }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: null,
                expectedDiagnostics: new[] { Diagnostic(RudeEditKind.Delete, "partial class C", FeaturesResources.constructor) },
                expectedDeclarationError: null);
        }

        [Fact]
        public void StaticCtor_Partial_Insert()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { static C() { } }";

            var srcA2 = "partial class C { static C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true)
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPublic()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { public C() { } }";

            var srcA2 = "partial class C { public C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPrivate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { private C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_InsertInternal()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { internal C() { } }";

            var srcA2 = "partial class C { internal C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPrivateToPublic()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { public C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: null,
                expectedDiagnostics: new[] { Diagnostic(RudeEditKind.ChangingConstructorVisibility, "public C()") },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPrivateToInternal()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { internal C() { } }";
            var srcB2 = "partial class C { }";

            var edits = GetTopEdits(srcA1, srcA2);

            edits.VerifySemantics(
                activeStatements: ActiveStatementsDescription.Empty,
                targetFrameworks: null,
                additionalOldSources: new[] { srcB1 },
                additionalNewSources: new[] { srcB2 },
                expectedSemanticEdits: null,
                expectedDiagnostics: new[] { Diagnostic(RudeEditKind.ChangingConstructorVisibility, "internal C()") },
                expectedDeclarationError: null);
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer1()
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

    public C()
    {
        F(<N:0.2>c => c + 1</N:0.2>);
    }
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

    public C()
    {
        F(<N:0.2>c => c + 2</N:0.2>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer_Trivia1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer_ExplicitInterfaceImpl1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Insert_Parameterless_LambdaInInitializer1()
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
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")]
        public void InstanceCtor_Partial_Insert_WithParameters_LambdaInInitializer1()
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
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C(int x)"));

            // TODO: bug https://github.com/dotnet/roslyn/issues/2504
            //edits.VerifySemantics(
            //    ActiveStatementsDescription.Empty,
            //    new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [WorkItem(2068, "https://github.com/dotnet/roslyn/issues/2068")]
        [Fact]
        public void Insert_ExternConstruct()
        {
            var src1 = "class C { }";
            var src2 = "class C { public extern C(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public extern C();]@10",
                "Insert [()]@25");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "public extern C()", FeaturesResources.constructor));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/18940")]
        public void ParameterlessConstructor_SemanticError_Delete1()
        {
            var src1 = @"
class C
{
    D() {}
}
";
            var src2 = @"
class C
{
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/18940")]
        public void ParameterlessConstructor_SemanticError_Delete_OutsideOfClass1()
        {
            var src1 = @"
C() {}
";
            var src2 = @"
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Constructor_SemanticError_Partial()
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

            edits.VerifySemanticDiagnostics(
                // (4,18): error CS0542: 'C': member names cannot be the same as their enclosing type
                //     partial void C(int x);
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "C").WithArguments("C").WithLocation(4, 18));
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Constructor_BlockBodyToExpressionBody()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void ConstructorWithInitializer_BlockBodyToExpressionBody()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Constructor_ExpressionBodyToBlockBody()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void ConstructorWithInitializer_ExpressionBodyToBlockBody()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
                });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
                });
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [Test(in int b) => throw null;]@13",
                "Insert [(in int b)]@17",
                "Insert [in int b]@18");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "class Test { Test() => throw null; }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { Test(int b) => throw null; }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@18 -> [in int b]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
        }

        #endregion

        #region Fields and Properties with Initializers

        [Fact]
        public void FieldInitializer_Update1()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [a = 1]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializer_Update1()
        {
            var src1 = "class C { int a { get; } = 0; }";
            var src2 = "class C { int a { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get; } = 0;]@10 -> [int a { get; } = 1;]@10");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializer_Update2()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [a]@14");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyInitializer_Update2()
        {
            var src1 = "class C { int a { get; } = 0; }";
            var src2 = "class C { int a { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get; } = 0;]@10 -> [int a { get { return 1; } }]@10",
                "Update [get;]@18 -> [get { return 1; }]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodBodyAdd, "get", CSharpFeaturesResources.property_getter));
        }

        [Fact]
        public void FieldInitializer_Update3()
        {
            var src1 = "class C { int a; }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializer_Update3()
        {
            var src1 = "class C { int a { get { return 1; } } }";
            var src2 = "class C { int a { get; } = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get { return 1; } }]@10 -> [int a { get; } = 0;]@10",
                "Update [get { return 1; }]@18 -> [get;]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.MethodBodyDelete, "get", CSharpFeaturesResources.property_getter));
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorUpdate1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void PropertyInitializerUpdate_StaticCtorUpdate1()
        {
            var src1 = "class C { static int a { get; } = 1; static C() { } }";
            var src2 = "class C { static int a { get; } = 2;}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate_Private()
        {
            var src1 = "class C { int a; C() { } }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate_Private()
        {
            var src1 = "class C { int a { get; } = 1; C() { } }";
            var src2 = "class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.constructor));
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate_Public()
        {
            var src1 = "class C { int a; public C() { } }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single()) });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate_Public()
        {
            var src1 = "class C { int a { get; } = 1; public C() { } }";
            var src2 = "class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single()) });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorUpdate2()
        {
            var src1 = "class C { static int a; static C() { } }";
            var src2 = "class C { static int a = 0; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_StaticCtorUpdate2()
        {
            var src1 = "class C { static int a { get; } = 1; static C() { } }";
            var src2 = "class C { static int a { get; } = 2; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate2()
        {
            var src1 = "class C { int a; public C() { } }";
            var src2 = "class C { int a = 0; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate2()
        {
            var src1 = "class C { int a { get; } = 1; public C() { } }";
            var src2 = "class C { int a { get; } = 2; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate3()
        {
            var src1 = "class C { int a; }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate3()
        {
            var src1 = "class C { int a { get; } = 1; }";
            var src2 = "class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate4()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [a]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate5()
        {
            var src1 = "class C { int a;     private C(int a) { }    private C(bool a) { } }";
            var src2 = "class C { int a = 0; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate5()
        {
            var src1 = "class C { int a { get; } = 1;     private C(int a) { }    private C(bool a) { } }";
            var src2 = "class C { int a { get; } = 10000; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate6()
        {
            var src1 = "class C { int a;     private C(int a) : this(true) { } private C(bool a) { } }";
            var src2 = "class C { int a = 0; private C(int a) : this(true) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorInsertImplicit()
        {
            var src1 = "class C { static int a; }";
            var src2 = "class C { static int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorInsertExplicit()
        {
            var src1 = "class C { static int a; }";
            var src2 = "class C { static int a = 0; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [static C() { }]@28",
                "Insert [()]@36",
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorInsertExplicit()
        {
            var src1 = "class C { int a; }";
            var src2 = "class C { int a = 0; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorInsertExplicit()
        {
            var src1 = "class C { int a { get; } = 1; }";
            var src2 = "class C { int a { get; } = 2; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_GenericType()
        {
            var src1 = "class C<T> { int a = 1; }";
            var src2 = "class C<T> { int a = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@17 -> [a = 2]@17");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "a = 2", FeaturesResources.field));
        }

        [Fact]
        public void PropertyInitializerUpdate_GenericType()
        {
            var src1 = "class C<T> { int a { get; } = 1; }";
            var src2 = "class C<T> { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeInitializerUpdate, "int a", FeaturesResources.auto_property));
        }

        [Fact]
        public void FieldInitializerUpdate_StackAllocInConstructor()
        {
            var src1 = "unsafe class C { int a = 1; public C() { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a = 2; public C() { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@21 -> [a = 2]@21");

            // TODO (tomat): diagnostic should point to the field initializer
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void FieldInitializerUpdate_SwitchExpressionInConstructor()
        {
            var src1 = "class C { int a = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.SwitchExpressionUpdate, "switch", FeaturesResources.constructor));
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor1()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            // TODO (tomat): diagnostic should point to the property initializer
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor2()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor3()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            // TODO (tomat): diagnostic should point to the property initializer
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor1()
        {
            var src1 = "class C { int a { get; } = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a { get; } = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.SwitchExpressionUpdate, "switch", FeaturesResources.constructor));
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor2()
        {
            var src1 = "class C { int a { get; } = 1; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";
            var src2 = "class C { int a { get; } = 2; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor3()
        {
            var src1 = "class C { int a { get; } = 1; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a { get; } = 2; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.SwitchExpressionUpdate, "switch", FeaturesResources.constructor));
        }

        [Fact]
        public void FieldInitializerUpdate_LambdaInConstructor()
        {
            var src1 = "class C { int a = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
            var src2 = "class C { int a = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@14 -> [a = 2]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_LambdaInConstructor()
        {
            var src1 = "class C { int a { get; } = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
            var src2 = "class C { int a { get; } = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_QueryInConstructor()
        {
            var src1 = "using System.Linq; class C { int a = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
            var src2 = "using System.Linq; class C { int a = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@33 -> [a = 2]@33");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_QueryInConstructor()
        {
            var src1 = "using System.Linq; class C { int a { get; } = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
            var src2 = "using System.Linq; class C { int a { get; } = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_AnonymousTypeInConstructor()
        {
            var src1 = "class C { int a = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
            var src2 = "class C { int a = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_AnonymousTypeInConstructor()
        {
            var src1 = "class C { int a { get; } = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
            var src2 = "class C { int a { get; } = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_PartialTypeWithSingleDeclaration()
        {
            var src1 = "partial class C { int a = 1; }";
            var src2 = "partial class C { int a = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@22 -> [a = 2]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "a = 2", FeaturesResources.field));
        }

        [Fact]
        public void PropertyInitializerUpdate_PartialTypeWithSingleDeclaration()
        {
            var src1 = "partial class C { int a { get; } = 1; }";
            var src2 = "partial class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "int a { get; } = 2;", FeaturesResources.auto_property));
        }

        [Fact]
        public void FieldInitializerUpdate_PartialTypeWithMultipleDeclarations()
        {
            var src1 = "partial class C { int a = 1; } partial class C { }";
            var src2 = "partial class C { int a = 2; } partial class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@22 -> [a = 2]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "a = 2", FeaturesResources.field));
        }

        [Fact]
        public void PropertyInitializerUpdate_PartialTypeWithMultipleDeclarations()
        {
            var src1 = "partial class C { int a { get; } = 1; } partial class C { }";
            var src2 = "partial class C { int a { get; } = 2; } partial class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.PartialTypeInitializerUpdate, "int a { get; } = 2;", FeaturesResources.auto_property));
        }

        [Fact]
        public void FieldInitializerUpdate_ParenthesizedLambda()
        {
            var src1 = "class C { int a = F(1, (x, y) => x + y); }";
            var src2 = "class C { int a = F(2, (x, y) => x + y); }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_ParenthesizedLambda()
        {
            var src1 = "class C { int a { get; } = F(1, (x, y) => x + y); }";
            var src2 = "class C { int a { get; } = F(2, (x, y) => x + y); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_SimpleLambda()
        {
            var src1 = "class C { int a = F(1, x => x); }";
            var src2 = "class C { int a = F(2, x => x); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_SimpleLambda()
        {
            var src1 = "class C { int a { get; } = F(1, x => x); }";
            var src2 = "class C { int a { get; } = F(2, x => x); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_Query()
        {
            var src1 = "class C { int a = F(1, from goo in bar select baz); }";
            var src2 = "class C { int a = F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_Query()
        {
            var src1 = "class C { int a { get; } = F(1, from goo in bar select baz); }";
            var src2 = "class C { int a { get; } = F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_AnonymousType()
        {
            var src1 = "class C { int a = F(1, new { A = 1, B = 2 }); }";
            var src2 = "class C { int a = F(2, new { A = 1, B = 2 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_AnonymousType()
        {
            var src1 = "class C { int a { get; } = F(1, new { A = 1, B = 2 }); }";
            var src2 = "class C { int a { get; } = F(2, new { A = 1, B = 2 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithLambda1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithoutLambda1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C() {}
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C() {}
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithoutLambda1()
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
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializers_EditInitializerWithLambda1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda_Trivia1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda_Trivia1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithoutLambda1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_EditConstructorNotIncludingInitializers()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_RemoveCtorInitializer1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_AddCtorInitializer1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_UpdateBaseCtorInitializerWithLambdas1()
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_ActiveStatements1()
        {
            var src1 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0></AS:0>;
    int B = 1;

    public C(int a) { Console.WriteLine(1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0></AS:0>;
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
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors[1], syntaxMap[0]),
                });
        }

        [Fact]
        public void PropertyWithInitializer_SemanticError_Partial()
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
    partial int P => 1;

    public C() { }
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                // (4,5): error CS0106: The modifier 'partial' is not valid for this item
                //     partial int P => 1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "partial").WithArguments("partial").WithLocation(4, 5)
                );
        }

        #endregion

        #region Fields

        [Fact]
        public void FieldNameUpdate1()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int b = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [b = 0]@14");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b = 0", FeaturesResources.field));
        }

        [Fact]
        public void FieldUpdate_FieldKind()
        {
            var src1 = "class C { Action a; }";
            var src2 = "class C { event Action a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Action a;]@10 -> [event Action a;]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.FieldKindUpdate, "event Action a", CSharpFeaturesResources.event_field));
        }

        [Fact]
        public void EventFieldUpdate_VariableDeclarator()
        {
            var src1 = "class C { event Action a; }";
            var src2 = "class C { event Action a = () => { }; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@23 -> [a = () => { }]@23");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldReorder()
        {
            var src1 = "class C { int a = 0; int b = 1; int c = 2; }";
            var src2 = "class C { int c = 2; int a = 0; int b = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int c = 2;]@32 -> @10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "int c = 2", FeaturesResources.field));
        }

        [Fact]
        public void FieldInsert_Private()
        {
            var src1 = "class C {  }";
            var src2 = "class C { int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int a = 1;]@10",
                "Insert [int a = 1]@10",
                "Insert [a = 1]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.a")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInsert_PrivateReadonly()
        {
            var src1 = "class C {  }";
            var src2 = "class C { private readonly int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private readonly int a = 1;]@10",
                "Insert [int a = 1]@27",
                "Insert [a = 1]@31");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInsert_Public()
        {
            var src1 = "class C {  }";
            var src2 = "class C { public int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public int a = 1;]@10",
                "Insert [int a = 1]@17",
                "Insert [a = 1]@21");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInsert_Protected()
        {
            var src1 = "class C {  }";
            var src2 = "class C { protected int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [protected int a = 1;]@10",
                "Insert [int a = 1]@20",
                "Insert [a = 1]@24");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void FieldInsert_IntoStruct()
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
        public void FieldInsert_IntoLayoutClass_Auto()
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
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.b")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.c")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.d")),
                });
        }

        [Fact]
        public void FieldInsert_IntoLayoutClass_Explicit()
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
        public void FieldInsert_IntoLayoutClass_Sequential()
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
        public void FieldInsert_WithInitializersAndLambdas1()
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
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInsert_ParameterlessConstructorInsert_WithInitializersAndLambdas1()
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

    public C()                                // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
                });
        }

        [Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")]
        public void FieldInsert_ConstructorInsert_WithInitializersAndLambdas1()
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
            var syntaxMap = GetSyntaxMap(src1, src2);

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

        [Fact, WorkItem(2504, "https://github.com/dotnet/roslyn/issues/2504")]
        public void FieldInsert_ConstructorInsert_WithInitializersButNoExistingLambdas1()
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
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single())
                });
        }

        [Fact]
        public void FieldDelete1()
        {
            var src1 = "class C { int a = 1; }";
            var src2 = "class C {  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int a = 1;]@10",
                "Delete [int a = 1]@10",
                "Delete [a = 1]@14");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.field));
        }

        [Fact]
        public void FieldUnsafeModifierUpdate()
        {
            var src1 = "struct Node { unsafe Node* left; }";
            var src2 = "struct Node { Node* left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [unsafe Node* left;]@14 -> [Node* left;]@14");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Node* left", FeaturesResources.field));
        }

        [Fact]
        public void FieldModifierAndTypeUpdate()
        {
            var src1 = "struct Node { unsafe Node* left; }";
            var src2 = "struct Node { Node left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [unsafe Node* left;]@14 -> [Node left;]@14",
                "Update [Node* left]@21 -> [Node left]@14");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "Node left", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "Node left", FeaturesResources.field));
        }

        [Fact]
        public void FieldTypeUpdateNullable()
        {
            var src1 = "class C { int left; }";
            var src2 = "class C { int? left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int left]@10 -> [int? left]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "int? left", FeaturesResources.field));
        }

        [Fact]
        public void FieldTypeUpdateNonNullable()
        {
            var src1 = "class C { int? left; }";
            var src2 = "class C { int left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int? left]@10 -> [int left]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "int left", FeaturesResources.field));
        }

        [Fact]
        public void EventFieldReorder()
        {
            var src1 = "class C { int a = 0; int b = 1; event int c = 2; }";
            var src2 = "class C { event int c = 2; int a = 0; int b = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [event int c = 2;]@32 -> @10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "event int c = 2", CSharpFeaturesResources.event_field));
        }

        #endregion

        #region Properties

        [Fact]
        public void PropertyWithExpressionBody_Update()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P => 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int P => 1;]@10 -> [int P => 2;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
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
                "Insert [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
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
                "Insert [set { }]@36");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int P { get { return 2; } } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get { return 2; } }]@10 -> [int P => 1;]@10",
                "Delete [{ get { return 2; } }]@16",
                "Delete [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int P { get { return 2; } set { } } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get { return 2; } set { } }]@10 -> [int P => 1;]@10",
                "Delete [{ get { return 2; } set { } }]@16",
                "Delete [get { return 2; }]@18",
                "Delete [set { }]@36");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "int P", CSharpFeaturesResources.property_setter));
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_ExpressionBodyToGetterExpressionBody()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P { get => 2; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P => 1;]@10 -> [int P { get => 2; }]@10",
                "Insert [{ get => 2; }]@16",
                "Insert [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyToExpressionBody()
        {
            var src1 = "class C { int P { get => 2; } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int P { get => 2; }]@10 -> [int P => 1;]@10",
                "Delete [{ get => 2; }]@16",
                "Delete [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterBlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int P { get { return 2; } } }";
            var src2 = "class C { int P { get => 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyToGetterBlockBody()
        {
            var src1 = "class C { int P { get => 2; } }";
            var src2 = "class C { int P { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterBlockBodyWithSetterToGetterExpressionBodyWithSetter()
        {
            var src1 = "class C { int P { get => 2;         set { Console.WriteLine(0); } } }";
            var src2 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyWithSetterToGetterBlockBodyWithSetter()
        {
            var src1 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int P { get => 2; set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void PropertyRename1()
        {
            var src1 = "class C { int P { get { return 1; } } }";
            var src2 = "class C { int Q { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int Q", FeaturesResources.property_));
        }

        [Fact]
        public void PropertyRename2()
        {
            var src1 = "class C { int I.P { get { return 1; } } }";
            var src2 = "class C { int J.P { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int J.P", FeaturesResources.property_));
        }

        [Fact]
        public void PropertyReorder1()
        {
            var src1 = "class C { int P { get { return 1; } } int Q { get { return 1; } }  }";
            var src2 = "class C { int Q { get { return 1; } } int P { get { return 1; } }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int Q { get { return 1; } }]@38 -> @10");

            // TODO: we can allow the move since the property doesn't have a backing field
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.property_));
        }

        [Fact]
        public void PropertyReorder2()
        {
            var src1 = "class C { int P { get; set; } int Q { get; set; }  }";
            var src2 = "class C { int Q { get; set; } int P { get; set; }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int Q { get; set; }]@30 -> @10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.auto_property));
        }

        [Fact]
        public void PropertyAccessorReorder()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { int P { set { } get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [set { }]@36 -> @18");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PropertyTypeUpdate()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { char P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get; set; }]@10 -> [char P { get; set; }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "char P", FeaturesResources.auto_property));
        }

        [WorkItem(835827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        [Fact]
        public void PropertyInsert_PInvoke()
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
    private static extern int P { [DllImport(""msvcrt.dll"")]get; }
}
";
            var edits = GetTopEdits(src1, src2);

            // CLR doesn't support methods without a body
            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int P", FeaturesResources.property_));
        }

        [Fact]
        public void Property_InsertIntoStruct()
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
    public S(int z) { a = z; }
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int c { get; set; }", FeaturesResources.auto_property, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int g { get; } = 1;", FeaturesResources.auto_property, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int i { get; set; } = 1;", FeaturesResources.auto_property, CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void PropertyInsert_IntoLayoutClass_Sequential()
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
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int b { get; set; }", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int c { get; set; }", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int f { get; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int g { get; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int h { get; set; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int i { get; set; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_));
        }

        // Design: Adding private accessors should also be allowed since we now allow adding private methods
        // and adding public properties and/or public accessors are not allowed.
        [Fact]
        public void PrivateProperty_AccessorAdd()
        {
            var src1 = "class C { int _p; int P { get { return 1; } } }";
            var src2 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set { _p = value; }]@44");

            edits.VerifyRudeDiagnostics();
        }

        [WorkItem(755975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
        [Fact]
        public void PrivatePropertyAccessorDelete()
        {
            var src1 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";
            var src2 = "class C { int _p; int P { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [set { _p = value; }]@44");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "int P", CSharpFeaturesResources.property_setter));
        }

        [Fact]
        public void PrivateAutoPropertyAccessorAdd1()
        {
            var src1 = "class C { int P { get; } }";
            var src2 = "class C { int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@23");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PrivateAutoPropertyAccessorAdd2()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; private set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [private set;]@30");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PrivateAutoPropertyAccessorAdd4()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@30");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PrivateAutoPropertyAccessorAdd5()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; internal set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [internal set;]@30");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void PrivateAutoPropertyAccessorAdd6()
        {
            var src1 = "class C { int P { get; } = 1; }";
            var src2 = "class C { int P { get; set; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@23");

            edits.VerifyRudeDiagnostics();
        }

        [WorkItem(755975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
        [Fact]
        public void PrivateAutoPropertyAccessorDelete1()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [get;]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "int P", CSharpFeaturesResources.property_getter));
        }

        [Fact]
        public void PrivateAutoPropertyAccessorDelete2()
        {
            var src1 = "class C { int P { get; set; } = 1; }";
            var src2 = "class C { int P { set; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [get;]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "int P", CSharpFeaturesResources.property_getter));
        }

        [Fact]
        public void AutoPropertyAccessorUpdate()
        {
            var src1 = "class C { int P { get; } }";
            var src2 = "class C { int P { set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get;]@18 -> [set;]@18");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.AccessorKindUpdate, "set", CSharpFeaturesResources.property_setter));
        }

        [WorkItem(992578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992578")]
        [Fact]
        public void InsertIncompleteProperty()
        {
            var src1 = "class C { }";
            var src2 = "class C { public int P { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [public int P { }]@10", "Insert [{ }]@23");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Property_ReadOnlyRef_Insert()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { ref readonly int M() { get; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref readonly int M() { get; }]@13",
                "Insert [()]@31");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Property_ReadOnlyRef_Update()
        {
            var src1 = "class Test { int M() { get; } }";
            var src2 = "class Test { ref readonly int M() { get; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int M() { get; }]@13 -> [ref readonly int M() { get; }]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "ref readonly int M()", FeaturesResources.method));
        }

        #endregion

        #region Indexers

        [Fact]
        public void Indexer_GetterUpdate()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [get { return 1; }]@28 -> [get { return 2; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Indexer_SetterUpdate()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value); } } }";
            var src2 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value + 1); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [set { System.Console.WriteLine(value); }]@46 -> [set { System.Console.WriteLine(value + 1); }]@46");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void IndexerWithExpressionBody_Update()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] => 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] => 2;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToBlockBody()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } }]@10",
                "Insert [{ get { return 1; } }]@26",
                "Insert [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_BlockBodyToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] => 1; } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get { return 1; } }]@10 -> [int this[int a] => 1;]@10",
                "Delete [{ get { return 1; } }]@26",
                "Delete [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToBlockBody()
        {
            var src1 = "class C { int this[int a] { get => 1; } }";
            var src2 = "class C { int this[int a] { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_BlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] { get => 1; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");
            edits.VerifyRudeDiagnostics();
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get => 1; } }";
            var src2 = "class C { int this[int a] => 1; } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get => 1; }]@10 -> [int this[int a] => 1;]@10",
                "Delete [{ get => 1; }]@26",
                "Delete [get => 1;]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get => 1; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get => 1; }]@10",
                "Insert [{ get => 1; }]@26",
                "Insert [get => 1;]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterBlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] { get => 1;         set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToGetterBlockBody()
        {
            var src1 = "class C { int this[int a] { get => 1; set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterAndSetterBlockBodiesToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] => 1; }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10 -> [int this[int a] => 1;]@10",
                "Delete [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
                "Delete [get { return 1; }]@28",
                "Delete [set { Console.WriteLine(0); }]@46");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "int this[int a]", CSharpFeaturesResources.indexer_setter));
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToGetterAndSetterBlockBodies()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10",
                "Insert [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
                "Insert [get { return 1; }]@28",
                "Insert [set { Console.WriteLine(0); }]@46");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Indexer_Rename()
        {
            var src1 = "class C { int I.this[int a] { get { return 1; } } }";
            var src2 = "class C { int J.this[int a] { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int J.this[int a]", CSharpFeaturesResources.indexer));
        }

        [Fact]
        public void Indexer_Reorder1()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } int this[string a] { get { return 1; } }  }";
            var src2 = "class C { int this[string a] { get { return 1; } } int this[int a] { get { return 1; } }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int this[string a] { get { return 1; } }]@48 -> @10");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Indexer_AccessorReorder()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { } } }";
            var src2 = "class C { int this[int a] { set { } get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [set { }]@46 -> @28");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Indexer_TypeUpdate()
        {
            var src1 = "class C { int this[int a] { get; set; } }";
            var src2 = "class C { string this[int a] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get; set; }]@10 -> [string this[int a] { get; set; }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "string this[int a]", CSharpFeaturesResources.indexer));
        }

        [Fact]
        public void Tuple_TypeUpdate()
        {
            var src1 = "class C { (int, int) M() { throw new System.Exception(); } }";
            var src2 = "class C { (string, int) M() { throw new System.Exception(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int) M() { throw new System.Exception(); }]@10 -> [(string, int) M() { throw new System.Exception(); }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "(string, int) M()", FeaturesResources.method));
        }

        [Fact]
        public void TupleNames_TypeUpdate()
        {
            var src1 = "class C { (int a, int) M() { throw new System.Exception(); } }";
            var src2 = "class C { (int notA, int) M() { throw new System.Exception(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int a, int) M() { throw new System.Exception(); }]@10 -> [(int notA, int) M() { throw new System.Exception(); }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "(int notA, int) M()", FeaturesResources.method));
        }

        [Fact]
        public void TupleElementDelete()
        {
            var src1 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";
            var src2 = "class C { (int, int) M() { return (1, 2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int, int a) M() { return (1, 2, 3); }]@10 -> [(int, int) M() { return (1, 2); }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "(int, int) M()", FeaturesResources.method));
        }

        [Fact]
        public void TupleElementAdd()
        {
            var src1 = "class C { (int, int) M() { return (1, 2); } }";
            var src2 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int) M() { return (1, 2); }]@10 -> [(int, int, int a) M() { return (1, 2, 3); }]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "(int, int, int a) M()", FeaturesResources.method));
        }

        [Fact]
        public void Indexer_ParameterUpdate()
        {
            var src1 = "class C { int this[int a] { get; set; } }";
            var src2 = "class C { int this[string a] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "string a", FeaturesResources.parameter));
        }

        [Fact]
        public void Indexer_AddGetAccessor()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [get { return arr[i]; }]@304");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Indexer_AddSetAccessor()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        System.Console.Write(stringCollection[0]);
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        System.Console.Write(stringCollection[0]);
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set { arr[i] = value; }]@348");

            edits.VerifyRudeDiagnostics();
        }

        [WorkItem(750109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750109")]
        [Fact]
        public void Indexer_DeleteGetAccessor()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [get { return arr[i]; }]@304");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public T this[int i]", CSharpFeaturesResources.indexer_getter));
        }

        [Fact]
        public void Indexer_DeleteSetAccessor()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [set { arr[i] = value; }]@336");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public T this[int i]", CSharpFeaturesResources.indexer_setter));
        }

        [Fact, WorkItem(1174850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174850")]
        public void Indexer_Insert()
        {
            var src1 = "struct C { }";
            var src2 = "struct C { public int this[int x, int y] { get { return x + y; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics();
        }

        [WorkItem(1120407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1120407")]
        [Fact]
        public void ConstField_Update()
        {
            var src1 = "class C { const int x = 0; }";
            var src2 = "class C { const int x = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [x = 0]@20 -> [x = 1]@20");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "x = 1", FeaturesResources.const_field));
        }

        [Fact]
        public void ConstField_Delete()
        {
            var src1 = "class C { const int x = 0; }";
            var src2 = "class C { int x = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [const int x = 0;]@10 -> [int x = 0;]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "int x = 0", FeaturesResources.field));
        }

        [Fact]
        public void ConstField_Add()
        {
            var src1 = "class C { int x = 0; }";
            var src2 = "class C { const int x = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int x = 0;]@10 -> [const int x = 0;]@10");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int x = 0", FeaturesResources.const_field));
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
                "Insert [in int i]@22");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Indexer_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { int this[int i] => throw null; }";
            var src2 = "class Test { int this[in int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int i]@22 -> [in int i]@22");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int i", FeaturesResources.parameter));
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
                "Insert [int i]@35");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void Indexer_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "class Test { int this[int i] => throw null; }";
            var src2 = "class Test { ref readonly int this[int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int i] => throw null;]@13 -> [ref readonly int this[int i] => throw null;]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "ref readonly int this[int i]", FeaturesResources.indexer_));
        }

        #endregion

        #region Events

        [Fact]
        public void EventAccessorReorder1()
        {
            var src1 = "class C { event int E { add { } remove { } } }";
            var src2 = "class C { event int E { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [remove { }]@32 -> @24");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void EventAccessorReorder2()
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
        public void EventAccessorReorder3()
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
        public void EventInsert_IntoLayoutClass_Sequential()
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

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
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

        [Fact, WorkItem(17681, "https://github.com/dotnet/roslyn/issues/17681")]
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

        #endregion

        #region Parameter

        [Fact]
        public void ParameterRename_Method1()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int a]@24 -> [int b]@24");
        }

        [Fact]
        public void ParameterRename_Ctor1()
        {
            var src1 = @"class C { public C(int a) {} }";
            var src2 = @"class C { public C(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int a]@19 -> [int b]@19");
        }

        [Fact]
        public void ParameterRename_Operator1()
        {
            var src1 = @"class C { public static implicit operator int(C a) {} }";
            var src2 = @"class C { public static implicit operator int(C b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [C a]@46 -> [C b]@46");
        }

        [Fact]
        public void ParameterRename_Operator2()
        {
            var src1 = @"class C { public static int operator +(C a, C b) { return 0; } }";
            var src2 = @"class C { public static int operator +(C a, C x) { return 0; } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [C b]@44 -> [C x]@44");
        }

        [Fact]
        public void ParameterRename_Indexer2()
        {
            var src1 = @"class C { public int this[int a, int b] { get { return 0; } } }";
            var src2 = @"class C { public int this[int a, int x] { get { return 0; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int b]@33 -> [int x]@33");
        }

        [Fact]
        public void ParameterModifierUpdate1()
        {
            var src1 = @"class C { public int this[int a, ref int b] { get { return 0; } } }";
            var src2 = @"class C { public int this[int a, int b] { get { return 0; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [ref int b]@33 -> [int b]@33");
        }

        [Fact]
        public void ParameterInsert1()
        {
            var src1 = @"class C { public void M() {} }";
            var src2 = @"class C { public void M(int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Insert [int a]@24");
        }

        [Fact]
        public void ParameterInsert2()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M(int a, ref int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [(int a)]@23 -> [(int a, ref int b)]@23",
                "Insert [ref int b]@31");
        }

        [Fact]
        public void ParameterDelete1()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [int a]@24");
        }

        [Fact]
        public void ParameterDelete2()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [(int a, int b)]@23 -> [(int b)]@23",
                "Delete [int a]@24");
        }

        [Fact]
        public void ParameterUpdate()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int a]@24 -> [int b]@24");
        }

        [Fact]
        public void ParameterReorder()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b, int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [int b]@31 -> @24");
        }

        [Fact]
        public void ParameterReorderAndUpdate()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b, int c) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [int b]@31 -> @24",
                "Update [int a]@24 -> [int c]@31");
        }

        [Fact]
        public void ParameterAttributeInsert1()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M([A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Insert [[A]]@24",
                "Insert [A]@25");
        }

        [Fact]
        public void ParameterAttributeInsert2()
        {
            var src1 = @"class C { public void M([A]int a) {} }";
            var src2 = @"class C { public void M([A, B]int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [[A]]@24 -> [[A, B]]@24",
                "Insert [B]@28");
        }

        [Fact]
        public void ParameterAttributeDelete()
        {
            var src1 = @"class C { public void M([A]int a) {} }";
            var src2 = @"class C { public void M(int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [[A]]@24",
                "Delete [A]@25");
        }

        [Fact]
        public void ParameterAttributeUpdate()
        {
            var src1 = @"class C { public void M([A(1), C]int a) {} }";
            var src2 = @"class C { public void M([A(2), B]int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [A(1)]@25 -> [A(2)]@25",
                "Update [C]@31 -> [B]@31");
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
        }

        [Fact]
        public void MethodTypeParameterUpdate()
        {
            var src1 = @"class C { public void M<A>() {} }";
            var src2 = @"class C { public void M<B>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [A]@24 -> [B]@24");
        }

        [Fact]
        public void MethodTypeParameterReorder()
        {
            var src1 = @"class C { public void M<A,B>() {} }";
            var src2 = @"class C { public void M<B,A>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [B]@26 -> @24");
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
        }

        [Fact]
        public void MethodTypeParameterAttributeInsert1()
        {
            var src1 = @"class C { public void M<T>() {} }";
            var src2 = @"class C { public void M<[A]T>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Insert [[A]]@24",
                "Insert [A]@25");
        }

        [Fact]
        public void MethodTypeParameterAttributeInsert2()
        {
            var src1 = @"class C { public void M<[A]T>() {} }";
            var src2 = @"class C { public void M<[A, B]T>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [[A]]@24 -> [[A, B]]@24",
                "Insert [B]@28");
        }

        [Fact]
        public void MethodTypeParameterAttributeDelete()
        {
            var src1 = @"class C { public void M<[A]T>() {} }";
            var src2 = @"class C { public void M<T>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [[A]]@24",
                "Delete [A]@25");
        }

        [Fact]
        public void MethodTypeParameterAttributeUpdate()
        {
            var src1 = @"class C { public void M<[A(1), C]T>() {} }";
            var src2 = @"class C { public void M<[A(2), B]T>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [A(1)]@25 -> [A(2)]@25",
                "Update [C]@31 -> [B]@31");
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "<A>", FeaturesResources.type_parameter));
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterDelete1()
        {
            var src1 = @"class C<A> { }";
            var src2 = @"class C { } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [<A>]@7",
                "Delete [A]@8");
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<B>", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterUpdate()
        {
            var src1 = @"class C<A> {}";
            var src2 = @"class C<B> {} ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A]@8 -> [B]@8");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterReorder()
        {
            var src1 = @"class C<A,B> { }";
            var src2 = @"class C<B,A> { } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [B]@10 -> @8");

            edits.VerifyRudeDiagnostics(
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

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.Renamed, "C", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterAttributeInsert1()
        {
            var src1 = @"class C<T> {}";
            var src2 = @"class C<[A]T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[A]]@8",
                "Insert [A]@9");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[A]", FeaturesResources.attribute));
        }

        [Fact]
        public void TypeTypeParameterAttributeInsert2()
        {
            var src1 = @"class C<[A]T> {}";
            var src2 = @"class C<[A, B]T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]]@8 -> [[A, B]]@8",
                "Insert [B]@12");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.attribute));
        }

        [Fact]
        public void TypeTypeParameterAttributeDelete()
        {
            var src1 = @"class C<[A]T> {}";
            var src2 = @"class C<T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [[A]]@8",
                "Delete [A]@9");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "T", FeaturesResources.attribute));
        }

        [Fact]
        public void TypeTypeParameterAttributeUpdate()
        {
            var src1 = @"class C<[A(1), C]T> {}";
            var src2 = @"class C<[A(2), B]T> {} ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A(1)]@9 -> [A(2)]@9",
                "Update [C]@15 -> [B]@15");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Update, "A(2)", FeaturesResources.attribute),
                Diagnostic(RudeEditKind.Update, "B", FeaturesResources.attribute));
        }

        #endregion

        #region Type Parameter Constraints

        [Fact]
        public void TypeConstraintInsert_Class()
        {
            var src1 = "class C<T> { }";
            var src2 = "class C<T> where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where T : class]@11");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "where T : class", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintInsert_Unmanaged()
        {
            var src1 = "class C<T> { }";
            var src2 = "class C<T> where T : unmanaged { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where T : unmanaged]@11");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "where T : unmanaged", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintInsert_DoubleStatement_New()
        {
            var src1 = "class C<S,T> where T : class { }";
            var src2 = "class C<S,T> where S : new() where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where S : new()]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "where S : new()", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintInsert_DoubleStatement_Unmanaged()
        {
            var src1 = "class C<S,T> where T : class { }";
            var src2 = "class C<S,T> where S : unmanaged where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where S : unmanaged]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Insert, "where S : unmanaged", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintDelete_Class()
        {
            var src1 = "class C<S,T> where T : class { }";
            var src2 = "class C<S,T> { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where T : class]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<S,T>", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintDelete_Unmanaged()
        {
            var src1 = "class C<S,T> where T : unmanaged { }";
            var src2 = "class C<S,T> { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where T : unmanaged]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<S,T>", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintDelete_DoubleStatement_New()
        {
            var src1 = "class C<S,T> where S : new() where T : class  { }";
            var src2 = "class C<S,T> where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where S : new()]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<S,T>", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintDelete_DoubleStatement_Unmanaged()
        {
            var src1 = "class C<S,T> where S : unmanaged where T : class  { }";
            var src2 = "class C<S,T> where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where S : unmanaged]@13");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<S,T>", FeaturesResources.type_constraint));
        }

        [Fact]
        public void TypeConstraintReorder_Class()
        {
            var src1 = "class C<S,T> where S : struct where T : class  { }";
            var src2 = "class C<S,T> where T : class where S : struct { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [where T : class]@30 -> @13");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void TypeConstraintReorder_Unmanaged()
        {
            var src1 = "class C<S,T> where S : struct where T : unmanaged  { }";
            var src2 = "class C<S,T> where T : unmanaged where S : struct { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [where T : unmanaged]@30 -> @13");

            edits.VerifyRudeDiagnostics();
        }

        [Fact]
        public void TypeConstraintUpdateAndReorder()
        {
            var src1 = "class C<S,T> where S : new() where T : class  { }";
            var src2 = "class C<T,S> where T : class, I where S : class, new() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [where T : class]@29 -> @13",
                "Reorder [T]@10 -> @8",
                "Update [where T : class]@29 -> [where T : class, I]@13",
                "Update [where S : new()]@13 -> [where S : class, new()]@32");

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Move, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.TypeUpdate, "where T : class, I", FeaturesResources.type_constraint),
                Diagnostic(RudeEditKind.TypeUpdate, "where S : class, new()", FeaturesResources.type_constraint));
        }

        #endregion
    }
}
