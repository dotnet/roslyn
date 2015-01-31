// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using EnvDTE;
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

        private CodeNamespace GetCodeNamespace(params object[] path)
        {
            return (CodeNamespace)GetCodeElement(path);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Children()
        {
            CodeNamespace testObject = GetCodeNamespace("Foo");

            Assert.Equal(3, testObject.Children.Count);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Members()
        {
            CodeNamespace testObject = GetCodeNamespace("Foo");

            Assert.Equal(3, testObject.Members.Count);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Parent()
        {
            CodeNamespace outer = GetCodeNamespace("Foo");
            CodeNamespace inner = outer.Members.Item("Bar") as CodeNamespace;

            Assert.Equal(outer.Name, ((CodeNamespace)inner.Parent).Name);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Kind()
        {
            CodeNamespace testObject = GetCodeNamespace("Foo");

            Assert.Equal(vsCMElement.vsCMElementNamespace, testObject.Kind);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name()
        {
            CodeNamespace testObject = GetCodeNamespace(2);

            Assert.Equal("Foo", testObject.Name);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name_Dotted()
        {
            CodeNamespace testObject = GetCodeNamespace(3);

            Assert.Equal("A.B", testObject.Name);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(11, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            TextPoint startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, endPoint.Line);
            Assert.Equal(14, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");
            AssertEx.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            TextPoint endPoint = testObject.EndPoint;

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Language()
        {
            CodeNamespace testObject = GetCodeNamespace("A.B");

            Assert.Equal(CodeModelLanguageConstants.vsCMLanguageCSharp, testObject.Language);
        }
    }
}
