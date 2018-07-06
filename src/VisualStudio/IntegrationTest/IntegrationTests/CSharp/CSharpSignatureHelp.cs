// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSignatureHelp : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpSignatureHelp()
            : base(nameof(CSharpSignatureHelp))
        {

        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task MethodSignatureHelpAsync()
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
}");

            await VisualStudio.SendKeys.SendAsync("var m = Method(1,");
            await VisualStudio.Editor.InvokeSignatureHelpAsync();
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C C.Method(int i, int i2)\r\nHello World 2.0!");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("i2", "an integer, anything you like.");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));

            await VisualStudio.Editor.SendKeysAsync(new object[] { VirtualKey.Home, new KeyPress(VirtualKey.End, ShiftState.Shift), VirtualKey.Delete });
            await VisualStudio.Editor.SendKeysAsync("var op = OutAndParam(");

            await VisualStudio.Editor.Verify.CurrentSignatureAsync("void C.OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d)\r\nComplex Method Params");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("strings", "Jagged MultiDimensional Array");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericMethodSignatureHelp1Async()
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
}");

            await VisualStudio.Editor.InvokeSignatureHelpAsync();
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C C.GenericMethod<T1, T2>(T1 i, T2 i2)");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("T1", "");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("T1", ""),
                ("T2", ""));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public async Task GenericMethodSignatureHelp2Async()
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
}");

            await VisualStudio.Editor.InvokeSignatureHelpAsync();
            await VisualStudio.Editor.Verify.CurrentSignatureAsync("C C.GenericMethod<string, int>(string i, int i2)");
            await VisualStudio.Editor.Verify.CurrentParameterAsync("i", "");
            await VisualStudio.Editor.Verify.ParametersAsync(
                ("i", ""),
                ("i2", ""));
        }
    }
}
