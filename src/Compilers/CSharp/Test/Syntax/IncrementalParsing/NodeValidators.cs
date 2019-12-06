// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class NodeValidators
    {
        #region Verifiers
        internal static void PointerNameVerification(ExpressionSyntax nameTree, string name)
        {
            Assert.IsType<PointerTypeSyntax>(nameTree);
            var pointerName = nameTree as PointerTypeSyntax;
            Assert.Equal(pointerName.ElementType.ToString(), name);
        }

        internal static void PredefinedNameVerification(ExpressionSyntax nameTree, string typeName)
        {
            Assert.IsType<PredefinedTypeSyntax>(nameTree);
            var predefName = nameTree as PredefinedTypeSyntax;
            Assert.Equal(predefName.ToString(), typeName);
        }

        internal static void ArrayNameVerification(ExpressionSyntax nameTree, string arrayName, int numRanks)
        {
            Assert.IsType<ArrayTypeSyntax>(nameTree);
            var arrayType = nameTree as ArrayTypeSyntax;
            Assert.Equal(arrayType.ElementType.ToString(), arrayName);
            Assert.Equal(arrayType.RankSpecifiers.Count(), numRanks);
        }

        internal static void AliasedNameVerification(ExpressionSyntax nameTree, string alias, string name)
        {
            // Verification of the change
            Assert.IsType<AliasQualifiedNameSyntax>(nameTree);
            var aliasName = nameTree as AliasQualifiedNameSyntax;
            Assert.Equal(aliasName.Alias.ToString(), alias);
            Assert.Equal(aliasName.Name.ToString(), name);
        }

        internal static void DottedNameVerification(ExpressionSyntax nameTree, string left, string right)
        {
            // Verification of the change
            Assert.IsType<QualifiedNameSyntax>(nameTree);
            var dottedName = nameTree as QualifiedNameSyntax;
            Assert.Equal(dottedName.Left.ToString(), left);
            Assert.Equal(dottedName.Right.ToString(), right);
        }

        internal static void GenericNameVerification(ExpressionSyntax nameTree, string name, params string[] typeNames)
        {
            // Verification of the change
            Assert.IsType<GenericNameSyntax>(nameTree);
            var genericName = nameTree as GenericNameSyntax;
            Assert.Equal(genericName.Identifier.ToString(), name);
            Assert.Equal(genericName.TypeArgumentList.Arguments.Count, typeNames.Count());
            int i = 0;
            foreach (string str in typeNames)
            {
                Assert.Equal(genericName.TypeArgumentList.Arguments[i].ToString(), str);
                i++;
            }
        }

        internal static void BasicNameVerification(ExpressionSyntax nameTree, string name)
        {
            // Verification of the change
            Assert.IsType<IdentifierNameSyntax>(nameTree);
            var genericName = nameTree as IdentifierNameSyntax;
            Assert.Equal(genericName.ToString(), name);
        }
        #endregion
    }
}
