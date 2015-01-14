// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.UnitTestFramework;
using Xunit;

namespace ImplementNotifyPropertyChangedCS.UnitTests
{
    public class IsExpandableTests : CodeActionProviderTestFixture
    {
        protected override string LanguageName
        {
            get
            {
                return LanguageNames.CSharp;
            }
        }

        [Fact]
        public void TryGetAccessors1()
        {
            const string Code = "class C { int P { get }";

            var property = SyntaxFactory.ParseCompilationUnit(Code).DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

            AccessorDeclarationSyntax getter;
            AccessorDeclarationSyntax setter;
            var result = ExpansionChecker.TryGetAccessors(property, out getter, out setter);

            Assert.False(result);
        }

        [Fact]
        public void TryGetAccessors2()
        {
            const string Code = "class C { int P { get; }";

            var property = SyntaxFactory.ParseCompilationUnit(Code).DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

            AccessorDeclarationSyntax getter;
            AccessorDeclarationSyntax setter;
            var result = ExpansionChecker.TryGetAccessors(property, out getter, out setter);

            Assert.False(result);
        }

        [Fact]
        public void TryGetAccessors3()
        {
            const string Code = "class C { int P { get; set }";

            var property = SyntaxFactory.ParseCompilationUnit(Code).DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

            AccessorDeclarationSyntax getter;
            AccessorDeclarationSyntax setter;
            var result = ExpansionChecker.TryGetAccessors(property, out getter, out setter);

            Assert.True(result);
        }

        [Fact]
        public void TryGetAccessors4()
        {
            const string Code = "class C { int P { get; set; }";

            var property = SyntaxFactory.ParseCompilationUnit(Code).DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

            AccessorDeclarationSyntax getter;
            AccessorDeclarationSyntax setter;
            var result = ExpansionChecker.TryGetAccessors(property, out getter, out setter);

            Assert.True(result);
        }

        private bool IsExpandableProperty(string code)
        {
            var document = CreateDocument(code);
            var property = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

            return ExpansionChecker.GetExpandablePropertyInfo(property, document.GetSemanticModelAsync().Result) != null;
        }

        [Fact]
        public void IsExpandableProperty1()
        {
            const string Code = "class C { int P { get }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty2()
        {
            const string Code = "class C { int P { get; }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty3()
        {
            const string Code = "class C { int P { get; set }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty4()
        {
            const string Code = "class C { int P { get; set; }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty5()
        {
            const string Code = "class C { int P { get { } set { } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty6()
        {
            const string Code = "class C { int P { get { return 1; } set { } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty7()
        {
            const string Code = "class C { int P { get { return P; } set { } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty8()
        {
            const string Code = "class C { int f; int P { get { return f; } set { } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty9()
        {
            const string Code = "class C { int f; int P { get { return f; } set { f = value; } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty10()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f != value) f = value; } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty11()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (value != f) f = value; } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty12()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f != value) { f = value; } } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty13()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (value != f) { f = value; } } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty14()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f == value) f = value; } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty15()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f != 1) f = value; } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty16()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f != value) { return; } } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty17()
        {
            const string Code = "class C { int f; int f2; int P { get { return f; } set { if (f != value) { f2 = value; } } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty18()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f == value) return; f = value; } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty19()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f == value) { return; } f = value; } }";
            Assert.True(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty20()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f != value) { return; } f = value; } }";
            Assert.False(IsExpandableProperty(Code));
        }

        [Fact]
        public void IsExpandableProperty21()
        {
            const string Code = "class C { int f; int P { get { return f; } set { if (f == value) { return; } f = 1; } }";
            Assert.False(IsExpandableProperty(Code));
        }
    }
}
