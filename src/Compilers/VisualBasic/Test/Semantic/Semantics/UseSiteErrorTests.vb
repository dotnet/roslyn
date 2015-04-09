' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class UseSiteErrorTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestPropertyAccessorModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits ILErrors.ClassProperties
    Shared Sub M(c As ILErrors.ClassProperties)
        c.GetSet1 = c.GetSet1
        c.GetSet2 = c.GetSet2
        Dim value As Integer = c.GetSet3
        c.GetSet3 = value
    End Sub
    Sub M()
        Dim value As Integer = GetSet3
        GetSet3 = value
    End Sub
End Class
    </file>
            </compilation>
            Dim compilation = CompileWithMissingReference(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        c.GetSet1 = c.GetSet1
        ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        c.GetSet1 = c.GetSet1
                    ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        c.GetSet2 = c.GetSet2
        ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        c.GetSet2 = c.GetSet2
                    ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        Dim value As Integer = c.GetSet3
                               ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        c.GetSet3 = value
        ~~~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        Dim value As Integer = GetSet3
                               ~~~~~~~
BC30652: Reference required to assembly 'Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'UnavailableClass'. Add one to your project.
        GetSet3 = value
        ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestOverrideMethodReturnType()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits CSharpErrors.ClassMethods

    Public Overrides Function ReturnType1() As UnavailableClass
        Return Nothing
    End Function

    Public Overrides Function ReturnType2() As UnavailableClass()
        Return Nothing
    End Function
End Class
    </file>
            </compilation>

            ' CONSIDER: Dev10 doesn't report the cascading errors (ERR_InvalidOverrideDueToReturn2)
            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "ReturnType1").WithArguments("Public Overrides Function ReturnType1() As UnavailableClass", "Public Overridable Overloads Function ReturnType1() As UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "ReturnType2").WithArguments("Public Overrides Function ReturnType2() As UnavailableClass()", "Public Overridable Overloads Function ReturnType2() As UnavailableClass()"))
        End Sub

        <Fact>
        Public Sub TestOverrideMethodReturnTypeModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits ILErrors.ClassMethods

    Public Overrides Function ReturnType1() As Integer
        Return 0
    End Function

    Public Overrides Function ReturnType2() As Integer()
        Return Nothing
    End Function
End Class
    </file>
            </compilation>

            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ReturnType1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ReturnType2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestOverrideMethodParameterType()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits CSharpErrors.ClassMethods

    Public Overrides Sub ParameterType1(x As UnavailableClass)
    End Sub

    Public Overrides Sub ParameterType2(x As UnavailableClass())
    End Sub
End Class
    </file>
            </compilation>

            ' CONSIDER: Dev10 doesn't report the cascading errors (ERR_OverrideNotNeeded3)
            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_OverrideNotNeeded3, "ParameterType1").WithArguments("sub", "ParameterType1"),
                Diagnostic(ERRID.ERR_OverrideNotNeeded3, "ParameterType2").WithArguments("sub", "ParameterType2"))
        End Sub

        <Fact>
        Public Sub TestOverrideMethodParameterTypeModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits ILErrors.ClassMethods

    Public Overrides Sub ParameterType1(x As Integer)
    End Sub

    Public Overrides Sub ParameterType2(x As Integer())
    End Sub
End Class
    </file>
            </compilation>

            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ParameterType1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ParameterType2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestImplementMethod()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Implements CSharpErrors.InterfaceMethods

    Public Function ReturnType1() As UnavailableClass Implements CSharpErrors.InterfaceMethods.ReturnType1
        Return Nothing
    End Function
    Public Function ReturnType2() As UnavailableClass() Implements CSharpErrors.InterfaceMethods.ReturnType2
        Return Nothing
    End Function

    Public Sub ParameterType1(x As UnavailableClass) Implements CSharpErrors.InterfaceMethods.ParameterType1
    End Sub
    Public Sub ParameterType2(x As UnavailableClass()) Implements CSharpErrors.InterfaceMethods.ParameterType2
    End Sub
