// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

[Trait(Traits.Feature, Traits.Features.CodeModel)]
public sealed class FileCodeClassWithMissingBraceTests : AbstractFileCodeElementTests
{
    public FileCodeClassWithMissingBraceTests()
        : base("""
            using System;


            public abstract class Goo : IDisposable, ICloneable
            {


            [Serializable]
            public class Bar
            {
                int a;

                public int A
                {
                    get
                    {
                        return a;
                    }
                }

            namespace N
            {
            }

            class Baz
            {


            """)
    {
    }

    private CodeClass GetCodeClass(params object[] path)
    {
        return (CodeClass)GetCodeElement(path);
    }

    [WpfFact]
    public void GetEndPoint_Body_BeforeNamespace()
    {
        var testObject = GetCodeClass("Goo");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

        Assert.Equal(20, endPoint.Line);
        Assert.Equal(1, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Body_BeforeOtherClass()
    {
        var testObject = GetCodeClass("Goo", "Bar");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

        Assert.Equal(20, endPoint.Line);
        Assert.Equal(1, endPoint.LineCharOffset);
    }

    [WpfFact]
    public void GetEndPoint_Body_Eof()
    {
        var testObject = GetCodeClass("Baz");

        var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

        Assert.Equal(27, endPoint.Line);
        Assert.Equal(1, endPoint.LineCharOffset);
    }
}
