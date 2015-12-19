' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class RootCodeModelTests
        Inherits AbstractRootCodeModelTests

#Region "CodeElements tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeElements1() As Task
            Dim code =
<code>
class Foo { }
</code>

            Await TestChildren(code,
                             "Foo",
                             "System",
                             "Microsoft")
        End Function

#End Region

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDotNetNameFromLanguageSpecific1() As Task
            Dim code =
<code>
using N.M;

namespace N
{
    namespace M
    {
        class Generic&lt;T&gt; { }
    }
}
</code>

            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("N.M.Generic<string>")
                    Assert.Equal("N.M.Generic`1[System.String]", dotNetName)
                End Sub)
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDotNetNameFromLanguageSpecific2() As Task
            Await TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary<int, string>")
                    Assert.Equal("System.Collections.Generic.Dictionary`2[System.Int32,System.String]", dotNetName)
                End Sub)
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDotNetNameFromLanguageSpecificWithAssemblyQualifiedName() As Task
            Await TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Assert.Throws(Of ArgumentException)(Sub() rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary<int, string>, mscorlib"))
                End Sub)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestExternalNamespaceChildren() As Task
            Dim code =
<code>
class Foo { }
</code>

            Await TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim systemNamespace = rootCodeModel.CodeElements.Find(Of EnvDTE.CodeNamespace)("System")
                    Assert.NotNull(systemNamespace)

                    Dim collectionsNamespace = systemNamespace.Members.Find(Of EnvDTE.CodeNamespace)("Collections")
                    Assert.NotNull(collectionsNamespace)

                    Dim genericNamespace = collectionsNamespace.Members.Find(Of EnvDTE.CodeNamespace)("Generic")
                    Assert.NotNull(genericNamespace)

                    Dim listClass = genericNamespace.Members.Find(Of EnvDTE.CodeClass)("List")
                    Assert.NotNull(listClass)
                End Sub)
        End Function

