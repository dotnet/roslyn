using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp;

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

        public PullMembersUpOptions GetPullMemberUpOptions(Document document, ISymbol selectedNodeSymbol)
        {
            var members = selectedNodeSymbol.ContainingType.GetMembers().Where(member => MemberAndDestinationValidator.IsMemberValid(member));

            var selectedMember = _selectedMembers == null
                ? members.Select(member => (member, false))
                : _selectedMembers.Select(selection => (members.Single(symbol => symbol.Name == selection.member), selection.makeAbstract));

            var allInterfaces = selectedNodeSymbol.ContainingType.AllInterfaces;
            var baseClass = selectedNodeSymbol.ContainingType.BaseType;

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
                            return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(i, selectedMember.ToImmutableArray());
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
                return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, selectedMember.ToImmutableArray());
            }
        }
    }
}
