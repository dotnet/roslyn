// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DocumentationCommentCompilerTests : CSharpTestBase
    {
        public static CSharpCompilation CreateCompilationUtil(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "Test") =>
            CreateCompilation(
                source,
                references,
                targetFramework: TargetFramework.Mscorlib40,
                options: (options ?? TestOptions.ReleaseDll).WithXmlReferenceResolver(XmlFileResolver.Default),
                parseOptions: TestOptions.RegularWithDocumentationComments,
                assemblyName: assemblyName);

        #region Single-line styleWRN_UnqualifiedNestedTypeInCref

        [Fact]
        public void SingleLine_OneLine()
        {
            var source = @"
/// <summary>Text</summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Text</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_MultipleLines()
        {
            var source = @"
/// <summary>
/// Text
/// </summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_EmptyOneLine()
        {
            var source = @"
///
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_EmptyMultipleLines()
        {
            var source = @"
///
///
///
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            
            
            
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_NoLeadingSpaces()
        {
            var source = @"
///<summary>
///Text
///</summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_SomeLeadingSpaces()
        {
            var source = @"
///<summary>
/// Text
///  </summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
             Text
              </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_LeadingTab()
        {
            var source = @"
/// <summary>
///	Tabbed
/// </summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Tabbed
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_WhitespaceBefore()
        {
            var source = @"
/// <summary>
 /// Text
  /// </summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SingleLine_BlankLines()
        {
            var source = @"
/// 
/// <summary>
/// Text
/// </summary>
/// 
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            
            <summary>
            Text
            </summary>
            
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Single-line style

        #region Multi-line style

        [Fact]
        public void MultiLine_OneLine()
        {
            var source = @"
/** <summary>Text</summary> */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Text</summary> 
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_EmptyOneLine()
        {
            var source = @"
/** */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_EmptyTwoLines()
        {
            var source = @"
/**
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_EmptyThreeLines()
        {
            var source = @"
/**

 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_FirstLineSpace()
        {
            var source = @"
/** <summary>
 *  Text
 *  </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_FirstLineNoSpace()
        {
            var source = @"
/**<summary>
 *  Text
 *  </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_StarsPattern()
        {
            var source = @"
/**
 * <summary>
 * Text
 * </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Text
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_StarsNoPattern()
        {
            var source = @"
/**
 * <summary>
  * Text
 * </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
             * <summary>
              * Text
             * </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_WhitespacePattern()
        {
            var source = @"
/**
   <summary>
   Text
   </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
               <summary>
               Text
               </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_WhitespaceNoPattern()
        {
            var source = @"
/**
   <summary>
    Text
   </summary>
 */
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
               <summary>
                Text
               </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultiLine_LegacyTests()
        {
            var source = @"
class A
{
	/**
			* <summary>
			*	/** 
			*	
			*	
			* </summary>
			*/
	public void foo1(){}

	/**
	* ///
	*		///
	* /**
	*/
	public void foo2(){}

	/**
	/// <summary>
	///
	/// </summary>
	*/
	public void foo3(){}

	// Test: // should not be xml comment
	/**
	// <summary>
	// 
	// </summary>
	*/
	public void foo4(){}
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:A.foo1"">
             <summary>
            	/** 
            	
            	
             </summary>
        </member>
        <member name=""M:A.foo2"">
             ///
            		///
             /**
        </member>
        <member name=""M:A.foo3"">
            	/// <summary>
            	///
            	/// </summary>
        </member>
        <member name=""M:A.foo4"">
            	// <summary>
            	// 
            	// </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(547164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547164")]
        [Fact]
        public void MultiLine_PatternShorterOnSubsequentLine()
        {
            var source = @"
//repro.cs
public class Point
{
    /**
    * <summary>Instance variable in the 
    *  Point Class.</summary>
    */
    private int x;
 
    /**
    * <summary>This is the entry point of the Point class testing
    * program.</summary>
    */
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (3,14): warning CS1591: Missing XML comment for publicly visible type or member 'Point'
                // public class Point
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Point").WithArguments("Point"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""F:Point.x"">
            <summary>Instance variable in the 
             Point Class.</summary>
        </member>
        <member name=""M:Point.Main"">
            <summary>This is the entry point of the Point class testing
            program.</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Multi-line style

        #region Partial types

        [Fact]
        public void PartialTypes_OneFile()
        {
            var source = @"
/// <summary>Summary 1</summary>
public partial class C { }

/// <summary>Summary 2</summary>
public partial class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 1</summary>
            <summary>Summary 2</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PartialTypes_MultipleFiles()
        {
            var source1 = @"
/// <summary>Summary 1</summary>
public partial class C { }
";

            var source2 = @"
/// <summary>Summary 2</summary>
public partial class C { }
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            var actualA = GetDocumentationCommentText(compA);
            var expectedA = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 1</summary>
            <summary>Summary 2</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expectedA, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            var actualB = GetDocumentationCommentText(compB);
            var expectedB = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 2</summary>
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expectedB, actualB);
        }

        [Fact]
        public void PartialTypes_MultipleStyles()
        {
            var source = @"
/// <summary>Summary 1</summary>
public partial class C { }

/** <summary>Summary 2</summary> */
public partial class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 1</summary>
            <summary>Summary 2</summary> 
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Partial types

        #region Partial methods

        [Fact]
        public void PartialMethods_OneFile()
        {
            var source = @"
partial class C
{
    /// <summary>Summary 1</summary>
    partial void M();
}

partial class C
{
    /// <summary>Summary 2</summary>
    partial void M() { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M"">
            <summary>Summary 2</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PartialMethods_MultipleFiles()
        {
            var source1 = @"
partial class C
{
    /** <summary>Summary 1</summary>*/
    partial void M() { }
}
";

            var source2 = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    partial void M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void PartialMethod_NoImplementation()
        {
            // Whole document XML does not include the member, but single symbol XML does include it
            var source = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    partial void M();
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularWithDocumentationComments);
            var comp = CreateCompilation(tree, assemblyName: "Test");
            var method = comp.GlobalNamespace.GetMember<MethodSymbol>("C.M");

            AssertEx.AssertLinesEqual(expected: """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                    </members>
                </doc>
                """, actual: GetDocumentationCommentText(comp));

            AssertEx.AssertLinesEqual("""
                <member name="M:C.M">
                    <summary>Summary 2</summary>
                </member>
                """, DocumentationCommentCompiler.GetDocumentationCommentXml(method, processIncludes: true, cancellationToken: default));
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    /** <summary>Summary 1</summary>*/
    public partial int M() => 42;
}
";

            var source2 = @"
public partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""M:C.M"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles_DefinitionComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    public partial int M() => 42;
}
";

            var source2 = @"
public partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""M:C.M"">
            <summary>Summary 2</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles_ImplementationComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    /** <summary>Summary 1</summary>*/
    public partial int M() => 42;
}
";

            var source2 = @"
public partial class C
{
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""M:C.M"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles_NoComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    public partial int M() => 42;
}
";

            var source2 = @"
public partial class C
{
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            var expectedDiagnostics = new[]
            {
                // (4,24): warning CS1591: Missing XML comment for publicly visible type or member 'C.M()'
                //     public partial int M();
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "M").WithArguments("C.M()").WithLocation(4, 24)
            };

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics(expectedDiagnostics);
            var actualA = GetDocumentationCommentText(compA, expectedDiagnostics);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics(expectedDiagnostics);
            var actualB = GetDocumentationCommentText(compB, expectedDiagnostics);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles_Overlap()
        {
            var source1 = @"
partial class C
{
    /** <remarks>Remarks 1</remarks> */
    public partial int M() => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M"">
            <remarks>Remarks 1</remarks> 
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void ExtendedPartialMethods_MultipleFiles_ImplComment_Invalid()
        {
            var source1 = @"
partial class C
{
    /// <summary></a></summary>
    public partial int M() => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int M();
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            var expectedDiagnostics = new[]
            {
                // (4,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /// <summary></a></summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(4, 20),
                // (4,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /// <summary></a></summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(4, 22)
            };

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics(expectedDiagnostics);
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""M:C.M"" -->
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics(expectedDiagnostics);
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        [Fact]
        public void PartialMethod_Paramref_01()
        {
            var source1 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p1""/>.</summary> */
    public partial int M(int p1) => 42;
}
";

            var source2 = @"
partial class C
{
    public partial int M(int p2);
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                    Assert.Equal("p2", method.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (5,24): warning CS8826: Partial method declarations 'int C.M(int p2)' and 'int C.M(int p1)' have signature differences.
                    //     public partial int M(int p1) => 42;
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("int C.M(int p2)", "int C.M(int p1)").WithLocation(5, 24));

                var actual = GetDocumentationCommentText(compilation);
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32)"">
            <summary>Accepts <paramref name=""p1""/>.</summary> 
        </member>
    </members>
</doc>
    ".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        [Fact]
        public void PartialMethod_Paramref_02()
        {
            var source1 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p2""/>.</summary> */
    public partial int M(int p1) => 42;
}
";

            var source2 = @"
partial class C
{
    public partial int M(int p2);
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                    Assert.Equal("p2", method.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,42): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'p2', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p2").WithArguments("p2", "C.M(int)").WithLocation(4, 42),
                    // (5,24): warning CS8826: Partial method declarations 'int C.M(int p2)' and 'int C.M(int p1)' have signature differences.
                    //     public partial int M(int p1) => 42;
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("int C.M(int p2)", "int C.M(int p1)").WithLocation(5, 24));

                var actual = GetDocumentationCommentText(compilation,
                    // (4,42): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'p2', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p2").WithArguments("p2", "C.M(int)").WithLocation(4, 42));
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32)"">
            <summary>Accepts <paramref name=""p2""/>.</summary> 
        </member>
    </members>
</doc>
    ".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        [Fact]
        public void PartialMethod_Paramref_03()
        {
            var source1 = @"
partial class C
{
    public partial int M(int p1) => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p1""/>.</summary> */
    public partial int M(int p2);
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                    Assert.Equal("p2", method.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,24): warning CS8826: Partial method declarations 'int C.M(int p2)' and 'int C.M(int p1)' have signature differences.
                    //     public partial int M(int p1) => 42;
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("int C.M(int p2)", "int C.M(int p1)").WithLocation(4, 24),
                    // (4,42): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'p1', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p1").WithArguments("p1", "C.M(int)").WithLocation(4, 42));

                var actual = GetDocumentationCommentText(compilation,
                    // (4,42): warning CS1734: XML comment on 'C.M(int)' has a paramref tag for 'p1', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p1").WithArguments("p1", "C.M(int)").WithLocation(4, 42));
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32)"">
            <summary>Accepts <paramref name=""p1""/>.</summary> 
        </member>
    </members>
</doc>
    ".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        [Fact]
        public void PartialMethod_Paramref_04()
        {
            var source1 = @"
partial class C
{
    public partial int M(int p1) => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p2""/>.</summary> */
    public partial int M(int p2);
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var method = module.GlobalNamespace.GetMember<MethodSymbol>("C.M");
                    Assert.Equal("p2", method.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,24): warning CS8826: Partial method declarations 'int C.M(int p2)' and 'int C.M(int p1)' have signature differences.
                    //     public partial int M(int p1) => 42;
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("int C.M(int p2)", "int C.M(int p1)").WithLocation(4, 24));

                var actual = GetDocumentationCommentText(compilation);
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32)"">
            <summary>Accepts <paramref name=""p2""/>.</summary> 
        </member>
    </members>
</doc>
".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        /// <summary>Counterpart to <see cref="PartialMethod_NoImplementation"/>.</summary>
        [Fact]
        public void PartialProperty_NoImplementation()
        {
            // Whole document XML does not include the member, but single symbol XML does include it
            var source = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int P { get; set; }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularWithDocumentationComments);
            var comp = CreateCompilation(tree, assemblyName: "Test");
            var property = comp.GlobalNamespace.GetMember<PropertySymbol>("C.P");

            AssertEx.AssertLinesEqual(expected: """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                    </members>
                </doc>
                """, actual: GetDocumentationCommentText(comp));

            AssertEx.AssertLinesEqual("""
                <member name="P:C.P">
                    <summary>Summary 2</summary>
                </member>
                """, DocumentationCommentCompiler.GetDocumentationCommentXml(property, processIncludes: true, cancellationToken: default));
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    /** <summary>Summary 1</summary>*/
    public partial int P => 42;
}
";

            var source2 = @"
public partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""P:C.P"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles"/>.</summary>
        [Fact]
        public void PartialIndexers_MultipleFiles()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    /** <summary>Summary 1</summary>*/
    public partial int this[int p] => 42;
}
";

            var source2 = @"
public partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int this[int p] { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""P:C.Item(System.Int32)"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="PartialMethod_NoImplementation"/>.</summary>
        [Fact]
        public void PartialIndexer_NoImplementation()
        {
            // Whole document XML does not include the member, but single symbol XML does include it
            var source = """
                partial class C
                {
                    /// <summary>Summary 2</summary>
                    /// <param name="p">My param</param>
                    public partial int this[int p] { get; set; }
                }
                """;

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.RegularWithDocumentationComments);
            var comp = CreateCompilation(tree, assemblyName: "Test");
            var property = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();

            AssertEx.AssertLinesEqual(expected: """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                    </members>
                </doc>
                """, actual: GetDocumentationCommentText(comp));

            AssertEx.AssertLinesEqual("""
                <member name="P:C.Item(System.Int32)">
                    <summary>Summary 2</summary>
                    <param name="p">My param</param>
                </member>
                """, DocumentationCommentCompiler.GetDocumentationCommentXml(property, processIncludes: true, cancellationToken: default));
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles_DefinitionComment"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles_DefinitionComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    public partial int P => 42;
}
";

            var source2 = @"
public partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""P:C.P"">
            <summary>Summary 2</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles_ImplementationComment"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles_ImplementationComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    /** <summary>Summary 1</summary>*/
    public partial int P => 42;
}
";

            var source2 = @"
public partial class C
{
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
        <member name=""P:C.P"">
            <summary>Summary 1</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles_NoComment"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles_NoComment()
        {
            var source1 = @"
/// <summary>Summary 0</summary>
public partial class C
{
    public partial int P => 42;
}
";

            var source2 = @"
public partial class C
{
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            var expectedDiagnostics = new[]
            {
                // (4,24): warning CS1591: Missing XML comment for publicly visible type or member 'C.P'
                //     public partial int P { get; }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "P").WithArguments("C.P").WithLocation(4, 24)
            };

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics(expectedDiagnostics);
            var actualA = GetDocumentationCommentText(compA, expectedDiagnostics);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>Summary 0</summary>
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics(expectedDiagnostics);
            var actualB = GetDocumentationCommentText(compB, expectedDiagnostics);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles_Overlap"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles_Overlap()
        {
            var source1 = @"
partial class C
{
    /** <remarks>Remarks 1</remarks> */
    public partial int P => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics();
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""P:C.P"">
            <remarks>Remarks 1</remarks> 
        </member>
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics();
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="ExtendedPartialMethods_MultipleFiles_ImplComment_Invalid"/>.</summary>
        [Fact]
        public void PartialProperties_MultipleFiles_ImplComment_Invalid()
        {
            var source1 = @"
partial class C
{
    /// <summary></a></summary>
    public partial int P => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Summary 2</summary>*/
    public partial int P { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            var expectedDiagnostics = new[]
            {
                // (4,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /// <summary></a></summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(4, 20),
                // (4,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /// <summary></a></summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(4, 22)
            };

            // Files passed in order.
            var compA = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");
            compA.VerifyDiagnostics(expectedDiagnostics);
            var actualA = GetDocumentationCommentText(compA);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""P:C.P"" -->
    </members>
</doc>
".Trim();
            AssertEx.Equal(expected, actualA);

            // Files passed in reverse order.
            var compB = CreateCompilation(new[] { tree2, tree1 }, assemblyName: "Test");
            compB.VerifyDiagnostics(expectedDiagnostics);
            var actualB = GetDocumentationCommentText(compB);
            Assert.Equal(expected, actualB);
        }

        /// <summary>Counterpart to <see cref="PartialMethod_Paramref_01"/>.</summary>
        [Fact]
        public void PartialIndexer_Paramref_01()
        {
            var source1 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p1""/>.</summary> */
    public partial int this[int p1] => 42;
}
";

            var source2 = @"
partial class C
{
    public partial int this[int p2] { get; }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var indexer = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
                    Assert.Equal("p2", indexer.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (5,24): warning CS9256: Partial member declarations 'int C.this[int p2]' and 'int C.this[int p1]' have signature differences.
                    //     public partial int this[int p1] => 42;
                    Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "this").WithArguments("int C.this[int p2]", "int C.this[int p1]").WithLocation(5, 24));

                var actual = GetDocumentationCommentText(compilation);
                var expected = """
                    <?xml version="1.0"?>
                    <doc>
                        <assembly>
                            <name>Test</name>
                        </assembly>
                        <members>
                            <member name="P:C.Item(System.Int32)">
                                <summary>Accepts <paramref name="p1"/>.</summary> 
                            </member>
                        </members>
                    </doc>
                    """.Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        /// <summary>Counterpart to <see cref="PartialMethod_Paramref_02"/>.</summary>
        [Fact]
        public void PartialIndexer_Paramref_02()
        {
            var source1 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p2""/>.</summary> */
    public partial int this[int p1] => 42;
}
";

            var source2 = @"
partial class C
{
    public partial int this[int p2] { get; }
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var indexer = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
                    Assert.Equal("p2", indexer.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,42): warning CS1734: XML comment on 'C.this[int]' has a paramref tag for 'p2', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p2").WithArguments("p2", "C.this[int]").WithLocation(4, 42),
                    // (5,24): warning CS9256: Partial member declarations 'int C.this[int p2]' and 'int C.this[int p1]' have signature differences.
                    //     public partial int this[int p1] => 42;
                    Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "this").WithArguments("int C.this[int p2]", "int C.this[int p1]").WithLocation(5, 24));

                var actual = GetDocumentationCommentText(compilation,
                    // (4,42): warning CS1734: XML comment on 'C.this[int]' has a paramref tag for 'p2', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p2").WithArguments("p2", "C.this[int]").WithLocation(4, 42));
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""P:C.Item(System.Int32)"">
            <summary>Accepts <paramref name=""p2""/>.</summary> 
        </member>
    </members>
</doc>
    ".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        /// <summary>Counterpart to <see cref="PartialMethod_Paramref_03"/>.</summary>
        [Fact]
        public void PartialIndexer_Paramref_03()
        {
            var source1 = @"
partial class C
{
    public partial int this[int p1] => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p1""/>.</summary> */
    public partial int this[int p2] { get; }
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var indexer = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
                    Assert.Equal("p2", indexer.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,24): warning CS9256: Partial member declarations 'int C.this[int p2]' and 'int C.this[int p1]' have signature differences.
                    //     public partial int this[int p1] => 42;
                    Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "this").WithArguments("int C.this[int p2]", "int C.this[int p1]").WithLocation(4, 24),
                    // (4,42): warning CS1734: XML comment on 'C.this[int]' has a paramref tag for 'p1', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p1").WithArguments("p1", "C.this[int]").WithLocation(4, 42));

                var actual = GetDocumentationCommentText(compilation,
                    // (4,42): warning CS1734: XML comment on 'C.this[int]' has a paramref tag for 'p1', but there is no parameter by that name
                    //     /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p1").WithArguments("p1", "C.this[int]").WithLocation(4, 42));
                var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""P:C.Item(System.Int32)"">
            <summary>Accepts <paramref name=""p1""/>.</summary> 
        </member>
    </members>
</doc>
".Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        /// <summary>Counterpart to <see cref="PartialMethod_Paramref_04"/>.</summary>
        [Fact]
        public void PartialIndexer_Paramref_04()
        {
            var source1 = @"
partial class C
{
    public partial int this[int p1] => 42;
}
";

            var source2 = @"
partial class C
{
    /** <summary>Accepts <paramref name=""p2""/>.</summary> */
    public partial int this[int p2] { get; }
}
";
            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularPreviewWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularPreviewWithDocumentationComments);

            // Files passed in order.
            verify(new[] { tree1, tree2 });

            // Files passed in reverse order.
            verify(new[] { tree2, tree1 });

            void verify(CSharpTestSource source)
            {
                var compilation = CreateCompilation(source, assemblyName: "Test");
                var verifier = CompileAndVerify(compilation, symbolValidator: module =>
                {
                    var indexer = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Single();
                    Assert.Equal("p2", indexer.Parameters.Single().Name);
                });
                verifier.VerifyDiagnostics(
                    // (4,24): warning CS9256: Partial member declarations 'int C.this[int p2]' and 'int C.this[int p1]' have signature differences.
                    //     public partial int this[int p1] => 42;
                    Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "this").WithArguments("int C.this[int p2]", "int C.this[int p1]").WithLocation(4, 24));

                var actual = GetDocumentationCommentText(compilation);
                var expected = """
                    <?xml version="1.0"?>
                    <doc>
                        <assembly>
                            <name>Test</name>
                        </assembly>
                        <members>
                            <member name="P:C.Item(System.Int32)">
                                <summary>Accepts <paramref name="p2"/>.</summary> 
                            </member>
                        </members>
                    </doc>
                    """.Trim();
                AssertEx.Equal(expected, actual);
            }
        }

        [Fact]
        public void PartialEvent_NoImplementation()
        {
            var source = """
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial event System.Action E;
                }
                """;
            var comp = CreateCompilation(source,
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(
                    // (1,22): warning CS1591: Missing XML comment for publicly visible type or member 'C'
                    // public partial class C
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C").WithLocation(1, 22),
                    // (4,40): error CS9275: Partial member 'C.E' must have an implementation part.
                    //     public partial event System.Action E;
                    Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "E").WithArguments("C.E").WithLocation(4, 40));
            var e = comp.GlobalNamespace.GetMember<EventSymbol>("C.E");

            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                    </members>
                </doc>
                """, GetDocumentationCommentText(comp,
                    // (1,22): warning CS1591: Missing XML comment for publicly visible type or member 'C'
                    // public partial class C
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C").WithLocation(1, 22)));

            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                <member name="E:C.E">
                    <summary>Summary 1</summary> 
                </member>
                """, DocumentationCommentCompiler.GetDocumentationCommentXml(e, processIncludes: true, cancellationToken: default));
        }

        [Fact]
        public void PartialConstructor_NoImplementation()
        {
            var source = """
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial C();
                }
                """;
            var comp = CreateCompilation(source,
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(
                    // (1,22): warning CS1591: Missing XML comment for publicly visible type or member 'C'
                    // public partial class C
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C").WithLocation(1, 22),
                    // (4,20): error CS9275: Partial member 'C.C()' must have an implementation part.
                    //     public partial C();
                    Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "C").WithArguments("C.C()").WithLocation(4, 20));
            var ctor = comp.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");

            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                    </members>
                </doc>
                """, GetDocumentationCommentText(comp,
                    // (1,22): warning CS1591: Missing XML comment for publicly visible type or member 'C'
                    // public partial class C
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C").WithLocation(1, 22)));

            AssertEx.AssertEqualToleratingWhitespaceDifferences("""
                <member name="M:C.#ctor">
                    <summary>Summary 1</summary>
                </member>
                """, DocumentationCommentCompiler.GetDocumentationCommentXml(ctor, processIncludes: true, cancellationToken: default));
        }

        [Fact]
        public void PartialEvent_MultipleFiles()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                public partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="E:C.E">
                            <summary>Summary 2</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_ImplementationComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                public partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="E:C.E">
                            <summary>Summary 2</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_DefinitionComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                public partial class C
                {
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="E:C.E">
                            <summary>Summary 1</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial C();
                }
                """;
            var source2 = """
                public partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="M:C.#ctor">
                            <summary>Summary 2</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_ImplementationComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    public partial C();
                }
                """;
            var source2 = """
                public partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="M:C.#ctor">
                            <summary>Summary 2</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_DefinitionComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial C();
                }
                """;
            var source2 = """
                public partial class C
                {
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                        <member name="M:C.#ctor">
                            <summary>Summary 1</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_NoComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                public partial class C
                {
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (4,40): warning CS1591: Missing XML comment for publicly visible type or member 'C.E'
                //     public partial event System.Action E;
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "E").WithArguments("C.E").WithLocation(4, 40)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_NoComment()
        {
            var source1 = """
                /** <summary>Summary 0</summary> */
                public partial class C
                {
                    public partial C();
                }
                """;
            var source2 = """
                public partial class C
                {
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="T:C">
                            <summary>Summary 0</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (4,20): warning CS1591: Missing XML comment for publicly visible type or member 'C.C()'
                //     public partial C();
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C.C()").WithLocation(4, 20)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_Overlap()
        {
            var source1 = """
                partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <remarks>Remarks 2</remarks> */
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="E:C.E">
                            <remarks>Remarks 2</remarks> 
                        </member>
                    </members>
                </doc>
                """;

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_Overlap()
        {
            var source1 = """
                partial class C
                {
                    /// <summary>Summary 1</summary>
                    /// <param name="x">Param 1</param>
                    public partial C(int x, int y);
                }
                """;
            var source2 = """
                partial class C
                {
                    /// <remarks>Remarks 2</remarks>
                    /// <param name="y">Param 2</param>
                    public partial C(int x, int y) { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor(System.Int32,System.Int32)">
                            <remarks>Remarks 2</remarks>
                            <param name="y">Param 2</param>
                        </member>
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (5,26): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.C(int, int)' (but other parameters do)
                //     public partial C(int x, int y) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.C(int, int)").WithLocation(5, 26),
                // (5,33): warning CS1573: Parameter 'y' has no matching param tag in the XML comment for 'C.C(int, int)' (but other parameters do)
                //     public partial C(int x, int y);
                Diagnostic(ErrorCode.WRN_MissingParamTag, "y").WithArguments("y", "C.C(int, int)").WithLocation(5, 33)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedDiagnostics));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_InvalidImplComment()
        {
            var source1 = """
                partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary></a></summary> */
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <!-- Badly formed XML comment ignored for member "E:C.E" -->
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(3, 20),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(3, 22)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_InvalidImplComment()
        {
            var source1 = """
                partial class C
                {
                    /** <summary>Summary 1</summary> */
                    public partial C();
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary></a></summary> */
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <!-- Badly formed XML comment ignored for member "M:C.#ctor" -->
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(3, 20),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(3, 22)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialEvent_MultipleFiles_InvalidDefComment()
        {
            var source1 = """
                partial class C
                {
                    /** <summary></a></summary> */
                    public partial event System.Action E;
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial event System.Action E { add { } remove { } }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="E:C.E">
                            <summary>Summary 2</summary> 
                        </member>
                        <!-- Badly formed XML comment ignored for member "E:C.E" -->
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(3, 20),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(3, 22)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_MultipleFiles_InvalidDefComment()
        {
            var source1 = """
                partial class C
                {
                    /** <summary></a></summary> */
                    public partial C();
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary>Summary 2</summary> */
                    public partial C() { }
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor">
                            <summary>Summary 2</summary> 
                        </member>
                        <!-- Badly formed XML comment ignored for member "M:C.#ctor" -->
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS1570: XML comment has badly formed XML -- 'End tag 'a' does not match the start tag 'summary'.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "a").WithArguments("a", "summary").WithLocation(3, 20),
                // (3,22): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                //     /** <summary></a></summary> */
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(3, 22)
            };

            var comp = CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments)
                .VerifyDiagnostics(expectedDiagnostics);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));
        }

        [Fact]
        public void PartialConstructor_Paramref_01()
        {
            var source1 = """
                partial class C
                {
                    /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    public partial C(int p1) { }
                }
                """;
            var source2 = """
                partial class C
                {
                    public partial C(int p2);
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor(System.Int32)">
                            <summary>Accepts <paramref name="p1"/>.</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (4,20): warning CS9256: Partial member declarations 'C.C(int p2)' and 'C.C(int p1)' have signature differences.
                //     public partial C(int p1) { }
                Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int p2)", "C.C(int p1)").WithLocation(4, 20)
            };

            var comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            static void validate(ModuleSymbol module)
            {
                var ctor = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
                Assert.Equal("p2", ctor.Parameters.Single().Name);
            }
        }

        [Fact]
        public void PartialConstructor_Paramref_02()
        {
            var source1 = """
                partial class C
                {
                    /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    public partial C(int p1) { }
                }
                """;
            var source2 = """
                partial class C
                {
                    public partial C(int p2);
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor(System.Int32)">
                            <summary>Accepts <paramref name="p2"/>.</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedXmlDiagnostic =
                // (3,42): warning CS1734: XML comment on 'C.C(int)' has a paramref tag for 'p2', but there is no parameter by that name
                //     /** <summary>Accepts <paramref name="p2"/>.</summary> */
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p2").WithArguments("p2", "C.C(int)").WithLocation(3, 42);

            var expectedDiagnostics = new[]
            {
                expectedXmlDiagnostic,
                // (4,20): warning CS9256: Partial member declarations 'C.C(int p2)' and 'C.C(int p1)' have signature differences.
                //     public partial C(int p1) { }
                Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int p2)", "C.C(int p1)").WithLocation(4, 20)
            };

            var comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedXmlDiagnostic));

            comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedXmlDiagnostic));

            static void validate(ModuleSymbol module)
            {
                var ctor = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
                Assert.Equal("p2", ctor.Parameters.Single().Name);
            }
        }

        [Fact]
        public void PartialConstructor_Paramref_03()
        {
            var source1 = """
                partial class C
                {
                    public partial C(int p1) { }
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary>Accepts <paramref name="p1"/>.</summary> */
                    public partial C(int p2);
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor(System.Int32)">
                            <summary>Accepts <paramref name="p1"/>.</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedXmlDiagnostic =
                // (3,42): warning CS1734: XML comment on 'C.C(int)' has a paramref tag for 'p1', but there is no parameter by that name
                //     /** <summary>Accepts <paramref name="p1"/>.</summary> */
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "p1").WithArguments("p1", "C.C(int)").WithLocation(3, 42);

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS9256: Partial member declarations 'C.C(int p2)' and 'C.C(int p1)' have signature differences.
                //     public partial C(int p1) { }
                Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int p2)", "C.C(int p1)").WithLocation(3, 20),
                expectedXmlDiagnostic
            };

            var comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedXmlDiagnostic));

            comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp, expectedXmlDiagnostic));

            static void validate(ModuleSymbol module)
            {
                var ctor = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
                Assert.Equal("p2", ctor.Parameters.Single().Name);
            }
        }

        [Fact]
        public void PartialConstructor_Paramref_04()
        {
            var source1 = """
                partial class C
                {
                    public partial C(int p1) { }
                }
                """;
            var source2 = """
                partial class C
                {
                    /** <summary>Accepts <paramref name="p2"/>.</summary> */
                    public partial C(int p2);
                }
                """;

            var expected = """
                <?xml version="1.0"?>
                <doc>
                    <assembly>
                        <name>Test</name>
                    </assembly>
                    <members>
                        <member name="M:C.#ctor(System.Int32)">
                            <summary>Accepts <paramref name="p2"/>.</summary> 
                        </member>
                    </members>
                </doc>
                """;

            var expectedDiagnostics = new[]
            {
                // (3,20): warning CS9256: Partial member declarations 'C.C(int p2)' and 'C.C(int p1)' have signature differences.
                //     public partial C(int p1) { }
                Diagnostic(ErrorCode.WRN_PartialMemberSignatureDifference, "C").WithArguments("C.C(int p2)", "C.C(int p1)").WithLocation(3, 20)
            };

            var comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source1, source2],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            comp = (CSharpCompilation)CompileAndVerify(CreateCompilation([source2, source1],
                assemblyName: "Test",
                parseOptions: TestOptions.RegularPreviewWithDocumentationComments),
                sourceSymbolValidator: validate,
                symbolValidator: validate)
                .VerifyDiagnostics(expectedDiagnostics).Compilation;
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, GetDocumentationCommentText(comp));

            static void validate(ModuleSymbol module)
            {
                var ctor = module.GlobalNamespace.GetMember<MethodSymbol>("C..ctor");
                Assert.Equal("p2", ctor.Parameters.Single().Name);
            }
        }

        #endregion Partial methods

        #region Crefs

        [Fact]
        public void ValidCrefs()
        {
            var source = @"
/// <summary>
/// A <see cref=""C""/> B 
/// <see cref=""object""/> C.
/// </summary>
public class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            A <see cref=""T:C""/> B 
            <see cref=""T:System.Object""/> C.
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InvalidCrefs()
        {
            var source = @"
/// <summary>
/// A <see cref=""Q""/>.
/// </summary>
public class C { }

/// <summary>
/// A <see cref=""R{S, T}""/>.
/// </summary>
public class D { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (3,18): warning CS1574: XML comment has cref attribute 'Q' that could not be resolved
                // /// A <see cref="Q"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Q").WithArguments("Q"),
                // (8,18): warning CS1574: XML comment has cref attribute 'R{S, T}' that could not be resolved
                // /// A <see cref="R{S, T}"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "R{S, T}").WithArguments("R{S, T}"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            A <see cref=""!:Q""/>.
            </summary>
        </member>
        <member name=""T:D"">
            <summary>
            A <see cref=""!:R&lt;S, T&gt;""/>.
            </summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Crefs

        #region Output name

        [Fact]
        public void AssemblyNameFromCompilationName()
        {
            var source = @"
/// A <see cref=""Main""/>.
public class C
{
    static void Main() {}
}
";
            var comp = CreateCompilationUtil(source, assemblyName: "CompilationName");
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>CompilationName</name>
    </assembly>
    <members>
        <member name=""T:C"">
            A <see cref=""M:C.Main""/>.
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AssemblyNameFromOutputName()
        {
            var source = @"
/// A <see cref=""Main""/>.
public class C
{
    static void Main() {}
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp, "OutputName");
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>OutputName</name>
    </assembly>
    <members>
        <member name=""T:C"">
            A <see cref=""M:C.Main""/>.
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Output name

        #region WRN_UnprocessedXMLComment

        [Fact]
        public void UnprocessedXMLComment_Types()
        {
            var source = @"
class C<T> : object where T : I
{
    
}

struct S<T, U> where T : U
{

}

interface I
{

}

delegate void D<T, U>(T t, U u) where T : U;

enum E : byte
{

}
";

            var revisedSource = new DocumentationCommentAdder().Visit(Parse(source).GetCompilationUnitRoot()).ToFullString();
            // Manually verified that positions match dev11.
            CreateCompilationUtil(revisedSource).VerifyDiagnostics(
                // (2,15): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,24): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,33): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,42): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,52): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,62): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,77): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,91): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,101): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (2,111): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C/** 2 */</** 3 */T/** 4 */> /** 5 */: /** 6 */object /** 7 */where /** 8 */T /** 9 */: /** 10 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (3,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 11 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 12 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,17): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,27): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,37): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,47): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,58): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,68): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,79): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,94): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,105): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,116): warning CS1587: XML comment is not placed on a valid language element
                // /** 13 */struct /** 14 */S/** 15 */</** 16 */T/** 17 */, /** 18 */U/** 19 */> /** 20 */where /** 21 */T /** 22 */: /** 23 */U
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 24 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 25 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,20): warning CS1587: XML comment is not placed on a valid language element
                // /** 26 */interface /** 27 */I
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (13,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 28 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 29 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,19): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,33): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,43): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,53): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,63): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,74): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,84): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,94): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,104): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,115): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,125): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,136): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,147): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,157): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,168): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,183): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,194): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,205): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,215): warning CS1587: XML comment is not placed on a valid language element
                // /** 30 */delegate /** 31 */void /** 32 */D/** 33 */</** 34 */T/** 35 */, /** 36 */U/** 37 */>/** 38 */(/** 39 */T /** 40 */t/** 41 */, /** 42 */U /** 43 */u/** 44 */) /** 45 */where /** 46 */T /** 47 */: /** 48 */U/** 49 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (19,15): warning CS1587: XML comment is not placed on a valid language element
                // /** 50 */enum /** 51 */E /** 52 */: /** 53 */byte
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (19,26): warning CS1587: XML comment is not placed on a valid language element
                // /** 50 */enum /** 51 */E /** 52 */: /** 53 */byte
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (19,37): warning CS1587: XML comment is not placed on a valid language element
                // /** 50 */enum /** 51 */E /** 52 */: /** 53 */byte
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (20,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 54 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (22,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 55 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (23,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 56 */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void UnprocessedXMLComment_Members()
        {
            var source = @"
class C
{
    private int field;
    private int Property { get; set; }
    private int this[int x] { get { return 0; } set { } }
    private event System.Action FieldLikeEvent;
    private event System.Action CustomEvent { add { } remove { } }
    private void Method<T, U>(T t, U u) where T : U { }
    public static int operator +(C c) { return 0; }
    public static explicit operator int(C c) { return 0; }
    private C(int x) : base() { }
}

enum E
{
    A,
}
";

            var revisedSource = new DocumentationCommentAdder().Visit(Parse(source).GetCompilationUnitRoot()).ToFullString();
            // Manually verified that positions match dev11.
            CreateCompilationUtil(revisedSource).VerifyDiagnostics(
                // (4,41): warning CS0169: The field 'C.field' is never used
                //     /** 3 */private /** 4 */int /** 5 */field/** 6 */;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("C.field"),
                // (7,87): warning CS0067: The event 'C.FieldLikeEvent' is never used
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "FieldLikeEvent").WithArguments("C.FieldLikeEvent"),

                // (2,15): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (3,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 2 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field/** 6 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,33): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field/** 6 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,46): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field/** 6 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,33): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,50): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,61): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,73): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,84): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,96): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,107): warning CS1587: XML comment is not placed on a valid language element
                //     /** 7 */private /** 8 */int /** 9 */Property /** 10 */{ /** 11 */get/** 12 */; /** 13 */set/** 14 */; /** 15 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,35): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,48): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,58): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,71): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,81): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,92): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,103): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,116): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,127): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,143): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,153): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,164): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,175): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,188): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,199): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,210): warning CS1587: XML comment is not placed on a valid language element
                //     /** 16 */private /** 17 */int /** 18 */this/** 19 */[/** 20 */int /** 21 */x/** 22 */] /** 23 */{ /** 24 */get /** 25 */{ /** 26 */return /** 27 */0/** 28 */; /** 29 */} /** 30 */set /** 31 */{ /** 32 */} /** 33 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,37): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,52): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,62): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,78): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,101): warning CS1587: XML comment is not placed on a valid language element
                //     /** 34 */private /** 35 */event /** 36 */System/** 37 */./** 38 */Action /** 39 */FieldLikeEvent/** 40 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,37): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,52): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,62): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,78): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,99): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,110): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,123): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,134): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,145): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,161): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,172): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,183): warning CS1587: XML comment is not placed on a valid language element
                //     /** 41 */private /** 42 */event /** 43 */System/** 44 */./** 45 */Action /** 46 */CustomEvent /** 47 */{ /** 48 */add /** 49 */{ /** 50 */} /** 51 */remove /** 52 */{ /** 53 */} /** 54 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,36): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,51): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,61): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,71): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,82): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,92): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,102): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,112): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,123): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,133): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,144): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,155): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,165): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,176): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,191): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,202): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,213): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,224): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,235): warning CS1587: XML comment is not placed on a valid language element
                //     /** 55 */private /** 56 */void /** 57 */Method/** 58 */</** 59 */T/** 60 */, /** 61 */U/** 62 */>/** 63 */(/** 64 */T /** 65 */t/** 66 */, /** 67 */U /** 68 */u/** 69 */) /** 70 */where /** 71 */T /** 72 */: /** 73 */U /** 74 */{ /** 75 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,37): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,50): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,68): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,78): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,88): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,99): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,109): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,120): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,131): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,147): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,157): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,168): warning CS1587: XML comment is not placed on a valid language element
                //     /** 76 */public /** 77 */static /** 78 */int /** 79 */operator /** 80 */+/** 81 */(/** 82 */C /** 83 */c/** 84 */) /** 85 */{ /** 86 */return /** 87 */0/** 88 */; /** 89 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,37): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,55): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,73): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,85): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,95): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,106): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,116): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,127): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,138): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,155): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,166): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,178): warning CS1587: XML comment is not placed on a valid language element
                //     /** 90 */public /** 91 */static /** 92 */explicit /** 93 */operator /** 94 */int/** 95 */(/** 96 */C /** 97 */c/** 98 */) /** 99 */{ /** 100 */return /** 101 */0/** 102 */; /** 103 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,23): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,34): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,45): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,59): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,70): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,82): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,94): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,108): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,119): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,131): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (12,143): warning CS1587: XML comment is not placed on a valid language element
                //     /** 104 */private /** 105 */C/** 106 */(/** 107 */int /** 108 */x/** 109 */) /** 110 */: /** 111 */base/** 112 */(/** 113 */) /** 114 */{ /** 115 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (13,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 116 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,16): warning CS1587: XML comment is not placed on a valid language element
                // /** 117 */enum /** 118 */E
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (16,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 119 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,16): warning CS1587: XML comment is not placed on a valid language element
                //     /** 120 */A/** 121 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (18,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 122 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (19,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 123 */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void UnprocessedXMLComment_Expressions()
        {
            var source = @"
class C
{
    private int field = 1;
    private event System.Action FieldLikeEvent = () => { return; };
    private C(int x = 1, int y = 2) { }
    private C(int x) : this(x, x + 1)
    {
        int y = x--;
    }
}

enum E
{
    A = 1 + 1,
}
";

            var revisedSource = new DocumentationCommentAdder().Visit(Parse(source).GetCompilationUnitRoot()).ToFullString();
            // Manually verified that positions match dev11.
            CreateCompilationUtil(revisedSource).VerifyDiagnostics(
                // (4,41): warning CS0414: The field 'C.field' is assigned but its value is never used
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C.field"),

                // (2,15): warning CS1587: XML comment is not placed on a valid language element
                // /** 0 */class /** 1 */C
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (3,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 2 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,33): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,47): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,57): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (4,66): warning CS1587: XML comment is not placed on a valid language element
                //     /** 3 */private /** 4 */int /** 5 */field /** 6 */= /** 7 */1/** 8 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,21): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,36): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,51): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,61): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,77): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,101): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,112): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,122): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,133): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,145): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,156): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,171): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,182): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (5,192): warning CS1587: XML comment is not placed on a valid language element
                //     /** 9 */private /** 10 */event /** 11 */System/** 12 */./** 13 */Action /** 14 */FieldLikeEvent /** 15 */= /** 16 */(/** 17 */) /** 18 */=> /** 19 */{ /** 20 */return/** 21 */; /** 22 */}/** 23 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,32): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,42): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,55): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,66): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,77): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,87): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,98): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,111): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,122): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,133): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,143): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,154): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (6,165): warning CS1587: XML comment is not placed on a valid language element
                //     /** 24 */private /** 25 */C/** 26 */(/** 27 */int /** 28 */x /** 29 */= /** 30 */1/** 31 */, /** 32 */int /** 33 */y /** 34 */= /** 35 */2/** 36 */) /** 37 */{ /** 38 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,22): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,32): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,42): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,55): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,65): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,76): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,87): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,100): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,110): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,120): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,131): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,142): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,153): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,163): warning CS1587: XML comment is not placed on a valid language element
                //     /** 39 */private /** 40 */C/** 41 */(/** 42 */int /** 43 */x/** 44 */) /** 45 */: /** 46 */this/** 47 */(/** 48 */x/** 49 */, /** 50 */x /** 51 */+ /** 52 */1/** 53 */)
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (8,5): warning CS1587: XML comment is not placed on a valid language element
                //     /** 54 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,9): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,22): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,33): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,44): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,54): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (9,65): warning CS1587: XML comment is not placed on a valid language element
                //         /** 55 */int /** 56 */y /** 57 */= /** 58 */x/** 59 */--/** 60 */;
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (10,5): warning CS1587: XML comment is not placed on a valid language element
                //     /** 61 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (11,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 62 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (13,15): warning CS1587: XML comment is not placed on a valid language element
                // /** 63 */enum /** 64 */E
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (14,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 65 */{
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,16): warning CS1587: XML comment is not placed on a valid language element
                //     /** 66 */A /** 67 */= /** 68 */1 /** 69 */+ /** 70 */1/** 71 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,27): warning CS1587: XML comment is not placed on a valid language element
                //     /** 66 */A /** 67 */= /** 68 */1 /** 69 */+ /** 70 */1/** 71 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,38): warning CS1587: XML comment is not placed on a valid language element
                //     /** 66 */A /** 67 */= /** 68 */1 /** 69 */+ /** 70 */1/** 71 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,49): warning CS1587: XML comment is not placed on a valid language element
                //     /** 66 */A /** 67 */= /** 68 */1 /** 69 */+ /** 70 */1/** 71 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (15,59): warning CS1587: XML comment is not placed on a valid language element
                //     /** 66 */A /** 67 */= /** 68 */1 /** 69 */+ /** 70 */1/** 71 */,
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (16,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 72 */}
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (17,1): warning CS1587: XML comment is not placed on a valid language element
                // /** 73 */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void UnprocessedXMLComment_AfterAttribute()
        {
            var source = @"
[System.Serializable]
/// Comment
class C
{
}
";
            CreateCompilationUtil(source).VerifyDiagnostics(
                // (3,1): warning CS1587: XML comment is not placed on a valid language element
                // /// Comment
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void UnprocessedXMLComment_CompiledOut()
        {
            var source = @"
class C
{
#if false
/// Comment
#endif
}
";
            CreateCompilationUtil(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnprocessedXMLComment_FilterTree()
        {
            var source1 = @"
partial class C
{
    /// Unprocessed 1
}
";
            var source2 = @"
partial class C
{
    /// Unprocessed 2
}
";
            var tree1 = Parse(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = Parse(source2, options: TestOptions.RegularWithDocumentationComments);

            var comp = CreateCompilation(new[] { tree1, tree2 });
            comp.GetSemanticModel(tree1).GetDiagnostics().Verify(
                // (4,5): warning CS1587: XML comment is not placed on a valid language element
                //     /// Unprocessed 1
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
            comp.GetSemanticModel(tree2).GetDiagnostics().Verify(
                // (4,5): warning CS1587: XML comment is not placed on a valid language element
                //     /// Unprocessed 2
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        [Fact]
        public void UnprocessedXMLComment_Unparsed()
        {
            var source = @"
partial class C
{
    /// Unprocessed 1
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(547139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547139")]
        [Fact]
        public void UnprocessedXMLComment_Accessor()
        {
            var source = @"
class MyClass
{
    string MyProperty
    {
        get;
        /// <param name=""a"" />
        /// <param name=""b"" />
        set;
    }
}
";
            CreateCompilationUtil(source).VerifyDiagnostics(
                // (7,9): warning CS1587: XML comment is not placed on a valid language element
                //         /// <param name="a" />
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));
        }

        /// <summary>
        /// Insert a numbered documentation comment as leading trivia on every token.
        /// </summary>
        private class DocumentationCommentAdder : CSharpSyntaxRewriter
        {
            private int _count;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var newToken = base.VisitToken(token);

                if (newToken.Width == 0 && newToken.Kind() != SyntaxKind.EndOfFileToken)
                {
                    return newToken;
                }

                var existingLeadingTrivia = token.LeadingTrivia;
                var newLeadingTrivia = SyntaxFactory.ParseToken("/** " + (_count++) + " */1)").LeadingTrivia;
                return newToken.WithLeadingTrivia(existingLeadingTrivia.Concat(newLeadingTrivia));
            }
        }

        #endregion WRN_UnprocessedXMLComment

        #region Invalid XML

        [Fact]
        public void InvalidXml()
        {
            var source = @"
/// <unterminated_tag
class C1 { }

/// <unterminated_element>
class C2 { }

/// <no_attribute_value attr/>
class C3 { }

/// <bad_attribute_value attr=""&""/>
class C4 { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:C1"" -->
        <!-- Badly formed XML comment ignored for member ""T:C2"" -->
        <!-- Badly formed XML comment ignored for member ""T:C3"" -->
        <!-- Badly formed XML comment ignored for member ""T:C4"" -->
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InvalidXmlOnPartialTypes()
        {
            var source = @"
/// <invalid
partial class C
{
}

/// <valid/>
partial class C
{
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:C"" -->
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void InvalidXmlOnPartialMethods()
        {
            var source = @"
partial class C
{
    /// <invalid1
    partial void M1();

    /// <valid1/>
    partial void M2();
}

partial class C
{
    /// <valid2/>
    partial void M1() { }

    /// <invalid2
    partial void M2() { }
}
";
            // NOTE: separate error comment for each part.
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M1"">
            <valid2/>
        </member>
        <!-- Badly formed XML comment ignored for member ""M:C.M1"" -->
        <!-- Badly formed XML comment ignored for member ""M:C.M2"" -->
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(637435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/637435")]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void NonXmlWhitespace()
        {
            var ch = '\u1680';
            Assert.True(char.IsWhiteSpace(ch));
            Assert.True(SyntaxFacts.IsWhitespace(ch));
            Assert.False(XmlCharType.IsWhiteSpace(ch));

            var xml = "<see\u1680cref='C'/>";
            Assert.Throws<XmlException>(() => XElement.Parse(xml));

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            var source = string.Format(sourceTemplate, xml);

            // NOTE: separate error comment for each part.
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (2,4): warning CS1570: XML comment has badly formed XML -- 'The '\u1680' character, hexadecimal value 0x1680, cannot be included in a name.'
                // /// <see cref='C'/>
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("The '\u1680' character, hexadecimal value 0x1680, cannot be included in a name."));

            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:C"" -->
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(637435, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/637435")]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void Repro637435()
        {
            var sourceTemplate = @"
// 1) Both Roslyn and Dev11 report an error.

///<see
///{0}cref='C1'/>
class C1 {{ }}


// 2) Both Roslyn and Dev11 report an error.

/// <see
/// {0}cref='C2'/>
class C2 {{ }}


// 3) Both Roslyn and Dev11 report an error.

///<see
/// {0}cref='C3'/>
class C3 {{ }}


// 4) Dev11 reports an error, but Roslyn does not.

/// <see
///{0}cref='C4'/>
class C4 {{ }}
";
            var source = string.Format(sourceTemplate, '\u1680');

            CreateCompilationUtil(source).GetDiagnostics().VerifyWithFallbackToErrorCodeOnlyForNonEnglish(
                // (4,4): warning CS1570: XML comment has badly formed XML -- 'Name cannot begin with the '\u1680' character, hexadecimal value 0x1680.'
                // ///<see
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("Name cannot begin with the '\u1680' character, hexadecimal value 0x1680."),
                // (11,4): warning CS1570: XML comment has badly formed XML -- 'Name cannot begin with the '\u1680' character, hexadecimal value 0x1680.'
                // /// <see
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("Name cannot begin with the '\u1680' character, hexadecimal value 0x1680."),
                // (18,4): warning CS1570: XML comment has badly formed XML -- 'Name cannot begin with the '\u1680' character, hexadecimal value 0x1680.'
                // ///<see
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("Name cannot begin with the '\u1680' character, hexadecimal value 0x1680."));
        }

        #endregion Invalid XML

        #region Include

        [Fact]
        public void IncludeNone()
        {
            var xml = @"
<root/>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            string xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//target'/>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);

            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//target"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeOne()
        {
            var xml = @"
<root>
    <target stuff=""things"" />
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            string xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//target'/>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);

            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <target stuff=""things"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeMultiple()
        {
            var xml = @"
<root>
    <target stuff=""things"" />
    <parent>
        <target stuff=""garbage"" />
    </parent>
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            string xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//target'/>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);

            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <target stuff=""things"" /><target stuff=""garbage"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeWithChildren_Success()
        {
            var xml = @"
<root>
    <target stuff=""things"" />
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            string xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//target'>
///   <child />
/// </include>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);

            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <target stuff=""things"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeWithChildren_Failure()
        {
            var xml = @"
<root>
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            string xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//target'>
///   <child />
/// </include>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);

            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//target"">
              <child />
            </include>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void IncludeFileResolution()
        {
            var xml1 = @"
<root>
    <include file=""test.xml"" path=""//element""/> <!--relative to d1 -->
    <include file=""d2/test.xml"" path=""//include""/> <!-- relative to root -->
    <element value=""1""/>
</root>
";

            var xml2 = @"
<root>
    <include file=""test.xml"" path=""//element""/> <!--relative to d2 -->
    <include file=""d3/test.xml"" path=""//include""/> <!-- relative to root -->
    <element value=""2""/>
</root>
";

            var xml3 = @"
<root>
    <include file=""test.xml"" path=""//element""/> <!--relative to d3 -->
    <element value=""3""/>
</root>
";

            var rootDir = Temp.CreateDirectory();

            var dir1 = rootDir.CreateDirectory("d1");
            var dir1XmlFile = dir1.CreateFile("test.xml").WriteAllText(xml1);

            var dir2 = rootDir.CreateDirectory("d2");
            var dir2XmlFile = dir2.CreateFile("test.xml").WriteAllText(xml2);

            var dir3 = rootDir.CreateDirectory("d3");
            var dir3XmlFile = dir3.CreateFile("test.xml").WriteAllText(xml3);

            var source = @"
/// <include file='d1\test.xml' path='//include' />
class C { }
";
            var tree = Parse(source, options: TestOptions.RegularWithDocumentationComments);
            var resolver = new XmlFileResolver(rootDir.Path);
            var comp = CSharpCompilation.Create("Test", new[] { tree }, new[] { MscorlibRef }, TestOptions.ReleaseDll.WithXmlReferenceResolver(resolver));
            var actual = GetDocumentationCommentText(comp);

            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <element value=""1"" /><element value=""2"" /><element value=""3"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WRN_InvalidInclude_Source()
        {
            var source = @"
/// <include/>
/// <include other='stuff'/>
/// <include path='path'/>
/// <include file='file'/>
class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1590: Invalid XML include element -- Missing file attribute
                // /// <include/>
                Diagnostic(ErrorCode.WRN_InvalidInclude, "<include/>").WithArguments("Missing file attribute"),
                // (3,5): warning CS1590: Invalid XML include element -- Missing file attribute
                // /// <include other='stuff'/>
                Diagnostic(ErrorCode.WRN_InvalidInclude, "<include other='stuff'/>").WithArguments("Missing file attribute"),
                // (4,5): warning CS1590: Invalid XML include element -- Missing file attribute
                // /// <include path='path'/>
                Diagnostic(ErrorCode.WRN_InvalidInclude, "<include path='path'/>").WithArguments("Missing file attribute"),
                // (5,5): warning CS1590: Invalid XML include element -- Missing path attribute
                // /// <include file='file'/>
                Diagnostic(ErrorCode.WRN_InvalidInclude, "<include file='file'/>").WithArguments("Missing path attribute"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Include tag is invalid --><include />
            <!-- Include tag is invalid --><include other=""stuff"" />
            <!-- Include tag is invalid --><include path=""path"" />
            <!-- Include tag is invalid --><include file=""file"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WRN_InvalidInclude_Xml()
        {
            var xml = @"
<root>
    <include/>
    <include other='stuff'/>
    <include path='path'/>
    <include file='file'/>
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFile.Path));
            var actual = GetDocumentationCommentText(comp,
                // warning CS1590: Invalid XML include element -- Missing file attribute
                Diagnostic(ErrorCode.WRN_InvalidInclude).WithArguments("Missing file attribute"),
                // warning CS1590: Invalid XML include element -- Missing file attribute
                Diagnostic(ErrorCode.WRN_InvalidInclude).WithArguments("Missing file attribute"),
                // warning CS1590: Invalid XML include element -- Missing file attribute
                Diagnostic(ErrorCode.WRN_InvalidInclude).WithArguments("Missing file attribute"),
                // warning CS1590: Invalid XML include element -- Missing path attribute
                Diagnostic(ErrorCode.WRN_InvalidInclude).WithArguments("Missing path attribute"));

            // NOTE: the whitespace is external to the selected nodes, so it's not included.
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <include /><include other=""stuff"" /><include path=""path"" /><include file=""file"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WRN_FailedInclude_NonExistent_Source()
        {
            var source = @"
/// <include file='file' path='path'/>
class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1589: Unable to include XML fragment 'path' of file 'file' -- File not found.
                // /// <include file='file' path='path'/>
                Diagnostic(ErrorCode.WRN_FailedInclude, "<include file='file' path='path'/>").WithArguments("file", "path", "File not found."));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Failed to insert some or all of included XML --><include file=""file"" path=""path"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WRN_FailedInclude_NonExistent_Xml()
        {
            var xml = @"
<root>
    <include file='file' path='path'/>
</root>
";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFile.Path));
            var actual = GetDocumentationCommentText(comp,
                // 56e57d80-44fc-4e2c-b839-0bf3d9c830b7.xml(3,6): warning CS1589: Unable to include XML fragment 'path' of file 'file' -- File not found.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments("file", "path", "File not found."));

            // NOTE: the whitespace is external to the selected nodes, so it's not included.
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <include file=""file"" path=""path"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void WRN_FailedInclude_Locked_Source()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            var includeTemplate = "<include file='{0}' path='path'/>";
            var includeElement = string.Format(includeTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            using (File.Open(xmlFilePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));
                var actual = GetDocumentationCommentText(comp,
                    // (2,5): warning CS1589: Unable to include XML fragment 'path' of file 'c3af0dc5a3cf.xml' -- The process cannot access the file 'c3af0dc5a3cf.xml' because it is being used by another process.
                    // /// <include file='c3af0dc5a3cf.xml' path='path'/>
                    Diagnostic(ErrorCode.WRN_FailedInclude, includeElement).WithArguments(xmlFilePath, "path", string.Format("The process cannot access the file '{0}' because it is being used by another process.", xmlFilePath)));
                var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Failed to insert some or all of included XML --><include file=""{0}"" path=""path"" />
        </member>
    </members>
</doc>
").Trim();
                Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
            }
        }

        [ClrOnlyFact(ClrOnlyReason.Unknown)]
        public void WRN_FailedInclude_Locked_Xml()
        {
            var xmlFile1 = Temp.CreateFile(extension: ".xml");
            var xmlFilePath1 = xmlFile1.Path;

            var xmlFile2 = Temp.CreateFile(extension: ".xml").WriteAllText(string.Format("<include file='{0}' path='path'/>", xmlFilePath1));
            var xmlFilePath2 = xmlFile2.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            using (File.Open(xmlFilePath1, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath2));
                var actual = GetDocumentationCommentText(comp,
                    // 3fba660141b6.xml(1,2): warning CS1589: Unable to include XML fragment 'path' of file 'd4241d125755.xml' -- The process cannot access the file 'd4241d125755.xml' because it is being used by another process.
                    Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath1, "path", string.Format("The process cannot access the file '{0}' because it is being used by another process.", xmlFilePath1)));
                var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <include file=""{0}"" path=""path"" />
        </member>
    </members>
</doc>
").Trim();
                Assert.Equal(string.Format(expectedTemplate, xmlFilePath1), actual);
            }
        }

        [Fact]
        public void WRN_FailedInclude_XPath_Source()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<element/>");
            var xmlFilePath = xmlFile.Path;

            var includeTemplate = "<include file='{0}' path=':'/>";
            var includeElement = string.Format(includeTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));
            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1589: Unable to include XML fragment 'path' of file 'c3af0dc5a3cf.xml' -- The process cannot access the file 'c3af0dc5a3cf.xml' because it is being used by another process.
                // /// <include file='c3af0dc5a3cf.xml' path='path'/>
                Diagnostic(ErrorCode.WRN_FailedInclude, includeElement).WithArguments(xmlFilePath, ":", "':' has an invalid token."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Failed to insert some or all of included XML --><include file=""{0}"" path="":"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void WRN_FailedInclude_XPath_Xml()
        {
            var xmlFile1 = Temp.CreateFile(extension: ".xml").WriteAllText("<element/>");
            var xmlFilePath1 = xmlFile1.Path;

            var xmlFile2 = Temp.CreateFile(extension: ".xml").WriteAllText(string.Format("<include file='{0}' path=':'/>", xmlFilePath1));
            var xmlFilePath2 = xmlFile2.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath2));
            var actual = GetDocumentationCommentText(comp,
                // 3fba660141b6.xml(1,2): warning CS1589: Unable to include XML fragment 'path' of file 'd4241d125755.xml' -- The process cannot access the file 'd4241d125755.xml' because it is being used by another process.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath1, ":", "':' has an invalid token."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <include file=""{0}"" path="":"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath1), actual);
        }

        [ClrOnlyFact(ClrOnlyReason.DocumentationComment, Skip = "https://github.com/dotnet/roslyn/issues/8807")]
        public void WRN_XMLParseIncludeError_Source()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<OpenWithoutClose>");
            var xmlFilePath = xmlFile.Path;

            var includeTemplate = "<include file='{0}' path='path'/>";
            var includeElement = string.Format(includeTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));
            var actual = GetDocumentationCommentText(comp,
                // 327697461814.xml(1,19): warning CS1592: Badly formed XML in included comments file -- 'Unexpected end of file has occurred. The following elements are not closed: OpenWithoutClose.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Unexpected end of file has occurred. The following elements are not closed: OpenWithoutClose."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, TestHelpers.AsXmlCommentText(xmlFilePath)), actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void WRN_XMLParseIncludeError_Xml()
        {
            var xmlFile1 = Temp.CreateFile(extension: ".xml").WriteAllText("<OpenWithoutClose>");
            var xmlFilePath1 = xmlFile1.Path;

            var xmlFile2 = Temp.CreateFile(extension: ".xml").WriteAllText(string.Format("<include file='{0}' path='path'/>", xmlFilePath1));
            var xmlFilePath2 = xmlFile2.Path;

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath2));
            var actual = GetDocumentationCommentText(comp,
                // 408eee49f410.xml(1,19): warning CS1592: Badly formed XML in included comments file -- 'Unexpected end of file has occurred. The following elements are not closed: OpenWithoutClose.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Unexpected end of file has occurred. The following elements are not closed: OpenWithoutClose."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath2), actual);
        }

        [Fact]
        public void IncludeCycle_Simple()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            xmlFile.WriteAllText(string.Format(@"<include file=""{0}"" path=""//include""/>", xmlFilePath)); //Includes itself.

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp,
                // 3fba660141b6.xml(1,2): warning CS1589: Unable to include XML fragment 'path' of file 'd4241d125755.xml' -- The process cannot access the file 'd4241d125755.xml' because it is being used by another process.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeCycle_Containment()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            xmlFile.WriteAllText(string.Format(@"<parent><include file=""{0}"" path=""//parent""/></parent>", xmlFilePath)); //Includes its parent.

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));

            // CONSIDER: differs from dev11, but this is a reasonable recovery.
            var actual = GetDocumentationCommentText(comp,
                // 3fba660141b6.xml(1,2): warning CS1589: Unable to include XML fragment 'path' of file 'd4241d125755.xml' -- The process cannot access the file 'd4241d125755.xml' because it is being used by another process.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//parent", "Operation caused a stack overflow."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <parent><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//parent"" /></parent>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        [Fact]
        public void IncludeCycle_Nesting()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            xmlFile.WriteAllText(string.Format(@"
<include file=""{0}"" path=""//include"">
    <include file=""{0}"" path=""//include"" />
</include>", xmlFilePath)); //Everything includes everything, includes within includes.

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));

            // CONSIDER: not checked against dev11 - just don't blow up.
            var actual = GetDocumentationCommentText(comp,
                // 1dc0fa5fb526.xml(2,2): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."),
                // 1dc0fa5fb526.xml(2,2): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."),
                // 1dc0fa5fb526.xml(2,2): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."),
                // 1dc0fa5fb526.xml(3,6): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."),
                // 1dc0fa5fb526.xml(3,6): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."),
                // 1dc0fa5fb526.xml(3,6): warning CS1589: Unable to include XML fragment '//include' of file '1dc0fa5fb526.xml' -- Operation caused a stack overflow.
                Diagnostic(ErrorCode.WRN_FailedInclude).WithArguments(xmlFilePath, "//include", "Operation caused a stack overflow."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"">
    <include file=""{0}"" path=""//include"" />
</include><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"">
    <include file=""{0}"" path=""//include"" />
</include><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"" /><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"">
    <include file=""{0}"" path=""//include"" />
</include><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"" /><!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//include"" />
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath), actual);
        }

        // It should be legal to include the same acyclic element along multiple paths - that isn't a cycle.
        [Fact]
        public void IncludeAlongMultiplePaths()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            string xmlTemplate = @"
<root>
  <include file=""{0}"" path=""//stuff""/>
  <stuff/>
</root>";
            xmlFile.WriteAllText(string.Format(xmlTemplate, xmlFilePath));

            var sourceTemplate = @"
/// <include file='{0}' path='//include'/>
/// <include file='{0}' path='//include'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <stuff />
            <stuff />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        // As in dev11, the xpath is evaluated *before* includes are expanded.
        [Fact]
        public void XPathAssumesExpandedInclude()
        {
            var xmlFile1 = Temp.CreateFile(extension: ".xml");
            var xmlFilePath1 = xmlFile1.Path;

            var xmlFile2 = Temp.CreateFile(extension: ".xml");
            var xmlFilePath2 = xmlFile2.Path;

            string xmlTemplate1 = @"<include file=""{0}"" path=""//stuff""/>";
            string xml2 = @"<stuff/>";

            xmlFile1.WriteAllText(string.Format(xmlTemplate1, xmlFilePath2));
            xmlFile2.WriteAllText(xml2);

            var sourceTemplate = @"
/// <include file='{0}' path='//stuff'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath1));

            var actual = GetDocumentationCommentText(comp);
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- No matching elements were found for the following include tag --><include file=""{0}"" path=""//stuff"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(string.Format(expectedTemplate, xmlFilePath1), actual);
        }

        [WorkItem(554196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554196")]
        [Fact]
        public void XPathDocumentRoot()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;

            xmlFile.WriteAllText(@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Roslyn.Utilities</name>
    </assembly>
    <members>
    </members>
</doc>");

            var sourceTemplate = @"
/// <include file=""{0}"" path=""/""/>
enum A {{ }}

/// <include file=""{0}"" path="".""/>
enum B {{ }}

/// <include file=""{0}"" path=""doc""/>
enum C {{ }}

/// <include file=""{0}"" path=""/doc""/>
enum D {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));

            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1589: Unable to include XML fragment '/' of file '012bf028d62c.xml' -- The XPath expression evaluated to unexpected type System.Xml.Linq.XDocument.
                // /// <include file="012bf028d62c.xml" path="/"/>
                Diagnostic(ErrorCode.WRN_FailedInclude, string.Format(@"<include file=""{0}"" path=""/""/>", xmlFilePath)).WithArguments(xmlFilePath, "/", "The XPath expression evaluated to unexpected type System.Xml.Linq.XDocument."),
                // (5,5): warning CS1589: Unable to include XML fragment '.' of file '012bf028d62c.xml' -- The XPath expression evaluated to unexpected type System.Xml.Linq.XDocument.
                // /// <include file="012bf028d62c.xml" path="."/>
                Diagnostic(ErrorCode.WRN_FailedInclude, string.Format(@"<include file=""{0}"" path="".""/>", xmlFilePath)).WithArguments(xmlFilePath, ".", "The XPath expression evaluated to unexpected type System.Xml.Linq.XDocument."));

            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A"">
            <!-- Failed to insert some or all of included XML -->
        </member>
        <member name=""T:B"">
            <!-- Failed to insert some or all of included XML -->
        </member>
        <member name=""T:C"">
            <doc>
    <assembly>
        <name>Roslyn.Utilities</name>
    </assembly>
    <members>
    </members>
</doc>
        </member>
        <member name=""T:D"">
            <doc>
    <assembly>
        <name>Roslyn.Utilities</name>
    </assembly>
    <members>
    </members>
</doc>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        #region Included crefs

        [Fact]
        public void IncludedCref_Valid()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<see cref=""Main""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C
{{
    static void Main() {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""M:C.Main"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedCref_Verbatim()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<see cref=""M:Verbatim""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C
{{
    static void Main() {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""M:Verbatim"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedCref_MultipleSyntaxTrees()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<see cref=""Int32""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
namespace N
{{
    /// {0}
    class C {{ }}
}}

namespace N
{{
    using System;

    /// {0}
    class C {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            // Error for the first include, but not for the second.
            var actual = GetDocumentationCommentText(comp,
                // (4,9): warning CS1574: XML comment has cref attribute 'Int32' that could not be resolved
                Diagnostic(ErrorCode.WRN_BadXMLRef, includeElement).WithArguments("Int32"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:N.C"">
            <see cref=""!:Int32"" />
            <see cref=""T:System.Int32"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedCref_SyntaxError()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<see cref=""#""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1584: XML comment has syntactically incorrect cref attribute '#'
                // /// <include file='aa671ee8adcd.xml' path='//see'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, includeElement).WithArguments("#"),
                // (2,5): warning CS1658: Identifier expected. See also error CS1001.
                // /// <include file='aa671ee8adcd.xml' path='//see'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Identifier expected", "1001"),
                // (2,5): warning CS1658: Unexpected character '#'. See also error CS1056.
                // /// <include file='aa671ee8adcd.xml' path='//see'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Unexpected character '#'", "1056"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""!:#"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedCref_SemanticError()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<see cref=""Invalid""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1574: XML comment has cref attribute 'Invalid' that could not be resolved
                // /// <include file='f76ef125d03d.xml' path='//see'/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, includeElement).WithArguments("Invalid"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""!:Invalid"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(552495, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552495")]
        [Fact]
        public void IncludeMismatchedQuotationMarks()
        {
            var source = @"
/// <summary>
/// <include file='C:\file.xml"" path=""/""/>
/// </summary>
class C { }
";

            // This is mode typically used by the IDE.
            var tree = Parse(source, options: TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse));
            var compilation = CreateCompilation(tree);
            compilation.VerifyDiagnostics();
        }

        [WorkItem(598371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598371")]
        [Fact]
        public void CrefParameterOrReturnTypeLookup1()
        {
            var seeElement = @"<see cref=""Y.implicit operator Y.Y""/>";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(seeElement);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
class X
{{
    /// {0}
    /// {1}
    public class Y : X
    {{
        public static implicit operator Y(int x)
        {{
            return null;
        }}
    }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, seeElement, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:X.Y"">
            <see cref=""M:X.Y.op_Implicit(System.Int32)~X.Y"" />
            <see cref=""M:X.Y.op_Implicit(System.Int32)~X.Y"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(586815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586815")]
        [Fact]
        public void CrefParameterOrReturnTypeLookup2()
        {
            var seeElement = @"<see cref=""Foo(B)""/>";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(seeElement);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//see'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
class A<T>
{{
    class B : A<B>
    {{
        /// {0}
        /// {1}
        void Foo(B x) {{ }}
    }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, seeElement, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:A`1.B.Foo(A{A{`0}.B}.B)"">
            <see cref=""M:A`1.B.Foo(A{A{`0}.B}.B)"" />
            <see cref=""M:A`1.B.Foo(A{A{`0}.B}.B)"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Included crefs

        #region Included names

        [Fact]
        public void IncludedName_Success()
        {
            var xml = @"
<root>
    <member0>
        <summary>
            <typeparam name=""T"">Text</typeparam>
            <typeparamref name=""T"">Text</typeparamref>
        </summary>
    </member0>
    <member1>
        <summary>
            <param name=""x"">Text</param>
            <paramref name=""x"">Text</paramref>
        </summary>
    </member1>
    <member2>
        <summary>
            <param name=""u"">Text</param>
            <paramref name=""u"">Text</paramref>
            <typeparam name=""U"">Text</typeparam>
            <typeparamref name=""U"">Text</typeparamref>
        </summary>
    </member2>
    <member3>
        <summary>
            <param name=""v"">Text</param>
            <paramref name=""v"">Text</paramref>
            <typeparam name=""V"">Text</typeparam>
            <typeparamref name=""V"">Text</typeparamref>
        </summary>
    </member3>
</root>
";

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='root/member{1}/summary'/>";
            string[] includeElements = Enumerable.Range(0, 4).Select(i => string.Format(includeElementTemplate, xmlFilePath, i)).ToArray();

            var sourceTemplate = @"
/// {0}
class C<T>
{{
    /// {1}
    int this[int x] {{ get {{ return 0; }} set {{ }} }}
    
    /// {2}
    void M<U>(U u) {{ }}

    /// {3}
    delegate void D<V>(V v) {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElements));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C`1"">
            <summary>
            <typeparam name=""T"">Text</typeparam>
            <typeparamref name=""T"">Text</typeparamref>
        </summary>
        </member>
        <member name=""P:C`1.Item(System.Int32)"">
            <summary>
            <param name=""x"">Text</param>
            <paramref name=""x"">Text</paramref>
        </summary>
        </member>
        <member name=""M:C`1.M``1(``0)"">
            <summary>
            <param name=""u"">Text</param>
            <paramref name=""u"">Text</paramref>
            <typeparam name=""U"">Text</typeparam>
            <typeparamref name=""U"">Text</typeparamref>
        </summary>
        </member>
        <member name=""T:C`1.D`1"">
            <summary>
            <param name=""v"">Text</param>
            <paramref name=""v"">Text</paramref>
            <typeparam name=""V"">Text</typeparam>
            <typeparamref name=""V"">Text</typeparamref>
        </summary>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedName_OverlappedWithSource()
        {
            var xml = @"
<root>
    <param name=""u"">Text</param>
    <param name=""v"">Text</param>
    <paramref name=""u"">Text</paramref>
    <paramref name=""v"">Text</paramref>
    <typeparam name=""U"">Text</typeparam>
    <typeparam name=""V"">Text</typeparam>
    <typeparamref name=""U"">Text</typeparamref>
    <typeparamref name=""V"">Text</typeparamref>
</root>
";

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='root/*'/>";
            string includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// <param name=""u"">Text</param>
/// <param name=""v"">Text</param>
/// <paramref name=""u"">Text</paramref>
/// <paramref name=""v"">Text</paramref>
/// <typeparam name=""U"">Text</typeparam>
/// <typeparam name=""V"">Text</typeparam>
/// <typeparamref name=""U"">Text</typeparamref>
/// <typeparamref name=""V"">Text</typeparamref>
/// {0}
delegate void D<U, V>(U u, V v);
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // (10,5): warning CS1571: XML comment has a duplicate param tag for 'u'
                // /// <include file='f59a2ef50b4d.xml' path='root/*'/>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, includeElement).WithArguments("u"),
                // (10,5): warning CS1571: XML comment has a duplicate param tag for 'v'
                // /// <include file='f59a2ef50b4d.xml' path='root/*'/>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, includeElement).WithArguments("v"),
                // (10,5): warning CS1710: XML comment has a duplicate typeparam tag for 'U'
                // /// <include file='f59a2ef50b4d.xml' path='root/*'/>
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, includeElement).WithArguments("U"),
                // (10,5): warning CS1710: XML comment has a duplicate typeparam tag for 'V'
                // /// <include file='f59a2ef50b4d.xml' path='root/*'/>
                Diagnostic(ErrorCode.WRN_DuplicateTypeParamTag, includeElement).WithArguments("V"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:D`2"">
            <param name=""u"">Text</param>
            <param name=""v"">Text</param>
            <paramref name=""u"">Text</paramref>
            <paramref name=""v"">Text</paramref>
            <typeparam name=""U"">Text</typeparam>
            <typeparam name=""V"">Text</typeparam>
            <typeparamref name=""U"">Text</typeparamref>
            <typeparamref name=""V"">Text</typeparamref>
            <param name=""u"">Text</param><param name=""v"">Text</param><paramref name=""u"">Text</paramref><paramref name=""v"">Text</paramref><typeparam name=""U"">Text</typeparam><typeparam name=""V"">Text</typeparam><typeparamref name=""U"">Text</typeparamref><typeparamref name=""V"">Text</typeparamref>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedName_MixedWithSource()
        {
            var xml = @"
<root>
    <param name=""v"">Text</param>
    <paramref name=""u"">Text</paramref>
    <paramref name=""v"">Text</paramref>
    <typeparam name=""V"">Text</typeparam>
    <typeparamref name=""U"">Text</typeparamref>
    <typeparamref name=""V"">Text</typeparamref>
</root>
";

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='root/*'/>";
            string includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// <param name=""u"">Text</param>
/// <paramref name=""u"">Text</paramref>
/// <paramref name=""v"">Text</paramref>
/// <typeparam name=""U"">Text</typeparam>
/// <typeparamref name=""U"">Text</typeparamref>
/// <typeparamref name=""V"">Text</typeparamref>
/// {0}
delegate void D<U, V>(U u, V v);
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:D`2"">
            <param name=""u"">Text</param>
            <paramref name=""u"">Text</paramref>
            <paramref name=""v"">Text</paramref>
            <typeparam name=""U"">Text</typeparam>
            <typeparamref name=""U"">Text</typeparamref>
            <typeparamref name=""V"">Text</typeparamref>
            <param name=""v"">Text</param><paramref name=""u"">Text</paramref><paramref name=""v"">Text</paramref><typeparam name=""V"">Text</typeparam><typeparamref name=""U"">Text</typeparamref><typeparamref name=""V"">Text</typeparamref>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedName_SyntacticError()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<typeparam name=""#""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//typeparam'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C<T> {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1658: Unexpected character '#'. See also error CS1056.
                // /// <include file='3d2052d10358.xml' path='//typeparam'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, includeElement).WithArguments("Unexpected character '#'", "1056"),
                // (3,9): warning CS1712: Type parameter 'T' has no matching typeparam tag in the XML comment on 'C<T>' (but other type parameters do)
                // class C<T> { }
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "T").WithArguments("T", "C<T>"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C`1"">
            <typeparam name=""#"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedName_SemanticError()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<param name=""Q""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//param'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
class C
{{ 
    /// {0}
    void M(int x) {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // (4,9): warning CS1572: XML comment has a param tag for 'Q', but there is no parameter by that name
                //     /// <include file='4f57d3a0db53.xml' path='//param'/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, includeElement).WithArguments("Q"),
                // (5,16): warning CS1573: Parameter 'x' has no matching param tag in the XML comment for 'C.M(int)' (but other parameters do)
                //     void M(int x) { }
                Diagnostic(ErrorCode.WRN_MissingParamTag, "x").WithArguments("x", "C.M(int)"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32)"">
            <param name=""Q"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IncludedName_DuplicateParameterName()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<param name=""x""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//param'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
class C
{{ 
    /// {0}
    void M(int x, int x) {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            // NOTE: no *xml* diagnostics, not no diagnostics.
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32,System.Int32)"">
            <param name=""x"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        [ClrOnlyFact(ClrOnlyReason.DocumentationComment, Skip = "https://github.com/dotnet/roslyn/issues/8807")]
        public void IncludedName_DuplicateNameAttribute()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(@"<param name=""x"" name=""y""/>");
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//param'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
class C
{{ 
    /// {0}
    void M(int x, int y) {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp,
                // df33b60df5a9.xml(1,17): warning CS1592: Badly formed XML in included comments file -- ''name' is a duplicate attribute name.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("'name' is a duplicate attribute name."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32,System.Int32)"">
            <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(string.Format(expectedTemplate, TestHelpers.AsXmlCommentText(xmlFilePath)), actual);
        }

        [Fact]
        public void IncludedName_PartialMethod()
        {
            string xml = @"
<root>
    <param name=""x""/>
    <param name=""y""/>
</root>";
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//param'/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
partial class C
{{ 
    /// Part 1.
    /// {0}
    partial void M(int x, int y);
}}

partial class C
{{ 
    /// Part 2.
    /// {0}
    partial void M(int x, int y) {{ }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp);
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32,System.Int32)"">
            Part 2.
            <param name=""x"" /><param name=""y"" />
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(string.Format(expectedTemplate, TestHelpers.AsXmlCommentText(xmlFilePath)), actual);
        }

        [Fact]
        [WorkItem(21348, "https://github.com/dotnet/roslyn/issues/21348")]
        public void IncludedName_TypeParamAndTypeParamRefHandling()
        {
            var xml = @"
<root>
    <target>
    Included section
    <summary>
      See <typeparam/>.
      See <typeparam name=""X""/>.
      See <typeparam name=""Y""/>.
      See <typeparam name=""XY""/>.
    </summary>
    <remarks></remarks>
    </target>
    <target>
    Included section
    <summary>
      See <typeparamref/>.
      See <typeparamref name=""X""/>.
      See <typeparamref name=""Y""/>.
      See <typeparamref name=""XY""/>.
    </summary>
    <remarks></remarks>
    </target>
</root>
";

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var includeElementTemplate = @"<include file='{0}' path='//target'/>";
            string includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"

/// {0}
class OuterClass<X>
{{
    /// {0}
    class InnerClass<Y>
    {{
        /// {0}
        public void Foo() {{}}
    }}
}}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));

            var actual = GetDocumentationCommentText(comp, expectedDiagnostics: new[] {
                // (3,5): warning CS1711: XML comment has a typeparam tag for 'Y', but there is no type parameter by that name
                // /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("Y").WithLocation(3, 5),
                // (3,5): warning CS1711: XML comment has a typeparam tag for 'XY', but there is no type parameter by that name
                // /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("XY").WithLocation(3, 5),
                // (3,5): warning CS1735: XML comment on 'OuterClass<X>' has a typeparamref tag for 'Y', but there is no type parameter by that name
                // /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, includeElement).WithArguments("Y", "OuterClass<X>").WithLocation(3, 5),
                // (3,5): warning CS1735: XML comment on 'OuterClass<X>' has a typeparamref tag for 'XY', but there is no type parameter by that name
                // /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, includeElement).WithArguments("XY", "OuterClass<X>").WithLocation(3, 5),
                // (6,9): warning CS1711: XML comment has a typeparam tag for 'X', but there is no type parameter by that name
                //     /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("X").WithLocation(6, 9),
                // (6,9): warning CS1711: XML comment has a typeparam tag for 'XY', but there is no type parameter by that name
                //     /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("XY").WithLocation(6, 9),
                // (6,9): warning CS1735: XML comment on 'OuterClass<X>.InnerClass<Y>' has a typeparamref tag for 'XY', but there is no type parameter by that name
                //     /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, includeElement).WithArguments("XY", "OuterClass<X>.InnerClass<Y>").WithLocation(6, 9),
                // (9,13): warning CS1711: XML comment has a typeparam tag for 'X', but there is no type parameter by that name
                //         /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("X").WithLocation(9, 13),
                // (9,13): warning CS1711: XML comment has a typeparam tag for 'Y', but there is no type parameter by that name
                //         /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("Y").WithLocation(9, 13),
                // (9,13): warning CS1711: XML comment has a typeparam tag for 'XY', but there is no type parameter by that name
                //         /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("XY").WithLocation(9, 13),
                // (9,13): warning CS1735: XML comment on 'OuterClass<X>.InnerClass<Y>.Foo()' has a typeparamref tag for 'XY', but there is no type parameter by that name
                //         /// <include file='b16c2dc7f738.xml' path='//target'/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, includeElement).WithArguments("XY", "OuterClass<X>.InnerClass<Y>.Foo()").WithLocation(9, 13)
            });
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:OuterClass`1"">
            <target>
    Included section
    <summary>
      See <typeparam />.
      See <typeparam name=""X"" />.
      See <typeparam name=""Y"" />.
      See <typeparam name=""XY"" />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <typeparamref />.
      See <typeparamref name=""X"" />.
      See <typeparamref name=""Y"" />.
      See <typeparamref name=""XY"" />.
    </summary>
    <remarks />
    </target>
        </member>
        <member name=""T:OuterClass`1.InnerClass`1"">
            <target>
    Included section
    <summary>
      See <typeparam />.
      See <typeparam name=""X"" />.
      See <typeparam name=""Y"" />.
      See <typeparam name=""XY"" />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <typeparamref />.
      See <typeparamref name=""X"" />.
      See <typeparamref name=""Y"" />.
      See <typeparamref name=""XY"" />.
    </summary>
    <remarks />
    </target>
        </member>
        <member name=""M:OuterClass`1.InnerClass`1.Foo"">
            <target>
    Included section
    <summary>
      See <typeparam />.
      See <typeparam name=""X"" />.
      See <typeparam name=""Y"" />.
      See <typeparam name=""XY"" />.
    </summary>
    <remarks />
    </target><target>
    Included section
    <summary>
      See <typeparamref />.
      See <typeparamref name=""X"" />.
      See <typeparamref name=""Y"" />.
      See <typeparamref name=""XY"" />.
    </summary>
    <remarks />
    </target>
        </member>
    </members>
</doc>
        ").Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Included names

        #endregion Include

        #region For single symbol

        [Fact]
        public void ForSingleType()
        {
            var source = @"
/// <summary>
///  A
///   B
///  C
/// </summary>
class C { }
";

            var compilation = CreateCompilationUtil(source);

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var actualText = DocumentationCommentCompiler.GetDocumentationCommentXml(type, processIncludes: true, cancellationToken: default(CancellationToken));
            var expectedText =
@"<member name=""T:C"">
    <summary>
     A
      B
     C
    </summary>
</member>
";
            Assert.Equal(expectedText, actualText);
        }

        [Fact]
        public void ForSingleTypeWithInclude()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<stuff />");
            var xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <summary>
///  A
///   <include file='{0}' path='stuff'/>
///  C
/// </summary>
class C 
{{
    /// Shouldn't appear in doc comment for C.
    void M(){{}}
}}
";
            var source = string.Format(sourceTemplate, xmlFilePath);

            var compilation = CreateCompilationUtil(source);

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            // Expand includes.
            {
                var actualText = DocumentationCommentCompiler.GetDocumentationCommentXml(type, processIncludes: true, cancellationToken: default(CancellationToken));
                var expectedText =
                    @"<member name=""T:C"">
    <summary>
     A
      <stuff />
     C
    </summary>
</member>
";
                Assert.Equal(expectedText, actualText);
            }

            // Do not expand includes.
            {
                var actualText = DocumentationCommentCompiler.GetDocumentationCommentXml(type, processIncludes: false, cancellationToken: default(CancellationToken));
                var expectedTextTemplate =
                    @"<member name=""T:C"">
    <summary>
     A
      <include file='{0}' path='stuff'/>
     C
    </summary>
</member>
";
                Assert.Equal(string.Format(expectedTextTemplate, xmlFilePath), actualText);
            }
        }

        #endregion

        #region Misc

        [Fact]
        public void FilterTree()
        {
            var source1 = @"
partial class C
{
    /// <see cref=""Bogus1""/>
    void M1() { }
}
";

            var source2 = @"
partial class C
{
    /// <see cref=""Bogus2""/>
    void M2() { }
}
";

            var tree1 = SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.RegularWithDocumentationComments);
            var tree2 = SyntaxFactory.ParseSyntaxTree(source2, options: TestOptions.RegularWithDocumentationComments);

            // Files passed in order.
            var comp = CreateCompilation(new[] { tree1, tree2 }, assemblyName: "Test");

            var actual1 = GetDocumentationCommentText(comp, null, filterTree: tree1, expectedDiagnostics: new[] {
                // (4,20): warning CS1574: XML comment has cref attribute 'Bogus1' that could not be resolved
                //     /// <see cref="Bogus1"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Bogus1").WithArguments("Bogus1") });
            var expected1 = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M1"">
            <see cref=""!:Bogus1""/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected1, actual1);

            var actual2 = GetDocumentationCommentText(comp, null, filterTree: tree2, expectedDiagnostics: new[] {
                // (4,20): warning CS1574: XML comment has cref attribute 'Bogus2' that could not be resolved
                //     /// <see cref="Bogus2"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "Bogus2").WithArguments("Bogus2")});
            var expected2 = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M2"">
            <see cref=""!:Bogus2""/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected2, actual2);
        }

        [Fact]
        public void Utf8()
        {
            // NOTE: This character is interesting because it has a three-byte utf-8 representation.
            var source = "///\u20ac" + @"
public class C
{
    static void Main() {}
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp, "OutputName");
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>OutputName</name>
    </assembly>
    <members>
        <member name=""T:C"">
            " + "\u20ac" + @"
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact, WorkItem(921838, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/921838")]
        public void InaccessibleMembers()
        {
            var source =
@"/// <summary>
/// See <see cref=""C.M""/>.
/// </summary>
class A
{
}

class C
{
    private void M() { }
}";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp, "OutputName");
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>OutputName</name>
    </assembly>
    <members>
        <member name=""T:A"">
            <summary>
            See <see cref=""M:C.M""/>.
            </summary>
        </member>
    </members>
</doc>").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(531144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531144")]
        [Fact]
        public void NamespaceCref()
        {
            var source = @"
/// <see cref=""System""/>
public class C
{
    static void Main() {}
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""N:System""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(531144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531144")]
        [Fact]
        public void SymbolKinds()
        {
            var source = @"
using AliasN = System;
using AliasT = System.String;

class Generic<T>
{
    /// <summary>
    /// Namespace alias <see cref=""AliasN""/>
    /// Type alias <see cref=""AliasT""/>
    /// Array type <see cref=""C[]""/> -- warning
    /// There's no way to get a cref to bind to an assembly.
    /// Dynamic type <see cref=""dynamic""/> -- warning
    /// Error type <see cref=""C{T}""/> -- warning
    /// Event <see cref=""E""/>
    /// Field <see cref=""f""/>
    /// There's no way to get a cref to bind to a label.
    /// There's no way to get a cref to bind to a local.
    /// Method <see cref=""M""/>
    /// There's no way to get a cref to bind to a net module.
    /// Named type <see cref=""C""/>
    /// Namespace <see cref=""System""/>
    /// There's no way to get a cref to bind to a parameter.
    /// Pointer type <see cref=""C*""/> -- warning
    /// Property <see cref=""P""/>
    /// There's no way to get a cref to bind to a range variable.
    /// Type parameter <see cref=""T""/> -- warning
    /// </summary>
    public class C
    {
        int f;
        event System.Action E;
        int P { get; set; }
        void M() {}
    }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics(

                // Cref parse warnings.

                // (10,31): warning CS1584: XML comment has syntactically incorrect cref attribute 'C[]'
                //     /// Array type <see cref="C[]"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C").WithArguments("C[]"),
                // (23,33): warning CS1584: XML comment has syntactically incorrect cref attribute 'C*'
                //     /// Pointer type <see cref="C*"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C").WithArguments("C*"),

                // Boring warnings.

                // (31,29): warning CS0067: The event 'Generic<T>.C.E' is never used
                //         event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Generic<T>.C.E"),
                // (30,13): warning CS0169: The field 'Generic<T>.C.f' is never used
                //         int f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "f").WithArguments("Generic<T>.C.f"),

                // Cref binding warnings.

                // (12,33): warning CS1574: XML comment has cref attribute 'dynamic' that could not be resolved
                //     /// Dynamic type <see cref="dynamic"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRef, "dynamic").WithArguments("dynamic"),
                // (13,31): warning CS1574: XML comment has cref attribute 'C{T}' that could not be resolved
                //     /// Error type <see cref="C{T}"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{T}").WithArguments("C{T}"),
                // (26,35): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                //     /// Type parameter <see cref="T"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"));

            var actual = GetDocumentationCommentText(comp,
                // (12,33): warning CS1574: XML comment has cref attribute 'dynamic' that could not be resolved
                //     /// Dynamic type <see cref="dynamic"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRef, "dynamic").WithArguments("dynamic"),
                // (13,31): warning CS1574: XML comment has cref attribute 'C{T}' that could not be resolved
                //     /// Error type <see cref="C{T}"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{T}").WithArguments("C{T}"),
                // (26,35): warning CS1723: XML comment has cref attribute 'T' that refers to a type parameter
                //     /// Type parameter <see cref="T"/> -- warning
                Diagnostic(ErrorCode.WRN_BadXMLRefTypeVar, "T").WithArguments("T"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Generic`1.C"">
            <summary>
            Namespace alias <see cref=""N:System""/>
            Type alias <see cref=""T:System.String""/>
            Array type <see cref=""!:C[]""/> -- warning
            There's no way to get a cref to bind to an assembly.
            Dynamic type <see cref=""!:dynamic""/> -- warning
            Error type <see cref=""!:C&lt;T&gt;""/> -- warning
            Event <see cref=""E:Generic`1.C.E""/>
            Field <see cref=""F:Generic`1.C.f""/>
            There's no way to get a cref to bind to a label.
            There's no way to get a cref to bind to a local.
            Method <see cref=""M:Generic`1.C.M""/>
            There's no way to get a cref to bind to a net module.
            Named type <see cref=""T:Generic`1.C""/>
            Namespace <see cref=""N:System""/>
            There's no way to get a cref to bind to a parameter.
            Pointer type <see cref=""!:C*""/> -- warning
            Property <see cref=""P:Generic`1.C.P""/>
            There's no way to get a cref to bind to a range variable.
            Type parameter <see cref=""!:T""/> -- warning
            </summary>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(530695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530695")]
        [Fact]
        public void FieldDocComment()
        {
            var source = @"
class C
{
    /// 1
    int f;
    /// 2
    int g, h;

    /// 3
    event System.Action p;
    /// 4
    event System.Action q, r;
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""F:C.f"">
            1
        </member>
        <member name=""F:C.g"">
            2
        </member>
        <member name=""F:C.h"">
            2
        </member>
        <member name=""E:C.p"">
            3
        </member>
        <member name=""E:C.q"">
            4
        </member>
        <member name=""E:C.r"">
            4
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(530695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530695")]
        [Fact]
        public void FieldDocCommentDiagnostics()
        {
            var source = @"
class C
{
    /// <see cref=""fake1""/>
    int f;
    /// <see cref=""fake2""/>
    int g, h;

    /// <see cref=""fake3""/>
    event System.Action p;
    /// <see cref=""fake4""/>
    event System.Action q, r;
}
";
            // Duplicate diagnostics, as in dev11.
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (4,20): warning CS1574: XML comment has cref attribute 'fake1' that could not be resolved
                //     /// <see cref="fake1"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake1").WithArguments("fake1"),
                // (6,20): warning CS1574: XML comment has cref attribute 'fake2' that could not be resolved
                //     /// <see cref="fake2"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake2").WithArguments("fake2"),
                // (6,20): warning CS1574: XML comment has cref attribute 'fake2' that could not be resolved
                //     /// <see cref="fake2"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake2").WithArguments("fake2"),
                // (9,20): warning CS1574: XML comment has cref attribute 'fake3' that could not be resolved
                //     /// <see cref="fake3"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake3").WithArguments("fake3"),
                // (11,20): warning CS1574: XML comment has cref attribute 'fake4' that could not be resolved
                //     /// <see cref="fake4"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake4").WithArguments("fake4"),
                // (11,20): warning CS1574: XML comment has cref attribute 'fake4' that could not be resolved
                //     /// <see cref="fake4"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "fake4").WithArguments("fake4"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""F:C.f"">
            <see cref=""!:fake1""/>
        </member>
        <member name=""F:C.g"">
            <see cref=""!:fake2""/>
        </member>
        <member name=""F:C.h"">
            <see cref=""!:fake2""/>
        </member>
        <member name=""E:C.p"">
            <see cref=""!:fake3""/>
        </member>
        <member name=""E:C.q"">
            <see cref=""!:fake4""/>
        </member>
        <member name=""E:C.r"">
            <see cref=""!:fake4""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(531187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531187")]
        [Fact]
        public void DelegateDocComments()
        {
            var source = @"
/// <param name=""t""/>
/// <param name=""q""/>
/// <paramref name=""t""/>
/// <paramref name=""q""/>
/// <typeparam name=""T""/>
/// <typeparam name=""Q""/>
/// <typeparamref name=""T""/>
/// <typeparamref name=""Q""/>
delegate void D<T, U>(T t, U u);
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (3,18): warning CS1572: XML comment has a param tag for 'q', but there is no parameter by that name
                // /// <param name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "q").WithArguments("q"),
                // (5,21): warning CS1734: XML comment on 'D<T, U>' has a paramref tag for 'q', but there is no parameter by that name
                // /// <paramref name="q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "q").WithArguments("q", "D<T, U>"),
                // (7,22): warning CS1711: XML comment has a typeparam tag for 'Q', but there is no type parameter by that name
                // /// <typeparam name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, "Q").WithArguments("Q"),
                // (9,25): warning CS1735: XML comment on 'D<T, U>' has a typeparamref tag for 'Q', but there is no type parameter by that name
                // /// <typeparamref name="Q"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamRefTag, "Q").WithArguments("Q", "D<T, U>"),
                // (10,30): warning CS1573: Parameter 'u' has no matching param tag in the XML comment for 'D<T, U>' (but other parameters do)
                // delegate void D<T, U>(T t, U u);
                Diagnostic(ErrorCode.WRN_MissingParamTag, "u").WithArguments("u", "D<T, U>"),
                // (10,20): warning CS1712: Type parameter 'U' has no matching typeparam tag in the XML comment on 'D<T, U>' (but other type parameters do)
                // delegate void D<T, U>(T t, U u);
                Diagnostic(ErrorCode.WRN_MissingTypeParamTag, "U").WithArguments("U", "D<T, U>"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:D`2"">
            <param name=""t""/>
            <param name=""q""/>
            <paramref name=""t""/>
            <paramref name=""q""/>
            <typeparam name=""T""/>
            <typeparam name=""Q""/>
            <typeparamref name=""T""/>
            <typeparamref name=""Q""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NoWarn1591()
        {
            var source = @"
public class C { }
";
            var tree = Parse(source, options: TestOptions.RegularWithDocumentationComments);
            var warnDict = new Dictionary<string, ReportDiagnostic> { { MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingXMLComment), ReportDiagnostic.Suppress } };
            var comp = CreateCompilation(tree, options: TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(warnDict), assemblyName: "Test");
            comp.VerifyDiagnostics(); //NOTE: no WRN_MissingXMLComment

            var actual = GetDocumentationCommentText(comp,
                // (2,14): warning CS1591: Missing XML comment for publicly visible type or member 'C'
                // public class C { }
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C")); //Filtering happens later.
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(531233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531233")]
        [Fact]
        public void CrefAttributeInOtherElement()
        {
            var source = @"
class C
{
    /// <other cref=""C""/>
    void M()
    {
    }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics();

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M"">
            <other cref=""T:C""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(531233, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531233")]
        [Fact]
        public void NameAttributeInOtherElement()
        {
            var source = @"
class C
{
    /// <other name=""X""/>
    void M()
    {
    }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics();

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M"">
            <other name=""X""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetParseDiagnostics()
        {
            var source = @"
class Program
{
    /// <summary>
    /// 
    static void Main(string[] args)
    {
    }
}";
            var comp = CreateCompilationUtil(source);
            Assert.NotEmpty(comp.GetParseDiagnostics());
            Assert.Empty(comp.GetDeclarationDiagnostics());
            Assert.Empty(comp.GetMethodBodyDiagnostics());
            Assert.NotEmpty(comp.GetDiagnostics());
        }

        [WorkItem(531349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531349")]
        [Fact]
        public void GetDeclarationDiagnostics()
        {
            var source = @"
class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name=""a""></param>
    static void Main(string[] args)
    {
    }
}";
            var comp = CreateCompilationUtil(source);
            Assert.Empty(comp.GetParseDiagnostics());
            Assert.Empty(comp.GetDeclarationDiagnostics());
            Assert.Equal(2, comp.GetMethodBodyDiagnostics().Count());
            Assert.Equal(2, comp.GetDiagnostics().Count());
        }

        [WorkItem(531409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531409")]
        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var source = @"
interface I<T>
{
    void M();
}

class C<T> : I<T>
{
    /// <see cref=""object""/>
    void I<T>.M() { }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics();

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C`1.I{T}#M"">
            <see cref=""T:System.Object""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ArrayRankSpecifierOrder()
        {
            var source = @"
class C
{
    /// <see cref=""M""/>
    int[][,] M(int[,][] x) { return null; }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics();

            var actual = GetDocumentationCommentText(comp);
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C.M(System.Int32[][0:,0:])"">
            <see cref=""M:C.M(System.Int32[][0:,0:])""/>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        // As in dev11, the pragma has no effect.
        [ClrOnlyFact(ClrOnlyReason.DocumentationComment, Skip = "https://github.com/dotnet/roslyn/issues/8807")]
        public void PragmaDisableWarningInXmlFile()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("&");

            var sourceTemplate = @"
#pragma warning disable 1592
/// <include file='{0}' path='element'/>
class C {{ }}
";
            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFile.Path));

            var actual = GetDocumentationCommentText(comp,
                // 054c2dcb7959.xml(1,1): warning CS1592: Badly formed XML in included comments file -- 'Data at the root level is invalid.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("Data at the root level is invalid."));
            var expectedTemplate = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Badly formed XML file ""{0}"" cannot be included -->
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(string.Format(expectedTemplate, TestHelpers.AsXmlCommentText(xmlFile.Path)), actual);
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
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree }, assemblyName: "Test");

            var actualText = GetDocumentationCommentText(comp);
            var expectedText = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C`1"">
            <see cref=""M:C`1.M1(System.Object)""/>
            <see cref=""M:C`1.M1(C{System.Object})""/>
            <see cref=""M:C`1.M2(System.Object)""/>
            <see cref=""M:C`1.M2(C{System.Object})""/>
            
            <see cref=""M:C`1.M1(System.Object)""/>
            <see cref=""M:C`1.M1(C{System.Object})""/>
            <see cref=""M:C`1.M2(System.Object)""/>
            <see cref=""M:C`1.M2(C{System.Object})""/>
        </member>
    </members>
</doc>".Trim();
            Assert.Equal(expectedText, actualText);
        }

        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void GenericMethodWithoutTypeParameters1()
        {
            var source = @"
/// <see cref=""M""/>
/// <see cref=""M(int)""/>
/// <see cref=""M{T}""/>
/// <see cref=""M{T}(int)""/>
/// 
/// <see cref=""C.M""/>
/// <see cref=""C.M(int)""/>
/// <see cref=""C.M{T}""/>
/// <see cref=""C.M{T}(int)""/>
class C
{
    void M(int x) { }
    void M(string x) { }

    void M<T>(int x) { }
    void M<T>(string x) { }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'M'. Assuming 'C.M(int)', but could have also matched other overloads including 'C.M(string)'.
                // /// <see cref="M"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M").WithArguments("M", "C.M(int)", "C.M(string)"),
                // (4,16): warning CS0419: Ambiguous reference in cref attribute: 'M{T}'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="M{T}"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M{T}").WithArguments("M{T}", "C.M<T>(int)", "C.M<T>(string)"),
                // (7,16): warning CS0419: Ambiguous reference in cref attribute: 'C.M'. Assuming 'C.M(int)', but could have also matched other overloads including 'C.M(string)'.
                // /// <see cref="C.M"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C.M").WithArguments("C.M", "C.M(int)", "C.M(string)"),
                // (9,16): warning CS0419: Ambiguous reference in cref attribute: 'C.M{T}'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="C.M{T}"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C.M{T}").WithArguments("C.M{T}", "C.M<T>(int)", "C.M<T>(string)"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""M:C.M(System.Int32)""/>
            <see cref=""M:C.M(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            
            <see cref=""M:C.M(System.Int32)""/>
            <see cref=""M:C.M(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void GenericMethodWithoutTypeParameters2()
        {
            var source = @"
/// <see cref=""M""/>
/// <see cref=""M(int)""/>
/// <see cref=""M{T}""/>
/// <see cref=""M{T}(int)""/>
/// 
/// <see cref=""C.M""/>
/// <see cref=""C.M(int)""/>
/// <see cref=""C.M{T}""/>
/// <see cref=""C.M{T}(int)""/>
class C
{
    void M<T>(int x) { }
    void M<T>(string x) { }

    void M<T, U>(int x) { }
    void M<T, U>(string x) { }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'M'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="M"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M").WithArguments("M", "C.M<T>(int)", "C.M<T>(string)"),
                // (3,16): warning CS0419: Ambiguous reference in cref attribute: 'M(int)'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T, U>(int)'.
                // /// <see cref="M(int)"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M(int)").WithArguments("M(int)", "C.M<T>(int)", "C.M<T, U>(int)"),
                // (4,16): warning CS0419: Ambiguous reference in cref attribute: 'M{T}'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="M{T}"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M{T}").WithArguments("M{T}", "C.M<T>(int)", "C.M<T>(string)"),
                // (7,16): warning CS0419: Ambiguous reference in cref attribute: 'C.M'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="C.M"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C.M").WithArguments("C.M", "C.M<T>(int)", "C.M<T>(string)"),
                // (8,16): warning CS0419: Ambiguous reference in cref attribute: 'C.M(int)'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T, U>(int)'.
                // /// <see cref="C.M(int)"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C.M(int)").WithArguments("C.M(int)", "C.M<T>(int)", "C.M<T, U>(int)"),
                // (9,16): warning CS0419: Ambiguous reference in cref attribute: 'C.M{T}'. Assuming 'C.M<T>(int)', but could have also matched other overloads including 'C.M<T>(string)'.
                // /// <see cref="C.M{T}"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "C.M{T}").WithArguments("C.M{T}", "C.M<T>(int)", "C.M<T>(string)"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
            <see cref=""M:C.M``1(System.Int32)""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void GenericMethodWithoutTypeParameters3()
        {
            var source = @"
/// <see cref=""M""/>
/// <see cref=""M(int)""/>
/// 
/// <see cref=""N""/>
/// <see cref=""N(int)""/>
class C
{
    void M<T, U>(int x) { }
    void M<T>(int x) { }
    void M(int x) { }

    void N<T>(int x) { }
    void N(int x) { }
    void N(string x) { }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (5,16): warning CS0419: Ambiguous reference in cref attribute: 'N'. Assuming 'C.N(int)', but could have also matched other overloads including 'C.N(string)'.
                // /// <see cref="N"/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "N").WithArguments("N", "C.N(int)", "C.N(string)"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref=""M:C.M(System.Int32)""/>
            <see cref=""M:C.M(System.Int32)""/>
            
            <see cref=""M:C.N(System.Int32)""/>
            <see cref=""M:C.N(System.Int32)""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(547163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547163")]
        [Fact]
        public void NestedGenericTypes()
        {
            var source = @"
class A<TA1, TA2>
{
    class B<TB1, TB2>
    {
        class C<TC1, TC2>
        {
            /// Comment
            void M<TM1, TM2>(TA1 a1, TA2 a2, TB1 b1, TB2 b2, TC1 c1, TC2 c2, TM1 m1, TM2 m2)
            {
            }
        }
    }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:A`2.B`2.C`2.M``2(`0,`1,`2,`3,`4,`5,``0,``1)"">
            Comment
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WhitespaceAroundCref()
        {
            var source = @"
/// <see cref=""   A   ""/>
class A { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A"">
            <see cref=""T:A""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TypeParamRef_ContainingType()
        {
            var source = @"
class A<T>
{
    class B
    {
        class C<U>
        {
            class D
            {
                /// <typeparamref name=""T"" />
                /// <typeparamref name=""U"" />
                /// <typeparamref name=""V"" />
                class E<V> { }

                /// <typeparamref name=""T"" />
                /// <typeparamref name=""U"" />
                /// <typeparamref name=""V"" />
                void M<V>() { }

                /// <typeparamref name=""T"" />
                /// <typeparamref name=""U"" />
                int P { get; set; }
            }
        }
    }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A`1.B.C`1.D.E`1"">
            <typeparamref name=""T"" />
            <typeparamref name=""U"" />
            <typeparamref name=""V"" />
        </member>
        <member name=""M:A`1.B.C`1.D.M``1"">
            <typeparamref name=""T"" />
            <typeparamref name=""U"" />
            <typeparamref name=""V"" />
        </member>
        <member name=""P:A`1.B.C`1.D.P"">
            <typeparamref name=""T"" />
            <typeparamref name=""U"" />
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(527260, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527260")]
        [Fact]
        public void IllegalXmlCharacter()
        {
            var source = @"
/// <" + "\u037F" + @"/>
class A { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:A"" -->
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(547311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547311")]
        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void UndeclaredXmlNamespace()
        {
            var source = @"
/// <summary>
/// Implement of the bindable radio button
///             
/// Usage:
/// Bind your source property to the Control property Value, and set the CheckValue to your expected value.
///             
/// Sample:
///             
/// public enum Shapes
/// {
///     Square, Circle, Rectangle, Ellipse
/// }
///             
/// class MyControl
/// {
///     public Shapes Shape { get; set; }
/// }
///             
/// <WpfUtils:BindableRadioButton Value=""{Binding Shape}"" CheckedValue=""Square"">Square</WpfUtils:BindableRadioButton>
/// <WpfUtils:BindableRadioButton Value=""{Binding Shape}"" CheckedValue=""Circle"">Circle</WpfUtils:BindableRadioButton>
/// <WpfUtils:BindableRadioButton Value=""{Binding Shape}"" CheckedValue=""Ellipse"">Ellipse</WpfUtils:BindableRadioButton>
///             
/// </summary>
class A { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (2,4): warning CS1570: XML comment has badly formed XML -- ''WpfUtils' is an undeclared prefix.'
                // /// <summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("'WpfUtils' is an undeclared prefix."));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:A"" -->
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(551323, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551323")]
        [Fact]
        public void MultiLine_OneLinePlusEnding()
        {
            var source = @"
/** Stuff
*/
class C { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            Stuff
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(577385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577385")]
        [Fact]
        public void FormatBeforeFinalParse()
        {
            var source = @"
/// <summary>
/// Get the syntax node(s) where this symbol was declared in source. Some symbols (for
/// example, partial classes) may be defined in more than one location. This property should
/// return one or more syntax nodes only if the symbol was declared in source code and also
/// was not implicitly declared (see the IsImplicitlyDeclared property). 
/// 
/// Note that for namespace symbol, the declaring syntax might be declaring a nested
/// namespace. For example, the declaring syntax node for N1 in ""namespace N1.N2 {...}"" is
/// the entire NamespaceDeclarationSyntax for N1.N2. For the global namespace, the declaring
/// syntax will be the CompilationUnitSyntax.
/// </summary>
/// <returns>
/// The syntax node(s) that declared the symbol. If the symbol was declared in metadata or
/// was implicitly declared, returns an empty read-only array.
/// </returns>
/// <remarks>
/// To go the opposite direction (from syntax node to symbol), see <see
/// cref=""SemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)""/>.
/// </remarks>
class C { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (19,11): warning CS1574: XML comment has cref attribute 'SemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)' that could not be resolved
                // /// cref="SemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "SemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)").WithArguments("GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            Get the syntax node(s) where this symbol was declared in source. Some symbols (for
            example, partial classes) may be defined in more than one location. This property should
            return one or more syntax nodes only if the symbol was declared in source code and also
            was not implicitly declared (see the IsImplicitlyDeclared property). 
            
            Note that for namespace symbol, the declaring syntax might be declaring a nested
            namespace. For example, the declaring syntax node for N1 in ""namespace N1.N2 {...}"" is
            the entire NamespaceDeclarationSyntax for N1.N2. For the global namespace, the declaring
            syntax will be the CompilationUnitSyntax.
            </summary>
            <returns>
            The syntax node(s) that declared the symbol. If the symbol was declared in metadata or
            was implicitly declared, returns an empty read-only array.
            </returns>
            <remarks>
            To go the opposite direction (from syntax node to symbol), see <see
            cref=""!:SemanticModel.GetDeclaredSymbol(MemberDeclarationSyntax, CancellationToken)""/>.
            </remarks>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(587126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587126")]
        [Fact]
        public void DeclaringGenericTypeInReturnType()
        {
            var source = @"
/// <see cref='System.Nullable{T}.op_Implicit'/>
class C { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref='M:System.Nullable`1.op_Implicit(`0)~System.Nullable{`0}'/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(587126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587126")]
        [Fact]
        public void DeclaringGenericTypeInParameterType1()
        {
            var source = @"
/// <see cref=""C{T}.M""/>
/// <see cref=""M""/>
class C<T>
{
    void M(T t, C<T> c, C<C<T>> cc) { }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C`1"">
            <see cref=""M:C`1.M(`0,C{`0},C{C{`0}})""/>
            <see cref=""M:C`1.M(`0,C{`0},C{C{`0}})""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(587126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/587126")]
        [Fact]
        public void DeclaringGenericTypeInParameterType2()
        {
            var source = @"
class B<U>
{
    /// <see cref=""M1""/>
    /// <see cref=""M2""/>
    /// <see cref=""M3""/>
    /// <see cref=""M4""/>
    class C<T>
    {
        void M1(T t, C<T> c, C<C<T>> cc) { }
        void M2(U u, C<U> c, C<C<U>> cc) { }
        void M3(T t, B<T> b, B<B<T>> bb) { }
        void M4(U u, B<U> b, B<B<U>> bb) { }
    }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:B`1.C`1"">
            <see cref=""M:B`1.C`1.M1(`1,B{`0}.C{`1},B{`0}.C{B{`0}.C{`1}})""/>
            <see cref=""M:B`1.C`1.M2(`0,B{`0}.C{`0},B{`0}.C{B{`0}.C{`0}})""/>
            <see cref=""M:B`1.C`1.M3(`1,B{`1},B{B{`1}})""/>
            <see cref=""M:B`1.C`1.M4(`0,B{`0},B{B{`0}})""/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(552379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552379")]
        [Fact]
        public void MultipleDocComments()
        {
            var source = @"
/** Multiline 1. */
/** Multiline 2. */
public class A { }

/** Multiline 1. */
/// Single line 1.
/** Multiline 2. */
/// Single line 2.
public class B { }

/** Multiline 1. */
/// Single line 1.
public partial class C { }

/** Multiline 2. */
/// Single line 2.
public partial class C { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A"">
            Multiline 1. 
            Multiline 2. 
        </member>
        <member name=""T:B"">
            Multiline 1. 
            Single line 1.
            Multiline 2. 
            Single line 2.
        </member>
        <member name=""T:C"">
            Multiline 1. 
            Single line 1.
            Multiline 2. 
            Single line 2.
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(552379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552379")]
        [Fact]
        public void MultipleDocComments_Separated()
        {
            var source = @"
/** Multiline 1. */
// Normal single-line comment.
/** Multiline 2. */
public class A { }

/** Multiline 1. */
/* Normal multiline comment. */
/** Multiline 2. */
public class B { }

/** Multiline 1. */
public partial class C { }

// Normal single-line comment.
/** Multiline 2. */
public partial class C { }

/** Multiline 1. */
#region
/** Multiline 2. */
public class D { }

/** Multiline 1. */
#endregion
/** Multiline 2. */
public class E { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (2,1): warning CS1587: XML comment is not placed on a valid language element
                // /** Multiline 1. */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (7,1): warning CS1587: XML comment is not placed on a valid language element
                // /** Multiline 1. */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (19,1): warning CS1587: XML comment is not placed on a valid language element
                // /** Multiline 1. */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"),
                // (24,1): warning CS1587: XML comment is not placed on a valid language element
                // /** Multiline 1. */
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A"">
            Multiline 2. 
        </member>
        <member name=""T:B"">
            Multiline 2. 
        </member>
        <member name=""T:C"">
            Multiline 1. 
            Multiline 2. 
        </member>
        <member name=""T:D"">
            Multiline 2. 
        </member>
        <member name=""T:E"">
            Multiline 2. 
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(552379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552379")]
        [Fact]
        public void MultipleDocComments_SplitXml()
        {
            var source = @"
/** <tag> */
/** </tag> */
public class A { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            // NOTE: Dev11 allows this but Roslyn does not.  There's no way for
            // us to build sensible structured trivia for the XML in this scenario.
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:A"" -->
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(689497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/689497")]
        [Fact]
        public void TriviaBetweenDocCommentAndDeclaration()
        {
            var source = @"
/// <summary/>
// Single-line comment.
/* Multi-line comment. */
#if true
#endif
#if false
#endif
#region
#endregion
public class A { }
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:A"">
            <summary/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(703368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703368")]
        [Fact]
        public void NonGenericBeatsGeneric()
        {
            var source = @"
/// <see cref='M(string)'/>
public class C
{
    void M(string s) { }
    void M<T>(string s) { }
}
";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp);

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref='M:C.M(System.String)'/>
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(703587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703587")]
        [Fact]
        public void ObjectMemberViaInterface()
        {
            var source = @"
using System;

/// Comment
public class C : IEquatable<C>
{
    /// Implements <see cref=""IEquatable{T}.Equals""/>.
    /// Implements <see cref=""IEquatable{T}.GetHashCode""/>.
    bool IEquatable<C>.Equals(C c) { throw null; }
}

";
            var comp = CreateCompilationUtil(source);

            var actual = GetDocumentationCommentText(comp,
                // (7,31): warning CS1574: XML comment has cref attribute 'IEquatable{T}.GetHashCode' that could not be resolved
                //     /// Implements <see cref="IEquatable{T}.GetHashCode"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "IEquatable{T}.GetHashCode").WithArguments("GetHashCode"));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            Comment
        </member>
        <member name=""M:C.System#IEquatable{C}#Equals(C)"">
            Implements <see cref=""M:System.IEquatable`1.Equals(`0)""/>.
            Implements <see cref=""!:IEquatable&lt;T&gt;.GetHashCode""/>.
        </member>
    </members>
</doc>".Trim();

            Assert.Equal(expected, actual);
        }

        [WorkItem(531505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531505")]
        [ClrOnlyFact]
        public void Pia()
        {
            var source = @"
/// <see cref='FooStruct'/>
/// <see cref='FooStruct.NET'/>
public class C { }
";

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref='T:FooStruct'/>
            <see cref='F:FooStruct.NET'/>
        </member>
    </members>
</doc>".Trim();

            Action<ModuleSymbol> validator = module =>
            {
                ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                // No reference added.
                AssertEx.None(module.GetReferencedAssemblies(), id => id.Name.Contains("GeneralPia"));

                // No type embedded.
                Assert.Equal(0, module.GlobalNamespace.GetMembers("FooStruct").Length);
            };

            // Don't embed.
            {
                var reference = TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(false);
                var comp = CreateCompilationUtil(source, new[] { reference });
                var actual = GetDocumentationCommentText(comp);
                Assert.Equal(expected, actual);

                CompileAndVerify(comp, symbolValidator: validator);
            }

            // Do embed.
            {
                var reference = TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(true);
                var comp = CreateCompilationUtil(source, new[] { reference });
                var actual = GetDocumentationCommentText(comp);
                Assert.Equal(expected, actual);

                CompileAndVerify(comp, symbolValidator: validator);
            }
        }

        [WorkItem(757110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/757110")]
        [Fact]
        public void NoAssemblyElementForNetModule()
        {
            var source = @"
/// <summary>Text</summary>
public class C { }
";
            var comp = CreateCompilationUtil(source, options: TestOptions.ReleaseModule);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <members>
        <member name=""T:C"">
            <summary>Text</summary>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(743425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743425")]
        [Fact]
        public void WRN_UnqualifiedNestedTypeInCref()
        {
            var source = @"
class C<T>
{
    class Inner { }

    void M(Inner i) { }

    /// <see cref=""M""/>
    /// <see cref=""C{T}.M""/>
    /// <see cref=""C{Q}.M""/>
    /// <see cref=""C{Q}.M(C{Q}.Inner)""/>
    /// <see cref=""C{Q}.M(Inner)""/>
    void N() { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (12,27): warning CS8018: Within cref attributes, nested types of generic types should be qualified.
                //     /// <see cref="C{Q}.M(Inner)"/>
                Diagnostic(ErrorCode.WRN_UnqualifiedNestedTypeInCref, "Inner"),
                // (12,20): warning CS1574: XML comment has cref attribute 'C{Q}.M(Inner)' that could not be resolved
                //     /// <see cref="C{Q}.M(Inner)"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{Q}.M(Inner)").WithArguments("M(Inner)"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C`1.N"">
            <see cref=""M:C`1.M(C{`0}.Inner)""/>
            <see cref=""M:C`1.M(C{`0}.Inner)""/>
            <see cref=""M:C`1.M(C{`0}.Inner)""/>
            <see cref=""M:C`1.M(C{`0}.Inner)""/>
            <see cref=""!:C&lt;Q&gt;.M(Inner)""/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(743425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743425")]
        [Fact]
        public void WRN_UnqualifiedNestedTypeInCref_Buried()
        {
            var source = @"
class C<T>
{
    class Inner { }

    void M(C<Inner[]> i) { }

    /// <see cref=""C{Q}.M(C{Inner[]})""/>
    /// <see cref=""C{Q}.M(C{C{Q}.Inner[]})""/>
    void N() { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (8,27): warning CS8018: Within cref attributes, nested types of generic types should be qualified.
                //     /// <see cref="C{Q}.M(C{Inner[]})"/>
                Diagnostic(ErrorCode.WRN_UnqualifiedNestedTypeInCref, "C{Inner[]}"),
                // (8,20): warning CS1574: XML comment has cref attribute 'C{Q}.M(C{Inner[]})' that could not be resolved
                //     /// <see cref="C{Q}.M(C{Inner[]})"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{Q}.M(C{Inner[]})").WithArguments("M(C{Inner[]})"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C`1.N"">
            <see cref=""!:C&lt;Q&gt;.M(C&lt;Inner[]&gt;)""/>
            <see cref=""M:C`1.M(C{C{`0}.Inner[]})""/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [WorkItem(743425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743425")]
        [Fact]
        public void WRN_UnqualifiedNestedTypeInCref_Generic()
        {
            var source = @"
class C<T>
{
    class Inner<U> { }

    void M(Inner<int> i) { }

    /// <see cref=""C{Q}.M(C{Q}.Inner{int})""/>
    /// <see cref=""C{Q}.M(Inner{int})""/>
    void N() { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (9,27): warning CS8018: Within cref attributes, nested types of generic types should be qualified.
                //     /// <see cref="C{Q}.M(Inner{int})"/>
                Diagnostic(ErrorCode.WRN_UnqualifiedNestedTypeInCref, "Inner{int}"),
                // (9,20): warning CS1574: XML comment has cref attribute 'C{Q}.M(Inner{int})' that could not be resolved
                //     /// <see cref="C{Q}.M(Inner{int})"/>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "C{Q}.M(Inner{int})").WithArguments("M(Inner{int})"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:C`1.N"">
            <see cref=""M:C`1.M(C{`0}.Inner{System.Int32})""/>
            <see cref=""!:C&lt;Q&gt;.M(Inner&lt;int&gt;)""/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Misc

        #region Dev11 bugs

        [Fact]
        public void Dev11_422418()
        {
            // Warn-as-error
            var source = @"
public class C {} // CS1587
";

            var tree = Parse(source, options: TestOptions.RegularWithDocumentationComments);
            var compOptions = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(tree, options: compOptions).VerifyDiagnostics(
                // (2,14): error CS1591: Warning as Error: Missing XML comment for publicly visible type or member 'C'
                // public class C {} // CS1587
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C").WithWarningAsError(true));
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void Dev11_303769()
        {
            // XML processing instructions
            var source = @"
/// <summary>
/// <?xml:a ?>
/// </summary>
class C { }
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (2,4): warning CS1570: XML comment has badly formed XML -- 'The ':' character, hexadecimal value 0x3A, cannot be included in a name.'
                // /// <summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("The ':' character, hexadecimal value 0x3A, cannot be included in a name."));

            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:C"" -->
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dev11_275507()
        {
            // Array rank specifier order
            var source = @"
class Program
{
    /**
    * <param name=""x1""></param>
    * <param name=""x2""></param>
    * <returns></returns>
    */
    public void M2(int[] x1, long[][, ,] x2) { }
    public static void main() { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:Program.M2(System.Int32[],System.Int64[0:,0:,0:][])"">
            <param name=""x1""></param>
            <param name=""x2""></param>
            <returns></returns>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dev11_274116()
        {
            // Included element order.
            var xml = @"
<?xml version=""1.0"" encoding=""utf-8"" ?> 
<Docs> 

<Class1> 

<Remarks name=""Part1""> 
<para>EXAMPLE 1</para> 
</Remarks> 

<Remarks name=""Part2""> 
<para>EXAMPLE 2</para> 
</Remarks> 

</Class1> 

</Docs> 
".Trim();
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(xml);
            var xmlFilePath = xmlFile.Path;

            var sourceTemplate = @"
/// <summary> 
/// ... 
/// </summary> 
/// <remarks> 
/// <para>One</para> 
/// <include file=""{0}"" path=""Docs/Class1/Remarks[@name='Part1']/*"" /> 
/// <para>Two</para> 
/// <include file=""{0}"" path=""Docs/Class1/Remarks[@name='Part2']/*"" /> 
/// <para>Three</para> 
/// </remarks> 
public class C {{ }}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, xmlFilePath));
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary> 
            ... 
            </summary> 
            <remarks> 
            <para>One</para> 
            <para>EXAMPLE 1</para> 
            <para>Two</para> 
            <para>EXAMPLE 2</para> 
            <para>Three</para> 
            </remarks> 
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dev11_209994()
        {
            // Array rank specifier order
            // NOTE: the remark on the method is copied directly from the bug - 
            // it does not correctly indicate the doc comment ID of the method.
            var source = @"
namespace Demo
{
    public class Example
    {
        /// <remarks>M:Demo.Example.M(double[0:,0:,0:][])</remarks>
        public static void M(double[][, , ,] value)
        {
        }
    }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (4,18): warning CS1591: Missing XML comment for publicly visible type or member 'Demo.Example'
                //     public class Example
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "Example").WithArguments("Demo.Example"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""M:Demo.Example.M(System.Double[0:,0:,0:,0:][])"">
            <remarks>M:Demo.Example.M(double[0:,0:,0:][])</remarks>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void Dev11_142553()
        {
            // Need to cache XML files.

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<hello/>");

            string fullPath = xmlFile.Path;
            string fileName = Path.GetFileName(fullPath);
            string dirPath = Path.GetDirectoryName(fullPath);

            var source = @"
/// <include file='" + fullPath + @"' path='hello'/>
/// <include file='" + fullPath + @"' path='hello'/>
/// <include file='" + Path.Combine(dirPath, "a/..", fileName) + @"' path='hello'/>
/// <include file='" + Path.Combine(dirPath, @"a\b/../..", fileName) + @"' path='hello'/>
class C { }
";

            CreateCompilationUtil(source).VerifyDiagnostics();
            Assert.InRange(DocumentationCommentIncludeCache.CacheMissCount, 1, 2); //Not none, not all.
        }

        [Fact]
        public void UriNotAllowed()
        {
            // Need to cache XML files.

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<hello/>");

            var source = @"
/// <include file='file://" + xmlFile.Path + @"' path='hello'/>
class C { }
";

            CreateCompilationUtil(source).VerifyDiagnostics(
                // (2,5): warning CS1589: Unable to include XML fragment 'hello' of file '' -- Unable to find the specified file.
                Diagnostic(ErrorCode.WRN_FailedInclude,
                @"<include file='file://" + xmlFile.Path + @"' path='hello'/>").
                WithArguments("file://" + xmlFile.Path, "hello", "File not found.").WithLocation(2, 5));
        }

        [Fact]
        public void FileDirective()
        {
            // Line directive not considered.

            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText("<hello/>");

            string xmlFilePath = Path.GetFileName(xmlFile.Path);
            string dirPath = Path.GetDirectoryName(xmlFile.Path);
            string sourcePath = Path.Combine(dirPath, "test.cs");

            var source = @"
#line 200 ""C:\path\that\doesnt\exist.cs""
/// <include file='" + xmlFilePath + @"' path='hello'/>
class C { }
";

            var comp = CreateCompilation(
                Parse(source, options: TestOptions.RegularWithDocumentationComments, filename: sourcePath),
                options: TestOptions.ReleaseDll.WithSourceReferenceResolver(SourceFileResolver.Default).WithXmlReferenceResolver(XmlFileResolver.Default),
                assemblyName: "Test");

            var actual = GetDocumentationCommentText(comp);

            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <hello />
        </member>
    </members>
</doc>";
            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void DtdDenialOfService()
        {
            var xmlFile = Temp.CreateFile(extension: ".xml").WriteAllText(
@"<?xml version=""1.0""?>
<!DOCTYPE root [
  <!ENTITY expand ""expand"">
  <!ENTITY expand2 ""&expand;&expand;&expand;&expand;&expand;&expand;&expand;&expand;&expand;&expand;"">
  <!ENTITY expand3 ""&expand2;&expand2;&expand2;&expand2;&expand2;&expand2;&expand2;&expand2;&expand2;&expand2;"">
  <!ENTITY expand4 ""&expand3;&expand3;&expand3;&expand3;&expand3;&expand3;&expand3;&expand3;&expand3;&expand3;"">
  <!ENTITY expand5 ""&expand4;&expand4;&expand4;&expand4;&expand4;&expand4;&expand4;&expand4;&expand4;&expand4;"">
  <!ENTITY expand6 ""&expand5;&expand5;&expand5;&expand5;&expand5;&expand5;&expand5;&expand5;&expand5;&expand5;"">
  <!ENTITY expand7 ""&expand6;&expand6;&expand6;&expand6;&expand6;&expand6;&expand6;&expand6;&expand6;&expand6;"">
  <!ENTITY expand8 ""&expand7;&expand7;&expand7;&expand7;&expand7;&expand7;&expand7;&expand7;&expand7;&expand7;"">
  <!ENTITY expand9 ""&expand8;&expand8;&expand8;&expand8;&expand8;&expand8;&expand8;&expand8;&expand8;&expand8;"">
]>
<root>&expand9;</root>
");
            var source = @"
/// <include file='" + xmlFile.Path + @"' path='hello'/>
class C { }
";

            CreateCompilationUtil(source).GetDiagnostics().VerifyWithFallbackToErrorCodeOnlyForNonEnglish(
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("For security reasons DTD is prohibited in this XML document. To enable DTD processing set the DtdProcessing property on XmlReaderSettings to Parse and pass the settings into XmlReader.Create method.").WithLocation(1, 1));
        }

        #endregion Dev11 bugs

        #region Dev10 bugs

        [Fact]
        public void Dev10_898556()
        {
            // Somehow, this was causing an infinite loop (even though there's no cycle?).
            // Delete some irrelevant sections to save space.
            var xmlTemplate = @"
<?xml version=""1.0"" encoding=""utf-8"" ?>
<docs>
<doc name=""ArrayExtensions.BinarySearchCore"">
<overloads>Searches a sorted array for a value using a binary search algorithm.</overloads>
<typeparam name=""T"">The type of items in the array.</typeparam>
<typeparam name=""TComparator"">The type of comparator used to compare items during the search operation.</typeparam>
<param name=""array"">The sorted array to search.</param>
<param name=""value"">The object to search for.</param>
<returns>If found, the index of the specified value in the given array. Otherwise, if not found, and the value is less than one or more items in the array, a negative number which is the bitwise complement of the index of the first item that is larger than the given value. If the value is not found and it is greater than any of the items in the array, a negative number which is the bitwise complement of (the index of the last item plus 1).</returns>
</doc>
<doc name=""ArrayExtensions.BinarySearch(ArrayType,T)"">
<include file=""{0}"" path=""docs/doc[@name='ArrayExtensions.BinarySearchCore']/*"" />
</doc>
<doc name=""ArrayExtensions.BinarySearch(ArrayType,T,TComparator)"">
<include file=""{0}"" path=""docs/doc[@name='ArrayExtensions.BinarySearchCore']/*"" />
<param name=""comp"">The comparator used to evaluate the order of items.</param>
</doc>
<doc name=""ArrayExtensions.BinarySearch(ArrayType,int,int,T)"">
<include file=""{0}"" path=""docs/doc[@name='ArrayExtensions.BinarySearchCore']/*"" />
<param name=""index"">The index of the first item in the range.</param>
<param name=""count"">The total number of items in the range.</param>
</doc>
<doc name=""ArrayExtensions.BinarySearch(ArrayType,int,int,T,TComparator)"">
<include file=""{0}"" path=""docs/doc[@name='ArrayExtensions.BinarySearch(ArrayType,int,int,T)']/*"" />
<param name=""comp"">The comparator used to evaluate the order of items.</param>
</doc>
</docs>
".Trim();

            var xmlFile = Temp.CreateFile(extension: ".xml");
            var xmlFilePath = xmlFile.Path;
            xmlFile.WriteAllText(string.Format(xmlTemplate, xmlFilePath));

            var includeElementTemplate = @"<include file=""{0}"" path=""docs/doc[@name=&quot;ArrayExtensions.BinarySearch(ArrayType,T)&quot;]/*""/>";
            var includeElement = string.Format(includeElementTemplate, xmlFilePath);

            var sourceTemplate = @"
/// {0}
class C
{{
    static void Main() {{ }}
}}
";

            var comp = CreateCompilationUtil(string.Format(sourceTemplate, includeElement));
            var actual = GetDocumentationCommentText(comp,
                // (2,5): warning CS1711: XML comment has a typeparam tag for 'T', but there is no type parameter by that name
                // /// <include file="52f50b557f3d.xml" path="docs/doc[@name=&quot;ArrayExtensions.BinarySearch(ArrayType,T)&quot;]/*"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("T"),
                // (2,5): warning CS1711: XML comment has a typeparam tag for 'TComparator', but there is no type parameter by that name
                // /// <include file="52f50b557f3d.xml" path="docs/doc[@name=&quot;ArrayExtensions.BinarySearch(ArrayType,T)&quot;]/*"/>
                Diagnostic(ErrorCode.WRN_UnmatchedTypeParamTag, includeElement).WithArguments("TComparator"),
                // (2,5): warning CS1572: XML comment has a param tag for 'array', but there is no parameter by that name
                // /// <include file="52f50b557f3d.xml" path="docs/doc[@name=&quot;ArrayExtensions.BinarySearch(ArrayType,T)&quot;]/*"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, includeElement).WithArguments("array"),
                // (2,5): warning CS1572: XML comment has a param tag for 'value', but there is no parameter by that name
                // /// <include file="52f50b557f3d.xml" path="docs/doc[@name=&quot;ArrayExtensions.BinarySearch(ArrayType,T)&quot;]/*"/>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, includeElement).WithArguments("value"));
            var expected = (@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <overloads>Searches a sorted array for a value using a binary search algorithm.</overloads><typeparam name=""T"">" +
            @"The type of items in the array.</typeparam><typeparam name=""TComparator"">The type of comparator used to compare " +
            @"items during the search operation.</typeparam><param name=""array"">The sorted array to search.</param><param name=""value"">" +
            @"The object to search for.</param><returns>If found, the index of the specified value in the given array. Otherwise, if not " +
            @"found, and the value is less than one or more items in the array, a negative number which is the bitwise complement of the " +
            @"index of the first item that is larger than the given value. If the value is not found and it is greater than any of the items " +
            @"in the array, a negative number which is the bitwise complement of (the index of the last item plus 1).</returns>
        </member>
    </members>
</doc>
").Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dev10_785160()
        {
            // Someone suggested preferring the more public member in case of ambiguity, but it was not implemented.
            var source = @"
/// <see cref='M'/>
class C
{
    private void M(char c) { }
    public void M(int x) { }
}
";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // (2,16): warning CS0419: Ambiguous reference in cref attribute: 'M'. Assuming 'C.M(char)', but could have also matched other overloads including 'C.M(int)'.
                // /// <see cref='M'/>
                Diagnostic(ErrorCode.WRN_AmbiguousXMLReference, "M").WithArguments("M", "C.M(char)", "C.M(int)"));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <see cref='M:C.M(System.Char)'/>
        </member>
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Dev10_747421()
        {
            // Bad XML.
            var source = @"
class Module1
{
    ///<summary>
    ///
    ///</summary>
    ///<remarks><</remarks>
    public static void Main() { }
}
";
            var comp = CreateCompilationUtil(source);
            comp.VerifyDiagnostics(
                // (7,18): warning CS1570: XML comment has badly formed XML -- 'An identifier was expected.'
                //     ///<remarks><</remarks>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""));
            var actual = GetDocumentationCommentText(comp);
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""M:Module1.Main"" -->
    </members>
</doc>
".Trim();
            Assert.Equal(expected, actual);
        }

        #endregion Dev10 bugs

        [ClrOnlyFact]
        [WorkItem(1115058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115058")]
        public void UnterminatedElement()
        {
            var source = @"
class Module1
{
    ///<summary>
    /// Something
    ///<summary>
    static void Main()
    {
        System.Console.WriteLine(""Here"");
    }
}";
            var comp = CreateCompilationUtil(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "Here").VerifyDiagnostics(
    // (7,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
    //     static void Main()
    Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary").WithLocation(7, 1),
    // (7,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
    //     static void Main()
    Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary").WithLocation(7, 1)
                );
        }

        /// <summary>
        /// "--" is not valid within an XML comment.
        /// </summary>
        [WorkItem(8807, "https://github.com/dotnet/roslyn/issues/8807")]
        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(18610, "https://github.com/dotnet/roslyn/issues/18610")]
        public void IncludeErrorDashDashInName()
        {
            var dir = Temp.CreateDirectory();
            var path = dir.Path;
            var xmlFile = dir.CreateFile("---.xml").WriteAllText(@"<summary attrib="""" attrib=""""/>");
            var source =
$@"/// <include file='{Path.Combine(path, "---.xml")}' path='//summary'/>
class C {{ }}";
            var comp = CreateCompilationUtil(source);
            var actual = GetDocumentationCommentText(comp,
                // warning CS1592: Badly formed XML in included comments file -- ''attrib' is a duplicate attribute name.'
                Diagnostic(ErrorCode.WRN_XMLParseIncludeError).WithArguments("'attrib' is a duplicate attribute name.").WithLocation(1, 1));
            var expected =
$@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <!-- Badly formed XML file ""{Path.Combine(TestHelpers.AsXmlCommentText(path), "- - -.xml")}"" cannot be included -->
        </member>
    </members>
</doc>";
            Assert.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_01()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Value"">Parameter of the record.</param>
record Rec(string Value);
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                //     public static class IsExternalInit
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Value"">Parameter of the record.</param>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
            <summary>The record.</summary>
            <param name=""Value"">Parameter of the record.</param>
        </member>
        <member name=""P:Rec.Value"">
            <summary>Parameter of the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_02()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Value"">Parameter of the record.
record Rec(string Value);
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                //     public static class IsExternalInit
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:Rec"" -->
        <!-- Badly formed XML comment ignored for member ""M:Rec.#ctor(System.String)"" -->
        <!-- Badly formed XML comment ignored for member ""P:Rec.Value"" -->
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_03()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Value"">Parameter of the record.</param>
/// <param name=""Value"">Also the value of the record.</param>
record Rec(string Value);
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                // (4,12): warning CS1571: XML comment has a duplicate param tag for 'Value'
                // /// <param name="Value">Also the value of the record.</param>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""Value""").WithArguments("Value").WithLocation(4, 12),
                // (4,12): warning CS1571: XML comment has a duplicate param tag for 'Value'
                // /// <param name="Value">Also the value of the record.</param>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""Value""").WithArguments("Value").WithLocation(4, 12),
                // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                //     public static class IsExternalInit
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));

            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Value"">Parameter of the record.</param>
            <param name=""Value"">Also the value of the record.</param>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
            <summary>The record.</summary>
            <param name=""Value"">Parameter of the record.</param>
            <param name=""Value"">Also the value of the record.</param>
        </member>
        <member name=""P:Rec.Value"">
            <summary>Parameter of the record.</summary>
            <summary>Also the value of the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_04()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Item1"">First item in the record.</param>
/// <param name=""Item2"">Second item in the record.</param>
record Rec(string Item1, object Item2);
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                //     public static class IsExternalInit
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25)
);
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Item1"">First item in the record.</param>
            <param name=""Item2"">Second item in the record.</param>
        </member>
        <member name=""M:Rec.#ctor(System.String,System.Object)"">
            <summary>The record.</summary>
            <param name=""Item1"">First item in the record.</param>
            <param name=""Item2"">Second item in the record.</param>
        </member>
        <member name=""P:Rec.Item1"">
            <summary>First item in the record.</summary>
        </member>
        <member name=""P:Rec.Item2"">
            <summary>Second item in the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_05()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Item2"">Second item in the record.</param>
record Rec(string Item1, object Item2);
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,19): warning CS1573: Parameter 'Item1' has no matching param tag in the XML comment for 'Rec.Rec(string, object)' (but other parameters do)
                    // record Rec(string Item1, object Item2);
                    Diagnostic(ErrorCode.WRN_MissingParamTag, "Item1").WithArguments("Item1", "Rec.Rec(string, object)").WithLocation(4, 19),
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Item2"">Second item in the record.</param>
        </member>
        <member name=""M:Rec.#ctor(System.String,System.Object)"">
            <summary>The record.</summary>
            <param name=""Item2"">Second item in the record.</param>
        </member>
        <member name=""P:Rec.Item2"">
            <summary>Second item in the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_06()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Item"">Item within the record.</param>
record Rec(string Item)
{
    public string Item { get; init; } = Item;
}
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_07()
        {
            var source = @"
/// <summary>The record.</summary>
/// <param name=""Item"">Item within the record.</param1>
record Rec(string Item)
{
}
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <!-- Badly formed XML comment ignored for member ""T:Rec"" -->
        <!-- Badly formed XML comment ignored for member ""M:Rec.#ctor(System.String)"" -->
        <!-- Badly formed XML comment ignored for member ""P:Rec.Item"" -->
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_08()
        {
            var source = @"
/**
 * <summary>The record.</summary>
 * <param name=""Item"">Item within the record.</param>
 * <remarks>The remarks.</remarks>
 */
record Rec(string Item)
{
}
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
            <remarks>The remarks.</remarks>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
            <remarks>The remarks.</remarks>
        </member>
        <member name=""P:Rec.Item"">
            <summary>Item within the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_09()
        {
            var source = @"
/**
 *<summary>The record.</summary>
 *<param name=""Item"">Item within the record.</param>
 *<remarks>The remarks.</remarks>
 */
record Rec(string Item)
{
}
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
            <remarks>The remarks.</remarks>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
            <summary>The record.</summary>
            <param name=""Item"">Item within the record.</param>
            <remarks>The remarks.</remarks>
        </member>
        <member name=""P:Rec.Item"">
            <summary>Item within the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }

        [Fact]
        [WorkItem(52663, "https://github.com/dotnet/roslyn/issues/52663")]
        public void PositionalRecord_10()
        {
            var source = @"
/**
   <summary>The record.</summary>
   <param name=""Item"">Item within the record.</param>
   <remarks>The remarks.</remarks>
 */
record Rec(string Item)
{
}
";
            var comp = CreateCompilationUtil(new[] { source, IsExternalInitTypeDefinition });
            var actual = GetDocumentationCommentText(comp,
                    // (4,25): warning CS1591: Missing XML comment for publicly visible type or member 'IsExternalInit'
                    //     public static class IsExternalInit
                    Diagnostic(ErrorCode.WRN_MissingXMLComment, "IsExternalInit").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 25));

            // Ideally, the 'P:Rec.Item' summary would have exactly the same leading indentation as the param comment it was derived from.
            // However, it doesn't seem essential for this to match in all cases.
            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:Rec"">
               <summary>The record.</summary>
               <param name=""Item"">Item within the record.</param>
               <remarks>The remarks.</remarks>
        </member>
        <member name=""M:Rec.#ctor(System.String)"">
               <summary>The record.</summary>
               <param name=""Item"">Item within the record.</param>
               <remarks>The remarks.</remarks>
        </member>
        <member name=""P:Rec.Item"">
            <summary>Item within the record.</summary>
        </member>
    </members>
</doc>";
            AssertEx.Equal(expected, actual);
        }
    }
}
