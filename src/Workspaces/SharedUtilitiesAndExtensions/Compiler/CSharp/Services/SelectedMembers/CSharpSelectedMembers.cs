// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal class CSharpSelectedMembers
    {
        public static readonly CSharpSelectedMembers Instance = new CSharpSelectedMembers();

        private CSharpSelectedMembers()
        {
        }

        public async Task<ImmutableArray<SyntaxNode>> GetSelectedFieldsAndPropertiesAsync(
            SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
        {
            var text = await tree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // If there is a selection, look for the token to the right of the selection That helps
            // the user select like so:
            //
            //          int i;[|
            //          int j;|]
            //
            // In this case (which is common with a mouse), we want to consider 'j' selected, and
            // 'i' not involved in all.
            //
            // However, if there is no selection and the user has:
            //
            //          int i;$$
            //          int j;
            //
            // Then we want to consider 'i' selected instead.  So we do a normal FindToken.

            var token = textSpan.IsEmpty
                ? root.FindToken(textSpan.Start)
                : root.FindTokenOnRightOfPosition(textSpan.Start);
            var firstMember = token.GetAncestors<MemberDeclarationSyntax>().FirstOrDefault();
            if (firstMember != null)
            {
                if (firstMember.Parent is TypeDeclarationSyntax containingType)
                    return GetFieldsAndPropertiesInSpan(root, text, textSpan, containingType, firstMember, allowPartialSelection);
            }

            return ImmutableArray<SyntaxNode>.Empty;
        }

        private static ImmutableArray<SyntaxNode> GetFieldsAndPropertiesInSpan(
            SyntaxNode root,
            SourceText text,
            TextSpan textSpan,
            TypeDeclarationSyntax containingType,
            MemberDeclarationSyntax firstMember,
            bool allowPartialSelection)
        {
            var members = containingType.Members;
            var fieldIndex = members.IndexOf(firstMember);
            if (fieldIndex < 0)
                return ImmutableArray<SyntaxNode>.Empty;

            var selectedMembers = ArrayBuilder<SyntaxNode>.GetInstance();
            for (var i = fieldIndex; i < members.Count; i++)
            {
                var member = members[i];
                AddSelectedFieldOrPropertyDeclarations(member);
            }

            return selectedMembers.ToImmutableAndFree();

            void AddAllMembers(MemberDeclarationSyntax member)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax field:
                        selectedMembers.AddRange(field.Declaration.Variables);
                        return;
                    case PropertyDeclarationSyntax property:
                        selectedMembers.Add(property);
                        return;
                }
            }

            // local functions
            void AddSelectedFieldOrPropertyDeclarations(MemberDeclarationSyntax member)
            {
                if (!member.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration))
                    return;

                // first, check if entire member is selected.  If so, we definitely include this member.
                if (textSpan.Contains(member.Span))
                {
                    AddAllMembers(member);
                    return;
                }

                if (textSpan.IsEmpty)
                {
                    // No selection.  We consider this member selected if a few cases are true:
                    //
                    //  1. Position precedes the first token of the member (on the same line).
                    //  2. Position touches the name of the field/prop.
                    //  3. Position is after the last token of the member (on the same line).

                    var position = textSpan.Start;
                    if (text.IsBeforeOrAfterNodeOnSameLine(root, member, position))
                    {
                        AddAllMembers(member);
                        return;
                    }
                    else
                    {
                        switch (member)
                        {
                            case FieldDeclarationSyntax field:
                                foreach (var varDecl in field.Declaration.Variables)
                                {
                                    if (varDecl.Identifier.FullSpan.IntersectsWith(position))
                                        selectedMembers.Add(varDecl);
                                }

                                return;
                            case PropertyDeclarationSyntax property:
                                if (property.Identifier.FullSpan.IntersectsWith(position))
                                    selectedMembers.Add(property);

                                return;
                        }
                    }
                }
                else
                {
                    // if the user has an actual selection, get the fields/props if the selection
                    // surrounds the names of in the case of allowPartialSelection.

                    if (!allowPartialSelection)
                        return;

                    switch (member)
                    {
                        case FieldDeclarationSyntax field:
                            var variables = field.Declaration.Variables;
                            foreach (var variable in variables)
                            {
                                if (textSpan.OverlapsWith(variable.Identifier.Span))
                                    selectedMembers.Add(variable);
                            }

                            return;
                        case PropertyDeclarationSyntax property:
                            selectedMembers.Add(property);
                            return;
                    }
                }
            }
        }
    }
}
