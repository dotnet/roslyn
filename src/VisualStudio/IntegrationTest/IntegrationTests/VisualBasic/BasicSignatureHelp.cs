// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Xunit;

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

        public BasicSignatureHelp(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicSignatureHelp))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void MethodSignatureHelp()
        {
            SetUpEditor(Baseline);

            this.SendKeys("Dim m=Method(1,");
            this.InvokeSignatureHelp();
            this.VerifyCurrentSignature("C.Method(i As Integer, i2 As Integer) As C\r\nHello World 2.0!");
            this.VerifyCurrentParameter("i2", "an integer, anything you like.");
            this.VerifyParameters(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp1()
        {
            SetUpEditor(Baseline);

            this.SendKeys("Dim gm = GenericMethod");
            this.SendKeys(VirtualKey.Escape);
            this.SendKeys("(");
            this.VerifyCurrentSignature("C.GenericMethod(Of T1)(i As T1) As C\r\nHello Generic World!");
            this.VerifyCurrentParameter("i", "Param 1 of type T1");
            this.VerifyParameters(
                ("i", "Param 1 of type T1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            this.SendKeys("GenericMethod");
            this.SendKeys(VirtualKey.Escape);
            this.SendKeys("(Of ");
            this.VerifyCurrentSignature("C(Of T, R).GenericMethod(Of T1)(i As T1)\r\nGeneric Method with 1 Type Param");
            this.VerifyCurrentParameter("T1", "Type Parameter");
            this.VerifyParameters(
                ("T1", "Type Parameter"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            this.InvokeSignatureHelp();
            this.VerifyCurrentSignature("C.GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C");
            this.VerifyCurrentParameter("T2", "");
            this.VerifyParameters(
                ("T1", ""),
                ("T2", ""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void VerifyActiveParameterChanges()
        {
            SetUpEditor(@"
Module M
    Sub Method(a As Integer, b As Integer)
        $$
    End Sub
End Module");

            this.SendKeys("Method(");
            this.VerifyCurrentSignature("M.Method(a As Integer, b As Integer)");
            this.VerifyCurrentParameter("a", "");
            this.SendKeys("1, ");
            this.VerifyCurrentParameter("b", "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void BufferTextReplacedWithSigHelpActiveWithLengthOfUpdatedTextLessThanPositionOfInvocationExpression()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            this.SendKeys("Foo(");
            this.VerifyCurrentSignature("C.Foo()");

            Editor.SetText(@"
Class C
    'Marker");

            Assert.False(Editor.IsSignatureHelpActive());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void JaggedMultidimensionalArray()
        {
            SetUpEditor(Baseline);

            this.SendKeys("Dim op = OutAndParam(");
            this.VerifyCurrentSignature("C.OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)\r\nComplex Method Params");
            this.VerifyCurrentParameter("strings", "Jagged MultiDimensional Array");
            this.VerifyParameters(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }
    }
}
