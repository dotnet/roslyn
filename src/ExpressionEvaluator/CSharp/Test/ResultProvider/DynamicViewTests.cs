// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Dynamic;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DynamicViewTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void MultipleMembers()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            o.Philosophers = new object[] { "Pythagoras", "Lucretius", "Zeno" };
            o.WhatsForDinner = "Crab Cakes";
            o.NumForks = 2;

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            var dynamicView = GetChildren(result).Last();
            Verify(dynamicView,
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(dynamicView),
                EvalResult("NumForks", "2", "System.Int32", "new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items[0]", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Philosophers", "{object[3]}", "System.Object[]", "new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items[1]", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("WhatsForDinner", "\"Crab Cakes\"", "System.String", "new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items[2]", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void MultipleExpansions()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            o.Answer = 42;

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            // Dynamic View should appear after all other expansions.
            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(result),
                EvalResult("Class", "{System.Dynamic.ExpandoClass}", "System.Dynamic.ExpandoClass", "o.Class", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Internal),
                EvalResult("LockObject", "{object}", "object", "o.LockObject", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Internal),
                EvalResult("System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>.Count", "1", "int", "((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>)o).Count", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>.IsReadOnly", "false", "bool", "((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>)o).IsReadOnly", DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.IDictionary<string, object>.Keys", "Count = 1", "System.Collections.Generic.ICollection<string> {System.Dynamic.ExpandoObject.KeyCollection}", "((System.Collections.Generic.IDictionary<string, object>)o).Keys", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("System.Collections.Generic.IDictionary<string, object>.Values", "Count = 1", "System.Collections.Generic.ICollection<object> {System.Dynamic.ExpandoObject.ValueCollection}", "((System.Collections.Generic.IDictionary<string, object>)o).Values", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("_count", "1", "int", "o._count", category: DkmEvaluationResultCategory.Data, access: DkmEvaluationResultAccessType.Private),
                EvalResult("_data", "{System.Dynamic.ExpandoObject.ExpandoData}", "System.Dynamic.ExpandoObject.ExpandoData", "o._data", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Private),
                EvalResult("_propertyChanged", "null", "System.ComponentModel.PropertyChangedEventHandler", "o._propertyChanged", category: DkmEvaluationResultCategory.Data, access: DkmEvaluationResultAccessType.Private),
                EvalResult(Resources.StaticMembers, null, "", "System.Dynamic.ExpandoObject", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Method));
        }

        [Fact]
        public void ExceptionTypeMember()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            var exception = new NotImplementedException();
            o.Member = exception;

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            var dynamicView = GetChildren(result).Last();
            Verify(dynamicView,
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(dynamicView),
                EvalResult("Member", $"{{{exception.ToString()}}}", "System.NotImplementedException", "new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items[0]", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void DynamicTypeMember()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            o.Pi = Math.PI;
            o.OnAndOn = o;

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            var members = GetChildren(result);
            var fullNameOnAndOn = "o";
            var fullNamePi = "o";
            // Expand 3 levels...
            for (var i = 0; i < 3; i++)
            {
                var dynamicView = members.Last();
                Verify(dynamicView,
                    EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", $"{fullNameOnAndOn}, dynamic", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
                members = GetChildren(dynamicView);
                fullNamePi = $"new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView({fullNameOnAndOn}).Items[1]";
                fullNameOnAndOn = $"new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView({fullNameOnAndOn}).Items[0]";
                Verify(members,
                    EvalResult("OnAndOn", "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", fullNameOnAndOn, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Pi", "3.1415926535897931", "System.Double", fullNamePi, DkmEvaluationResultFlags.ReadOnly));
                members = GetChildren(members[0]);
            }
        }

        [Fact]
        public void NoMembers()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "{System.Dynamic.ExpandoObject}", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            var dynamicView = GetChildren(result).Last();
            Verify(dynamicView,
                EvalResult(Resources.DynamicView, Resources.DynamicViewValueWarning, "", "o, dynamic", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(dynamicView),
                EvalFailedResult(Resources.ErrorName, DynamicDebugViewEmptyMessage));
        }

        [Fact]
        public void NullComObject()
        {
            var comObjectTypeName = "System.__ComObject";
            var expression = $"({comObjectTypeName})null";

            var type = new DkmClrType((TypeImpl)typeof(object).Assembly.GetType(comObjectTypeName));
            var value = CreateDkmClrValue(null, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "null", comObjectTypeName, expression));

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic));
        }

        [Fact]
        public void NullIDynamicMetaObjectProvider()
        {
            var expression = "o";

            var type = new DkmClrType((TypeImpl)typeof(IDynamicMetaObjectProvider));
            var value = CreateDkmClrValue(null, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "null", "System.Dynamic.IDynamicMetaObjectProvider", expression));

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic));
        }

        [Fact]
        public void NullDynamicObject()
        {
            var expression = "o";

            var type = new DkmClrType((TypeImpl)typeof(ExpandoObject));
            var value = CreateDkmClrValue(null, type);

            var result = FormatResult(expression, value);
            Verify(result,
                EvalResult(expression, "null", "System.Dynamic.ExpandoObject", expression, DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(result),
                EvalResult(Resources.StaticMembers, null, "", "System.Dynamic.ExpandoObject", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));

            result = FormatResult(expression, expression + ",dynamic", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic));
        }

        [Fact]
        public void DynamicTypeError()
        {
            var expression = "o";
            var obj = new ExpandoObject();

            // Verify that things *work* in this scenario if there was no error in member access.
            var value = CreateDkmClrValue(obj);
            var fullName = expression + ", dynamic";
            var result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));

            // Verify no Dynamic View if member access is changed to result in an error.
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore());
            value = CreateErrorValue(runtime.GetType(obj.GetType()), "Function evaluation timed out");
            result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic));
        }

        [Fact]
        public void DynamicMetaObjectProviderDebugViewItemsError()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            o.Answer = 42;

            DkmClrRuntimeInstance runtime = null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(),
                getMemberValue: (_, m) => (m == "Items") ? CreateErrorValue(runtime.GetType(typeof(Array)), "Function evaluation timed out") : null);
            var type = new DkmClrType(runtime, (TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var fullName = expression + ", dynamic";
            var result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(result),
                EvalFailedResult(Resources.ErrorName, "Function evaluation timed out"));
        }

        [Fact]
        public void DynamicMetaObjectProviderDebugViewItemsException()
        {
            var expression = "o";
            var fullName = expression + ", dynamic";
            dynamic o = new ExpandoObject();
            o.Answer = 42;

            DkmClrRuntimeInstance runtime = null;
            Func<DkmClrValue> getExceptionValue = () => CreateDkmClrValue(new NotImplementedException(), evalFlags: DkmEvaluationResultFlags.ExceptionThrown);
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(), getMemberValue: (_, m) => (m == "Items") ? getExceptionValue() : null);
            var type = new DkmClrType(runtime, (TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            var members = GetChildren(result);
            Assert.Equal(32, members.Length);
            Verify(members[1],
                EvalResult("HResult", "-2147467263", "int", null, category: DkmEvaluationResultCategory.Property, access: DkmEvaluationResultAccessType.Public));

            getExceptionValue = () => CreateDkmClrValue(new NotImplementedException());
            result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            members = GetChildren(result);
            Assert.Equal(32, members.Length);
            Verify(members[1],
                EvalResult("HResult", "-2147467263", "int", "((System.Exception)new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items).HResult", category: DkmEvaluationResultCategory.Property, access: DkmEvaluationResultAccessType.Public));
        }

        [Fact]
        public void DynamicFormatSpecifier()
        {
            var expression = "o";
            dynamic o = new ExpandoObject();
            o.Answer = 42;

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue((object)o, type);

            var fullName = expression + ", dynamic";
            var result = FormatResult(expression, fullName, value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalResult(expression, Resources.DynamicViewValueWarning, "", fullName, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(result),
                EvalResult("Answer", "42", "System.Int32", "new Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView(o).Items[0]", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void DynamicFormatSpecifierError()
        {
            var expression = "o";
            var o = new Object();

            var type = new DkmClrType((TypeImpl)o.GetType());
            var value = CreateDkmClrValue(o, type);

            var result = FormatResult(expression, expression + ",dynamic", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.DynamicView));
            Verify(result,
                EvalFailedResult(expression, Resources.DynamicViewNotDynamic));
        }
    }
}
