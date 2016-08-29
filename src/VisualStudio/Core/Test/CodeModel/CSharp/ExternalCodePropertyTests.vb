' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "OverrideKind tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_None() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Abstract() As Task
            Dim code =
<Code>
abstract class C
{
    public abstract int $$P
    {
        get;
        set;
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Virtual() As Task
            Dim code =
<Code>
class C
{
    public virtual int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Override() As Task
            Dim code =
<Code>
abstract class B
{
    public abstract int P { get; set; }
}

class C : B
{
    public override int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestOverrideKind_Sealed() As Task
            Dim code =
<Code>
abstract class B
{
    public abstract int P { get; set; }
}

class C : B
{
    public override sealed int $$P
    {
        get { return default(int); }
        set { }
    }
}
</Code>

            Await TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Function

#End Region

#Region "Parameter name tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestParameterNameWithEscapeCharacters() As Task
            Dim code =
<Code>
class Program
{
    public int $$this[int x, int y]
    {
        get { return x * y; }
        set { }
    }
}
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
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
        set
        {
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Get() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        get
        {
            return default(int);
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Function

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestReadWrite_Set() As Task
            Dim code =
<Code>
class C
{
    public int $$P
    {
        set
        {
        }
    }
}
</Code>

            Await TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.CSharp
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace