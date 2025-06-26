' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class VisualBasicCompilerFeatureRequiredTests
        Inherits BaseCompilerFeatureRequiredTests(Of VisualBasicCompilation, XElement)

        Private Class CompilerFeatureRequiredTests_VisualBasic
            Inherits BasicTestBase
        End Class

        Private ReadOnly _testBase As New CompilerFeatureRequiredTests_VisualBasic()

        Friend Overrides Function VisualizeRealIL(peModule As IModuleSymbol, methodData As CompilationTestData.MethodData, markers As IReadOnlyDictionary(Of Integer, String), areLocalsZeroed As Boolean) As String
            Return _testBase.VisualizeRealIL(peModule, methodData, markers, areLocalsZeroed)
        End Function

        Protected Overrides Function GetUsage() As XElement
            Return <compilation>
                       <file name="a.cs"><![CDATA[
#Disable Warning ' Unused locals
Class C
    Public Sub M()
        Dim onType As OnType
        OnType.M()
        OnMethod.M()
        OnMethodReturn.M()
        OnParameter.M(1)
        Dim _1 = OnField.Field
        OnProperty.Property = 1
        Dim _2 = OnProperty.Property
        OnPropertySetter.Property = 1
        Dim _3 = OnPropertySetter.Property
        OnPropertyGetter.Property = 1
        Dim _4 = OnPropertyGetter.Property
        AddHandler OnEvent.Event, AddressOf EmptySub
        RemoveHandler OnEvent.Event, AddressOf EmptySub
        AddHandler OnEventAdder.Event, AddressOf EmptySub
        RemoveHandler OnEventAdder.Event, AddressOf EmptySub
        AddHandler OnEventRemover.Event, AddressOf EmptySub
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
        Dim onEnum As OnEnum
        Dim _5 = OnEnumMember.A
        Dim onClassTypeParameter As OnClassTypeParameter(Of Integer) 
        OnMethodTypeParameter.M(Of Integer)()
        Dim onDelegateType As OnDelegateType 
        OnIndexedPropertyParameter.Property(1) = 1
        Dim _6 = OnIndexedPropertyParameter.Property(1)
        Dim onThis = New OnThisIndexerParameter()
        onThis(1) = 1
        Dim _7 = onThis(1)
    End Sub

    Sub EmptySub()
    End Sub
End Class
]]>
                       </file>
                   </compilation>
        End Function

        Protected Overrides Function CreateCompilationWithIL(source As XElement, ilSource As String) As VisualBasicCompilation
            Return CreateCompilationWithCustomILSource(source, ilSource)
        End Function

        Protected Overrides Function CreateCompilation(source As XElement, references() As MetadataReference) As VisualBasicCompilation
            Return CompilationUtils.CreateCompilation(source, references)
        End Function

        Protected Overrides Function CompileAndVerify(compilation As VisualBasicCompilation) As CompilationVerifier
            Return _testBase.CompileAndVerify(compilation)
        End Function

        Protected Overrides Sub AssertNormalErrors(comp As VisualBasicCompilation)
            comp.AssertTheseDiagnostics(<errors>
                                            <![CDATA[
BC37319: 'OnType' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onType As OnType
                      ~~~~~~
BC37319: 'OnType' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnType.M()
               ~
BC37319: 'Public Shared Overloads Sub M()' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethod.M()
                 ~
BC37319: 'Param As Void' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodReturn.M()
                       ~
BC37319: 'param As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnParameter.M(1)
                    ~
BC37319: 'Public Shared Field As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _1 = OnField.Field
                 ~~~~~~~~~~~~~
BC37319: 'Public Shared Overloads Property [Property] As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnProperty.Property = 1
                   ~~~~~~~~
BC37319: 'Public Shared Overloads Property [Property] As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _2 = OnProperty.Property
                            ~~~~~~~~
BC37319: 'Public Shared Overloads Property Set [Property](value As Integer)' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertySetter.Property = 1
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'Public Shared Overloads Property Get [Property]() As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _4 = OnPropertyGetter.Property
                 ~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'Public Shared Event [Event] As Action' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEvent.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~
BC37319: 'Public Shared Event [Event] As Action' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEvent.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~
BC37319: 'Public Shared Overloads AddHandler Event [Event](value As Action)' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventAdder.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~~~~~
BC37319: 'Public Shared Overloads RemoveHandler Event [Event](value As Action)' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnEnum' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onEnum As OnEnum
                      ~~~~~~
BC37319: 'A' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _5 = OnEnumMember.A
                 ~~~~~~~~~~~~~~
BC37319: 'T' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onClassTypeParameter As OnClassTypeParameter(Of Integer) 
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'T' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodTypeParameter.M(Of Integer)()
                              ~~~~~~~~~~~~~
BC37319: 'OnDelegateType' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onDelegateType As OnDelegateType 
                              ~~~~~~~~~~~~~~
BC37319: 'param As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnIndexedPropertyParameter.Property(1) = 1
                                   ~~~~~~~~
BC37319: 'param As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _6 = OnIndexedPropertyParameter.Property(1)
                                            ~~~~~~~~
BC37319: 'i As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        onThis(1) = 1
        ~~~~~~
BC37319: 'i As Integer' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _7 = onThis(1)
                 ~~~~~~
]]>
                                        </errors>)

            Dim onType = comp.GetTypeByMetadataName("OnType")
            Assert.True(onType.HasUnsupportedMetadata)
            Assert.True(onType.GetMember(Of MethodSymbol)("M").HasUnsupportedMetadata)

            Dim onMethod = comp.GetTypeByMetadataName("OnMethod")
            Assert.False(onMethod.HasUnsupportedMetadata)
            Assert.True(onMethod.GetMember(Of MethodSymbol)("M").HasUnsupportedMetadata)

            Dim onMethodReturn = comp.GetTypeByMetadataName("OnMethodReturn")
            Assert.False(onMethodReturn.HasUnsupportedMetadata)
            Assert.True(onMethodReturn.GetMember(Of MethodSymbol)("M").HasUnsupportedMetadata)

            Dim onParameter = comp.GetTypeByMetadataName("OnParameter")
            Assert.False(onParameter.HasUnsupportedMetadata)
            Dim onParameterMethod = onParameter.GetMember(Of MethodSymbol)("M")
            Assert.True(onParameterMethod.HasUnsupportedMetadata)
            Assert.True(onParameterMethod.Parameters(0).HasUnsupportedMetadata)

            Dim onField = comp.GetTypeByMetadataName("OnField")
            Assert.False(onField.HasUnsupportedMetadata)
            Assert.True(onField.GetMember(Of FieldSymbol)("Field").HasUnsupportedMetadata)

            Dim onProperty = comp.GetTypeByMetadataName("OnProperty")
            Assert.False(onProperty.HasUnsupportedMetadata)
            Assert.True(onProperty.GetMember(Of PropertySymbol)("Property").HasUnsupportedMetadata)

            Dim onPropertyGetter = comp.GetTypeByMetadataName("OnPropertyGetter")
            Assert.False(onPropertyGetter.HasUnsupportedMetadata)
            Dim onPropertyGetterProperty = onPropertyGetter.GetMember(Of PropertySymbol)("Property")
            Assert.False(onPropertyGetterProperty.HasUnsupportedMetadata)
            Assert.False(onPropertyGetterProperty.SetMethod.HasUnsupportedMetadata)
            Assert.True(onPropertyGetterProperty.GetMethod.HasUnsupportedMetadata)

            Dim onPropertySetter = comp.GetTypeByMetadataName("OnPropertySetter")
            Assert.False(onPropertySetter.HasUnsupportedMetadata)
            Dim onPropertySetterProperty = onPropertySetter.GetMember(Of PropertySymbol)("Property")
            Assert.False(onPropertySetterProperty.HasUnsupportedMetadata)
            Assert.True(onPropertySetterProperty.SetMethod.HasUnsupportedMetadata)
            Assert.False(onPropertySetterProperty.GetMethod.HasUnsupportedMetadata)

            Dim onEvent = comp.GetTypeByMetadataName("OnEvent")
            Assert.False(onEvent.HasUnsupportedMetadata)
            Assert.True(onEvent.GetMember(Of EventSymbol)("Event").HasUnsupportedMetadata)

            Dim onEventAdder = comp.GetTypeByMetadataName("OnEventAdder")
            Assert.False(onEventAdder.HasUnsupportedMetadata)
            Dim onEventAdderEvent = onEventAdder.GetMember(Of EventSymbol)("Event")
            Assert.False(onEventAdderEvent.HasUnsupportedMetadata)
            Assert.True(onEventAdderEvent.AddMethod.HasUnsupportedMetadata)
            Assert.False(onEventAdderEvent.RemoveMethod.HasUnsupportedMetadata)

            Dim onEventRemover = comp.GetTypeByMetadataName("OnEventRemover")
            Assert.False(onEventRemover.HasUnsupportedMetadata)
            Dim onEventRemoverEvent = onEventRemover.GetMember(Of EventSymbol)("Event")
            Assert.False(onEventRemoverEvent.HasUnsupportedMetadata)
            Assert.False(onEventRemoverEvent.AddMethod.HasUnsupportedMetadata)
            Assert.True(onEventRemoverEvent.RemoveMethod.HasUnsupportedMetadata)

            Dim onEnum = comp.GetTypeByMetadataName("OnEnum")
            Assert.True(onEnum.HasUnsupportedMetadata)

            Dim onEnumMember = comp.GetTypeByMetadataName("OnEnumMember")
            Assert.False(onEnumMember.HasUnsupportedMetadata)
            Assert.True(onEnumMember.GetMember(Of FieldSymbol)("A").HasUnsupportedMetadata)

            Dim onClassTypeParameter = comp.GetTypeByMetadataName("OnClassTypeParameter`1")
            Assert.True(onClassTypeParameter.HasUnsupportedMetadata)
            Assert.True(onClassTypeParameter.TypeParameters(0).HasUnsupportedMetadata)

            Dim onMethodTypeParameter = comp.GetTypeByMetadataName("OnMethodTypeParameter")
            Assert.False(onMethodTypeParameter.HasUnsupportedMetadata)
            Dim onMethodTypeParameterMethod = onMethodTypeParameter.GetMember(Of MethodSymbol)("M")
            Assert.True(onMethodTypeParameterMethod.HasUnsupportedMetadata)
            Assert.True(onMethodTypeParameterMethod.TypeParameters(0).HasUnsupportedMetadata)

            Dim onDelegateType = comp.GetTypeByMetadataName("OnDelegateType")
            Assert.True(onDelegateType.HasUnsupportedMetadata)

            Dim onIndexedPropertyParameter = comp.GetTypeByMetadataName("OnIndexedPropertyParameter")
            Assert.False(onIndexedPropertyParameter.HasUnsupportedMetadata)
            Assert.True(onIndexedPropertyParameter.GetMember(Of MethodSymbol)("get_Property").Parameters(0).HasUnsupportedMetadata)
            Assert.True(onIndexedPropertyParameter.GetMember(Of MethodSymbol)("set_Property").Parameters(0).HasUnsupportedMetadata)

            Dim onThisParameterIndexer = comp.GetTypeByMetadataName("OnThisIndexerParameter")
            Assert.False(onThisParameterIndexer.HasUnsupportedMetadata)
            Dim indexer = onThisParameterIndexer.GetMember(Of PropertySymbol)("Item")
            Assert.True(indexer.HasUnsupportedMetadata)
            Assert.True(indexer.Parameters(0).HasUnsupportedMetadata)
        End Sub

        Protected Overrides Sub AssertModuleErrors(comp As VisualBasicCompilation, ilRef As MetadataReference)
            comp.AssertTheseDiagnostics(<errors>
                                            <![CDATA[
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onType As OnType
                      ~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnType.M()
               ~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethod.M()
        ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethod.M()
                 ~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodReturn.M()
        ~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodReturn.M()
                       ~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnParameter.M(1)
        ~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnParameter.M(1)
                    ~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _1 = OnField.Field
                 ~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _1 = OnField.Field
                 ~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnProperty.Property = 1
        ~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnProperty.Property = 1
                   ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _2 = OnProperty.Property
                 ~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _2 = OnProperty.Property
                            ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertySetter.Property = 1
        ~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertySetter.Property = 1
                         ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _3 = OnPropertySetter.Property
                 ~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _3 = OnPropertySetter.Property
                                  ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertyGetter.Property = 1
        ~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertyGetter.Property = 1
                         ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _4 = OnPropertyGetter.Property
                 ~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _4 = OnPropertyGetter.Property
                                  ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEvent.Event, AddressOf EmptySub
                   ~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEvent.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEvent.Event, AddressOf EmptySub
                      ~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEvent.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventAdder.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventAdder.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventAdder.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventAdder.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventRemover.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventRemover.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onEnum As OnEnum
                      ~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _5 = OnEnumMember.A
                 ~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _5 = OnEnumMember.A
                 ~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onClassTypeParameter As OnClassTypeParameter(Of Integer) 
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodTypeParameter.M(Of Integer)()
        ~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodTypeParameter.M(Of Integer)()
                              ~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onDelegateType As OnDelegateType 
                              ~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnIndexedPropertyParameter.Property(1) = 1
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnIndexedPropertyParameter.Property(1) = 1
                                   ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _6 = OnIndexedPropertyParameter.Property(1)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _6 = OnIndexedPropertyParameter.Property(1)
                                            ~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onThis = New OnThisIndexerParameter()
                         ~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onThis = New OnThisIndexerParameter()
                         ~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        onThis(1) = 1
        ~~~~~~
BC37319: 'OnModule' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _7 = onThis(1)
                 ~~~~~~
]]>
                                        </errors>)

            Assert.True(comp.GetReferencedAssemblySymbol(ilRef).Modules.Single().HasUnsupportedMetadata)
        End Sub

        Protected Overrides Sub AssertAssemblyErrors(comp As VisualBasicCompilation, ilRef As MetadataReference)
            comp.AssertTheseDiagnostics(<errors>
                                            <![CDATA[
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onType As OnType
                      ~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnType.M()
               ~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethod.M()
        ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethod.M()
                 ~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodReturn.M()
        ~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodReturn.M()
                       ~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnParameter.M(1)
        ~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnParameter.M(1)
                    ~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _1 = OnField.Field
                 ~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _1 = OnField.Field
                 ~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnProperty.Property = 1
        ~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnProperty.Property = 1
                   ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _2 = OnProperty.Property
                 ~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _2 = OnProperty.Property
                            ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertySetter.Property = 1
        ~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertySetter.Property = 1
                         ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _3 = OnPropertySetter.Property
                 ~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _3 = OnPropertySetter.Property
                                  ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertyGetter.Property = 1
        ~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnPropertyGetter.Property = 1
                         ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _4 = OnPropertyGetter.Property
                 ~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _4 = OnPropertyGetter.Property
                                  ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEvent.Event, AddressOf EmptySub
                   ~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEvent.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEvent.Event, AddressOf EmptySub
                      ~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEvent.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventAdder.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventAdder.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventAdder.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventAdder.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventRemover.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        AddHandler OnEventRemover.Event, AddressOf EmptySub
                   ~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        RemoveHandler OnEventRemover.Event, AddressOf EmptySub
                      ~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onEnum As OnEnum
                      ~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _5 = OnEnumMember.A
                 ~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _5 = OnEnumMember.A
                 ~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onClassTypeParameter As OnClassTypeParameter(Of Integer) 
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodTypeParameter.M(Of Integer)()
        ~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnMethodTypeParameter.M(Of Integer)()
                              ~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onDelegateType As OnDelegateType 
                              ~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnIndexedPropertyParameter.Property(1) = 1
        ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        OnIndexedPropertyParameter.Property(1) = 1
                                   ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _6 = OnIndexedPropertyParameter.Property(1)
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _6 = OnIndexedPropertyParameter.Property(1)
                                            ~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onThis = New OnThisIndexerParameter()
                         ~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim onThis = New OnThisIndexerParameter()
                         ~~~~~~~~~~~~~~~~~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        onThis(1) = 1
        ~~~~~~
BC37319: 'AssemblyTest, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' requires compiler feature 'test', which is not supported by this version of the Visual Basic compiler.
        Dim _7 = onThis(1)
                 ~~~~~~
]]>
                                        </errors>)

            Assert.True(comp.GetReferencedAssemblySymbol(ilRef).HasUnsupportedMetadata)
        End Sub

        <Fact>
        Public Sub Application()
            Dim compilation = CompilationUtils.CreateCompilation(<compilation>
                                                                     <file name="a.cs"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<CompilerFeatureRequired("OnType")>
Public Class OnType
End Class

Public Class OnMethod
    <CompilerFeatureRequired("OnMethod")>
    Public Sub OnMethod()
    End Sub
End Class

Public Class OnMethodReturn
    Public Function OnMethodReturn() As <CompilerFeatureRequired("OnMethodReturn")> Integer
        Return 1
    End Function
End Class

Public Class OnField
    <CompilerFeatureRequired("OnField")>
    Public Field As Integer
End Class

Public Class OnProperty
    <CompilerFeatureRequired("OnProperty")>
    Public Property Prop As Integer
End Class

Public Class OnPropertySetter
    Public Property Prop As Integer
        Get
            Return 1
        End Get
        <CompilerFeatureRequired("OnPropertySetter")>
        Set
        End Set
    End Property
End Class

Public Class OnPropertyGetter
    Public Property Prop As Integer
        <CompilerFeatureRequired("OnPropertyGetter")>
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class

Public Class OnEvent
    <CompilerFeatureRequired("OnEvent")>
    Public Event [Event] As Action
End Class

Public Class OnEventAdder
    Public Custom Event [Event] As Action
        <CompilerFeatureRequired("OnEventAdder")>
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class

Public Class OnEventRemover
    Public Custom Event [Event] As Action
        AddHandler(value As Action)
        End AddHandler
        <CompilerFeatureRequired("OnEventRemover")>
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent
        End RaiseEvent
    End Event
End Class

Public Class OnEventRaiseEvent
    Public Custom Event [Event] As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        <CompilerFeatureRequired("OnEventRaiseEvent")>
        RaiseEvent
        End RaiseEvent
    End Event
End Class

<CompilerFeatureRequired("OnEnum")>
Public Enum OnEnum
    A
End Enum

Public Enum OnEnumMember
    <CompilerFeatureRequired("OnEnumMember")> A
End Enum

<CompilerFeatureRequired("OnDelegate")>
Public Delegate Sub OnDelegate()
]]>
                                                                     </file>
                                                                     <file name="CompilerFeatureRequiredAttribute.cs"><![CDATA[
Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.All)>
    Public Class CompilerFeatureRequiredAttribute
        Inherits Attribute

        Public Sub New(featureName As String)
            FeatureName = featureName
        End Sub

        Public ReadOnly Property FeatureName As String

        Public Property IsOptional As Boolean
    End Class
End Namespace
]]>
                                                                     </file>
                                                                 </compilation>)

            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
<CompilerFeatureRequired("OnType")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    <CompilerFeatureRequired("OnMethod")>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    Public Function OnMethodReturn() As <CompilerFeatureRequired("OnMethodReturn")> Integer
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    <CompilerFeatureRequired("OnField")>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    <CompilerFeatureRequired("OnProperty")>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
        <CompilerFeatureRequired("OnPropertySetter")>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
        <CompilerFeatureRequired("OnPropertyGetter")>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    <CompilerFeatureRequired("OnEvent")>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
        <CompilerFeatureRequired("OnEventAdder")>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
        <CompilerFeatureRequired("OnEventRemover")>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
        <CompilerFeatureRequired("OnEventRaiseEvent")>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
<CompilerFeatureRequired("OnEnum")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
    <CompilerFeatureRequired("OnEnumMember")> A
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37320: 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute' is reserved for compiler usage only.
<CompilerFeatureRequired("OnDelegate")>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

]]></errors>)
        End Sub
    End Class
End Namespace
