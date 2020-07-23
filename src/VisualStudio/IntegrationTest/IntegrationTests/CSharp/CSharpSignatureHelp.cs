// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class CSharpSignatureHelp : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpSignatureHelp(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpSignatureHelp))
        {

        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.SendKeys.Send("var m = Method(1,");
            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature("C C.Method(int i, int i2)\r\nHello World 2.0!");
            VisualStudio.Editor.Verify.CurrentParameter("i2", "an integer, anything you like.");
            VisualStudio.Editor.Verify.Parameters(
                ("i", "an integer, preferably 42."),
                ("i2", "an integer, anything you like."));

            VisualStudio.Editor.SendKeys(new object[] { VirtualKey.Home, new KeyPress(VirtualKey.End, ShiftState.Shift), VirtualKey.Delete });
            VisualStudio.Editor.SendKeys("var op = OutAndParam(");

            VisualStudio.Editor.Verify.CurrentSignature("void C.OutAndParam(ref string[][,] strings, out string[] outArr, params dynamic d)\r\nComplex Method Params");
            VisualStudio.Editor.Verify.CurrentParameter("strings", "Jagged MultiDimensional Array");
            VisualStudio.Editor.Verify.Parameters(
                ("strings", "Jagged MultiDimensional Array"),
                ("outArr", "Out Array"),
                ("d", "Dynamic and Params param"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature("C C.GenericMethod<T1, T2>(T1 i, T2 i2)");
            VisualStudio.Editor.Verify.CurrentParameter("T1", "");
            VisualStudio.Editor.Verify.Parameters(
                ("T1", ""),
                ("T2", ""));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
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

            VisualStudio.Editor.InvokeSignatureHelp();
            VisualStudio.Editor.Verify.CurrentSignature("C C.GenericMethod<string, int>(string i, int i2)");
            VisualStudio.Editor.Verify.CurrentParameter("i", "");
            VisualStudio.Editor.Verify.Parameters(
                ("i", ""),
                ("i2", ""));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        [WorkItem(42484, "https://github.com/dotnet/roslyn/issues/42484")]
        public void ExplicitSignatureHelpDismissesCompletion()
        {
            SetUpEditor(@"
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
}");

            VisualStudio.Workspace.SetTriggerCompletionInArgumentLists(true);

            VisualStudio.Editor.SendKeys("(");

            Assert.True(VisualStudio.Editor.IsCompletionActive());
            Assert.True(VisualStudio.Editor.IsSignatureHelpActive());

            VisualStudio.Editor.InvokeSignatureHelp();

            Assert.False(VisualStudio.Editor.IsCompletionActive());
            Assert.True(VisualStudio.Editor.IsSignatureHelpActive());

            VisualStudio.Editor.Verify.CurrentSignature("void C.Test()");

            VisualStudio.Editor.SendKeys(VirtualKey.Down);

            VisualStudio.Editor.Verify.CurrentSignature("void C.Test(int x)");
        }
    }
}
