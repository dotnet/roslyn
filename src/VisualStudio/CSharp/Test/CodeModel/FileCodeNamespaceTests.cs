// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeNamespaceTests : AbstractFileCodeElementTests
    {
        public FileCodeNamespaceTests()
            : base(@"using System;

namespace Foo
{
    public class Alpha
    {
    }

    public class Beta
    {
    }

    namespace Bar
    {
    }
}

namespace A.B
{
    public class Alpha
    {
    }

    public class Beta
    {
    }
}")
        {
        }

        private async Task<CodeNamespace> GetCodeNamespaceAsync(params object[] path)
        {
            return (CodeNamespace)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Children()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("Foo");

            Assert.Equal(3, testObject.Children.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Members()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("Foo");

            Assert.Equal(3, testObject.Members.Count);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Parent()
        {
            CodeNamespace outer = await GetCodeNamespaceAsync("Foo");
            CodeNamespace inner = outer.Members.Item("Bar") as CodeNamespace;

            Assert.Equal(outer.Name, ((CodeNamespace)inner.Parent).Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Kind()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("Foo");

            Assert.Equal(vsCMElement.vsCMElementNamespace, testObject.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Name()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync(2);

            Assert.Equal("Foo", testObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Name_Dotted()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync(3);

            Assert.Equal("A.B", testObject.Name);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Attributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_AttributesWithDelimiter()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Body()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_BodyWithDelimiter()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Header()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_HeaderWithAttributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Name()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Navigate()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(11, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Whole()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_WholeWithAttributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Attributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_AttributesWithDelimiter()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_BodyWithDelimiter()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Header()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_HeaderWithAttributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Name()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Navigate()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, endPoint.Line);
            Assert.Equal(14, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Whole()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_WholeWithAttributes()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task StartPoint()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task EndPoint()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Language()
        {
            CodeNamespace testObject = await GetCodeNamespaceAsync("A.B");

            Assert.Equal(CodeModelLanguageConstants.vsCMLanguageCSharp, testObject.Language);
        }
    }
}
