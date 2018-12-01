// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpSignatureHelp : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpSignatureHelp( )
            : base( nameof(CSharpSignatureHelp))
        {

        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void MethodSignatureHelp()
        {
            SetUpEditor(@"
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

            VisualStudioInstance.SendKeys.Send("var m = Method(1,");
            VisualStudioInstance.Editor.InvokeSignatureHelp();
            VisualStudioInstance.Editor.Verify.CurrentSignature("C C.Method(int i, int i2)\r\nHello World 2.0!");
            VisualStudioInstance.Editor.Verify.CurrentParameter("i2", "an integer, anything you like.");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));

            VisualStudioInstance.Editor.SendKeys(new object[] { VirtualKey.Home, new KeyPress(VirtualKey.End, ShiftState.Shift), VirtualKey.Delete });
            VisualStudioInstance.Editor.SendKeys("var op = OutAndParam(");

            VisualStudioInstance.Editor.Verify.CurrentSignature("void C.OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d)\r\nComplex Method Params");
            VisualStudioInstance.Editor.Verify.CurrentParameter("strings", "Jagged MultiDimensional Array");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp1()
        {
            SetUpEditor(@"
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

            VisualStudioInstance.Editor.InvokeSignatureHelp();
            VisualStudioInstance.Editor.Verify.CurrentSignature("C C.GenericMethod<T1, T2>(T1 i, T2 i2)");
            VisualStudioInstance.Editor.Verify.CurrentParameter("T1", "");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("T1", ""),
                ("T2", ""));
        }

        [TestMethod, TestCategory(Traits.Features.SignatureHelp)]
        public void GenericMethodSignatureHelp2()
        {
            SetUpEditor(@"
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

            VisualStudioInstance.Editor.InvokeSignatureHelp();
            VisualStudioInstance.Editor.Verify.CurrentSignature("C C.GenericMethod<string, int>(string i, int i2)");
            VisualStudioInstance.Editor.Verify.CurrentParameter("i", "");
            VisualStudioInstance.Editor.Verify.Parameters(
                ("i", ""),
                ("i2", ""));
        }
    }
}
