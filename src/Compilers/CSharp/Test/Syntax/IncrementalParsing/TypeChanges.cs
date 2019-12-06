// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.IncrementalParsing
{
    // These tests handle changing between constructors/destructors and methods.
    // In addition, changes between get/set and add/remove are also tested
    public class TypeChanges
    {
        [Fact]
        public void ConstructorToDestructor()
        {
            string oldText = @"class construct{
                              public construct(){}   
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "construct", "~construct");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                Assert.IsType<DestructorDeclarationSyntax>(classType.Members[0]);
            });
        }

        [Fact]
        public void MethodToConstructor()
        {
            string oldText = @"class construct{
                              public M(){}   
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "M", "construct");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                Assert.IsType<ConstructorDeclarationSyntax>(classType.Members[0]);
            });
        }

        [Fact]
        public void ConstructorToMethod()
        {
            string oldText = @"class construct{
                              public construct(){}   
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "construct", "M");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                Assert.IsType<ConstructorDeclarationSyntax>(classType.Members[0]);
            });
        }

        [Fact]
        public void DestructorToConstructor()
        {
            string oldText = @"class construct{
                              public ~construct(){}   
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "~construct", "construct");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                Assert.IsType<ConstructorDeclarationSyntax>(classType.Members[0]);
            });
        }

        [Fact]
        public void SetToGet()
        {
            string oldText = @"class construct{
                                public int B {get {} }
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "get", "set");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                var propertyType = classType.Members[0] as PropertyDeclarationSyntax;
                Assert.Equal(SyntaxKind.SetAccessorDeclaration, propertyType.AccessorList.Accessors[0].Kind());
            });
        }

        [Fact]
        public void GetToSet()
        {
            string oldText = @"class construct{
                                public int B {set {} }
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "set", "get");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                var propertyType = classType.Members[0] as PropertyDeclarationSyntax;
                Assert.Equal(SyntaxKind.GetAccessorDeclaration, propertyType.AccessorList.Accessors[0].Kind());
            });
        }

        [Fact]
        public void EventAddToRemove()
        {
            string oldText = @"class construct{
                                public event B b {add {} }
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "add", "remove");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                var propertyType = classType.Members[0] as EventDeclarationSyntax;
                Assert.Equal(SyntaxKind.RemoveAccessorDeclaration, propertyType.AccessorList.Accessors[0].Kind());
            });
        }

        [Fact]
        public void EventRemoveToAdd()
        {
            string oldText = @"class construct{
                                public event B b {remove {} }
                              }";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithReplace(16, "remove", "add");
                var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
                var propertyType = classType.Members[0] as EventDeclarationSyntax;
                Assert.Equal(SyntaxKind.AddAccessorDeclaration, propertyType.AccessorList.Accessors[0].Kind());
            });
        }

        #region Helpers
        private static void ParseAndVerify(string text, Action<SyntaxTree> validator)
        {
            ParseAndValidate(text, validator);
            ParseAndValidate(text, validator, TestOptions.Script);
        }

        private static void ParseAndValidate(string text, Action<SyntaxTree> validator, CSharpParseOptions options = null)
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree(text);
            validator(oldTree);
        }
        #endregion
    }
}
