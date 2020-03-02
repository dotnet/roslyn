// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.PullMemberUp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp
{
    internal class TestPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IEnumerable<(string member, bool makeAbstract)> _selectedMembers;

        private string DestinationName { get; }

        public TestPullMemberUpService(IEnumerable<(string member, bool makeAbstract)> selectedMembers, string destinationName)
        {
            _selectedMembers = selectedMembers;
            DestinationName = destinationName;
        }

        public PullMembersUpOptions GetPullMemberUpOptions(Document document, INamedTypeSymbol selectedNodeSymbol)
        {
            var members = selectedNodeSymbol.ContainingType.GetMembers().Where(member => MemberAndDestinationValidator.IsMemberValid(member));

            var selectedMember = _selectedMembers == null
                ? members.Select(member => (member: member, makeAbstract: false))
                : _selectedMembers.Select(selection => (member: members.Single(symbol => symbol.Name == selection.member), makeAbstract: selection.makeAbstract));

            var allInterfaces = selectedNodeSymbol.ContainingType.AllInterfaces;
            var baseClass = selectedNodeSymbol.ContainingType.BaseType;

            var analysisResults = selectedMember.SelectAsArray(member => new MemberAnalysisResult(member.member, makeMemberDeclarationAbstract: member.makeAbstract));

            INamedTypeSymbol destination = default;
            if (DestinationName == null)
            {
                destination = allInterfaces.FirstOrDefault() ?? baseClass;

                if (destination == null)
                {
                    throw new ArgumentException($"No target base type for {selectedNodeSymbol}");
                }
            }
            else
            {
                if (allInterfaces != null)
                {
                    destination = allInterfaces.SingleOrDefault(@interface => @interface.Name == DestinationName);
                }

                if (baseClass != null && destination == null)
                {
                    for (var i = baseClass; i != null; i = i.BaseType)
                    {
                        if (i.Name == DestinationName)
                        {
                            return new PullMembersUpOptions(i, analysisResults);
                        }
                    }
                }
            }

            if (destination == null)
            {
                throw new ArgumentException($"No Matching target base type for {DestinationName}");
            }
            else
            {
                return new PullMembersUpOptions(destination, analysisResults);
            }
        }
    }
}
