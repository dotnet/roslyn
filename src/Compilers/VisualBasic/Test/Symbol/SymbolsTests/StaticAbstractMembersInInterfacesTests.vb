' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    <CompilerTrait(CompilerFeature.DefaultInterfaceImplementation)>
    Public Class StaticAbstractMembersInInterfacesTests
        Inherits BasicTestBase

        Private Const _supportingFramework As TargetFramework = TargetFramework.Net60

        Private Function GetCSharpCompilation(
            csSource As String,
            Optional additionalReferences As MetadataReference() = Nothing,
            Optional targetFramework As TargetFramework = _supportingFramework,
            Optional compilationOptions As CSharp.CSharpCompilationOptions = Nothing
        ) As CSharp.CSharpCompilation
            Return CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(targetFramework, additionalReferences),
                                           compilationOptions:=compilationOptions)
        End Function

        <Fact>
        Public Sub DefineAbstractStaticMethod_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Sub M1()
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30270: 'Shared' is not valid on an interface method declaration.
    Shared Sub M1()
    ~~~~~~
</errors>
            )

            Dim i1M1 = comp1.GetMember(Of MethodSymbol)("I1.M1")
            Assert.False(i1M1.IsShared)
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticMethod_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Sub M1()
    End Sub
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30270: 'Overridable' is not valid on an interface method declaration.
    Overridable Shared Sub M1()
    ~~~~~~~~~~~
BC30501: 'Shared' cannot be combined with 'Overridable' on a method declaration.
    Overridable Shared Sub M1()
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    End Sub
    ~~~~~~~
</errors>
            )

            Dim i1M1 = comp1.GetMember(Of MethodSymbol)("I1.M1")
            Assert.False(i1M1.IsShared)
        End Sub

        Private Function GetModifierAndBody(isVirtual As Boolean) As (modifier As String, body As String)
            If isVirtual Then
                Return ("virtual", " => throw null;")
            Else
                Return ("abstract", ";")
            End If
        End Function

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticMethod_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " void M1()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Sub M1()'.
    Implements I1
               ~~
</errors>
            )

            Dim i1M1 = comp1.GetMember(Of MethodSymbol)("I1.M1")
            Assert.Empty(i1M1.ExplicitInterfaceImplementations)
            Assert.Null(i1M1.ContainingType.FindImplementationForInterfaceMember(i1M1))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1M1))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticMethod_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " void M1()" + md.body + " 
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Sub M1()'.
    Implements I1
               ~~
BC30401: 'M1' cannot implement 'M1' because there is no matching sub on interface 'I1'.
    Sub M1() Implements I1.M1
                        ~~~~~
