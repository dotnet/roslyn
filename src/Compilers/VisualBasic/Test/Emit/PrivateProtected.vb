' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class PrivateProtected
        Inherits BasicTestBase

        Private Shared ReadOnly s_defaultProvider As DesktopStrongNameProvider = New SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create(Of String)())

        <Fact>
        Public Sub RejectIncompatibleModifiers()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    <compilation>
                        <file name="a.vb">
                            <![CDATA[
Public Class Base
    Private Friend Field1 As Integer
    Friend Private Field2 As Integer
    Private Friend Protected Field3 As Integer
    Friend Protected Private Field4 As Integer
End Class
]]>
                        </file>
                    </compilation>,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Friend Field1 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Friend Private Field2 As Integer
           ~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Friend Protected Field3 As Integer
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Friend Protected Private Field4 As Integer
                     ~~~~~~~
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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

            compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
    Private Protected Field1 As Integer
    Protected Private Field2 As Integer
End Class
]]>
                              </file>
                          </compilation>
            Dim baseCompilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source1,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(baseCompilation,
<errors>
</errors>)

            Dim source2 = <compilation>
                              <file name="a.vb">
                                  <![CDATA[
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
            Dim derivedCompilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(baseCompilation)},
                    assemblyName:="WantsIVTAccessButCantHave",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30389: 'Base.Field1' is not accessible in this context because it is 'Private Protected'.
        Field1 = 1
        ~~~~~~
BC30389: 'Base.Field2' is not accessible in this context because it is 'Private Protected'.
        Field2 = 2
        ~~~~~~
</errors>)
            derivedCompilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={MetadataReference.CreateFromImage(baseCompilation.EmitToArray())},
                    assemblyName:="WantsIVTAccessButCantHave",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
BC30389: 'Base.Field1' is not accessible in this context because it is 'Private Protected'.
        Field1 = 1
        ~~~~~~
BC30389: 'Base.Field2' is not accessible in this context because it is 'Private Protected'.
        Field2 = 2
        ~~~~~~
</errors>)
            derivedCompilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source2,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5),
                    additionalRefs:={New VisualBasicCompilationReference(baseCompilation)},
                    assemblyName:="WantsIVTAccess",
                    options:=TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider))
            CompilationUtils.AssertTheseDiagnostics(derivedCompilation,
<errors>
</errors>)
            derivedCompilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
    Private Protected Sub M1(x As Internal)
    End Sub
    Private Protected Sub M2(x As Prot)
    End Sub
    Private Protected Class Nested
        Private Protected Sub M1(x As Internal)
        End Sub
        Private Protected Sub M2(x As Prot)
        End Sub
    End Class
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Field As Integer                                       ' 1
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Property Prop As Integer                               ' 2
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Function M() As Integer : Return 1 : End Function      ' 3
            ~~~~~~
BC30040: First statement of a method body cannot be on the same line as the method declaration.
    Private Public Function M() As Integer : Return 1 : End Function      ' 3
                                             ~~~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Class C : End Class                                    ' 4
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Structure S : End Structure                            ' 5
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Enum E : End Enum                                      ' 6
            ~~~~~~
BC30280: Enum 'E' must contain at least one member.
    Private Public Enum E : End Enum                                      ' 6
                        ~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
    Private Public Event V As System.Action                               ' 7
            ~~~~~~
BC30176: Only one of 'Public', 'Private', 'Protected', 'Friend', or 'Protected Friend' can be specified.
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
</errors>)
            compilation = CreateCompilationWithMscorlibAndVBRuntime(
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
    End Class
End Namespace


