﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class RootCodeModelTests
        Inherits AbstractRootCodeModelTests

#Region "CodeElements tests"

        ' This test depends On the version Of mscorlib used by the TestWorkspace And may 
        ' change in the future
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeElements1()
            Dim code =
<code>
Class Foo
End Class
</code>

            TestChildren(code, "MS", "My", "Microsoft", "System", "Foo", "Windows")
        End Sub

        ' This test depends On the version Of mscorlib used by the TestWorkspace And may 
        ' change in the future
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeElements2()
            Dim code =
<code>
Module Foo
End Module
</code>

            TestChildren(code, "MS", "My", "Microsoft", "System", "Foo", "Windows")
        End Sub

#End Region

#Region "CreateCodeTypeRef"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCreateCodeTypeRef_Int32()
            TestCreateCodeTypeRef("System.Int32",
                                  New CodeTypeRefData With {
                                      .AsString = "Integer",
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
            TestCreateCodeTypeRef("Integer?",
                                  New CodeTypeRefData With {
                                      .AsString = "Integer?",
                                      .AsFullName = "System.Nullable(Of System.Int32)",
                                      .CodeTypeFullName = "System.Nullable(Of System.Int32)",
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
                                    <Document FilePath="C.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
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
                    Assert.Equal("C.vb", filePath)
                End Sub)

        End Sub


        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_Generated()

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.g.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
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
                    Assert.Equal("C.g.vb", filePath)
                End Sub)

        End Sub

        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_NonGenerated_Generated()

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
]]></Document>
                                    <Document FilePath="C.g.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
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
                    Assert.Equal("C.vb", filePath)
                End Sub)

        End Sub

        <WorkItem(1107453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107453")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestCodeTypeFromFullName_Generated_NonGenerated()

            Dim workspace = <Workspace>
                                <Project Language=<%= LanguageName %> CommonReferences="true">
                                    <Document FilePath="C.g.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
]]></Document>
                                    <Document FilePath="C.vb"><![CDATA[
Namespace N
    Class C
    End Class
End Namespace
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
                    Assert.Equal("C.vb", filePath)
                End Sub)

        End Sub

#End Region

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecific1()
            Dim code =
<code>
Imports N.M

Namespace N
    Namespace M
        Class Generic(Of T)
        End Class
    End Namespace
End Namespace
</code>

            TestRootCodeModelWithCodeFile(code,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("N.M.Generic(Of String)")
                    Assert.Equal("N.M.Generic`1[System.String]", dotNetName)
                End Sub)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecific2()
            TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Dim dotNetName = rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary(Of Integer, String)")
                    Assert.Equal("System.Collections.Generic.Dictionary`2[System.Int32,System.String]", dotNetName)
                End Sub)
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDotNetNameFromLanguageSpecificWithAssemblyQualifiedName()
            TestRootCodeModelWithCodeFile(<code></code>,
                Sub(rootCodeModel)
                    Assert.Throws(Of ArgumentException)(Sub() rootCodeModel.DotNetNameFromLanguageSpecific("System.Collections.Generic.Dictionary(Of Integer, String), mscorlib"))
                End Sub)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
