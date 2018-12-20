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
        private IEnumerable<(string member, bool makeAbstract)> SelectedMembers { get; }
        
        private string DestinationName { get; }

        public TestPullMemberUpService(IEnumerable<(string member, bool makeAbstract)> selectedMembers, string destinationName)
        {
            SelectedMembers = selectedMembers;
            DestinationName = destinationName;
        }

        public PullMembersUpAnalysisResult GetPullMemberUpOptions(Document document, ISymbol selectedNodeSymbol)
        {
            var members = selectedNodeSymbol.ContainingType.GetMembers().Where(member => MemberAndDestinationValidator.IsMemeberValid(member));

            var selectedMember = SelectedMembers == null
                ? members.Select(member => (member, false))
                : SelectedMembers.Select(selection => (members.Single(symbol => symbol.Name == selection.member), selection.makeAbstract));
        
            var allInterfaces = selectedNodeSymbol.ContainingType.AllInterfaces;
            var baseClass = selectedNodeSymbol.ContainingType.BaseType;

            ISymbol destination = default;
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
                            return PullMembersUpAnalysisBuilder.BuildAnalysisResult(i, selectedMember.ToImmutableArray());
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
                return PullMembersUpAnalysisBuilder.BuildAnalysisResult(destination as INamedTypeSymbol, selectedMember.ToImmutableArray());
            }
        }
    }
}
