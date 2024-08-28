// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

    public BasicSignatureHelp()
        : base(nameof(BasicSignatureHelp))
    {
    }

    [IdeFact]
    public async Task MethodSignatureHelp()
    {
        await SetUpEditorAsync(Baseline, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Dim m=Method(1,", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C.Method(i As Integer, i2 As Integer) As C\r\nHello World 2.0!", signature.Content);
        Assert.Equal("i2", signature.CurrentParameter.Name);
        Assert.Equal("an integer, anything you like.", signature.CurrentParameter.Documentation);
        Assert.Collection(
            signature.Parameters,
            [
                parameter =>
                {
                    Assert.Equal("i", parameter.Name);
                    Assert.Equal("an integer, preferably 42.", parameter.Documentation);
                }
        ,
                parameter =>
                {
                    Assert.Equal("i2", parameter.Name);
                    Assert.Equal("an integer, anything you like.", parameter.Documentation);
                }
        ,
            ]);
    }

    [IdeFact]
    public async Task GenericMethodSignatureHelp1()
    {
        await SetUpEditorAsync(Baseline, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Dim gm = GenericMethod", HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("(", HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C.GenericMethod(Of T1)(i As T1) As C\r\nHello Generic World!", signature.Content);
        Assert.Equal("i", signature.CurrentParameter.Name);
        Assert.Equal("Param 1 of type T1", signature.CurrentParameter.Documentation);
        Assert.Collection(
            signature.Parameters,
            [
                parameter =>
                {
                    Assert.Equal("i", parameter.Name);
                    Assert.Equal("Param 1 of type T1", parameter.Documentation);
                }
        ,
            ]);
    }

    [IdeFact]
    public async Task GenericMethodSignatureHelp2()
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
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("GenericMethod", HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("(Of ", HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C(Of T, R).GenericMethod(Of T1)(i As T1)\r\nGeneric Method with 1 Type Param", signature.Content);
        Assert.Equal("T1", signature.CurrentParameter.Name);
        Assert.Equal("Type Parameter", signature.CurrentParameter.Documentation);
        Assert.Collection(
            signature.Parameters,
            [
                parameter =>
                {
                    Assert.Equal("T1", parameter.Name);
                    Assert.Equal("Type Parameter", parameter.Documentation);
                }
        ,
            ]);
    }

    [IdeFact]
    public async Task GenericMethodSignatureHelp_InvokeSighelp()
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
End Class", HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C.GenericMethod(Of T1, T2)(i As T1, i2 As T2) As C", signature.Content);
        Assert.Equal("T2", signature.CurrentParameter.Name);
        Assert.Equal("", signature.CurrentParameter.Documentation);
        Assert.Collection(
            signature.Parameters,
            [
                parameter =>
                {
                    Assert.Equal("T1", parameter.Name);
                    Assert.Equal("", parameter.Documentation);
                }
        ,
                parameter =>
                {
                    Assert.Equal("T2", parameter.Name);
                    Assert.Equal("", parameter.Documentation);
                }
        ,
            ]);
    }

    [IdeFact]
    public async Task VerifyActiveParameterChanges()
    {
        await SetUpEditorAsync(@"
Module M
    Sub Method(a As Integer, b As Integer)
        $$
    End Sub
End Module", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Method(", HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("M.Method(a As Integer, b As Integer)", signature.Content);
        Assert.Equal("a", signature.CurrentParameter.Name);
        Assert.Equal("", signature.CurrentParameter.Documentation);
        await TestServices.Input.SendAsync("1, ", HangMitigatingCancellationToken);
        signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("b", signature.CurrentParameter.Name);
        Assert.Equal("", signature.CurrentParameter.Documentation);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?id=741415&fullScreen=true&_a=edit")]
    [IdeFact]
    public async Task HandleBufferTextChangesDuringComputation()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Goo()
    End Sub
    Sub Test()
        $$
    End Sub
End Class", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Goo(", HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C.Goo()", signature.Content);

        await TestServices.Editor.SetTextAsync(@"
Class C
    'Marker", HangMitigatingCancellationToken);

        Assert.False(await TestServices.Editor.IsSignatureHelpActiveAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task JaggedMultidimensionalArray()
    {
        await SetUpEditorAsync(Baseline, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Dim op = OutAndParam(", HangMitigatingCancellationToken);
        var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
        Assert.Equal("C.OutAndParam(ByRef strings As String()(,), ByRef outArr As String(), ParamArray d As Object)\r\nComplex Method Params", signature.Content);
        Assert.Equal("strings", signature.CurrentParameter.Name);
        Assert.Equal("Jagged MultiDimensional Array", signature.CurrentParameter.Documentation);
        Assert.Collection(
            signature.Parameters,
            [
                parameter =>
                {
                    Assert.Equal("strings", parameter.Name);
                    Assert.Equal("Jagged MultiDimensional Array", parameter.Documentation);
                }
        ,
                parameter =>
                {
                    Assert.Equal("outArr", parameter.Name);
                    Assert.Equal("Out Array", parameter.Documentation);
                }
        ,
                parameter =>
                {
                    Assert.Equal("d", parameter.Name);
                    Assert.Equal("Dynamic and Params param", parameter.Documentation);
                }
        ,
            ]);
    }
}