End Class
    </file>
            </compilation>

            ' CONSIDER: Dev10 doesn't report the cascading errors (ERR_IdentNotMemberOfInterface4, ERR_UnimplementedMember3)
            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceMethods.ReturnType1").WithArguments("ReturnType1", "ReturnType1", "function", "InterfaceMethods"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceMethods.ReturnType2").WithArguments("ReturnType2", "ReturnType2", "function", "InterfaceMethods"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceMethods.ParameterType1").WithArguments("ParameterType1", "ParameterType1", "sub", "InterfaceMethods"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceMethods.ParameterType2").WithArguments("ParameterType2", "ParameterType2", "sub", "InterfaceMethods"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceMethods").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceMethods").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceMethods").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceMethods").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestImplementMethodModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Implements ILErrors.InterfaceMethods

    Public Function ReturnType1() As Integer Implements ILErrors.InterfaceMethods.ReturnType1
        Return 0
    End Function
    Public Function ReturnType2() As Integer() Implements ILErrors.InterfaceMethods.ReturnType2
        Return Nothing
    End Function

    Public Sub ParameterType1(x As Integer) Implements ILErrors.InterfaceMethods.ParameterType1
    End Sub
    Public Sub ParameterType2(x As Integer()) Implements ILErrors.InterfaceMethods.ParameterType2
    End Sub
End Class
    </file>
            </compilation>

            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ReturnType1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ReturnType2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ParameterType1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "ParameterType2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestOverridePropertyType()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits CSharpErrors.ClassProperties

    Public Overrides ReadOnly Property Get1 As UnavailableClass
        Get
            Return Nothing
        End Get
    End Property
    Public Overrides ReadOnly Property Get2 As UnavailableClass()
        Get
            Return Nothing
        End Get
    End Property

    Public Overrides WriteOnly Property Set1 As UnavailableClass
        Set(value As UnavailableClass)
        End Set
    End Property

    Public Overrides WriteOnly Property Set2 As UnavailableClass()
        Set(value As UnavailableClass())
        End Set
    End Property

    Public Overrides Property GetSet1 As UnavailableClass
        Get
            Return Nothing
        End Get
        Set(value As UnavailableClass)
        End Set
    End Property
    Public Overrides Property GetSet2 As UnavailableClass()
        Get
            Return Nothing
        End Get
        Set(value As UnavailableClass())
        End Set
    End Property
End Class
    </file>
            </compilation>

            ' CONSIDER: it might be nice to suppress the extra ERR_UndefinedType1 that we see for each setter
            ' CONSIDER: Dev10 doesn't report the cascading errors (ERR_InvalidOverrideDueToReturn2)
            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "Get1").WithArguments("Public Overrides ReadOnly Property Get1 As UnavailableClass", "Public Overridable Overloads ReadOnly Property Get1 As UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "Get2").WithArguments("Public Overrides ReadOnly Property Get2 As UnavailableClass()", "Public Overridable Overloads ReadOnly Property Get2 As UnavailableClass()"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "Set1").WithArguments("Public Overrides WriteOnly Property Set1 As UnavailableClass", "Public Overridable Overloads WriteOnly Property Set1 As UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "Set2").WithArguments("Public Overrides WriteOnly Property Set2 As UnavailableClass()", "Public Overridable Overloads WriteOnly Property Set2 As UnavailableClass()"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "GetSet1").WithArguments("Public Overrides Property GetSet1 As UnavailableClass", "Public Overridable Overloads Property GetSet1 As UnavailableClass"),
                Diagnostic(ERRID.ERR_InvalidOverrideDueToReturn2, "GetSet2").WithArguments("Public Overrides Property GetSet2 As UnavailableClass()", "Public Overridable Overloads Property GetSet2 As UnavailableClass()"))
        End Sub

        <Fact>
        Public Sub TestOverridePropertyTypeModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Inherits ILErrors.ClassProperties

    Public Overrides ReadOnly Property Get1 As Integer
        Get
            Return 0
        End Get
    End Property
    Public Overrides ReadOnly Property Get2 As Integer()
        Get
            Return Nothing
        End Get
    End Property

    Public Overrides WriteOnly Property Set1 As Integer
        Set(value As Integer)
        End Set
    End Property

    Public Overrides WriteOnly Property Set2 As Integer()
        Set(value As Integer())
        End Set
    End Property

    Public Overrides Property GetSet1 As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Overrides Property GetSet2 As Integer()
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Class
    </file>
            </compilation>

            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Get1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Get2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Set1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Set2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "GetSet1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "GetSet2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestImplementProperty()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Implements CSharpErrors.InterfaceProperties

    Public ReadOnly Property Get1 As UnavailableClass Implements CSharpErrors.InterfaceProperties.Get1
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Property Get2 As UnavailableClass() Implements CSharpErrors.InterfaceProperties.Get2
        Get
            Return Nothing
        End Get
    End Property

    Public WriteOnly Property Set1 As UnavailableClass Implements CSharpErrors.InterfaceProperties.Set1
        Set(value As UnavailableClass)
        End Set
    End Property

    Public WriteOnly Property Set2 As UnavailableClass() Implements CSharpErrors.InterfaceProperties.Set2
        Set(value As UnavailableClass())
        End Set
    End Property

    Public Property GetSet1 As UnavailableClass Implements CSharpErrors.InterfaceProperties.GetSet1
        Get
            Return Nothing
        End Get
        Set(value As UnavailableClass)
        End Set
    End Property
    Public Property GetSet2 As UnavailableClass() Implements CSharpErrors.InterfaceProperties.GetSet2
        Get
            Return Nothing
        End Get
        Set(value As UnavailableClass())
        End Set
    End Property
