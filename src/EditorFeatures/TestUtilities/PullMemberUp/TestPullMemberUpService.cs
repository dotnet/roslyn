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
        
        private string TargetBaseTypeName { get; }

        public TestPullMemberUpService(IEnumerable<(string member, bool makeAbstract)> selectedMembers, string targetBaseTypeName)
        {
            SelectedMembers = selectedMembers;
            TargetBaseTypeName = targetBaseTypeName;
        }

        public PullMembersUpAnalysisResult GetPullMemberUpAnalysisResultFromDialogBox(ISymbol selectedNodeSymbol, Document document)
        {
            var members = selectedNodeSymbol.ContainingType.GetMembers().Where(member => MemberAndDestinationValidator.IsMemeberValid(member));

            var selectedMember = SelectedMembers == null
                ? members.Select(member => (member, false))
                : SelectedMembers.Select(selection => (members.Single(symbol => symbol.Name == selection.member), selection.makeAbstract));
        
            var allInterfaces = selectedNodeSymbol.ContainingType.AllInterfaces;
            var baseClass = selectedNodeSymbol.ContainingType.BaseType;

            ISymbol targetSymbol = default;
            if (TargetBaseTypeName == null)
            {
                targetSymbol = allInterfaces.FirstOrDefault() ?? baseClass;

                if (targetSymbol == null)
                {
                    throw new ArgumentException($"No target base type for {selectedNodeSymbol}");
                }
            }
            else
            {
                if (allInterfaces != null)
                {
                   targetSymbol = allInterfaces.SingleOrDefault(@interface => @interface.Name == TargetBaseTypeName);
                }
                
                if (baseClass != null && targetSymbol == null)
                {
                    for (var i = baseClass; i != null; i = i.BaseType)
                    {
                        if (i.Name == TargetBaseTypeName)
                        {
                            return PullMembersUpAnalysisBuilder.BuildAnalysisResult(i, selectedMember.ToImmutableArray());
                        }
                    }
                }
            }

            if (targetSymbol == null)
            {
                throw new ArgumentException($"No Matching target base type for {TargetBaseTypeName}");
            }
            else
            {
                return PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetSymbol as INamedTypeSymbol, selectedMember.ToImmutableArray());
            }
        }
    }
}
