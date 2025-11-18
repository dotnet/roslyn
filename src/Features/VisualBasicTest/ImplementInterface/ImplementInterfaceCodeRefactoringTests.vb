' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVb = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.ImplementInterface.ImplementInterfaceCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ImplementInterface
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
    Public NotInheritable Class ImplementInterfaceCodeRefactoringTests

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78294")>
        Public Function TestInBody() As Task
            Return VerifyVb.VerifyRefactoringAsync("
interface IGoo
    sub Goo()
end interface

class C
    implements {|BC30149:IGoo|}

    $$
end class
                ", "
interface IGoo
    sub Goo()
end interface

class C
    implements IGoo

    Public Sub Goo() Implements IGoo.Goo
        Throw New System.NotImplementedException()
    End Sub
end class
                ")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78294")>
        Public Function TestNotOnInterfaceInBody() As Task
            Return VerifyVb.VerifyRefactoringAsync("
interface IGoo
    sub Goo()
end interface

interface IBar
    inherits IGoo

    $$
end interface
                ")
        End Function
    End Class
End Namespace
