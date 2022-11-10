' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    <Trait(Traits.Feature, Traits.Features.Expansion)>
    Public Class ModuleNameExpansionTests
        Inherits AbstractExpansionTest

        <Fact>
        Public Async Function TestExpandModuleNameForSimpleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|Expand:Goo|}()
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports N

Module Program
    Sub Main()
        Call Global.N.X.Goo()
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestExpandModuleNameForQualifiedNameWithMissingModuleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        Dim bar As {|Expand:N.Goo|}
    End Sub
End Module

Namespace N
    Module X
        Class Goo
        End Class
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports N

Module Program
    Sub Main()
        Dim bar As Global.N.X.Goo
    End Sub
End Module

Namespace N
    Module X
        Class Goo
        End Class
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestExpandModuleNameForMemberAccessWithMissingModuleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|Expand:N.Goo()|}
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports N

Module Program
    Sub Main()
        Global.N.X.Goo()
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestExpandAndOmitModuleNameWhenConflicting() As Task
            Dim input =
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="Project1" CommonReferences="true">
                        <Document>
                            Namespace X
                                Public Module Y
                                    Public Class C
                                    End Class
                                End Module
                            End Namespace
                         </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="Project2" CommonReferences="true">
                        <ProjectReference>Project1</ProjectReference>
                        <Document>
                            Namespace X
                                Namespace Y
                                    Class D
                                        Inherits {|Expand:C|}
                                    End Class
                                End Namespace
                            End Namespace
                         </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <code>
Namespace X
    Namespace Y
        Class D
            Inherits Global.X.C
        End Class
    End Namespace
End Namespace
                </code>

            Await TestAsync(input, expected, useLastProject:=True)
        End Function

        <Fact>
        Public Async Function TestExpandModuleNameForSimpleNameRoundtrip() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|ExpandAndSimplify:Goo|}()
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports N

Module Program
    Sub Main()
        Goo()
    End Sub
End Module

Namespace N
    Module X
        Sub Goo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestExpandModuleNameForQualifiedNameWithMissingModuleNameRoundtrip() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        Dim bar As {|ExpandAndSimplify:N.Goo|}
    End Sub
End Module

Namespace N
    Module X
        Class Goo
        End Class
    End Module
End Namespace
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports N

Module Program
    Sub Main()
        Dim bar As N.Goo
    End Sub
End Module

Namespace N
    Module X
        Class Goo
        End Class
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

    End Class
End Namespace
