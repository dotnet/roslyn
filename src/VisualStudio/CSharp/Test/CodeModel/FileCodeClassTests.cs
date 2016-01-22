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
    public class FileCodeClassTests : AbstractFileCodeElementTests
    {
        public FileCodeClassTests()
            : base(@"using System;

public abstract class Foo : IDisposable, ICloneable
{
}

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
}")
        {
        }

        private async Task<CodeClass> GetCodeClassAsync(params object[] path)
        {
            return (CodeClass)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task IsAbstract()
        {
            CodeClass cc = await GetCodeClassAsync("Foo");

            Assert.True(cc.IsAbstract);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Bases()
        {
            CodeClass cc = await GetCodeClassAsync("Foo");

            var bases = cc.Bases;

            Assert.Equal(bases.Count, 1);
            Assert.Equal(bases.Cast<CodeElement>().Count(), 1);

            Assert.NotNull(bases.Parent);

            var parentClass = bases.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal(parentClass.FullName, "Foo");

            Assert.True(bases.Item("object") is CodeClass);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task ImplementedInterfaces()
        {
            CodeClass cc = await GetCodeClassAsync("Foo");

            var interfaces = cc.ImplementedInterfaces;

            Assert.Equal(interfaces.Count, 2);
            Assert.Equal(interfaces.Cast<CodeElement>().Count(), 2);

            Assert.NotNull(interfaces.Parent);

            var parentClass = interfaces.Parent as CodeClass;
            Assert.NotNull(parentClass);
            Assert.Equal(parentClass.FullName, "Foo");

            Assert.True(interfaces.Item("System.IDisposable") is CodeInterface);
            Assert.True(interfaces.Item("ICloneable") is CodeInterface);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task KindTest()
        {
            CodeClass cc = await GetCodeClassAsync("Foo");

            Assert.Equal(vsCMElement.vsCMElementClass, cc.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Attributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_AttributesWithDelimiter()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Body()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(10, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_BodyWithDelimiter()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Header()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartHeader);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_HeaderWithAttributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Name()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Navigate()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, startPoint.Line);
            Assert.Equal(14, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Whole()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_WholeWithAttributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Attributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_AttributesWithDelimiter()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter);

            Assert.Equal(7, endPoint.Line);
            Assert.Equal(15, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_BodyWithDelimiter()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Header()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_HeaderWithAttributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Name()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Navigate()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(8, endPoint.Line);
            Assert.Equal(17, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Whole()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_WholeWithAttributes()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task StartPoint()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(7, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task EndPoint()
        {
            CodeClass testObject = await GetCodeClassAsync("Bar");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(19, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }
    }
}
