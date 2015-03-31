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

        Protected Overridable Sub TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddClass(code As XElement, expectedCode As XElement, data As ClassData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddDelegate(code As XElement, expectedCode As XElement, data As DelegateData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddEnum(code As XElement, expectedCode As XElement, data As EnumData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddEnumMember(code As XElement, expectedCode As XElement, data As EnumMemberData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddEvent(code As XElement, expectedCode As XElement, data As EventData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddImport(code As XElement, expectedCode As XElement, data As ImportData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddInterface(code As XElement, expectedCode As XElement, data As InterfaceData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddNamespace(code As XElement, expectedCode As XElement, data As NamespaceData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddParameter(code As XElement, expectedCode As XElement, data As ParameterData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddProperty(code As XElement, expectedCode As XElement, data As PropertyData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddStruct(code As XElement, expectedCode As XElement, data As StructData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData)
            Throw New NotImplementedException
        End Sub

        Protected Overridable Sub TestRemoveChild(code As XElement, expectedCode As XElement, child As Object)
            Throw New NotImplementedException
        End Sub

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
