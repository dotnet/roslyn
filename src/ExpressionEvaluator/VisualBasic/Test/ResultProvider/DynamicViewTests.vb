' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Dynamic
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class DynamicViewTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub MultipleMembers()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            o.Philosophers = New Object() {"Pythagoras", "Lucretius", "Zeno"}
            o.WhatsForDinner = "Crab Cakes"
            o.NumForks = 2

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
            Dim dynamicView = GetChildren(result).Last()
            Verify(dynamicView,
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            Verify(GetChildren(dynamicView),
                EvalResult("NumForks", "2", "System.Int32", "New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items(0)", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Philosophers", "{Length=3}", "System.Object[]", "New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items(1)", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("WhatsForDinner", """Crab Cakes""", "System.String", "New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items(2)", DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact>
        Public Sub MultipleExpansions()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            o.Answer = 42

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            ' Dynamic View should appear after all other expansions.
            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
            Verify(GetChildren(result),
                EvalResult("[Class]", "{System.Dynamic.ExpandoClass}", "System.Dynamic.ExpandoClass", "o.[Class]", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Internal),
                EvalResult("LockObject", "{Object}", "Object", "o.LockObject", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Internal),
                EvalResult("System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, Object)).Count", "1", "Integer", "((System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, Object)))o).Count", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, Object)).IsReadOnly", "False", "Boolean", "((System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of String, Object)))o).IsReadOnly", DkmEvaluationResultFlags.Boolean Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.IDictionary(Of String, Object).Keys", "Count = 1", "System.Collections.Generic.ICollection(Of String) {System.Dynamic.ExpandoObject.KeyCollection}", "((System.Collections.Generic.IDictionary(Of String, Object))o).Keys", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.IDictionary(Of String, Object).Values", "Count = 1", "System.Collections.Generic.ICollection(Of Object) {System.Dynamic.ExpandoObject.ValueCollection}", "((System.Collections.Generic.IDictionary(Of String, Object))o).Values", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("_count", "1", "Integer", "o._count", category:=DkmEvaluationResultCategory.Data, access:=DkmEvaluationResultAccessType.Private),
                EvalResult("_data", "{System.Dynamic.ExpandoObject.ExpandoData}", "System.Dynamic.ExpandoObject.ExpandoData", "o._data", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Private),
                EvalResult("_propertyChanged", "Nothing", "System.ComponentModel.PropertyChangedEventHandler", "o._propertyChanged", category:=DkmEvaluationResultCategory.Data, access:=DkmEvaluationResultAccessType.Private),
                EvalResult(Resources.SharedMembers, Nothing, "", "System.Dynamic.ExpandoObject", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact>
        Public Sub ExceptionTypeMember()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            Dim exception = New NotImplementedException()
            o.Member = exception

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
            Dim dynamicView = GetChildren(result).Last()
            Verify(dynamicView,
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            Verify(GetChildren(dynamicView),
                EvalResult("Member", $"{{{exception.ToString()}}}", "System.NotImplementedException", "New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items(0)", DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact>
        Public Sub DynamicTypeMember()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            o.Pi = Math.PI
            o.OnAndOn = o

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
            Dim members = GetChildren(result)
            Dim fullNameOnAndOn = "o"
            Dim fullNamePi = "o"
            ' Expand 3 levels...
            For i = 1 To 3
                Dim dynamicView = members.Last()
                Verify(dynamicView,
                    EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", $"{fullNameOnAndOn}, dynamic", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
                members = GetChildren(dynamicView)
                fullNamePi = $"New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView({fullNameOnAndOn}).Items(1)"
                fullNameOnAndOn = $"New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView({fullNameOnAndOn}).Items(0)"
                Verify(members,
                    EvalResult("OnAndOn", "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", fullNameOnAndOn, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Pi", "3.1415926535897931", "System.Double", fullNamePi, DkmEvaluationResultFlags.ReadOnly))
                members = GetChildren(members(0))
            Next
        End Sub

        <Fact>
        <WorkItem(5667, "https://github.com/dotnet/roslyn/issues/5667")>
        Public Sub NoMembers()
            Using New EnsureEnglishUICulture()
                Dim expression = "o"
                Dim o As Object = New ExpandoObject()

                Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
                Dim value = CreateDkmClrValue(o, type)

                Dim result = FormatResult(expression, value)
                Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
                Dim dynamicView = GetChildren(result).Last()
                Verify(dynamicView,
                       EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
                Verify(GetChildren(dynamicView),
                       EvalFailedResult(Resources.ErrorName, GetDynamicDebugViewEmptyMessage()))
            End Using
        End Sub

        <Fact>
        Public Sub NullComObject()
            Dim comObjectTypeName = "System.__ComObject"
            Dim expression = $"DirectCast(Nothing, {comObjectTypeName})"

            Dim type = New DkmClrType(CType(GetType(Object).Assembly.GetType(comObjectTypeName), TypeImpl))
            Dim value = CreateDkmClrValue(Nothing, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "Nothing", comObjectTypeName, expression))

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic))
        End Sub

        <Fact>
        Public Sub NullIDynamicMetaObjectProvider()
            Dim expression = "o"

            Dim type = New DkmClrType(CType(GetType(IDynamicMetaObjectProvider), TypeImpl))
            Dim value = CreateDkmClrValue(Nothing, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "Nothing", "System.Dynamic.IDynamicMetaObjectProvider", expression))

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic))
        End Sub

        <Fact>
        Public Sub NullDynamicObject()
            Dim expression = "o"

            Dim type = New DkmClrType(CType(GetType(ExpandoObject), TypeImpl))
            Dim value = CreateDkmClrValue(Nothing, type)

            Dim result = FormatResult(expression, value)
            Verify(result,
                EvalResult(expression, "Nothing", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable))
            Verify(GetChildren(result),
                EvalResult(Resources.SharedMembers, Nothing, "", "System.Dynamic.ExpandoObject", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class))

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic))
        End Sub

        <Fact>
        Public Sub DynamicTypeError()
            Dim expression = "o"
            Dim obj = New ExpandoObject()

            ' Verify that things *work* in this scenario if there was no error in member access.
            Dim value = CreateDkmClrValue(obj)
            Dim fullName = expression + ", dynamic"
            Dim result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))

            ' Verify no Dynamic View if member access is changed to result in an error.
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore())
            value = CreateErrorValue(runtime.GetType(obj.GetType()), "Function evaluation timed out")
            result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic))
        End Sub

        <Fact>
        Public Sub DynamicMetaObjectProviderDebugViewItemsError()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            o.Answer = 42

            Dim runtime As DkmClrRuntimeInstance = Nothing
            runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(),
                getMemberValue:=Function(v, m) If(m = "Items", CreateErrorValue(runtime.GetType(GetType(Array)), "Function evaluation timed out"), Nothing))
            Dim type = New DkmClrType(runtime, CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim fullName = expression + ", dynamic"
            Dim result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            Verify(GetChildren(result),
                EvalFailedResult(Resources.ErrorName, "Function evaluation timed out"))
        End Sub

        <Fact>
        Public Sub DynamicMetaObjectProviderDebugViewItemsException()
            Dim expression = "o"
            Dim fullName = expression + ", dynamic"
            Dim o As Object = New ExpandoObject()
            o.Answer = 42

            Dim runtime As DkmClrRuntimeInstance = Nothing
            Dim getExceptionValue = Function() CreateDkmClrValue(New NotImplementedException(), evalFlags:=DkmEvaluationResultFlags.ExceptionThrown)
            runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(), getMemberValue:=Function(v, m) If(m = "Items", getExceptionValue(), Nothing))
            Dim type = New DkmClrType(runtime, CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            Dim members = GetChildren(result)
            Assert.Equal(32, members.Length)
            Verify(members(1),
                EvalResult("HResult", "-2147467263", "Integer", Nothing, category:=DkmEvaluationResultCategory.Property, access:=DkmEvaluationResultAccessType.Public))

            getExceptionValue = Function() CreateDkmClrValue(New NotImplementedException())
            result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            members = GetChildren(result)
            Assert.Equal(32, members.Length)
            Verify(members(1),
                EvalResult("HResult", "-2147467263", "Integer", "DirectCast(New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items, System.Exception).HResult", category:=DkmEvaluationResultCategory.Property, access:=DkmEvaluationResultAccessType.Public))
        End Sub

        <Fact>
        Public Sub DynamicFormatSpecifier()
            Dim expression = "o"
            Dim o As Object = New ExpandoObject()
            o.Answer = 42

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim fullName = expression + ", dynamic"
            Dim result = FormatResult(expression, fullName, value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
            Verify(GetChildren(result),
                EvalResult("Answer", "42", "System.Int32", "New Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items(0)", DkmEvaluationResultFlags.ReadOnly))
        End Sub

        <Fact>
        Public Sub DynamicFormatSpecifierError()
            Dim expression = "o"
            Dim o = New Object()

            Dim type = New DkmClrType(CType(o.GetType(), TypeImpl))
            Dim value = CreateDkmClrValue(o, type)

            Dim result = FormatResult(expression, expression + ",dynamic", value, inspectionContext:=CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView))
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic))
        End Sub

    End Class

End Namespace
