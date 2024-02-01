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
    [Trait(Traits.Feature, Traits.Features.CodeModel)]
    public class FileCodeImportTests : AbstractFileCodeElementTests
    {
        public FileCodeImportTests()
            : base(@"using System;
using Goo = System.Data;")
        {
        }

        private CodeImport GetCodeImport(EditorTestWorkspace workspace, object path)
        {
            return (CodeImport)GetCodeElement(workspace, path);
        }

        [WpfFact]
        public void Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 1);
            Assert.Throws<COMException>(() => { var value = import.Name; });
        }

        [WpfFact]
        public void FullName()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 1);
            Assert.Throws<COMException>(() => { var value = import.FullName; });
        }

        [WpfFact]
        public void Kind()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 1);

            Assert.Equal(vsCMElement.vsCMElementImportStmt, import.Kind);
        }

        [WpfFact]
        public void Namespace()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 1);

            Assert.Equal("System", import.Namespace);
        }

        [WpfFact]
        public void Alias()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);

            Assert.Equal("Goo", import.Alias);
        }

        [WpfFact]
        public void GetStartPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<COMException>(() => import.GetStartPoint(vsCMPart.vsCMPartBody));
        }

        [WpfFact]
        public void GetStartPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetStartPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetStartPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetStartPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            var startPoint = import.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(13, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetStartPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            var startPoint = import.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<COMException>(() => import.GetEndPoint(vsCMPart.vsCMPartBody));
        }

        [WpfFact]
        public void GetEndPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetEndPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetEndPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetEndPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);

            var endPoint = import.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(24, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);
            Assert.Throws<NotImplementedException>(() => import.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetEndPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);

            var endPoint = import.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void StartPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);

            var startPoint = import.StartPoint;

            Assert.Equal(2, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void EndPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var import = GetCodeImport(workspace, 2);

            var endPoint = import.EndPoint;

            Assert.Equal(2, endPoint.Line);
            Assert.Equal(25, endPoint.LineCharOffset);
        }
    }
}
