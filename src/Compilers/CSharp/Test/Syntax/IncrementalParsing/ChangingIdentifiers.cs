// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.IncrementalParsing
{
    // These tests test changing between different types of identifiers
    public class ChangingIdentifiers
    {
        internal enum NameTypes
        {
            SingleName,
            DottedName,
            GenericName,
            AliasedName,
            PredefinedName,
            ArrayName,
            PointerName,

            // Not done
            // ArrayRankSpecifier
            // Type Argument - TODO: How would we do this???
        }

        [Fact]
        public void BasicToDotted()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.DottedName, expressionValidator: nameTree =>
            {
                NodeValidators.DottedNameVerification(nameTree, "b", "b");
            });
        }

        [Fact]
        public void BasicToGeneric()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.GenericName, expressionValidator: nameTree =>
            {
                NodeValidators.GenericNameVerification(nameTree, "b", "T");
            });
        }

        [Fact]
        public void BasicToAlias()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.AliasedName, expressionValidator: nameTree =>
            {
                NodeValidators.AliasedNameVerification(nameTree, "b", "d");
            });
        }

        [Fact]
        public void BasicToArray()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.ArrayName, expressionValidator: nameTree =>
            {
                NodeValidators.ArrayNameVerification(nameTree, "b", 1);
            });
        }

        [Fact]
        public void BasicToPredefined()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.PredefinedName, expressionValidator: nameTree =>
            {
                NodeValidators.PredefinedNameVerification(nameTree, "int");
            });
        }

        [Fact]
        public void BasicToPointer()
        {
            MakeIncrementalNameChange(NameTypes.SingleName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        [Fact]
        public void GenericToDotted()
        {
            MakeIncrementalNameChange(NameTypes.GenericName, NameTypes.DottedName, expressionValidator: nameTree =>
            {
                NodeValidators.DottedNameVerification(nameTree, "b", "b");
            });
        }

        [Fact]
        public void GenericToAlias()
        {
            MakeIncrementalNameChange(NameTypes.GenericName, NameTypes.AliasedName, expressionValidator: nameTree =>
            {
                NodeValidators.AliasedNameVerification(nameTree, "b", "d");
            });
        }

        [Fact]
        public void GenericToArray()
        {
            MakeIncrementalNameChange(NameTypes.GenericName, NameTypes.ArrayName, expressionValidator: nameTree =>
            {
                NodeValidators.ArrayNameVerification(nameTree, "b", 1);
            });
        }

        [Fact]
        public void GenericToPredefined()
        {
            MakeIncrementalNameChange(NameTypes.GenericName, NameTypes.PredefinedName, expressionValidator: nameTree =>
            {
                NodeValidators.PredefinedNameVerification(nameTree, "int");
            });
        }

        [Fact]
        public void GenericToPointer()
        {
            MakeIncrementalNameChange(NameTypes.GenericName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        [Fact]
        public void DottedToAlias()
        {
            MakeIncrementalNameChange(NameTypes.DottedName, NameTypes.AliasedName, expressionValidator: nameTree =>
            {
                NodeValidators.AliasedNameVerification(nameTree, "b", "d");
            });
        }

        [Fact]
        public void DottedToArray()
        {
            MakeIncrementalNameChange(NameTypes.DottedName, NameTypes.ArrayName, expressionValidator: nameTree =>
                {
                    NodeValidators.ArrayNameVerification(nameTree, "b", 1);
                });
        }

        [Fact]
        public void DottedToPredefined()
        {
            MakeIncrementalNameChange(NameTypes.DottedName, NameTypes.PredefinedName, expressionValidator: nameTree =>
            {
                NodeValidators.PredefinedNameVerification(nameTree, "int");
            });
        }

        [Fact]
        public void DottedToPointer()
        {
            MakeIncrementalNameChange(NameTypes.AliasedName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        [Fact]
        public void AliasToArray()
        {
            MakeIncrementalNameChange(NameTypes.AliasedName, NameTypes.ArrayName, expressionValidator: nameTree =>
            {
                NodeValidators.ArrayNameVerification(nameTree, "b", 1);
            });
        }

        [Fact]
        public void AliasToPredefined()
        {
            MakeIncrementalNameChange(NameTypes.AliasedName, NameTypes.PredefinedName, expressionValidator: nameTree =>
            {
                NodeValidators.PredefinedNameVerification(nameTree, "int");
            });
        }

        [Fact]
        public void AliasToPointer()
        {
            MakeIncrementalNameChange(NameTypes.AliasedName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        [Fact]
        public void ArrayToPredefined()
        {
            MakeIncrementalNameChange(NameTypes.ArrayName, NameTypes.PredefinedName, expressionValidator: nameTree =>
            {
                NodeValidators.PredefinedNameVerification(nameTree, "int");
            });
        }

        [Fact]
        public void ArrayToPointer()
        {
            MakeIncrementalNameChange(NameTypes.ArrayName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        [Fact]
        public void PredefinedToPointer()
        {
            MakeIncrementalNameChange(NameTypes.PredefinedName, NameTypes.PointerName, expressionValidator: nameTree =>
            {
                NodeValidators.PointerNameVerification(nameTree, "b");
            });
        }

        #region Specific helper functions

        private static string GetNameString(NameTypes newStyle)
        {
            switch (newStyle)
            {
                case NameTypes.SingleName: return "abc";
                case NameTypes.PredefinedName: return "int";
                case NameTypes.PointerName: return "b*";
                case NameTypes.GenericName: return "b<T>";
                case NameTypes.DottedName: return "b.b";
                case NameTypes.ArrayName: return "b[]";
                case NameTypes.AliasedName: return "b::d";
                default:
                    throw new Exception("Unexpected type here!!");
            }
        }

        private static void MakeIncrementalNameChange(NameTypes oldStyle, NameTypes newStyle, Action<ExpressionSyntax> expressionValidator)
        {
            MakeIncrementalNameChanges(oldStyle, newStyle, expressionValidator);
            MakeIncrementalNameChanges(oldStyle, newStyle, expressionValidator, options: TestOptions.Script);
            MakeIncrementalNameChanges(oldStyle, newStyle, expressionValidator, topLevel: true, options: TestOptions.Script);
        }

        private static void MakeIncrementalNameChanges(NameTypes oldStyle, NameTypes newStyle,
            Action<ExpressionSyntax> expressionValidator, bool topLevel = false, CSharpParseOptions options = null)
        {
            string oldName = GetNameString(oldStyle);
            string newName = GetNameString(newStyle);

            string code = oldName + @" m() {}";
            if (!topLevel)
            {
                code = @"class C { " + code + @"}";
            }
            else if (oldStyle == NameTypes.PointerName || newStyle == NameTypes.PointerName)
            {
                code = "unsafe " + code;
            }

            var oldTree = SyntaxFactory.ParseSyntaxTree(code, options: options);

            // Make the change to the node
            var newTree = oldTree.WithReplaceFirst(oldName, newName);
            var nameTree = topLevel ? GetGlobalMethodDeclarationSyntaxChange(newTree) : GetExpressionSyntaxChange(newTree);
            expressionValidator(nameTree);
        }

        private static ExpressionSyntax GetExpressionSyntaxChange(SyntaxTree newTree)
        {
            TypeDeclarationSyntax classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            MethodDeclarationSyntax method = classType.Members[0] as MethodDeclarationSyntax;
            var nameTree = method.ReturnType;
            return nameTree;
        }

        private static ExpressionSyntax GetGlobalMethodDeclarationSyntaxChange(SyntaxTree newTree)
        {
            var method = newTree.GetCompilationUnitRoot().Members[0] as MethodDeclarationSyntax;
            return method.ReturnType;
        }
        #endregion
    }
}
