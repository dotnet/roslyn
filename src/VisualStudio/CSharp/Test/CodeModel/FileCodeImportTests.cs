// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeImportTests : AbstractFileCodeElementTests
    {
        public FileCodeImportTests()
            : base(@"using System;
using Foo = System.Data;")
        {
        }

        private CodeImport GetCodeImport(object path)
        {
            return (CodeImport)GetCodeElement(path);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name()
        {
            CodeImport import = GetCodeImport(1);
            AssertEx.Throws<COMException>(() => { var value = import.Name; });
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FullName()
        {
            CodeImport import = GetCodeImport(1);
            AssertEx.Throws<COMException>(() => { var value = import.FullName; });
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Kind()
        {
            CodeImport import = GetCodeImport(1);

            Assert.Equal(vsCMElement.vsCMElementImportStmt, import.Kind);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Namespace()
        {
            CodeImport import = GetCodeImport(1);

            Assert.Equal("System", import.Namespace);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Alias()
        {
            CodeImport import = GetCodeImport(2);

            Assert.Equal("Foo", import.Alias);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            CodeImport import = GetCodeImport(2);
            TextPoint startPoint = import.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(13, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            CodeImport import = GetCodeImport(2);
            TextPoint startPoint = import.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            CodeImport import = GetCodeImport(2);

            TextPoint endPoint = import.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(24, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            CodeImport import = GetCodeImport(2);
            AssertEx.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            CodeImport import = GetCodeImport(2);

            TextPoint endPoint = import.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            CodeImport import = GetCodeImport(2);

            TextPoint startPoint = import.StartPoint;

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            CodeImport import = GetCodeImport(2);

            TextPoint endPoint = import.EndPoint;

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }
    }
}
