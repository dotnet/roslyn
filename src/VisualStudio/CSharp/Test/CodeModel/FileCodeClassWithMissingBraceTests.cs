// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeClassWithMissingBraceTests : AbstractFileCodeElementTests
    {
        public FileCodeClassWithMissingBraceTests()
            : base(@"using System;


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

")
        {
        }

        private CodeClass GetCodeClass(params object[] path)
        {
            return (CodeClass)GetCodeElement(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body_BeforeNamespace()
        {
            CodeClass testObject = GetCodeClass("Goo");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body_BeforeOtherClass()
        {
            CodeClass testObject = GetCodeClass("Goo", "Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body_Eof()
        {
            CodeClass testObject = GetCodeClass("Baz");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }
    }
}
