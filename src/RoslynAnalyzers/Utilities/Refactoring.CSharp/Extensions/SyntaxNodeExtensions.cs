//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

//namespace Analyzer.Utilities.Extensions
//{
//    internal static partial class SyntaxNodeExtensions
//    {
//        public static SyntaxList<AttributeListSyntax> GetAttributeLists(this SyntaxNode declaration)
//            => declaration switch
//            {
//                MemberDeclarationSyntax memberDecl => memberDecl.AttributeLists,
//                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
//                ParameterSyntax parameter => parameter.AttributeLists,
//                CompilationUnitSyntax compilationUnit => compilationUnit.AttributeLists,
//                _ => default,
//            };
//    }
//}
