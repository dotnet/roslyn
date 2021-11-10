// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.StackTraceExplorer
{
    [Shared]
    [ExportLanguageService(typeof(IStackTraceExplorerService), language: LanguageNames.CSharp)]
    internal class CSharpStackTraceExplorerService : IStackTraceExplorerService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpStackTraceExplorerService()
        {
        }

        public string GetTypeMetadataName(string className)
        {
            var typeNameSyntax = SyntaxFactory.ParseTypeName(className);
            if (typeNameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
            {
                return GetMetadataNameOfQualifiedNameSyntax(qualifiedNameSyntax);
            }

            return GetGenericNameWithArity(typeNameSyntax);
        }

        private static string GetMetadataNameOfQualifiedNameSyntax(QualifiedNameSyntax originalSyntax)
        {
            SyntaxNode syntax = originalSyntax;
            Stack<string> parts = new();

            while (true)
            {
                if (syntax is QualifiedNameSyntax qualifiedNameSyntax)
                {
                    syntax = qualifiedNameSyntax.Left;
                    parts.Push(GetGenericNameWithArity(qualifiedNameSyntax.Right));
                }
                else
                {
                    parts.Push(GetGenericNameWithArity(syntax));
                    break;
                }
            }

            return parts.Join(".");
        }

        private static string GetGenericNameWithArity(SyntaxNode node)
        {
            if (node is not GenericNameSyntax genericNameSyntax)
            {
                return node.ToString();
            }

            return $"{genericNameSyntax.Identifier}`{genericNameSyntax.Arity}";
        }

        public string GetMethodSymbolName(string methodName)
        {
            methodName = methodName.Replace("[", "<").Replace("]", ">");
            // Add a void return so it parses as a method declaration and not constructor
            var declaration = SyntaxFactory.ParseMemberDeclaration($"void {methodName}");
            if (declaration is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                var paramList = methodDeclarationSyntax.ParameterList.Parameters.Select(p => p.Type?.ToString());
                return $"{methodDeclarationSyntax.Identifier}{methodDeclarationSyntax.TypeParameterList}({paramList.Join(", ")})";
            }

            return methodName;
        }
    }
}
