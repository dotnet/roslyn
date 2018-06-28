// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicSignatureHelp : AbstractIdeEditorTest
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

        public BasicSignatureHelp()
            : base(nameof(BasicSignatureHelp))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task MethodSignatureHelpAsync()
        {
            await SetUpEditorAsync(Baseline);

            await VisualStudio.Editor.SendKeysAsync("Dim m=Method(1,");
            await VisualStudio.Editor.InvokeSignatureHelpAsync();
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C.Method(i As Integer, i2 As Integer) As C\r\nHello World 2.0!");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("i2", "an integer, anything you like.");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericMethodSignatureHelp1Async()
        {
            await SetUpEditorAsync(Baseline);

            await VisualStudio.Editor.SendKeysAsync("Dim gm = GenericMethod");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Escape);
            await VisualStudio.Editor.SendKeysAsync("(");
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C.GenericMethod(Of T1)(i As T1) As C\r\nHello Generic World!");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("i", "Param 1 of type T1");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("i", "Param 1 of type T1"));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericMethodSignatureHelp2Async()
        {
            await SetUpEditorAsync(@"
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

            await VisualStudio.Editor.SendKeysAsync("GenericMethod");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Escape);
            await VisualStudio.Editor.SendKeysAsync("(Of ");
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C(Of T, R).GenericMethod(Of T1)(i As T1)\r\nGeneric Method with 1 Type Param");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("T1", "Type Parameter");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("T1", "Type Parameter"));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericMethodSignatureHelp_InvokeSighelpAsync()
        {
            await SetUpEditorAsync(@"
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

            await VisualStudio.Editor.InvokeSignatureHelpAsync();
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C.GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("T2", "");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("T1", ""),
                ("T2", ""));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task VerifyActiveParameterChangesAsync()
        {
            await SetUpEditorAsync(@"
Module M
    Sub Method(a As Integer, b As Integer)
        $$
    End Sub
End Module");

            await VisualStudio.Editor.SendKeysAsync("Method(");
            await Task.Delay(1000);
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("M.Method(a As Integer, b As Integer)");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("a", "");
            await VisualStudio.Editor.SendKeysAsync("1, ");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("b", "");
        }

        [WorkItem(741415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?id=741415&fullScreen=true&_a=edit")]
        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task HandleBufferTextChangesDuringComputationAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class");

            await VisualStudio.Editor.SendKeysAsync("Goo(");
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C.Goo()");

            await VisualStudio.Editor.SetTextAsync(@"
Class C
    'Marker");

            Assert.False(await VisualStudio.Editor.IsSignatureHelpActiveAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task JaggedMultidimensionalArrayAsync()
        {
            await SetUpEditorAsync(Baseline);

            await VisualStudio.Editor.SendKeysAsync("Dim op = OutAndParam(");
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C.OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)\r\nComplex Method Params");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("strings", "Jagged MultiDimensional Array");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }
    }
}
