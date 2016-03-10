' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "OverrideKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind2() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

#End Region

#Region "ReadWrite tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite2() As Task
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
        Get
        End Get
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite3() As Task
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite4() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite5() As Task
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite6() As Task
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
End Class
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True
    End Class
End Namespace