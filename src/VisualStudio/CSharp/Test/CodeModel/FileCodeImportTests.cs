// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public class FileCodeImportTests : AbstractFileCodeElementTests
    {
        public FileCodeImportTests()
            : base(@"using System;
using Goo = System.Data;")
        {
        }

        private CodeImport GetCodeImport(object path)
        {
            return (CodeImport)GetCodeElement(path);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Name()
        {
            var import = GetCodeImport(1);
            Assert.Throws<COMException>(() => { var value = import.Name; });
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void FullName()
        {
            var import = GetCodeImport(1);
            Assert.Throws<COMException>(() => { var value = import.FullName; });
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Kind()
        {
            var import = GetCodeImport(1);

            Assert.Equal(vsCMElement.vsCMElementImportStmt, import.Kind);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Namespace()
        {
            var import = GetCodeImport(1);

            Assert.Equal("System", import.Namespace);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void Alias()
        {
            var import = GetCodeImport(2);

            Assert.Equal("Goo", import.Alias);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Attributes()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            var import = GetCodeImport(2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Body()
        {
            var import = GetCodeImport(2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartBody));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_BodyWithDelimiter()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Header()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_HeaderWithAttributes()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Name()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Navigate()
        {
            var import = GetCodeImport(2);
            var startPoint = import.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(13, startPoint.LineCharOffset);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_Whole()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetStartPoint_WholeWithAttributes()
        {
            var import = GetCodeImport(2);
            var startPoint = import.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Attributes()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            var import = GetCodeImport(2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Body()
        {
            var import = GetCodeImport(2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartBody));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_BodyWithDelimiter()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Header()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_HeaderWithAttributes()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Name()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Navigate()
        {
            var import = GetCodeImport(2);

            var endPoint = import.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(24, endPoint.LineCharOffset);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_Whole()
        {
            var import = GetCodeImport(2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void GetEndPoint_WholeWithAttributes()
        {
            var import = GetCodeImport(2);

            var endPoint = import.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void StartPoint()
        {
            var import = GetCodeImport(2);

            var startPoint = import.StartPoint;

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeModel)]
        public void EndPoint()
        {
            var import = GetCodeImport(2);

            var endPoint = import.EndPoint;

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }
    }
}
