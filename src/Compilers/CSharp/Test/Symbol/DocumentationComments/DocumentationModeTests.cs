// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DocumentationModeTests : CSharpTestBase
    {
        [Fact]
        public void XmlSyntaxError_Inline()
        {
            var xml = @"<unclosed>";

            var expectedText = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:Partial"" -->
        <!-- Badly formed XML comment ignored for member ""T:Parse"" -->
        <!-- Badly formed XML comment ignored for member ""T:Diagnose"" -->
    </members>
</doc>
".Trim();

            TestInline(xml, expectedText,
                // Diagnose.cs(4,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'unclosed'.'
                //  */
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("unclosed"),
                // Diagnose.cs(9,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'unclosed'.'
                //  */
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("unclosed"));
        }

        [ClrOnlyFact(ClrOnlyReason.DocumentationComment, Skip = "https://github.com/dotnet/roslyn/issues/8807")]
        public void XmlSyntaxError_Included()
        {
            var xml = @"<unclosed>";
            var xpath = "*";

            var expectedTextTemplate = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse: <!-- Badly formed XML file ""{0}"" cannot be included -->
            Diagnose: <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
        <member name=""T:Parse"">
            <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
        <member name=""T:Diagnose"">
            <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
    </members>
</doc>
".Trim();

            // Diagnostics are from types Diagnose and Partial.
            TestIncluded(xml, xpath, expectedTextTemplate, /*fallbackToErrorCodeOnlyForNonEnglish*/ true,
                // ff1abe1df1d7.xml(1,11): warning CS1592: Badly formed XML in included comments file -- 'Unexpected end of file has occurred. The following elements are not closed: unclosed.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Unexpected end of file has occurred. The following elements are not closed: unclosed."),
                // ff1abe1df1d7.xml(1,11): warning CS1592: Badly formed XML in included comments file -- 'Unexpected end of file has occurred. The following elements are not closed: unclosed.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Unexpected end of file has occurred. The following elements are not closed: unclosed."));
        }

        [Fact]
        public void CrefSyntaxError_Inline()
        {
            var xml = @"<see cref='#' />";

            var expectedText = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse
            <see cref='!:#' />
            Diagnose
            <see cref='!:#' />
        </member>
        <member name=""T:Parse"">
            <see cref='!:#' />
        </member>
        <member name=""T:Diagnose"">
            <see cref='!:#' />
        </member>
    </members>
</doc>
".Trim();

            TestInline(xml, expectedText,
                // Diagnose.cs(3,12): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "#").WithArguments("#"),
                // Diagnose.cs(3,12): warning CS1658: Identifier expected. See also error CS1001.
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, "#").WithArguments("Identifier expected", "1001"),
                // Diagnose.cs(3,12): warning CS1658: Unexpected character '#'. See also error CS1056.
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '#'", "1056"),

                // Diagnose.cs(9,12): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "#").WithArguments("#"),
                // Diagnose.cs(9,12): warning CS1658: Identifier expected. See also error CS1001.
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, "#").WithArguments("Identifier expected", "1001"),
                // Diagnose.cs(9,12): warning CS1658: Unexpected character '#'. See also error CS1056.
                // <see cref='#' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '#'", "1056"));
        }

        [Fact]
        public void CrefSyntaxError_Included()
        {
            var xml = @"<see cref='#' />";
            var xpath = "see";

            var expectedTextTemplate = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse: <see cref=""!:#"" />
            Diagnose: <see cref=""!:#"" />
        </member>
        <member name=""T:Parse"">
            <see cref=""!:#"" />
        </member>
        <member name=""T:Diagnose"">
            <see cref=""!:#"" />
        </member>
    </members>
</doc>
".Trim();

            TestIncluded(xml, xpath, expectedTextTemplate, includeElement => new[]
            {
                // ExpandIncludes.cs(2,5): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // /// <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, includeElement).WithArguments("#"),
                // ExpandIncludes.cs(2,5): warning CS1658: Identifier expected. See also error CS1001.
                // /// <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Identifier expected", "1001"),
                // ExpandIncludes.cs(2,5): warning CS1658: Unexpected character '#'. See also error CS1056.
                // /// <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Unexpected character '#'", "1056"),

                // ExpandIncludes.cs(5,21): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // /// ExpandIncludes: <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, includeElement).WithArguments("#"),
                // ExpandIncludes.cs(5,21): warning CS1658: Identifier expected. See also error CS1001.
                // /// ExpandIncludes: <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Identifier expected", "1001"),
                // ExpandIncludes.cs(5,21): warning CS1658: Unexpected character '#'. See also error CS1056.
                // /// ExpandIncludes: <include file='d6f61c210f5e.xml' path='see' />
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Unexpected character '#'", "1056")
            });
        }

        [Fact]
        public void CrefSemanticError_Inline()
        {
            var xml = @"<see cref='NotFound' />";

            var expectedText = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse
            <see cref='!:NotFound' />
            Diagnose
            <see cref='!:NotFound' />
        </member>
        <member name=""T:Parse"">
            <see cref='!:NotFound' />
        </member>
        <member name=""T:Diagnose"">
            <see cref='!:NotFound' />
        </member>
    </members>
</doc>
".Trim();

            TestInline(xml, expectedText,
                // Diagnose.cs(9,12): warning CS1574: XML comment has cref attribute 'NotFound' that could not be resolved
                // <see cref='NotFound' />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "NotFound").WithArguments("NotFound"),
                // ExpandIncludes.cs(9,12): warning CS1574: XML comment has cref attribute 'NotFound' that could not be resolved
                // <see cref='NotFound' />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "NotFound").WithArguments("NotFound"));
        }

        [Fact]
        public void CrefSemanticError_Included()
        {
            var xml = @"<see cref='NotFound' />";
            var xpath = "see";

            var expectedTextTemplate = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse: <see cref=""!:NotFound"" />
            Diagnose: <see cref=""!:NotFound"" />
        </member>
        <member name=""T:Parse"">
            <see cref=""!:NotFound"" />
        </member>
        <member name=""T:Diagnose"">
            <see cref=""!:NotFound"" />
        </member>
    </members>
</doc>
".Trim();

            TestIncluded(xml, xpath, expectedTextTemplate, includeElement => new[]
            {
                // ExpandIncludes.cs(2,5): warning CS1574: XML comment has cref attribute 'NotFound' that could not be resolved
                // /// <include file='5127bff2acf3.xml' path='see' />
                Diagnostic(ErrorCode.WRN_BadXMLRef, includeElement).WithArguments("NotFound"),

                // ExpandIncludes.cs(5,21): warning CS1574: XML comment has cref attribute 'NotFound' that could not be resolved
                // /// ExpandIncludes: <include file='5127bff2acf3.xml' path='see' />
                Diagnostic(ErrorCode.WRN_BadXMLRef, includeElement).WithArguments("NotFound")
            });
        }

        [Fact]
        public void NameSemanticError_Inline()
        {
            var xml = @"<typeparam name='NotFound' />";

            var expectedText = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse
            <typeparam name='NotFound' />
            Diagnose
            <typeparam name='NotFound' />
        </member>
        <member name=""T:Parse"">
            <typeparam name='NotFound' />
        </member>
        <member name=""T:Diagnose"">
            <typeparam name='NotFound' />
        </member>
    </members>
</doc>
".Trim();

            TestInline(xml, expectedText,
                // Diagnose.cs(3,18): warning CS1711: XML comment has a typeparam tag for 'NotFound', but there is no type parameter by that name
                // <typeparam name='NotFound' />
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "NotFound").WithArguments("NotFound"),
                // Diagnose.cs(9,18): warning CS1711: XML comment has a typeparam tag for 'NotFound', but there is no type parameter by that name
                // <typeparam name='NotFound' />
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "NotFound").WithArguments("NotFound"));
        }

        [Fact]
        public void NameSemanticError_Included()
        {
            var xml = @"<typeparam name='NotFound' />";
            var xpath = "typeparam";

            var expectedTextTemplate = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Partial"">
            Parse: <typeparam name=""NotFound"" />
            Diagnose: <typeparam name=""NotFound"" />
        </member>
        <member name=""T:Parse"">
            <typeparam name=""NotFound"" />
        </member>
        <member name=""T:Diagnose"">
            <typeparam name=""NotFound"" />
        </member>
    </members>
</doc>
".Trim();

            TestIncluded(xml, xpath, expectedTextTemplate, includeElement => new[]
            {
                // ExpandIncludes.cs(5,21): warning CS1711: XML comment has a typeparam tag for 'NotFound', but there is no type parameter by that name
                // /// ExpandIncludes: <include file='3590e97bd224.xml' path='typeparam' />
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("NotFound"),
                // ExpandIncludes.cs(2,5): warning CS1711: XML comment has a typeparam tag for 'NotFound', but there is no type parameter by that name
                // /// <include file='3590e97bd224.xml' path='typeparam' />
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("NotFound")
            });
        }

        private static void TestInline(string xml, string expectedText, params DiagnosticDescription[] expectedDiagnostics)
        {
            var sourceTemplate = @"
/**
{0}
 */
class {1} {{ }}

/**
{1}
{0}
 */
partial class Partial {{ }}
";

            var trees = AllModes.Select(mode =>
                Parse(string.Format(sourceTemplate, xml, mode), string.Format("{0}.cs", mode), GetOptions(mode)));

            var comp = CreateCompilationWithMscorlib(trees, assemblyName: "Test");
            comp.VerifyDiagnostics(expectedDiagnostics);

            var actualText = GetDocumentationCommentText(comp, expectedDiagnostics: null);
            Assert.Equal(expectedText, actualText);
        }

        private void TestIncluded(string xml, string xpath, string expectedTextTemplate, bool fallbackToErrorCodeOnlyForNonEnglish, params DiagnosticDescription[] expectedDiagnostics)
        {
            TestIncluded(xml, xpath, expectedTextTemplate, unused => expectedDiagnostics, fallbackToErrorCodeOnlyForNonEnglish);
        }

        private void TestIncluded(string xml, string xpath, string expectedTextTemplate, Func<string, DiagnosticDescription[]> makeExpectedDiagnostics, bool fallbackToErrorCodeOnlyForNonEnglish = false)
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            string includeElement = string.Format(@"<include file='{0}' path='{1}' />", xmlFilePath, xpath);
            var sourceTemplate = @"