</errors>
            )

            Dim i1M1 = comp1.GetMember(Of MethodSymbol)("I1.M1")
            Assert.Empty(i1M1.ExplicitInterfaceImplementations)
            Assert.Null(i1M1.ContainingType.FindImplementationForInterfaceMember(i1M1))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1M1))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticMethod_03(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " void M1()" + md.body + " 
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Shared Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Sub M1()'.
    Implements I1
               ~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Sub M1() Implements I1.M1
    ~~~~~~
</errors>
            )

            Dim i1M1 = comp1.GetMember(Of MethodSymbol)("I1.M1")
            Assert.Empty(i1M1.ExplicitInterfaceImplementations)
            Assert.Null(i1M1.ContainingType.FindImplementationForInterfaceMember(i1M1))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1M1))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared Sub MT1(x As I1)
        I1.M01()
        x.M01()
        I1.M04()
        x.M04()
    End Sub

    Shared Sub MT2(Of T As I1)()
        T.M01()
        T.M03()
        T.M04()
        T.M00()
        T.M05()

        Dim x = CType(Sub() T.M01(), System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.M01()
        ~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.M01()
        ~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.M01()
        ~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.M04()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.M01()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.M03()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.M04()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.M00()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.M05()
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() T.M01(), System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared s As String

    Shared Sub MT1(x As I1)
        s = nameof(I1.M01)
        s = nameof(x.M01)
        s = nameof(I1.M04)
        s = nameof(x.M04)
    End Sub

    Shared Sub MT2(Of T As I1)()
        s = nameof(T.M01)
        s = nameof(T.M03)
        s = nameof(T.M04)
        s = nameof(T.M00)
        s = nameof(T.M05)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC32098: Type parameters cannot be used as qualifiers.
        s = nameof(T.M01)
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        s = nameof(T.M03)
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        s = nameof(T.M04)
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        s = nameof(T.M00)
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        s = nameof(T.M05)
                   ~~~~~

</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_AddressOf_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _d As System.Action

    Shared Sub MT1(x As I1)
        _d = AddressOf I1.M01
        _d = AddressOf x.M01
        _d = AddressOf I1.M04
        _d = AddressOf x.M04
    End Sub

    Shared Sub MT2(Of T As I1)()
        _d = AddressOf T.M01
        _d = AddressOf T.M03
        _d = AddressOf T.M04
        _d = AddressOf T.M00
        _d = AddressOf T.M05

        Dim x = CType(Function() AddressOf T.M01, System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = AddressOf I1.M01
                       ~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = AddressOf x.M01
             ~~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = AddressOf x.M01
                       ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = AddressOf x.M04
             ~~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = AddressOf T.M01
                       ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = AddressOf T.M03
                       ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = AddressOf T.M04
                       ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = AddressOf T.M00
                       ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = AddressOf T.M05
                       ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Function() AddressOf T.M01, System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
                                           ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_AddressOf_DirectCastToDelegate_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _d As System.Action

    Shared Sub MT1(x As I1)
        _d = DirectCast(AddressOf I1.M01, System.Action)
        _d = DirectCast(AddressOf x.M01, System.Action)
        _d = DirectCast(AddressOf I1.M04, System.Action)
        _d = DirectCast(AddressOf x.M04, System.Action)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _d = DirectCast(AddressOf T.M01, System.Action)
        _d = DirectCast(AddressOf T.M03, System.Action)
        _d = DirectCast(AddressOf T.M04, System.Action)
        _d = DirectCast(AddressOf T.M00, System.Action)
        _d = DirectCast(AddressOf T.M05, System.Action)

        Dim x = CType(Function() DirectCast(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = DirectCast(AddressOf I1.M01, System.Action)
                                  ~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = DirectCast(AddressOf x.M01, System.Action)
                        ~~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = DirectCast(AddressOf x.M01, System.Action)
                                  ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = DirectCast(AddressOf x.M04, System.Action)
                        ~~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = DirectCast(AddressOf T.M01, System.Action)
                                  ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = DirectCast(AddressOf T.M03, System.Action)
                                  ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = DirectCast(AddressOf T.M04, System.Action)
                                  ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = DirectCast(AddressOf T.M00, System.Action)
                                  ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = DirectCast(AddressOf T.M05, System.Action)
                                  ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Function() DirectCast(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
                                                      ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_AddressOf_TryCastToDelegate_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _d As System.Action

    Shared Sub MT1(x As I1)
        _d = TryCast(AddressOf I1.M01, System.Action)
        _d = TryCast(AddressOf x.M01, System.Action)
        _d = TryCast(AddressOf I1.M04, System.Action)
        _d = TryCast(AddressOf x.M04, System.Action)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _d = TryCast(AddressOf T.M01, System.Action)
        _d = TryCast(AddressOf T.M03, System.Action)
        _d = TryCast(AddressOf T.M04, System.Action)
        _d = TryCast(AddressOf T.M00, System.Action)
        _d = TryCast(AddressOf T.M05, System.Action)

        Dim x = CType(Function() TryCast(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = TryCast(AddressOf I1.M01, System.Action)
                               ~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = TryCast(AddressOf x.M01, System.Action)
                     ~~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = TryCast(AddressOf x.M01, System.Action)
                               ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = TryCast(AddressOf x.M04, System.Action)
                     ~~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = TryCast(AddressOf T.M01, System.Action)
                               ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = TryCast(AddressOf T.M03, System.Action)
                               ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = TryCast(AddressOf T.M04, System.Action)
                               ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = TryCast(AddressOf T.M00, System.Action)
                               ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = TryCast(AddressOf T.M05, System.Action)
                               ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Function() TryCast(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
                                                   ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_AddressOf_CTypeToDelegate_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _d As System.Action

    Shared Sub MT1(x As I1)
        _d = CType(AddressOf I1.M01, System.Action)
        _d = CType(AddressOf x.M01, System.Action)
        _d = CType(AddressOf I1.M04, System.Action)
        _d = CType(AddressOf x.M04, System.Action)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _d = CType(AddressOf T.M01, System.Action)
        _d = CType(AddressOf T.M03, System.Action)
        _d = CType(AddressOf T.M04, System.Action)
        _d = CType(AddressOf T.M00, System.Action)
        _d = CType(AddressOf T.M05, System.Action)

        Dim x = CType(Function() CType(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = CType(AddressOf I1.M01, System.Action)
                             ~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = CType(AddressOf x.M01, System.Action)
                   ~~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = CType(AddressOf x.M01, System.Action)
                             ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = CType(AddressOf x.M04, System.Action)
                   ~~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = CType(AddressOf T.M01, System.Action)
                             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = CType(AddressOf T.M03, System.Action)
                             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = CType(AddressOf T.M04, System.Action)
                             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = CType(AddressOf T.M00, System.Action)
                             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = CType(AddressOf T.M05, System.Action)
                             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Function() CType(AddressOf T.M01, System.Action), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
                                                 ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticMethod_AddressOf_DelegateCreation_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static void M01()" + md.body + "

    void M03()
    {
    }

    static void M04() {}

    protected " + md.modifier + " static void M05()" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _d As System.Action

    Shared Sub MT1(x As I1)
        _d = New System.Action(AddressOf I1.M01)
        _d = New System.Action(AddressOf x.M01)
        _d = New System.Action(AddressOf I1.M04)
        _d = New System.Action(AddressOf x.M04)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _d = New System.Action(AddressOf T.M01)
        _d = New System.Action(AddressOf T.M03)
        _d = New System.Action(AddressOf T.M04)
        _d = New System.Action(AddressOf T.M00)
        _d = New System.Action(AddressOf T.M05)

        Dim x = CType(Function() New System.Action(AddressOf T.M01), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = New System.Action(AddressOf I1.M01)
                                         ~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = New System.Action(AddressOf x.M01)
                               ~~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _d = New System.Action(AddressOf x.M01)
                                         ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _d = New System.Action(AddressOf x.M04)
                               ~~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = New System.Action(AddressOf T.M01)
                                         ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = New System.Action(AddressOf T.M03)
                                         ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = New System.Action(AddressOf T.M04)
                                         ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = New System.Action(AddressOf T.M00)
                                         ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _d = New System.Action(AddressOf T.M05)
                                         ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Function() New System.Action(AddressOf T.M01), System.Linq.Expressions.Expression(Of System.Func(Of System.Action)))
                                                             ~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineAbstractStaticProperty_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Property P1 As Integer
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30273: 'Shared' is not valid on an interface property declaration.
    Shared Property P1 As Integer
    ~~~~~~
</errors>
            )

            Dim i1P1 = comp1.GetMember(Of PropertySymbol)("I1.P1")
            Assert.False(i1P1.IsShared)
            Assert.False(i1P1.GetMethod.IsShared)
            Assert.False(i1P1.SetMethod.IsShared)
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticProperty_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Property P1 As Integer
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30273: 'Overridable' is not valid on an interface property declaration.
    Overridable Shared Property P1 As Integer
    ~~~~~~~~~~~
BC30502: 'Shared' cannot be combined with 'Overridable' on a property declaration.
    Overridable Shared Property P1 As Integer
    ~~~~~~~~~~~
</errors>
            )

            Dim i1P1 = comp1.GetMember(Of PropertySymbol)("I1.P1")
            Assert.False(i1P1.IsShared)
            Assert.False(i1P1.GetMethod.IsShared)
            Assert.False(i1P1.SetMethod.IsShared)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticProperty_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " int P1 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Property P1 As Integer'.
    Implements I1
               ~~
</errors>
            )

            Dim i1P1 = comp1.GetMember(Of PropertySymbol)("I1.P1")
            Assert.Empty(i1P1.ExplicitInterfaceImplementations)
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.SetMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.SetMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticProperty_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " int P1 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Property P1 As Integer'.
    Implements I1
               ~~
BC30401: 'P1' cannot implement 'P1' because there is no matching property on interface 'I1'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
</errors>
            )

            Dim i1P1 = comp1.GetMember(Of PropertySymbol)("I1.P1")
            Assert.Empty(i1P1.ExplicitInterfaceImplementations)
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.SetMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.SetMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticProperty_03(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " int P1 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Shared Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Property P1 As Integer'.
    Implements I1
               ~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Property P1 As Integer Implements I1.P1
    ~~~~~~
</errors>
            )

            Dim i1P1 = comp1.GetMember(Of PropertySymbol)("I1.P1")
            Assert.Empty(i1P1.ExplicitInterfaceImplementations)
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(i1P1.ContainingType.FindImplementationForInterfaceMember(i1P1.SetMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.GetMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1P1.SetMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticPropertyGet_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static int P01 { get; set;}

    static int P04 { get; set; }

    protected " + md.modifier + " static int P05 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer

    Shared Sub MT1(x As I1)
        _i = I1.P01
        _i = x.P01
        _i = I1.P04
        _i = x.P04
    End Sub

    Shared Sub MT2(Of T As I1)()
        _i = T.P01
        _i = T.P03
        _i = T.P04
        _i = T.P00
        _i = T.P05

        Dim x = CType(Sub() T.P01.ToString(), System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = I1.P01
             ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = x.P01
             ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x.P01
             ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x.P04
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.P01
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.P03
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.P04
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.P00
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.P05
             ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() T.P01.ToString(), System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticPropertySet_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static int P01 { get; set;}

    static int P04 { get; set; }

    protected " + md.modifier + " static int P05 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared Sub MT1(x As I1)
        I1.P01 = 1
        x.P01 = 1
        I1.P04 = 1
        x.P04 = 1
    End Sub

    Shared Sub MT2(Of T As I1)()
        T.P01 = 1
        T.P03 = 1
        T.P04 = 1
        T.P00 = 1
        T.P05 = 1

        Dim x = CType(Sub() T.P01 = 1, System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.P01 = 1
        ~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.P01 = 1
        ~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.P01 = 1
        ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.P04 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P01 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P03 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P04 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P00 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P05 = 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() T.P01 = 1, System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Dim x = CType(Sub() T.P01 = 1, System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticPropertyCompound_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static int P01 { get; set;}

    static int P04 { get; set; }

    protected " + md.modifier + " static int P05 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared Sub MT1(x As I1)
        I1.P01 += 1
        x.P01 += 1
        I1.P04 += 1
        x.P04 += 1
    End Sub

    Shared Sub MT2(Of T As I1)()
        T.P01 += 1
        T.P03 += 1
        T.P04 += 1
        T.P00 += 1
        T.P05 += 1

        Dim x = CType(Sub() T.P01 += 1, System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.P01 += 1
        ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.P01 += 1
        ~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.P01 += 1
        ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.P01 += 1
        ~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.P01 += 1
        ~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.P04 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P01 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P03 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P04 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P00 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.P05 += 1
        ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() T.P01 += 1, System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~
BC36534: Expression cannot be converted into an expression tree.
        Dim x = CType(Sub() T.P01 += 1, System.Linq.Expressions.Expression(Of System.Action))
                            ~~~~~~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticProperty_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static int P01 { get; set;}

    static int P04 { get; set; }

    protected " + md.modifier + " static int P05 { get; set; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _s As String

    Shared Sub MT1(x As I1)
        _s = nameof(I1.P01)
        _s = nameof(x.P01)
        _s = nameof(I1.P04)
        _s = nameof(x.P04)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _s = nameof(T.P01)
        _s = nameof(T.P03)
        _s = nameof(T.P04)
        _s = nameof(T.P00)
        _s = nameof(T.P05)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P01)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P03)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P04)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P00)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P05)
                    ~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub ConsumeAbstractStaticIndexedProperty_03()

            Dim ilSource =
"
.class interface public auto ansi abstract I1
{
    .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
        01 00 04 49 74 65 6d 00 00
    )

    // Methods
    .method public hidebysig specialname newslot abstract virtual 
        static int32 get_Item (
            int32 x
        ) cil managed 
    {
    } // end of method I1::get_Item

    .method public hidebysig specialname newslot abstract virtual 
        static void set_Item (
            int32 x,
            int32 'value'
        ) cil managed 
    {
    } // end of method I1::set_Item

    // Properties
    .property int32 Item(
        int32 x
    )
    {
        .get int32 I1::get_Item(int32)
        .set void I1::set_Item(int32, int32)
    }

} // end of class I1
"

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer
    Shared _s As String

    Shared Sub MT1(x As I1)
        _i = I1.Item(0)
        I1.Item(0) = 1
        I1.Item(0) += 1
        _i = x.Item(0)
        x.Item(0) = 1
        x.Item(0) += 1
        _s = nameof(I1.Item)
        _s = nameof(x.Item)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _i = T.Item(0)
        T.Item(0) = 1
        T.Item(0) += 1
        _s = nameof(T.Item)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={CreateReferenceFromIlCode(ilSource)})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = I1.Item(0)
             ~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) = 1
        ~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) += 1
        ~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) += 1
        ~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x.Item(0)
             ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = x.Item(0)
             ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.Item(0) = 1
        ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) = 1
        ~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.Item(0) += 1
        ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) += 1
        ~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) += 1
        ~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.Item(0)
             ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.Item(0) = 1
        ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.Item(0) += 1
        ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.Item)
                    ~~~~~~
