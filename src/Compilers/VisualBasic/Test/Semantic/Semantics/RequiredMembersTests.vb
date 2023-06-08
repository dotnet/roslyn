' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RequiredMembersTests
        Inherits BasicTestBase

        Private Function CreateCSharpCompilationWithRequiredMembers(source As String) As CSharpCompilation
            Return CreateCSharpCompilation(source, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)
        End Function

        <Fact>
        Public Sub CannotInheritFromTypesWithRequiredMembers()
            Dim csharp = "
public class Base
{
    public required int Field { get; set; }
}
public class Derived : Base {}"

            Dim csharpReference = CreateCSharpCompilationWithRequiredMembers(csharp).EmitToImageReference()

            Dim vb = CreateCompilation("
Class VbDerivedBase
    Inherits Base
End Class

Class VbDerivedDerived
    Inherits Derived
End Class

Module M
    Sub Main()
        Dim v1 = New VbDerivedBase()
        Dim v2 = New VbDerivedDerived()
        G(Of VbDerivedBase)()
        G(Of VbDerivedDerived)()
    End Sub

    Sub G(Of T As New)()
    End Sub
End Module", references:={csharpReference})

            vb.AssertTheseDiagnostics(<expected>
BC37322: Cannot inherit from 'Base' because it has required members.
    Inherits Base
    ~~~~~~~~~~~~~
BC37322: Cannot inherit from 'Derived' because it has required members.
    Inherits Derived
    ~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Field As Integer' must be set in the object initializer or attribute arguments.
        Dim v1 = New VbDerivedBase()
                     ~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Field As Integer' must be set in the object initializer or attribute arguments.
        Dim v2 = New VbDerivedDerived()
                     ~~~~~~~~~~~~~~~~
BC37324: 'VbDerivedBase' cannot satisfy the 'New' constraint on parameter 'T' in the generic type or or method 'Public Sub G(Of T As New)()' because 'VbDerivedBase' has required members.
        G(Of VbDerivedBase)()
        ~~~~~~~~~~~~~~~~~~~
BC37324: 'VbDerivedDerived' cannot satisfy the 'New' constraint on parameter 'T' in the generic type or or method 'Public Sub G(Of T As New)()' because 'VbDerivedDerived' has required members.
        G(Of VbDerivedDerived)()
        ~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim vbDerived = vb.GlobalNamespace.GetTypeMember("VbDerivedBase")
            Assert.False(vbDerived.HasRequiredMembersError)
            Assert.False(vbDerived.HasAnyDeclaredRequiredMembers)
            AssertEx.Equal({"Property Base.Field As System.Int32"}, vbDerived.AllRequiredMembers.Select(Function(m) m.Value.ToTestDisplayString()))

            Dim vbDerivedDerived = vb.GlobalNamespace.GetTypeMember("VbDerivedDerived")
            Assert.False(vbDerivedDerived.HasRequiredMembersError)
            Assert.False(vbDerivedDerived.HasAnyDeclaredRequiredMembers)
            AssertEx.Equal({"Property Base.Field As System.Int32"}, vbDerivedDerived.AllRequiredMembers.Select(Function(m) m.Value.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub CannotInheritFromTypesWithRequiredMembers_WithMalformedRequiredMemberList()
            ' Equivalent to
            ' public class Base
            ' {
            '     public required int P { get; set; }
            ' }
            ' public class Derived : Base
            ' {
            '     public new required int P { get; set; }
            '     public Derived() {}
            ' }
            Dim il = "
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim vb = CreateCompilation("
Class VbDerivedDerived
    Inherits Derived
End Class

Module M
    Sub Main()
        Dim v2 = New VbDerivedDerived()
        G(Of VbDerivedDerived)()
    End Sub

    Sub G(Of T As New)()
    End Sub
End Module", references:={ilRef})

            vb.AssertTheseDiagnostics(<expected>
BC37322: Cannot inherit from 'Derived' because it has required members.
    Inherits Derived
    ~~~~~~~~~~~~~~~~
BC37324: 'VbDerivedDerived' cannot satisfy the 'New' constraint on parameter 'T' in the generic type or or method 'Public Sub G(Of T As New)()' because 'VbDerivedDerived' has required members.
        G(Of VbDerivedDerived)()
        ~~~~~~~~~~~~~~~~~~~~~~
                                      </expected>)

            Dim vbDerivedDerived = vb.GlobalNamespace.GetTypeMember("VbDerivedDerived")
            Assert.True(vbDerivedDerived.HasRequiredMembersError)
            Assert.False(vbDerivedDerived.HasAnyDeclaredRequiredMembers)
            AssertEx.Empty(vbDerivedDerived.AllRequiredMembers.Select(Function(m) m.Value.ToTestDisplayString()))
        End Sub

        Private Shared Function GetCDefinition(hasSetsRequiredMembers As Boolean, Optional typeKind As String = "class") As String
            Return $"
using System.Diagnostics.CodeAnalysis;
public {typeKind} C
{{
    public required int Prop {{ get; set; }}
    public required int Field;

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public C() {{}}
}}"
        End Function

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_NoInheritance_NoneSet(<CombinatorialValues("As New C()", " = New C()")> constructor As String)

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_NoInheritance_PartialSet(<CombinatorialValues("As New C()", " = new C()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop = 1 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %> With { .Prop = 1 }
                     ~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_NoInheritance_AllSet(<CombinatorialValues("As New C()", " = new C()")> constructor As String)

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop = 1, .Field = 2 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_NoInheritance_HasSetsRequiredMembers(<CombinatorialValues("As New C()", " = new C()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=True))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics
        End Sub

        Private Shared Function GetBaseDerivedDefinition(hasSetsRequiredMembers As Boolean) As String
            Return $"
using System.Diagnostics.CodeAnalysis;
public class Base
{{
    public required int Prop1 {{ get; set; }}
    public required int Field1;

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public Base() {{}}
}}
public class Derived : Base
{{
    public required int Prop2 {{ get; set; }}
    public required int Field2;

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public Derived() {{}}
}}
public class DerivedDerived : Derived
{{
    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public DerivedDerived() {{}}
}}"
        End Function

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Inheritance_NoneSet(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetBaseDerivedDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~~~~~~~~
BC37321: Required member 'Public Field2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Prop1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Prop2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Inheritance_PartialSet(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetBaseDerivedDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop1 = 1, .Field2 = 2 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %> With { .Prop1 = 1, .Field2 = 2 }
                     ~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Prop2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %> With { .Prop1 = 1, .Field2 = 2 }
                     ~~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Inheritance_AllSet(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetBaseDerivedDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop1 = 1, .Prop2 = 2, .Field1 = 1, .Field2 = 2 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Inheritance_NoneSet_HasSetsRequiredMembers(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetBaseDerivedDefinition(hasSetsRequiredMembers:=True))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_NoneSet()
            Dim retargetedCode = GetCDefinition(hasSetsRequiredMembers:=False)

            Dim originalC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim originalBasic = CreateCompilation("
Public Class Base
    Public Property C As C
End Class", {originalC.EmitToImageReference()})

            Dim retargetedC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim comp = CreateCompilation("
Module M
    Public Sub Main()
        Dim b As New Base() With { .C = New C() }
    End Sub
End Module", {originalBasic.ToMetadataReference(), retargetedC.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim b As New Base() With { .C = New C() }
                                            ~
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim b As New Base() With { .C = New C() }
                                            ~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_AllSet(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)
            Dim retargetedCode = GetCDefinition(hasSetsRequiredMembers:=False)

            Dim originalC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim originalBasic = CreateCompilation("
Public Class Base
    Public Property C As C
End Class", {originalC.EmitToImageReference()})

            Dim retargetedC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim comp = CreateCompilation("
Module M
    Public Sub Main()
        Dim b As New Base() With { .C = New C() With { .Field = 1, .Prop = 1 } }
    End Sub
End Module", {originalBasic.ToMetadataReference(), retargetedC.EmitToImageReference()})

            comp.AssertNoDiagnostics()
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_HasSetsRequiredMembers(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)

            Dim retargetedCode = GetCDefinition(hasSetsRequiredMembers:=True)

            Dim originalC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim originalBasic = CreateCompilation("
Public Class Base
    Public Property C As C
End Class", {originalC.EmitToImageReference()})

            Dim retargetedC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True), retargetedCode, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim comp = CreateCompilation("
Module M
    Public Sub Main()
        Dim b As New Base() With { .C = New C() }
    End Sub
End Module", {originalBasic.ToMetadataReference(), retargetedC.EmitToImageReference()})

            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ThroughRetargeting_RequiredMemberAdded()
            Dim codeWithRequired = GetCDefinition(hasSetsRequiredMembers:=False)
            Dim codeWithoutRequired = codeWithRequired.Replace("required ", "")

            Dim originalC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(1, 0, 0, 0), isRetargetable:=True), codeWithoutRequired, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim originalBasic = CreateCompilation("
Public Class Derived
    Inherits C
End Class", {originalC.EmitToImageReference()})

            Dim retargetedC = CreateCSharpCompilation(New AssemblyIdentity("Ret", New Version(2, 0, 0, 0), isRetargetable:=True), codeWithRequired, referencedAssemblies:=Basic.Reference.Assemblies.Net70.All)

            Dim comp = CreateCompilation("
Module M
    Public Sub Main()
        Dim b As New Derived()
    End Sub
End Module", {originalBasic.ToMetadataReference(), retargetedC.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim b As New Derived()
                     ~~~~~~~
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim b As New Derived()
                     ~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_ThroughMetadataAndSource(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)
            Dim originalVbComp = CreateCompilation("
Public Class Base
End Class", targetFramework:=TargetFramework.Net70)

            originalVbComp.AssertNoDiagnostics()

            Dim csharpComp = CreateCSharpCompilation("
public class Derived : Base
{
    public required int Prop { get; set; }
}", referencedAssemblies:=DirectCast(Basic.Reference.Assemblies.Net70.All, IEnumerable(Of MetadataReference)).Append(originalVbComp.EmitToImageReference()))

            Dim comp = CreateCompilation($"
Module M
    Sub Main()
        Dim derived {constructor}
    End Sub
End Module", {originalVbComp.ToMetadataReference(), csharpComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim derived <%= constructor %>
                           ~~~~~~~
                                        </expected>
            )
        End Sub

        Private Function GetDerivedOverrideDefinition(hasSetsRequiredMembers As Boolean) As String
            Return $"
using System.Diagnostics.CodeAnalysis;
public class Base
{{
    public virtual required int Prop {{ get; set; }}

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public Base() {{}}
}}
public class Derived : Base
{{
    public override required int Prop {{ get; set; }}

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public Derived() {{}}
}}
public class DerivedDerived : Derived
{{
    public override required int Prop {{ get; set; }}

    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}    
    public DerivedDerived() {{}}
}}"
        End Function

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Override_NoneSet_01(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetDerivedOverrideDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overrides Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Override_NoneSet_02(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetDerivedOverrideDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overrides Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim t <%= constructor %>
                     ~~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Override_AllSet_01(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetDerivedOverrideDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop = 1 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Override_AllSet_02(<CombinatorialValues("As New DerivedDerived()", " = new DerivedDerived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetDerivedOverrideDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor} With {{ .Prop = 1 }}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub EnforcedRequiredMembers_Override_HasSetsRequiredMembers(<CombinatorialValues("As New Derived()", " = new Derived()")> constructor As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetDerivedOverrideDefinition(hasSetsRequiredMembers:=True))

            Dim vbCode = $"
Module M
    Sub Main()
        Dim t {constructor}
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_StructureNothing_ImplicitCtor()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers("
using System.Diagnostics.CodeAnalysis;
public struct S
{
    public required int F;
}")

            Dim vbCode = $"
Module M
    Sub Main()
        Dim s As S = Nothing
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_StructureNothing_ExplicitCtor()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers("
using System.Diagnostics.CodeAnalysis;
public struct S
{
    public required int F;
    public S() {}
}")

            Dim vbCode = $"
Module M
    Sub Main()
        Dim s As S = Nothing
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_StructureNothing_HasSetsRequiredMembers()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers("
using System.Diagnostics.CodeAnalysis;
public struct S
{
    public required int F;
    [SetsRequiredMembers]
    public S() {}
}")

            Dim vbCode = $"
Module M
    Sub Main()
        Dim s As S = Nothing
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ShadowedFromMetadata_01()
            ' Equivalent to
            ' public class Base
            ' {
            '     public required int P { get; set; }
            ' }
            ' public class Derived
            ' {
            '     public new required int P { get; set; }
            '     public Derived() {}
            '     [SetsRequiredMembers] public Derived(int unused) {}
            ' }
            Dim il = "
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim d1 = New Derived()
        Dim d2 = New Derived(1)
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37323: The required members list for 'Derived' is malformed and cannot be interpreted.
        Dim d1 = New Derived()
                 ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ShadowedFromMetadata_02()
            ' Equivalent to
            ' public class Base
            ' {
            '     public required int P { get; set; }
            ' }
            ' [RequiredMember] public class Derived
            ' {
            '     public new int P { get; set; }
            '     public Derived() {}
            '     [SetsRequiredMembers] public Derived(int unused) {}
            ' }
            Dim il = "
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim d1 = New Derived()
        Dim d2 = New Derived(1)
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37323: The required members list for 'Derived' is malformed and cannot be interpreted.
        Dim d1 = New Derived()
                 ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_ShadowedFromMetadata_03()
            ' Equivalent to
            ' public class Base
            ' {
            '     public required int P { get; set; }
            ' }
            ' public class Derived
            ' {
            '     public new int P { get; set; }
            '     public Derived() {}
            '     [SetsRequiredMembers] public Derived(int unused) {}
            ' }
            Dim il = "
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor (
            int32 'unused'
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute::.ctor() = (
            01 00 00 00
        )
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim d1 = New Derived()
        Dim d2 = New Derived(1)
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37323: The required members list for 'Derived' is malformed and cannot be interpreted.
        Dim d1 = New Derived()
                 ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub EnforcedRequiredMembers_OverriddenFromMetadata()
            ' Equivalent to
            ' public class Base
            ' {
            '     public virtual int P { get; set; }
            '     public virtual required modopt(int) int P { get; set; }
            ' }
            ' public class Derived
            ' {
            '     public override int P { get; set; }
            '     public Derived() {}
            ' }
            Dim il = "
.class public auto ansi beforefieldinit Base extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )

    .field private int32 '<Prop>k__BackingField'

    // Methods
    .method public hidebysig specialname newslot virtual 
        instance int32 get_Prop () cil managed 
    {
        ldarg.0
        ldfld int32 Base::'<Prop>k__BackingField'
        ret
    }

    .method public hidebysig specialname newslot virtual 
        instance int32 modopt(int32) get_Prop () cil managed 
    {
        ldarg.0
        ldfld int32 Base::'<Prop>k__BackingField'
        ret
    }

    .method public hidebysig specialname newslot virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::'<Prop>k__BackingField'
        ret
    }

    .method public hidebysig specialname newslot virtual 
        instance void set_Prop (
            int32 modopt(int32) 'value'
        ) cil managed 
    {
        ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 5f 43 6f 6e 73 74 72 75 63 74 6f 72 73 20
            6f 66 20 74 79 70 65 73 20 77 69 74 68 20 72 65
            71 75 69 72 65 64 20 6d 65 6d 62 65 72 73 20 61
            72 65 20 6e 6f 74 20 73 75 70 70 6f 72 74 65 64
            20 69 6e 20 74 68 69 73 20 76 65 72 73 69 6f 6e
            20 6f 66 20 79 6f 75 72 20 63 6f 6d 70 69 6c 65
            72 2e 01 00 00
        )
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0f 52 65 71 75 69 72 65 64 4d 65 6d 62 65
            72 73 00 00
        )

        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        nop
        ret
    }

    .property instance int32 modopt(int32) Prop()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 modopt(int32) Base::get_Prop()
        .set instance void Base::set_Prop(int32 modopt(int32))
    }

    .property instance int32 Prop()
    {
        .get instance int32 Base::get_Prop()
        .set instance void Base::set_Prop(int32)
    }

} // end of class Base

.class public auto ansi beforefieldinit Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 '<Prop>k__BackingField'

    .method public hidebysig specialname virtual 
        instance int32 get_Prop () cil managed 
    {
        ldarg.0
        ldfld int32 Derived::'<Prop>k__BackingField'
        ret
    }

    .method public hidebysig specialname virtual 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::'<Prop>k__BackingField'
        ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void Base::.ctor()
        nop
        ret
    }

    .property instance int32 Prop()
    {
        .get instance int32 Derived::get_Prop()
        .set instance void Derived::set_Prop(int32)
    }

}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim d1 = New Derived()
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37323: The required members list for 'Derived' is malformed and cannot be interpreted.
        Dim d1 = New Derived()
                 ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub CoClassWithRequiredMembers_NoneSet()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers("
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(C))]
public interface I
{
}

public class C : I
{
    public required int P { get; set; }
}
")
            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim i = New I()
    End Sub
End Module", {cComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property P As Integer' must be set in the object initializer or attribute arguments.
        Dim i = New I()
                    ~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub CoClassWithRequiredMembers_AllSet()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers("
using System;
using System.Runtime.InteropServices;

[ComImport, Guid(""00020810-0000-0000-C000-000000000046"")]
[CoClass(typeof(C))]
public interface I
{
    public int P { get; set; }
}

public class C : I
{
    public required int P { get; set; }
}
")
            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim i = New I() With { .P = 1 }
    End Sub
End Module", {cComp.EmitToImageReference()})

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property P As Integer' must be set in the object initializer or attribute arguments.
        Dim i = New I() With { .P = 1 }
                    ~
                                        </expected>)
        End Sub

        Public Function GetAttributeDefinition(hasSetsRequiredMembers As Boolean) As String
            Return $"
using System;
public class AttrAttribute : Attribute
{{
    {If(hasSetsRequiredMembers, "[SetsRequiredMembers]", "")}
    public AttrAttribute() {{}}

    public required int P {{ get; set; }}
}}
"
        End Function

        <Fact>
        Public Sub RequiredMemberInAttribute_NoneSet()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetAttributeDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
<Attr>
Class C
End Class"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected><![CDATA[
BC37321: Required member 'Public Overloads Property P As Integer' must be set in the object initializer or attribute arguments.
<Attr>
 ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub RequiredMemberInAttribute_AllSet()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetAttributeDefinition(hasSetsRequiredMembers:=False))

            Dim vbCode = $"
<Attr(P:=1)>
Class C
End Class"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub PublicAPITests()
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=False))

            Dim vbComp = CreateCompilation("", {cComp.EmitToImageReference()})

            Dim c = vbComp.GetTypeByMetadataName("C")
            Assert.False(c.HasRequiredMembersError)
            Dim prop = c.GetMember(Of PropertySymbol)("Prop")
            Dim field = c.GetMember(Of FieldSymbol)("Field")

            AssertEx.Equal(Of Symbol)({field, prop}, From kvp In c.AllRequiredMembers
                                                     Order By kvp.Key
                                                     Select kvp.Value)

            Assert.True(prop.IsRequired)
            Assert.True(field.IsRequired)
        End Sub

        <Theory>
        <InlineData("class")>
        <InlineData("struct")>
        Public Sub GenericConstrainedToNew_Forbidden(typeKind As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=False, typeKind))

            Dim vbCode = "
Module M
    Sub Main()
        Generic(Of C)()
    End Sub

    Sub Generic(Of T As New)()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37324: 'C' cannot satisfy the 'New' constraint on parameter 'T' in the generic type or or method 'Public Sub Generic(Of T As New)()' because 'C' has required members.
        Generic(Of C)()
        ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <InlineData("class")>
        <InlineData("struct")>
        Public Sub GenericConstrainedToNew_Forbidden_HasRequiredMembersError(typeKind As String)
            ' Equivalent to
            ' public class Base
            ' {
            '     public required int P { get; set; }
            ' }
            ' public class Derived
            ' {
            '     public new required int P { get; set; }
            '     public Derived() {}
            ' }
            Dim il = "
.class public auto ansi Base
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Base::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Base::_P
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Base::get_P()
        .set instance void Base::set_P(int32)
    }
}

.class public auto ansi Derived
    extends Base
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P

    .method public specialname 
        instance int32 get_P () cil managed 
    {
        IL_0000: ldarg.0
        IL_0001: ldfld int32 Derived::_P
        IL_0006: br.s IL_0008

        IL_0008: ret
    }

    .method public specialname 
        instance void set_P (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 Derived::_P
        ret
    }

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        nop
        ldarg.0
        call instance void Base::.ctor()
        ret
    }

    .property instance int32 P()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 Derived::get_P()
        .set instance void Derived::set_P(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim vbCode = "
Module M
    Sub Main()
        Generic(Of Derived)()
    End Sub

    Sub Generic(Of T As New)()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {ilRef})
            comp.AssertTheseDiagnostics(<expected>
BC37324: 'Derived' cannot satisfy the 'New' constraint on parameter 'T' in the generic type or or method 'Public Sub Generic(Of T As New)()' because 'Derived' has required members.
        Generic(Of Derived)()
        ~~~~~~~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Theory>
        <InlineData("class")>
        <InlineData("struct")>
        Public Sub GenericConstrainedToNew_HasSetsRequiredMembers_Allowed(typeKind As String)
            Dim cComp = CreateCSharpCompilationWithRequiredMembers(GetCDefinition(hasSetsRequiredMembers:=True, typeKind))

            Dim vbCode = "
Module M
    Sub Main()
        Generic(Of C)()
    End Sub

    Sub Generic(Of T As New)()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub GenericSubstitution_NoneSet()
            Dim cDef = "
public class C<T>
{
    public required T Prop { get; set; }
    public required T Field;
}"

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(cDef)

            Dim vbCode = "
Module M
    Sub Main()
        Dim c = New C(Of Integer)()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim c = New C(Of Integer)()
                    ~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim c = New C(Of Integer)()
                    ~~~~~~~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub GenericSubstitution_AllSet()
            Dim cDef = "
public class C<T>
{
    public required T Prop { get; set; }
    public required T Field;
}"

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(cDef)

            Dim vbCode = "
Module M
    Sub Main()
        Dim c = New C(Of Integer)() With { .Prop = 1, .Field = 2 }
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub GenericSubstitution_Unbound()
            Dim cDef = "
public class C<T>
{
    public required T Prop { get; set; }
    public required T Field;
}"

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(cDef)

            Dim vbCode = "
Module M
    Sub Main()
        Dim c = New C(Of)()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As ?' must be set in the object initializer or attribute arguments.
        Dim c = New C(Of)()
                    ~~~~~
BC37321: Required member 'Public Overloads Property Prop As ?' must be set in the object initializer or attribute arguments.
        Dim c = New C(Of)()
                    ~~~~~
BC30182: Type expected.
        Dim c = New C(Of)()
                        ~
                                        </expected>)

            Dim c = comp.GetTypeByMetadataName("C`1")
            Dim u_c = c.ConstructUnboundGenericType()
            Assert.False(u_c.HasAnyDeclaredRequiredMembers)
            Assert.Empty(u_c.AllRequiredMembers)
        End Sub

        <Fact>
        Public Sub GenericSubstitution_Inheritance_NoneSet()
            Dim cDef = "
public class C<T>
{
    public required T Prop { get; set; }
    public required T Field;
}
public class D : C<int> {}"

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(cDef)

            Dim vbCode = "
Module M
    Sub Main()
        Dim d = New D()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Field As Integer' must be set in the object initializer or attribute arguments.
        Dim d = New D()
                    ~
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim d = New D()
                    ~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub GenericSubstitution_InheritanceAndOverride_NoneSet()
            Dim cDef = "
public class C<T>
{
    public virtual required T Prop { get; set; }
}
public class D : C<int>
{
    public override required int Prop { get; set; }
}"

            Dim cComp = CreateCSharpCompilationWithRequiredMembers(cDef)

            Dim vbCode = "
Module M
    Sub Main()
        Dim d = New D()
    End Sub
End Module"

            Dim comp = CreateCompilation(vbCode, {cComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overrides Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim d = New D()
                    ~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub ProtectedParameterlessConstructorInStruct()
            ' Equivalent to
            ' public struct S
            ' {
            '     protected S() {}
            '     public required int Prop { get; set; }
            ' }
            Dim il = "
.class public sequential ansi sealed beforefieldinit S
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 f

    .method family hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
            01 00 5f 43 6f 6e 73 74 72 75 63 74 6f 72 73 20
            6f 66 20 74 79 70 65 73 20 77 69 74 68 20 72 65
            71 75 69 72 65 64 20 6d 65 6d 62 65 72 73 20 61
            72 65 20 6e 6f 74 20 73 75 70 70 6f 72 74 65 64
            20 69 6e 20 74 68 69 73 20 76 65 72 73 69 6f 6e
            20 6f 66 20 79 6f 75 72 20 63 6f 6d 70 69 6c 65
            72 2e 01 00 00
        )
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
            01 00 0f 52 65 71 75 69 72 65 64 4d 65 6d 62 65
            72 73 00 00
        )
        ret
    }

    .method public hidebysig specialname 
        instance int32 get_Prop () cil managed 
    {
        ldarg.0
        ldfld int32 S::f
        ret
    }

    .method public hidebysig specialname 
        instance void set_Prop (
            int32 'value'
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 S::f
        ret
    }

    .property instance int32 Prop()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 S::get_Prop()
        .set instance void S::set_Prop(int32)
    }
}
"

            Dim ilAssembly = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim s = New S()
    End Sub
End Module
", {ilAssembly}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property Prop As Integer' must be set in the object initializer or attribute arguments.
        Dim s = New S()
                    ~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub RequiredMemberAttributeDisallowedInSource()
            Dim comp = CreateCompilation("
Imports System.Runtime.CompilerServices
<RequiredMember> ' 1
Public Class C
    <RequiredMember> ' 2
    Public Property P As Integer

    <RequiredMember> ' 3
    Public F As Integer
End Class", targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected><![CDATA[
BC37325: 'System.Runtime.CompilerServices.RequiredMemberAttribute' is reserved for compiler usage only.
<RequiredMember> ' 1
 ~~~~~~~~~~~~~~
BC37325: 'System.Runtime.CompilerServices.RequiredMemberAttribute' is reserved for compiler usage only.
    <RequiredMember> ' 2
     ~~~~~~~~~~~~~~
BC37325: 'System.Runtime.CompilerServices.RequiredMemberAttribute' is reserved for compiler usage only.
    <RequiredMember> ' 3
     ~~~~~~~~~~~~~~]]></expected>)
        End Sub

        <Fact>
        Public Sub TupleWithRequiredFields()
            Dim csharpComp = CreateCSharpCompilation("
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public required T1 Item1;
        public required T2 Item2;
        public required int AnotherField;
        public required int Property { get; set; }

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public static bool operator ==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;
        public static bool operator !=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public override bool Equals(object o)
            => throw null;
        public override int GetHashCode()
            => throw null;
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public required TRest Rest;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
            this.Item7 = item7;
            this.Rest = rest;
        }

        public static bool operator ==(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t1, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t2)
            => throw null;
        public static bool operator !=(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t1, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> t2)
            => throw null;

        public override bool Equals(object o)
            => throw null;
        public override int GetHashCode()
            => throw null;
    }

    namespace Runtime.CompilerServices
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public sealed class RequiredMemberAttribute : Attribute
        {
            public RequiredMemberAttribute()
            {
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
        public sealed class CompilerFeatureRequiredAttribute : Attribute
        {
            public CompilerFeatureRequiredAttribute(string featureName)
            {
                FeatureName = featureName;
            }
            public string FeatureName { get; }
            public bool IsOptional { get; set; }
        }
    }
    namespace Diagnostics.CodeAnalysis
    {
        [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
        public sealed class SetsRequiredMembersAttribute : Attribute
        {
            public SetsRequiredMembersAttribute()
            {
            }
        }
    }
}
", referencedAssemblies:=Basic.Reference.Assemblies.Net461.All)
            Dim csharpCompReference As MetadataReference = csharpComp.EmitToImageReference()

            ' Using Net461 to get a framework without ValueTuple

            Dim comp = CreateCompilation("
Class C
    Sub Main()
        Dim t1 = New (Integer, Integer)(1, 2)
        Dim t2 = new System.ValueTuple(Of Integer, Integer)(3, 4)
        Dim t3 = new System.ValueTuple(Of Integer, Integer)()
        Dim t4 As (Integer, Integer) = Nothing 
        Dim t5 As System.ValueTuple(of integer, integer) = Nothing
        Dim t6 = new System.ValueTuple(Of Integer, Integer)() With {
            .Item1 = 1,
            .Item2 = 2,
            .[Property] = 3,
            .AnotherField = 4
        }
    End Sub
End Class", {csharpCompReference}, targetFramework:=TargetFramework.Mscorlib461)

            comp.AssertTheseDiagnostics(<expected>
BC37280: 'New' cannot be used with tuple type. Use a tuple literal expression instead.
        Dim t1 = New (Integer, Integer)(1, 2)
                     ~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = New (Integer, Integer)(1, 2)
                     ~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = New (Integer, Integer)(1, 2)
                     ~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = New (Integer, Integer)(1, 2)
                     ~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = New (Integer, Integer)(1, 2)
                     ~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 = new System.ValueTuple(Of Integer, Integer)(3, 4)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 = new System.ValueTuple(Of Integer, Integer)(3, 4)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 = new System.ValueTuple(Of Integer, Integer)(3, 4)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 = new System.ValueTuple(Of Integer, Integer)(3, 4)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = new System.ValueTuple(Of Integer, Integer)()
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = new System.ValueTuple(Of Integer, Integer)()
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = new System.ValueTuple(Of Integer, Integer)()
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = new System.ValueTuple(Of Integer, Integer)()
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                        </expected>)

            comp = CreateCompilation("
Class C
    Sub Main()
        Dim t1 = (1, 2)
        Dim t2 As (Integer, Integer) = (3, Nothing)
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
    End Sub
End Class
", {csharpCompReference}, targetFramework:=TargetFramework.Mscorlib461)

            comp.AssertTheseEmitDiagnostics(<expected>
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = (1, 2)
                 ~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = (1, 2)
                 ~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = (1, 2)
                 ~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t1 = (1, 2)
                 ~~~~~~
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 As (Integer, Integer) = (3, Nothing)
                                       ~~~~~~~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 As (Integer, Integer) = (3, Nothing)
                                       ~~~~~~~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 As (Integer, Integer) = (3, Nothing)
                                       ~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t2 As (Integer, Integer) = (3, Nothing)
                                       ~~~~~~~~~~~~
BC37321: Required member 'Public AnotherField As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item1 As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Item2 As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Overloads Property [Property] As Integer' must be set in the object initializer or attribute arguments.
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37321: Required member 'Public Rest As TRest' must be set in the object initializer or attribute arguments.
        Dim t3 = (1, 2, 3, 4, 5, 6, 7, 8, 9)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                            </expected>)

            Dim tree = comp.SyntaxTrees(0)
            Dim tuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().First()
            Dim model = comp.GetSemanticModel(tree)
            Dim tupleType = DirectCast(model.GetTypeInfo(tuple).Type, TupleTypeSymbol)

            Assert.True(tupleType.HasAnyDeclaredRequiredMembers)
            AssertEx.Equal(
                {"AnotherField", "Item1", "Item2", "Property"},
                tupleType.AllRequiredMembers.Select(Function(kvp) kvp.Key).OrderBy(StringComparer.InvariantCulture))
            Assert.All(tupleType.TupleElements, Function(field) field.IsRequired)
            Assert.True(tupleType.GetMember(Of PropertySymbol)("Property").IsRequired)
        End Sub

        <Fact>
        Public Sub TupleWithRequiredFields_SetsRequiredMembers()
            Dim csharpComp = CreateCSharpCompilation("
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public required T1 Item1;
        public required T2 Item2;
        public required int AnotherField;
        public required int Property { get; set; }

        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public static bool operator ==(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;
        public static bool operator !=(ValueTuple<T1, T2> t1, ValueTuple<T1, T2> t2)
            => throw null;

        public override bool Equals(object o)
            => throw null;
        public override int GetHashCode()
            => throw null;
    }

    namespace Runtime.CompilerServices
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public sealed class RequiredMemberAttribute : Attribute
        {
            public RequiredMemberAttribute()
            {
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
        public sealed class CompilerFeatureRequiredAttribute : Attribute
        {
            public CompilerFeatureRequiredAttribute(string featureName)
            {
                FeatureName = featureName;
            }
            public string FeatureName { get; }
            public bool IsOptional { get; set; }
        }
    }
    namespace Diagnostics.CodeAnalysis
    {
        [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
        public sealed class SetsRequiredMembersAttribute : Attribute
        {
            public SetsRequiredMembersAttribute()
            {
            }
        }
    }
}
", referencedAssemblies:=Basic.Reference.Assemblies.Net461.All)

            ' Using Net461 to get a framework without ValueTuple

            Dim comp = CreateCompilation("
Class C
    Sub Main()
        Dim t1 = (1, 2)
        Dim t2 As (Integer, Integer) = (3, Nothing)
        Dim t3 = New System.ValueTuple(Of Integer, Integer)(4, 5)
    End Sub
End Class", {csharpComp.EmitToImageReference()}, targetFramework:=TargetFramework.Mscorlib461)

            CompileAndVerify(comp).VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees(0)
            Dim tuple = tree.GetRoot().DescendantNodes().OfType(Of TupleExpressionSyntax)().First()
            Dim model = comp.GetSemanticModel(tree)
            Dim tupleType = DirectCast(model.GetTypeInfo(tuple).Type, TupleTypeSymbol)

            Assert.True(tupleType.HasAnyDeclaredRequiredMembers)
            AssertEx.Equal(
                {"AnotherField", "Item1", "Item2", "Property"},
                tupleType.AllRequiredMembers.Select(Function(kvp) kvp.Key).OrderBy(StringComparer.InvariantCulture))
            Assert.All(tupleType.TupleElements, Function(field) field.IsRequired)
            Assert.True(tupleType.GetMember(Of PropertySymbol)("Property").IsRequired)
        End Sub

        <Fact>
        Public Sub IndexedPropertyCannotBeRequired()
            ' Equivalent to
            ' <RequiredMember>
            ' Public Class C
            '     <RequiredMember>
            '     Public Property P1(x As Integer) As Integer
            '         Get
            '             Return 0
            '         End Get
            '         Set
            '         End Set
            '     End Property
            ' End Class
            Dim il = "
.class public auto ansi C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public specialname 
        instance int32 get_P1 (
            int32 x
        ) cil managed 
    {
        ldc.i4.0
        ret
    }

    .method public specialname 
        instance void set_P1 (
            int32 x,
            int32 Value
        ) cil managed 
    {
        ret
    }

    .property instance int32 P1(
        int32 x
    )
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 C::get_P1(int32)
        .set instance void C::set_P1(int32, int32)
    }

}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim c = New C()
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37323: The required members list for 'C' is malformed and cannot be interpreted.
        Dim c = New C()
                ~~~~~~~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub IndexedPropertyOverload_NoneSet()
            ' Equivalent to
            ' <RequiredMember>
            ' Public Class C
            '     <RequiredMember>
            '     Public Overloads Property P1 As Integer
            '     Public Overloads Property P1(x As Integer) As Integer
            '         Get
            '             Return 0
            '         End Get
            '         Set
            '         End Set
            '     End Property
            ' End Class
            Dim il = "
.class public auto ansi C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P1

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public hidebysig specialname 
        instance int32 get_P1 (
            int32 x
        ) cil managed 
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig specialname 
        instance void set_P1 (
            int32 x,
            int32 Value
        ) cil managed 
    {
        ret
    }

    .method public hidebysig specialname 
        instance int32 get_P1 () cil managed 
    {
        ldarg.0
        ldfld int32 C::_P1
        ret
    }

    .method public hidebysig specialname 
        instance void set_P1 (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 C::_P1
        ret
    }

    .property instance int32 P1(
        int32 x
    )
    {
        .get instance int32 C::get_P1(int32)
        .set instance void C::set_P1(int32, int32)
    }
    .property instance int32 P1()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 C::get_P1()
        .set instance void C::set_P1(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim c = New C()
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property P1 As Integer' must be set in the object initializer or attribute arguments.
        Dim c = New C()
                    ~
                                        </expected>)
        End Sub

        <Fact>
        Public Sub IndexedPropertyOverload_AllSet()
            ' Equivalent to
            ' <RequiredMember>
            ' Public Class C
            '     <RequiredMember>
            '     Public Overloads Property P1 As Integer
            '     Public Overloads Property P1(x As Integer) As Integer
            '         Get
            '             Return 0
            '         End Get
            '         Set
            '         End Set
            '     End Property
            ' End Class
            Dim il = "
.class public auto ansi C
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .field private int32 _P1

    .method public specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public hidebysig specialname 
        instance int32 get_P1 (
            int32 x
        ) cil managed 
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig specialname 
        instance void set_P1 (
            int32 x,
            int32 Value
        ) cil managed 
    {
        ret
    }

    .method public hidebysig specialname 
        instance int32 get_P1 () cil managed 
    {
        ldarg.0
        ldfld int32 C::_P1
        ret
    }

    .method public hidebysig specialname 
        instance void set_P1 (
            int32 AutoPropertyValue
        ) cil managed 
    {
        ldarg.0
        ldarg.1
        stfld int32 C::_P1
        ret
    }

    .property instance int32 P1(
        int32 x
    )
    {
        .get instance int32 C::get_P1(int32)
        .set instance void C::set_P1(int32, int32)
    }
    .property instance int32 P1()
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
            01 00 00 00
        )
        .get instance int32 C::get_P1()
        .set instance void C::set_P1(int32)
    }
}"

            Dim ilRef = CompileIL(il)

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim c = New C() With { .P1 = 1 }
    End Sub
End Module", {ilRef}, targetFramework:=TargetFramework.Net70)

            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub IndexedProperty_OverloadInDerivedType()
            Dim csharpComp = CreateCSharpCompilationWithRequiredMembers("
public class C1
{
    public required int P1 {get;set;}
}

public class C2 : C1
{
    [System.Runtime.CompilerServices.IndexerNameAttribute(nameof(P1))]
    public int this[int x] => x;
}")

            Dim comp = CreateCompilation("
Module M
    Sub Main()
        Dim c = New C2()
        Dim c2 = New C2() With { .P1 = 1 }
    End Sub
End Module", {csharpComp.EmitToImageReference()}, targetFramework:=TargetFramework.Net70)

            comp.AssertTheseDiagnostics(<expected>
BC37321: Required member 'Public Overloads Property P1 As Integer' must be set in the object initializer or attribute arguments.
        Dim c = New C2()
                    ~~
                                        </expected>)
        End Sub
    End Class
End Namespace
