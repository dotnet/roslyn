' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class PrivateProtected
        Inherits BasicTestBase

        Private Shared ReadOnly s_defaultProvider As StrongNameProvider = SigningTestHelpers.DefaultDesktopStrongNameProvider

        <Fact>
        Public Sub RejectIncompatibleModifiers()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Public Class Base
    Private Friend Field1 As Integer
    Friend Private Field2 As Integer
    Private Friend Protected Field3 As Integer
    Friend Protected Private Field4 As Integer
    Private Public Protected Field5 As Integer
    Private ReadOnly Protected Field6 As Integer ' ok
End Class
]]>
                        </file>
                    </compilation>,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Friend Field1 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Friend Private Field2 As Integer
           ~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Friend Protected Field3 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Friend Protected Private Field4 As Integer
                     ~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Protected Field5 As Integer
            ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AccessibleWhereRequired_01()
            Dim sources = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class Base
    Private Protected Field1 As Integer
    Protected Private Field2 As Integer
End Class

Public Class Derived
        Inherits Base
    Sub M()
        Field1 = 1
        Field2 = 2
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    sources,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36716: Visual Basic 15.3 does not support Private Protected.
    Private Protected Field1 As Integer
            ~~~~~~~~~
BC36716: Visual Basic 15.3 does not support Private Protected.
    Protected Private Field2 As Integer
              ~~~~~~~
</errors>)

            compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    sources,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub AccessibleWhereRequired_02()
            Dim source1 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("WantsIVTAccess")>
Public Class Base
    Private Protected Const Constant As Integer = 3
    Private Protected Field1 As Integer
    Protected Private Field2 As Integer
    Private Protected Sub Method() : End Sub
    Private Protected Event Event1 As System.Action
    Private Protected WriteOnly Property Property1
        Set
        End Set
    End Property
    Public Property Property2
        Private Protected Set
        End Set
        Get
            Return 4
        End Get
    End Property
    Private Protected Sub New()
    End Sub
    Public Sub New(x As String) ' Unused
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim baseCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source1,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(baseCompilation,
<errors>
</errors>)
            Dim bb = CType(baseCompilation.GlobalNamespace.GetMember("Base"), INamedTypeSymbol)
            For Each member In bb.GetMembers()
                Select Case member.Name
                    Case "Property2", "Event1Event", "get_Property2", ".ctor"
                        ' not expected to be private protected
                    Case Else
                        If Accessibility.ProtectedAndInternal <> member.DeclaredAccessibility Then
                            Assert.Equal("private protected member", member.Name)
                        End If
                        Assert.Equal(Accessibility.ProtectedAndInternal, member.DeclaredAccessibility)
                End Select
            Next

            Dim source2 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Public Class Derived
        Inherits Base
    Sub M()
        Field1 = Constant
        Field2 = 2
        Method()
        AddHandler Event1, Sub()
                           End Sub
        Property1 = 3
        Property2 = 4
    End Sub
    Sub New(x As Integer)
        MyBase.New()
    End Sub
    Sub New(x As Long)
        ' MyBase.New()
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(baseCompilation)},
                    assemblyName:="WantsIVTAccessButCantHave",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30389: 'Base.Field1' is not accessible in this context because it is 'Private Protected'.
        Field1 = Constant
        ~~~~~~
BC30389: 'Base.Constant' is not accessible in this context because it is 'Private Protected'.
        Field1 = Constant
                 ~~~~~~~~
BC30389: 'Base.Field2' is not accessible in this context because it is 'Private Protected'.
        Field2 = 2
        ~~~~~~
BC30390: 'Base.Private Protected Sub Method()' is not accessible in this context because it is 'Private Protected'.
        Method()
        ~~~~~~
BC30389: 'Base.Event1' is not accessible in this context because it is 'Private Protected'.
        AddHandler Event1, Sub()
                   ~~~~~~
BC30389: 'Base.Property1' is not accessible in this context because it is 'Private Protected'.
        Property1 = 3
        ~~~~~~~~~
BC31102: 'Set' accessor of property 'Property2' is not accessible.
        Property2 = 4
        ~~~~~~~~~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public Sub New(x As String)'.
        MyBase.New()
               ~~~
