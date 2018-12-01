// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class BasicSignatureHelp : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        private const string Baseline = @"
Class C
    Sub M()
        $$
    End Sub
    
    Function Method(i As Integer) As C
        Return Nothing
    End Function
    
    ''' <summary>
    ''' Hello World 2.0!
    ''' </summary>
    ''' <param name=""i"">an integer, preferably 42.</param>
    ''' <param name=""i2"">an integer, anything you like.</param>
    ''' <returns>returns an object of type C</returns>
    Function Method(i As Integer, i2 As Integer) As C
        Return Nothing
    End Function


    ''' <summary>
    ''' Hello Generic World!
    ''' </summary>
    ''' <typeparam name=""T1"">Type Param 1</typeparam>
    ''' <param name=""i"">Param 1 of type T1</param>
    ''' <returns>Null</returns>
    Function GenericMethod(Of T1)(i As T1) As C
        Return Nothing
    End Function


    Function GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C
        Return Nothing
    End Function


    ''' <summary>
    ''' Complex Method Params
    ''' </summary>
    ''' <param name=""strings"">Jagged MultiDimensional Array</param>
    ''' <param name=""outArr"">Out Array</param>
    ''' <param name=""d"">Dynamic and Params param</param>
    ''' <returns>Null</returns>
    Sub OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)
    End Sub
End Class
";

        public BasicSignatureHelp( )
            : base( nameof(BasicSignatureHelp))
        {
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void MethodSignatureHelp()
        {
            SetUpEditor(Baseline);

            VisualStudioInstance.Editor.SendKeys("Dim m=Method(1,");
            VisualStudioInstance.Editor.InvokeSignatureHelp();
            VisualStudioInstance.Editor.Verify.CurrentSignature("C.Method(i As Integer, i2 As Integer) As C\r\nHello World 2.0!");
            VisualStudioInstance.Editor.Verify.CurrentParameter("i2", "an integer, anything you like.");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp1()
        {
            SetUpEditor(Baseline);

            VisualStudioInstance.Editor.SendKeys("Dim gm = GenericMethod");
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Escape);
            VisualStudioInstance.Editor.SendKeys("(");
            VisualStudioInstance.Editor.Verify.CurrentSignature("C.GenericMethod(Of T1)(i As T1) As C\r\nHello Generic World!");
            VisualStudioInstance.Editor.Verify.CurrentParameter("i", "Param 1 of type T1");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("i", "Param 1 of type T1"));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp2()
        {
            SetUpEditor(@"
Imports System
Class C(Of T, R)
    Sub M()
        $$
    End Sub
    
    ''' <summary>
    ''' Generic Method with 1 Type Param
    ''' </summary>
    ''' <typeparam name=""T1"">Type Parameter</typeparam>
    ''' <param name=""i"">param i of type T1</param>
    Sub GenericMethod(Of T1)(i As T1)
    End Sub


    ''' <summary>
    ''' Generic Method with 2 Type Params
    ''' </summary>
    ''' <typeparam name=""T1"">Type Parameter 1</typeparam>
    ''' <typeparam name=""T2"">Type Parameter 2</typeparam>
    ''' <param name=""i"">param i of type T1</param>
    ''' <param name=""i2"">param i2 of type T2</param>
    ''' <returns>Null</returns>
    Function GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C(Of T, R)
        Return Nothing
    End Function
End Class");

            VisualStudioInstance.Editor.SendKeys("GenericMethod");
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Escape);
            VisualStudioInstance.Editor.SendKeys("(Of ");
            VisualStudioInstance.Editor.Verify.CurrentSignature("C(Of T, R).GenericMethod(Of T1)(i As T1)\r\nGeneric Method with 1 Type Param");
            VisualStudioInstance.Editor.Verify.CurrentParameter("T1", "Type Parameter");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("T1", "Type Parameter"));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp_InvokeSighelp()
        {
            SetUpEditor(@"
Imports System
Class C
    Sub M()
        GenericMethod(Of String, $$Integer)(Nothing, 1)
    End Sub
    
    Function GenericMethod(Of T1)(i As T1) As C
        Return Nothing
    End Function
    
    Function GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C
        Return Nothing
    End Function
End Class");

            VisualStudioInstance.Editor.InvokeSignatureHelp();
            VisualStudioInstance.Editor.Verify.CurrentSignature("C.GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C");
            VisualStudioInstance.Editor.Verify.CurrentParameter("T2", "");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("T1", ""),
                ("T2", ""));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void VerifyActiveParameterChanges()
        {
            SetUpEditor(@"
Module M
    Sub Method(a As Integer, b As Integer)
        $$
    End Sub
End Module");

            VisualStudioInstance.Editor.SendKeys("Method(");
            VisualStudioInstance.Editor.Verify.CurrentSignature("M.Method(a As Integer, b As Integer)");
            VisualStudioInstance.Editor.Verify.CurrentParameter("a", "");
            VisualStudioInstance.Editor.SendKeys("1, ");
            VisualStudioInstance.Editor.Verify.CurrentParameter("b", "");
        }

        [WorkItem(741415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?id=741415&fullScreen=true&_a=edit")]
        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void HandleBufferTextChangesDuringComputation()
        {
            SetUpEditor(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys("Goo(");
            VisualStudioInstance.Editor.Verify.CurrentSignature("C.Goo()");

            VisualStudioInstance.Editor.SetText(@"
Class C
    'Marker");

            Assert.IsFalse(VisualStudioInstance.Editor.IsSignatureHelpActive());
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void JaggedMultidimensionalArray()
        {
            SetUpEditor(Baseline);

            VisualStudioInstance.Editor.SendKeys("Dim op = OutAndParam(");
            VisualStudioInstance.Editor.Verify.CurrentSignature("C.OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)\r\nComplex Method Params");
            VisualStudioInstance.Editor.Verify.CurrentParameter("strings", "Jagged MultiDimensional Array");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }
    }
}