End Class
    </file>
            </compilation>

            ' CONSIDER: it might be nice to suppress the extra ERR_UndefinedType1 that we see for each setter
            ' CONSIDER: Dev10 doesn't report the cascading errors (ERR_IdentNotMemberOfInterface4, ERR_UnimplementedMember3)
            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnavailableClass").WithArguments("UnavailableClass"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.Get1").WithArguments("Get1", "Get1", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.Get2").WithArguments("Get2", "Get2", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.Set1").WithArguments("Set1", "Set1", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.Set2").WithArguments("Set2", "Set2", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.GetSet1").WithArguments("GetSet1", "GetSet1", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "CSharpErrors.InterfaceProperties.GetSet2").WithArguments("GetSet2", "GetSet2", "property", "InterfaceProperties"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "CSharpErrors.InterfaceProperties").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact>
        Public Sub TestImplementPropertyModOpt()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Implements ILErrors.InterfaceProperties

    Public ReadOnly Property Get1 As Integer Implements ILErrors.InterfaceProperties.Get1
        Get
            Return 0
        End Get
    End Property
    Public ReadOnly Property Get2 As Integer() Implements ILErrors.InterfaceProperties.Get2
        Get
            Return Nothing
        End Get
    End Property

    Public WriteOnly Property Set1 As Integer Implements ILErrors.InterfaceProperties.Set1
        Set(value As Integer)
        End Set
    End Property

    Public WriteOnly Property Set2 As Integer() Implements ILErrors.InterfaceProperties.Set2
        Set(value As Integer())
        End Set
    End Property

    Public Property GetSet1 As Integer Implements ILErrors.InterfaceProperties.GetSet1
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property GetSet2 As Integer() Implements ILErrors.InterfaceProperties.GetSet2
        Get
            Return Nothing
        End Get
        Set(value As Integer())
        End Set
    End Property
End Class
    </file>
            </compilation>

            CompileWithMissingReference(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "GetSet1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "GetSet2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Get1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Get2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Set1").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"),
                Diagnostic(ERRID.ERR_UnreferencedAssembly3, "Set2").WithArguments("Unavailable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "UnavailableClass"))
        End Sub

        <Fact()>
        Public Sub CompilerGeneratedAttributeNotRequired()
            Dim compilation1 = CompilationUtils.CreateCompilationWithReferences(
            <compilation name="CompilerGeneratedAttributeNotRequired">
                <file name="a.vb">
Class C
    Property Foo as Integer 
End Class
    </file>
            </compilation>, Enumerable.Empty(Of MetadataReference)())

            AssertTheseDiagnostics(compilation1, <errors>
BC30002: Type 'System.Void' is not defined.
Class C
~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'CompilerGeneratedAttributeNotRequired.dll' failed.
Class C
      ~
BC30002: Type 'System.Void' is not defined.
    Property Foo as Integer 
    ~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Int32' is not defined.
    Property Foo as Integer 
                    ~~~~~~~                                                    
                                                </errors>)

            ' the important bit here is, that there is no complaint about a missing CompilerGeneratedAttribute..ctor.
            For Each diag In compilation1.GetDiagnostics()
                Assert.DoesNotContain("System.Runtime.CompilerServices.CompilerGeneratedAttribute", diag.GetMessage, StringComparison.Ordinal)
            Next
        End Sub

        ''' <summary>
        ''' First, compile the provided source with all assemblies and confirm that there are no errors.
        ''' Then, compile the provided source again without the unavailable assembly and return the result.
        ''' </summary>
        Private Shared Function CompileWithMissingReference(sources As XElement) As VisualBasicCompilation
            Dim unavailableAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.Unavailable
            Dim csharpAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.CSharp
            Dim ilAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.IL
            Dim successfulCompilation = CreateCompilationWithMscorlibAndReferences(sources, New MetadataReference() {unavailableAssemblyReference, csharpAssemblyReference, ilAssemblyReference})
            successfulCompilation.VerifyDiagnostics()
            Dim failingCompilation = CreateCompilationWithMscorlibAndReferences(sources, New MetadataReference() {csharpAssemblyReference, ilAssemblyReference})
            Return failingCompilation
        End Function

    End Class
End Namespace
