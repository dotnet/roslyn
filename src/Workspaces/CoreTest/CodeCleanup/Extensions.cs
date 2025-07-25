// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup;

using Microsoft.CodeAnalysis;
using CSharp = Microsoft.CodeAnalysis.CSharp;

public static class Extensions
{
    extension(SyntaxNode node)
    {
        public TextSpan GetCodeCleanupSpan()
        {
            var previousToken = node.GetFirstToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).GetPreviousToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            var endToken = node.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true).GetNextToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

            return TextSpan.FromBounds(previousToken.SpanStart, endToken.Span.End);
        }

        public IEnumerable<M> RemoveMember<M>(int index) where M : SyntaxNode
        {
            dynamic d = node;

            var members = ((IEnumerable<M>)d.Members).ToList();
            members.RemoveAt(index);

            return members;
        }

        public IEnumerable<M> AddMember<M>(M member, int index)
            where M : SyntaxNode
        {
            dynamic d = node;

            var members = ((IEnumerable<M>)d.Members).ToList();
            members.Insert(index, member);

            return members;
        }
    }

    extension(Document document)
    {
        public T GetMember<T>(int index) where T : SyntaxNode
        => (T)document.GetSyntaxRootAsync().Result.GetMember(index);
    }

    extension<T>(T node) where T : SyntaxNode
    {
        public T GetMember(int index)
        {
            dynamic d = node;
            return (T)d.Members[index];
        }

        public T RemoveCSharpMember(int index)
        {
            var newMembers = CSharp.SyntaxFactory.List(node.RemoveMember<CSharp.Syntax.MemberDeclarationSyntax>(index));

            dynamic d = node;
            return (T)d.WithMembers(newMembers);
        }

        public T AddCSharpMember(CSharp.Syntax.MemberDeclarationSyntax member, int index)
        {
            var newMembers = CSharp.SyntaxFactory.List(node.AddMember<CSharp.Syntax.MemberDeclarationSyntax>(member, index));

            dynamic d = node;
            return (T)d.WithMembers(newMembers);
        }
    }
}
