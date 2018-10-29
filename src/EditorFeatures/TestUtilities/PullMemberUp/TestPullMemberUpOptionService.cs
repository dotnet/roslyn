//using System;
//using System.Collections.Generic;
//using System.Composition;
//using System.Linq;
//using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
//using Microsoft.CodeAnalysis.Host.Mef;

//namespace Microsoft.CodeAnalysis.Test.Utilities.PullMemberUp
//{
//    [ExportWorkspaceService(typeof(IPullMemberUpDialogService), ServiceLayer.Default), Shared]
//    internal class TestPullMemberUpService : IPullMemberUpDialogService
//    {
//        public IEnumerable<(ISymbol member, bool makeAbstract)> SelectMembers { get; set; }

//        public INamedTypeSymbol Target { get; set; }

//        public bool IsCanceled { get; set; } = false;

//        public bool UserClickFinish { get; set; } = true;

//        public bool CreateWarningDialog(List<string> warningMessageList)
//        {
//            return UserClickFinish;
//        }

//        private INamedTypeSymbol GetDefaultTarget(ISymbol member)
//        {
//            if (member.ContainingType != null)
//            {
//                if (member.ContainingType.AllInterfaces != null)
//                {
//                    return member.ContainingType.AllInterfaces.FirstOrDefault();
//                }

//                if (member.ContainingType.BaseType != null)
//                {
//                    return member.ContainingType.BaseType;
//                }
//            }
//            return default;
//        }

//        public PullMemberDialogResult GetPullTargetAndMembers(
//            ISymbol selectedNodeSymbol,
//            IEnumerable<ISymbol> members,
//            Dictionary<ISymbol, Lazy<List<ISymbol>>> lazyDependentsMap)
//        {
//            if (IsCanceled)
//            {
//                return PullMemberDialogResult.CanceledResult;
//            }
//            var selectedMembers = SelectMembers ?? members.Select(m => (m, false));
//            SelectMembers = SelectMembers;
//            var target = Target ?? GetDefaultTarget(selectedNodeSymbol);
//            Target = target;
//            return new PullMemberDialogResult(SelectMembers, target);
//        }

//        public PullMemberDialogResult RestoreSelectionDialog()
//        {
//            return new PullMemberDialogResult(SelectMembers, Target);
//        }
//    }
//}
