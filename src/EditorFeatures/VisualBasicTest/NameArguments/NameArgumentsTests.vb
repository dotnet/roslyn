' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.NameArguments

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseExplicitTupleName
    Public Class NameArgumentsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicNameArgumentsDiagnosticAnalyzer(),
                    New VisualBasicNameArgumentsCodeFixProvider())
        End Function

        Private Shared ReadOnly s_parseOptions As VisualBasicParseOptions =
            VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestNamedTuple1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.i
    end sub
end class")
        End Function

    End Class
End Namespace
