// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExpressionEvaluator : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(BasicBuild));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);

            await VisualStudio.Editor.SetTextAsync(@"Imports System

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

        [IdeFact]
        public async Task ValidateLocalsWindowAsync()
        {
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            await VisualStudio.LocalsWindow.Verify.CheckCountAsync(20);
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("mySByte", "SByte", "64");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myShort", "Short", "16384");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myInt", "Integer", "1073741824");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myLong", "Long", "4611686018427387904");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myByte", "Byte", "128");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myUShort", "UShort", "32768");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myUInt", "UInteger", "2147483648");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myULong", "ULong", "9223372036854775808");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myFloat", "Single", "1.70141173E+38");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myDouble", "Double", "8.9884656743115785E+307");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myDecimal", "Decimal", "39614081257132168796771975168");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myChar", "Char", "\"A\"c");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myBool", "Boolean", "True");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myObject", "Object", "Nothing");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myString", "String", "\"\"");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myValueType", "System.ValueType {Short}", "16384");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myEnum", "System.Enum", "Nothing");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myArray", "System.Array", "Nothing");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myDelegate", "System.Delegate", "Nothing");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("myMulticastDelegate", "System.MulticastDelegate", "Nothing");
        }

        [IdeFact]
        public async Task EvaluatePrimitiveValuesAsync()
        {
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await VisualStudio.Debugger.CheckExpressionAsync("myByte", "Byte", "128");
            await VisualStudio.Debugger.CheckExpressionAsync("myFloat", "Single", "1.70141173E+38");
            await VisualStudio.Debugger.CheckExpressionAsync("myChar", "Char", "\"A\"c");
            await VisualStudio.Debugger.CheckExpressionAsync("myObject", "Object", "Nothing");
            await VisualStudio.Debugger.CheckExpressionAsync("myString", "String", "\"\"");
        }

        [IdeFact]
        public async Task EvaluateLambdaExpressionsAsync()
        {
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await VisualStudio.Debugger.CheckExpressionAsync("(Function(val As Integer)(val+val))(1)", "Integer", "2");
        }

        [IdeFact]
        public async Task EvaluateInvalidExpressionsAsync()
        {
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.Debugger.CheckExpressionAsync("myNonsense", "", "error BC30451: 'myNonsense' is not declared. It may be inaccessible due to its protection level.");
        }

        [IdeFact]
        public async Task StateMachineTypeParametersAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
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
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync("Type variables", "", "");
            await VisualStudio.LocalsWindow.Verify.CheckEntryAsync( new string[] { "Type variables", "T" }, "String", "String");

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await VisualStudio.Debugger.CheckExpressionAsync("GetType(T) = GetType(String)", "Boolean", "True");
        }
    }
}
