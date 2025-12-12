' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities.BracePairs

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.BracePairs
    Public Class VisualBasicBracePairsTests
        Inherits AbstractBracePairsTests

        Protected Overrides Function CreateWorkspace(input As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(input)
        End Function

        <Fact>
        Public Async Function Test1() As Task
            Await Test("
{|a:<|}Attr{|a:>|}
class C
    sub Goo{|b:(|}i as List{|c:(|}of Bar{|c:)|}{|b:)|}
        dim x = new Goo{|d:(|}{|d:)|} From {|e:{|} 1, 2, 3 {|e:}|}
    end sub
end class
                ")
        End Function
    End Class
End Namespace
