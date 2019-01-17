// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicExpressionEvaluator : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicExpressionEvaluator() : base(nameof(BasicExpressionEvaluator))
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);

            VisualStudioInstance.Editor.SetText(@"Imports System

Module Module1

    Sub Main()
        Dim mySByte As SByte = SByte.MaxValue / 2
        Dim myShort As Short = Short.MaxValue / 2
        Dim myInt As Integer = Integer.MaxValue / 2
        Dim myLong As Long = Long.MaxValue / 2

        Dim myByte As Byte = Byte.MaxValue / 2
        Dim myUShort As UShort = UShort.MaxValue / 2
        Dim myUInt As UInteger = UInteger.MaxValue / 2
        Dim myULong As ULong = ULong.MaxValue / 2

        Dim myFloat As Single = Single.MaxValue / 2
        Dim myDouble As Double = Double.MaxValue / 2
        Dim myDecimal As Decimal = Decimal.MaxValue / 2

        Dim myChar As Char = ""A""c
        Dim myBool As Boolean = True

        Dim myObject As Object = Nothing
        Dim myString As String = String.Empty

        Dim myValueType As System.ValueType = myShort
        Dim myEnum As System.Enum = Nothing
        Dim myArray As System.Array = Nothing
        Dim myDelegate As System.Delegate = Nothing
        Dim myMulticastDelegate As System.MulticastDelegate = Nothing

        System.Diagnostics.Debugger.Break()
    End Sub

End Module");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/20979")]
        public void ValidateLocalsWindow()
        {
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            VisualStudioInstance.LocalsWindow.Verify.CheckCount(20);
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("mySByte", "SByte", "64");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myShort", "Short", "16384");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myInt", "Integer", "1073741824");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myLong", "Long", "4611686018427387904");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myByte", "Byte", "128");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myUShort", "UShort", "32768");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myUInt", "UInteger", "2147483648");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myULong", "ULong", "9223372036854775808");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myFloat", "Single", "1.70141173E+38");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myDouble", "Double", "8.9884656743115785E+307");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myDecimal", "Decimal", "39614081257132168796771975168");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myChar", "Char", "\"A\"c");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myBool", "Boolean", "True");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myObject", "Object", "Nothing");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myString", "String", "\"\"");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myValueType", "System.ValueType {Short}", "16384");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myEnum", "System.Enum", "Nothing");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myArray", "System.Array", "Nothing");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myDelegate", "System.Delegate", "Nothing");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("myMulticastDelegate", "System.MulticastDelegate", "Nothing");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/20979")]
        public void EvaluatePrimitiveValues()
        {
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudioInstance.Debugger.CheckExpression("myByte", "Byte", "128");
            VisualStudioInstance.Debugger.CheckExpression("myFloat", "Single", "1.70141173E+38");
            VisualStudioInstance.Debugger.CheckExpression("myChar", "Char", "\"A\"c");
            VisualStudioInstance.Debugger.CheckExpression("myObject", "Object", "Nothing");
            VisualStudioInstance.Debugger.CheckExpression("myString", "String", "\"\"");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/19526")]
        public void EvaluateLambdaExpressions()
        {
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudioInstance.Debugger.CheckExpression("(Function(val)(val+val))(1)", "Integer", "2");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/20979")]
        public void EvaluateInvalidExpressions()
        {
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.Debugger.CheckExpression("myNonsense", "", "error BC30451: 'myNonsense' is not declared. It may be inaccessible due to its protection level.");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/20979")]
        public void StateMachineTypeParameters()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        For Each arg In I({ ""a"", ""b""})
            Console.WriteLine(arg)
        Next
    End Sub

    Iterator Function I(Of T)(tt As T()) As IEnumerable(Of T)
        For Each item In tt
            Stop
            Yield item
        Next
    End Function

End Module
");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry("Type variables", "", "");
            VisualStudioInstance.LocalsWindow.Verify.CheckEntry(new string[] { "Type variables", "T" }, "String", "String");

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudioInstance.Debugger.CheckExpression("GetType(T) = GetType(String)", "Boolean", "True");
        }
    }
}
