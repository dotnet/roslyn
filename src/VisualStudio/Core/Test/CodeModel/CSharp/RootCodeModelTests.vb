' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class RootCodeModelTests
        Inherits AbstractRootCodeModelTests

#Region "CodeElements tests"

        ' This test depends On the version Of mscorlib used by the TestWorkspace And may 
        ' change in the future
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeElements1()
            Dim code =
<code>
class Goo { }
</code>

            TestChildren(code,
                             "Goo",
                             "System",
                             "Microsoft",
                             "Windows")
        End Sub

#End Region

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecific1()
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

            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("N.M.Generic<string>")
                    Assert.Equal("N.M.Generic`1[System.String]", dotNetName)
                End Sub)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecific2()
            TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary<int, string>")
                    Assert.Equal("System.Collections.Generic.Dictionary`2[System.Int32,System.String]", dotNetName)
                End Sub)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecificWithAssemblyQualifiedName()
            TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Assert.Throws(Of ArgumentException)(Sub() rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary<int, string>, mscorlib"))
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExternalNamespaceChildren()
            Dim code =
<code>
class Goo { }
</code>

            TestRootCodeModelWithCodeFile(code,
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
        End Sub

#Region "CreateCodeTypeRef"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCreateCodeTypeRef_Int32()
            TestCreateCodeTypeRef("System.Int32",
                                  New CodeTypeRefData With {
                                      .AsString = "int",
                                      .AsFullName = "System.Int32",
                                      .CodeTypeFullName = "System.Int32",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                                  })
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCreateCodeTypeRef_System_Text_StringBuilder()
            TestCreateCodeTypeRef("System.Text.StringBuilder",
                                  New CodeTypeRefData With {
                                      .AsString = "System.Text.StringBuilder",
                                      .AsFullName = "System.Text.StringBuilder",
                                      .CodeTypeFullName = "System.Text.StringBuilder",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCreateCodeTypeRef_NullableInteger()
            TestCreateCodeTypeRef("int?",
                                  New CodeTypeRefData With {
                                      .AsString = "int?",
                                      .AsFullName = "System.Nullable<System.Int32>",
                                      .CodeTypeFullName = "System.Int32?",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCreateCodeTypeRef_ListOfInt()
            TestCreateCodeTypeRef("System.Collections.Generic.List<int>",
                                  New CodeTypeRefData With {
                                      .AsString = "System.Collections.Generic.List<int>",
                                      .AsFullName = "System.Collections.Generic.List<System.Int32>",
                                      .CodeTypeFullName = "System.Collections.Generic.List<System.Int32>",
                                      .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                                  })
        End Sub

#End Region

#Region "CodeTypeFromFullName"

        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_NonGenerated()

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

            TestCodeTypeFromFullName(workspace, "N.C",
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

        End Sub


        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_Generated()

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

            TestCodeTypeFromFullName(workspace, "N.C",
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

        End Sub

        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_NonGenerated_Generated()

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

            TestCodeTypeFromFullName(workspace, "N.C",
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

        End Sub

        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_Generated_NonGenerated()

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

            TestCodeTypeFromFullName(workspace, "N.C",
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

        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
