' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "OverrideKind tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_None() As Task
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

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
MustInherit Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
Class C
    Public Overridable Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override() As Task
            Dim code =
<Code>
MustInherit Class A
    Public MustOverride Property P As Integer
End Class

Class C
    Inherits A

    Public Overrides Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Sealed() As Task
            Dim code =
<Code>
MustInherit Class A
    Public MustOverride Property P As Integer
End Class

Class C
    Inherits A

    Public NotOverridable Overrides Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterName() As Task
            Dim code =
<Code>
Class C
    Property $$P(x As Integer, y as String) As Integer
        Get
            Return x * y
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestAllParameterNames(code, "x", "y")
        End Function

#End Region

#Region "ReadWrite tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_GetSet() As Task
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

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Get() As Task
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

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Set() As Task
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

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True
    End Class
End Namespace