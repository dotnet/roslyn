' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Xunit
Imports SyntaxUtilities = Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.SyntaxUtilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Public Class SyntaxUtilitiesTests

        Private Sub VerifySyntaxMap(oldSource As String, newSource As String)

            Dim oldRoot = SyntaxFactory.ParseSyntaxTree(oldSource).GetRoot()
            Dim newRoot = SyntaxFactory.ParseSyntaxTree(newSource).GetRoot()

            For Each oldNode In oldRoot.DescendantNodes().Where(Function(n) n.FullSpan.Length > 0)
                Dim newNode = SyntaxUtilities.FindPartner(oldRoot, newRoot, oldNode)
                Assert.True(SyntaxFactory.AreEquivalent(oldNode, newNode), $"Node 'oldNodeEnd' not equivalent to 'newNodeEnd'.")
            Next
        End Sub

        <WpfFact>
        Public Sub FindPartner1()
            Dim source1 = "
Imports System
Class C
    Shared Sub Main(args As String())
    

        ' sdasd
        Dim b = true
        Do
            Console.WriteLine(""hi"")
        While b = True
    End Sub
End Class
"

            Dim source2 = "
Imports System
Class C
    Shared Sub Main(args As String())
        Dim b = true
        Do
            Console.WriteLine(""hi"")
        While b = True
    End Sub
End Class
"
            VerifySyntaxMap(source1, source2)
        End Sub
    End Class
End Namespace
