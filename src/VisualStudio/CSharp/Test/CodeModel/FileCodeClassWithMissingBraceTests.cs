// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeClassWithMissingBraceTests : AbstractFileCodeElementTests
    {
        public FileCodeClassWithMissingBraceTests()
            : base(@"using System;


public abstract class Foo : IDisposable, ICloneable
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

        private async Task<CodeClass> GetCodeClassAsync(params object[] path)
        {
            return (CodeClass)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body_BeforeNamespace()
        {
            CodeClass testObject = await GetCodeClassAsync("Foo");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body_BeforeOtherClass()
        {
            CodeClass testObject = await GetCodeClassAsync("Foo", "Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body_Eof()
        {
            CodeClass testObject = await GetCodeClassAsync("Baz");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }
    }
}