#Region "CreateCodeTypeRef"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCreateCodeTypeRef_Int32() As Task
            Await TestCreateCodeTypeRef("System.Int32",
                                  New CodeTypeRefData With {
                                      .AsString = "int",
                                      .AsFullName = "System.Int32",
                                      .CodeTypeFullName = "System.Int32",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                                  })
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCreateCodeTypeRef_System_Text_StringBuilder() As Task
            Await TestCreateCodeTypeRef("System.Text.StringBuilder",
                                  New CodeTypeRefData With {
                                      .AsString = "System.Text.StringBuilder",
                                      .AsFullName = "System.Text.StringBuilder",
                                      .CodeTypeFullName = "System.Text.StringBuilder",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCreateCodeTypeRef_NullableInteger() As Task
            Await TestCreateCodeTypeRef("int?",
                                  New CodeTypeRefData With {
                                      .AsString = "int?",
                                      .AsFullName = "System.Nullable<System.Int32>",
                                      .CodeTypeFullName = "System.Int32?",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCreateCodeTypeRef_ListOfInt() As Task
            Await TestCreateCodeTypeRef("System.Collections.Generic.List<int>",
                                  New CodeTypeRefData With {
                                      .AsString = "System.Collections.Generic.List<int>",
                                      .AsFullName = "System.Collections.Generic.List<System.Int32>",
                                      .CodeTypeFullName = "System.Collections.Generic.List<System.Int32>",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Function

#End Region

#Region "CodeTypeFromFullName"

        <WorkItem(1107453)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeTypeFromFullName_NonGenerated() As Task

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.cs"><![CDATA[
namespace N
{
    class C
    {
    }
}
]]></Document>
                                </Project>
                            </Workspace>

            Await TestCodeTypeFromFullName(workspace, "N.C",
                Sub(codeType)
                    Assert.NotNull(codeType)
                    Assert.Equal("N.C", codeType.FullName)

                    Dim codeNamespace = TryCast(codeType.Parent, EnvDTE.CodeNamespace)
                    Assert.NotNull(codeNamespace)

                    Dim fileCodeModel = TryCast(codeNamespace.Parent, EnvDTE.FileCodeModel)
                    Assert.NotNull(fileCodeModel)

                    Dim underlyingFileCodeModel = ComAggregate.GetManagedObject(Of FileCodeModel)(fileCodeModel)
                    Assert.NotNull(underlyingFileCodeModel)

                    Dim filePath = underlyingFileCodeModel.Workspace.GetFilePath(underlyingFileCodeModel.GetDocumentId())
                    Assert.Equal("C.cs", filePath)
                End Sub)

        End Function


        <WorkItem(1107453)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeTypeFromFullName_Generated() As Task

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.g.cs"><![CDATA[
namespace N
{
    class C
    {
    }
}
]]></Document>
                                </Project>
                            </Workspace>

            Await TestCodeTypeFromFullName(workspace, "N.C",
                Sub(codeType)
                    Assert.NotNull(codeType)
                    Assert.Equal("N.C", codeType.FullName)

                    Dim codeNamespace = TryCast(codeType.Parent, EnvDTE.CodeNamespace)
                    Assert.NotNull(codeNamespace)

                    Dim fileCodeModel = TryCast(codeNamespace.Parent, EnvDTE.FileCodeModel)
                    Assert.NotNull(fileCodeModel)

                    Dim underlyingFileCodeModel = ComAggregate.GetManagedObject(Of FileCodeModel)(fileCodeModel)
                    Assert.NotNull(underlyingFileCodeModel)

                    Dim filePath = underlyingFileCodeModel.Workspace.GetFilePath(underlyingFileCodeModel.GetDocumentId())
                    Assert.Equal("C.g.cs", filePath)
                End Sub)

        End Function

        <WorkItem(1107453)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeTypeFromFullName_NonGenerated_Generated() As Task

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.cs"><![CDATA[
namespace N
{
    partial class C
    {
    }
}
]]></Document>
                                    <Document FilePath="C.g.cs"><![CDATA[
namespace N
{
    partial class C
    {
    }
}
]]></Document>
                                </Project>
                            </Workspace>

            Await TestCodeTypeFromFullName(workspace, "N.C",
                Sub(codeType)
                    Assert.NotNull(codeType)
                    Assert.Equal("N.C", codeType.FullName)

                    Dim codeNamespace = TryCast(codeType.Parent, EnvDTE.CodeNamespace)
                    Assert.NotNull(codeNamespace)

                    Dim fileCodeModel = TryCast(codeNamespace.Parent, EnvDTE.FileCodeModel)
                    Assert.NotNull(fileCodeModel)

                    Dim underlyingFileCodeModel = ComAggregate.GetManagedObject(Of FileCodeModel)(fileCodeModel)
                    Assert.NotNull(underlyingFileCodeModel)

                    Dim filePath = underlyingFileCodeModel.Workspace.GetFilePath(underlyingFileCodeModel.GetDocumentId())
                    Assert.Equal("C.cs", filePath)
                End Sub)

        End Function

        <WorkItem(1107453)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestCodeTypeFromFullName_Generated_NonGenerated() As Task

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.g.cs"><![CDATA[
namespace N
{
    partial class C
    {
    }
}
]]></Document>
                                    <Document FilePath="C.cs"><![CDATA[
namespace N
{
    partial class C
    {
    }
}
]]></Document>
                                </Project>
                            </Workspace>

            Await TestCodeTypeFromFullName(workspace, "N.C",
                Sub(codeType)
                    Assert.NotNull(codeType)
                    Assert.Equal("N.C", codeType.FullName)

                    Dim codeNamespace = TryCast(codeType.Parent, EnvDTE.CodeNamespace)
                    Assert.NotNull(codeNamespace)

                    Dim fileCodeModel = TryCast(codeNamespace.Parent, EnvDTE.FileCodeModel)
                    Assert.NotNull(fileCodeModel)

                    Dim underlyingFileCodeModel = ComAggregate.GetManagedObject(Of FileCodeModel)(fileCodeModel)
                    Assert.NotNull(underlyingFileCodeModel)

                    Dim filePath = underlyingFileCodeModel.Workspace.GetFilePath(underlyingFileCodeModel.GetDocumentId())
                    Assert.Equal("C.cs", filePath)
                End Sub)

        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
