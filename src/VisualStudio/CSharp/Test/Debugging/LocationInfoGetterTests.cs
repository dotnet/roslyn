// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging
{
    public class LocationInfoGetterTests
    {
        private void Test(string markup, string expectedName, int expectedLineOffset)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(markup))
            {
                var testDocument = workspace.Documents.Single();
                var position = testDocument.CursorPosition.Value;
                var snapshot = testDocument.TextBuffer.CurrentSnapshot;
                var locationInfo = LocationInfoGetter.GetInfoAsync(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    position,
                    CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                Assert.Equal(expectedName, locationInfo.Name);
                Assert.Equal(expectedLineOffset, locationInfo.LineOffset);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        public void TestCSharpLanguageDebugInfoTryGetNameOfLocation()
        {
            Test("class F$$oo { }", "Foo", 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668), WorkItem(538415)]
        public void TestMethod()
        {
            Test(
@"class Class
{
    public static void Meth$$od()
    {
    }
}
", "Class.Method()", 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestNamespaces()
        {
            Test(
@"namespace Namespace
{
    class Class
    {
        void Method()
        {
        }$$
    }
}", "Namespace.Class.Method()", 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestDottedNamespaces()
        {
            Test(
@"namespace Namespace.Another
{
    class Class
    {
        void Method()
        {
        }$$
    }
}", "Namespace.Another.Class.Method()", 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestNestedTypes()
        {
            Test(
@"class Foo
{
    class Bar
    {
        void Quux()
        {$$
        }
    }
}", "Foo.Bar.Quux()", 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestPropertyGetter()
        {
            Test(
@"class Class
{
    string Property
    {
        get
        {
            return null;$$
        }
    }
}", "Class.Property", 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(527668)]
        public void TestPropertySetter()
        {
            Test(
@"class Class
{
    string Property
    {
        get
        {
            return null;
        }

        set
        {
            string s = $$value;
        }
    }
}", "Class.Property", 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(538415)]
        public void TestField()
        {
            Test(
@"class Class
{
    int fi$$eld;
}", "Class.field", 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(543494)]
        public void TestField2()
        {
            Test(
@"class Class
{
    Action<int> a = b => { in$$t c; };
}", "Class.a", 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)]
        [WorkItem(543494)]
        public void TestField3()
        {
            Test(
@"class Class
{
    int a1, a$$2;
}", "Class.a2", 0);
        }
    }
}
