﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    <[UseExportProvider]>
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected MustOverride ReadOnly Property LanguageName As String

        Protected Function GetWorkspaceDefinition(code As XElement, Optional editorConfig As String = "") As XElement
            Return <Workspace>
                       <Project Language=<%= LanguageName %> CommonReferences="true">
                           <Document><%= code.Value.Trim() %></Document>
                           <AnalyzerConfigDocument FilePath="z:\\.editorconfig">
                               <%= editorConfig %>
                           </AnalyzerConfigDocument>
                       </Project>
                   </Workspace>
        End Function

        Protected Overridable Function TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddClass(code As XElement, expectedCode As XElement, data As ClassData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddDelegate(code As XElement, expectedCode As XElement, data As DelegateData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEnum(code As XElement, expectedCode As XElement, data As EnumData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEnumMember(code As XElement, expectedCode As XElement, data As EnumMemberData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEvent(code As XElement, expectedCode As XElement, data As EventData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddImport(code As XElement, expectedCode As XElement, data As ImportData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddInterface(code As XElement, expectedCode As XElement, data As InterfaceData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddNamespace(code As XElement, expectedCode As XElement, data As NamespaceData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddParameter(code As XElement, expectedCode As XElement, data As ParameterData) As Task
            Throw New NotImplementedException
        End Function

        Private Protected Overridable Function TestAddProperty(
                code As XElement, expectedCode As XElement, data As PropertyData,
                Optional options As IDictionary(Of OptionKey2, Object) = Nothing,
                Optional editorConfig As String = "") As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddStruct(code As XElement, expectedCode As XElement, data As StructData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData) As Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestRemoveChild(code As XElement, expectedCode As XElement, child As Object) As Task
            Throw New NotImplementedException
        End Function

        Protected Sub TestCodeTypeRef(codeTypeRef As EnvDTE.CodeTypeRef, data As CodeTypeRefData)
            Assert.NotNull(codeTypeRef)

            If data.CodeTypeFullName IsNot Nothing Then
                Assert.True(codeTypeRef.CodeType IsNot Nothing, "Test specified CodeTypeFullName but CodeType was null.")

                Assert.Equal(data.CodeTypeFullName, codeTypeRef.CodeType.FullName)
            Else
                Assert.True(codeTypeRef.CodeType Is Nothing, "Test didn't specify CodeTypeFullName but CodeType was not null.")
            End If

            If data.AsFullName IsNot Nothing Then
                Assert.Equal(data.AsFullName, codeTypeRef.AsFullName)
            End If

            If data.AsString IsNot Nothing Then
                Assert.Equal(data.AsString, codeTypeRef.AsString)
            End If

            Assert.Equal(data.TypeKind, codeTypeRef.TypeKind)
        End Sub

        Protected Function IsFileCodeModel() As Action(Of Object)
            Return Sub(o)
                       Dim fcm = TryCast(o, EnvDTE.FileCodeModel)
                       Assert.NotNull(fcm)
                   End Sub
        End Function

        Protected MustOverride Sub TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object))

        Protected Function IsElement(name As String, Optional kind? As EnvDTE.vsCMElement = Nothing) As Action(Of Object)
            Return _
                Sub(o)
                    Dim e = TryCast(o, EnvDTE.CodeElement)
                    Assert.NotNull(e)
                    Assert.Equal(name, e.Name)

                    If kind IsNot Nothing Then
                        Assert.Equal(kind.Value, e.Kind)
                    End If
                End Sub
        End Function

        Protected ReadOnly NoElements As Action(Of Object)() = Array.Empty(Of Action(Of Object))()

    End Class
End Namespace
