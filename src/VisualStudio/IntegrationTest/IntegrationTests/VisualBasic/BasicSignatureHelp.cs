// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
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

        public BasicSignatureHelp(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicSignatureHelp))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void MethodSignatureHelp()
        {
            SetUpEditor(Baseline);

            VisualStudio.Editor.SendKeys("Dim m=Method(1,");
            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature("C.Method(i As Integer, i2 As Integer) As C\r\nHello World 2.0!");
            VisualStudio.Editor.Verify.CurrentParameter("i2", "an integer, anything you like.");
            VisualStudio.Editor.Verify.Parameters(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp1()
        {
            SetUpEditor(Baseline);

            VisualStudio.Editor.SendKeys("Dim gm = GenericMethod");
            VisualStudio.Editor.SendKeys(VirtualKey.Escape);
            VisualStudio.Editor.SendKeys("(");
            VisualStudio.Editor.Verify.CurrentSignature("C.GenericMethod(Of T1)(i As T1) As C\r\nHello Generic World!");
            VisualStudio.Editor.Verify.CurrentParameter("i", "Param 1 of type T1");
            VisualStudio.Editor.Verify.Parameters(
                ("i", "Param 1 of type T1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.Editor.SendKeys("GenericMethod");
            VisualStudio.Editor.SendKeys(VirtualKey.Escape);
            VisualStudio.Editor.SendKeys("(Of ");
            VisualStudio.Editor.Verify.CurrentSignature("C(Of T, R).GenericMethod(Of T1)(i As T1)\r\nGeneric Method with 1 Type Param");
            VisualStudio.Editor.Verify.CurrentParameter("T1", "Type Parameter");
            VisualStudio.Editor.Verify.Parameters(
                ("T1", "Type Parameter"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature("C.GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C");
            VisualStudio.Editor.Verify.CurrentParameter("T2", "");
            VisualStudio.Editor.Verify.Parameters(
                ("T1", ""),
                ("T2", ""));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void VerifyActiveParameterChanges()
        {
            SetUpEditor(@"
Module M
    Sub Method(a As Integer, b As Integer)
        $$
    End Sub
End Module");

            VisualStudio.Editor.SendKeys("Method(");
            VisualStudio.Editor.Verify.CurrentSignature("M.Method(a As Integer, b As Integer)");
            VisualStudio.Editor.Verify.CurrentParameter("a", "");
            VisualStudio.Editor.SendKeys("1, ");
            VisualStudio.Editor.Verify.CurrentParameter("b", "");
        }

        [WorkItem(741415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?id=741415&fullScreen=true&_a=edit")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.Editor.SendKeys("Goo(");
            VisualStudio.Editor.Verify.CurrentSignature("C.Goo()");

            VisualStudio.Editor.SetText(@"
Class C
    'Marker");

            Assert.False(VisualStudio.Editor.IsSignatureHelpActive());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void JaggedMultidimensionalArray()
        {
            SetUpEditor(Baseline);

            VisualStudio.Editor.SendKeys("Dim op = OutAndParam(");
            VisualStudio.Editor.Verify.CurrentSignature("C.OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)\r\nComplex Method Params");
            VisualStudio.Editor.Verify.CurrentParameter("strings", "Jagged MultiDimensional Array");
            VisualStudio.Editor.Verify.Parameters(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }
    }
}
