// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using SymbolExtensions = Microsoft.CodeAnalysis.Test.Utilities.SymbolExtensions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CrefTests : CSharpTestBase
    {
        [Fact]
        public void EmptyCref()
        {
            var source = @"
/// <summary>
/// See <see cref=""""/>.
/// </summary>
class Program { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute ''
                // /// See <see cref=""/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, @"""").WithArguments(""),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=""/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Identifier expected", "1001"));
        }

        [Fact]
        public void WhitespaceCref()
        {
            var source = @"
/// <summary>
/// See <see cref="" ""/>.
/// </summary>
class Program { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute ''
                // /// See <see cref=""/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, @"""").WithArguments(""),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=""/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Identifier expected", "1001"));
        }

        [Fact] //Lexer makes bad token with diagnostic and parser produces additional diagnostic when it consumes the bad token.
        public void InvalidCrefCharacter1()
        {
            var source = @"
/// <summary>
/// See <see cref=""#""/>.
/// </summary>
class Program { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // /// See <see cref="#"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "#").WithArguments("#"),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref="#"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "#").WithArguments("Identifier expected", "1001"),
                // (3,20): warning CS1658: Unexpected character '#'. See also error CS1056.
                // /// See <see cref="#"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '#'", "1056"));
        }

        [Fact]
        public void InvalidCrefCharacter2()
        {
            var source = @"
/// <summary>
/// See <see cref="" `""/>.
/// </summary>
class Program { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,21): warning CS1584: XML comment has syntactically incorrect cref attribute ' `'
                // /// See <see cref=" `"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "`").WithArguments(" `").WithLocation(3, 21),
                // (3,21): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=" `"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "`").WithArguments("Identifier expected", "1001").WithLocation(3, 21),
                // (3,21): warning CS1658: Unexpected character '`'. See also error CS1056.
                // /// See <see cref=" `"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '`'", "1056").WithLocation(3, 21));
        }

        [Fact]
        public void IncompleteCref1()
        {
            var source = @"
/// <summary>
/// See <see cref=""
/// </summary>
class Program { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (4,5): warning CS1584: XML comment has syntactically incorrect cref attribute ''
                // /// </summary>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "<").WithArguments(""),
                // (4,5): warning CS1658: Identifier expected. See also error CS1001.
                // /// </summary>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "<").WithArguments("Identifier expected", "1001"),
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                // /// See <see cref="
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'see'.'
                // /// See <see cref="
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("see"));
        }

        [Fact]
        public void IncompleteCref2()
        {
            var source = @"
/// <summary>
/// See <see cref='";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute ''
                // /// See <see cref='
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "").WithArguments(""),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref='
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Identifier expected", "1001"),
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                // /// See <see cref='
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'see'.'
                // /// See <see cref='
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("see"),
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
                // /// See <see cref='
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary"),

                // (2,1): warning CS1587: XML comment is not placed on a valid language element
                // /// <summary>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact(), WorkItem(546839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546839")]
        public void IncompleteCref3()
        {
            var source = @"
/// <summary>
/// See <see cref='M(T, /// </summary>";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'M(T, ///'
                // /// See <see cref='M(T, /// </summary>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "M(T,").WithArguments("M(T, ///"),
                // (3,25): warning CS1658: ) expected. See also error CS1026.
                // /// See <see cref='M(T, /// </summary>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "/").WithArguments(") expected", "1026"),
                // (3,28): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                // /// See <see cref='M(T, /// </summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (3,28): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'see'.'
                // /// See <see cref='M(T, /// </summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("see"),

                // (2,1): warning CS1587: XML comment is not placed on a valid language element
                // /// <summary>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact(), WorkItem(546919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546919")]
        public void IncompleteCref4()
        {
            var source = @"
/// <summary>
/// See <see cref='M{";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'M{'
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "M{").WithArguments("M{"),
                // (3,22): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Identifier expected", "1001"),
                // (3,22): warning CS1658: Syntax error, '>' expected. See also error CS1003.
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Syntax error, '>' expected", "1003"),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'see'.'
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("see"),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
                // /// See <see cref='M{
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary"),

                // (2,1): warning CS1587: XML comment is not placed on a valid language element
                // /// <summary>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact(), WorkItem(547000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547000")]
        public void IncompleteCref5()
        {
            var source = @"
/// <summary>
/// See <see cref='T"; // Make sure the verbatim check doesn't choke on EOF.

            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,21): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                // /// See <see cref='T
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (3,21): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'see'.'
                // /// See <see cref='T
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("see"),
                // (3,21): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
                // /// See <see cref='T
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary"),

                // (2,1): warning CS1587: XML comment is not placed on a valid language element
                // /// <summary>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [WorkItem(547000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547000")]
        [Fact]
        public void Verbatim()
        {
            var source = @"
/// <summary>
/// See <see cref=""Gibberish""/>.
/// See <see cref=""T:Gibberish""/>.
/// See <see cref=""&#84;:Gibberish""/>.
/// See <see cref=""T&#58;Gibberish""/>.
/// See <see cref=""&#84;&#58;Gibberish""/>.
/// </summary>
class Program { }
";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments);
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1574: XML comment has cref attribute 'Gibberish' that could not be resolved
                // /// See <see cref="Gibberish"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Gibberish").WithArguments("Gibberish"));

            // Only the first one counts as a cref attribute.
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'Gibberish' that could not be resolved
                // /// See <see cref="Gibberish"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Gibberish").WithArguments("Gibberish"));
            Assert.Null(actualSymbol);
        }

        [WorkItem(547000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547000")]
        [Fact]
        public void NotQuiteVerbatim()
        {
            var source = @"
/// <summary>
/// See <see cref=""A""/> - only one character.
/// See <see cref="":""/> - first character is colon.
/// See <see cref=""::""/> - first character is colon.
/// See <see cref=""&#58;&#58;Gibberish""/> - first character is colon.
/// </summary>
class Program { }
";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments);
            compilation.VerifyDiagnostics(
                // (4,20): warning CS1584: XML comment has syntactically incorrect cref attribute ':'
                // /// See <see cref=":"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, ":").WithArguments(":").WithLocation(4, 20),
                // (4,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=":"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_ErrorOverride, ":").WithArguments("Identifier expected", "1001").WithLocation(4, 20),
                // (5,20): warning CS1584: XML comment has syntactically incorrect cref attribute '::'
                // /// See <see cref="::"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "::").WithArguments("::").WithLocation(5, 20),
                // (5,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref="::"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "::").WithArguments("Identifier expected", "1001").WithLocation(5, 20),
                // (6,20): warning CS1584: XML comment has syntactically incorrect cref attribute '&#58;&#58;Gibberish'
                // /// See <see cref="&#58;&#58;Gibberish"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "&#58;&#58;").WithArguments("&#58;&#58;Gibberish").WithLocation(6, 20),
                // (6,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref="&#58;&#58;Gibberish"/> - first character is colon.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "&#58;&#58;").WithArguments("Identifier expected", "1001").WithLocation(6, 20),
                // (3,20): warning CS1574: XML comment has cref attribute 'A' that could not be resolved
                // /// See <see cref="A"/> - only one character.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "A").WithArguments("A").WithLocation(3, 20));

            var crefSyntaxes = GetCrefSyntaxes(compilation);
            Assert.Equal(4, crefSyntaxes.Count());

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            AssertEx.All(crefSyntaxes, cref => model.GetSymbolInfo(cref).Symbol == null);
        }

        [Fact]
        public void SpecialName1()
        {
            var source = @"
/// <summary>
/// See <see cref="".ctor""/>.
/// </summary>
class Program { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);

            // The dot is syntactically incorrect.
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute '.ctor'
                // /// See <see cref=".ctor"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, ".").WithArguments(".ctor"),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=".ctor"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, ".").WithArguments("Identifier expected", "1001"));

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute '.ctor' that could not be resolved
                // /// See <see cref=".ctor"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "").WithArguments(""));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void SpecialName2()
        {
            var source = @"
/// <summary>
/// See <see cref="".cctor""/>.
/// </summary>
class Program { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);

            // The dot is syntactically incorrect.
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute '.cctor'
                // /// See <see cref=".cctor"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, ".").WithArguments(".cctor"),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref=".cctor"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, ".").WithArguments("Identifier expected", "1001"));

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute '.cctor' that could not be resolved
                // /// See <see cref=".cctor"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "").WithArguments(""));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void SpecialName3()
        {
            var source = @"
/// <summary>
/// See <see cref=""~Program""/>.
/// </summary>
class Program { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);

            // The tilde is syntactically incorrect.
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute '~Program'
                // /// See <see cref="~Program"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "~").WithArguments("~Program"),
                // (3,20): warning CS1658: Identifier expected. See also error CS1001.
                // /// See <see cref="~Program"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "~").WithArguments("Identifier expected", "1001"));

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute '~Program' that could not be resolved
                // /// See <see cref="~Program"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "").WithArguments(""));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void TypeScope1()
        {
            var source = @"
/// <summary>
/// See <see cref=""Program""/>.
/// </summary>
class Program { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeScope2()
        {
            var source = @"
/// <summary>
/// See <see cref=""M""/>.
/// </summary>
class Program
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeScope3()
        {
            var source = @"
/// <summary>
/// See <see cref=""T""/>.
/// </summary>
class Program<T> { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").TypeParameters.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                // /// See <see cref="T"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"));
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeScope4()
        {
            var source = @"
class Base
{
    void M() { }
}

/// <summary>
/// See <see cref=""M""/>.
/// </summary>
class Derived : Base { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // As in dev11, we ignore the inherited method symbol.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (8,20): warning CS1574: XML comment has cref attribute 'M' that could not be resolved
                // /// See <see cref="M"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M").WithArguments("M"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void TypeScope5()
        {
            var source = @"
class M
{
}

class Base
{
    void M() { }
}

/// <summary>
/// See <see cref=""M""/>.
/// </summary>
class Derived : Base { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // As in dev11, we ignore the inherited method symbol.
            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeScope6()
        {
            var source = @"
class Outer
{
    void M() { }

    /// <summary>
    /// See <see cref=""M""/>.
    /// </summary>
    class Inner { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Outer").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void MethodScope1()
        {
            var source = @"
class Program
{
    /// <summary>
    /// See <see cref=""M""/>.
    /// </summary>
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void MethodScope2()
        {
            var source = @"
class Program
{
    /// <summary>
    /// See <see cref=""T""/>.
    /// </summary>
    void M<T>() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Type parameters are not in scope.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,24): warning CS1574: XML comment has cref attribute 'T' that could not be resolved
                //     /// See <see cref="T"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "T").WithArguments("T"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void MethodScope3()
        {
            var source = @"
class Program
{
    /// <summary>
    /// See <see cref=""p""/>.
    /// </summary>
    void M(int p) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Type parameters are not in scope.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,24): warning CS1574: XML comment has cref attribute 'p' that could not be resolved
                //     /// See <see cref="p"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "p").WithArguments("p"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void IndexerScope1()
        {
            var source = @"
class Program
{
    /// <summary>
    /// See <see cref=""Item""/>.
    /// </summary>
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Slightly surprising, but matches the dev11 behavior (you're supposed to use "this").
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,24): warning CS1574: XML comment has cref attribute 'Item' that could not be resolved
                //     /// See <see cref="Item"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Item").WithArguments("Item"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void IndexerScope2()
        {
            var source = @"
class Program
{
    /// <summary>
    /// See <see cref=""x""/>.
    /// </summary>
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,24): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// See <see cref="x"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ObsoleteType()
        {
            var source = @"
using System;

/// <summary>
/// See <see cref=""A""/>.
/// </summary>
class Test
{
}

[Obsolete(""error"", true)]
class A
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var obsoleteType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            obsoleteType.ForceCompleteObsoleteAttribute();

            var testType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            testType.ForceCompleteObsoleteAttribute();

            var expectedSymbol = obsoleteType;
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void ObsoleteParameterType()
        {
            var source = @"
using System;

/// <summary>
/// See <see cref=""M(A)""/>.
/// </summary>
class Test
{
    void M(A a) { }
}

[Obsolete(""error"", true)]
class A
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var obsoleteType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            obsoleteType.ForceCompleteObsoleteAttribute();

            var testType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            testType.ForceCompleteObsoleteAttribute();

            var expectedSymbol = testType.GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeNotConstructor1()
        {
            var source = @"
/// <summary>
/// See <see cref=""A""/>.
/// </summary>
class A
{
}

/// <summary>
/// See <see cref=""B""/>.
/// </summary>
class B
{
    B() { }
}

/// <summary>
/// See <see cref=""C""/>.
/// </summary>
static class C
{
    static int x = 1;
}

/// <summary>
/// See <see cref=""D""/>.
/// </summary>
static class D
{
    static D() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                Assert.Equal(SymbolKind.NamedType, GetReferencedSymbol(crefSyntax, compilation).Kind);
            }
        }

        [Fact]
        public void TypeNotConstructor2()
        {
            var il = @"
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit B
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

  } // end of class B

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B
";

            var csharp = @"
/// <summary>
/// See <see cref=""B""/>.
/// See <see cref=""B.B""/>.
/// </summary>
class C { }
";
            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                Assert.Equal(SymbolKind.NamedType, GetReferencedSymbol(crefSyntax, compilation).Kind);
            }
        }

        [Fact]
        public void ConstructorNotType1()
        {
            var source = @"
/// <summary>
/// See <see cref=""A()""/>.
/// See <see cref=""A.A()""/>.
/// See <see cref=""A.A""/>.
/// </summary>
class A
{
}

/// <summary>
/// See <see cref=""B()""/>.
/// See <see cref=""B.B()""/>.
/// See <see cref=""B.B""/>.
/// </summary>
class B
{
    B() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                Assert.Equal(SymbolKind.Method, GetReferencedSymbol(crefSyntax, compilation).Kind);
            }
        }

        [Fact]
        public void ConstructorNotType2()
        {
            var il = @"
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit B
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }

  } // end of class B

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B
";

            var csharp = @"
/// <summary>
/// See <see cref=""B.B.B""/>.
/// See <see cref=""B()""/>.
/// See <see cref=""B.B()""/>.
/// See <see cref=""B.B.B()""/>.
/// </summary>
class C { }
";
            var compilation = CreateCompilationWithILAndMscorlib40(csharp, il);
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                Assert.Equal(SymbolKind.Method, GetReferencedSymbol(crefSyntax, compilation).Kind);
            }
        }

        /// <summary>
        /// Comment on constructor type.
        /// </summary>
        [Fact]
        public void TypeVersusConstructor1()
        {
            var source = @"
/// <summary>
/// See <see cref=""A""/>.
/// See <see cref=""A()""/>.
/// See <see cref=""A.A""/>.
/// See <see cref=""A.A()""/>.
/// </summary>
class A
{
}

/// <summary>
/// See <see cref=""B""/>.
/// See <see cref=""B()""/>.
/// See <see cref=""B.B""/>.
/// See <see cref=""B.B()""/>.
/// 
/// See <see cref=""B{T}""/>.
/// See <see cref=""B{T}()""/>.
/// See <see cref=""B{T}.B""/>.
/// See <see cref=""B{T}.B()""/>.
/// 
/// See <see cref=""B{T}.B{T}""/>.
/// See <see cref=""B{T}.B{T}()""/>.
/// </summary>
class B<T>
{
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            var crefs = GetCrefSyntaxes(compilation);

            var typeA = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var ctorA = typeA.InstanceConstructors.Single();

            var typeB = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("B");
            var ctorB = typeB.InstanceConstructors.Single();

            var expected = new ISymbol[]
            {
                typeA,
                ctorA,
                ctorA,
                ctorA,

                null,
                ctorB,
                null,
                null,

                typeB,
                ctorB,
                null,
                ctorB,

                null,
                null,
            };

            var actual = GetCrefOriginalDefinitions(model, crefs);
            AssertEx.Equal(expected, actual);

            compilation.VerifyDiagnostics(
                // (13,20): warning CS1574: XML comment has cref attribute 'B' that could not be resolved
                // /// See <see cref="B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B").WithArguments("B"),
                // (15,20): warning CS1574: XML comment has cref attribute 'B.B' that could not be resolved
                // /// See <see cref="B.B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B").WithArguments("B"),
                // (16,20): warning CS1574: XML comment has cref attribute 'B.B()' that could not be resolved
                // /// See <see cref="B.B()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B()").WithArguments("B()"),
                // (20,20): warning CS1574: XML comment has cref attribute 'B{T}.B' that could not be resolved
                // /// See <see cref="B{T}.B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B").WithArguments("B"),
                // (23,20): warning CS1574: XML comment has cref attribute 'B{T}.B{T}' that could not be resolved
                // /// See <see cref="B{T}.B{T}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}").WithArguments("B{T}"),
                // (24,20): warning CS1574: XML comment has cref attribute 'B{T}.B{T}()' that could not be resolved
                // /// See <see cref="B{T}.B{T}()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}()").WithArguments("B{T}()"));
        }

        /// <summary>
        /// Comment on unrelated type.
        /// </summary>
        [WorkItem(554077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554077")]
        [Fact]
        public void TypeVersusConstructor2()
        {
            var source = @"
class A
{
}

class B<T>
{
}

/// <summary>
/// See <see cref=""A""/>.
/// See <see cref=""A()""/>.
/// See <see cref=""A.A""/>.
/// See <see cref=""A.A()""/>.
///
/// See <see cref=""B""/>.
/// See <see cref=""B()""/>.
/// See <see cref=""B.B""/>.
/// See <see cref=""B.B()""/>.
/// 
/// See <see cref=""B{T}""/>.
/// See <see cref=""B{T}()""/>.
/// See <see cref=""B{T}.B""/>.
/// See <see cref=""B{T}.B()""/>.
/// 
/// See <see cref=""B{T}.B{T}""/>.
/// See <see cref=""B{T}.B{T}()""/>.
/// </summary>
class Other
{
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            var crefs = GetCrefSyntaxes(compilation);

            var typeA = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var ctorA = typeA.InstanceConstructors.Single();

            var typeB = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("B");
            var ctorB = typeB.InstanceConstructors.Single();

            var expected = new ISymbol[]
            {
                typeA,
                ctorA,
                ctorA,
                ctorA,

                null,
                null,
                null,
                null,

                typeB,
                ctorB,
                ctorB, //NB: different when comment is not applied on/in B.
                ctorB,

                null,
                null,
            };

            var actual = GetCrefOriginalDefinitions(model, crefs);
            AssertEx.Equal(expected, actual);

            compilation.VerifyDiagnostics(
                // (16,20): warning CS1574: XML comment has cref attribute 'B' that could not be resolved
                // /// See <see cref="B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B").WithArguments("B"),
                // (17,20): warning CS1574: XML comment has cref attribute 'B()' that could not be resolved
                // /// See <see cref="B()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B()").WithArguments("B()"),
                // (18,20): warning CS1574: XML comment has cref attribute 'B.B' that could not be resolved
                // /// See <see cref="B.B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B").WithArguments("B"),
                // (19,20): warning CS1574: XML comment has cref attribute 'B.B()' that could not be resolved
                // /// See <see cref="B.B()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B()").WithArguments("B()"),
                // (26,20): warning CS1574: XML comment has cref attribute 'B{T}.B{T}' that could not be resolved
                // /// See <see cref="B{T}.B{T}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}").WithArguments("B{T}"),
                // (27,20): warning CS1574: XML comment has cref attribute 'B{T}.B{T}()' that could not be resolved
                // /// See <see cref="B{T}.B{T}()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}()").WithArguments("B{T}()"));
        }

        /// <summary>
        /// Comment on nested type of constructor type (same behavior as unrelated type).
        /// </summary>
        [WorkItem(554077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554077")]
        [Fact]
        public void TypeVersusConstructor3()
        {
            var source = @"
class A
{
    /// <summary>
    /// See <see cref=""A""/>.
    /// See <see cref=""A()""/>.
    /// See <see cref=""A.A""/>.
    /// See <see cref=""A.A()""/>.
    /// </summary>
    class Inner
    {
    }
}

class B<T>
{
    /// <summary>
    /// See <see cref=""B""/>.
    /// See <see cref=""B()""/>.
    /// See <see cref=""B.B""/>.
    /// See <see cref=""B.B()""/>.
    /// 
    /// See <see cref=""B{T}""/>.
    /// See <see cref=""B{T}()""/>.
    /// See <see cref=""B{T}.B""/>.
    /// See <see cref=""B{T}.B()""/>.
    /// 
    /// See <see cref=""B{T}.B{T}""/>.
    /// See <see cref=""B{T}.B{T}()""/>.
    /// </summary>
    class Inner
    {
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            var crefs = GetCrefSyntaxes(compilation);

            var typeA = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var ctorA = typeA.InstanceConstructors.Single();

            var typeB = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("B");
            var ctorB = typeB.InstanceConstructors.Single();

            var expected = new ISymbol[]
            {
                typeA,
                ctorA,
                ctorA,
                ctorA,

                null,
                null,
                null,
                null,

                typeB,
                ctorB,
                ctorB, //NB: different when comment is not applied on/in B.
                ctorB,

                null,
                null,
            };

            var actual = GetCrefOriginalDefinitions(model, crefs);
            AssertEx.Equal(expected, actual);

            compilation.VerifyDiagnostics(
                // (18,24): warning CS1574: XML comment has cref attribute 'B' that could not be resolved
                //     /// See <see cref="B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B").WithArguments("B"),
                // (19,24): warning CS1574: XML comment has cref attribute 'B()' that could not be resolved
                //     /// See <see cref="B()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B()").WithArguments("B()"),
                // (20,24): warning CS1574: XML comment has cref attribute 'B.B' that could not be resolved
                //     /// See <see cref="B.B"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B").WithArguments("B"),
                // (21,24): warning CS1574: XML comment has cref attribute 'B.B()' that could not be resolved
                //     /// See <see cref="B.B()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.B()").WithArguments("B()"),
                // (28,24): warning CS1574: XML comment has cref attribute 'B{T}.B{T}' that could not be resolved
                //     /// See <see cref="B{T}.B{T}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}").WithArguments("B{T}"),
                // (29,24): warning CS1574: XML comment has cref attribute 'B{T}.B{T}()' that could not be resolved
                //     /// See <see cref="B{T}.B{T}()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B{T}.B{T}()").WithArguments("B{T}()"));
        }

        [Fact]
        public void NoConstructor()
        {
            var source = @"
/// <summary>
/// See <see cref=""C()""/>.
/// See <see cref=""C.C()""/>.
/// See <see cref=""C.C""/>.
/// </summary>
static class C
{
}

/// <summary>
/// See <see cref=""D()""/>.
/// See <see cref=""D.D()""/>.
/// See <see cref=""D.D""/>.
/// </summary>
static class D
{
    static D() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            foreach (var crefSyntax in GetCrefSyntaxes(compilation))
            {
                string text = crefSyntax.ToString();
                string arguments = text.Contains("C()") ? "C()" : text.Contains("C") ? "C" : text.Contains("D()") ? "D()" : "D";
                Assert.Null(GetReferencedSymbol(crefSyntax, compilation,
                    Diagnostic(ErrorCode.WRN_BadXMLRef, text).WithArguments(arguments)));
            }
        }

        [Fact]
        public void AmbiguousReferenceWithoutParameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""M""/>.
/// </summary>
class C
{
    void M() { }
    void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // CONSIDER: Dev11 actually picks the constructor of C - probably an accidental fall-through.
            var expectedCandidates = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers("M").OfType<MethodSymbol>();
            var expectedWinner = expectedCandidates.Single(m => m.ParameterCount == 0);

            Symbol actualWinner;
            var actualCandidates = GetReferencedSymbols(crefSyntax, compilation, out actualWinner,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'M'. Assuming 'C.M()', but could have also matched other overloads including 'C.M(int)'.
                // /// See <see cref="M"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M").WithArguments("M", "C.M()", "C.M(int)"));

            Assert.Equal(expectedWinner, actualWinner);
            AssertEx.SetEqual(expectedCandidates.AsEnumerable(), actualCandidates.ToArray());
        }

        [Fact]
        public void SourceMetadataConflict()
        {
            var il = @"
.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class B
";

            var csharp = @"
/// <summary>
/// See <see cref=""B""/>.
/// </summary>
class B { }
";
            var ilRef = CompileIL(il);
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(csharp, new[] { ilRef });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // NOTE: As in Dev11, no warning is produced.
            var expectedSymbol = compilation.GlobalNamespace.GetMembers("B").OfType<SourceNamedTypeSymbol>().Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Basic()
        {
            var source = @"
/// <summary>
/// See <see cref=""M(int)""/>.
/// </summary>
class B
{
    void M(string x) { }
    void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => m.Parameters.Single().Type.SpecialType == SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Ref()
        {
            var source = @"
/// <summary>
/// See <see cref=""M(ref int)""/>.
/// </summary>
class B
{
    void M(ref int x) { }
    void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => !m.ParameterRefKinds.IsDefault);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Out()
        {
            var source = @"
/// <summary>
/// See <see cref=""M(out int)""/>.
/// </summary>
class B
{
    void M(out int x) { }
    void M(ref int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => m.ParameterRefKinds.Single() == RefKind.Out);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Params()
        {
            var source = @"
/// <summary>
/// See <see cref=""M(int[])""/>.
/// </summary>
class B
{
    void M(int x) { }
    void M(params int[] x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => m.HasParamsParameter());
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Extension()
        {
            var source = @"
/// <summary>
/// See <see cref=""M(B)""/>.
/// </summary>
class B
{
    public static void M(string s) { }
    public static void M(this B self) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => m.IsExtensionMethod);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void OverloadResolution_Arglist1()
        {
            var source = @"
/// <summary>
/// See <see cref=""M()""/>.
/// </summary>
class B
{
    void M() { }
    void M(__arglist) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedCandidates = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M");
            var expectedWinner = expectedCandidates.OfType<MethodSymbol>().Single(m => !m.IsVararg);

            Symbol actualWinner;
            var actualCandidates = GetReferencedSymbols(crefSyntax, compilation, out actualWinner,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'M()'. Assuming 'B.M()', but could have also matched other overloads including 'B.M(__arglist)'.
                // /// See <see cref="M()"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M()").WithArguments("M()", "B.M()", "B.M(__arglist)"));

            Assert.Equal(expectedWinner, actualWinner);
            AssertEx.SetEqual(expectedCandidates, actualCandidates.ToArray());
        }

        [Fact]
        public void OverloadResolution_Arglist2()
        {
            var source = @"
/// <summary>
/// See <see cref=""M()""/>.
/// </summary>
class B
{
    void M(int x) { }
    void M(__arglist) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>().
                Single(m => m.IsVararg);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void TypeParameters_Simple1()
        {
            var source = @"
/// <summary>
/// See <see cref=""B{T}""/>.
/// </summary>
class B<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var typeArgument = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
            Assert.NotEqual(expectedOriginalDefinitionSymbol.TypeParameters.Single(), typeArgument);
            Assert.Equal("T", typeArgument.Name);
            Assert.IsType<CrefTypeParameterSymbol>(typeArgument);
            Assert.Equal(0, ((TypeParameterSymbol)typeArgument).Ordinal);
        }

        [Fact]
        public void TypeParameters_Simple2()
        {
            var source = @"
/// <summary>
/// See <see cref=""B{U}""/>.
/// </summary>
class B<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var typeArgument = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
            Assert.NotEqual(expectedOriginalDefinitionSymbol.TypeParameters.Single(), typeArgument);
            Assert.Equal("U", typeArgument.Name);
            Assert.IsType<CrefTypeParameterSymbol>(typeArgument);
            Assert.Equal(0, ((TypeParameterSymbol)typeArgument).Ordinal);
        }

        [Fact]
        public void TypeParameters_Simple3()
        {
            var source = @"
/// <summary>
/// See <see cref=""M{T}""/>.
/// </summary>
class B
{
    void M<T>() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var typeArgument = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
            Assert.NotEqual(expectedOriginalDefinitionSymbol.TypeParameters.Single(), typeArgument);
            Assert.Equal("T", typeArgument.Name);
            Assert.IsType<CrefTypeParameterSymbol>(typeArgument);
            Assert.Equal(0, ((TypeParameterSymbol)typeArgument).Ordinal);
        }

        [Fact]
        public void TypeParameters_Simple4()
        {
            var source = @"
/// <summary>
/// See <see cref=""M{U}""/>.
/// </summary>
class B
{
    void M<T>() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var typeArgument = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
            Assert.NotEqual(expectedOriginalDefinitionSymbol.TypeParameters.Single(), typeArgument);
            Assert.Equal("U", typeArgument.Name);
            Assert.IsType<CrefTypeParameterSymbol>(typeArgument);
            Assert.Equal(0, ((TypeParameterSymbol)typeArgument).Ordinal);
        }

        [Fact]
        public void TypeParameters_Duplicate()
        {
            var source = @"
/// <summary>
/// See <see cref=""T""/>.
/// </summary>
class B<T, T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").TypeArguments()[0];
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                // /// See <see cref="T"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"));

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        // Keep code coverage happy.
        [Fact]
        public void TypeParameters_Symbols()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A, B}""/>.
/// </summary>
class C<T, U, V>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            var actualTypeParameters = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Cast<CrefTypeParameterSymbol>().ToArray();

            AssertEx.None(actualTypeParameters, p => p.IsFromCompilation(compilation));
            AssertEx.None(actualTypeParameters, p => p.IsImplicitlyDeclared);
            AssertEx.All(actualTypeParameters, p => p.Variance == VarianceKind.None);
            AssertEx.All(actualTypeParameters, p => p.Locations.Single() == p.DeclaringSyntaxReferences.Single().GetLocation());
            AssertEx.None(actualTypeParameters, p => p.HasValueTypeConstraint);
            AssertEx.None(actualTypeParameters, p => p.HasReferenceTypeConstraint);
            AssertEx.None(actualTypeParameters, p => p.HasConstructorConstraint);
            AssertEx.All(actualTypeParameters, p => p.ContainingSymbol == null);
            AssertEx.All(actualTypeParameters, p => p.GetConstraintTypes(null).Length == 0);
            AssertEx.All(actualTypeParameters, p => p.GetInterfaces(null).Length == 0);

            foreach (var p in actualTypeParameters)
            {
                Assert.ThrowsAny<Exception>(() => p.GetEffectiveBaseClass(null));
                Assert.ThrowsAny<Exception>(() => p.GetDeducedBaseType(null));
            }

            Assert.Equal(actualTypeParameters[0], actualTypeParameters[1]);
            Assert.Equal(actualTypeParameters[0].GetHashCode(), actualTypeParameters[1].GetHashCode());

            Assert.NotEqual(actualTypeParameters[0], actualTypeParameters[2]);

#if !DISABLE_GOOD_HASH_TESTS
            Assert.NotEqual(actualTypeParameters[0].GetHashCode(), actualTypeParameters[2].GetHashCode());
#endif
        }

        [Fact]
        public void TypeParameters_Signature1()
        {
            var source = @"
/// <summary>
/// See <see cref=""M{U}(U)""/>.
/// </summary>
class B
{
    void M<T>(T t) { }
    void M<T>(int t) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMembers("M").OfType<MethodSymbol>()
                .Single(method => method.Parameters.Single().Type.TypeKind == TypeKind.TypeParameter);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var typeArgument = actualSymbol.GetMemberTypeArgumentsNoUseSiteDiagnostics().Single();
            Assert.NotEqual(expectedOriginalDefinitionSymbol.TypeParameters.Single(), typeArgument);
            Assert.Equal("U", typeArgument.Name);
            Assert.IsType<CrefTypeParameterSymbol>(typeArgument);
            Assert.Equal(0, ((TypeParameterSymbol)typeArgument).Ordinal);
            Assert.Equal(typeArgument, actualSymbol.GetParameters().Single().Type);
        }

        [Fact]
        public void TypeParameters_Signature2()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{S, T}.B{U, V}.M{W, X}(S, U, W, T, V, X)""/>.
/// </summary>
class A<M, N>
{
    class B<O, P>
    {
        internal void M<Q, R>(M m, O o, Q q, N n, P p, R r) { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("M");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);

            var expectedOriginalParameterTypes = expectedOriginalDefinitionSymbol.Parameters.Select(p => p.Type).Cast<TypeParameterSymbol>();
            var actualParameterTypes = actualSymbol.GetParameters().Select(p => p.Type).Cast<TypeParameterSymbol>();

            AssertEx.Equal(expectedOriginalParameterTypes.Select(t => t.Ordinal), actualParameterTypes.Select(t => t.Ordinal));
            AssertEx.None(expectedOriginalParameterTypes.Zip(actualParameterTypes, object.Equals), x => x);
        }

        [Fact]
        public void TypeParameters_Duplicates1()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{T, T}.M(T)""/>.
/// </summary>
class A<T, U>
{
    void M(T t) { }
    void M(U u) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // CONSIDER: In Dev11, this unambiguously matches M(U) (i.e. the last type parameter wins).

            Symbol actualWinner;
            var actualCandidates = GetReferencedSymbols(crefSyntax, compilation, out actualWinner,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'A{T, T}.M(T)'. Assuming 'A<T, T>.M(T)', but could have also matched other overloads including 'A<T, T>.M(T)'.
                // /// See <see cref="A{T, T}.M(T)"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "A{T, T}.M(T)").WithArguments("A{T, T}.M(T)", "A<T, T>.M(T)", "A<T, T>.M(T)"));

            Assert.False(actualWinner.IsDefinition);

            var actualParameterType = actualWinner.GetParameters().Single().Type;
            AssertEx.All(actualWinner.ContainingType.TypeArguments(), typeParam => TypeSymbol.Equals(typeParam, actualParameterType, TypeCompareKind.ConsiderEverything2)); //CONSIDER: Would be different in Dev11.
            Assert.Equal(1, ((TypeParameterSymbol)actualParameterType).Ordinal);

            Assert.Equal(2, actualCandidates.Length);
            Assert.Equal(actualWinner, actualCandidates[0]);
            Assert.Equal(actualWinner.ContainingType.GetMembers(actualWinner.Name).Single(member => member != actualWinner), actualCandidates[1]);
        }

        [Fact]
        public void TypeParameters_Duplicates2()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{T}.B{T}.M(T)""/>.
/// </summary>
class A<T>
{
    class B<U>
    {
        internal void M(T t) { }
        internal void M(U u) { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // CONSIDER: In Dev11, this unambiguously matches M(U) (i.e. the last type parameter wins).

            Symbol actualWinner;
            var actualCandidates = GetReferencedSymbols(crefSyntax, compilation, out actualWinner,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'A{T}.B{T}.M(T)'. Assuming 'A<T>.B<T>.M(T)', but could have also matched other overloads including 'A<T>.B<T>.M(T)'.
                // /// See <see cref="A{T}.B{T}.M(T)"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "A{T}.B{T}.M(T)").WithArguments("A{T}.B{T}.M(T)", "A<T>.B<T>.M(T)", "A<T>.B<T>.M(T)"));

            Assert.False(actualWinner.IsDefinition);

            var actualParameterType = actualWinner.GetParameters().Single().Type;
            Assert.Equal(actualParameterType, actualWinner.ContainingType.TypeArguments().Single());
            Assert.Equal(actualParameterType, actualWinner.ContainingType.ContainingType.TypeArguments().Single());

            Assert.Equal(2, actualCandidates.Length);
            Assert.Equal(actualWinner, actualCandidates[0]);
            Assert.Equal(actualWinner.ContainingType.GetMembers(actualWinner.Name).Single(member => member != actualWinner), actualCandidates[1]);
        }

        [Fact]
        public void TypeParameters_ExistingTypes1()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{U}.M(U)""/>.
/// </summary>
class A<T>
{
    void M(T t) { } // This one.
    void M(U u) { }
}

class U { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMembers("M").OfType<MethodSymbol>().
                Single(method => method.Parameters.Single().Type.TypeKind == TypeKind.TypeParameter);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);
            Assert.Equal(TypeKind.TypeParameter, actualSymbol.GetParameterTypes().Single().Type.TypeKind);
        }

        [Fact]
        public void TypeParameters_ExistingTypes2()
        {
            var source = @"
using System;

/// <summary>
/// See <see cref=""A{Int32}.M(Int32)""/>.
/// </summary>
class A<T>
{
    void M(T t) { } // This one.
    void M(int u) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMembers("M").OfType<MethodSymbol>().
                Single(method => method.Parameters.Single().Type.TypeKind == TypeKind.TypeParameter);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);
            Assert.Equal(TypeKind.TypeParameter, actualSymbol.GetParameterTypes().Single().Type.TypeKind);
        }

        [Fact]
        public void GenericTypeConstructor()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{T}()""/>.
/// </summary>
class A<T>
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedOriginalDefinitionSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedOriginalDefinitionSymbol, actualSymbol.OriginalDefinition);
        }

        [Fact]
        public void Inaccessible1()
        {
            var source = @"
/// <summary>
/// See <see cref=""C.M""/>.
/// </summary>
class A
{
}

class C
{
    private void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.NotNull(actualSymbol);
            Assert.Equal(
                compilation.GlobalNamespace
                           .GetMember<NamedTypeSymbol>("C")
                           .GetMember<SourceOrdinaryMethodSymbol>("M"),
                actualSymbol);
            Assert.Equal(SymbolKind.Method, actualSymbol.Kind);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(info.Symbol, actualSymbol.ISymbol);
        }

        [Fact]
        public void Inaccessible2()
        {
            var source = @"
/// <summary>
/// See <see cref=""Outer.Inner.M""/>.
/// </summary>
class A
{
}

class Outer
{
    private class Inner
    {
        private void M() { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.GlobalNamespace
                                            .GetMember<NamedTypeSymbol>("Outer")
                                            .GetMember<NamedTypeSymbol>("Inner")
                                            .GetMember<SourceOrdinaryMethodSymbol>("M");

            // Consider inaccessible symbols, as in Dev11
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")]
        [Fact]
        public void Inaccessible3()
        {
            var lib1Source = @"internal class C { }";
            var lib2Source = @"public class C { }";

            var source = @"
/// <summary>
/// See <see cref=""C""/>.
/// </summary>
class Test { }
";

            var lib1Ref = CreateCompilation(lib1Source, assemblyName: "A").EmitToImageReference();
            var lib2Ref = CreateCompilation(lib2Source, assemblyName: "B").EmitToImageReference();

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { lib1Ref, lib2Ref });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Break: In dev11 the accessible symbol is preferred. We simply prefer the "first"
            Symbol actualSymbol;
            var symbols = GetReferencedSymbols(crefSyntax, compilation, out actualSymbol,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'C'. Assuming 'C', but could have also matched other overloads including 'C'.
                // /// See <see cref="C"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C").WithArguments("C", "C", "C").WithLocation(3, 20));
            Assert.Equal("A", actualSymbol.ContainingAssembly.Name);
        }

        [WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")]
        [Fact]
        public void Inaccessible4()
        {
            var source = @"
namespace Test
{
    using System;
 
    /// <summary>
    /// <see cref=""ClientUtils.Goo""/>
    /// </summary>
    enum E { }
}

class ClientUtils
{
    public static void Goo() { }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // NOTE: Matches dev11 - the accessible symbol is preferred (vs System.ClientUtils).
            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("ClientUtils").GetMember<MethodSymbol>("Goo");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")]
        [WorkItem(709199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709199")]
        [Fact]
        public void ProtectedInstanceBaseMember()
        {
            var source = @"
class Base
{
    protected int F;
}

/// Accessible: <see cref=""Base.F""/>
class Derived : Base
{
}

/// Not accessible: <see cref=""Base.F""/>
class Other
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'Base.F' is never assigned to, and will always have its default value 0
                //     protected static int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("Base.F", "0"));

            var crefSyntax = GetCrefSyntaxes(compilation).First();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base").GetMember<FieldSymbol>("F");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(568006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568006")]
        [WorkItem(709199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709199")]
        [Fact]
        public void ProtectedStaticBaseMember()
        {
            var source = @"
class Base
{
    protected static int F;
}

/// Accessible: <see cref=""Base.F""/>
class Derived : Base
{
}

/// Not accessible: <see cref=""Base.F""/>
class Other
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'Base.F' is never assigned to, and will always have its default value 0
                //     protected static int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("Base.F", "0"));

            var crefSyntax = GetCrefSyntaxes(compilation).First();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base").GetMember<FieldSymbol>("F");
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Ambiguous1()
        {
            var libSource = @"
public class A
{
}
";

            var source = @"
/// <summary>
/// See <see cref=""A""/>.
/// </summary>
class B : A
{
}
";
            var lib1 = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib1");
            var lib2 = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib2");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib1), new CSharpCompilationReference(lib2) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // CONSIDER: Dev11 fails with WRN_BadXMLRef.
            Symbol actualWinner;
            var actualCandidates = GetReferencedSymbols(crefSyntax, compilation, out actualWinner,
                // (3,20): warning CS0419: Ambiguous reference in cref attribute: 'A'. Assuming 'A', but could have also matched other overloads including 'A'.
                // /// See <see cref="A"/>.
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "A").WithArguments("A", "A", "A"));

            Assert.Contains(actualWinner, actualCandidates);
            Assert.Equal(2, actualCandidates.Length);
            AssertEx.SetEqual(actualCandidates.Select(sym => sym.ContainingAssembly.Name), "Lib1", "Lib2");

            var model = compilation.GetSemanticModel(crefSyntax.SyntaxTree);
            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, info.CandidateReason);
            AssertEx.SetEqual(info.CandidateSymbols.Select(sym => sym.ContainingAssembly.Name), "Lib1", "Lib2");
        }

        [Fact]
        public void Ambiguous2()
        {
            var libSource = @"
public class A
{
    public void M() { }
}
";

            var source = @"
/// <summary>
/// See <see cref=""A.M""/>.
/// </summary>
class B
{
}
";
            var lib1 = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib1");
            var lib2 = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib2");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib1), new CSharpCompilationReference(lib2) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Not ideal, but matches dev11.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'A.M' that could not be resolved
                // /// See <see cref="A.M"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "A.M").WithArguments("M"));

            Assert.Null(actualSymbol);

            var model = compilation.GetSemanticModel(crefSyntax.SyntaxTree);
            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation))]
        public void Ambiguous3()
        {
            var lib1Source = @"
public class A
{
}
";

            var lib2Source = @"
public class A
{

}

public class B
{
    public void M(A a) { }
    public void M(int a) { }
}
";

            var source = @"
/// <summary>
/// See <see cref=""B.M(A)""/>.
/// </summary>
class C
{
}
";
            var lib1 = CreateCompilationWithMscorlib40AndDocumentationComments(lib1Source, assemblyName: "Lib1");
            var lib2 = CreateCompilationWithMscorlib40AndDocumentationComments(lib2Source, assemblyName: "Lib2");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib1), new CSharpCompilationReference(lib2) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // Not ideal, but matches dev11.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1580: Invalid type for parameter 'A' in XML comment cref attribute: 'B.M(A)'
                // /// See <see cref="B.M(A)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "A").WithArguments("A", "B.M(A)"),
                // (3,20): warning CS1574: XML comment has cref attribute 'B.M(A)' that could not be resolved
                // /// See <see cref="B.M(A)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "B.M(A)").WithArguments("M(A)"));

            Assert.Null(actualSymbol);

            var model = compilation.GetSemanticModel(crefSyntax.SyntaxTree);
            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [Fact]
        public void TypeCref1()
        {
            var libSource = @"
public class A
{
}
";

            var source = @"
extern alias LibAlias;

/// <summary>
/// See <see cref=""LibAlias::A""/>.
/// </summary>
class C
{
}
";
            var lib = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib, aliases: ImmutableArray.Create("LibAlias")) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);
            Assert.IsType<SourceNamedTypeSymbol>(actualSymbol);
            Assert.Equal("A", actualSymbol.Name);
            Assert.Equal("Lib", actualSymbol.ContainingAssembly.Name);
        }

        [Fact]
        public void TypeCref2()
        {
            var libSource = @"
public class A
{
}
";

            var source = @"
extern alias LibAlias;

/// <summary>
/// See <see cref=""BadAlias::A""/>.
/// </summary>
class C
{
}
";
            var lib = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib, aliases: ImmutableArray.Create("LibAlias")) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,20): warning CS1574: XML comment has cref attribute 'BadAlias::A' that could not be resolved
                // /// See <see cref="BadAlias::A"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "BadAlias::A").WithArguments("BadAlias::A"));
        }

        [Fact]
        public void TypeCref3()
        {
            var libSource = @"
public class A
{
}
";

            var source = @"
extern alias LibAlias;

/// <summary>
/// See <see cref=""LibAlias::BadType""/>.
/// </summary>
class C
{
}
";
            var lib = CreateCompilationWithMscorlib40AndDocumentationComments(libSource, assemblyName: "Lib");

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { new CSharpCompilationReference(lib, aliases: ImmutableArray.Create("LibAlias")) });
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (5,20): warning CS1574: XML comment has cref attribute 'LibAlias::BadType' that could not be resolved
                // /// See <see cref="LibAlias::BadType"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "LibAlias::BadType").WithArguments("LibAlias::BadType"));
        }

        [Fact]
        public void TypeCref4()
        {
            var source = @"
/// <summary>
/// See <see cref=""int""/>.
/// </summary>
class C
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GetSpecialType(SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void IndexerCref_NoParameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""this""/>.
/// </summary>
class C
{
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void IndexerCref_Parameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""this[int]""/>.
/// </summary>
class C
{
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void IndexerCref_OverloadResolutionFailure()
        {
            var source = @"
/// <summary>
/// See <see cref=""this[float]""/>.
/// </summary>
class C
{
    int this[int x] { get { return 0; } set { } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'this[float]' that could not be resolved
                // /// See <see cref="this[float]"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "this[float]").WithArguments("this[float]"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void UnaryOperator_NoParameters_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !""/>.
/// </summary>
class C
{
    public static C operator !(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.LogicalNotOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_NoParameters_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator -""/>.
/// </summary>
class C
{
    public static C operator -(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'operator -' that could not be resolved
                // /// See <see cref="operator -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator -").WithArguments("operator -").WithLocation(3, 20)
                );

            Assert.Null(actualSymbol);
        }

        [Fact]
        public void UnaryOperator_OneParameter()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !(C)""/>.
/// </summary>
class C
{
    public static C operator !(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.LogicalNotOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_OverloadResolutionFailure()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !(int)""/>.
/// </summary>
class C
{
    public static C operator !(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'operator !(int)' that could not be resolved
                // /// See <see cref="operator !(int)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator !(int)").WithArguments("operator !(int)"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void UnaryOperator_OverloadResolution()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !(int)""/>.
/// </summary>
class C
{
    public static bool operator !(C q)
    {
        return false;
    }

    public static bool op_LogicalNot(int x)
    {
        return false;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.LogicalNotOperatorName).OfType<MethodSymbol>().
                Single(method => method.ParameterTypesWithAnnotations.Single().SpecialType == SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_Type()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !""/>.
/// </summary>
class op_LogicalNot
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.LogicalNotOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_Constructor0()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !()""/>.
/// </summary>
class op_LogicalNot
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.LogicalNotOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_Constructor1()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !(int)""/>.
/// </summary>
class op_LogicalNot
{
    op_LogicalNot(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.LogicalNotOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_Constructor2()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator !(int, int)""/>.
/// </summary>
class op_LogicalNot
{
    op_LogicalNot(int x, int y) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.LogicalNotOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_NoParameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /""/>.
/// </summary>
class C
{
    public static C operator /(C c, int x)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.DivisionOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_TwoParameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(C, int)""/>.
/// </summary>
class C
{
    public static C operator /(C c, int x)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.DivisionOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_OverloadResolutionFailure()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(int)""/>.
/// </summary>
class C
{
    public static C operator /(C c, int x)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'operator /(int)' that could not be resolved
                // /// See <see cref="operator /(int)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator /(int)").WithArguments("operator /(int)"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void BinaryOperator_OverloadResolution()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(int, int)""/>.
/// </summary>
class C
{
    public static bool operator /(C q, int x)
    {
        return false;
    }

    public static bool op_Division(int x, int x)
    {
        return false;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.DivisionOperatorName).OfType<MethodSymbol>().
                Single(method => method.ParameterTypesWithAnnotations.First().SpecialType == SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_Type()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /""/>.
/// </summary>
class op_Division
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.DivisionOperatorName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_Constructor0()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /()""/>.
/// </summary>
class op_Division
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.DivisionOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_Constructor1()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(int)""/>.
/// </summary>
class op_Division
{
    op_Division(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            // CONSIDER: This is a syntactic error in dev11.
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'operator /(int)' that could not be resolved
                // /// See <see cref="operator /(int)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator /(int)").WithArguments("operator /(int)"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void BinaryOperator_Constructor2()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(int, int)""/>.
/// </summary>
class op_Division
{
    op_Division(int x, int y) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.DivisionOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_Constructor3()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator /(int, int, int)""/>.
/// </summary>
class op_Division
{
    op_Division(int x, int y, int z) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.DivisionOperatorName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_NoParameters()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int""/>.
/// </summary>
class C
{
    public static explicit operator int(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_OneParameter()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(C)""/>.
/// </summary>
class C
{
    public static implicit operator int(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>(WellKnownMemberNames.ImplicitConversionName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_OverloadResolutionFailure()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int(int)""/>.
/// </summary>
class C
{
    public static explicit operator int(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation,
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator int(int)' that could not be resolved
                // /// See <see cref="explicit operator int(int)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator int(int)").WithArguments("explicit operator int(int)"));
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void Conversion_OverloadResolution()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(int)""/>.
/// </summary>
class C
{
    public static implicit operator int(C q)
    {
        return false;
    }

    public static int op_Implicit(int x)
    {
        return false;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.ImplicitConversionName).OfType<MethodSymbol>().
                Single(method => method.ParameterTypesWithAnnotations.Single().SpecialType == SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_ReturnType()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(int)""/>.
/// </summary>
class C
{
    public static implicit operator int(C q)
    {
        return false;
    }

    public static string op_Implicit(int x)
    {
        return false;
    }

    // Declaration error, but not important for test.
    public static int op_Implicit(int x)
    {
        return false;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.ImplicitConversionName).OfType<MethodSymbol>().
                Single(method => method.ParameterTypesWithAnnotations.Single().SpecialType == SpecialType.System_Int32 && method.ReturnType.SpecialType == SpecialType.System_Int32);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_Type()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int""/>.
/// </summary>
class op_Explicit
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.ExplicitConversionName);
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_Constructor0()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int()""/>.
/// </summary>
class op_Implicit
{
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.ImplicitConversionName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_Constructor1()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int(int)""/>.
/// </summary>
class op_Explicit
{
    op_Explicit(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.ExplicitConversionName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void Conversion_Constructor2()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(int, int)""/>.
/// </summary>
class op_Implicit
{
    op_Implicit(int x, int y) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>(WellKnownMemberNames.ImplicitConversionName).InstanceConstructors.Single();
            var actualSymbol = GetReferencedSymbol(crefSyntax, compilation);

            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void CrefSymbolInfo()
        {
            var source = @"
/// <summary>
/// See <see cref=""C.M""/>.
/// </summary>
class C
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            var actualSymbol = model.GetSymbolInfo(crefSyntax).Symbol;

            Assert.Equal(expectedSymbol.ISymbol, actualSymbol);
        }

        [Fact]
        public void CrefPartSymbolInfo1()
        {
            var source = @"
/// <summary>
/// See <see cref=""C.M""/>.
/// </summary>
class C
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (QualifiedCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var expectedTypeSymbol = ((Compilation)compilation).GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var expectedMethodSymbol = expectedTypeSymbol.GetMember<IMethodSymbol>("M");

            var actualTypeSymbol = model.GetSymbolInfo(crefSyntax.Container).Symbol;
            Assert.Equal(expectedTypeSymbol, actualTypeSymbol);

            var actualMethodSymbol1 = model.GetSymbolInfo(crefSyntax.Member).Symbol;
            Assert.Equal(actualMethodSymbol1, expectedMethodSymbol);

            var actualMethodSymbol2 = model.GetSymbolInfo(((NameMemberCrefSyntax)crefSyntax.Member).Name).Symbol;
            Assert.Equal(actualMethodSymbol2, expectedMethodSymbol);
        }

        [Fact]
        public void CrefPartSymbolInfo2()
        {
            var source = @"
/// <summary>
/// See <see cref=""A{J}.B{K}.M{L}(int, J, K, L, A{L}, A{int}.B{K})""/>.
/// </summary>
class A<T>
{
    class B<U>
    {
        internal void M<V>(int s, T t, U u, V v, A<V> w, A<int>.B<U> x) { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            var crefSyntax = (QualifiedCrefSyntax)GetCrefSyntaxes(compilation).Single();
            var nameMemberSyntax = (NameMemberCrefSyntax)crefSyntax.Member;
            var containingTypeSyntax = (QualifiedNameSyntax)crefSyntax.Container;

            var typeA = ((Compilation)compilation).GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var typeB = typeA.GetMember<INamedTypeSymbol>("B");
            var method = typeB.GetMember<IMethodSymbol>("M");

            var typeInt = ((Compilation)compilation).GetSpecialType(SpecialType.System_Int32);

            // A{J}
            ITypeParameterSymbol actualJ;
            {
                var left = (GenericNameSyntax)containingTypeSyntax.Left;
                var actualTypeA = (INamedTypeSymbol)model.GetSymbolInfo(left).Symbol;
                Assert.False(actualTypeA.IsDefinition);
                actualJ = (ITypeParameterSymbol)actualTypeA.TypeArguments.Single();
                Assert.Equal(typeA, actualTypeA.OriginalDefinition);

                var actualTypeArgument = model.GetSymbolInfo(left.TypeArgumentList.Arguments.Single()).Symbol;
                Assert.Equal(actualJ, actualTypeArgument);
            }

            // B{K}
            ITypeParameterSymbol actualK;
            {
                var actualTypeB = (INamedTypeSymbol)model.GetSymbolInfo(containingTypeSyntax).Symbol;
                Assert.False(actualTypeB.IsDefinition);
                actualK = (ITypeParameterSymbol)actualTypeB.TypeArguments.Single();
                Assert.Equal(typeB, actualTypeB.OriginalDefinition);

                var right = (GenericNameSyntax)containingTypeSyntax.Right;
                Assert.Equal(actualTypeB, model.GetSymbolInfo(right).Symbol);

                var actualTypeArgument = model.GetSymbolInfo(right.TypeArgumentList.Arguments.Single()).Symbol;
                Assert.Equal(actualK, actualTypeArgument);
            }

            // M{L}
            ITypeParameterSymbol actualL;
            {
                var actualMethod = (IMethodSymbol)model.GetSymbolInfo(crefSyntax).Symbol;
                Assert.False(actualMethod.IsDefinition);
                actualL = (ITypeParameterSymbol)actualMethod.TypeArguments.Single();
                Assert.Equal(method, actualMethod.OriginalDefinition);

                Assert.Equal(actualMethod, model.GetSymbolInfo(crefSyntax.Member).Symbol);
                Assert.Equal(actualMethod, model.GetSymbolInfo(nameMemberSyntax.Name).Symbol);

                var actualParameterTypes = nameMemberSyntax.Parameters.Parameters.Select(syntax => model.GetSymbolInfo(syntax.Type).Symbol).ToArray();
                Assert.Equal(6, actualParameterTypes.Length);
                Assert.Equal(typeInt, actualParameterTypes[0]);
                Assert.Equal(actualJ, actualParameterTypes[1]);
                Assert.Equal(actualK, actualParameterTypes[2]);
                Assert.Equal(actualL, actualParameterTypes[3]);
                Assert.Equal(typeA.Construct(actualL), actualParameterTypes[4]);
                Assert.Equal(typeA.Construct(typeInt).GetMember<INamedTypeSymbol>("B").Construct(actualK), actualParameterTypes[5]);
            }
        }

        [Fact]
        public void IndexerCrefSymbolInfo()
        {
            var source = @"
/// <summary>
/// See <see cref=""this[int]""/>.
/// </summary>
class C
{
    int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (IndexerMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var expectedIndexer = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single().ISymbol;
            var actualIndexer = model.GetSymbolInfo(crefSyntax).Symbol;
            Assert.Equal(expectedIndexer, actualIndexer);

            var expectedParameterType = compilation.GetSpecialType(SpecialType.System_Int32).ISymbol;
            var actualParameterType = model.GetSymbolInfo(crefSyntax.Parameters.Parameters.Single().Type).Symbol;
            Assert.Equal(expectedParameterType, actualParameterType);
        }

        [Fact]
        public void OperatorCrefSymbolInfo()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator +(C)""/>.
/// </summary>
class C
{
    public static int operator +(C c) { return 0; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (OperatorMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var typeC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var expectedOperator = typeC.GetMember<MethodSymbol>(WellKnownMemberNames.UnaryPlusOperatorName).ISymbol;
            var actualOperator = model.GetSymbolInfo(crefSyntax).Symbol;
            Assert.Equal(expectedOperator, actualOperator);

            var expectedParameterType = typeC.ISymbol;
            var actualParameterType = model.GetSymbolInfo(crefSyntax.Parameters.Parameters.Single().Type).Symbol;
            Assert.Equal(expectedParameterType, actualParameterType);
        }

        [Fact]
        public void ConversionOperatorCrefSymbolInfo()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(C)""/>.
/// </summary>
class C
{
    public static implicit operator int(C c) { return 0; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (ConversionOperatorMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var typeC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var expectedOperator = typeC.GetMember<MethodSymbol>(WellKnownMemberNames.ImplicitConversionName).ISymbol;
            var actualOperator = model.GetSymbolInfo(crefSyntax).Symbol;
            Assert.Equal(expectedOperator, actualOperator);

            var expectedParameterType = typeC.ISymbol;
            var actualParameterType = model.GetSymbolInfo(crefSyntax.Parameters.Parameters.Single().Type).Symbol;
            Assert.Equal(expectedParameterType, actualParameterType);

            var expectedReturnType = compilation.GetSpecialType(SpecialType.System_Int32).ISymbol;
            var actualReturnType = model.GetSymbolInfo(crefSyntax.Type).Symbol;
            Assert.Equal(expectedReturnType, actualReturnType);
        }

        [Fact]
        public void CrefSymbolInfo_None()
        {
            var source = @"
/// <summary>
/// See <see cref=""C.N""/>.
/// </summary>
class C
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [Fact]
        public void CrefSymbolInfo_Ambiguous1()
        {
            var source = @"
/// <summary>
/// See <see cref=""M()""/>.
/// </summary>
class C
{
    int M { get; set; }
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, info.CandidateReason); // Candidates have different kinds.
            Assert.Equal(2, info.CandidateSymbols.Length);
        }

        [Fact]
        public void CrefSymbolInfo_Ambiguous2()
        {
            var source = @"
/// <summary>
/// See <see cref=""M""/>.
/// </summary>
class C
{
    void M() { }
    void M(int x) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, info.CandidateReason); // No parameter list.
            Assert.Equal(2, info.CandidateSymbols.Length);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution1()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A}.M(A)""/>.
/// </summary>
class C<T, U>
{
    void M(T t) { }
    void M(U u) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal(MethodKind.Ordinary, ((IMethodSymbol)info.CandidateSymbols[0]).MethodKind);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution2()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A}.this[A]""/>.
/// </summary>
class C<T, U>
{
    int this[T t] { }
    int this[U u] { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.True(((IPropertySymbol)info.CandidateSymbols[0]).IsIndexer);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution3()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A}.explicit operator C{A, A}(A)""/>.
/// </summary>
class C<T, U>
{
    public static explicit operator C<T, U>(T t) { return null; }
    public static explicit operator C<T, U>(U u) { return null; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal(MethodKind.Conversion, ((IMethodSymbol)info.CandidateSymbols[0]).MethodKind);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution4()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A}.operator +(C{A, A}, A)""/>.
/// </summary>
class C<T, U>
{
    public static object operator +(C<T, U> c, T t) { return null; }
    public static object operator +(C<T, U> c, U u) { return null; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal(MethodKind.UserDefinedOperator, ((IMethodSymbol)info.CandidateSymbols[0]).MethodKind);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution5()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{A, A}(A)""/>.
/// </summary>
class C<T, U>
{
    C(T t) { }
    C(U u) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)info.CandidateSymbols[0]).MethodKind);
        }

        [Fact]
        public void CrefSymbolInfo_OverloadResolution()
        {
            var source = @"
/// <summary>
/// See <see cref=""C.N""/>.
/// </summary>
class C
{
    void M() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [Fact]
        public void CrefLookup()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{U}""/>.
/// </summary>
class C<T>
{
    void M() { }
}

class Outer
{
    private class Inner { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var global = compilation.GlobalNamespace;
            var typeC = global.GetMember<NamedTypeSymbol>("C");
            var methodM = typeC.GetMember<MethodSymbol>("M");
            var typeOuter = global.GetMember<NamedTypeSymbol>("Outer");
            var typeInner = typeOuter.GetMember<NamedTypeSymbol>("Inner");

            int position = source.IndexOf("{U}", StringComparison.Ordinal);

            AssertEx.SetEqual(model.LookupSymbols(position).Select(SymbolExtensions.ToTestDisplayString),
                // Implicit type parameter
                "U",

                // From source declarations
                "T",
                "void C<T>.M()",
                "C<T>",

                // Boring
                "System",

                // Inaccessible and boring
                "FXAssembly",
                "ThisAssembly",
                "AssemblyRef",
                "SRETW",
                "Outer",
                "Microsoft");

            // Consider inaccessible symbols, as in Dev11
            Assert.Equal(typeInner.GetPublicSymbol(), model.LookupSymbols(position, typeOuter.GetPublicSymbol(), typeInner.Name).Single());
        }

        [Fact]
        public void InvalidIdentifier()
        {
            var source = @"
/// <summary>
/// Error <see cref=""2""/>
/// Error <see cref=""3A""/>
/// Error <see cref=""@4""/>
/// Error <see cref=""&#64;5""/>
/// </summary>
class C
{
}
";
            // CONSIDER: The "Unexpected character" warnings are redundant.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,22): warning CS1584: XML comment has syntactically incorrect cref attribute '2'
                // /// Error <see cref="2"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "2").WithArguments("2"),
                // (3,22): warning CS1658: Identifier expected. See also error CS1001.
                // /// Error <see cref="2"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "2").WithArguments("Identifier expected", "1001"),
                // (3,22): warning CS1658: Unexpected character '2'. See also error CS1056.
                // /// Error <see cref="2"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '2'", "1056"),

                // (4,22): warning CS1584: XML comment has syntactically incorrect cref attribute '3A'
                // /// Error <see cref="3A"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "3").WithArguments("3A"),
                // (4,22): warning CS1658: Identifier expected. See also error CS1001.
                // /// Error <see cref="3A"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "3").WithArguments("Identifier expected", "1001"),
                // (4,22): warning CS1658: Unexpected character '3'. See also error CS1056.
                // /// Error <see cref="3A"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '3'", "1056"),

                // (5,22): warning CS1584: XML comment has syntactically incorrect cref attribute '@4'
                // /// Error <see cref="@4"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "@").WithArguments("@4"),
                // (5,22): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // /// Error <see cref="@4"/>
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""),
                // (5,23): warning CS1658: Unexpected character '4'. See also error CS1056.
                // /// Error <see cref="@4"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '4'", "1056"),

                // (6,22): warning CS1584: XML comment has syntactically incorrect cref attribute '&#64;5'
                // /// Error <see cref="&#64;5"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "&#64;").WithArguments("&#64;5"),
                // (6,22): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // /// Error <see cref="&#64;5"/>
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""),
                // (6,27): warning CS1658: Unexpected character '5'. See also error CS1056.
                // /// Error <see cref="&#64;5"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '5'", "1056"));
        }

        [Fact]
        public void InvalidIdentifier2()
        {
            var source = @"
/// <summary>
/// Error <see cref=""G&lt;3&gt;""/>
/// Error <see cref=""G{T}.M&lt;3&gt;""/>
/// </summary>
class G<T>
{
    void M<U>(G<G<U>>) { }
}
";
            // CONSIDER: There's room for improvement here, but it's a corner case.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G&lt;3&gt;'
                // /// Error <see cref="G&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G&lt;").WithArguments("G&lt;3&gt;"),
                // (3,27): warning CS1658: Identifier expected. See also error CS1001.
                // /// Error <see cref="G&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "3").WithArguments("Identifier expected", "1001"),
                // (3,27): warning CS1658: Syntax error, '>' expected. See also error CS1003.
                // /// Error <see cref="G&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "3").WithArguments("Syntax error, '>' expected", "1003"),
                // (3,27): warning CS1658: Unexpected character '3'. See also error CS1056.
                // /// Error <see cref="G&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '3'", "1056"),
                // (4,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{T}.M&lt;3&gt;'
                // /// Error <see cref="G{T}.M&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{T}.M&lt;").WithArguments("G{T}.M&lt;3&gt;"),
                // (4,32): warning CS1658: Identifier expected. See also error CS1001.
                // /// Error <see cref="G{T}.M&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "3").WithArguments("Identifier expected", "1001"),
                // (4,32): warning CS1658: Syntax error, '>' expected. See also error CS1003.
                // /// Error <see cref="G{T}.M&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "3").WithArguments("Syntax error, '>' expected", "1003"),
                // (4,32): warning CS1658: Unexpected character '3'. See also error CS1056.
                // /// Error <see cref="G{T}.M&lt;3&gt;"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '3'", "1056"),
                // (8,22): error CS1001: Identifier expected
                //     void M<U>(G<G<U>>) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [Fact]
        public void ERR_TypeParamMustBeIdentifier2()
        {
            var source = @"
/// <summary>
/// Error <see cref=""G{int}""/>
/// Error <see cref=""G{A.B}""/>
/// Error <see cref=""G{G{T}}}""/>
///
/// Error <see cref=""G{T}.M{int}""/>
/// Error <see cref=""G{T}.M{A.B}""/>
/// Error <see cref=""G{T}.M{G{T}}""/>
/// 
/// Fine <see cref=""G{T}.M{U}(int)""/>
/// Fine <see cref=""G{T}.M{U}(A.B)""/>
/// Fine <see cref=""G{T}.M{U}(G{G{U}})""/>
/// </summary>
class G<T>
{
    void M<U>(int x) { }
    void M<U>(A.B b) { }
    void M<U>(G<G<U>> g) { }
}

class A
{
    public class B 
    {
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{int}'
                // /// Error <see cref="G{int}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{int}").WithArguments("G{int}"),
                // (3,24): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{int}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (4,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{A.B}'
                // /// Error <see cref="G{A.B}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{A.B}").WithArguments("G{A.B}"),
                // (4,24): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{A.B}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "A.B").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (5,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{G{T}}}'
                // /// Error <see cref="G{G{T}}}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{G{T}}").WithArguments("G{G{T}}}"),
                // (5,24): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{G{T}}}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "G{T}").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (7,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{T}.M{int}'
                // /// Error <see cref="G{T}.M{int}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{T}.M{int}").WithArguments("G{T}.M{int}"),
                // (7,29): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{T}.M{int}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (8,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{T}.M{A.B}'
                // /// Error <see cref="G{T}.M{A.B}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{T}.M{A.B}").WithArguments("G{T}.M{A.B}"),
                // (8,29): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{T}.M{A.B}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "A.B").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (9,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'G{T}.M{G{T}}'
                // /// Error <see cref="G{T}.M{G{T}}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "G{T}.M{G{T}}").WithArguments("G{T}.M{G{T}}"),
                // (9,29): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="G{T}.M{G{T}}"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "G{T}").WithArguments("Type parameter declaration must be an identifier not a type", "0081"));
        }

        [Fact]
        public void WRN_DuplicateParamTag()
        {
            var source = @"
class C
{
    /// <param name=""x""/>
    /// <param name=""x""/> -- warning
    void M(int x) { }

    /// <param name=""value""/>
    /// <param name=""value""/> -- fine, as in dev11
    int P { get; set; }

    /// <param name=""x""/>
    /// <param name=""y""/>
    /// <param name=""value""/>
    /// <param name=""x""/> -- warning
    /// <param name=""y""/> -- warning
    /// <param name=""value""/> -- fine, as in dev11
    int this[int x, int y] { get { return 0; } set { } }
}

partial class P
{
    /// <param name=""x""/>
    partial void M(int x);
}

partial class P
{
    /// <param name=""x""/> -- fine, other is dropped
    partial void M(int x) { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (5,16): warning CS1571: XML comment has a duplicate param tag for 'x'
                //     /// <param name="x"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""x""").WithArguments("x"),
                // (15,16): warning CS1571: XML comment has a duplicate param tag for 'x'
                //     /// <param name="x"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""x""").WithArguments("x"),
                // (16,16): warning CS1571: XML comment has a duplicate param tag for 'y'
                //     /// <param name="y"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""y""").WithArguments("y"));
        }

        [Fact]
        public void WRN_UnmatchedParamTag()
        {
            var source = @"
class C
{
    /// <param name=""q""/>
    /// <param name=""value""/>
    void M(int x) { }

    /// <param name=""x""/>
    int P { get; set; }

    /// <param name=""q""/>
    int this[int x, int y] { get { return 0; } set { } }

    /// <param name=""q""/>
    /// <param name=""value""/>
    event System.Action E;
}

partial class P
{
    /// <param name=""x""/>
    partial void M(int y);
}

partial class P
{
    /// <param name=""y""/>
    partial void M(int x) { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (28,18): warning CS8826: Partial method declarations 'void P.M(int y)' and 'void P.M(int x)' have signature differences.
                //     partial void M(int x) { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void P.M(int y)", "void P.M(int x)").WithLocation(28, 18),
                // (16,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(16, 25),
                // (4,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q").WithLocation(4, 22),
                // (5,22): warning CS1572: XML comment has a param tag for 'value', but there is no parameter by that name
                //     /// <param name="value"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "value").WithArguments("value").WithLocation(5, 22),
                // (6,16): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.M(int)' (but other parameters do)
                //     void M(int x) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.M(int)").WithLocation(6, 16),
                // (8,22): warning CS1572: XML comment has a param tag for 'x', but there is no parameter by that name
                //     /// <param name="x"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "x").WithArguments("x").WithLocation(8, 22),
                // (11,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q").WithLocation(11, 22),
                // (12,18): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.this[int, int]' (but other parameters do)
                //     int this[int x, int y] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.this[int, int]").WithLocation(12, 18),
                // (12,25): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'C.this[int, int]' (but other parameters do)
                //     int this[int x, int y] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "C.this[int, int]").WithLocation(12, 25),
                // (14,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q").WithLocation(14, 22),
                // (15,22): warning CS1572: XML comment has a param tag for 'value', but there is no parameter by that name
                //     /// <param name="value"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "value").WithArguments("value").WithLocation(15, 22),
                // (27,22): warning CS1572: XML comment has a param tag for 'y', but there is no parameter by that name
                //     /// <param name="y"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "y").WithArguments("y").WithLocation(27, 22),
                // (28,24): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'P.M(int)' (but other parameters do)
                //     partial void M(int x) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "P.M(int)").WithLocation(28, 24),
                // (21,22): warning CS1572: XML comment has a param tag for 'x', but there is no parameter by that name
                //     /// <param name="x"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "x").WithArguments("x").WithLocation(21, 22),
                // (22,24): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'P.M(int)' (but other parameters do)
                //     partial void M(int y);
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "P.M(int)").WithLocation(22, 24));
        }

        [Fact]
        public void WRN_MissingParamTag()
        {
            var source = @"
class C
{
    /// <param name=""x""/>
    void M(int x, int y) { }

    /// <param name=""x""/>
    int this[int x, int y] { get { return 0; } set { } }

    /// <param name=""value""/>
    int this[int x] { get { return 0; } set { } }
}

partial class P
{
    /// <param name=""q""/>
    partial void M(int q, int r);
}

partial class P
{
    /// <param name=""x""/>
    partial void M(int x, int y) { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (23,18): warning CS8826: Partial method declarations 'void P.M(int q, int r)' and 'void P.M(int x, int y)' have signature differences.
                //     partial void M(int x, int y) { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void P.M(int q, int r)", "void P.M(int x, int y)").WithLocation(23, 18),
                // (5,23): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'C.M(int, int)' (but other parameters do)
                //     void M(int x, int y) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "C.M(int, int)").WithLocation(5, 23),
                // (8,25): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'C.this[int, int]' (but other parameters do)
                //     int this[int x, int y] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "C.this[int, int]").WithLocation(8, 25),
                // (11,18): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.this[int]' (but other parameters do)
                //     int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.this[int]").WithLocation(11, 18),
                // (23,31): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'P.M(int, int)' (but other parameters do)
                //     partial void M(int x, int y) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "P.M(int, int)").WithLocation(23, 31),
                // (17,31): warning CS1573: Parameter 'r' has no matching param tag in the XML comment for 'P.M(int, int)' (but other parameters do)
                //     partial void M(int q, int r);
                Diagnostic(ErrorCode.WRN_MissingParamTag, "r").WithArguments("r", "P.M(int, int)").WithLocation(17, 31));
        }

        [Fact]
        public void WRN_DuplicateTypeParamTag()
        {
            var source = @"
/// <typeparam name=""T""/>
/// <typeparam name=""T""/> -- warning
class C<T>
{
    /// <typeparam name=""U""/>
    /// <typeparam name=""U""/> -- warning
    void M<U>() { }
}

/// <typeparam name=""T""/>
partial class P<T>
{
    /// <typeparam name=""U""/>
    partial void M1<U>();

    /// <typeparam name=""U""/>
    /// <typeparam name=""U""/> -- warning
    partial void M2<U>();
}

/// <typeparam name=""T""/> -- warning
partial class P<T>
{
    /// <typeparam name=""U""/> -- fine, other is dropped
    partial void M1<U>() { }

    /// <typeparam name=""U""/>
    /// <typeparam name=""U""/> -- warning
    partial void M2<U>() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,16): warning CS1710: XML comment has a duplicate typeparam tag for 'T'
                // /// <typeparam name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""T""").WithArguments("T"),
                // (7,20): warning CS1710: XML comment has a duplicate typeparam tag for 'U'
                //     /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""U""").WithArguments("U"),
                // (22,16): warning CS1710: XML comment has a duplicate typeparam tag for 'T'
                // /// <typeparam name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""T""").WithArguments("T"),
                // (29,20): warning CS1710: XML comment has a duplicate typeparam tag for 'U'
                //     /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""U""").WithArguments("U"),
                // (18,20): warning CS1710: XML comment has a duplicate typeparam tag for 'U'
                //     /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, @"name=""U""").WithArguments("U"));
        }

        [Fact]
        public void WRN_UnmatchedParamRefTag()
        {
            var source = @"
class C
{
    /// <paramref name=""q""/>
    /// <paramref name=""value""/>
    void M(int x) { }

    /// <paramref name=""x""/>
    int P { get; set; }

    /// <paramref name=""q""/>
    int this[int x, int y] { get { return 0; } set { } }

    /// <paramref name=""q""/>
    /// <paramref name=""value""/>
    event System.Action E;
}

partial class P
{
    /// <paramref name=""x""/>
    partial void M(int y);
}

partial class P
{
    /// <paramref name=""y""/>
    partial void M(int x) { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (28,18): warning CS8826: Partial method declarations 'void P.M(int y)' and 'void P.M(int x)' have signature differences.
                //     partial void M(int x) { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void P.M(int y)", "void P.M(int x)").WithLocation(28, 18),
                // (16,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(16, 25),
                // (4,25): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'q', but there is no parameter by that name
                //     /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "C.M(int)").WithLocation(4, 25),
                // (5,25): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'value', but there is no parameter by that name
                //     /// <paramref name="value"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "value").WithArguments("value", "C.M(int)").WithLocation(5, 25),
                // (8,25): warning CS1734: XML comment on 'C.P' has a paramref tag for 'x', but there is no parameter by that name
                //     /// <paramref name="x"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "x").WithArguments("x", "C.P").WithLocation(8, 25),
                // (11,25): warning CS1734: XML comment on 'C.this[int, int]' has a paramref tag for 'q', but there is no parameter by that name
                //     /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "C.this[int, int]").WithLocation(11, 25),
                // (14,25): warning CS1734: XML comment on 'C.E' has a paramref tag for 'q', but there is no parameter by that name
                //     /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "C.E").WithLocation(14, 25),
                // (15,25): warning CS1734: XML comment on 'C.E' has a paramref tag for 'value', but there is no parameter by that name
                //     /// <paramref name="value"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "value").WithArguments("value", "C.E").WithLocation(15, 25),
                // (27,25): warning CS1734: XML comment on 'P.M(int)' has a paramref tag for 'y', but there is no parameter by that name
                //     /// <paramref name="y"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "y").WithArguments("y", "P.M(int)").WithLocation(27, 25),
                // (21,25): warning CS1734: XML comment on 'P.M(int)' has a paramref tag for 'x', but there is no parameter by that name
                //     /// <paramref name="x"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "x").WithArguments("x", "P.M(int)").WithLocation(21, 25));
        }

        [Fact]
        public void DuplicateParameterName()
        {
            var source = @"
class C
{
    /// <param name=""x""/>
    /// <paramref name=""x""/>
    /// <param name=""q""/>
    /// <paramref name=""q""/>
    void M(int x, int x) { }

    /// <param name=""x""/>
    /// <paramref name=""x""/>
    /// <param name=""q""/>
    /// <paramref name=""q""/>
    int this[int x, int x] { get { return 0; } set { } }

    /// <param name=""q""/>
    void M(double x, double x) { }

    /// <param name=""q""/>
    double this[double x, double x] { get { return 0; } set { } }
}
";
            // These diagnostics don't exactly match dev11, but they seem reasonable and the main point
            // of the test is to confirm that we don't crash.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (17,29): error CS0100: The parameter name 'x' is a duplicate
                //     void M(double x, double x) { }
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x"),
                // (8,23): error CS0100: The parameter name 'x' is a duplicate
                //     void M(int x, int x) { }
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x"), // NOTE: double-reported in dev11
                                                                                      // (14,25): error CS0100: The parameter name 'x' is a duplicate
                                                                                      //     int this[int x, int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x"),
                // (20,34): error CS0100: The parameter name 'x' is a duplicate
                //     double this[double x, double x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x"), // NOTE: double-reported in dev11

                // Dev11 doesn't report these, but they seem reasonable (even desirable).

                // (6,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q"),
                // (7,25): warning CS1734: XML comment on 'C.M(int, int)' has a paramref tag for 'q', but there is no parameter by that name
                //     /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "C.M(int, int)"),

                // These match dev11.

                // (12,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q"),
                // (13,25): warning CS1734: XML comment on 'C.this[int, int]' has a paramref tag for 'q', but there is no parameter by that name
                //     /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "C.this[int, int]"),

                // Dev11 doesn't report these, but they seem reasonable (even desirable).

                // (16,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q"),
                // (17,19): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.M(double, double)' (but other parameters do)
                //     void M(double x, double x) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.M(double, double)"),
                // (17,29): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.M(double, double)' (but other parameters do)
                //     void M(double x, double x) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.M(double, double)"),

                // These match dev11.

                // (19,22): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                //     /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q"),
                // (20,24): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.this[double, double]' (but other parameters do)
                //     double this[double x, double x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.this[double, double]"),
                // (20,34): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.this[double, double]' (but other parameters do)
                //     double this[double x, double x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.this[double, double]"));
        }

        [Fact]
        public void DuplicateTypeParameterName()
        {
            var source = @"
/// <typeparam name=""T""/>
/// <typeparamref name=""T""/>
/// <typeparam name=""Q""/>
/// <typeparamref name=""Q""/>
class C<T, T>
{
    /// <typeparam name=""U""/>
    /// <typeparamref name=""U""/>
    /// <typeparam name=""Q""/>
    /// <typeparamref name=""Q""/>
    void M<U, U>() { }
}

/// <typeparam name=""Q""/>
class D<T, T>
{
    /// <typeparam name=""Q""/>
    void M<U, U>() { }
}
";
            // Dev11 stops after the CS0692s on the types.
            // We just want to confirm that the errors are sensible and we don't crash.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (6,12): error CS0692: Duplicate type parameter 'T'
                // class C<T, T>
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T"),
                // (16,12): error CS0692: Duplicate type parameter 'T'
                // class D<T, T>
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "T").WithArguments("T"),
                // (12,15): error CS0692: Duplicate type parameter 'U'
                //     void M<U, U>() { }
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "U").WithArguments("U"),
                // (19,15): error CS0692: Duplicate type parameter 'U'
                //     void M<U, U>() { }
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "U").WithArguments("U"),

                // (4,22): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                // /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (5,25): warning CS1735: XML comment on 'C<T, T>' has a typeparamref tag for 'Q', but there is no type parameter by that name
                // /// <typeparamref name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "Q").WithArguments("Q", "C<T, T>"),
                // (10,26): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                //     /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (11,29): warning CS1735: XML comment on 'C<T, T>.M<U, U>()' has a typeparamref tag for 'Q', but there is no type parameter by that name
                //     /// <typeparamref name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "Q").WithArguments("Q", "C<T, T>.M<U, U>()"),
                // (15,22): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                // /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (16,9): warning CS1712: Type parameter 'T' has no matching typeparam tag in the XML comment on 'D<T, T>' (but other type parameters do)
                // class D<T, T>
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "T").WithArguments("T", "D<T, T>"),
                // (16,12): warning CS1712: Type parameter 'T' has no matching typeparam tag in the XML comment on 'D<T, T>' (but other type parameters do)
                // class D<T, T>
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "T").WithArguments("T", "D<T, T>"),
                // (18,26): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                //     /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (19,12): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'D<T, T>.M<U, U>()' (but other type parameters do)
                //     void M<U, U>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "D<T, T>.M<U, U>()"),
                // (19,15): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'D<T, T>.M<U, U>()' (but other type parameters do)
                //     void M<U, U>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "D<T, T>.M<U, U>()"));
        }

        [Fact]
        public void WRN_UnmatchedTypeParamTag()
        {
            var source = @"
/// <typeparam name=""T""/> -- warning
class C
{
    /// <typeparam name=""T""/> -- warning
    void M() { }
}

/// <typeparam name=""T""/>
/// <typeparam name=""U""/> -- warning
class C<T>
{
    /// <typeparam name=""U""/>
    /// <typeparam name=""V""/> -- warning
    void M<U>() { }
}

/// <typeparam name=""U""/> -- warning
partial class P<T>
{
    /// <typeparam name=""V""/> -- warning
    partial void M1<U>();
}

/// <typeparam name=""V""/> -- warning
partial class P<T>
{
    /// <typeparam name=""U""/> -- warning
    partial void M1<V>() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (29,18): warning CS8826: Partial method declarations 'void P<T>.M1<U>()' and 'void P<T>.M1<V>()' have signature differences.
                //     partial void M1<V>() { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("void P<T>.M1<U>()", "void P<T>.M1<V>()").WithLocation(29, 18),
                // (2,22): warning CS1711: XML comment has a typeparam tag for 'T', but there is no type parameter by that name
                // /// <typeparam name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "T").WithArguments("T"),
                // (5,26): warning CS1711: XML comment has a typeparam tag for 'T', but there is no type parameter by that name
                //     /// <typeparam name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "T").WithArguments("T"),
                // (10,22): warning CS1711: XML comment has a typeparam tag for 'U', but there is no type parameter by that name
                // /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "U").WithArguments("U"),
                // (14,26): warning CS1711: XML comment has a typeparam tag for 'V', but there is no type parameter by that name
                //     /// <typeparam name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "V").WithArguments("V"),
                // (18,22): warning CS1711: XML comment has a typeparam tag for 'U', but there is no type parameter by that name
                // /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "U").WithArguments("U"),
                // (25,22): warning CS1711: XML comment has a typeparam tag for 'V', but there is no type parameter by that name
                // /// <typeparam name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "V").WithArguments("V"),
                // (28,26): warning CS1711: XML comment has a typeparam tag for 'U', but there is no type parameter by that name
                //     /// <typeparam name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "U").WithArguments("U"),
                // (21,26): warning CS1711: XML comment has a typeparam tag for 'V', but there is no type parameter by that name
                //     /// <typeparam name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "V").WithArguments("V"),

                // (29,21): warning CS1712: Type parameter 'V' has no matching typeparam tag in the XML comment on 'P<T>.M1<V>()' (but other type parameters do)
                //     partial void M1<V>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "V").WithArguments("V", "P<T>.M1<V>()"),
                // (19,17): warning CS1712: Type parameter 'T' has no matching typeparam tag in the XML comment on 'P<T>' (but other type parameters do)
                // partial class P<T>
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "T").WithArguments("T", "P<T>"),
                // (22,21): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'P<T>.M1<U>()' (but other type parameters do)
                //     partial void M1<U>();
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "P<T>.M1<U>()"));
        }

        [Fact]
        public void WRN_MissingTypeParamTag()
        {
            var source = @"
/// <typeparam name=""T""/>
class C<T, U>
{
    /// <typeparam name=""V""/>
    void M<V, W, X>() { }
}

/// <typeparam name=""Q""/>
class C<T>
{
    /// <typeparam name=""Q""/>
    void M<U>() { }
}

/// <typeparam name=""T""/>
partial class P<T, U>
{
    /// <typeparam name=""V""/>
    partial void M1<V, W>();
}

/// <typeparam name=""U""/>
partial class P<T, U>
{
    /// <typeparam name=""W""/>
    partial void M1<V, W>() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,12): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'C<T, U>' (but other type parameters do)
                // class C<T, U>
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "C<T, U>"),
                // (6,15): warning CS1712: Type parameter 'W' has no matching typeparam tag in the XML comment on 'C<T, U>.M<V, W, X>()' (but other type parameters do)
                //     void M<V, W, X>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "W").WithArguments("W", "C<T, U>.M<V, W, X>()"),
                // (6,18): warning CS1712: Type parameter 'X' has no matching typeparam tag in the XML comment on 'C<T, U>.M<V, W, X>()' (but other type parameters do)
                //     void M<V, W, X>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "X").WithArguments("X", "C<T, U>.M<V, W, X>()"),
                // (9,22): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                // /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (10,9): warning CS1712: Type parameter 'T' has no matching typeparam tag in the XML comment on 'C<T>' (but other type parameters do)
                // class C<T>
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "T").WithArguments("T", "C<T>"),
                // (12,26): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                //     /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (13,12): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'C<T>.M<U>()' (but other type parameters do)
                //     void M<U>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "C<T>.M<U>()"),
                // (27,21): warning CS1712: Type parameter 'V' has no matching typeparam tag in the XML comment on 'P<T, U>.M1<V, W>()' (but other type parameters do)
                //     partial void M1<V, W>() { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "V").WithArguments("V", "P<T, U>.M1<V, W>()"),
                // (20,24): warning CS1712: Type parameter 'W' has no matching typeparam tag in the XML comment on 'P<T, U>.M1<V, W>()' (but other type parameters do)
                //     partial void M1<V, W>();
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "W").WithArguments("W", "P<T, U>.M1<V, W>()"));
        }

        [Fact]
        public void WRN_UnmatchedTypeParamRefTag()
        {
            var source = @"
/// <typeparamref name=""T""/> -- warning
class C
{
    /// <typeparamref name=""T""/> -- warning
    void M() { }
}

/// <typeparamref name=""T""/>
/// <typeparamref name=""U""/> -- warning
class C<T>
{
    /// <typeparamref name=""U""/>
    /// <typeparamref name=""V""/> -- warning
    void M<U>() { }
}

/// <typeparamref name=""U""/> -- warning
partial class P<T>
{
    /// <typeparamref name=""V""/> -- warning
    partial void M1<U>();
}

/// <typeparamref name=""V""/> -- warning
partial class P<T>
{
    /// <typeparamref name=""U""/> -- warning
    partial void M1<V>() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (29,18): warning CS8826: Partial method declarations 'void P<T>.M1<U>()' and 'void P<T>.M1<V>()' have signature differences.
                //     partial void M1<V>() { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M1").WithArguments("void P<T>.M1<U>()", "void P<T>.M1<V>()").WithLocation(29, 18),
                // (2,25): warning CS1735: XML comment on 'C' has a typeparamref tag for 'T', but there is no type parameter by that name
                // /// <typeparamref name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "T").WithArguments("T", "C"),
                // (5,29): warning CS1735: XML comment on 'C.M()' has a typeparamref tag for 'T', but there is no type parameter by that name
                //     /// <typeparamref name="T"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "T").WithArguments("T", "C.M()"),
                // (10,25): warning CS1735: XML comment on 'C<T>' has a typeparamref tag for 'U', but there is no type parameter by that name
                // /// <typeparamref name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "U").WithArguments("U", "C<T>"),
                // (14,29): warning CS1735: XML comment on 'C<T>.M<U>()' has a typeparamref tag for 'V', but there is no type parameter by that name
                //     /// <typeparamref name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "V").WithArguments("V", "C<T>.M<U>()"),
                // (18,25): warning CS1735: XML comment on 'P<T>' has a typeparamref tag for 'U', but there is no type parameter by that name
                // /// <typeparamref name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "U").WithArguments("U", "P<T>"),
                // (25,25): warning CS1735: XML comment on 'P<T>' has a typeparamref tag for 'V', but there is no type parameter by that name
                // /// <typeparamref name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "V").WithArguments("V", "P<T>"),
                // (28,29): warning CS1735: XML comment on 'P<T>.M1<V>()' has a typeparamref tag for 'U', but there is no type parameter by that name
                //     /// <typeparamref name="U"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "U").WithArguments("U", "P<T>.M1<V>()"),
                // (21,29): warning CS1735: XML comment on 'P<T>.M1<U>()' has a typeparamref tag for 'V', but there is no type parameter by that name
                //     /// <typeparamref name="V"/> -- warning
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "V").WithArguments("V", "P<T>.M1<U>()"));
        }

        [Fact]
        public void WRN_MissingXMLComment_Accessibility()
        {
            var source = @"
/// <summary/>
public class C
{
    public void M1() { }
    protected internal void M2() { }
    protected void M3() { }
    internal void M4() { }
    private void M5() { }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (5,17): warning CS1591: Missing XML comment for publicly visible type or member 'C.M1()'
                //     public void M1() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M1").WithArguments("C.M1()"),
                // (6,29): warning CS1591: Missing XML comment for publicly visible type or member 'C.M2()'
                //     protected internal void M2() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M2").WithArguments("C.M2()"),
                // (7,20): warning CS1591: Missing XML comment for publicly visible type or member 'C.M3()'
                //     protected void M3() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M3").WithArguments("C.M3()"));
        }

        [Fact]
        public void WRN_MissingXMLComment_EffectiveAccessibility()
        {
            var source = @"
/// <summary/>
public class A
{
    /// <summary/>
    public class B1
    {
        /// <summary/>
        public class C
        {
            public void M1() { }
        }
    }

    /// <summary/>
    protected internal class B2
    {
        /// <summary/>
        public class C
        {
            public void M2() { }
        }
    }

    /// <summary/>
    protected class B3
    {
        /// <summary/>
        public class C
        {
            public void M3() { }
        }
    }

    internal class B4
    {
        public class C
        {
            public void M4() { }
        }
    }

    private class B5
    {
        public class C
        {
            public void M5() { }
        }
    }
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (11,25): warning CS1591: Missing XML comment for publicly visible type or member 'A.B1.C.M1()'
                //             public void M1() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M1").WithArguments("A.B1.C.M1()"),
                // (21,25): warning CS1591: Missing XML comment for publicly visible type or member 'A.B2.C.M2()'
                //             public void M2() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M2").WithArguments("A.B2.C.M2()"),
                // (31,25): warning CS1591: Missing XML comment for publicly visible type or member 'A.B3.C.M3()'
                //             public void M3() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M3").WithArguments("A.B3.C.M3()"));
        }

        [Fact]
        public void WRN_MissingXMLComment_Kind()
        {
            var source = @"
/// <summary/>
public class C
{
    public class Class { }
    public void Method() { }
    public int Field;
    public int Property { get; set; }
    public int this[int x] { get { return 0; } set { } }
    public event System.Action FieldLikeEvent;
    public event System.Action Event { add { } remove { } }
    public delegate void Delegate();
}
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (5,18): warning CS1591: Missing XML comment for publicly visible type or member 'C.Class'
                //     public class Class { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Class").WithArguments("C.Class"),
                // (6,17): warning CS1591: Missing XML comment for publicly visible type or member 'C.Method()'
                //     public void Method() { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Method").WithArguments("C.Method()"),
                // (7,16): warning CS1591: Missing XML comment for publicly visible type or member 'C.Field'
                //     public int Field;
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Field").WithArguments("C.Field"),
                // (8,16): warning CS1591: Missing XML comment for publicly visible type or member 'C.Property'
                //     public int Property { get; set; }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Property").WithArguments("C.Property"),
                // (9,16): warning CS1591: Missing XML comment for publicly visible type or member 'C.this[int]'
                //     public int this[int x] { get { return 0; } set { } }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "this").WithArguments("C.this[int]"),
                // (10,32): warning CS1591: Missing XML comment for publicly visible type or member 'C.FieldLikeEvent'
                //     public event System.Action FieldLikeEvent;
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "FieldLikeEvent").WithArguments("C.FieldLikeEvent"),
                // (11,32): warning CS1591: Missing XML comment for publicly visible type or member 'C.Event'
                //     public event System.Action Event { add { } remove { } }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Event").WithArguments("C.Event"),
                // (12,26): warning CS1591: Missing XML comment for publicly visible type or member 'C.Delegate'
                //     public delegate void Delegate();
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Delegate").WithArguments("C.Delegate"),

                // (10,32): warning CS0067: The event 'C.FieldLikeEvent' is never used
                //     public event System.Action FieldLikeEvent;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "FieldLikeEvent").WithArguments("C.FieldLikeEvent"));
        }

        [Fact]
        public void WRN_MissingXMLComment_Interface()
        {
            var source = @"
interface I
{
    void M();
}
";
            // As in dev11, doesn't count since the *declared* accessibility is not public.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics();
        }

        [Fact]
        public void WRN_MissingXMLComment_PartialClass()
        {
            var source = @"
/// <summary/>
public partial class C { }
public partial class C { }

public partial class D { }
public partial class D { }
";
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (6,22): warning CS1591: Missing XML comment for publicly visible type or member 'D'
                // public partial class D { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "D").WithArguments("D"));
        }

        [Fact]
        public void WRN_MissingXMLComment_DifferentOptions()
        {
            var source1 = @"
/// <summary/>
public partial class C { }

public partial class D { }

public partial class E { }
";
            var source2 = @"
public partial class C { }

/// <summary/>
public partial class D { }

public partial class E { }
";

            var tree1 = Parse(source1, options: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose).WithLanguageVersion(LanguageVersion.Latest));
            var tree2 = Parse(source2, options: TestOptions.Regular.WithDocumentationMode(DocumentationMode.None).WithLanguageVersion(LanguageVersion.Latest));

            // This scenario does not exist in dev11, but the diagnostics seem reasonable.
            CreateCompilation(new[] { tree1, tree2 }).VerifyDiagnostics(
                // (5,22): warning CS1591: Missing XML comment for publicly visible type or member 'D'
                // public partial class D { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "D").WithArguments("D").WithLocation(5, 22),
                // (7,22): warning CS1591: Missing XML comment for publicly visible type or member 'E'
                // public partial class E { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "E").WithArguments("E").WithLocation(7, 22));
        }

        [Fact]
        public void WRN_BadXMLRefParamType()
        {
            var source = @"
/// <see cref=""M(Q)""/>
/// <see cref=""M(C{Q})""/>
/// <see cref=""M(Q[])""/>
/// <see cref=""M(Q*)""/>
class C
{
    void M(int x) { }
}
";

            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (2,16): warning CS1580: Invalid type for parameter 'Q' in XML comment cref attribute: 'M(Q)'
                // /// <see cref="M(Q)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "Q").WithArguments("Q", "M(Q)"),
                // (2,16): warning CS1574: XML comment has cref attribute 'M(Q)' that could not be resolved
                // /// <see cref="M(Q)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(Q)").WithArguments("M(Q)"),
                // (3,16): warning CS1580: Invalid type for parameter 'C{Q}' in XML comment cref attribute: 'M(C{Q})'
                // /// <see cref="M(C{Q})"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "C{Q}").WithArguments("C{Q}", "M(C{Q})"),
                // (3,16): warning CS1574: XML comment has cref attribute 'M(C{Q})' that could not be resolved
                // /// <see cref="M(C{Q})"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(C{Q})").WithArguments("M(C{Q})"),
                // (4,16): warning CS1580: Invalid type for parameter 'Q[]' in XML comment cref attribute: 'M(Q[])'
                // /// <see cref="M(Q[])"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "Q[]").WithArguments("Q[]", "M(Q[])"),
                // (4,16): warning CS1574: XML comment has cref attribute 'M(Q[])' that could not be resolved
                // /// <see cref="M(Q[])"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(Q[])").WithArguments("M(Q[])"),
                // (5,16): warning CS1580: Invalid type for parameter 'Q*' in XML comment cref attribute: 'M(Q*)'
                // /// <see cref="M(Q*)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "Q*").WithArguments("Q*", "M(Q*)"),
                // (5,16): warning CS1574: XML comment has cref attribute 'M(Q*)' that could not be resolved
                // /// <see cref="M(Q*)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(Q*)").WithArguments("M(Q*)"));
        }

        [Fact]
        public void WRN_BadXMLRefReturnType()
        {
            var source = @"
/// <see cref=""explicit operator Q""/>
/// <see cref=""explicit operator C{Q}""/>
/// <see cref=""explicit operator Q[]""/>
/// <see cref=""explicit operator Q*""/>
class C
{
    public static explicit operator int(C c) { return 0; }
}
";

            // BREAK: dev11 doesn't report CS1581 for "Q[]" or "Q*" because it only checks for error
            // types and it finds an array type and a pointer type, respectively.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (2,34): warning CS1581: Invalid return type in XML comment cref attribute
                // /// <see cref="explicit operator Q"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefReturnType, "Q").WithLocation(2, 34),
                // (2,16): warning CS1574: XML comment has cref attribute 'explicit operator Q' that could not be resolved
                // /// <see cref="explicit operator Q"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator Q").WithArguments("explicit operator Q").WithLocation(2, 16),
                // (3,34): warning CS1581: Invalid return type in XML comment cref attribute
                // /// <see cref="explicit operator C{Q}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefReturnType, "C{Q}").WithLocation(3, 34),
                // (3,16): warning CS1574: XML comment has cref attribute 'explicit operator C{Q}' that could not be resolved
                // /// <see cref="explicit operator C{Q}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator C{Q}").WithArguments("explicit operator C{Q}").WithLocation(3, 16),
                // (4,34): warning CS1581: Invalid return type in XML comment cref attribute
                // /// <see cref="explicit operator Q[]"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefReturnType, "Q[]").WithLocation(4, 34),
                // (4,16): warning CS1574: XML comment has cref attribute 'explicit operator Q[]' that could not be resolved
                // /// <see cref="explicit operator Q[]"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator Q[]").WithArguments("explicit operator Q[]").WithLocation(4, 16),
                // (5,34): warning CS1581: Invalid return type in XML comment cref attribute
                // /// <see cref="explicit operator Q*"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefReturnType, "Q*").WithLocation(5, 34),
                // (5,16): warning CS1574: XML comment has cref attribute 'explicit operator Q*' that could not be resolved
                // /// <see cref="explicit operator Q*"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator Q*").WithArguments("explicit operator Q*").WithLocation(5, 16));
        }

        [Fact]
        public void WRN_BadXMLRefTypeVar()
        {
            // NOTE: there isn't a corresponding case for indexers since they use an impossible member name.
            var source = @"
class C<T, op_Explicit, op_Division>
{
    /// <see cref=""T""/>
    /// <see cref=""explicit operator int""/>
    /// <see cref=""operator /""/>
    void M() { }
}
";

            // BREAK: Dev11 reports WRN_BadXMLRef, instead of WRN_BadXMLRefTypeVar, for the conversion operator.
            // This seems like a bug; it binds to the type parameter, but throw it away because it's not a conversion
            // method.  On its own, this seems reasonable, but it actually performs this filtering *after* accepting
            // type symbols for crefs without parameter lists (see Conversion_Type()).  Therefore, conversion crefs
            // can bind to aggregates, but not type parameters.  To be both more consistent and more permissive,
            // Roslyn binds to the type parameter and produces a more specific error messages.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (4,20): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                //     /// <see cref="T"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"),
                // (5,20): warning CS1723: XML comment has cref attribute 'explicit operator int' that refers to a type parameter
                //     /// <see cref="explicit operator int"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "explicit operator int").WithArguments("explicit operator int"),
                // (6,20): warning CS1723: XML comment has cref attribute 'operator /' that refers to a type parameter
                //     /// <see cref="operator /"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "operator /").WithArguments("operator /"));
        }

        [WorkItem(530970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530970")]
        [Fact]
        public void DanglingDocComment()
        {
            var source = @"
/// <summary>
/// See <see cref=""C""/>.
/// </summary>
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            Assert.Equal(SyntaxKind.EndOfFileToken, crefSyntax.Ancestors().First(n => n.IsStructuredTrivia).ParentTrivia.Token.Kind());
            model.GetSymbolInfo(crefSyntax);
        }

        [WorkItem(530969, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530969")]
        [Fact]
        public void MissingCrefTypeParameter()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{}""/>.
/// </summary>
class C<T> { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            model.GetSymbolInfo(crefSyntax);
            model.GetSymbolInfo(((GenericNameSyntax)crefSyntax.Name).TypeArgumentList.Arguments.Single());
        }

        [WorkItem(530969, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530969")]
        [Fact]
        public void InvalidCrefTypeParameter()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{&}""/>.
/// </summary>
class C<T> { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            model.GetSymbolInfo(crefSyntax);
            model.GetSymbolInfo(((GenericNameSyntax)crefSyntax.Name).TypeArgumentList.Arguments.Single());
        }

        [Fact]
        public void GenericTypeArgument()
        {
            var source = @"
/// <summary>
/// See <see cref=""C{C{T}}""/>.
/// </summary>
class C<T> { }
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            model.GetSymbolInfo(crefSyntax);
            model.GetSymbolInfo(((GenericNameSyntax)crefSyntax.Name).TypeArgumentList.Arguments.Single());
        }

        [Fact]
        public void CrefAttributeNameCaseMismatch()
        {
            var source = @"
/// <summary>
/// See <see Cref=""C{C{T}}""/>.
/// </summary>
class C<T> { }
";

            // Element names don't have to be lowercase, but "cref" does.
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();
            AssertEx.None(GetCrefSyntaxes(compilation), x => true);
        }

        [WorkItem(546965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546965")]
        [Fact]
        public void MultipleCrefs()
        {
            var source = @"
/// <summary>
/// See <see cref=""int""/>.
/// See <see cref=""C{T}""/>.
/// </summary>
class C<T> { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntaxes = GetCrefSyntaxes(compilation);

            // Make sure we're not reusing the binder from the first cref (no type parameters)
            // for the second cref (has type parameters).
            model.GetSymbolInfo(crefSyntaxes.ElementAt(0));
            model.GetSymbolInfo(crefSyntaxes.ElementAt(1));
        }

        [WorkItem(546992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546992")]
        [Fact]
        public void NestedGenerics()
        {
            var source = @"
/// <summary>
/// Error <see cref=""A{A{T}}""/>.
/// Error <see cref=""A{T}.B{A{T}}""/>.
/// Error <see cref=""A{T}.B{U}.M{A{T}}""/>.
/// Fine <see cref=""A{T}.B{U}.M{V}(A{A{T}})""/>.
/// Fine <see cref=""A{T}.B{U}.explicit operator A{A{T}}""/>.
/// </summary>
class A<T>
{
    class B<U>
    {
        internal void M<V>(A<A<T>> a) { }
        public static explicit operator A<A<T>>(B<U> b) { throw null; }
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (3,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{A{T}}'
                // /// Error <see cref="A{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{A{T}}").WithArguments("A{A{T}}"),
                // (3,24): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "A{T}").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (4,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{T}.B{A{T}}'
                // /// Error <see cref="A{T}.B{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{T}.B{A{T}}").WithArguments("A{T}.B{A{T}}"),
                // (4,29): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{T}.B{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "A{T}").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (5,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{T}.B{U}.M{A{T}}'
                // /// Error <see cref="A{T}.B{U}.M{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{T}.B{U}.M{A{T}}").WithArguments("A{T}.B{U}.M{A{T}}"),
                // (5,34): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{T}.B{U}.M{A{T}}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "A{T}").WithArguments("Type parameter declaration must be an identifier not a type", "0081"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntaxes = GetCrefSyntaxes(compilation);
            Assert.Equal(5, crefSyntaxes.Count());

            var symbols = crefSyntaxes.Select(cref => model.GetSymbolInfo(cref).Symbol).ToArray();
            Assert.Equal("A<A<T>>", symbols[0].ToTestDisplayString());
            Assert.Equal("A<T>.B<A<T>>", symbols[1].ToTestDisplayString());
            Assert.Equal("void A<T>.B<U>.M<A<T>>(A<A<T>> a)", symbols[2].ToTestDisplayString());
            Assert.Equal("void A<T>.B<U>.M<V>(A<A<T>> a)", symbols[3].ToTestDisplayString());
            Assert.Equal("A<A<T>> A<T>.B<U>.op_Explicit(A<T>.B<U> b)", symbols[4].ToTestDisplayString());
        }

        [WorkItem(546992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546992")]
        [WorkItem(546993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546993")]
        [Fact]
        public void NestedPredefinedTypes()
        {
            var source = @"
/// <summary>
/// Error <see cref=""A{int}""/>.
/// Error <see cref=""A{T}.B{int}""/>.
/// Error <see cref=""A{T}.B{U}.M{int}""/>.
/// Fine <see cref=""A{T}.B{U}.M{V}(A{int})""/>.
/// Fine <see cref=""A{T}.B{U}.explicit operator A{int}""/>.
/// </summary>
class A<T>
{
    class B<U>
    {
        internal void M<V>(A<int> a) { }
        public static explicit operator A<int>(B<U> b) { throw null; }
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (3,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{int}'
                // /// Error <see cref="A{int}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{int}").WithArguments("A{int}"),
                // (3,24): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{int}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (4,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{T}.B{int}'
                // /// Error <see cref="A{T}.B{int}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{T}.B{int}").WithArguments("A{T}.B{int}"),
                // (4,29): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{T}.B{int}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081"),
                // (5,22): warning CS1584: XML comment has syntactically incorrect cref attribute 'A{T}.B{U}.M{int}'
                // /// Error <see cref="A{T}.B{U}.M{int}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "A{T}.B{U}.M{int}").WithArguments("A{T}.B{U}.M{int}"),
                // (5,34): warning CS1658: Type parameter declaration must be an identifier not a type. See also error CS0081.
                // /// Error <see cref="A{T}.B{U}.M{int}"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "int").WithArguments("Type parameter declaration must be an identifier not a type", "0081"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntaxes = GetCrefSyntaxes(compilation);
            Assert.Equal(5, crefSyntaxes.Count());

            var symbols = crefSyntaxes.Select(cref => model.GetSymbolInfo(cref).Symbol).ToArray();
            Assert.Equal("A<System.Int32>", symbols[0].ToTestDisplayString());
            Assert.Equal("A<T>.B<System.Int32>", symbols[1].ToTestDisplayString());
            Assert.Equal("void A<T>.B<U>.M<System.Int32>(A<System.Int32> a)", symbols[2].ToTestDisplayString());
            Assert.Equal("void A<T>.B<U>.M<V>(A<System.Int32> a)", symbols[3].ToTestDisplayString());
            Assert.Equal("A<System.Int32> A<T>.B<U>.op_Explicit(A<T>.B<U> b)", symbols[4].ToTestDisplayString());
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [Fact]
        public void NewMethods1()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

/// <see cref=""Derived.M"" />
class Derived : Base
{
    public new void M() { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var overridingMethod = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Derived").GetMember<IMethodSymbol>("M");
            Assert.Equal(overridingMethod, model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [Fact]
        public void NewMethods2()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

class Middle : Base
{
    public new void M() { }
}

/// <see cref=""Derived.M"" />
class Derived : Middle
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (12,16): warning CS1574: XML comment has cref attribute 'Derived.M' that could not be resolved
                // /// <see cref="Derived.M" />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Derived.M").WithArguments("M"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var overridingMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Middle").GetMember<MethodSymbol>("M");
            Assert.Null(model.GetSymbolInfo(cref).Symbol); // As in dev11.
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [WorkItem(547037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547037")]
        [Fact]
        public void NewMethods3()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

/// <see cref=""M"" />
class Derived : Base
{
    public new void M() { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var overridingMethod = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Derived").GetMember<IMethodSymbol>("M");
            Assert.Equal(overridingMethod, model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [Fact]
        public void Overrides1()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

/// <see cref=""Derived.M"" />
class Derived : Base
{
    public override void M() { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var overridingMethod = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Derived").GetMember<IMethodSymbol>("M");
            Assert.Equal(overridingMethod, model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [Fact]
        public void Overrides2()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

class Middle : Base
{
    public override void M() { }
}

/// <see cref=""Derived.M"" />
class Derived : Middle
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (12,16): warning CS1574: XML comment has cref attribute 'Derived.M' that could not be resolved
                // /// <see cref="Derived.M" />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Derived.M").WithArguments("M"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol); // As in dev11.
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [WorkItem(547037, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547037")]
        [Fact]
        public void Overrides3()
        {
            var source = @"
class Base
{
    public virtual void M() { }
}

/// <see cref=""M"" />
class Derived : Base
{
    public override void M() { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var overridingMethod = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Derived").GetMember<IMethodSymbol>("M");
            Assert.Equal(overridingMethod, model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(546991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546991")]
        [Fact]
        public void ExtensionMethod()
        {
            var source = @"
static class Extensions
{
    public static void M1(this Derived d) { }
    public static void M2(this Derived d) { }
    public static void M3(this Derived d) { }
}

class Base
{
    public void M2() { }
}

/// <see cref=""Derived.M1"" />
/// <see cref=""Derived.M2"" />
/// <see cref=""Derived.M3"" />
class Derived : Base
{
    public void M1() { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.RegularWithDocumentationComments);
            compilation.VerifyDiagnostics(
                // (15,16): warning CS1574: XML comment has cref attribute 'Derived.M2' that could not be resolved
                // /// <see cref="Derived.M2" />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Derived.M2").WithArguments("M2"),
                // (16,16): warning CS1574: XML comment has cref attribute 'Derived.M3' that could not be resolved
                // /// <see cref="Derived.M3" />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Derived.M3").WithArguments("M3"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation).ToArray();

            var global = compilation.GlobalNamespace;
            var derivedM1 = global.GetMember<INamedTypeSymbol>("Derived").GetMember<IMethodSymbol>("M1");
            var baseM2 = global.GetMember<INamedTypeSymbol>("Base").GetMember<IMethodSymbol>("M2");

            Assert.Equal(derivedM1, model.GetSymbolInfo(crefs[0]).Symbol);
            Assert.Null(model.GetSymbolInfo(crefs[1]).Symbol);
            Assert.Null(model.GetSymbolInfo(crefs[2]).Symbol);
        }

        [WorkItem(546990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546990")]
        [Fact]
        public void ConstructorOfGenericTypeWithinThatType()
        {
            var source = @"
/// Fine <see cref=""G()""/>.
/// Fine <see cref=""G{T}()""/>.
class G<T> { }

/// Error <see cref=""G()""/>.
/// Fine <see cref=""G{T}()""/>.
class Other { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (6,22): warning CS1574: XML comment has cref attribute 'G()' that could not be resolved
                // /// Error <see cref="G()"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "G()").WithArguments("G()"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation).ToArray();

            var constructor = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("G").InstanceConstructors.Single();

            Assert.Equal(constructor, model.GetSymbolInfo(crefs[0]).Symbol.OriginalDefinition);
            Assert.Equal(constructor, model.GetSymbolInfo(crefs[1]).Symbol.OriginalDefinition);

            Assert.Null(model.GetSymbolInfo(crefs[2]).Symbol);
            Assert.Equal(constructor, model.GetSymbolInfo(crefs[3]).Symbol.OriginalDefinition);
        }

        [WorkItem(546990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546990")]
        [Fact]
        public void ConstructorOfGenericTypeWithinNestedType()
        {
            var source = @"
class Outer<T>
{
    class Inner<U>
    {
        /// <see cref=""Outer()""/>
        void M()
        {
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (6,24): warning CS1574: XML comment has cref attribute 'Outer()' that could not be resolved
                //         /// <see cref="Outer()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Outer()").WithArguments("Outer()"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(546990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546990")]
        [WorkItem(554077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554077")]
        [Fact]
        public void QualifiedConstructorOfGenericTypeWithinNestedType()
        {
            var source = @"
/// <see cref=""Outer{T}.Outer""/>
class Outer<T>
{
    /// <see cref=""Outer{T}.Outer""/>
    void M()
    {
    }

    /// <see cref=""Outer{T}.Outer""/>
    class Inner<U>
    {
        /// <see cref=""Outer{T}.Outer""/>
        void M()
        {
        }
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'Outer{T}.Outer' that could not be resolved
                // /// <see cref="Outer{T}.Outer"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Outer{T}.Outer").WithArguments("Outer"),
                // (5,20): warning CS1574: XML comment has cref attribute 'Outer{T}.Outer' that could not be resolved
                //     /// <see cref="Outer{T}.Outer"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Outer{T}.Outer").WithArguments("Outer"));

            var outerCtor = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Outer").InstanceConstructors.Single();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation);
            var expectedSymbols = new ISymbol[] { null, null, outerCtor, outerCtor };
            var actualSymbols = GetCrefOriginalDefinitions(model, crefs);
            AssertEx.Equal(expectedSymbols, actualSymbols);
        }

        // VB had some problems with these cases between dev10 and dev11.
        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void GenericTypeWithoutTypeParameters()
        {
            var source = @"
class GenericClass<T>
{
    internal void NormalSub()
    {
    }
    internal void GenericSub<T2>()
    {
    }
}


/// <summary>This is other class</summary>
/// <remarks>
/// You may also like <see cref=""GenericClass""/>. <see cref=""GenericClass{T}""/> provides you some interesting methods.
/// <see cref=""GenericClass{T}.NormalSub""/> is normal. <see cref=""GenericClass.NormalSub""/> performs a normal operation.
/// <see cref=""GenericClass{T}.GenericSub""/> is generic. <see cref=""GenericClass.GenericSub""/> performs a generic operation.
/// <see cref=""GenericClass{T}.GenericSub{T}""/> has a generic parameter. 
/// <see cref=""GenericClass.GenericSub{T}""/> 's parameters is called <c>T2</c>.
/// </remarks>
class SomeOtherClass
{
}
";
            var tree = Parse(source, options: TestOptions.RegularWithDocumentationComments);
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            compilation.VerifyDiagnostics(
                // (15,34): warning CS1574: XML comment has cref attribute 'GenericClass' that could not be resolved
                // /// You may also like <see cref="GenericClass"/>. <see cref="GenericClass{T}"/> provides you some interesting methods.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "GenericClass").WithArguments("GenericClass"),
                // (16,67): warning CS1574: XML comment has cref attribute 'GenericClass.NormalSub' that could not be resolved
                // /// <see cref="GenericClass{T}.NormalSub"/> is normal. <see cref="GenericClass.NormalSub"/> performs a normal operation.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "GenericClass.NormalSub").WithArguments("NormalSub"),
                // (17,69): warning CS1574: XML comment has cref attribute 'GenericClass.GenericSub' that could not be resolved
                // /// <see cref="GenericClass{T}.GenericSub"/> is generic. <see cref="GenericClass.GenericSub"/> performs a generic operation.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "GenericClass.GenericSub").WithArguments("GenericSub"),
                // (19,16): warning CS1574: XML comment has cref attribute 'GenericClass.GenericSub{T}' that could not be resolved
                // /// <see cref="GenericClass.GenericSub{T}"/> 's parameters is called <c>T2</c>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "GenericClass.GenericSub{T}").WithArguments("GenericSub{T}"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation).ToArray();

            var type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("GenericClass");
            var nonGenericMethod = type.GetMember<IMethodSymbol>("NormalSub");
            var genericMethod = type.GetMember<IMethodSymbol>("GenericSub");

            Assert.Null(model.GetSymbolInfo(crefs[0]).Symbol);
            Assert.Null(model.GetSymbolInfo(crefs[3]).Symbol);
            Assert.Null(model.GetSymbolInfo(crefs[5]).Symbol);
            Assert.Null(model.GetSymbolInfo(crefs[7]).Symbol);

            Assert.Equal(type, model.GetSymbolInfo(crefs[1]).Symbol.OriginalDefinition);
            Assert.Equal(nonGenericMethod, model.GetSymbolInfo(crefs[2]).Symbol.OriginalDefinition);
            Assert.Equal(genericMethod, model.GetSymbolInfo(crefs[4]).Symbol.OriginalDefinition);
            Assert.Equal(genericMethod, model.GetSymbolInfo(crefs[6]).Symbol.OriginalDefinition);
        }

        [WorkItem(546990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546990")]
        [Fact]
        public void Dynamic()
        {
            // This can't bind to the type "dynamic" because it is not a type-only context 
            // (e.g. a method called "dynamic" would be fine).
            var source = @"
/// <see cref=""dynamic""/>
class C
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'dynamic' that could not be resolved
                // /// <see cref="dynamic"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "dynamic").WithArguments("dynamic"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [Fact]
        public void DynamicConstructor()
        {
            var source = @"
/// <see cref=""dynamic()""/>
class C
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, new[] { SystemCoreRef });
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'dynamic()' that could not be resolved
                // /// <see cref="dynamic()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "dynamic()").WithArguments("dynamic()"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [Fact]
        public void DynamicInParameters()
        {
            // BREAK: Dev11 drops candidates with "dynamic" anywhere in their parameter lists.
            // As a result, it does not match the first two or last two crefs.

            var source = @"
/// <see cref=""M1(dynamic)""/>
/// <see cref=""M1(C{dynamic})""/>
/// <see cref=""M2(object)""/>
/// <see cref=""M2(C{object})""/>
/// 
/// <see cref=""M1(object)""/>
/// <see cref=""M1(C{object})""/>
/// <see cref=""M2(dynamic)""/>
/// <see cref=""M2(C{dynamic})""/>
class C<T>
{
    void M1(dynamic p) { }
    void M1(C<dynamic> p) { }
    void M2(object p) { }
    void M2(C<object> p) { }
}
";

            SyntaxTree tree = Parse(source, options: TestOptions.RegularWithDocumentationComments);
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            compilation.VerifyDiagnostics();

            var type = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");

            //NOTE: deterministic, since GetMembers respects syntax order.
            var m1a = type.GetMembers("M1").First();
            var m1b = type.GetMembers("M1").Last();
            var m2a = type.GetMembers("M2").First();
            var m2b = type.GetMembers("M2").Last();

            var model = compilation.GetSemanticModel(tree);
            var crefs = GetCrefSyntaxes(compilation).ToArray();
            Assert.Equal(8, crefs.Length);

            Assert.Equal(m1a, model.GetSymbolInfo(crefs[0]).Symbol.OriginalDefinition);
            Assert.Equal(m1b, model.GetSymbolInfo(crefs[1]).Symbol.OriginalDefinition);
            Assert.Equal(m2a, model.GetSymbolInfo(crefs[2]).Symbol.OriginalDefinition);
            Assert.Equal(m2b, model.GetSymbolInfo(crefs[3]).Symbol.OriginalDefinition);

            Assert.Equal(m1a, model.GetSymbolInfo(crefs[4]).Symbol.OriginalDefinition);
            Assert.Equal(m1b, model.GetSymbolInfo(crefs[5]).Symbol.OriginalDefinition);
            Assert.Equal(m2a, model.GetSymbolInfo(crefs[6]).Symbol.OriginalDefinition);
            Assert.Equal(m2b, model.GetSymbolInfo(crefs[7]).Symbol.OriginalDefinition);
        }

        [WorkItem(531152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531152")]
        [Fact]
        public void MissingArgumentTypes()
        {
            var source = @"
using System;
 
/// <see cref=""Console.WriteLine(,,)""/>
class Program
{
}
";
            // Note: using is unused because syntactically invalid cref is never bound.
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (4,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'Console.WriteLine(,,)'
                // /// <see cref="Console.WriteLine(,,)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "Console.WriteLine(,,)").WithArguments("Console.WriteLine(,,)"),
                // (4,34): warning CS1658: Identifier expected. See also error CS1001.
                // /// <see cref="Console.WriteLine(,,)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, ",").WithArguments("Identifier expected", "1001"),
                // (4,35): warning CS1658: Identifier expected. See also error CS1001.
                // /// <see cref="Console.WriteLine(,,)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, ",").WithArguments("Identifier expected", "1001"),
                // (2,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(531135, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531135")]
        [Fact]
        public void NonOverloadableOperator()
        {
            var source = @"
/// <see cref=""operator =""/>
class Program
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator ='
                // /// <see cref="operator ="/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator").WithArguments("operator ="),
                // (2,25): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// <see cref="operator ="/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "=").WithArguments("Overloadable operator expected", "1037"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(531135, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531135")]
        [Fact]
        public void InvalidOperator()
        {
            var source = @"
/// <see cref=""operator q""/>
class Program
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (4,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator q'
                // /// <see cref="operator q"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator").WithArguments("operator q"),
                // (4,25): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// <see cref="operator q"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "q").WithArguments("Overloadable operator expected", "1037"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(547041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547041")]
        [Fact]
        public void EmptyVerbatimIdentifier()
        {
            var source = @"
/// <see cref=""@""/>
class Program
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute '@'
                // /// <see cref="@"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "@").WithArguments("@"),
                // (2,16): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                // /// <see cref="@"/>
                Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, ""));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(531161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531161")]
        [Fact]
        public void AttributeNameHasPrefix()
        {
            var source = @"
/// <see xmlns:cref=""Invalid""/>
class Program
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();
            Assert.Equal(0, GetCrefSyntaxes(compilation).Count());
        }

        [WorkItem(531160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531160")]
        [Fact]
        public void DuplicateAttribute()
        {
            var source = @"
/// <see cref=""int"" cref=""long""/>
class Program
{
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,21): warning CS1570: XML comment has badly formed XML -- 'Duplicate 'cref' attribute'
                // /// <see cref="int" cref="long"/>
                Diagnostic(ErrorCode.WRN_XMLParseError, @"cref=""long""").WithArguments("cref").WithLocation(2, 21));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefSyntaxes = GetCrefSyntaxes(compilation).ToArray();

            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), model.GetSymbolInfo(crefSyntaxes[0]).Symbol);
            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int64), model.GetSymbolInfo(crefSyntaxes[1]).Symbol);
        }

        [WorkItem(531157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531157")]
        [Fact]
        public void IntPtrConversion()
        {
            var source = @"
using System;
 
/// <see cref=""IntPtr.op_Explicit(void*)""/>
class C
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Equal("System.IntPtr System.IntPtr.op_Explicit(System.Void* value)", model.GetSymbolInfo(cref).Symbol.ToTestDisplayString());
        }

        [WorkItem(531233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531233")]
        [Fact]
        public void CrefInOtherElement()
        {
            var source = @"
/// <other cref=""C""/>
class C
{
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C"), model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(531162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531162")]
        [Fact]
        public void OuterVersusInheritedFromOuter()
        {
            var source = @"
class C<T>
{
    public void Goo(T x) { }
 
    class D : C<int>
    {
        /// <see cref=""Goo(T)""/>
        void Bar() { }
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("Goo");
            Assert.Equal(expectedSymbol, model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(531344, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531344")]
        [Fact]
        public void ConstraintsInCrefs()
        {
            var source = @"
/// <see cref=""Outer{Q}.Inner""/>
class Outer<T> where T: System.IFormattable
{
    class Inner { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Outer").GetMember<INamedTypeSymbol>("Inner");
            Assert.Equal(expectedSymbol, model.GetSymbolInfo(cref).Symbol.OriginalDefinition);
        }

        [Fact]
        public void CrefTypeParameterEquality1()
        {
            var source = @"
/// <see cref=""C{Q}""/>
class C<T>
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var cref = GetCrefSyntaxes(compilation).Single();

            Func<Symbol> lookupSymbol = () =>
            {
                var factory = new BinderFactory(compilation, tree, ignoreAccessibility: false);
                var binder = factory.GetBinder(cref);
                var lookupResult = LookupResult.GetInstance();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                binder.LookupSymbolsSimpleName(
                    lookupResult,
                    qualifierOpt: null,
                    plainName: "Q",
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.Default,
                    diagnose: false,
                    useSiteDiagnostics: ref useSiteDiagnostics);
                Assert.Equal(LookupResultKind.Viable, lookupResult.Kind);
                var symbol = lookupResult.Symbols.Single();
                lookupResult.Free();
                Assert.NotNull(symbol);
                Assert.IsType<CrefTypeParameterSymbol>(symbol);
                return symbol;
            };

            var symbol1 = lookupSymbol();
            var symbol2 = lookupSymbol();
            Assert.Equal(symbol1, symbol2); // Required for correctness.
            Assert.NotSame(symbol1, symbol2); // Not required, just documenting.
        }

        [Fact]
        public void CrefTypeParameterEquality2()
        {
            var source = @"
/// <see cref=""C{T}""/>
class C<T>
{
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var cref = GetCrefSyntaxes(compilation).Single();
            var model = compilation.GetSemanticModel(tree);

            var referencedType = (INamedTypeSymbol)model.GetSymbolInfo(cref).Symbol;
            Assert.NotNull(referencedType);

            var crefTypeParam = referencedType.TypeArguments.Single();
            Assert.IsType<CrefTypeParameterSymbol>(crefTypeParam.GetSymbol());

            var sourceTypeParam = referencedType.TypeParameters.Single();
            Assert.IsType<SourceTypeTypeParameterSymbol>(sourceTypeParam.GetSymbol());

            Assert.NotEqual(crefTypeParam, sourceTypeParam);
            Assert.NotEqual(sourceTypeParam, crefTypeParam);
        }

        [WorkItem(531337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531337")]
        [Fact]
        public void CrefInMethodBody()
        {
            var source = @"
class C
{
    void M()
    {
        /// <see cref=""C""/>
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <see cref="C"/>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));

            var tree = compilation.SyntaxTrees.Single();
            var cref = GetCrefSyntaxes(compilation).Single();
            var model = compilation.GetSemanticModel(tree);

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(531337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531337")]
        [Fact]
        public void CrefOnAccessor()
        {
            var source = @"
class C
{
    int P
    {
        /// <see cref=""C""/>
        get { return 0; }
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <see cref="C"/>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));

            var tree = compilation.SyntaxTrees.Single();
            var cref = GetCrefSyntaxes(compilation).Single();
            var model = compilation.GetSemanticModel(tree);

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(531391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531391")]
        [Fact]
        public void IncompleteGenericCrefMissingName()
        {
            var source = @"
/// <see cref=' {'/>
class C { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute ' {'
                // /// <see cref=' {'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, " {").WithArguments(" {"),
                // (2,17): warning CS1658: Identifier expected. See also error CS1001.
                // /// <see cref=' {'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "{").WithArguments("Identifier expected", "1001"),
                // (2,18): warning CS1658: Identifier expected. See also error CS1001.
                // /// <see cref=' {'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "'").WithArguments("Identifier expected", "1001"),
                // (2,18): warning CS1658: Syntax error, '>' expected. See also error CS1003.
                // /// <see cref=' {'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "'").WithArguments("Syntax error, '>' expected", "1003"));

            var tree = compilation.SyntaxTrees.Single();
            var cref = GetCrefSyntaxes(compilation).Single();
            var model = compilation.GetSemanticModel(tree);

            Assert.Null(model.GetSymbolInfo(cref).Symbol);
        }

        [WorkItem(548900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/548900")]
        [Fact]
        public void InvalidOperatorCref()
        {
            var source = @"
/// <see cref=""operator@""/>
class C
{
    public static C operator +(C x, C y) { }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var cref = GetCrefSyntaxes(compilation).Single();

            AssertEx.None(cref.DescendantTokens(descendIntoTrivia: true), token => token.ValueText == null);
        }

        [WorkItem(549210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/549210")]
        [Fact]
        public void InvalidGenericCref()
        {
            var source = @"
///<see cref=""X{@
///
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var cref = GetCrefSyntaxes(compilation).Single();

            AssertEx.None(cref.DescendantTokens(descendIntoTrivia: true), token => token.ValueText == null);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            foreach (var id in cref.DescendantNodes().OfType<NameSyntax>())
            {
                Assert.Null(model.GetSymbolInfo(id).Symbol); //Used to assert/throw.
            }
        }

        [WorkItem(549351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/549351")]
        [Fact]
        public void CrefNotOnMember()
        {
            var source = @"
/// <see cref=""decimal.operator
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var cref = GetCrefSyntaxes(compilation).Single();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetSymbolInfo(cref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(MethodKind.UserDefinedOperator, ((IMethodSymbol)symbol).MethodKind);
            Assert.Equal(WellKnownMemberNames.AdditionOperatorName, symbol.Name);
            Assert.Equal(SpecialType.System_Decimal, symbol.ContainingType.SpecialType);
        }

        [WorkItem(551354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551354")]
        [Fact]
        public void DotIntoTypeParameter1()
        {
            var source = @"
/// <see cref=""F{T}(T.C)""/>
class C 
{
    void F<T>(T t) { }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1580: Invalid type for parameter 'T.C' in XML comment cref attribute: 'F{T}(T.C)'
                // /// <see cref="F{T}(T.C)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefParamType, "T.C").WithArguments("T.C", "F{T}(T.C)"),
                // (2,16): warning CS1574: XML comment has cref attribute 'F{T}(T.C)' that could not be resolved
                // /// <see cref="F{T}(T.C)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "F{T}(T.C)").WithArguments("F{T}(T.C)"));

            var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();
            var parameterType = cref.Parameters.Parameters.Single().Type;

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSymbolInfo(parameterType);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);

            var parameterTypeContainingType = parameterType.DescendantNodes().OfType<SimpleNameSyntax>().First();
            var containingTypeInfo = model.GetSymbolInfo(parameterTypeContainingType);
            Assert.IsType<CrefTypeParameterSymbol>(containingTypeInfo.Symbol.GetSymbol());
        }

        [WorkItem(551354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551354")]
        [WorkItem(552759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552759")]
        [Fact]
        public void DotIntoTypeParameter2()
        {
            var source = @"
/// <see cref=""C{C}""/>
/// <see cref=""C.D{C}""/>
/// <see cref=""C.D.E{C}""/>
class C 
{
    class D
    {
        class E
        {
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'C{C}' that could not be resolved
                // /// <see cref="C{C}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{C}").WithArguments("C{C}"),
                // (2,16): warning CS1574: XML comment has cref attribute 'C.D{C}' that could not be resolved
                // /// <see cref="C.D{C}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C.D{C}").WithArguments("D{C}"),
                // (3,16): warning CS1574: XML comment has cref attribute 'C.D.E{C}' that could not be resolved
                // /// <see cref="C.D.E{C}"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C.D.E{C}").WithArguments("E{C}"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var crefs = GetCrefSyntaxes(compilation).ToArray();

            foreach (var cref in crefs)
            {
                var typeSyntax = cref.DescendantNodes().OfType<SimpleNameSyntax>().First();
                var typeSymbol = model.GetSymbolInfo(typeSyntax).Symbol;
                if (typeSyntax.Parent.Kind() == SyntaxKind.NameMemberCref)
                {
                    Assert.Null(typeSymbol);
                }
                else
                {
                    Assert.IsType<CrefTypeParameterSymbol>(typeSymbol.GetSymbol());
                }
            }
        }

        [WorkItem(549351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/549351")]
        [WorkItem(675600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675600")]
        [Fact]
        public void OperatorGreaterThanGreaterThanEquals()
        {
            var source = @"
/// <see cref=""operator }}=""/>
class C { }
";

            // Just don't blow up.
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'operator }}=' that could not be resolved
                // /// <see cref="operator }}="/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator }}=").WithArguments("operator }}=").WithLocation(2, 16));
        }

        [WorkItem(554077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554077")]
        [Fact]
        public void GenericDelegateConstructor()
        {
            var source = @"
using System;
 
/// <summary>
/// <see cref=""Action{T}.Action""/>
/// </summary>
class C { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var delegateConstructor = compilation.GlobalNamespace.
                GetMember<INamespaceSymbol>("System").GetMembers("Action").OfType<INamedTypeSymbol>().
                Single(t => t.Arity == 1).
                InstanceConstructors.Single();

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var symbol = model.GetSymbolInfo(cref).Symbol;
            Assert.NotNull(symbol);
            Assert.False(symbol.IsDefinition);
            Assert.Equal(delegateConstructor, symbol.OriginalDefinition);
        }

        [WorkItem(553394, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553394")]
        [Fact]
        public void InaccessibleViaImports()
        {
            var source = @"
using System;

/// <see cref=""RuntimeType.Equals""/>
enum E { }
";

            // Restore compat: include inaccessible members in cref lookup
            var comp = CreateEmptyCompilation(
                new[] { Parse(source, options: TestOptions.RegularWithDocumentationComments) },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default));
            comp.VerifyDiagnostics();
        }

        [WorkItem(554086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554086")]
        [Fact]
        public void InheritedInterfaceMember()
        {
            var source = @"
using System.Collections;
 
class GetEnumerator
{
    /// <summary>
    /// <see cref=""GetEnumerator""/>
    /// </summary>
    interface I : IEnumerable { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("GetEnumerator");

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(553609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553609")]
        [Fact]
        public void StringConstructor()
        {
            var source = @"
/// <summary>
/// <see cref=""string(char[])""/>
/// </summary>
enum E { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var expectedSymbol = compilation.GetSpecialType(SpecialType.System_String).
                InstanceConstructors.Single(ctor => ctor.Parameters.Length == 1 && ctor.GetParameterType(0).Kind == SymbolKind.ArrayType);

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(553609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553609")]
        [Fact]
        public void InvalidStringConstructor()
        {
            var source = @"
/// <summary>
/// <see cref=""string(float[])""/>
/// </summary>
enum E { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (3,16): warning CS1574: XML comment has cref attribute 'string(float[])' that could not be resolved
                // /// <see cref="string(float[])"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "string(float[])").WithArguments("string(float[])"));

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var info = model.GetSymbolInfo(cref);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [WorkItem(553609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553609")]
        [Fact]
        public void AliasQualifiedTypeConstructor()
        {
            var source = @"
/// <summary>
/// <see cref=""global::C()""/>
/// </summary>
class C { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var expectedSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single();

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [WorkItem(553609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553609")]
        [Fact]
        public void InvalidAliasQualifiedTypeConstructor()
        {
            var source = @"
/// <summary>
/// <see cref=""global::D()""/>
/// </summary>
class C { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (3,16): warning CS1574: XML comment has cref attribute 'global::D()' that could not be resolved
                // /// <see cref="global::D()"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "global::D()").WithArguments("global::D()"));

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var info = model.GetSymbolInfo(cref);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [WorkItem(553609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553609")]
        [Fact]
        public void AliasQualifiedGenericTypeConstructor()
        {
            var source = @"
/// <summary>
/// <see cref=""global::C{Q}(Q)""/>
/// </summary>
class C<T>
{
    C(T t) { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var expectedSymbolOriginalDefinition = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single();

            var cref = GetCrefSyntaxes(compilation).Single();

            var model = compilation.GetSemanticModel(cref.SyntaxTree);
            var actualSymbol = model.GetSymbolInfo(cref).Symbol;
            Assert.NotEqual(expectedSymbolOriginalDefinition, actualSymbol);
            Assert.Equal(expectedSymbolOriginalDefinition, actualSymbol.OriginalDefinition);
        }

        [WorkItem(553592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553592")]
        [Fact]
        public void CrefTypeParameterMemberLookup1()
        {
            var source = @"
/// <see cref=""C{T}""/>
class C<U> { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var crefSyntax = GetCrefSyntaxes(compilation).Single();
            var typeParameterSyntax = crefSyntax.DescendantNodes().OfType<IdentifierNameSyntax>().Last();
            Assert.Equal("T", typeParameterSyntax.ToString());

            var model = compilation.GetSemanticModel(typeParameterSyntax.SyntaxTree);
            var typeParameterSymbol = model.GetSymbolInfo(typeParameterSyntax).Symbol;
            Assert.IsType<CrefTypeParameterSymbol>(((CSharp.Symbols.PublicModel.Symbol)typeParameterSymbol).UnderlyingSymbol);

            var members = model.LookupSymbols(typeParameterSyntax.SpanStart, (ITypeSymbol)typeParameterSymbol);
            Assert.Equal(0, members.Length);
        }

        [WorkItem(553592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553592")]
        [Fact]
        public void CrefTypeParameterMemberLookup2()
        {
            var source = @"
/// <see cref=""System.Nullable{T}.GetValueOrDefault()""/>
enum E { }
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var crefSyntax = GetCrefSyntaxes(compilation).Single();
            var methodNameSyntax = crefSyntax.DescendantNodes().OfType<IdentifierNameSyntax>().Last();
            Assert.Equal("GetValueOrDefault", methodNameSyntax.ToString());

            var model = compilation.GetSemanticModel(methodNameSyntax.SyntaxTree);
            var methodSymbol = model.GetSymbolInfo(methodNameSyntax).Symbol;
            Assert.Equal(SymbolKind.Method, methodSymbol.Kind);

            var members = model.LookupSymbols(methodNameSyntax.SpanStart, ((IMethodSymbol)methodSymbol).ReturnType);
            Assert.Equal(0, members.Length);
        }

        [WorkItem(598371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598371")]
        [Fact]
        public void CrefParameterOrReturnTypeLookup1()
        {
            var source = @"
class X
{
    /// <summary>
    /// <see cref=""Y.implicit operator Y.Y""/>
    /// </summary>
    public class Y : X
    {
        public static implicit operator Y(int x)
        {
            return null;
        }
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var returnTypeSyntax = ((ConversionOperatorMemberCrefSyntax)(((QualifiedCrefSyntax)crefSyntax).Member)).Type;
            var expectedReturnTypeSymbol = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("X").GetMember<INamedTypeSymbol>("Y");
            var actualReturnTypeSymbol = model.GetSymbolInfo(returnTypeSyntax).Symbol;
            Assert.Equal(expectedReturnTypeSymbol, actualReturnTypeSymbol);

            var expectedCrefSymbol = expectedReturnTypeSymbol.GetMember<IMethodSymbol>(WellKnownMemberNames.ImplicitConversionName);
            var actualCrefSymbol = model.GetSymbolInfo(crefSyntax).Symbol;
            Assert.Equal(expectedCrefSymbol, actualCrefSymbol);
        }

        [WorkItem(586815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586815")]
        [Fact]
        public void CrefParameterOrReturnTypeLookup2()
        {
            var source = @"
class A<T>
{
    class B : A<B>
    {
        /// <summary>
        /// <see cref=""Goo(B)""/>
        /// </summary>
        void Goo(B x) { }
    }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var classA = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("A");
            var classB = classA.GetMember<INamedTypeSymbol>("B");

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var parameterTypeSyntax = ((NameMemberCrefSyntax)crefSyntax).Parameters.Parameters[0].Type;
            var expectedParameterTypeSymbol = classA.Construct(classB).GetMember<INamedTypeSymbol>("B");
            var actualParameterTypeSymbol = model.GetSymbolInfo(parameterTypeSyntax).Symbol;
            Assert.Equal(expectedParameterTypeSymbol, actualParameterTypeSymbol);

            var expectedCrefSymbol = classB.GetMember<IMethodSymbol>("Goo");
            var actualCrefSymbol = model.GetSymbolInfo(crefSyntax).Symbol;
            Assert.Equal(expectedCrefSymbol, actualCrefSymbol);
        }

        [WorkItem(743425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743425")]
        [Fact]
        public void NestedTypeInParameterList()
        {
            var source = @"
class Outer<T>
{
    class Inner { }

    /// <see cref='Outer{Q}.M(Inner)'/>
    void M() { }

    void M(Inner i) { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (6,31): warning CS8018: Within cref attributes, nested types of generic types should be qualified.
                //     /// <see cref='Outer{Q}.M(Inner)'/>
                Diagnostic(ErrorCode.WRN_UnqualifiedNestedTypeInCref, "Inner"),
                // (6,20): warning CS1574: XML comment has cref attribute 'Outer{Q}.M(Inner)' that could not be resolved
                //     /// <see cref='Outer{Q}.M(Inner)'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Outer{Q}.M(Inner)").WithArguments("M(Inner)"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var outer = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Outer");
            var inner = outer.GetMember<INamedTypeSymbol>("Inner");

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var parameterTypeSyntax = crefSyntax.DescendantNodes().OfType<CrefParameterSyntax>().Single().Type;
            var parameterTypeSymbol = model.GetSymbolInfo(parameterTypeSyntax).Symbol;
            Assert.True(parameterTypeSymbol.IsDefinition);
            Assert.Equal(inner, parameterTypeSymbol);
        }

        [WorkItem(653402, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653402")]
        [Fact]
        public void CrefAliasInfo_TopLevel()
        {
            var source = @"
using A = System.Int32;

/// <see cref=""A""/>
class C { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var crefSyntax = GetCrefSyntaxes(compilation).Single();

            var info = model.GetSymbolInfo(crefSyntax);
            var alias = model.GetAliasInfo(crefSyntax.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single());

            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), info.Symbol);
            Assert.Equal(info.Symbol, alias.Target);
            Assert.Equal("A", alias.Name);
        }

        [WorkItem(653402, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/653402")]
        [Fact]
        public void CrefAliasInfo_Parameter()
        {
            var source = @"
using A = System.Int32;

/// <see cref=""M(A)""/>
class C
{
    void M(A a) { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var crefSyntax = GetCrefSyntaxes(compilation).Single();
            var parameterSyntax = crefSyntax.
                DescendantNodes().OfType<CrefParameterSyntax>().Single().
                DescendantNodes().OfType<IdentifierNameSyntax>().Single();

            var info = model.GetSymbolInfo(parameterSyntax);
            var alias = model.GetAliasInfo(parameterSyntax);

            Assert.Equal(compilation.GetSpecialType(SpecialType.System_Int32), info.Symbol);
            Assert.Equal(info.Symbol, alias.Target);
            Assert.Equal("A", alias.Name);
        }

        [Fact]
        [WorkItem(760850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")]
        public void TestGetSpeculativeSymbolInfoInsideCref()
        {
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
using System;

class P
{
    Action<int> b = (int x) => { };
    class B
    {
        /// <see cref=""b""/>
        void a()
        {
        }
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();
            var crefName = cref.Name;

            var model = compilation.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(crefName);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind);
            Assert.Equal("System.Action<System.Int32> P.b", symbolInfo.Symbol.ToTestDisplayString());

            var speculatedName = SyntaxFactory.ParseName("b");
            symbolInfo = model.GetSpeculativeSymbolInfo(crefName.Position, speculatedName, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind);
            Assert.Equal("System.Action<System.Int32> P.b", symbolInfo.Symbol.ToTestDisplayString());

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(crefName.Position, speculatedName, out speculativeModel);
            Assert.True(success);

            Assert.NotNull(speculativeModel);
            symbolInfo = speculativeModel.GetSymbolInfo(speculatedName);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind);
            Assert.Equal("System.Action<System.Int32> P.b", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(760850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760850")]
        public void TestGetSpeculativeSymbolInfoInsideCrefParameterOrReturnType()
        {
            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(@"
class Base
{
    class Inherited { }
}

class Outer
{
    class Inner : Base
    {
        int P { get; set; };

        /// <see cref=""explicit operator Return(Param)""/>        
        void M()
        {
        }
    }
}
");
            var tree = compilation.SyntaxTrees.First();
            var cref = (ConversionOperatorMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();
            var crefReturnType = cref.Type;
            var crefParameterType = cref.Parameters.Parameters.Single().Type;

            var crefPosition = cref.SpanStart;
            var crefReturnTypePosition = crefReturnType.SpanStart;
            var crefParameterTypePosition = crefParameterType.SpanStart;
            var nonCrefPosition = tree.GetRoot().DescendantTrivia().Single(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).SpanStart;

            var accessor = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Outer").GetMember<INamedTypeSymbol>("Inner").GetMember<IPropertySymbol>("P").GetMethod;
            var inheritedType = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Base").GetMember<INamedTypeSymbol>("Inherited");

            var model = compilation.GetSemanticModel(tree);

            // Try a non-type.  Should work in a cref, unless it's in a parameter or return type.
            // Should not work outside a cref, because the accessor cannot be referenced by name.
            var accessorName = SyntaxFactory.ParseName(accessor.Name);
            var crefInfo = model.GetSpeculativeSymbolInfo(crefPosition, accessorName, SpeculativeBindingOption.BindAsExpression);
            var returnInfo = model.GetSpeculativeSymbolInfo(crefReturnTypePosition, accessorName, SpeculativeBindingOption.BindAsExpression);
            var paramInfo = model.GetSpeculativeSymbolInfo(crefParameterTypePosition, accessorName, SpeculativeBindingOption.BindAsExpression);
            var nonCrefInfo = model.GetSpeculativeSymbolInfo(nonCrefPosition, accessorName, SpeculativeBindingOption.BindAsExpression);

            Assert.Equal(accessor, crefInfo.Symbol);
            Assert.Equal(SymbolInfo.None, returnInfo);
            Assert.Equal(SymbolInfo.None, paramInfo);
            Assert.Equal(accessor, nonCrefInfo.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.NotReferencable, nonCrefInfo.CandidateReason);

            // Try an inaccessible inherited types.  Should work in a cref, but only if it's in a parameter or return type (since it's inherited).
            // Should not work outside a cref, because it's inaccessible.
            // NOTE: SpeculativeBindingOptions are ignored when the position is inside a cref.
            var inheritedTypeName = SyntaxFactory.ParseName(inheritedType.Name);
            crefInfo = model.GetSpeculativeSymbolInfo(crefPosition, inheritedTypeName, SpeculativeBindingOption.BindAsExpression);
            returnInfo = model.GetSpeculativeSymbolInfo(crefReturnTypePosition, inheritedTypeName, SpeculativeBindingOption.BindAsExpression);
            paramInfo = model.GetSpeculativeSymbolInfo(crefParameterTypePosition, inheritedTypeName, SpeculativeBindingOption.BindAsExpression);
            nonCrefInfo = model.GetSpeculativeSymbolInfo(nonCrefPosition, inheritedTypeName, SpeculativeBindingOption.BindAsExpression);

            Assert.Equal(SymbolInfo.None, crefInfo);
            Assert.Equal(inheritedType, returnInfo.Symbol);
            Assert.Equal(inheritedType, paramInfo.Symbol);
            Assert.Equal(inheritedType, nonCrefInfo.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.Inaccessible, nonCrefInfo.CandidateReason);
        }

        [WorkItem(768624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768624")]
        [Fact]
        public void CrefsOnDelegate()
        {
            var source = @"
/// <see cref='T'/>
/// <see cref='t'/>
/// <see cref='Invoke'/>
/// <see cref='ToString'/>
delegate void D< T > (T t);
";

            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (2,16): warning CS1574: XML comment has cref attribute 'T' that could not be resolved
                // /// <see cref='T'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "T").WithArguments("T"),
                // (3,16): warning CS1574: XML comment has cref attribute 't' that could not be resolved
                // /// <see cref='t'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "t").WithArguments("t"),
                // (4,16): warning CS1574: XML comment has cref attribute 'Invoke' that could not be resolved
                // /// <see cref='Invoke'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Invoke").WithArguments("Invoke"),
                // (5,16): warning CS1574: XML comment has cref attribute 'ToString' that could not be resolved
                // /// <see cref='ToString'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "ToString").WithArguments("ToString"));
        }

        [WorkItem(924473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924473")]
        [Fact]
        public void InterfaceInheritedMembersInSemanticModelLookup()
        {
            var source = @"
interface IBase
{
    int P { get; set; }
}

interface IDerived : IBase
{
}

/// <see cref='IDerived.P'/>
class C
{
}
";
            var comp = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);

            // Not expected to bind, since we don't consider inherited members.
            comp.VerifyDiagnostics(
                // (11,16): warning CS1574: XML comment has cref attribute 'IDerived.P' that could not be resolved
                // /// <see cref='IDerived.P'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "IDerived.P").WithArguments("P").WithLocation(11, 16));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = GetCrefSyntaxes(comp).Single();

            // No info, since it doesn't bind.
            var info = model.GetSymbolInfo(syntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);

            // No lookup results.
            var derivedInterface = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("IDerived");
            Assert.Equal(0, model.LookupSymbols(syntax.SpanStart, derivedInterface).Length);
        }

        [WorkItem(924473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924473")]
        [Fact]
        public void InterfaceObjectMembers()
        {
            var source = @"
interface I
{
}

/// <see cref='I.ToString'/>
class C
{
}
";
            var comp = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);

            // Not expected to bind, since we don't consider inherited members.
            comp.VerifyDiagnostics(
                // (6,16): warning CS1574: XML comment has cref attribute 'I.ToString' that could not be resolved
                // /// <see cref='I.ToString'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "I.ToString").WithArguments("ToString").WithLocation(6, 16));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = GetCrefSyntaxes(comp).Single();

            // No info, since it doesn't bind.
            var info = model.GetSymbolInfo(syntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);

            // No lookup results.
            var symbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("I");
            Assert.Equal(0, model.LookupSymbols(syntax.SpanStart, symbol).Length);
        }

        #region Dev10 bugs from KevinH

        [Fact]
        public void Dev10_461967()
        {
            // Can use anything we want as the name of the type parameter.
            var source = @"
/// <see cref=""C{Blah}"" />
/// <see cref=""C{Blah}.Inner"" />
class C<T>
{
    class Inner { }
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics();

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation).ToArray();
            Assert.Equal(2, crefs.Length);

            var outer = compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var inner = outer.GetMember<INamedTypeSymbol>("Inner");

            Assert.Equal(outer, model.GetSymbolInfo(crefs[0]).Symbol.OriginalDefinition);
            Assert.Equal(inner, model.GetSymbolInfo(crefs[1]).Symbol.OriginalDefinition);
        }

        [Fact]
        public void Dev10_461974()
        {
            // Can't omit type parameters.
            var source = @"
/// <see cref=""C"" />
/// <see cref=""C{}"" />
class C<T>
{
}
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (3,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'C{}'
                // /// <see cref="C{}" />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C{}").WithArguments("C{}").WithLocation(3, 16),
                // (3,18): warning CS1658: Identifier expected. See also error CS1001.
                // /// <see cref="C{}" />
                Diagnostic(ErrorCode.WRN_ErrorOverride, "}").WithArguments("Identifier expected", "1001").WithLocation(3, 18),
                // (2,16): warning CS1574: XML comment has cref attribute 'C' that could not be resolved
                // /// <see cref="C" />
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C").WithArguments("C").WithLocation(2, 16));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var crefs = GetCrefSyntaxes(compilation).ToArray();
            Assert.Equal(2, crefs.Length);

            var actualSymbol0 = model.GetSymbolInfo(crefs[0]).Symbol;
            Assert.Null(actualSymbol0);

            var actualSymbol1 = model.GetSymbolInfo(crefs[1]).Symbol;
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C"), actualSymbol1.OriginalDefinition);
            Assert.Equal(TypeKind.Error, ((INamedTypeSymbol)actualSymbol1).TypeArguments.Single().TypeKind);
        }

        [Fact]
        public void Dev10_461986()
        {
            // Can't cref an array type.
            var source = @"
/// <see cref=""C[]"" />
class C { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'C[]'
                // /// <see cref="C[]" />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C").WithArguments("C[]"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            // Once the square brackets are skipped, binding works just fine.
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C"), model.GetSymbolInfo(cref).Symbol);
        }

        [Fact]
        public void Dev10_461988()
        {
            // Can't cref a nullable type (unless you use the generic type syntax).
            var source = @"
/// <see cref=""C?"" />
class C { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'C?'
                // /// <see cref="C?" />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C").WithArguments("C?"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            // Once the question mark is skipped, binding works just fine.
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C"), model.GetSymbolInfo(cref).Symbol);
        }

        [Fact]
        public void Dev10_461990()
        {
            // Can't put a smiley face at the end of a cref.
            // NOTE: if we had used a type named "C", this would have been accepted as a verbatim cref.
            var source = @"
/// <see cref=""Cat:-)"" />
class Cat { }
";

            var compilation = (Compilation)CreateCompilationWithMscorlib40AndDocumentationComments(source);
            compilation.VerifyDiagnostics(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'Cat:-)'
                // /// <see cref="Cat:-)" />
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "Cat").WithArguments("Cat:-)"));

            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = GetCrefSyntaxes(compilation).Single();

            // Once the smiley is skipped, binding works just fine.
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("Cat"), model.GetSymbolInfo(cref).Symbol);
        }

        #endregion Dev10 bugs from KevinH

        private static ISymbol[] GetCrefOriginalDefinitions(SemanticModel model, IEnumerable<CrefSyntax> crefs)
        {
            return crefs.Select(syntax => model.GetSymbolInfo(syntax).Symbol).Select(symbol => (object)symbol == null ? null : symbol.OriginalDefinition).ToArray();
        }

        [Fact]
        [WorkItem(410932, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=410932")]
        public void LookupOnCrefTypeParameter()
        {
            var source = @"
class Test
{
    T F<T>()
    {
    }

    /// <summary>
    /// <see cref=""F{U}()""/>
    /// </summary>
    void S()
    { }
}
";

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var crefSyntax = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var name = ((GenericNameSyntax)crefSyntax.Name).TypeArgumentList.Arguments.Single();
            Assert.Equal("U", name.ToString());
            var typeParameter = (ITypeParameterSymbol)model.GetSymbolInfo(name).Symbol;
            Assert.Empty(model.LookupSymbols(name.SpanStart, typeParameter, "GetAwaiter"));
        }

        [Fact]
        [WorkItem(23957, "https://github.com/dotnet/roslyn/issues/23957")]
        public void CRef_InParameter()
        {
            var source = @"
class Test
{
    void M(in int x)
    {
    }

    /// <summary>
    /// <see cref=""M(in int)""/>
    /// </summary>
    void S()
    {
    }
}
";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments).VerifyDiagnostics();
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

            var parameter = cref.Parameters.Parameters.Single();
            Assert.Equal(SyntaxKind.InKeyword, parameter.RefKindKeyword.Kind());
            Assert.Equal(SyntaxKind.None, parameter.ReadOnlyKeyword.Kind());

            var parameterSymbol = ((IMethodSymbol)model.GetSymbolInfo(cref).Symbol).Parameters.Single();
            Assert.Equal(RefKind.In, parameterSymbol.RefKind);
        }

        [Fact]
        public void CRef_RefReadonlyParameter()
        {
            var source = """
                class Test
                {
                    void M(ref readonly int x)
                    {
                    }

                    /// <summary>
                    /// <see cref="M(ref readonly int)"/>
                    /// </summary>
                    void S()
                    {
                    }
                }
                """;

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(
                // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     void M(ref readonly int x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
                // (8,26): warning CS1658: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.. See also error CS9058.
                //     /// <see cref="M(ref readonly int)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments("Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.", "9058").WithLocation(8, 26)));

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular12.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics());
            verify(CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics());

            static void verify(CSharpCompilation compilation)
            {
                var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
                var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

                var parameter = cref.Parameters.Parameters.Single();
                Assert.Equal(SyntaxKind.RefKeyword, parameter.RefKindKeyword.Kind());
                Assert.Equal(SyntaxKind.ReadOnlyKeyword, parameter.ReadOnlyKeyword.Kind());

                var parameterSymbol = ((IMethodSymbol)model.GetSymbolInfo(cref).Symbol).Parameters.Single();
                Assert.Equal(RefKind.RefReadOnlyParameter, parameterSymbol.RefKind);
            }
        }

        [Fact]
        public void CRef_RefReadonlyParameter_ReadonlyRef()
        {
            var source = """
                class Test
                {
                    void M(ref readonly int x)
                    {
                    }

                    /// <summary>
                    /// <see cref="M(readonly ref int)"/>
                    /// </summary>
                    void S()
                    {
                    }
                }
                """;

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(
                // (3,16): error CS9058: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     void M(ref readonly int x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "readonly").WithArguments("ref readonly parameters", "12.0").WithLocation(3, 16),
                // (8,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'M(readonly ref int)'
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "M(").WithArguments("M(readonly ref int)").WithLocation(8, 20),
                // (8,22): warning CS1658: ) expected. See also error CS1026.
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(8, 22)));

            var expectedDiagnostics = new[]
            {
                // (8,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'M(readonly ref int)'
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "M(").WithArguments("M(readonly ref int)").WithLocation(8, 20),
                // (8,22): warning CS1658: ) expected. See also error CS1026.
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(8, 22)
            };

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular12.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));
            verify(CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));

            static void verify(CSharpCompilation compilation)
            {
                var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();
                Assert.Empty(cref.Parameters.Parameters);
            }
        }

        [Fact]
        public void CRef_ReadonlyRefParameter()
        {
            var source = """
                class Test
                {
                    void M(readonly ref int x)
                    {
                    }

                    /// <summary>
                    /// <see cref="M(readonly ref int)"/>
                    /// </summary>
                    void S()
                    {
                    }
                }
                """;

            var expectedDiagnostics = new[]
            {
                // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //     void M(readonly ref int x)
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
                // (8,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'M(readonly ref int)'
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "M(").WithArguments("M(readonly ref int)").WithLocation(8, 20),
                // (8,22): warning CS1658: ) expected. See also error CS1026.
                //     /// <see cref="M(readonly ref int)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments(") expected", "1026").WithLocation(8, 22)
            };

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));
            verify(CreateCompilation(source, parseOptions: TestOptions.Regular12.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));
            verify(CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));

            static void verify(CSharpCompilation compilation)
            {
                var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();
                Assert.Empty(cref.Parameters.Parameters);
            }
        }

        [Fact]
        public void CRef_ReadonlyRefParameter_RefReadonly()
        {
            var source = """
                class Test
                {
                    void M(readonly ref int x)
                    {
                    }

                    /// <summary>
                    /// <see cref="M(ref readonly int)"/>
                    /// </summary>
                    void S()
                    {
                    }
                }
                """;

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(
                // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //     void M(readonly ref int x)
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
                // (8,20): warning CS1574: XML comment has cref attribute 'M(ref readonly int)' that could not be resolved
                //     /// <see cref="M(ref readonly int)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(ref readonly int)").WithArguments("M(ref readonly int)").WithLocation(8, 20),
                // (8,26): warning CS1658: Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.. See also error CS9058.
                //     /// <see cref="M(ref readonly int)"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "readonly").WithArguments("Feature 'ref readonly parameters' is not available in C# 11.0. Please use language version 12.0 or greater.", "9058").WithLocation(8, 26)));

            var expectedDiagnostics = new[]
            {
                // (3,12): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //     void M(readonly ref int x)
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
                // (8,20): warning CS1574: XML comment has cref attribute 'M(ref readonly int)' that could not be resolved
                //     /// <see cref="M(ref readonly int)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "M(ref readonly int)").WithArguments("M(ref readonly int)").WithLocation(8, 20)
            };

            verify(CreateCompilation(source, parseOptions: TestOptions.Regular12.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));
            verify(CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose)).VerifyDiagnostics(expectedDiagnostics));

            static void verify(CSharpCompilation compilation)
            {
                var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
                var cref = (NameMemberCrefSyntax)GetCrefSyntaxes(compilation).Single();

                var parameter = cref.Parameters.Parameters.Single();
                Assert.Equal(SyntaxKind.RefKeyword, parameter.RefKindKeyword.Kind());
                Assert.Equal(SyntaxKind.ReadOnlyKeyword, parameter.ReadOnlyKeyword.Kind());

                Assert.True(model.GetSymbolInfo(cref).IsEmpty);
            }
        }

        [Fact]
        public void Cref_TupleType()
        {
            var source = @"
using System;
/// <summary>
/// See <see cref=""ValueTuple{T,T}""/>.
/// </summary>
class C
{
}
";
            var parseOptions = TestOptions.RegularWithDocumentationComments;
            var options = TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default);
            var compilation = CreateCompilation(source, parseOptions: parseOptions, options: options, targetFramework: TargetFramework.StandardAndCSharp);
            var cMember = compilation.GetMember<NamedTypeSymbol>("C");
            var xmlDocumentationString = cMember.GetDocumentationCommentXml();

            var xml = System.Xml.Linq.XDocument.Parse(xmlDocumentationString);
            var cref = xml.Descendants("see").Single().Attribute("cref").Value;

            Assert.Equal("T:System.ValueTuple`2", cref);
        }

        [Fact]
        public void Cref_TupleTypeField()
        {
            var source = @"
using System;
/// <summary>
/// See <see cref=""ValueTuple{Int32,Int32}.Item1""/>.
/// </summary>
class C
{
}
";
            var parseOptions = TestOptions.RegularWithDocumentationComments;
            var options = TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default);
            var compilation = CreateCompilation(source, parseOptions: parseOptions, options: options, targetFramework: TargetFramework.StandardAndCSharp);
            var cMember = compilation.GetMember<NamedTypeSymbol>("C");
            var xmlDocumentationString = cMember.GetDocumentationCommentXml();

            var xml = System.Xml.Linq.XDocument.Parse(xmlDocumentationString);
            var cref = xml.Descendants("see").Single().Attribute("cref").Value;

            Assert.Equal("F:System.ValueTuple`2.Item1", cref);
        }

        [Theory]
        [InlineData(" { }")]
        [InlineData(";")]
        [WorkItem(50330, "https://github.com/dotnet/roslyn/issues/50330")]
        public void OnRecord(string terminator)
        {
            var source = @"using System;

/// <summary>
/// Something with a <see cref=""String""/> instance.
/// See also <see cref=""RelativePathBase""/>.
/// See also <see cref=""InvalidCref""/>.
/// </summary>
record CacheContext(string RelativePathBase)" + terminator;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(6, 25),
                // (6,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(6, 25));
        }

        [Theory]
        [InlineData(" { }")]
        [InlineData(";")]
        [WorkItem(50330, "https://github.com/dotnet/roslyn/issues/50330")]
        public void OnRecordStruct(string terminator)
        {
            var source = @"using System;

/// <summary>
/// Something with a <see cref=""String""/> instance.
/// See also <see cref=""RelativePathBase""/>.
/// See also <see cref=""InvalidCref""/>.
/// </summary>
record struct CacheContext(string RelativePathBase)" + terminator;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp10), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(6, 25),
                // (6,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(6, 25));
        }

        [Theory]
        [InlineData(" { }")]
        [InlineData(";")]
        [WorkItem(50330, "https://github.com/dotnet/roslyn/issues/50330")]
        public void OnRecord_WithoutPrimaryCtor(string terminator)
        {
            var source = @"using System;

/// <summary>
/// Something with a <see cref=""String""/> instance.
/// See also <see cref=""InvalidCref""/>.
/// </summary>
record CacheContext" + terminator;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (5,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(5, 25));
        }

        [Theory]
        [InlineData(" { }")]
        [InlineData(";")]
        [WorkItem(50330, "https://github.com/dotnet/roslyn/issues/50330")]
        public void OnRecordStruct_WithoutPrimaryCtor(string terminator)
        {
            var source = @"using System;

/// <summary>
/// Something with a <see cref=""String""/> instance.
/// See also <see cref=""InvalidCref""/>.
/// </summary>
record struct CacheContext" + terminator;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp10), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (5,25): warning CS1574: XML comment has cref attribute 'InvalidCref' that could not be resolved
                // /// See also <see cref="InvalidCref"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "InvalidCref").WithArguments("InvalidCref").WithLocation(5, 25));
        }

        [Theory]
        [InlineData(" { }")]
        [InlineData(";")]
        public void Record_TypeAndPropertyWithSameNameInScope(string terminator)
        {
            var source = @"using System;

/// <summary>
/// Something with a <see cref=""String""/> instance.
/// </summary>
record CacheContext(string String)" + terminator;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithDocumentationComments, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1));

            var model = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var crefSyntaxes = GetCrefSyntaxes(comp);
            var symbol = model.GetSymbolInfo(crefSyntaxes.Single()).Symbol;
            Assert.Equal(SymbolKind.Property, symbol.Kind);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12345")]
        public void AmbiguousReferenceInDifferentNamespaces()
        {
            var source = """
                namespace System
                {
                    public class TypeA
                    {
                    }
                }

                namespace System.Foo
                {
                    public class TypeA
                    {
                    }
                }

                namespace A
                {
                    using System;
                    using System.Foo;

                    /// <summary>
                    ///     <see cref="TypeA"/>
                    /// </summary>
                    public class Bar
                    {
                    }
                }
                """;
            CreateCompilationWithMscorlib40AndDocumentationComments(source).VerifyDiagnostics(
                // (3,18): warning CS1591: Missing XML comment for publicly visible type or member 'TypeA'
                //     public class TypeA
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "TypeA").WithArguments("System.TypeA").WithLocation(3, 18),
                // (10,18): warning CS1591: Missing XML comment for publicly visible type or member 'TypeA'
                //     public class TypeA
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "TypeA").WithArguments("System.Foo.TypeA").WithLocation(10, 18),
                // (21,24): warning CS0419: Ambiguous reference in cref attribute: 'TypeA'. Assuming 'System.Foo.TypeA', but could have also matched other overloads including 'System.TypeA'.
                //         <see cref="TypeA"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "TypeA").WithArguments("TypeA", "System.Foo.TypeA", "System.TypeA").WithLocation(21, 24));
        }
    }
}