</errors>
            )

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer

    Shared Sub MT1(x As I1)
        _i = I1(0)
        I1(0) = 1
        I1(0) += 1
        _i = x(0)
        x(0) = 1
        x(0) += 1
    End Sub

    Shared Sub MT2(Of T As I1)()
        _i = T(0)
        T(0) = 1
        T(0) += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, targetFramework:=_supportingFramework, references:={CreateReferenceFromIlCode(ilSource)})
            comp2.AssertTheseDiagnostics(
<errors>
BC30111: 'I1' is an interface type and cannot be used as an expression.
        _i = I1(0)
             ~~
BC30111: 'I1' is an interface type and cannot be used as an expression.
        I1(0) = 1
        ~~
BC30111: 'I1' is an interface type and cannot be used as an expression.
        I1(0) += 1
        ~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x(0)
             ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = x(0)
             ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x(0) = 1
        ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x(0) = 1
        ~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x(0) += 1
        ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x(0) += 1
        ~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x(0) += 1
        ~~~~~~~~~
BC30108: 'T' is a type and cannot be used as an expression.
        _i = T(0)
             ~
BC30108: 'T' is a type and cannot be used as an expression.
        T(0) = 1
        ~
BC30108: 'T' is a type and cannot be used as an expression.
        T(0) += 1
        ~
</errors>
            )

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer

    Shared Sub MT1(x As I1)
        _i = I1!a
        I1!a = 1
        I1!a += 1
        _i = x!a
        x!a = 1
        x!a += 1
    End Sub

    Shared Sub MT2(Of T As I1)()
        _i = T!a
        T!a = 1
        T!a += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, targetFramework:=_supportingFramework, references:={CreateReferenceFromIlCode(ilSource)})
            comp3.AssertTheseDiagnostics(
<errors>
BC30111: 'I1' is an interface type and cannot be used as an expression.
        _i = I1!a
             ~~
BC30111: 'I1' is an interface type and cannot be used as an expression.
        I1!a = 1
        ~~
BC30111: 'I1' is an interface type and cannot be used as an expression.
        I1!a += 1
        ~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x!a
             ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = x!a
             ~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x!a = 1
        ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x!a = 1
        ~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x!a += 1
        ~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x!a += 1
        ~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x!a += 1
        ~~~~~~~~
BC30108: 'T' is a type and cannot be used as an expression.
        _i = T!a
             ~
BC30108: 'T' is a type and cannot be used as an expression.
        T!a = 1
        ~
BC30108: 'T' is a type and cannot be used as an expression.
        T!a += 1
        ~
</errors>
            )
        End Sub

        <Fact>
        Public Sub ConsumeAbstractStaticIndexedProperty_04()

            Dim ilSource =
