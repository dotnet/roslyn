// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task AddParameters1()
        {
            var markup = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, int, string, bool, int, string, int[])""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""x""></param>
    /// <param name=""y""></param>
    /// <param name=""p""></param>
    static void $$M(this object o, int a, string b, bool c, int x = 0, string y = ""Zero"", params int[] p)
    {
        object t = new object();

        M(t, 1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        M(t, 1, ""two"", true, 3, ""four"", 5, 6);
        t.M(1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        t.M(1, ""two"", true, 3, ""four"", 5, 6);

        M(t, 1, ""two"", true, 3, ""four"");
        M(t, 1, ""two"", true, 3);
        M(t, 1, ""two"", true);

        M(t, 1, ""two"", c: true);
        M(t, 1, ""two"", true, 3, y: ""four"");

        M(t, 1, ""two"", true, 3, p: new[] { 5 });
        M(t, 1, ""two"", true, p: new[] { 5 });
        M(t, 1, ""two"", true, y: ""four"");
        M(t, 1, ""two"", true, x: 3);

        M(t, 1, ""two"", true, y: ""four"", x: 3);
        M(t, 1, y: ""four"", x: 3, b: ""two"", c: true);
        M(t, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(t, p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1, o: t);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(new AddedParameter("int", "newIntegerParameter", "12345")),
                new AddedParameterOrExistingIndex(new AddedParameter("string", "newString", "")),
                new AddedParameterOrExistingIndex(5)};
            var updatedCode = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, string,int,string, string)""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""b""></param>
    /// <param name=""newIntegerParameter""></param>
    /// <param name=""newString""></param>
    /// <param name=""y""></param>
    /// 
    /// 
    static void M(this object o, string b, int newIntegerParameter, string newString, string y = ""Zero"")
    {
        object t = new object();

        M(t, ""two"", 12345,, ""four"");
        M(t, ""two"", 12345,, ""four"");
        t.M(""two"", 12345,, ""four"");
        t.M(""two"", 12345,, ""four"");

        M(t, ""two"", 12345,, ""four"");
        M(t, ""two"", 12345,);
        M(t, ""two"", 12345,);

        M(t, ""two"", 12345,);
        M(t, ""two"", 12345,, y: ""four"");

        M(t, ""two"", 12345,);
        M(t, ""two"", 12345,);
        M(t, ""two"", 12345,, y: ""four"");
        M(t, ""two"", 12345,);

        M(t, ""two"", 12345,, y: ""four"");
        M(t, y: ""four"", newIntegerParameter: 12345, newString:, b: ""two"");
        M(t, y: ""four"", newIntegerParameter: 12345, newString:, b: ""two"");
        M(t, y: ""four"", newIntegerParameter: 12345, newString:, b: ""two"");
        M(y: ""four"", b: ""two"", newIntegerParameter: 12345, newString:, o: t);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
