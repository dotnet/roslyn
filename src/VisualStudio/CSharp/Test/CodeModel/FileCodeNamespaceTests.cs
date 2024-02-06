// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    [Trait(Traits.Feature, Traits.Features.CodeModel)]
    public class FileCodeNamespaceTests : AbstractFileCodeElementTests
    {
        public FileCodeNamespaceTests()
            : base(@"using System;

namespace Goo
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

        private CodeNamespace GetCodeNamespace(EditorTestWorkspace workspace, params object[] path)
        {
            return (CodeNamespace)GetCodeElement(workspace, path);
        }

        [WpfFact]
        public void Children()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "Goo");

            Assert.Equal(3, testObject.Children.Count);
        }

        [WpfFact]
        public void Members()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "Goo");

            Assert.Equal(3, testObject.Members.Count);
        }

        [WpfFact]
        public void Parent()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var outer = GetCodeNamespace(workspace, "Goo");
            var inner = outer.Members.Item("Bar") as CodeNamespace;

            Assert.Equal(outer.Name, ((CodeNamespace)inner.Parent).Name);
        }

        [WpfFact]
        public void Kind()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "Goo");

            Assert.Equal(vsCMElement.vsCMElementNamespace, testObject.Kind);
        }

        [WpfFact]
        public void Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, 2);

            Assert.Equal("Goo", testObject.Name);
        }

        [WpfFact]
        public void Name_Dotted()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, 3);

            Assert.Equal("A.B", testObject.Name);
        }

        [WpfFact]
        public void GetStartPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetStartPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<COMException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(20, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetStartPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetStartPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetStartPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetStartPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(11, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetStartPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetStartPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetStartPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            var startPoint = testObject.GetStartPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Attributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributes));
        }

        [WpfFact]
        public void GetEndPoint_AttributesWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<COMException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartAttributesWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Body()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartBody);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(1, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_BodyWithDelimiter()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter));
        }

        [WpfFact]
        public void GetEndPoint_Header()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeader));
        }

        [WpfFact]
        public void GetEndPoint_HeaderWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartHeaderWithAttributes));
        }

        [WpfFact]
        public void GetEndPoint_Name()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartName));
        }

        [WpfFact]
        public void GetEndPoint_Navigate()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartNavigate);

            Assert.Equal(18, endPoint.Line);
            Assert.Equal(14, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void GetEndPoint_Whole()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");
            Assert.Throws<NotImplementedException>(() => testObject.GetEndPoint(vsCMPart.vsCMPartWhole));
        }

        [WpfFact]
        public void GetEndPoint_WholeWithAttributes()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var endPoint = testObject.GetEndPoint(vsCMPart.vsCMPartWholeWithAttributes);

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void StartPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var startPoint = testObject.StartPoint;

            Assert.Equal(18, startPoint.Line);
            Assert.Equal(1, startPoint.LineCharOffset);
        }

        [WpfFact]
        public void EndPoint()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            var endPoint = testObject.EndPoint;

            Assert.Equal(27, endPoint.Line);
            Assert.Equal(2, endPoint.LineCharOffset);
        }

        [WpfFact]
        public void Language()
        {
            using var workspace = CreateWorkspaceAndFileCodeModel();

            var testObject = GetCodeNamespace(workspace, "A.B");

            Assert.Equal(CodeModelLanguageConstants.vsCMLanguageCSharp, testObject.Language);
        }
    }
}
