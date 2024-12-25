' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "OverrideKind tests"

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/9646")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
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