BC30148: First statement of this 'Sub New' must be a call to 'MyBase.New' or 'MyClass.New' because base class 'Base' of 'Derived' does not have an accessible 'Sub New' that can be called with no arguments.
    Sub New(x As Long)
        ~~~
</errors>)
            derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={MetadataReference.CreateFromImage(baseCompilation.EmitToArray())},
                    assemblyName:="WantsIVTAccessButCantHave",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30389: 'Base.Field1' is not accessible in this context because it is 'Private Protected'.
        Field1 = Constant
        ~~~~~~
BC30389: 'Base.Constant' is not accessible in this context because it is 'Private Protected'.
        Field1 = Constant
                 ~~~~~~~~
BC30389: 'Base.Field2' is not accessible in this context because it is 'Private Protected'.
        Field2 = 2
        ~~~~~~
BC30390: 'Base.Private Protected Sub Method()' is not accessible in this context because it is 'Private Protected'.
        Method()
        ~~~~~~
BC30389: 'Base.Event1' is not accessible in this context because it is 'Private Protected'.
        AddHandler Event1, Sub()
                   ~~~~~~
BC30389: 'Base.Property1' is not accessible in this context because it is 'Private Protected'.
        Property1 = 3
        ~~~~~~~~~
BC31102: 'Set' accessor of property 'Property2' is not accessible.
        Property2 = 4
        ~~~~~~~~~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public Sub New(x As String)'.
        MyBase.New()
               ~~~
BC30148: First statement of this 'Sub New' must be a call to 'MyBase.New' or 'MyClass.New' because base class 'Base' of 'Derived' does not have an accessible 'Sub New' that can be called with no arguments.
    Sub New(x As Long)
        ~~~
</errors>)
            derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(baseCompilation)},
                    assemblyName:="WantsIVTAccess",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
</errors>)
            derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={MetadataReference.CreateFromImage(baseCompilation.EmitToArray())},
                    assemblyName:="WantsIVTAccess",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub NotInStructOrNamespace()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Private Protected Structure S
    Private Protected Field As Integer
End Structure
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31047: Protected types can only be declared inside of a class.
Private Protected Structure S
                            ~
BC31089: Types declared 'Private' must be inside another type.
Private Protected Structure S
                            ~
BC30435: Members in a Structure cannot be declared 'Protected'.
    Private Protected Field As Integer
            ~~~~~~~~~
</errors>)
        End Sub

        ' keeping test name equivalent to C# test name, though VB uses different terminology
        <Fact>
        Public Sub NotInStaticClass()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Private Protected Module Frog
    Private Protected Field As Integer
End Module
Public NotInheritable Class D
    Private Protected Field As Integer
    Protected Field2 As Integer
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31047: Protected types can only be declared inside of a class.
Private Protected Module Frog
                         ~~~~
BC31089: Types declared 'Private' must be inside another type.
Private Protected Module Frog
                         ~~~~
BC30593: Variables in Modules cannot be declared 'Protected'.
    Private Protected Field As Integer
            ~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub NestedTypes()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Class Outer
    Private Protected Class Inner
    End Class
End Class
Class Derived
    Inherits Outer
    Public Sub M()
        Dim x As Outer.Inner = Nothing
    End Sub
End Class
Class NotDerived
    Public Sub M()
        Dim x As Outer.Inner = Nothing '' Error: Outer.Inner not accessible
    End Sub
End Class
Structure Struct
    Private Protected Class Inner '' Error: Not allowed in Structure
    End Class
End Structure
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30389: 'Outer.Inner' is not accessible in this context because it is 'Private Protected'.
        Dim x As Outer.Inner = Nothing '' Error: Outer.Inner not accessible
                 ~~~~~~~~~~~
BC31047: Protected types can only be declared inside of a class.
    Private Protected Class Inner '' Error: Not allowed in Structure
                            ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub PermittedAccessorProtection()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Class Clazz
    Public Property Prop1 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)
        End Set
    End Property
    Protected Friend Property Prop2 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)
        End Set
    End Property
    Protected Property Prop3 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)
        End Set
    End Property
    Friend Property Prop4 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)
        End Set
    End Property
    Private Protected Property Prop5 As Integer
        Get
            Return 1
        End Get
        Private Set(value As Integer)
        End Set
    End Property
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub ForbiddenAccessorProtection_01()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Class Clazz
    Private Protected Property Prop1 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)

        End Set
    End Property
    Private Property Prop2 As Integer
        Get
            Return 1
        End Get
        Private Protected Set(value As Integer)

        End Set
    End Property
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31100: Access modifier 'Private' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
        Private Protected Set(value As Integer)
        ~~~~~~~
BC31100: Access modifier 'Private' is not valid. The access modifier of 'Get' and 'Set' should be more restrictive than the property access level.
        Private Protected Set(value As Integer)
        ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub ForbiddenAccessorProtection_02()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Interface ISomething
    Private Protected Sub M()
End Interface
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30270: 'Private' is not valid on an interface method declaration.
    Private Protected Sub M()
    ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub AtLeastAsRestrictivePositive_01()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class C
    Friend Class Internal
    End Class
    Protected Class Prot
    End Class
    Private Protected Class PrivateProtected
    End Class
    Private Protected Sub M1(x As Internal)
    End Sub
    Private Protected Sub M2(x As Prot)
    End Sub
    Private Protected Sub M3(x As PrivateProtected)
    End Sub
    Private Protected Class Nested
        Private Protected Sub M1(x As Internal)
        End Sub
        Private Protected Sub M2(x As Prot)
        End Sub
        Private Protected Sub M3(x As PrivateProtected)
        End Sub
    End Class
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub AtLeastAsRestrictiveNegative_01()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Container
    Private Protected Class PrivateProtected
    End Class
    Friend Sub M1(x As PrivateProtected) '' error: conflicting access
    End Sub
    Protected Sub M2(x As PrivateProtected) '' error: conflicting access
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30508: 'x' cannot expose type 'Container.PrivateProtected' in namespace '&lt;Default>' through class 'Container'.
    Friend Sub M1(x As PrivateProtected) '' error: conflicting access
                       ~~~~~~~~~~~~~~~~
BC30909: 'x' cannot expose type 'Container.PrivateProtected' outside the project through class 'Container'.
    Protected Sub M2(x As PrivateProtected) '' error: conflicting access
                          ~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub DuplicateAccessInBinder()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Container
    Private Public Field As Integer                                       ' 1
    Private Public Property Prop As Integer                               ' 2
    Private Public Function M() As Integer : Return 1 : End Function      ' 3
    Private Public Class C : End Class                                    ' 4
    Private Public Structure S : End Structure                            ' 5
    Private Public Enum E : End Enum                                      ' 6
    Private Public Event V As System.Action                               ' 7
    Private Public Interface I : End Interface                            ' 8
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Field As Integer                                       ' 1
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Property Prop As Integer                               ' 2
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Function M() As Integer : Return 1 : End Function      ' 3
            ~~~~~~
BC30040: First statement of a method body cannot be on the same line as the method declaration.
    Private Public Function M() As Integer : Return 1 : End Function      ' 3
                                             ~~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Class C : End Class                                    ' 4
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Structure S : End Structure                            ' 5
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Enum E : End Enum                                      ' 6
            ~~~~~~
BC30280: Enum 'E' must contain at least one member.
    Private Public Enum E : End Enum                                      ' 6
                        ~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Event V As System.Action                               ' 7
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', 'Protected Friend', or 'Private Protected' can be specified.
    Private Public Interface I : End Interface                            ' 8
            ~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub RequiredVersion()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Container
    Private Protected Field As Integer
    Private Protected Property Prop As Integer
    Private Protected Function M() As Integer
        Return 1 
    End Function
    Private Protected Class C : End Class
    Private Protected Structure S : End Structure
    Private Protected Enum E : Value : End Enum
    Private Protected Event V As System.Action
    Private Protected Interface I : End Interface
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
            compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Field As Integer
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Property Prop As Integer
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Function M() As Integer
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Class C : End Class
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Structure S : End Structure
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Enum E : Value : End Enum
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Event V As System.Action
            ~~~~~~~~~
BC36716: Visual Basic 15.0 does not support Private Protected.
    Private Protected Interface I : End Interface
            ~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub VerifyPrivateProtectedIL()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Program
    Private Protected Sub M() : End Sub
    Private Protected F As Integer
End Class
]]>
                             </file>
                         </compilation>
            CompileAndVerify(source,
                             parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                             expectedSignatures:=
                             {
                                Signature("Program", "M", ".method famandassem instance System.Void M() cil managed"),
                                Signature("Program", "F", ".field famandassem instance System.Int32 F")
                             })
        End Sub

        <Fact>
        Public Sub VerifyPartialPartsMatch()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Outer
    Private Protected Partial Class Inner : End Class
    Private           Partial Class Inner : End Class
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
            source = <compilation>
                         <file name="a.vb">
                             <![CDATA[
Public Class Outer
    Private Protected Partial Class Inner : End Class
    Private Protected Partial Class Inner : End Class
End Class
]]>
                         </file>
                     </compilation>
            compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub VerifyProtectedSemantics()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Class Base
    Private Protected Sub M()
        System.Console.WriteLine(Me.GetType().Name)
    End Sub
End Class

Class Derived
    Inherits Base

    Public Sub Main()
        Dim derived As Derived = new Derived()
        derived.M()
        Dim bb As Base = new Base()
        bb.M() ' error 1
        Dim other As Other = new Other()
        other.M() ' error 2
    End Sub
End Class

Class Other
    Inherits Base
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30390: 'Base.Private Protected Sub M()' is not accessible in this context because it is 'Private Protected'.
        bb.M() ' error 1
        ~~~~
BC30390: 'Base.Private Protected Sub M()' is not accessible in this context because it is 'Private Protected'.
        other.M() ' error 2
        ~~~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub HidingAbstract()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
MustInherit Class A
    Friend MustOverride Sub F()
End Class
MustInherit Class B
    Inherits A
    Private Protected Shadows Sub F()
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC31404: 'Private Protected Sub F()' cannot shadow a method declared 'MustOverride'.
    Private Protected Shadows Sub F()
                                  ~
</errors>)
        End Sub

        <Fact>
        Public Sub HidingInaccessible()
            Dim source1 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Public Class A
    Private Protected Sub F()
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
                    source1,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
</errors>)

            Dim source2 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Class B
    Inherits A
    Shadows Sub F()
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(compilation1)})
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
</errors>)
        End Sub

        <Fact>
        Public Sub UnimplementedInaccessible()
            Dim source1 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Public MustInherit Class A
    Private Protected MustOverride Sub F()
End Class
]]>
                              </file>
                          </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
                    source1,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
</errors>)

            Dim source2 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Class B
    Inherits A
End Class
]]>
                              </file>
                          </compilation>
            Dim derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(compilation1)})
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30610: Class 'B' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    A: Private Protected MustOverride Sub F().
Class B
      ~
</errors>)
        End Sub

        <Fact>
        Public Sub ImplementInaccessible()
            Dim source1 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Public MustInherit Class A
    Private Protected MustOverride Sub F()
End Class
]]>
                              </file>
                          </compilation>
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
                    source1,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
</errors>)

            Dim source2 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
Class B
    Inherits A
    Private Protected Overrides Sub F()
    End Sub
End Class
]]>
                              </file>
                          </compilation>
            Dim derivedCompilation = CreateCompilationWithMscorlib40AndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(compilation1)})
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30610: Class 'B' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 
    A: Private Protected MustOverride Sub F().
Class B
      ~
BC31417: 'Private Protected Overrides Sub F()' cannot override 'Private Protected MustOverride Sub F()' because it is not accessible in this context.
    Private Protected Overrides Sub F()
                                    ~
</errors>)
        End Sub

        <Fact>
        Public Sub VerifyPPExtension()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Module M
    <Extension()>
    Private Protected Sub SomeExtension(s As Integer) ' error: Methods in a Module cannot be declared 'Protected'.
    End Sub
End Module

Class C
    Shared Sub M(s As String)
        s.SomeExtension() ' error: no accessible SomeExtension
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.References.SystemCore},
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30433: Methods in a Module cannot be declared 'Protected'.
    Private Protected Sub SomeExtension(s As Integer) ' error: Methods in a Module cannot be declared 'Protected'.
            ~~~~~~~~~
BC30456: 'SomeExtension' is not a member of 'String'.
        s.SomeExtension() ' error: no accessible SomeExtension
        ~~~~~~~~~~~~~~~
</errors>)
        End Sub
    End Class
End Namespace

