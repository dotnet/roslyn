// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicExpressionEvaluator : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicExpressionEvaluator(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);

            VisualStudio.Editor.SetText(@"Imports System

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

        [WpfFact()]
        public void ValidateLocalsWindow()
        {
            VisualStudio.Debugger.Go(waitForBreakMode: true);

            VisualStudio.LocalsWindow.Verify.CheckCount(20);
            VisualStudio.LocalsWindow.Verify.CheckEntry("mySByte", "SByte", "64");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myShort", "Short", "16384");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myInt", "Integer", "1073741824");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myLong", "Long", "4611686018427387904");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myByte", "Byte", "128");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myUShort", "UShort", "32768");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myUInt", "UInteger", "2147483648");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myULong", "ULong", "9223372036854775808");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myFloat", "Single", "1.70141173E+38");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myDouble", "Double", "8.9884656743115785E+307");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myDecimal", "Decimal", "39614081257132168796771975168");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myChar", "Char", "\"A\"c");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myBool", "Boolean", "True");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myObject", "Object", "Nothing");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myString", "String", "\"\"");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myValueType", "System.ValueType {Short}", "16384");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myEnum", "System.Enum", "Nothing");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myArray", "System.Array", "Nothing");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myDelegate", "System.Delegate", "Nothing");
            VisualStudio.LocalsWindow.Verify.CheckEntry("myMulticastDelegate", "System.MulticastDelegate", "Nothing");
        }

        [WpfFact()]
        public void EvaluatePrimitiveValues()
        {
            VisualStudio.Debugger.Go(waitForBreakMode: true);

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudio.Debugger.CheckExpression("myByte", "Byte", "128");
            VisualStudio.Debugger.CheckExpression("myFloat", "Single", "1.70141173E+38");
            VisualStudio.Debugger.CheckExpression("myChar", "Char", "\"A\"c");
            VisualStudio.Debugger.CheckExpression("myObject", "Object", "Nothing");
            VisualStudio.Debugger.CheckExpression("myString", "String", "\"\"");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19526")]
        public void EvaluateLambdaExpressions()
        {
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudio.Debugger.CheckExpression("(Function(val)(val+val))(1)", "Integer", "2");
        }

        [WpfFact()]
        public void EvaluateInvalidExpressions()
        {
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.Debugger.CheckExpression("myNonsense", "", "error BC30451: 'myNonsense' is not declared. It may be inaccessible due to its protection level.");
        }

        [WpfFact()]
        public void StateMachineTypeParameters()
        {
            VisualStudio.Editor.SetText(@"
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
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.LocalsWindow.Verify.CheckEntry("Type variables", "", "");
            VisualStudio.LocalsWindow.Verify.CheckEntry(new string[] { "Type variables", "T" }, "String", "String");

            // It is better to use the Immediate Window but DTE does not provide an access to it.
            VisualStudio.Debugger.CheckExpression("GetType(T) = GetType(String)", "Boolean", "True");
        }
    }
}