"
.class interface public auto ansi abstract I1
{
    // Methods
    .method public hidebysig specialname newslot abstract virtual 
        static int32 get_Item (
            int32 x
        ) cil managed 
    {
    } // end of method I1::get_Item

    .method public hidebysig specialname newslot abstract virtual 
        static void set_Item (
            int32 x,
            int32 'value'
        ) cil managed 
    {
    } // end of method I1::set_Item

    // Properties
    .property int32 Item(
        int32 x
    )
    {
        .get int32 I1::get_Item(int32)
        .set void I1::set_Item(int32, int32)
    }

} // end of class I1
"

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer
    Shared _s As String

    Shared Sub MT1(x As I1)
        _i = I1.Item(0)
        I1.Item(0) = 1
        I1.Item(0) += 1
        _i = x.Item(0)
        x.Item(0) = 1
        x.Item(0) += 1
        _s = nameof(I1.Item)
        _s = nameof(x.Item)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _i = T.Item(0)
        T.Item(0) = 1
        T.Item(0) += 1
        _s = nameof(T.Item)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={CreateReferenceFromIlCode(ilSource)})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = I1.Item(0)
             ~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) = 1
        ~~~~~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) += 1
        ~~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        I1.Item(0) += 1
        ~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        _i = x.Item(0)
             ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        _i = x.Item(0)
             ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.Item(0) = 1
        ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) = 1
        ~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.Item(0) += 1
        ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) += 1
        ~~~~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        x.Item(0) += 1
        ~~~~~~~~~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _i = T.Item(0)
             ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.Item(0) = 1
        ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        T.Item(0) += 1
        ~~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.Item)
                    ~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineAbstractStaticEvent_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Event E1 As System.Action
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30275: 'Shared' is not valid on an interface event declaration.
    Shared Event E1 As System.Action
    ~~~~~~
