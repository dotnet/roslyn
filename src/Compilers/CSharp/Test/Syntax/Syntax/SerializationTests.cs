// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SerializationTests
    {
        [Fact]
        public void RoundTripSyntaxNode()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.True(droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithDiagnostics()
        {
            var text = "public class C {";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();
            Assert.Equal(1, root.Errors().Length);

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.Equal(1, droot.Errors().Length);
            Assert.True(droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
            Assert.Equal(root.Errors()[0].GetMessage(), droot.Errors()[0].GetMessage());
        }

        [Fact]
        public void RoundTripSyntaxNodeWithAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation();
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation);
            Assert.True(root.ContainsAnnotations);
            Assert.True(root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.True(droot.ContainsAnnotations);
            Assert.True(droot.HasAnnotation(annotation));
            Assert.True(droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithMultipleReferencesToSameAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation();
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation, annotation);
            Assert.True(root.ContainsAnnotations);
            Assert.True(root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.True(droot.ContainsAnnotations);
            Assert.True(droot.HasAnnotation(annotation));
            Assert.True(droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithSpecialAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation("TestAnnotation", "this is a test");
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation);
            Assert.True(root.ContainsAnnotations);
            Assert.True(root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.True(droot.ContainsAnnotations);
            Assert.True(droot.HasAnnotation(annotation));
            Assert.True(droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));

            var dannotation = droot.GetAnnotations("TestAnnotation").SingleOrDefault();
            Assert.NotNull(dannotation);
            Assert.NotSame(annotation, dannotation); // not exact same instance
            Assert.Equal(annotation, dannotation); // equivalent though
        }

        [Fact]
        public void RoundTripSyntaxNodeWithAnnotationsRemoved()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation1 = new SyntaxAnnotation("annotation1");
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation1);
            Assert.True(root.ContainsAnnotations);
            Assert.True(root.HasAnnotation(annotation1));
            var removedRoot = root.WithoutAnnotations(annotation1);
            Assert.False(removedRoot.ContainsAnnotations);
            Assert.False(removedRoot.HasAnnotation(annotation1));

            var stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.False(droot.ContainsAnnotations);
            Assert.False(droot.HasAnnotation(annotation1));

            var annotation2 = new SyntaxAnnotation("annotation2");

            var doubleAnnoRoot = droot.WithAdditionalAnnotations(annotation1, annotation2);
            Assert.True(doubleAnnoRoot.ContainsAnnotations);
            Assert.True(doubleAnnoRoot.HasAnnotation(annotation1));
            Assert.True(doubleAnnoRoot.HasAnnotation(annotation2));
            var removedDoubleAnnoRoot = doubleAnnoRoot.WithoutAnnotations(annotation1, annotation2);
            Assert.False(removedDoubleAnnoRoot.ContainsAnnotations);
            Assert.False(removedDoubleAnnoRoot.HasAnnotation(annotation1));
            Assert.False(removedDoubleAnnoRoot.HasAnnotation(annotation2));

            stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.False(droot.ContainsAnnotations);
            Assert.False(droot.HasAnnotation(annotation1));
            Assert.False(droot.HasAnnotation(annotation2));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithAnnotationRemovedWithMultipleReference()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation1 = new SyntaxAnnotation("MyAnnotationId", "SomeData");
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation1, annotation1);
            Assert.True(root.ContainsAnnotations);
            Assert.True(root.HasAnnotation(annotation1));
            var removedRoot = root.WithoutAnnotations(annotation1);
            Assert.False(removedRoot.ContainsAnnotations);
            Assert.False(removedRoot.HasAnnotation(annotation1));

            var stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.False(droot.ContainsAnnotations);
            Assert.False(droot.HasAnnotation(annotation1));
        }

        private static void RoundTrip(string text, bool expectRecursive = true)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();
            var originalText = root.ToFullString();

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;
            var newRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            var newText = newRoot.ToFullString();

            Assert.True(newRoot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
            Assert.Equal(originalText, newText);
        }

        [Fact]
        public void RoundTripXmlDocComment()
        {
            RoundTrip(@"/// <summary>XML Doc comment</summary>
class C { }");
        }

        [Fact]
        public void RoundTripCharLiteralWithIllegalUnicodeValue()
        {
            RoundTrip(@"public class C { char c = '\uDC00'; }");
        }

        [Fact]
        public void RoundTripCharLiteralWithIllegalUnicodeValue2()
        {
            RoundTrip(@"public class C { char c = '\");
        }

        [Fact]
        public void RoundTripCharLiteralWithIllegalUnicodeValue3()
        {
            RoundTrip(@"public class C { char c = '\u");
        }

        [Fact]
        public void RoundTripCharLiteralWithIllegalUnicodeValue4()
        {
            RoundTrip(@"public class C { char c = '\uDC00DC");
        }

        [Fact]
        public void RoundTripStringLiteralWithIllegalUnicodeValue()
        {
            RoundTrip(@"public class C { string s = ""\uDC00""; }");
        }

        [Fact]
        public void RoundTripStringLiteralWithUnicodeCharacters()
        {
            RoundTrip(@"public class C { string s = ""Юникод""; }");
        }

        [Fact]
        public void RoundTripStringLiteralWithUnicodeCharacters2()
        {
            RoundTrip(@"public class C { string c = ""\U0002A6A5𪚥""; }");
        }

        [Fact, WorkItem(1038237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038237")]
        public void RoundTripPragmaDirective()
        {
            var text = @"#pragma disable warning CS0618";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();
            Assert.True(root.ContainsDirectives);

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var newRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            Assert.True(newRoot.ContainsDirectives);
        }

        [Fact]
        public void RoundTripDeepSyntaxNode()
        {
            // trees with excessively deep expressions tend to overflow the stack when using recursive encoding.
            // test that the tree is successfully serialized using non-recursive encoding.
            var text = @"
public class C
{
    public string B = " + string.Join(" + ", Enumerable.Range(0, 1000).Select(i => "\"" + i.ToString() + "\"").ToArray()) + @";
}";

            // serialization should fail to encode stream using recursive object encoding and
            // succeed with non-recursive object encoding.
            RoundTrip(text, expectRecursive: false);
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
