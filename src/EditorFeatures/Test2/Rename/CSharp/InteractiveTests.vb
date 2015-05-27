' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class InteractiveTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenamingTopLevelMethodsSupported()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">
void [|$$Foo|]()
{
}
                    </Submission>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub
    End Class
End Namespace