</errors>
            )

            Dim i1E1 = comp1.GetMember(Of EventSymbol)("I1.E1")
            Assert.False(i1E1.IsShared)
            Assert.False(i1E1.AddMethod.IsShared)
            Assert.False(i1E1.RemoveMethod.IsShared)
            Assert.Null(i1E1.RaiseMethod)
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticEvent_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Event E1 As System.Action
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30243: 'Overridable' is not valid on an event declaration.
    Overridable Shared Event E1 As System.Action
    ~~~~~~~~~~~
BC30275: 'Overridable' is not valid on an interface event declaration.
    Overridable Shared Event E1 As System.Action
    ~~~~~~~~~~~
</errors>
            )

            Dim i1E1 = comp1.GetMember(Of EventSymbol)("I1.E1")
            Assert.False(i1E1.IsShared)
            Assert.False(i1E1.AddMethod.IsShared)
            Assert.False(i1E1.RemoveMethod.IsShared)
            Assert.Null(i1E1.RaiseMethod)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticEvent_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " event System.Action E1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Event E1 As Action'.
    Implements I1
               ~~
</errors>
            )

            Dim i1E1 = comp1.GetMember(Of EventSymbol)("I1.E1")
            Assert.Empty(i1E1.ExplicitInterfaceImplementations)
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.RemoveMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.RemoveMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticEvent_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " event System.Action E1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Event E1 As System.Action Implements I1.E1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Event E1 As Action'.
    Implements I1
               ~~
