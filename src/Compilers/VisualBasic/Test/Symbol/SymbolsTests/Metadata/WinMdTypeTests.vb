' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    End Class
End Namespace
