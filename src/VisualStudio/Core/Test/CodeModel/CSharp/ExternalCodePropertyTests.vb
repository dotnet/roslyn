﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "OverrideKind tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_None()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Sealed()
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

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
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

            TestAllParameterNames(code, "x", "y")
        End Sub

#End Region

#Region "ReadWrite tests"

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_GetSet()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <WorkItem(9646, "https://github.com/dotnet/roslyn/issues/9646")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Set()
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

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.CSharp
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace
