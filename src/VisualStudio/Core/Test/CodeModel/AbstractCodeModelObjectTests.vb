' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeModelObjectTests(Of TCodeModelObject As Class)

        Protected MustOverride ReadOnly Property LanguageName As String

        Protected Function GetWorkspaceDefinition(code As XElement) As XElement
            Return <Workspace>
                       <Project Language=<%= LanguageName %> CommonReferences="true">
                           <Document><%= code.Value.Trim() %></Document>
                       </Project>
                   </Workspace>
        End Function

        Protected Overridable Function TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddClass(code As XElement, expectedCode As XElement, data As ClassData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddDelegate(code As XElement, expectedCode As XElement, data As DelegateData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEnum(code As XElement, expectedCode As XElement, data As EnumData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEnumMember(code As XElement, expectedCode As XElement, data As EnumMemberData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddEvent(code As XElement, expectedCode As XElement, data As EventData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddImport(code As XElement, expectedCode As XElement, data As ImportData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddInterface(code As XElement, expectedCode As XElement, data As InterfaceData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddNamespace(code As XElement, expectedCode As XElement, data As NamespaceData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddParameter(code As XElement, expectedCode As XElement, data As ParameterData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddProperty(code As XElement, expectedCode As XElement, data As PropertyData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddStruct(code As XElement, expectedCode As XElement, data As StructData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData) As Threading.Tasks.Task
            Throw New NotImplementedException
        End Function

        Protected Overridable Function TestRemoveChild(code As XElement, expectedCode As XElement, child As Object) As Threading.Tasks.Task
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
