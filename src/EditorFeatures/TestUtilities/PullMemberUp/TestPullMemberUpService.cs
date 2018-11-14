using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;

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

        public PullMemberDialogResult GetPullTargetAndMembers(SemanticModel semanticModel, ISymbol selectedNodeSymbol)
        {
            IEnumerable<(ISymbol member, bool makeAbstract)> selectedMember = default;
            var members = selectedNodeSymbol.ContainingType.GetMembers().Where(
                    member => {
                        if (member is IMethodSymbol methodSymbol)
                        {
                            return methodSymbol.MethodKind == MethodKind.Ordinary;
                        }
                        else if (member is IFieldSymbol fieldSymbol)
                        {
                            return !member.IsImplicitlyDeclared;
                        }
                        else
                        {
                            return member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event;
                        }
                    });
            if (SelectedMembers == null)
            {
                selectedMember = members.Select(member => (member, false));
            }
            else
            {
                selectedMember = SelectedMembers.Select(selection => (members.Single(symbol => symbol.Name == selection.member), selection.makeAbstract));
            }
        
            ISymbol targetSymbol = default;
            var allInterfaces = selectedNodeSymbol.ContainingType.AllInterfaces;
            var baseClass = selectedNodeSymbol.ContainingType.BaseType;
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
                            return new PullMemberDialogResult(PullMembersUpAnalysisBuilder.BuildAnalysisResult(i, selectedMember));
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
                return new PullMemberDialogResult(PullMembersUpAnalysisBuilder.BuildAnalysisResult(targetSymbol as INamedTypeSymbol, selectedMember));
            }
        }
    }
}
