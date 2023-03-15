' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class TypeAccessibility : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim assembly = MetadataTestHelpers.LoadFromBytes(TestMetadata.ResourcesNet40.mscorlib)

            TestTypeAccessibilityHelper(assembly.Modules(0))
        End Sub

        Private Sub TestTypeAccessibilityHelper(module0 As ModuleSymbol)

            Dim system = (From n In module0.GlobalNamespace.GetMembers()
                          Where n.Kind = SymbolKind.Namespace AndAlso n.Name.Equals("System")).
                        Cast(Of NamespaceSymbol).Single()

            Dim obj = (From t In system.GetTypeMembers()
                       Where t.Name.Equals("Object")).Single()

            Assert.Equal(Accessibility.Public, obj.DeclaredAccessibility)

            Dim fxAssembly = (From t In module0.GlobalNamespace.GetTypeMembers()
                              Where t.Name.Equals("FXAssembly")).Single()

            Assert.Equal(Accessibility.Friend, fxAssembly.DeclaredAccessibility)

            Dim [enum] = (From t In system.GetTypeMembers()
                          Where t.Name.Equals("Enum")).Single()

            Dim console = (From t In system.GetTypeMembers()
                           Where t.Name.Equals("Console")).Single()

            Dim ControlKeyState = (From t In console.GetTypeMembers()
                                   Where t.Name.Equals("ControlKeyState")).Single()

            Assert.Equal(Accessibility.Friend, ControlKeyState.DeclaredAccessibility)

            Dim ActivationContext = (From t In system.GetTypeMembers()
                                     Where t.Name.Equals("ActivationContext")).Single()

            Dim ContextForm = (From t In ActivationContext.GetTypeMembers()
                               Where t.Name.Equals("ContextForm")).Single()

            Assert.Equal(Accessibility.Public, ContextForm.DeclaredAccessibility)

            Dim Runtime = (From t In system.GetMembers()
                           Where t.Kind = SymbolKind.Namespace AndAlso t.Name.Equals("Runtime")).
                          Cast(Of NamespaceSymbol)().Single()

            Dim Remoting = (From t In Runtime.GetMembers()
                            Where t.Kind = SymbolKind.Namespace AndAlso t.Name.Equals("Remoting")).
                          Cast(Of NamespaceSymbol)().Single()

            Dim Messaging = (From t In Remoting.GetMembers()
                             Where t.Kind = SymbolKind.Namespace AndAlso t.Name.Equals("Messaging")).
                          Cast(Of NamespaceSymbol)().Single()

            Dim MessageSmuggler = (From t In Messaging.GetTypeMembers()
                                   Where t.Name.Equals("MessageSmuggler")).Single()

            Dim SerializedArg = (From t In MessageSmuggler.GetTypeMembers()
                                 Where t.Name.Equals("SerializedArg")).Single()

            Assert.Equal(Accessibility.Protected, SerializedArg.DeclaredAccessibility)

            Dim Security = (From t In system.GetMembers()
                            Where t.Kind = SymbolKind.Namespace AndAlso t.Name.Equals("Security")).
                        Cast(Of NamespaceSymbol)().Single()

            Dim AccessControl = (From t In Security.GetMembers()
                                 Where t.Kind = SymbolKind.Namespace AndAlso t.Name.Equals("AccessControl")).
                          Cast(Of NamespaceSymbol)().Single()

            Dim NativeObjectSecurity = (From t In AccessControl.GetTypeMembers()
                                        Where t.Name.Equals("NativeObjectSecurity")).Single()

            Dim ExceptionFromErrorCode = (From t In NativeObjectSecurity.GetTypeMembers()
                                          Where t.Name.Equals("ExceptionFromErrorCode")).Single()

            Assert.Equal(Accessibility.ProtectedOrFriend, ExceptionFromErrorCode.DeclaredAccessibility)

            Assert.Same(module0, module0.GlobalNamespace.Locations.Single().MetadataModule)
            Assert.Same(module0, system.Locations.Single().MetadataModule)
            Assert.Same(module0, Runtime.Locations.Single().MetadataModule)
            Assert.Same(module0, obj.Locations.Single().MetadataModule)

        End Sub

    End Class

End Namespace
