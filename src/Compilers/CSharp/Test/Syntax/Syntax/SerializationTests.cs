// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

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

            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
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
            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
            Assert.Equal(root.Errors()[0].GetMessage(), droot.Errors()[0].GetMessage());
        }

        [Fact]
        public void RoundTripSyntaxNodeWithAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation();
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation);
            Assert.Equal(true, root.ContainsAnnotations);
            Assert.Equal(true, root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.Equal(true, droot.ContainsAnnotations);
            Assert.Equal(true, droot.HasAnnotation(annotation));
            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithMultipleReferencesToSameAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation();
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation, annotation);
            Assert.Equal(true, root.ContainsAnnotations);
            Assert.Equal(true, root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.Equal(true, droot.ContainsAnnotations);
            Assert.Equal(true, droot.HasAnnotation(annotation));
            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithSpecialAnnotation()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation = new SyntaxAnnotation("TestAnnotation", "this is a test");
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation);
            Assert.Equal(true, root.ContainsAnnotations);
            Assert.Equal(true, root.HasAnnotation(annotation));

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var dtext = droot.ToFullString();

            Assert.Equal(text, dtext);
            Assert.Equal(true, droot.ContainsAnnotations);
            Assert.Equal(true, droot.HasAnnotation(annotation));
            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));

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
            Assert.Equal(true, root.ContainsAnnotations);
            Assert.Equal(true, root.HasAnnotation(annotation1));
            var removedRoot = root.WithoutAnnotations(annotation1);
            Assert.Equal(false, removedRoot.ContainsAnnotations);
            Assert.Equal(false, removedRoot.HasAnnotation(annotation1));

            var stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.Equal(false, droot.ContainsAnnotations);
            Assert.Equal(false, droot.HasAnnotation(annotation1));

            var annotation2 = new SyntaxAnnotation("annotation2");

            var doubleAnnoRoot = droot.WithAdditionalAnnotations(annotation1, annotation2);
            Assert.Equal(true, doubleAnnoRoot.ContainsAnnotations);
            Assert.Equal(true, doubleAnnoRoot.HasAnnotation(annotation1));
            Assert.Equal(true, doubleAnnoRoot.HasAnnotation(annotation2));
            var removedDoubleAnnoRoot = doubleAnnoRoot.WithoutAnnotations(annotation1, annotation2);
            Assert.Equal(false, removedDoubleAnnoRoot.ContainsAnnotations);
            Assert.Equal(false, removedDoubleAnnoRoot.HasAnnotation(annotation1));
            Assert.Equal(false, removedDoubleAnnoRoot.HasAnnotation(annotation2));

            stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.Equal(false, droot.ContainsAnnotations);
            Assert.Equal(false, droot.HasAnnotation(annotation1));
            Assert.Equal(false, droot.HasAnnotation(annotation2));
        }

        [Fact]
        public void RoundTripSyntaxNodeWithAnnotationRemovedWithMultipleReference()
        {
            var text = "public class C {}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var annotation1 = new SyntaxAnnotation("MyAnnotationId", "SomeData");
            var root = tree.GetCompilationUnitRoot().WithAdditionalAnnotations(annotation1, annotation1);
            Assert.Equal(true, root.ContainsAnnotations);
            Assert.Equal(true, root.HasAnnotation(annotation1));
            var removedRoot = root.WithoutAnnotations(annotation1);
            Assert.Equal(false, removedRoot.ContainsAnnotations);
            Assert.Equal(false, removedRoot.HasAnnotation(annotation1));

            var stream = new MemoryStream();
            removedRoot.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);

            Assert.Equal(false, droot.ContainsAnnotations);
            Assert.Equal(false, droot.HasAnnotation(annotation1));
        }

        private static void RoundTrip(string text)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();
            var originalText = root.ToFullString();

            var stream = new MemoryStream();
            root.SerializeTo(stream);

            stream.Position = 0;

            var newRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            var newText = newRoot.ToFullString();

            Assert.Equal(true, newRoot.IsEquivalentTo(tree.GetCompilationUnitRoot()));
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

#if false
        [Fact]
        public void RoundTripBigSyntaxNode()
        {
            var path = Path.GetFullPath(@"..\..\open\compilers\csharp\source\syntax\generated.cs");
            var text = File.ReadAllText(path);

            var parseStart = DateTime.UtcNow;
            var tree = SyntaxTree.ParseCompilationUnit(text);
            var parseEnd = DateTime.UtcNow;

            var root = tree.GetCompilationUnitRoot();

            var stream = new MemoryStream();

            var serializeStart = DateTime.UtcNow;
            root.SerializeTo(stream);
            var serializeEnd = DateTime.UtcNow;

            stream.Position = 0;

            var deserializeStart = DateTime.UtcNow;
            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var deserializeEnd = DateTime.UtcNow;

            var dtext = droot.ToString();

            Assert.Equal(text, dtext);
            Assert.Equal(true, droot.IsEquivalentTo(tree.GetCompilationUnitRoot()));

            var parseTime = (parseEnd - parseStart).TotalMilliseconds;
            var serializeTime = (serializeEnd - serializeStart).TotalMilliseconds;
            var deserializeTime = (deserializeEnd - deserializeStart).TotalMilliseconds;
        }
#endif
    }
}
