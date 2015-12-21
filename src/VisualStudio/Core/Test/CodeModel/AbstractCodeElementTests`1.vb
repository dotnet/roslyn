' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Partial Public MustInherit Class AbstractCodeElementTests(Of TCodeElement As Class)
        Inherits AbstractCodeModelObjectTests(Of TCodeElement)

        Protected Overridable ReadOnly Property TargetExternalCodeElements As Boolean
            Get
                Return False
            End Get
        End Property

        Private Function GetCodeElement(state As CodeModelTestState) As TCodeElement
            Dim codeElement = state.GetCodeElementAtCursor(Of TCodeElement)

            Return If(codeElement IsNot Nothing AndAlso TargetExternalCodeElements,
                      codeElement.AsExternal(),
                      codeElement)
        End Function

        Protected Async Function TestElement(code As XElement, expected As Action(Of TCodeElement)) As Tasks.Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim codeElement = GetCodeElement(state)
                Assert.NotNull(codeElement)

                expected(codeElement)
            End Using
        End Function

        Protected Overloads Async Function TestElementUpdate(code As XElement, expectedCode As XElement, updater As Action(Of TCodeElement)) As Tasks.Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim codeElement = GetCodeElement(state)
                Assert.NotNull(codeElement)

                updater(codeElement)

                Dim text = (Await state.GetDocumentAtCursor().GetTextAsync()).ToString()
                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Function

        Friend Overloads Async Function TestElementUpdate(code As XElement, expectedCode As XElement, updater As Action(Of CodeModelTestState, TCodeElement)) As Tasks.Task
            Using state = Await CreateCodeModelTestStateAsync(GetWorkspaceDefinition(code))
                Dim codeElement = GetCodeElement(state)
                Assert.NotNull(codeElement)

                updater(state, codeElement)

                Dim text = (Await state.GetDocumentAtCursor().GetTextAsync()).ToString()
                Assert.Equal(expectedCode.NormalizedValue.Trim(), text.Trim())
            End Using
        End Function

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

        Protected Async Function TestPropertyDescriptors(code As XElement, ParamArray expectedPropertyNames As String()) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim propertyDescriptors = ComponentModel.TypeDescriptor.GetProperties(codeElement)
                    Dim propertyNames = propertyDescriptors _
                    .OfType(Of ComponentModel.PropertyDescriptor) _
                    .Select(Function(pd) pd.Name) _
                    .ToArray()

                    Assert.Equal(expectedPropertyNames, propertyNames)
                End Sub)
        End Function

        Protected Async Function TestGetStartPoint(code As XElement, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint))) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim textPointGetter = GetStartPointFunc(codeElement)

                    For Each action In expectedParts
                        action(textPointGetter)
                    Next
                End Sub)
        End Function

        Protected Async Function TestGetEndPoint(code As XElement, ParamArray expectedParts() As Action(Of Func(Of EnvDTE.vsCMPart, EnvDTE.TextPoint))) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim textPointGetter = GetEndPointFunc(codeElement)

                    For Each action In expectedParts
                        action(textPointGetter)
                    Next
                End Sub)
        End Function

        Protected Async Function TestAccess(code As XElement, expectedAccess As EnvDTE.vsCMAccess) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim access = GetAccess(codeElement)
                    Assert.Equal(expectedAccess, access)
                End Sub)
        End Function

        Protected Async Function TestAttributes(code As XElement, ParamArray expectedAttributes() As Action(Of Object)) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim attributes = GetAttributes(codeElement)
                    Assert.Equal(expectedAttributes.Length, attributes.Count)

                    For i = 1 To attributes.Count
                        expectedAttributes(i - 1)(attributes.Item(i))
                    Next
                End Sub)
        End Function

        Protected Async Function TestBases(code As XElement, ParamArray expectedBases() As Action(Of Object)) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim bases = GetBases(codeElement)
                    Assert.Equal(expectedBases.Length, bases.Count)

                    For i = 1 To bases.Count
                        expectedBases(i - 1)(bases.Item(i))
                    Next
                End Sub)
        End Function

        Protected Overrides Async Function TestChildren(code As XElement, ParamArray expectedChildren() As Action(Of Object)) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim element = CType(codeElement, EnvDTE.CodeElement)
                    Assert.True(element IsNot Nothing, $"Could not cast {GetType(TCodeElement).FullName} to {GetType(EnvDTE.CodeElement).FullName}.")

                    Dim children = element.Children
                    Assert.Equal(expectedChildren.Length, children.Count)

                    For i = 1 To children.Count
                        expectedChildren(i - 1)(children.Item(i))
                    Next
                End Sub)
        End Function

        Protected Async Function TestClassKind(code As XElement, expectedClassKind As EnvDTE80.vsCMClassKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim classKind = GetClassKind(codeElement)
                    Assert.Equal(expectedClassKind, classKind)
                End Sub)
        End Function

        Protected Async Function TestComment(code As XElement, expectedComment As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim comment = GetComment(codeElement)
                    Assert.Equal(expectedComment, comment)
                End Sub)
        End Function

        Protected Async Function TestConstKind(code As XElement, expectedConstKind As EnvDTE80.vsCMConstKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim constKind = GetConstKind(codeElement)
                    Assert.Equal(expectedConstKind, constKind)
                End Sub)
        End Function

        Protected Async Function TestDataTypeKind(code As XElement, expectedDataTypeKind As EnvDTE80.vsCMDataTypeKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim dataTypeKind = GetDataTypeKind(codeElement)
                    Assert.Equal(expectedDataTypeKind, dataTypeKind)
                End Sub)
        End Function

        Protected Async Function TestDocComment(code As XElement, expectedDocComment As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim docComment = GetDocComment(codeElement)
                    Assert.Equal(expectedDocComment, docComment)
                End Sub)
        End Function

        Protected Async Function TestFullName(code As XElement, expectedFullName As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim fullName = GetFullName(codeElement)
                    Assert.Equal(expectedFullName, fullName)
                End Sub)
        End Function

        Protected Async Function TestImplementedInterfaces(code As XElement, ParamArray expectedBases() As Action(Of Object)) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim implementedInterfaces = GetImplementedInterfaces(codeElement)
                    Assert.Equal(expectedBases.Length, implementedInterfaces.Count)

                    For i = 1 To implementedInterfaces.Count
                        expectedBases(i - 1)(implementedInterfaces.Item(i))
                    Next
                End Sub)
        End Function

        Protected Async Function TestInheritanceKind(code As XElement, expectedInheritanceKind As EnvDTE80.vsCMInheritanceKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim inheritanceKind = GetInheritanceKind(codeElement)
                    Assert.Equal(expectedInheritanceKind, inheritanceKind)
                End Sub)
        End Function

        Protected Async Function TestIsAbstract(code As XElement, expectedValue As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim value = GetIsAbstract(codeElement)
                    Assert.Equal(expectedValue, value)
                End Sub)
        End Function

        Protected Async Function TestIsDefault(code As XElement, expectedValue As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim value = GetIsDefault(codeElement)
                    Assert.Equal(expectedValue, value)
                End Sub)
        End Function

        Protected Async Function TestIsGeneric(code As XElement, expectedValue As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim value = GetIsGeneric(codeElement)
                    Assert.Equal(expectedValue, value)
                End Sub)
        End Function

        Protected Async Function TestIsShared(code As XElement, expectedValue As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim value = GetIsShared(codeElement)
                    Assert.Equal(expectedValue, value)
                End Sub)
        End Function

        Protected Async Function TestKind(code As XElement, expectedKind As EnvDTE.vsCMElement) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim kind = GetKind(codeElement)
                    Assert.Equal(expectedKind, kind)
                End Sub)
        End Function

        Protected Async Function TestMustImplement(code As XElement, expectedValue As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim value = GetMustImplement(codeElement)
                    Assert.Equal(expectedValue, value)
                End Sub)
        End Function

        Protected Async Function TestName(code As XElement, expectedName As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim name = GetName(codeElement)
                    Assert.Equal(expectedName, name)
                End Sub)
        End Function

        Protected Async Function TestOverrideKind(code As XElement, expectedOverrideKind As EnvDTE80.vsCMOverrideKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim overrideKind = GetOverrideKind(codeElement)
                    Assert.Equal(expectedOverrideKind, overrideKind)
                End Sub)
        End Function

        Protected Async Function TestParts(code As XElement, expectedPartCount As Integer) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim parts = GetParts(codeElement)

                    ' TODO: Test the elements themselves, not just the count (PartialTypeCollection.Item is not fully implemented)
                    Assert.Equal(expectedPartCount, parts.Count)
                End Sub)
        End Function

        Protected Async Function TestParent(code As XElement, expectedParent As Action(Of Object)) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim parent = GetParent(codeElement)
                    expectedParent(parent)
                End Sub)
        End Function

        Protected Async Function TestPrototype(code As XElement, flags As EnvDTE.vsCMPrototype, expectedPrototype As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim prototype = GetPrototype(codeElement, flags)
                    Assert.Equal(expectedPrototype, prototype)
                End Sub)
        End Function

        Protected Async Function TestPrototypeThrows(Of TException As Exception)(code As XElement, flags As EnvDTE.vsCMPrototype) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() GetPrototype(codeElement, flags))
                End Sub)
        End Function

        Protected Async Function TestReadWrite(code As XElement, expectedOverrideKind As EnvDTE80.vsCMPropertyKind) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim readWrite = GetReadWrite(codeElement)
                    Assert.Equal(expectedOverrideKind, readWrite)
                End Sub)
        End Function

        Protected Async Function TestIsDerivedFrom(code As XElement, baseFullName As String, expectedIsDerivedFrom As Boolean) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim actualIsDerivedFrom = IsDerivedFrom(codeElement, baseFullName)
                    Assert.Equal(expectedIsDerivedFrom, actualIsDerivedFrom)
                End Sub)
        End Function

        Protected Async Function TestTypeProp(code As XElement, data As CodeTypeRefData) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim codeTypeRef = GetTypeProp(codeElement)
                    TestCodeTypeRef(codeTypeRef, data)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddAttribute(code As XElement, expectedCode As XElement, data As AttributeData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim attribute = AddAttribute(codeElement, data)
                    Assert.NotNull(attribute)
                    Assert.Equal(data.Name, attribute.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddEnumMember(code As XElement, expectedCode As XElement, data As EnumMemberData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim enumMember = AddEnumMember(codeElement, data)
                    Assert.NotNull(enumMember)
                    Assert.Equal(data.Name, enumMember.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddEvent(code As XElement, expectedCode As XElement, data As EventData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim ev = AddEvent(codeElement, data)
                    Assert.NotNull(ev)
                    Assert.Equal(data.Name, ev.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddFunction(code As XElement, expectedCode As XElement, data As FunctionData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim func = AddFunction(codeElement, data)
                    Assert.NotNull(func)

                    If data.Kind <> EnvDTE.vsCMFunction.vsCMFunctionDestructor Then
                        Assert.Equal(data.Name, func.Name)
                    End If
                End Sub)
        End Function

        Protected Overrides Async Function TestAddParameter(code As XElement, expectedCode As XElement, data As ParameterData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim name = GetName(codeElement)

                    Dim parameter = AddParameter(codeElement, data)
                    Assert.NotNull(parameter)
                    Assert.Equal(data.Name, parameter.Name)

                    ' Verify we haven't screwed up any node keys by checking that we
                    ' can still access the parent element after adding the parameter.
                    Assert.Equal(name, GetName(codeElement))
                End Sub)
        End Function

        Protected Overrides Async Function TestAddProperty(code As XElement, expectedCode As XElement, data As PropertyData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim prop = AddProperty(codeElement, data)
                    Assert.NotNull(prop)
                    Assert.True(data.GetterName = prop.Name OrElse data.PutterName = prop.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestAddVariable(code As XElement, expectedCode As XElement, data As VariableData) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim variable = AddVariable(codeElement, data)
                    Assert.NotNull(variable)
                    Assert.Equal(data.Name, variable.Name)
                End Sub)
        End Function

        Protected Overrides Async Function TestRemoveChild(code As XElement, expectedCode As XElement, child As Object) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim name = GetName(codeElement)

                    RemoveChild(codeElement, child)

                    ' Verify we haven't screwed up any node keys by checking that we
                    ' can still access the parent element after deleting the child.
                    Assert.Equal(name, GetName(codeElement))
                End Sub)
        End Function

        Protected Async Function TestSetAccess(code As XElement, expectedCode As XElement, access As EnvDTE.vsCMAccess) As Tasks.Task
            Await TestSetAccess(code, expectedCode, access, NoThrow(Of EnvDTE.vsCMAccess)())
        End Function

        Protected Async Function TestSetAccess(code As XElement, expectedCode As XElement, access As EnvDTE.vsCMAccess, action As SetterAction(Of EnvDTE.vsCMAccess)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim accessSetter = GetAccessSetter(codeElement)
                    action(access, accessSetter)
                End Sub)
        End Function

        Protected Async Function TestSetClassKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMClassKind) As Tasks.Task
            Await TestSetClassKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMClassKind)())
        End Function

        Protected Async Function TestSetClassKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMClassKind, action As SetterAction(Of EnvDTE80.vsCMClassKind)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim classKindSetter = GetClassKindSetter(codeElement)
                    action(kind, classKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetComment(code As XElement, expectedCode As XElement, value As String) As Tasks.Task
            Await TestSetComment(code, expectedCode, value, NoThrow(Of String)())
        End Function

        Protected Async Function TestSetComment(code As XElement, expectedCode As XElement, value As String, action As SetterAction(Of String)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim commentSetter = GetCommentSetter(codeElement)
                    action(value, commentSetter)
                End Sub)
        End Function

        Protected Async Function TestSetConstKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMConstKind) As Tasks.Task
            Await TestSetConstKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMConstKind)())
        End Function

        Protected Async Function TestSetConstKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMConstKind, action As SetterAction(Of EnvDTE80.vsCMConstKind)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim constKindSetter = GetConstKindSetter(codeElement)
                    action(kind, constKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetDataTypeKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMDataTypeKind) As Tasks.Task
            Await TestSetDataTypeKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMDataTypeKind)())
        End Function

        Protected Async Function TestSetDataTypeKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMDataTypeKind, action As SetterAction(Of EnvDTE80.vsCMDataTypeKind)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim dataTypeKindSetter = GetDataTypeKindSetter(codeElement)
                    action(kind, dataTypeKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetDocComment(code As XElement, expectedCode As XElement, value As String) As Tasks.Task
            Await TestSetDocComment(code, expectedCode, value, NoThrow(Of String)())
        End Function

        Protected Async Function TestSetDocComment(code As XElement, expectedCode As XElement, value As String, action As SetterAction(Of String)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim docCommentSetter = GetDocCommentSetter(codeElement)
                    action(value, docCommentSetter)
                End Sub)
        End Function

        Protected Async Function TestSetInheritanceKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMInheritanceKind) As Tasks.Task
            Await TestSetInheritanceKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMInheritanceKind)())
        End Function

        Protected Async Function TestSetInheritanceKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMInheritanceKind, action As SetterAction(Of EnvDTE80.vsCMInheritanceKind)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim inheritanceKindSetter = GetInheritanceKindSetter(codeElement)
                    action(kind, inheritanceKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetIsAbstract(code As XElement, expectedCode As XElement, value As Boolean) As Tasks.Task
            Await TestSetIsAbstract(code, expectedCode, value, NoThrow(Of Boolean)())
        End Function

        Protected Async Function TestSetIsAbstract(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim isAbstractSetter = GetIsAbstractSetter(codeElement)
                    action(value, isAbstractSetter)
                End Sub)
        End Function

        Protected Async Function TestSetIsDefault(code As XElement, expectedCode As XElement, value As Boolean) As Tasks.Task
            Await TestSetIsDefault(code, expectedCode, value, NoThrow(Of Boolean)())
        End Function

        Protected Async Function TestSetIsDefault(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim isDefaultSetter = GetIsDefaultSetter(codeElement)
                    action(value, isDefaultSetter)
                End Sub)
        End Function

        Protected Async Function TestSetIsShared(code As XElement, expectedCode As XElement, value As Boolean) As Tasks.Task
            Await TestSetIsShared(code, expectedCode, value, NoThrow(Of Boolean)())
        End Function

        Protected Async Function TestSetIsShared(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim isSharedSetter = GetIsSharedSetter(codeElement)
                    action(value, isSharedSetter)
                End Sub)
        End Function

        Protected Async Function TestSetMustImplement(code As XElement, expectedCode As XElement, value As Boolean) As Tasks.Task
            Await TestSetMustImplement(code, expectedCode, value, NoThrow(Of Boolean)())
        End Function

        Protected Async Function TestSetMustImplement(code As XElement, expectedCode As XElement, value As Boolean, action As SetterAction(Of Boolean)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim mustImplementSetter = GetMustImplementSetter(codeElement)
                    action(value, mustImplementSetter)
                End Sub)
        End Function

        Protected Async Function TestSetOverrideKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMOverrideKind) As Tasks.Task
            Await TestSetOverrideKind(code, expectedCode, kind, NoThrow(Of EnvDTE80.vsCMOverrideKind)())
        End Function

        Protected Async Function TestSetOverrideKind(code As XElement, expectedCode As XElement, kind As EnvDTE80.vsCMOverrideKind, action As SetterAction(Of EnvDTE80.vsCMOverrideKind)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim overrideKindSetter = GetOverrideKindSetter(codeElement)
                    action(kind, overrideKindSetter)
                End Sub)
        End Function

        Protected Async Function TestSetName(code As XElement, expectedCode As XElement, name As String, action As SetterAction(Of String)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim nameSetter = GetNameSetter(codeElement)
                    action(name, nameSetter)
                End Sub)
        End Function

        Protected Async Function TestNamespaceName(code As XElement, name As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim codeNamespaceElement = GetNamespace(codeElement)
                    Assert.NotNull(codeNamespaceElement)

                    Assert.Equal(name, codeNamespaceElement.Name)
                End Sub)
        End Function

        Protected Async Function TestSetTypeProp(code As XElement, expectedCode As XElement, codeTypeRef As EnvDTE.CodeTypeRef) As Tasks.Task
            Await TestSetTypeProp(code, expectedCode, codeTypeRef, NoThrow(Of EnvDTE.CodeTypeRef)())
        End Function

        Protected Async Function TestSetTypeProp(code As XElement, expectedCode As XElement, codeTypeRef As EnvDTE.CodeTypeRef, action As SetterAction(Of EnvDTE.CodeTypeRef)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    Dim typePropSetter = GetTypePropSetter(codeElement)
                    action(codeTypeRef, typePropSetter)
                End Sub)
        End Function

        Protected Async Function TestSetTypeProp(code As XElement, expectedCode As XElement, typeName As String) As Tasks.Task
            Await TestSetTypeProp(code, expectedCode, typeName, NoThrow(Of EnvDTE.CodeTypeRef)())
        End Function

        Protected Async Function TestSetTypeProp(code As XElement, expectedCode As XElement, typeName As String, action As SetterAction(Of EnvDTE.CodeTypeRef)) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(state, codeElement)
                    Dim codeTypeRef = state.RootCodeModel.CreateCodeTypeRef(typeName)
                    Dim typePropSetter = GetTypePropSetter(codeElement)
                    action(codeTypeRef, typePropSetter)
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetBaseTypesCount(code As XElement, expected As Integer) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, GenericNameExtender_GetBaseTypesCount(codeElement))
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetImplementedTypesCount(code As XElement, expected As Integer) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, GenericNameExtender_GetImplementedTypesCount(codeElement))
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetImplementedTypesCountThrows(Of TException As Exception)(code As XElement) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() GenericNameExtender_GetImplementedTypesCount(codeElement))
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetBaseGenericName(code As XElement, index As Integer, expected As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, GenericNameExtender_GetBaseGenericName(codeElement, index))
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetImplTypeGenericName(code As XElement, index As Integer, expected As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Equal(expected, GenericNameExtender_GetImplTypeGenericName(codeElement, index))
                End Sub)
        End Function

        Protected Async Function TestGenericNameExtender_GetImplTypeGenericNameThrows(Of TException As Exception)(code As XElement, index As Integer) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() GenericNameExtender_GetImplTypeGenericName(codeElement, index))
                End Sub)
        End Function

        Protected Async Function TestAddBase(code As XElement, base As Object, position As Object, expectedCode As XElement) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    AddBase(codeElement, base, position)
                End Sub)
        End Function

        Protected Async Function TestAddBaseThrows(Of TException As Exception)(code As XElement, base As Object, position As Object) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() AddBase(codeElement, base, position))
                End Sub)
        End Function

        Protected Async Function TestRemoveBase(code As XElement, element As Object, expectedCode As XElement) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    RemoveBase(codeElement, element)
                End Sub)
        End Function

        Protected Async Function TestRemoveBaseThrows(Of TException As Exception)(code As XElement, element As Object) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() RemoveBase(codeElement, element))
                End Sub)
        End Function

        Protected Async Function TestAddImplementedInterface(code As XElement, base As Object, position As Object, expectedCode As XElement) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    AddImplementedInterface(codeElement, base, position)
                End Sub)
        End Function

        Protected Async Function TestAddImplementedInterfaceThrows(Of TException As Exception)(code As XElement, base As Object, position As Object) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() AddImplementedInterface(codeElement, base, position))
                End Sub)
        End Function

        Protected Async Function TestRemoveImplementedInterface(code As XElement, element As Object, expectedCode As XElement) As Tasks.Task
            Await TestElementUpdate(code, expectedCode,
                Sub(codeElement)
                    RemoveImplementedInterface(codeElement, element)
                End Sub)
        End Function

        Protected Async Function TestRemoveImplementedInterfaceThrows(Of TException As Exception)(code As XElement, element As Object) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Assert.Throws(Of TException)(Sub() RemoveImplementedInterface(codeElement, element))
                End Sub)
        End Function

        Protected Async Function TestAllParameterNames(code As XElement, ParamArray expectedParameterNames() As String) As Tasks.Task
            Await TestElement(code,
                Sub(codeElement)
                    Dim parameters = GetParameters(codeElement)
                    Assert.NotNull(parameters)

                    Assert.Equal(parameters.Count(), expectedParameterNames.Count())
                    If (expectedParameterNames.Any()) Then
                        TestAllParameterNamesByIndex(parameters, expectedParameterNames)
                        TestAllParameterNamesByName(parameters, expectedParameterNames)
                    End If
                End Sub)
        End Function

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
