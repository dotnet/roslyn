' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Expansion
    Public Class ModuleNameExpansionTests
        Inherits AbstractExpansionTest

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestExpandModuleNameForSimpleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|Expand:Foo|}()
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
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
        Call Global.N.X.Foo()
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestExpandModuleNameForQualifiedNameWithMissingModuleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        Dim bar As {|Expand:N.Foo|}
    End Sub
End Module

Namespace N
    Module X
        Class Foo
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
        Dim bar As Global.N.X.Foo
    End Sub
End Module

Namespace N
    Module X
        Class Foo
        End Class
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestExpandModuleNameForMemberAccessWithMissingModuleName() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|Expand:N.Foo()|}
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
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
        Global.N.X.Foo()
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestExpandModuleNameForSimpleNameRoundtrip() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        {|ExpandAndSimplify:Foo|}()
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
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
        Foo()
    End Sub
End Module

Namespace N
    Module X
        Sub Foo()
        End Sub
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Expansion)>
        Public Async Function TestExpandModuleNameForQualifiedNameWithMissingModuleNameRoundtrip() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports N

Module Program
    Sub Main()
        Dim bar As {|ExpandAndSimplify:N.Foo|}
    End Sub
End Module

Namespace N
    Module X
        Class Foo
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
        Dim bar As N.Foo
    End Sub
End Module

Namespace N
    Module X
        Class Foo
        End Class
    End Module
End Namespace
</code>

            Await TestAsync(input, expected)
        End Function

    End Class
End Namespace