BC30401: 'E1' cannot implement 'E1' because there is no matching event on interface 'I1'.
    Event E1 As System.Action Implements I1.E1
                                         ~~~~~
</errors>
            )

            Dim i1E1 = comp1.GetMember(Of EventSymbol)("I1.E1")
            Assert.Empty(i1E1.ExplicitInterfaceImplementations)
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.RemoveMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.RemoveMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractStaticEvent_03(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    static " + md.modifier + " event System.Action E1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Shared Event E1 As System.Action Implements I1.E1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'C' cannot implement interface 'I1' because it contains shared abstract or virtual 'Event E1 As Action'.
    Implements I1
               ~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Event E1 As System.Action Implements I1.E1
    ~~~~~~
</errors>
            )

            Dim i1E1 = comp1.GetMember(Of EventSymbol)("I1.E1")
            Assert.Empty(i1E1.ExplicitInterfaceImplementations)
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(i1E1.ContainingType.FindImplementationForInterfaceMember(i1E1.RemoveMethod))

            Dim c = comp1.GetMember(Of NamedTypeSymbol)("C")
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.AddMethod))
            Assert.Null(c.FindImplementationForInterfaceMember(i1E1.RemoveMethod))
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticEventAdd_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static event System.Action P01;

    static event System.Action P04;

    protected " + md.modifier + " static event System.Action P05;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer

    Shared Sub MT1(x As I1)
        AddHandler I1.P01, Nothing
        AddHandler x.P01, Nothing
        AddHandler I1.P04, Nothing
        AddHandler x.P04, Nothing
    End Sub

    Shared Sub MT2(Of T As I1)()
        AddHandler T.P01, Nothing
        AddHandler T.P03, Nothing
        AddHandler T.P04, Nothing
        AddHandler T.P00, Nothing
        AddHandler T.P05, Nothing

        Dim x = CType(Sub() AddHandler T.P01, Nothing, System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        AddHandler I1.P01, Nothing
                   ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        AddHandler x.P01, Nothing
                   ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler x.P01, Nothing
                   ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        AddHandler x.P04, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        AddHandler T.P01, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        AddHandler T.P03, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        AddHandler T.P04, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        AddHandler T.P00, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        AddHandler T.P05, Nothing
                   ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() AddHandler T.P01, Nothing, System.Linq.Expressions.Expression(Of System.Action))
                                       ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticEventRemove_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static event System.Action P01;

    static event System.Action P04;

    protected " + md.modifier + " static event System.Action P05;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _i As Integer

    Shared Sub MT1(x As I1)
        RemoveHandler I1.P01, Nothing
        RemoveHandler x.P01, Nothing
        RemoveHandler I1.P04, Nothing
        RemoveHandler x.P04, Nothing
    End Sub

    Shared Sub MT2(Of T As I1)()
        RemoveHandler T.P01, Nothing
        RemoveHandler T.P03, Nothing
        RemoveHandler T.P04, Nothing
        RemoveHandler T.P00, Nothing
        RemoveHandler T.P05, Nothing

        Dim x = CType(Sub() RemoveHandler T.P01, Nothing, System.Linq.Expressions.Expression(Of System.Action))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37314: A shared abstract or virtual interface member cannot be accessed.
        RemoveHandler I1.P01, Nothing
                      ~~~~~~
