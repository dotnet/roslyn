// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        private async Task<CodeImport> GetCodeImportAsync(object path)
        {
            return (CodeImport)await GetCodeElementAsync(path);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Name()
        {
            CodeImport import = await GetCodeImportAsync(1);
            Assert.Throws<COMException>(() => { var value = import.Name; });
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task FullName()
        {
            CodeImport import = await GetCodeImportAsync(1);
            Assert.Throws<COMException>(() => { var value = import.FullName; });
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Kind()
        {
            CodeImport import = await GetCodeImportAsync(1);

            Assert.Equal(vsCMElement.vsCMElementImportStmt, import.Kind);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Namespace()
        {
            CodeImport import = await GetCodeImportAsync(1);

            Assert.Equal("System", import.Namespace);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task Alias()
        {
            CodeImport import = await GetCodeImportAsync(2);

            Assert.Equal("Foo", import.Alias);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Attributes()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_AttributesWithDelimiter()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Body()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_BodyWithDelimiter()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Header()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_HeaderWithAttributes()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Name()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Navigate()
        {
            CodeImport import = await GetCodeImportAsync(2);
            TextPoint startPoint = import.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(13, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_Whole()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetStartPoint_WholeWithAttributes()
        {
            CodeImport import = await GetCodeImportAsync(2);
            TextPoint startPoint = import.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Attributes()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_AttributesWithDelimiter()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Body()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartBody));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_BodyWithDelimiter()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Header()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_HeaderWithAttributes()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Name()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Navigate()
        {
            CodeImport import = await GetCodeImportAsync(2);

            TextPoint endPoint = import.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(24, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_Whole()
        {
            CodeImport import = await GetCodeImportAsync(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task GetEndPoint_WholeWithAttributes()
        {
            CodeImport import = await GetCodeImportAsync(2);

            TextPoint endPoint = import.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task StartPoint()
        {
            CodeImport import = await GetCodeImportAsync(2);

            TextPoint startPoint = import.StartPoint;

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [ConditionalWpfFact(typeof(x86))]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public async Task EndPoint()
        {
            CodeImport import = await GetCodeImportAsync(2);

            TextPoint endPoint = import.EndPoint;

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }
    }
}
