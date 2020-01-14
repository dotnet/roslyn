' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.AddFileBanner
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddFileBanner
    Partial Public Class AddFileBannerTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicAddFileBannerCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestBanner1() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' This is the banner

class Program2
end class
        </Document>
    </Project>
</Workspace>",
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>' This is the banner

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' This is the banner

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestMultiLineBanner1() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' This is the banner
' It goes over multiple lines

class Program2
end class
        </Document>
    </Project>
</Workspace>",
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>' This is the banner
' It goes over multiple lines

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' This is the banner
' It goes over multiple lines

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        <WorkItem(33251, "https://github.com/dotnet/roslyn/issues/33251")>
        Public Async Function TestSingleLineDocCommentBanner() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>''' This is the banner
''' It goes over multiple lines

class Program2
end class
        </Document>
    </Project>
</Workspace>",
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>''' This is the banner
''' It goes over multiple lines

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>''' This is the banner
''' It goes over multiple lines

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <WorkItem(32792, "https://github.com/dotnet/roslyn/issues/32792")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestUpdateFileNameInComment() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Goo.vb"">[||]Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document FilePath=""Bar.vb"">' This is the banner in Bar.vb
' It goes over multiple lines.  This line has Baz.vb
' The last line includes Bar.vb

class Program2
end class
        </Document>
    </Project>
</Workspace>",
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Goo.vb"">' This is the banner in Goo.vb
' It goes over multiple lines.  This line has Baz.vb
' The last line includes Goo.vb

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document FilePath=""Bar.vb"">' This is the banner in Bar.vb
' It goes over multiple lines.  This line has Baz.vb
' The last line includes Bar.vb

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        <WorkItem(33251, "https://github.com/dotnet/roslyn/issues/33251")>
        Public Async Function TestUpdateFileNameInComment2() As Task
            Await TestInRegularAndScriptAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Goo.vb"">[||]Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document FilePath=""Bar.vb"">''' This is the banner in Bar.vb
''' It goes over multiple lines.  This line has Baz.vb
''' The last line includes Bar.vb

class Program2
end class
        </Document>
    </Project>
</Workspace>",
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Goo.vb"">''' This is the banner in Goo.vb
''' It goes over multiple lines.  This line has Baz.vb
''' The last line includes Goo.vb

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document FilePath=""Bar.vb"">''' This is the banner in Bar.vb
''' It goes over multiple lines.  This line has Baz.vb
''' The last line includes Bar.vb

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestMissingWhenAlreadyThere() As Task
            Await TestMissingAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]' I already have a banner

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' This is the banner

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestMissingIfOtherFileDoesNotHaveBanner() As Task
            Await TestMissingAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)>
        Public Async Function TestMissingIfOtherFileIsAutoGenerated() As Task
            Await TestMissingAsync(
"
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]

Imports System

class Program1
    sub Main()
    end sub
end class
        </Document>
        <Document>' &lt;autogenerated /&gt;

class Program2
end class
        </Document>
    </Project>
</Workspace>")
        End Function
    End Class
End Namespace
