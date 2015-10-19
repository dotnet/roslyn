// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    using Microsoft.CodeAnalysis;
    using CSharp = Microsoft.CodeAnalysis.CSharp;

    public static class Extensions
    {
        public static TextSpan GetCodeCleanupSpan(this SyntaxNode node)
        {
            var previousToken = node.GetFirstToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            var endToken = node.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

            return TextSpan.FromBounds(previousToken.SpanStart, endToken.Span.End);
        }

        public static T GetMember<T>(this Document document, int index) where T : SyntaxNode
        {
            return (T)document.GetSyntaxRootAsync().Result.GetMember(index);
        }

        public static T GetMember<T>(this T node, int index) where T : SyntaxNode
        {
            dynamic d = node;
            return (T)d.Members[index];
        }

        public static T RemoveCSharpMember<T>(this T node, int index) where T : SyntaxNode
        {
            var newMembers = CSharp.SyntaxFactory.List(node.RemoveMember<CSharp.Syntax.MemberDeclarationSyntax>(index));

            dynamic d = node;
            return (T)d.WithMembers(newMembers);
        }

        public static T AddCSharpMember<T>(this T node, CSharp.Syntax.MemberDeclarationSyntax member, int index) where T : SyntaxNode
        {
            var newMembers = CSharp.SyntaxFactory.List(node.AddMember<CSharp.Syntax.MemberDeclarationSyntax>(member, index));

            dynamic d = node;
            return (T)d.WithMembers(newMembers);
        }

        public static IEnumerable<M> RemoveMember<M>(this SyntaxNode node, int index) where M : SyntaxNode
        {
            dynamic d = node;

            var members = ((IEnumerable<M>)d.Members).ToList();
            members.RemoveAt(index);

            return members;
        }

        public static IEnumerable<M> AddMember<M>(this SyntaxNode node, M member, int index)
            where M : SyntaxNode
        {
            dynamic d = node;

            var members = ((IEnumerable<M>)d.Members).ToList();
            members.Insert(index, member);

            return members;
        }
    }
}