BC37314: A shared abstract or virtual interface member cannot be accessed.
        RemoveHandler x.P01, Nothing
                      ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        RemoveHandler x.P01, Nothing
                      ~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        RemoveHandler x.P04, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        RemoveHandler T.P01, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        RemoveHandler T.P03, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        RemoveHandler T.P04, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        RemoveHandler T.P00, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        RemoveHandler T.P05, Nothing
                      ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        Dim x = CType(Sub() RemoveHandler T.P01, Nothing, System.Linq.Expressions.Expression(Of System.Action))
                                          ~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractStaticEvent_02(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static event System.Action P01;

    static event System.Action P04;

    protected " + md.modifier + " static event System.Action P05;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _s As String

    Shared Sub MT1(x As I1)
        _s = nameof(I1.P01)
        _s = nameof(x.P01)
        _s = nameof(I1.P04)
        _s = nameof(x.P04)
    End Sub

    Shared Sub MT2(Of T As I1)()
        _s = nameof(T.P01)
        _s = nameof(T.P03)
        _s = nameof(T.P04)
        _s = nameof(T.P00)
        _s = nameof(T.P05)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P01)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P03)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P04)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P00)
                    ~~~~~
BC32098: Type parameters cannot be used as qualifiers.
        _s = nameof(T.P05)
                    ~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineAbstractStaticOperator_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Operator + (x as I1) as I1

    Shared Operator - (x as I1, y as I1) as I1
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30603: Statement cannot appear within an interface body.
    Shared Operator + (x as I1) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Shared Operator - (x as I1, y as I1) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticOperator_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Operator + (x as I1) as I1

    Overridable Shared Operator - (x as I1, y as I1) as I1
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Operator + (x as I1) as I1
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Operator + (x as I1) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Operator - (x as I1, y as I1) as I1
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Operator - (x as I1, y as I1) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineAbstractStaticOperator_02()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Operator IsTrue (x as I1) as Boolean

    Shared Operator IsFalse (x as I1) as Boolean
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30603: Statement cannot appear within an interface body.
    Shared Operator IsTrue (x as I1) as Boolean
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Shared Operator IsFalse (x as I1) as Boolean
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticOperator_02()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Operator IsTrue (x as I1) as Boolean

    Overridable Shared Operator IsFalse (x as I1) as Boolean
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Operator IsTrue (x as I1) as Boolean
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Operator IsTrue (x as I1) as Boolean
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Operator IsFalse (x as I1) as Boolean
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Operator IsFalse (x as I1) as Boolean
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractUnaryOperator_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static I1 operator - (I1 x)" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'Test' cannot implement interface 'I1' because it contains shared abstract or virtual 'Operator -(x As I1) As I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ImplementAbstractBinaryOperator_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static I1 operator - (I1 x, I1 y)" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'Test' cannot implement interface 'I1' because it contains shared abstract or virtual 'Operator -(x As I1, y As I1) As I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractUnaryOperator_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static I1 operator - (I1 x)" + md.body + "
}

public interface I2<T> where T : I2<T>
{
    " + md.modifier + " static T operator - (T x)" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _o As Object

    Shared Sub MT1(x As I1)
        _o = -x
    End Sub

    Shared Sub MT2(Of T As I1)(y as T)
        _o = -y

        Dim x = CType(Function() -y, System.Linq.Expressions.Expression(Of System.Func(Of Object)))
    End Sub