/// {0}
class {1} {{ }}

/// {1}: {0}
partial class Partial {{ }}
";

            var trees = AllModes.Select(mode =>
                Parse(string.Format(sourceTemplate, includeElement, mode), string.Format("{0}.cs", mode), GetOptions(mode)));

            var comp = CreateCompilationWithMscorlib(
                trees,
                options: TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default),
                assemblyName: "Test");

            comp.GetDiagnostics().Verify(fallbackToErrorCodeOnlyForNonEnglish: fallbackToErrorCodeOnlyForNonEnglish, expected: makeExpectedDiagnostics(includeElement));

            var actualText = GetDocumentationCommentText(comp, expectedDiagnostics: null);
            var expectedText = string.Format(expectedTextTemplate, xmlFilePath);
            Assert.Equal(expectedText, actualText);
        }

        private static CSharpParseOptions GetOptions(DocumentationMode mode)
        {
            return TestOptions.Regular.WithDocumentationMode(mode);
        }

        private static IEnumerable<DocumentationMode> AllModes
        {
            get
            {
                var modes = Enumerable.Range((int)DocumentationMode.None, DocumentationMode.Diagnose - DocumentationMode.None + 1).Select(i => (DocumentationMode)i);
                AssertEx.All(modes, mode => mode.IsValid());
                return modes;
            }
        }
    }
}
