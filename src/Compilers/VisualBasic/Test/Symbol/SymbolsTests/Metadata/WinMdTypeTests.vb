' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata

    Public Class WinMdTypeTests
        Inherits BasicTestBase

        ''' <summary>
        ''' Verify that value__ in enums in WinRt are marked as public.
        ''' By default value__ is actually private and must be changed
        ''' by the compiler.
        ''' 
        ''' We check the enum Windows.UI.Xaml.Controls.Primitives.
        '''  ComponentResourceLocation
        ''' </summary>
        <Fact>
        Public Sub WinMdEnum()
            Dim source =
           <compilation>
               <file name="a.vb">
                    Public Class abcdef
                    End Class
                </file>
           </compilation>
            Dim comp = CreateWinRtCompilation(source)
            Dim winmdlib = comp.ExternalReferences(0)
            Dim winmdNS = comp.GetReferencedAssemblySymbol(winmdlib)

            Dim wns1 = winmdNS.GlobalNamespace.GetMember(Of NamespaceSymbol)("Windows")
            wns1 = wns1.GetMember(Of NamespaceSymbol)("UI")
            wns1 = wns1.GetMember(Of NamespaceSymbol)("Xaml")
            wns1 = wns1.GetMember(Of NamespaceSymbol)("Controls")
            wns1 = wns1.GetMember(Of NamespaceSymbol)("Primitives")
            Dim type = wns1.GetMember(Of PENamedTypeSymbol)("ComponentResourceLocation")
            Dim value = type.GetMember(Of FieldSymbol)("value__")
            Assert.Equal(value.DeclaredAccessibility, Accessibility.Public)
        End Sub

        <Fact, WorkItem(1169511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1169511")>
        Public Sub WinMdAssemblyQualifiedType()
            Dim source =
           <compilation>
               <file name="a.vb"><![CDATA[
<MyAttribute(GetType(C1))>
Public Class C
    Public Shared Sub Main()
    End Sub
End Class

Public Class MyAttribute
    Inherits System.Attribute
    Sub New(type As System.Type)
    End Sub
End Class
                   ]]></file>
           </compilation>

            Dim comp = CreateWinRtCompilation(source).AddReferences(AssemblyMetadata.CreateFromImage(TestResources.WinRt.W1).GetReference())

            CompileAndVerify(comp, symbolValidator:=Sub(m)
                                                        Dim [module] = DirectCast(m, PEModuleSymbol)
                                                        Dim c = DirectCast([module].GlobalNamespace.GetTypeMember("C"), PENamedTypeSymbol)
                                                        Dim attributeHandle = [module].Module.MetadataReader.GetCustomAttributes(c.Handle).Single()
                                                        Dim value As String = Nothing
                                                        [module].Module.TryExtractStringValueFromAttribute(attributeHandle, value)

                                                        Assert.Equal("C1, W, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime", value)
                                                    End Sub)
        End Sub
    End Class
End Namespace
