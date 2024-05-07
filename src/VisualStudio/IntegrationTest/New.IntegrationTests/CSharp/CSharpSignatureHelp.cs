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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.SignatureHelp)]
    public class CSharpSignatureHelp : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpSignatureHelp()
            : base(nameof(CSharpSignatureHelp))
        {

        }

        [IdeFact]
        public async Task MethodSignatureHelp()
        {
            await SetUpEditorAsync(@"
using System;
class C
{
    void M()
    {
       GenericMethod<string, int>(null, 1);       
       $$
    }
    C Method(int i) { return null; }
    
    /// <summary>
    /// Hello World 2.0!
    /// </summary>
    /// <param name=""i"">an integer, preferably 42.</param>
    /// <param name=""i2"">an integer, anything you like.</param>
    /// <returns>returns an object of type C</returns>
    C Method(int i, int i2) { return null; }

    /// <summary>
    /// Hello Generic World!
    /// </summary>
    /// <typeparam name=""T1"">Type Param 1</typeparam>
    /// <param name=""i"">Param 1 of type T1</param>
    /// <returns>Null</returns>
    C GenericMethod<T1>(T1 i) { return null; }
    C GenericMethod<T1, T2>(T1 i, T2 i2) { return null; }

    /// <summary>
    /// Complex Method Params
    /// </summary>
    /// <param name=""strings"">Jagged MultiDimensional Array</param>
    /// <param name=""outArr"">Out Array</param>
    /// <param name=""d"">Dynamic and Params param</param>
    /// <returns>Null</returns>
    void OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d) {outArr = null;}
}", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("var m = Method(1,", HangMitigatingCancellationToken);
            await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);
            var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
            Assert.Equal("C C.Method(int i, int i2)\r\nHello World 2.0!", signature.Content);
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

            await TestServices.Input.SendAsync([VirtualKeyCode.HOME, (VirtualKeyCode.END, VirtualKeyCode.SHIFT), VirtualKeyCode.DELETE], HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("var op = OutAndParam(", HangMitigatingCancellationToken);

            signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
            Assert.Equal("void C.OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d)\r\nComplex Method Params", signature.Content);
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

        [IdeFact]
        public async Task GenericMethodSignatureHelp1()
        {
            await SetUpEditorAsync(@"
using System;
class C
{
    void M()
    {
       GenericMethod<$$string, int>(null, 1);       
    }
    C Method(int i) { return null; }
    
    /// <summary>
    /// Hello World 2.0!
    /// </summary>
    /// <param name=""i"">an integer, preferably 42.</param>
    /// <param name=""i2"">an integer, anything you like.</param>
    /// <returns>returns an object of type C</returns>
    C Method(int i, int i2) { return null; }

    /// <summary>
    /// Hello Generic World!
    /// </summary>
    /// <typeparam name=""T1"">Type Param 1</typeparam>
    /// <param name=""i"">Param 1 of type T1</param>
    /// <returns>Null</returns>
    C GenericMethod<T1>(T1 i) { return null; }
    C GenericMethod<T1, T2>(T1 i, T2 i2) { return null; }

    /// <summary>
    /// Complex Method Params
    /// </summary>
    /// <param name=""strings"">Jagged MultiDimensional Array</param>
    /// <param name=""outArr"">Out Array</param>
    /// <param name=""d"">Dynamic and Params param</param>
    /// <returns>Null</returns>
    void OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d) {outArr = null;}
}", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);
            var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
            Assert.Equal("C C.GenericMethod<T1, T2>(T1 i, T2 i2)", signature.Content);
            Assert.Equal("T1", signature.CurrentParameter.Name);
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
        public async Task GenericMethodSignatureHelp2()
        {
            await SetUpEditorAsync(@"
using System;
class C
{
    void M()
    {
       GenericMethod<string, int>($$null, 1);       
    }
    C Method(int i) { return null; }
    
    /// <summary>
    /// Hello World 2.0!
    /// </summary>
    /// <param name=""i"">an integer, preferably 42.</param>
    /// <param name=""i2"">an integer, anything you like.</param>
    /// <returns>returns an object of type C</returns>
    C Method(int i, int i2) { return null; }

    /// <summary>
    /// Hello Generic World!
    /// </summary>
    /// <typeparam name=""T1"">Type Param 1</typeparam>
    /// <param name=""i"">Param 1 of type T1</param>
    /// <returns>Null</returns>
    C GenericMethod<T1>(T1 i) { return null; }
    C GenericMethod<T1, T2>(T1 i, T2 i2) { return null; }

    /// <summary>
    /// Complex Method Params
    /// </summary>
    /// <param name=""strings"">Jagged MultiDimensional Array</param>
    /// <param name=""outArr"">Out Array</param>
    /// <param name=""d"">Dynamic and Params param</param>
    /// <returns>Null</returns>
    void OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d) {outArr = null;}
}", HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);
            var signature = await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken);
            Assert.Equal("C C.GenericMethod<string, int>(string i, int i2)", signature.Content);
            Assert.Equal("i", signature.CurrentParameter.Name);
            Assert.Equal("", signature.CurrentParameter.Documentation);
            Assert.Collection(
                signature.Parameters,
                [
                    parameter =>
                    {
                        Assert.Equal("i", parameter.Name);
                        Assert.Equal("", parameter.Documentation);
                    }
            ,
                    parameter =>
                    {
                        Assert.Equal("i2", parameter.Name);
                        Assert.Equal("", parameter.Documentation);
                    }
            ,
                ]);
        }

        [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/42484")]
        public async Task ExplicitSignatureHelpDismissesCompletion()
        {
            await SetUpEditorAsync(@"
class C
{
    void M()
    {
       Test$$
    }

    void Test() { }
    void Test(int x) { }
    void Test(int x, int y) { }
    void Test(int x, int y, int z) { }    
}", HangMitigatingCancellationToken);

            await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("(", HangMitigatingCancellationToken);

            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
            Assert.True(await TestServices.Editor.IsSignatureHelpActiveAsync(HangMitigatingCancellationToken));

            await TestServices.Editor.InvokeSignatureHelpAsync(HangMitigatingCancellationToken);

            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
            Assert.True(await TestServices.Editor.IsSignatureHelpActiveAsync(HangMitigatingCancellationToken));

            Assert.Equal("void C.Test()", (await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken)).Content);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);

            Assert.Equal("void C.Test(int x)", (await TestServices.Editor.GetCurrentSignatureAsync(HangMitigatingCancellationToken)).Content);
        }
    }
}
