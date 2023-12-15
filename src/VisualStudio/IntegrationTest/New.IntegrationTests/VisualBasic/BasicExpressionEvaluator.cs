// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicExpressionEvaluator : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicExpressionEvaluator()
            : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(BasicExpressionEvaluator), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic, HangMitigatingCancellationToken);

            await TestServices.Editor.SetTextAsync(@"Imports System

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

End Module", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ValidateLocalsWindow()
        {
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);

            Assert.Equal(20, await TestServices.LocalsWindow.GetCountAsync(HangMitigatingCancellationToken));
            Assert.Equal(("SByte", "64"), await TestServices.LocalsWindow.GetEntryAsync(["mySByte"], HangMitigatingCancellationToken));
            Assert.Equal(("Short", "16384"), await TestServices.LocalsWindow.GetEntryAsync(["myShort"], HangMitigatingCancellationToken));
            Assert.Equal(("Integer", "1073741824"), await TestServices.LocalsWindow.GetEntryAsync(["myInt"], HangMitigatingCancellationToken));
            Assert.Equal(("Long", "4611686018427387904"), await TestServices.LocalsWindow.GetEntryAsync(["myLong"], HangMitigatingCancellationToken));
            Assert.Equal(("Byte", "128"), await TestServices.LocalsWindow.GetEntryAsync(["myByte"], HangMitigatingCancellationToken));
            Assert.Equal(("UShort", "32768"), await TestServices.LocalsWindow.GetEntryAsync(["myUShort"], HangMitigatingCancellationToken));
            Assert.Equal(("UInteger", "2147483648"), await TestServices.LocalsWindow.GetEntryAsync(["myUInt"], HangMitigatingCancellationToken));
            Assert.Equal(("ULong", "9223372036854775808"), await TestServices.LocalsWindow.GetEntryAsync(["myULong"], HangMitigatingCancellationToken));
            Assert.Equal(("Single", "1.70141173E+38"), await TestServices.LocalsWindow.GetEntryAsync(["myFloat"], HangMitigatingCancellationToken));
            Assert.Equal(("Double", "8.9884656743115785E+307"), await TestServices.LocalsWindow.GetEntryAsync(["myDouble"], HangMitigatingCancellationToken));
            Assert.Equal(("Decimal", "39614081257132168796771975168"), await TestServices.LocalsWindow.GetEntryAsync(["myDecimal"], HangMitigatingCancellationToken));
            Assert.Equal(("Char", "\"A\"c"), await TestServices.LocalsWindow.GetEntryAsync(["myChar"], HangMitigatingCancellationToken));
            Assert.Equal(("Boolean", "True"), await TestServices.LocalsWindow.GetEntryAsync(["myBool"], HangMitigatingCancellationToken));
            Assert.Equal(("Object", "Nothing"), await TestServices.LocalsWindow.GetEntryAsync(["myObject"], HangMitigatingCancellationToken));
            Assert.Equal(("String", "\"\""), await TestServices.LocalsWindow.GetEntryAsync(["myString"], HangMitigatingCancellationToken));
            Assert.Equal(("System.ValueType {Short}", "16384"), await TestServices.LocalsWindow.GetEntryAsync(["myValueType"], HangMitigatingCancellationToken));
            Assert.Equal(("System.Enum", "Nothing"), await TestServices.LocalsWindow.GetEntryAsync(["myEnum"], HangMitigatingCancellationToken));
            Assert.Equal(("System.Array", "Nothing"), await TestServices.LocalsWindow.GetEntryAsync(["myArray"], HangMitigatingCancellationToken));
            Assert.Equal(("System.Delegate", "Nothing"), await TestServices.LocalsWindow.GetEntryAsync(["myDelegate"], HangMitigatingCancellationToken));
            Assert.Equal(("System.MulticastDelegate", "Nothing"), await TestServices.LocalsWindow.GetEntryAsync(["myMulticastDelegate"], HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task EvaluatePrimitiveValues()
        {
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await TestServices.Debugger.CheckExpressionAsync("myByte", "Byte", "128", HangMitigatingCancellationToken);
            await TestServices.Debugger.CheckExpressionAsync("myFloat", "Single", "1.70141173E+38", HangMitigatingCancellationToken);
            await TestServices.Debugger.CheckExpressionAsync("myChar", "Char", "\"A\"c", HangMitigatingCancellationToken);
            await TestServices.Debugger.CheckExpressionAsync("myObject", "Object", "Nothing", HangMitigatingCancellationToken);
            await TestServices.Debugger.CheckExpressionAsync("myString", "String", "\"\"", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task EvaluateLambdaExpressions()
        {
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await TestServices.Debugger.CheckExpressionAsync("(Function(val As Integer)(val+val))(1)", "Integer", "2", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task EvaluateInvalidExpressions()
        {
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
            await TestServices.Debugger.CheckExpressionAsync("myNonsense", "", "error BC30451: 'myNonsense' is not declared. It may be inaccessible due to its protection level.", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task StateMachineTypeParameters()
        {
            await TestServices.Editor.SetTextAsync(@"
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
", HangMitigatingCancellationToken);
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
            Assert.Equal(("", ""), await TestServices.LocalsWindow.GetEntryAsync(["Type variables"], HangMitigatingCancellationToken));
            Assert.Equal(("String", "String"), await TestServices.LocalsWindow.GetEntryAsync(["Type variables", "T"], HangMitigatingCancellationToken));

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            await TestServices.Debugger.CheckExpressionAsync("GetType(T) = GetType(String)", "Boolean", "True", HangMitigatingCancellationToken);
        }
    }
}