    Shared Sub MT3(Of T As I2(Of T))(z As T)
        _o = -z
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30487: Operator '-' is not defined for type 'I1'.
        _o = -x
             ~~
BC30487: Operator '-' is not defined for type 'T'.
        _o = -y
             ~~
BC30487: Operator '-' is not defined for type 'T'.
        Dim x = CType(Function() -y, System.Linq.Expressions.Expression(Of System.Func(Of Object)))
                                 ~~
BC30487: Operator '-' is not defined for type 'T'.
        _o = -z
             ~~
</errors>
            )
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub ConsumeAbstractBinaryOperator_01(isVirtual As Boolean)
            Dim md = GetModifierAndBody(isVirtual)

            Dim csSource =
"
public interface I1
{
    " + md.modifier + " static I1 operator - (I1 x, I1 y)" + md.body + "
}

public interface I2<T> where T : I2<T>
{
    " + md.modifier + " static T operator - (T x, T y)" + md.body + "
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test
    Shared _o As Object

    Shared Sub MT1(x1 As I1, x2 As I1)
        _o = x1 - x2
    End Sub

    Shared Sub MT2(Of T As I1)(y1 as T, y2 as T)
        _o = y1 - y2

        Dim x = CType(Function() y1 - y2, System.Linq.Expressions.Expression(Of System.Func(Of Object)))
    End Sub

    Shared Sub MT3(Of T As I2(Of T))(z1 As T, z2 As T)
        _o = z1 - z2
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30452: Operator '-' is not defined for types 'I1' and 'I1'.
        _o = x1 - x2
             ~~~~~~~
BC30452: Operator '-' is not defined for types 'T' and 'T'.
        _o = y1 - y2
             ~~~~~~~
BC30452: Operator '-' is not defined for types 'T' and 'T'.
        Dim x = CType(Function() y1 - y2, System.Linq.Expressions.Expression(Of System.Func(Of Object)))
                                 ~~~~~~~
BC30452: Operator '-' is not defined for types 'T' and 'T'.
        _o = z1 - z2
             ~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineAbstractStaticConversion_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Shared Widening Operator CType (x as Integer) as I1

    Shared Narrowing Operator CType (x as I1) as Integer
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC30603: Statement cannot appear within an interface body.
    Shared Widening Operator CType (x as Integer) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Shared Narrowing Operator CType (x as I1) as Integer
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub DefineVirtualStaticConversion_01()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I1
    Overridable Shared Widening Operator CType (x as Integer) as I1

    Overridable Shared Narrowing Operator CType (x as I1) as Integer
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework)
            comp1.AssertTheseDiagnostics(
<errors>
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Widening Operator CType (x as Integer) as I1
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Widening Operator CType (x as Integer) as I1
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33013: Operators cannot be declared 'Overridable'.
    Overridable Shared Narrowing Operator CType (x as I1) as Integer
    ~~~~~~~~~~~
BC30603: Statement cannot appear within an interface body.
    Overridable Shared Narrowing Operator CType (x as I1) as Integer
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub ImplementAbstractConversionOperator_01()

            Dim csSource =
"
public interface I1<T> where T : I1<T>
{
    abstract static implicit operator int(T x);
}"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test(Of T As I1(Of T))
    Implements I1(Of T)
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC37315: Class 'Test' cannot implement interface 'I1(Of T)' because it contains shared abstract or virtual 'Function op_Implicit(x As T) As Integer'.
    Implements I1(Of T)
               ~~~~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub ConsumeAbstractConversionOperator_01()

            Dim csSource =
"
public interface I1<T> where T : I1<T>
{
    abstract static implicit operator int(T x);
}"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test(Of T As I1(Of T))

    Shared Function MT1(x As I1(Of T)) As Integer
        Dim y = CType(Function() x, System.Linq.Expressions.Expression(Of System.Func(Of Integer)))

        Return x
    End Function

    Shared Function MT2(y as T) As Integer
        Dim x = CType(Function() y, System.Linq.Expressions.Expression(Of System.Func(Of Integer)))

        Return y
    End Function
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=_supportingFramework, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30311: Value of type 'I1(Of T As I1(Of T))' cannot be converted to 'Integer'.
        Dim y = CType(Function() x, System.Linq.Expressions.Expression(Of System.Func(Of Integer)))
                                 ~
BC30311: Value of type 'I1(Of T As I1(Of T))' cannot be converted to 'Integer'.
        Return x
               ~
BC30311: Value of type 'T' cannot be converted to 'Integer'.
        Dim x = CType(Function() y, System.Linq.Expressions.Expression(Of System.Func(Of Integer)))
                                 ~
BC30311: Value of type 'T' cannot be converted to 'Integer'.
        Return y
               ~
</errors>
            )
        End Sub

    End Class
End Namespace
