' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeElementTests(Of TCodeElement As Class)
        Inherits AbstractCodeModelObjectTests(Of TCodeElement)

        Protected Delegate Sub PartAction(part As EnvDTE.vsCMPart, textPointGetter As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint))

        Protected Function Part(p As EnvDTE.vsCMPart, action As PartAction) As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint))
            Return _
                Sub(textPointGetter)
                    action(p, textPointGetter)
                End Sub
        End Function

        Protected Function ThrowsNotImplementedException() As PartAction
            Return Sub(part, textPointGetter)
                       Assert.Throws(Of NotImplementedException)(
                           Sub()
                               textPointGetter(part)
                           End Sub)
                   End Sub
        End Function

        Protected Const E_FAIL = &H80004005

        Protected Function ThrowsCOMException(errorCode As Integer) As PartAction
            Return _
                Sub(part, textPointGetter)
                    Dim exception = Assert.Throws(Of COMException)(
                        Sub()
                            textPointGetter(part)
                        End Sub)

                    Assert.Equal(errorCode, exception.ErrorCode)
                End Sub
        End Function

        Protected ReadOnly NullTextPoint As PartAction =
            Sub(part, textPointGetter)
                Dim tp As EnvDTE.TextPoint = Nothing
                tp = textPointGetter(part)
                Assert.Null(tp)
            End Sub

        Protected Function TextPoint(Optional line As Integer? = Nothing, Optional lineOffset As Integer? = Nothing, Optional absoluteOffset As Integer? = Nothing, Optional lineLength As Integer? = Nothing) As PartAction
            Return _
                Sub(part, textPointGetter)
                    Dim tp As EnvDTE.TextPoint = Nothing
                    tp = textPointGetter(part)

                    Assert.NotNull(tp)

                    If line IsNot Nothing Then
                        Assert.True(tp.Line = line.Value,
                                    vbCrLf &
                                    "TextPoint.Line was incorrect for " & part.ToString() & "." & vbCrLf &
                                    "Expected: " & line & vbCrLf &
                                    "But was:  " & tp.Line)
                    End If

                    If lineOffset IsNot Nothing Then
                        Assert.True(tp.LineCharOffset = lineOffset.Value,
                                    vbCrLf &
                                    "TextPoint.LineCharOffset was incorrect for " & part.ToString() & "." & vbCrLf &
                                    "Expected: " & lineOffset & vbCrLf &
                                    "But was:  " & tp.LineCharOffset)
                    End If

                    If absoluteOffset IsNot Nothing Then
                        Assert.True(tp.AbsoluteCharOffset = absoluteOffset.Value,
                                    vbCrLf &
                                    "TextPoint.AbsoluteCharOffset was incorrect for " & part.ToString() & "." & vbCrLf &
                                    "Expected: " & absoluteOffset & vbCrLf &
                                    "But was:  " & tp.AbsoluteCharOffset)
                    End If

                    If lineLength IsNot Nothing Then
                        Assert.True(tp.LineLength = lineLength.Value,
                                    vbCrLf &
                                    "TextPoint.LineLength was incorrect for " & part.ToString() & "." & vbCrLf &
                                    "Expected: " & lineLength & vbCrLf &
                                    "But was:  " & tp.LineLength)
                    End If
                End Sub
        End Function

        Protected Delegate Sub SetterAction(Of T)(newValue As T, valueSetter As Action(Of T))

        Protected Function NoThrow(Of T)() As SetterAction(Of T)
            Return _
                Sub(value, valueSetter)
                    valueSetter(value)
                End Sub
        End Function

        Protected Function ThrowsArgumentException(Of T)() As SetterAction(Of T)
            Return _
                Sub(value, valueSetter)
                    Assert.Throws(Of ArgumentException)(
                        Sub()
                            valueSetter(value)
                        End Sub)
                End Sub

        End Function

        Protected Function ThrowsCOMException(Of T)(errorCode As Integer) As SetterAction(Of T)
            Return _
                Sub(value, valueSetter)
                    Dim exception = Assert.Throws(Of COMException)(
                        Sub()
                            valueSetter(value)
                        End Sub)

                    Assert.Equal(errorCode, exception.ErrorCode)
                End Sub

        End Function

        Protected Function ThrowsInvalidOperationException(Of T)() As SetterAction(Of T)
            Return _
                Sub(value, valueSetter)
                    Assert.Throws(Of InvalidOperationException)(
                        Sub()
                            valueSetter(value)
                        End Sub)
                End Sub

        End Function

        Protected Function ThrowsNotImplementedException(Of T)() As SetterAction(Of T)
            Return _
                Sub(value, valueSetter)
                    Assert.Throws(Of NotImplementedException)(
                        Sub()
                            valueSetter(value)
                        End Sub)
                End Sub

        End Function

        Protected MustOverride Function GetStartPointFunc(codeElement As TCodeElement) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
        Protected MustOverride Function GetEndPointFunc(codeElement As TCodeElement) As Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)
        Protected MustOverride Function GetFullName(codeElement As TCodeElement) As String
        Protected MustOverride Function GetKind(codeElement As TCodeElement) As EnvDTE.vsCMElement
        Protected MustOverride Function GetName(codeElement As TCodeElement) As String
        Protected MustOverride Function GetNameSetter(codeElement As TCodeElement) As Action(Of String)

        Protected Overridable Function GetNamespace(codeElement As TCodeElement) As EnvDTE.CodeNamespace
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetAccess(codeElement As TCodeElement) As EnvDTE.vsCMAccess
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetAttributes(codeElement As TCodeElement) As EnvDTE.CodeElements
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetBases(codeElement As TCodeElement) As EnvDTE.CodeElements
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetClassKind(codeElement As TCodeElement) As EnvDTE80.vsCMClassKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetComment(codeElement As TCodeElement) As String
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetCommentSetter(codeElement As TCodeElement) As Action(Of String)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetConstKind(codeElement As TCodeElement) As EnvDTE80.vsCMConstKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetDataTypeKind(codeElement As TCodeElement) As EnvDTE80.vsCMDataTypeKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetDocComment(codeElement As TCodeElement) As String
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetDocCommentSetter(codeElement As TCodeElement) As Action(Of String)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetImplementedInterfaces(codeElement As TCodeElement) As EnvDTE.CodeElements
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetInheritanceKind(codeElement As TCodeElement) As EnvDTE80.vsCMInheritanceKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsAbstract(codeElement As TCodeElement) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsDefault(codeElement As TCodeElement) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsGeneric(codeElement As TCodeElement) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsShared(codeElement As TCodeElement) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetMustImplement(codeElement As TCodeElement) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetOverrideKind(codeElement As TCodeElement) As EnvDTE80.vsCMOverrideKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetParent(codeElement As TCodeElement) As Object
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetParts(codeElement As TCodeElement) As EnvDTE.CodeElements
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetPrototype(codeElement As TCodeElement, flags As EnvDTE.vsCMPrototype) As String
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetReadWrite(codeElement As TCodeElement) As EnvDTE80.vsCMPropertyKind
            Throw New NotSupportedException
        End Function

        Protected Overridable Overloads Function GetTypeProp(codeElement As TCodeElement) As EnvDTE.CodeTypeRef
            Throw New NotSupportedException
        End Function

        Protected Overridable Function IsDerivedFrom(codeElement As TCodeElement, fullName As String) As Boolean
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddAttribute(codeElement As TCodeElement, data As AttributeData) As EnvDTE.CodeAttribute
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddEnumMember(codeElement As TCodeElement, data As EnumMemberData) As EnvDTE.CodeVariable
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddEvent(codeElement As TCodeElement, data As EventData) As EnvDTE80.CodeEvent
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddFunction(codeElement As TCodeElement, data As FunctionData) As EnvDTE.CodeFunction
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddParameter(codeElement As TCodeElement, data As ParameterData) As EnvDTE.CodeParameter
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddProperty(codeElement As TCodeElement, data As PropertyData) As EnvDTE.CodeProperty
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddVariable(codeElement As TCodeElement, data As VariableData) As EnvDTE.CodeVariable
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetAccessSetter(codeElement As TCodeElement) As Action(Of EnvDTE.vsCMAccess)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetClassKindSetter(codeElement As TCodeElement) As Action(Of EnvDTE80.vsCMClassKind)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetConstKindSetter(codeElement As TCodeElement) As Action(Of EnvDTE80.vsCMConstKind)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetDataTypeKindSetter(codeElement As TCodeElement) As Action(Of EnvDTE80.vsCMDataTypeKind)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetInheritanceKindSetter(codeElement As TCodeElement) As Action(Of EnvDTE80.vsCMInheritanceKind)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsAbstractSetter(codeElement As TCodeElement) As Action(Of Boolean)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsDefaultSetter(codeElement As TCodeElement) As Action(Of Boolean)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetIsSharedSetter(codeElement As TCodeElement) As Action(Of Boolean)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetMustImplementSetter(codeElement As TCodeElement) As Action(Of Boolean)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetOverrideKindSetter(codeElement As TCodeElement) As Action(Of EnvDTE80.vsCMOverrideKind)
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetTypePropSetter(codeElement As TCodeElement) As Action(Of EnvDTE.CodeTypeRef)
            Throw New NotSupportedException
        End Function

        Protected Overridable Sub RemoveChild(codeElement As TCodeElement, child As Object)
            Throw New NotSupportedException
        End Sub

        Protected Overridable Function GenericNameExtender_GetBaseTypesCount(codeElement As TCodeElement) As Integer
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GenericNameExtender_GetImplementedTypesCount(codeElement As TCodeElement) As Integer
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GenericNameExtender_GetBaseGenericName(codeElement As TCodeElement, index As Integer) As String
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GenericNameExtender_GetImplTypeGenericName(codeElement As TCodeElement, index As Integer) As String
            Throw New NotSupportedException
        End Function

        Protected Overridable Function AddBase(codeElement As TCodeElement, base As Object, position As Object) As EnvDTE.CodeElement
            Throw New NotSupportedException
        End Function

        Protected Overridable Sub RemoveBase(codeElement As TCodeElement, element As Object)
            Throw New NotSupportedException
        End Sub

        Protected Overridable Function AddImplementedInterface(codeElement As TCodeElement, base As Object, position As Object) As EnvDTE.CodeInterface
            Throw New NotSupportedException
        End Function

        Protected Overridable Function GetParameters(codeElement As TCodeElement) As EnvDTE.CodeElements
            Throw New NotSupportedException
        End Function

        Protected Overridable Sub RemoveImplementedInterface(codeElement As TCodeElement, element As Object)
            Throw New NotSupportedException
        End Sub

        Protected Sub TestPropertyDescriptors(code As XElement, ParamArray expectedPropertyNames As String())
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim propertyDescriptors = ComponentModel.TypeDescriptor.GetProperties(codeElement)
                Dim propertyNames = propertyDescriptors _
                    .OfType(Of ComponentModel.PropertyDescriptor) _
                    .Select(Function(pd) pd.Name) _
                    .ToArray()

                Assert.Equal(expectedPropertyNames, propertyNames)
            End Using
        End Sub

        Protected Sub TestElement(code As XElement, expected As Action(Of TCodeElement))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                expected(codeElement)
            End Using
        End Sub

        Protected Sub TestGetStartPoint(code As XElement, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim textPointGetter = GetStartPointFunc(codeElement)

                For Each action In expectedParts
                    action(textPointGetter)
                Next
            End Using
        End Sub

        Protected Sub TestGetEndPoint(code As XElement, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint)))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim textPointGetter = GetEndPointFunc(codeElement)

                For Each action In expectedParts
                    action(textPointGetter)
                Next
            End Using
        End Sub

        Protected Sub TestAccess(code As XElement, expectedAccess As EnvDTE.vsCMAccess)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim access = GetAccess(codeElement)
                Assert.Equal(expectedAccess, access)
            End Using
        End Sub

        Protected Sub TestAttributes(code As XElement, ParamArray expectedAttributes() As Action(Of Object))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim attributes = GetAttributes(codeElement)
                Assert.Equal(expectedAttributes.Length, attributes.Count)

                For i = 1 To attributes.Count
                    expectedAttributes(i - 1)(attributes.Item(i))
                Next
            End Using
        End Sub

        Protected Sub TestBases(code As XElement, ParamArray expectedBases() As Action(Of Object))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim bases = GetBases(codeElement)
                Assert.Equal(expectedBases.Length, bases.Count)

                For i = 1 To bases.Count
                    expectedBases(i - 1)(bases.Item(i))
                Next
            End Using
        End Sub

        Protected Sub TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim element = CType(codeElement, EnvDTE.CodeElement)
                Assert.True(element IsNot Nothing, "Could not cast " & GetType(TCodeElement).FullName & " to " & GetType(EnvDTE.CodeElement).FullName & ".")

                Dim children = element.Children
                Assert.Equal(expectedChildren.Length, children.Count)

                For i = 1 To children.Count
                    expectedChildren(i - 1)(children.Item(i))
                Next
            End Using
        End Sub

        Protected Sub TestClassKind(code As XElement, expectedClassKind As EnvDTE80.vsCMClassKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim classKind = GetClassKind(codeElement)
                Assert.Equal(expectedClassKind, classKind)
            End Using
        End Sub

        Protected Sub TestComment(code As XElement, expectedComment As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim comment = GetComment(codeElement)
                Assert.Equal(expectedComment, comment)
            End Using
        End Sub

        Protected Sub TestConstKind(code As XElement, expectedConstKind As EnvDTE80.vsCMConstKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim constKind = GetConstKind(codeElement)
                Assert.Equal(expectedConstKind, constKind)
            End Using
        End Sub

        Protected Sub TestDataTypeKind(code As XElement, expectedDataTypeKind As EnvDTE80.vsCMDataTypeKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim dataTypeKind = GetDataTypeKind(codeElement)
                Assert.Equal(expectedDataTypeKind, dataTypeKind)
            End Using
        End Sub

        Protected Sub TestDocComment(code As XElement, expectedDocComment As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim docComment = GetDocComment(codeElement)
                Assert.Equal(expectedDocComment, docComment)
            End Using
        End Sub

        Protected Sub TestFullName(code As XElement, expectedFullName As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim fullName = GetFullName(codeElement)
                Assert.Equal(expectedFullName, fullName)
            End Using
        End Sub

        Protected Sub TestImplementedInterfaces(code As XElement, ParamArray expectedBases() As Action(Of Object))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim implementedInterfaces = GetImplementedInterfaces(codeElement)
                Assert.Equal(expectedBases.Length, implementedInterfaces.Count)

                For i = 1 To implementedInterfaces.Count
                    expectedBases(i - 1)(implementedInterfaces.Item(i))
                Next
            End Using
        End Sub

        Protected Sub TestInheritanceKind(code As XElement, expectedInheritanceKind As EnvDTE80.vsCMInheritanceKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim inheritanceKind = GetInheritanceKind(codeElement)
                Assert.Equal(expectedInheritanceKind, inheritanceKind)
            End Using
        End Sub

        Protected Sub TestIsAbstract(code As XElement, expectedValue As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim value = GetIsAbstract(codeElement)
                Assert.Equal(expectedValue, value)
            End Using
        End Sub

        Protected Sub TestIsDefault(code As XElement, expectedValue As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim value = GetIsDefault(codeElement)
                Assert.Equal(expectedValue, value)
            End Using
        End Sub

        Protected Sub TestIsGeneric(code As XElement, expectedValue As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim value = GetIsGeneric(codeElement)
                Assert.Equal(expectedValue, value)
            End Using
        End Sub

        Protected Sub TestIsShared(code As XElement, expectedValue As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim value = GetIsShared(codeElement)
                Assert.Equal(expectedValue, value)
            End Using
        End Sub

        Protected Sub TestKind(code As XElement, expectedKind As EnvDTE.vsCMElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)(expectedKind)
                Assert.NotNull(codeElement)

                Dim kind = GetKind(codeElement)

                Assert.Equal(expectedKind, kind)
            End Using
        End Sub

        Protected Sub TestMustImplement(code As XElement, expectedValue As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim value = GetMustImplement(codeElement)
                Assert.Equal(expectedValue, value)
            End Using
        End Sub

        Protected Sub TestName(code As XElement, expectedName As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim name = GetName(codeElement)
                Assert.Equal(expectedName, name)
            End Using
        End Sub

        Protected Sub TestOverrideKind(code As XElement, expectedOverrideKind As EnvDTE80.vsCMOverrideKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim overrideKind = GetOverrideKind(codeElement)
                Assert.Equal(expectedOverrideKind, overrideKind)
            End Using
        End Sub

        Protected Sub TestParts(code As XElement, expectedPartCount As Integer)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim parts = GetParts(codeElement)

                ' TODO: Test the elements themselves, not just the count (PartialTypeCollection.Item is not 
                ' fully implemented)
                Assert.Equal(expectedPartCount, parts.Count)
            End Using
        End Sub

        Protected Sub TestParent(code As XElement, expectedParent As Action(Of Object))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim parent = GetParent(codeElement)

                expectedParent(parent)
            End Using
        End Sub

        Protected Sub TestPrototype(code As XElement, flags As EnvDTE.vsCMPrototype, expectedPrototype As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim prototype = GetPrototype(codeElement, flags)

                Assert.Equal(expectedPrototype, prototype)
            End Using
        End Sub

        Protected Sub TestPrototypeThrows(Of TException As Exception)(code As XElement, flags As EnvDTE.vsCMPrototype)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() GetPrototype(codeElement, flags))
            End Using
        End Sub

        Protected Sub TestReadWrite(code As XElement, expectedOverrideKind As EnvDTE80.vsCMPropertyKind)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim readWrite = GetReadWrite(codeElement)
                Assert.Equal(expectedOverrideKind, readWrite)
            End Using
        End Sub

        Protected Sub TestIsDerivedFrom(code As XElement, baseFullName As String, expectedIsDerivedFrom As Boolean)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim actualIsDerivedFrom = IsDerivedFrom(codeElement, baseFullName)

                Assert.Equal(expectedIsDerivedFrom, actualIsDerivedFrom)
            End Using
        End Sub

        Protected Overrides Sub TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim attribute = AddAttribute(codeElement, data)
                Assert.NotNull(attribute)
                Assert.Equal(data.Name, attribute.Name)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddEnumMember(code As XElement, expectedCode As XElement, data As EnumMemberData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim enumMember = AddEnumMember(codeElement, data)
                Assert.NotNull(enumMember)
                Assert.Equal(data.Name, enumMember.Name)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddEvent(code As XElement, expectedCode As XElement, data As EventData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim ev = AddEvent(codeElement, data)
                Assert.NotNull(ev)
                Assert.Equal(data.Name, ev.Name)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim func = AddFunction(codeElement, data)
                Assert.NotNull(func)

                If data.Kind <> EnvDTE.vsCMFunction.vsCMFunctionDestructor Then
                    Assert.Equal(data.Name, func.Name)
                End If

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddParameter(code As XElement, expectedCode As XElement, data As ParameterData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim name = GetName(codeElement)

                Dim parameter = AddParameter(codeElement, data)
                Assert.NotNull(parameter)
                Assert.Equal(data.Name, parameter.Name)

                ' Verify we haven't screwed up any node keys by checking that we
                ' can still access the parent element after adding the parameter.
                Assert.Equal(name, GetName(codeElement))

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddProperty(code As XElement, expectedCode As XElement, data As PropertyData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim prop = AddProperty(codeElement, data)
                Assert.NotNull(prop)
                Assert.True(data.GetterName = prop.Name OrElse data.PutterName = prop.Name)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim variable = AddVariable(codeElement, data)
                Assert.NotNull(variable)
                Assert.Equal(data.Name, variable.Name)

                Dim text = state.GetDocumentAtCursor().GetTextAsync(CancellationToken.None).Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Overrides Sub TestRemoveChild(code As XElement, expectedCode As XElement, child As Object)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim name = GetName(codeElement)

                RemoveChild(codeElement, child)

                ' Verify we haven't screwed up any node keys by checking that we
                ' can still access the parent element after deleting the child.
                Assert.Equal(name, GetName(codeElement))

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestTypeProp(code As XElement, data As CodeTypeRefData)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim codeTypeRef = GetTypeProp(codeElement)

                TestCodeTypeRef(codeTypeRef, data)
            End Using
        End Sub

        Protected Sub TestSetAccess(code As XElement, expectedCode As XElement, access As EnvDTE.vsCMAccess)
            TestSetAccess(code, expectedCode, access, NoThrow(Of EnvDTE.vsCMAccess)())
        End Sub

        Protected Sub TestSetAccess(code As XElement, expectedCode As XElement, access As EnvDTE.vsCMAccess, action As SetterAction(Of EnvDTE.vsCMAccess))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim accessSetter = GetAccessSetter(codeElement)

                action(access, accessSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetClassKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMClassKind)
            TestSetClassKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMClassKind)())
        End Sub

        Protected Sub TestSetClassKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMClassKind, action As SetterAction(Of EnvDTE80.vsCMClassKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim classKindSetter = GetClassKindSetter(codeElement)

                action(kind, classKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetComment(code As XElement, expectedCode As XElement, value As String)
            TestSetComment(code, expectedCode, value, NoThrow(Of String)())
        End Sub

        Protected Sub TestSetComment(code As XElement, expectedCode As XElement, value As String, action As SetterAction(Of String))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim commentSetter = GetCommentSetter(codeElement)

                action(value, commentSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetConstKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMConstKind)
            TestSetConstKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMConstKind)())
        End Sub

        Protected Sub TestSetConstKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMConstKind, action As SetterAction(Of EnvDTE80.vsCMConstKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim constKindSetter = GetConstKindSetter(codeElement)

                action(kind, constKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetDataTypeKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMDataTypeKind)
            TestSetDataTypeKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMDataTypeKind)())
        End Sub

        Protected Sub TestSetDataTypeKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMDataTypeKind, action As SetterAction(Of EnvDTE80.vsCMDataTypeKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim dataTypeKindSetter = GetDataTypeKindSetter(codeElement)

                action(kind, dataTypeKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetDocComment(code As XElement, expectedCode As XElement, value As String)
            TestSetDocComment(code, expectedCode, value, NoThrow(Of String)())
        End Sub

        Protected Sub TestSetDocComment(code As XElement, expectedCode As XElement, value As String, action As SetterAction(Of String))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim docCommentSetter = GetDocCommentSetter(codeElement)

                action(value, docCommentSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetInheritanceKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMInheritanceKind)
            TestSetInheritanceKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMInheritanceKind)())
        End Sub

        Protected Sub TestSetInheritanceKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMInheritanceKind, action As SetterAction(Of EnvDTE80.vsCMInheritanceKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim inheritanceKindSetter = GetInheritanceKindSetter(codeElement)

                action(kind, inheritanceKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetIsAbstract(code As XElement, expectedCode As XElement, value As Boolean)
            TestSetIsAbstract(code, expectedCode, value, NoThrow(Of Boolean)())
        End Sub

        Protected Sub TestSetIsAbstract(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim isAbstractSetter = GetIsAbstractSetter(codeElement)

                action(value, isAbstractSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetIsDefault(code As XElement, expectedCode As XElement, value As Boolean)
            TestSetIsDefault(code, expectedCode, value, NoThrow(Of Boolean)())
        End Sub

        Protected Sub TestSetIsDefault(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim isDefaultSetter = GetIsDefaultSetter(codeElement)

                action(value, isDefaultSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetIsShared(code As XElement, expectedCode As XElement, value As Boolean)
            TestSetIsShared(code, expectedCode, value, NoThrow(Of Boolean)())
        End Sub

        Protected Sub TestSetIsShared(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim isSharedSetter = GetIsSharedSetter(codeElement)

                action(value, isSharedSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetMustImplement(code As XElement, expectedCode As XElement, value As Boolean)
            TestSetMustImplement(code, expectedCode, value, NoThrow(Of Boolean)())
        End Sub

        Protected Sub TestSetMustImplement(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim mustImplementSetter = GetMustImplementSetter(codeElement)

                action(value, mustImplementSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetOverrideKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMOverrideKind)
            TestSetOverrideKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMOverrideKind)())
        End Sub

        Protected Sub TestSetOverrideKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMOverrideKind, action As SetterAction(Of EnvDTE80.vsCMOverrideKind))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim overrideKindSetter = GetOverrideKindSetter(codeElement)

                action(kind, overrideKindSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetName(code As XElement, expectedCode As XElement, name As String, action As SetterAction(Of String))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim nameSetter = GetNameSetter(codeElement)

                action(name, nameSetter)

                Assert.Equal(name, GetName(codeElement))

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestNamespaceName(Code As XElement, name As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(Code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim codeNamespaceElement = GetNamespace(codeElement)
                Assert.NotNull(codeNamespaceElement)

                Assert.Equal(name, codeNamespaceElement.Name)
            End Using
        End Sub

        Protected Sub TestSetTypeProp(code As XElement, expectedCode As XElement, codeTypeRef As EnvDTE.CodeTypeRef)
            TestSetTypeProp(code, expectedCode, codeTypeRef, NoThrow(Of EnvDTE.CodeTypeRef)())
        End Sub

        Protected Sub TestSetTypeProp(code As XElement, expectedCode As XElement, codeTypeRef As EnvDTE.CodeTypeRef, action As SetterAction(Of EnvDTE.CodeTypeRef))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim typePropSetter = GetTypePropSetter(codeElement)

                action(codeTypeRef, typePropSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestSetTypeProp(code As XElement, expectedCode As XElement, typeName As String)
            TestSetTypeProp(code, expectedCode, typeName, NoThrow(Of EnvDTE.CodeTypeRef)())
        End Sub

        Protected Sub TestSetTypeProp(code As XElement, expectedCode As XElement, typeName As String, action As SetterAction(Of EnvDTE.CodeTypeRef))
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeTypeRef = state.RootCodeModel.CreateCodeTypeRef(typeName)

                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim typePropSetter = GetTypePropSetter(codeElement)

                action(codeTypeRef, typePropSetter)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetBaseTypesCount(code As XElement, expected As Integer)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Equal(expected, GenericNameExtender_GetBaseTypesCount(codeElement))
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetImplementedTypesCount(code As XElement, expected As Integer)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Equal(expected, GenericNameExtender_GetImplementedTypesCount(codeElement))
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetImplementedTypesCountThrows(Of TException As Exception)(code As XElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() GenericNameExtender_GetImplementedTypesCount(codeElement))
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetBaseGenericName(code As XElement, index As Integer, expected As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Equal(expected, GenericNameExtender_GetBaseGenericName(codeElement, index))
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetImplTypeGenericName(code As XElement, index As Integer, expected As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Equal(expected, GenericNameExtender_GetImplTypeGenericName(codeElement, index))
            End Using
        End Sub

        Protected Sub TestGenericNameExtender_GetImplTypeGenericNameThrows(Of TException As Exception)(code As XElement, index As Integer)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() GenericNameExtender_GetImplTypeGenericName(codeElement, index))
            End Using
        End Sub

        Protected Sub TestAddBase(code As XElement, base As Object, position As Object, expectedCode As XElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                AddBase(codeElement, base, position)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestAddBaseThrows(Of TException As Exception)(code As XElement, base As Object, position As Object)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() AddBase(codeElement, base, position))
            End Using
        End Sub

        Protected Sub TestRemoveBase(code As XElement, element As Object, expectedCode As XElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                RemoveBase(codeElement, element)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestRemoveBaseThrows(Of TException As Exception)(code As XElement, element As Object)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() RemoveBase(codeElement, element))
            End Using
        End Sub

        Protected Sub TestAddImplementedInterface(code As XElement, base As Object, position As Object, expectedCode As XElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                AddImplementedInterface(codeElement, base, position)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestAddImplementedInterfaceThrows(Of TException As Exception)(code As XElement, base As Object, position As Object)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() AddImplementedInterface(codeElement, base, position))
            End Using
        End Sub

        Protected Sub TestRemoveImplementedInterface(code As XElement, element As Object, expectedCode As XElement)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                RemoveImplementedInterface(codeElement, element)

                Dim text = state.GetDocumentAtCursor().GetTextAsync().Result.ToString()

                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Sub

        Protected Sub TestRemoveImplementedInterfaceThrows(Of TException As Exception)(code As XElement, element As Object)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Assert.Throws(Of TException)(Sub() RemoveImplementedInterface(codeElement, element))
            End Using
        End Sub

        Protected Sub TestAllParameterNames(code As XElement, ParamArray expectedParameterNames() As String)
            Using state = CreateCodeModelTestState(GetWorkspaceDefinition(code))
                Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)()
                Assert.NotNull(codeElement)

                Dim parameters = GetParameters(codeElement)
                Assert.NotNull(parameters)

                Assert.Equal(parameters.Count(), expectedParameterNames.Count())
                If (expectedParameterNames.Any()) Then
                    TestAllParameterNamesByIndex(parameters, expectedParameterNames)
                    TestAllParameterNamesByName(parameters, expectedParameterNames)
                End If
            End Using
        End Sub

        Private Sub TestAllParameterNamesByName(parameters As EnvDTE.CodeElements, expectedParameterNames() As String)
            For index = 0 To expectedParameterNames.Count() - 1
                Assert.NotNull(parameters.Item(expectedParameterNames(index)))
            Next
        End Sub

        Private Sub TestAllParameterNamesByIndex(parameters As EnvDTE.CodeElements, expectedParameterNames() As String)
            For index = 0 To expectedParameterNames.Count() - 1
                ' index + 1 for Item because Parameters are not zero indexed
                Assert.Equal(expectedParameterNames(index), parameters.Item(index + 1).Name)
            Next
        End Sub
    End Class
End Namespace
