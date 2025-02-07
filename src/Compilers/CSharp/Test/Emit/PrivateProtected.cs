// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using static Roslyn.Test.Utilities.SigningTestHelpers;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.PrivateProtected)]
    public class PrivateProtected : CSharpTestBase
    {
        private static readonly string s_keyPairFile = SigningTestHelpers.KeyPairFile;
        private static readonly string s_publicKeyFile = SigningTestHelpers.PublicKeyFile;
        private static readonly ImmutableArray<byte> s_publicKey = SigningTestHelpers.PublicKey;

        [ConditionalFact(typeof(DesktopOnly))]
        public void RejectIncompatibleModifiers()
        {
            string source =
@"public class Base
{
    private internal int Field1;
    internal private int Field2;
    private internal protected int Field3;
    internal protected private int Field4;
    private public protected int Field5;
    private readonly protected int Field6; // ok
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (3,26): error CS0107: More than one protection modifier
                //     private internal int Field1;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field1").WithLocation(3, 26),
                // (4,26): error CS0107: More than one protection modifier
                //     internal private int Field2;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field2").WithLocation(4, 26),
                // (5,36): error CS0107: More than one protection modifier
                //     private internal protected int Field3;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field3").WithLocation(5, 36),
                // (6,36): error CS0107: More than one protection modifier
                //     internal protected private int Field4;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field4").WithLocation(6, 36),
                // (7,34): error CS0107: More than one protection modifier
                //     private public protected int Field5;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field5").WithLocation(7, 34)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void AccessibleWhereRequired_01()
        {
            string source =
@"public class Base
{
    private protected int Field1;
    protected private int Field2;
}

public class Derived : Base
{
    void M()
    {
        Field1 = 1;
        Field2 = 2;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void AccessibleWhereRequired_02()
        {
            string source1 =
@"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""WantsIVTAccess"")]
public class Base
{
    private protected const int Constant = 3;
    private protected int Field1;
    protected private int Field2;
    private protected void Method() { }
    private protected event System.Action Event1;
    private protected int Property1 { set {} }
    public int Property2 { private protected set {} get { return 4; } }
    private protected int this[int x] { set { } get { return 6; } }
    public int this[string x] { private protected set { } get { return 5; } }
    private protected Base() { Event1?.Invoke(); }
}";
            var baseCompilation = CreateCompilation(source1, parseOptions: TestOptions.Regular7_2,
                options: TestOptions.SigningReleaseDll,
                assemblyName: "Paul");
            var bb = (NamedTypeSymbol)baseCompilation.GlobalNamespace.GetMember("Base");
            foreach (var member in bb.GetMembers())
            {
                switch (member.Name)
                {
                    case "Property2":
                    case "get_Property2":
                    case "this[]":
                    case "get_Item":
                        break;
                    default:
                        Assert.Equal(Accessibility.ProtectedAndInternal, member.DeclaredAccessibility);
                        break;
                }
            }

            string source2 =
@"public class Derived : Base
{
    void M()
    {
        Field1 = Constant;
        Field2 = Constant;
        Method();
        Event1 += null;
        Property1 = Constant;
        Property2 = Constant;
        this[1] = 2;
        this[string.Empty] = 4;
    }
    Derived(int x) : base() {}
    Derived(long x) {} // implicit base()
}
";
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { new CSharpCompilationReference(baseCompilation) },
                assemblyName: "WantsIVTAccessButCantHave",
                options: TestOptions.SigningReleaseDll)
            .VerifyDiagnostics(
                // (5,9): error CS0122: 'Base.Field1' is inaccessible due to its protection level
                //         Field1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field1").WithArguments("Base.Field1").WithLocation(5, 9),
                // (5,18): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Field1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(5, 18),
                // (6,9): error CS0122: 'Base.Field2' is inaccessible due to its protection level
                //         Field2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field2").WithArguments("Base.Field2").WithLocation(6, 9),
                // (6,18): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Field2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(6, 18),
                // (7,9): error CS0122: 'Base.Method()' is inaccessible due to its protection level
                //         Method();
                Diagnostic(ErrorCode.ERR_BadAccess, "Method").WithArguments("Base.Method()").WithLocation(7, 9),
                // (8,9): error CS0122: 'Base.Event1' is inaccessible due to its protection level
                //         Event1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "Event1").WithArguments("Base.Event1").WithLocation(8, 9),
                // (8,9): error CS0122: 'Base.Event1.add' is inaccessible due to its protection level
                //         Event1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "Event1 += null").WithArguments("Base.Event1.add").WithLocation(8, 9),
                // (9,9): error CS0122: 'Base.Property1' is inaccessible due to its protection level
                //         Property1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Property1").WithArguments("Base.Property1").WithLocation(9, 9),
                // (9,21): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Property1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(9, 21),
                // (10,9): error CS0272: The property or indexer 'Base.Property2' cannot be used in this context because the set accessor is inaccessible
                //         Property2 = Constant;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "Property2").WithArguments("Base.Property2").WithLocation(10, 9),
                // (10,21): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Property2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(10, 21),
                // (11,14): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         this[1] = 2;
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(11, 14),
                // (12,9): error CS0272: The property or indexer 'Base.this[string]' cannot be used in this context because the set accessor is inaccessible
                //         this[string.Empty] = 4;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "this[string.Empty]").WithArguments("Base.this[string]").WithLocation(12, 9),
                // (14,22): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                //     Derived(int x) : base() {}
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("Base.Base()").WithLocation(14, 22),
                // (15,5): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                //     Derived(long x) {} // implicit base()
                Diagnostic(ErrorCode.ERR_BadAccess, "Derived").WithArguments("Base.Base()").WithLocation(15, 5)
                );
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { MetadataReference.CreateFromImage(baseCompilation.EmitToArray()) },
                assemblyName: "WantsIVTAccessButCantHave",
                options: TestOptions.SigningReleaseDll)
            .VerifyDiagnostics(
                // (5,9): error CS0122: 'Base.Field1' is inaccessible due to its protection level
                //         Field1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field1").WithArguments("Base.Field1").WithLocation(5, 9),
                // (5,18): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Field1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(5, 18),
                // (6,9): error CS0122: 'Base.Field2' is inaccessible due to its protection level
                //         Field2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field2").WithArguments("Base.Field2").WithLocation(6, 9),
                // (6,18): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Field2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(6, 18),
                // (7,9): error CS0122: 'Base.Method()' is inaccessible due to its protection level
                //         Method();
                Diagnostic(ErrorCode.ERR_BadAccess, "Method").WithArguments("Base.Method()").WithLocation(7, 9),
                // (8,9): error CS0122: 'Base.Event1' is inaccessible due to its protection level
                //         Event1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "Event1").WithArguments("Base.Event1").WithLocation(8, 9),
                // (8,9): error CS0122: 'Base.Event1.add' is inaccessible due to its protection level
                //         Event1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "Event1 += null").WithArguments("Base.Event1.add").WithLocation(8, 9),
                // (9,9): error CS0122: 'Base.Property1' is inaccessible due to its protection level
                //         Property1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Property1").WithArguments("Base.Property1").WithLocation(9, 9),
                // (9,21): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Property1 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(9, 21),
                // (10,9): error CS0272: The property or indexer 'Base.Property2' cannot be used in this context because the set accessor is inaccessible
                //         Property2 = Constant;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "Property2").WithArguments("Base.Property2").WithLocation(10, 9),
                // (10,21): error CS0122: 'Base.Constant' is inaccessible due to its protection level
                //         Property2 = Constant;
                Diagnostic(ErrorCode.ERR_BadAccess, "Constant").WithArguments("Base.Constant").WithLocation(10, 21),
                // (11,14): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         this[1] = 2;
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(11, 14),
                // (12,9): error CS0272: The property or indexer 'Base.this[string]' cannot be used in this context because the set accessor is inaccessible
                //         this[string.Empty] = 4;
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "this[string.Empty]").WithArguments("Base.this[string]").WithLocation(12, 9),
                // (14,22): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                //     Derived(int x) : base() {}
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("Base.Base()").WithLocation(14, 22),
                // (15,5): error CS0122: 'Base.Base()' is inaccessible due to its protection level
                //     Derived(long x) {} // implicit base()
                Diagnostic(ErrorCode.ERR_BadAccess, "Derived").WithArguments("Base.Base()").WithLocation(15, 5)
                );

            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { new CSharpCompilationReference(baseCompilation) },
                assemblyName: "WantsIVTAccess",
                options: TestOptions.SigningReleaseDll)
                .VerifyDiagnostics(
                );
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { MetadataReference.CreateFromImage(baseCompilation.EmitToArray()) },
                assemblyName: "WantsIVTAccess",
                options: TestOptions.SigningReleaseDll)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NotAccessibleWhereRequired()
        {
            string source =
@"public class Base
{
    private protected int Field1;
    protected private int Field2;
}

public class Derived // : Base
{
    void M()
    {
        Base b = null;
        b.Field1 = 1;
        b.Field2 = 2;
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (12,11): error CS0122: 'Base.Field1' is inaccessible due to its protection level
                //         b.Field1 = 1;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field1").WithArguments("Base.Field1").WithLocation(12, 11),
                // (13,11): error CS0122: 'Base.Field2' is inaccessible due to its protection level
                //         b.Field2 = 2;
                Diagnostic(ErrorCode.ERR_BadAccess, "Field2").WithArguments("Base.Field2").WithLocation(13, 11)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NotInStructOrNamespace()
        {
            string source =
@"protected private struct Struct
{
    private protected int Field1;
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (1,18): error CS1527: Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected
                // protected private struct Struct
                Diagnostic(ErrorCode.ERR_NoNamespacePrivate, "Struct").WithLocation(1, 26),
                // (3,27): error CS0666: 'Struct.Field1': new protected member declared in struct
                //     private protected int Field1;
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Field1").WithArguments("Struct.Field1").WithLocation(3, 27)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NotInStaticClass()
        {
            string source =
@"static class C
{
    static private protected int Field1 = 2;
}
sealed class D
{
    static private protected int Field2 = 2;
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (7,34): warning CS0628: 'D.Field2': new protected member declared in sealed type
                //     static private protected int Field2 = 2;
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "Field2").WithArguments("D.Field2").WithLocation(7, 34),
                // (3,34): error CS1057: 'C.Field1': static classes cannot contain protected members
                //     static private protected int Field1 = 2;
                Diagnostic(ErrorCode.ERR_ProtectedInStatic, "Field1").WithArguments("C.Field1").WithLocation(3, 34)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NestedTypes()
        {
            string source =
@"class Outer
{
    private protected class Inner
    {
    }
}
class Derived : Outer
{
    public void M()
    {
        Outer.Inner x = null;
    }
}
class NotDerived
{
    public void M()
    {
        Outer.Inner x = null; // error: Outer.Inner not accessible
    }
}
struct Struct
{
    private protected class Inner // error: protected not allowed in struct
    {
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (23,29): error CS0666: 'Struct.Inner': new protected member declared in struct
                //     private protected class Inner // error: protected not allowed in struct
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Inner").WithArguments("Struct.Inner").WithLocation(23, 29),
                // (11,21): warning CS0219: The variable 'x' is assigned but its value is never used
                //         Outer.Inner x = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(11, 21),
                // (18,15): error CS0122: 'Outer.Inner' is inaccessible due to its protection level
                //         Outer.Inner x = null; // error: Outer.Inner not accessible
                Diagnostic(ErrorCode.ERR_BadAccess, "Inner").WithArguments("Outer.Inner").WithLocation(18, 15)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void PermittedAccessorProtection()
        {
            string source =
@"class Class
{
    public int Prop1 { get; private protected set; }
    protected internal int Prop2 { get; private protected set; }
    protected int Prop3 { get; private protected set; }
    internal int Prop4 { get; private protected set; }
    private protected int Prop5 { get; private set; }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ForbiddenAccessorProtection_01()
        {
            string source =
@"class Class
{
    private protected int Prop1 { get; private protected set; }
    private int Prop2 { get; private protected set; }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (3,58): error CS0273: The accessibility modifier of the 'Class.Prop1.set' accessor must be more restrictive than the property or indexer 'Class.Prop1'
                //     private protected int Prop1 { get; private protected set; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("Class.Prop1.set", "Class.Prop1").WithLocation(3, 58),
                // (4,48): error CS0273: The accessibility modifier of the 'Class.Prop2.set' accessor must be more restrictive than the property or indexer 'Class.Prop2'
                //     private int Prop2 { get; private protected set; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("Class.Prop2.set", "Class.Prop2").WithLocation(4, 48)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ForbiddenAccessorProtection_02()
        {
            string source =
@"interface ISomething
{
    private protected int M();
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (3,27): error CS8503: The modifier 'private protected' is not valid for this item in C# 7.2. Please use language version '8.0' or greater.
                //     private protected int M();
                Diagnostic(ErrorCode.ERR_InvalidModifierForLanguageVersion, "M").WithArguments("private protected", "7.2", "8.0").WithLocation(3, 27),
                // (3,27): error CS8707: Target runtime doesn't support 'protected', 'protected internal', or 'private protected' accessibility for a member of an interface.
                //     private protected int M();
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember, "M").WithLocation(3, 27)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void AtLeastAsRestrictivePositive_01()
        {
            string source =
@"
public class C
{
    internal class Internal {}
    protected class Protected {}
    private protected class PrivateProtected {}
    private protected void M(Internal x) {} // ok
    private protected void M(Protected x) {} // ok
    private protected void M(PrivateProtected x) {} // ok
    private protected class Nested
    {
        public void M(Internal x) {} // ok
        public void M(Protected x) {} // ok
        private protected void M(PrivateProtected x) {} // ok
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void AtLeastAsRestrictiveNegative_01()
        {
            string source =
@"
public class Container
{
    private protected class PrivateProtected {}
    internal void M1(PrivateProtected x) {} // error: conflicting access
    protected void M2(PrivateProtected x) {} // error: conflicting access
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (6,20): error CS0051: Inconsistent accessibility: parameter type 'Container.PrivateProtected' is less accessible than method 'Container.M2(Container.PrivateProtected)'
                //     protected void M2(PrivateProtected x) {} // error: conflicting access
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M2").WithArguments("Container.M2(Container.PrivateProtected)", "Container.PrivateProtected").WithLocation(6, 20),
                // (5,19): error CS0051: Inconsistent accessibility: parameter type 'Container.PrivateProtected' is less accessible than method 'Container.M1(Container.PrivateProtected)'
                //     internal void M1(PrivateProtected x) {} // error: conflicting access
                Diagnostic(ErrorCode.ERR_BadVisParamType, "M1").WithArguments("Container.M1(Container.PrivateProtected)", "Container.PrivateProtected").WithLocation(5, 19)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void DuplicateAccessInBinder()
        {
            string source =
@"
public class Container
{
    private public int Field;                           // 1
    private public int Property { get; set; }           // 2
    private public int M() => 1;                        // 3
    private public class C {}                           // 4
    private public struct S {}                          // 5
    private public enum E {}                            // 6
    private public event System.Action V;               // 7
    private public interface I {}                       // 8
    private public int this[int index] => 1;            // 9
    void Q() { V.Invoke(); V = null; }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (7,26): error CS0107: More than one protection modifier
                //     private public class C {}                           // 4
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "C").WithLocation(7, 26),
                // (8,27): error CS0107: More than one protection modifier
                //     private public struct S {}                          // 5
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "S").WithLocation(8, 27),
                // (9,25): error CS0107: More than one protection modifier
                //     private public enum E {}                            // 6
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "E").WithLocation(9, 25),
                // (11,30): error CS0107: More than one protection modifier
                //     private public interface I {}                       // 8
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "I").WithLocation(11, 30),
                // (4,24): error CS0107: More than one protection modifier
                //     private public int Field;                           // 1
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Field").WithLocation(4, 24),
                // (5,24): error CS0107: More than one protection modifier
                //     private public int Property { get; set; }           // 2
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Property").WithLocation(5, 24),
                // (6,24): error CS0107: More than one protection modifier
                //     private public int M() => 1;                        // 3
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "M").WithLocation(6, 24),
                // (10,40): error CS0107: More than one protection modifier
                //     private public event System.Action V;               // 7
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "V").WithLocation(10, 40),
                // (12,24): error CS0107: More than one protection modifier
                //     private public int this[int index] => 1;            // 9
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "this").WithLocation(12, 24)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NotInVersion71()
        {
            string source =
@"
public class Container
{
    private protected int Field;                           // 1
    private protected int Property { get; set; }           // 2
    private protected int M() => 1;                        // 3
    private protected class C {}                           // 4
    private protected struct S {}                          // 5
    private protected enum E {}                            // 6
    private protected event System.Action V;               // 7
    private protected interface I {}                       // 8
    private protected int this[int index] => 1;            // 9
    void Q() { V.Invoke(); V = null; }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_1)
                .VerifyDiagnostics(
                // (7,29): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected class C {}                           // 4
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "C").WithArguments("private protected", "7.2").WithLocation(7, 29),
                // (8,30): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected struct S {}                          // 5
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "S").WithArguments("private protected", "7.2").WithLocation(8, 30),
                // (9,28): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected enum E {}                            // 6
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "E").WithArguments("private protected", "7.2").WithLocation(9, 28),
                // (11,33): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected interface I {}                       // 8
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "I").WithArguments("private protected", "7.2").WithLocation(11, 33),
                // (4,27): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected int Field;                           // 1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "Field").WithArguments("private protected", "7.2").WithLocation(4, 27),
                // (5,27): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected int Property { get; set; }           // 2
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "Property").WithArguments("private protected", "7.2").WithLocation(5, 27),
                // (6,27): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected int M() => 1;                        // 3
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "M").WithArguments("private protected", "7.2").WithLocation(6, 27),
                // (10,43): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected event System.Action V;               // 7
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "V").WithArguments("private protected", "7.2").WithLocation(10, 43),
                // (12,27): error CS8302: Feature 'private protected' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     private protected int this[int index] => 1;            // 9
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "this").WithArguments("private protected", "7.2").WithLocation(12, 27)
                );
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VerifyPrivateProtectedIL()
        {
            var text = @"
class Program
{
    private protected void M() {}
    private protected int F;
}
";
            var verifier = CompileAndVerify(
                text,
                parseOptions: TestOptions.Regular7_2,
                expectedSignatures: new[]
                {
                    Signature("Program", "M", ".method famandassem hidebysig instance System.Void M() cil managed"),
                    Signature("Program", "F", ".field famandassem instance System.Int32 F"),
                });
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VerifyPartialPartsMatch()
        {
            var source =
@"class Outer
{
    private protected partial class Inner {}
    private           partial class Inner {}
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (3,37): error CS0262: Partial declarations of 'Outer.Inner' have conflicting accessibility modifiers
                //     private protected partial class Inner {}
                Diagnostic(ErrorCode.ERR_PartialModifierConflict, "Inner").WithArguments("Outer.Inner").WithLocation(3, 37)
                );
            source =
@"class Outer
{
    private protected partial class Inner {}
    private protected partial class Inner {}
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VerifyProtectedSemantics()
        {
            var source =
@"class Base
{
    private protected void M()
    {
        System.Console.WriteLine(this.GetType().Name);
    }
}

class Derived : Base
{
    public void Main()
    {
        Derived derived = new Derived();
        derived.M();
        Base bb = new Base();
        bb.M(); // error 1
        Other other = new Other();
        other.M(); // error 2
    }
}

class Other : Base
{
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                // (16,12): error CS1540: Cannot access protected member 'Base.M()' via a qualifier of type 'Base'; the qualifier must be of type 'Derived' (or derived from it)
                //         bb.M(); // error 1
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "M").WithArguments("Base.M()", "Base", "Derived").WithLocation(16, 12),
                // (18,15): error CS1540: Cannot access protected member 'Base.M()' via a qualifier of type 'Other'; the qualifier must be of type 'Derived' (or derived from it)
                //         other.M(); // error 2
                Diagnostic(ErrorCode.ERR_BadProtectedAccess, "M").WithArguments("Base.M()", "Other", "Derived").WithLocation(18, 15)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void HidingAbstract()
        {
            var source =
@"abstract class A
{
    internal abstract void F();
}
abstract class B : A
{
    private protected new void F() { } // No CS0533
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular7_2)
                .VerifyDiagnostics(
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void HidingInaccessible()
        {
            string source1 =
@"public class A
{
    private protected void F() { }
}
";
            var compilation1 = CreateCompilation(source1, parseOptions: TestOptions.Regular7_2);
            compilation1.VerifyDiagnostics();

            string source2 =
@"class B : A
{
    new void F() { } // CS0109
}
";
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { new CSharpCompilationReference(compilation1) })
            .VerifyDiagnostics(
                // (3,14): warning CS0109: The member 'B.F()' does not hide an accessible member. The new keyword is not required.
                //     new void F() { } // CS0109
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F").WithArguments("B.F()").WithLocation(3, 14)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void UnimplementedInaccessible()
        {
            string source1 =
@"public abstract class A
{
    private protected abstract void F();
}
";
            var compilation1 = CreateCompilation(source1, parseOptions: TestOptions.Regular7_2);
            compilation1.VerifyDiagnostics();

            string source2 =
@"class B : A // CS0534
{
}
";
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { new CSharpCompilationReference(compilation1) })
            .VerifyDiagnostics(
                // (1,7): error CS0534: 'B' does not implement inherited abstract member 'A.F()'
                // class B : A // CS0534
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.F()").WithLocation(1, 7)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ImplementInaccessible()
        {
            string source1 =
@"public abstract class A
{
    private protected abstract void F();
}
";
            var compilation1 = CreateCompilation(source1, parseOptions: TestOptions.Regular7_2);
            compilation1.VerifyDiagnostics();

            string source2 =
@"class B : A // CS0534
{
    override private protected void F() {}
}
";
            CreateCompilation(source2, parseOptions: TestOptions.Regular7_2,
                references: new[] { new CSharpCompilationReference(compilation1) })
            .VerifyDiagnostics(
                // (3,37): error CS0115: 'B.F()': no suitable method found to override
                //     override private protected void F() {}
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "F").WithArguments("B.F()").WithLocation(3, 37),
                // (1,7): error CS0534: 'B' does not implement inherited abstract member 'A.F()'
                // class B : A // CS0534
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.F()").WithLocation(1, 7)
                );
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VerifyPPExtension()
        {
            string source = @"
static class Extensions
{
    static private protected void SomeExtension(this string s) { } // error: no pp in static class
}

class Client
{
    public static void M(string s)
    {
        s.SomeExtension(); // error: no accessible SomeExtension
    }
}
";
            CreateCompilationWithMscorlib461(source, parseOptions: TestOptions.Regular7_2)
            .VerifyDiagnostics(
                // (4,35): error CS1057: 'Extensions.SomeExtension(string)': static classes cannot contain protected members
                //     static private protected void SomeExtension(this string s) { } // error: no pp in static class
                Diagnostic(ErrorCode.ERR_ProtectedInStatic, "SomeExtension").WithArguments("Extensions.SomeExtension(string)").WithLocation(4, 35),
                // (11,11): error CS1061: 'string' does not contain a definition for 'SomeExtension' and no accessible extension method 'SomeExtension' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         s.SomeExtension(); // error: no accessible SomeExtension
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "SomeExtension").WithArguments("string", "SomeExtension").WithLocation(11, 11)
                );
        }
    }
}
