// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.WarningDialog
{
    internal class MoveMembersWarningViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<string> WarningMessageContainer { get; set; }
        public string ProblemsListViewAutomationText => ServicesVSResources.Review_Changes;

        public MoveMembersWarningViewModel(ISymbol destinationSymbol, ImmutableArray<MemberAnalysisResult> analysisResults)
        {
            WarningMessageContainer = GenerateMessage(destinationSymbol, analysisResults);
        }

        private ImmutableArray<string> GenerateMessage(ISymbol destinationSymbol, ImmutableArray<MemberAnalysisResult> analysisResults)
        {
            var warningMessagesBuilder = ImmutableArray.CreateBuilder<string>();

            if (!destinationSymbol.IsAbstract &&
                analysisResults.Any(result => result.ChangeDestinationTypeToAbstract))
            {
                Logger.Log(FunctionId.PullMembersUpWarning_ChangeTargetToAbstract);
                warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_abstract, destinationSymbol.Name));
            }

            foreach (var result in analysisResults)
            {
                if (result.ChangeOriginalToPublic)
                {
                    Logger.Log(FunctionId.PullMembersUpWarning_ChangeOriginToPublic);
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_public, result.Member.Name));
                }

                if (result.ChangeOriginalToNonStatic)
                {
                    Logger.Log(FunctionId.PullMembersUpWarning_ChangeOriginToNonStatic);
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_non_static, result.Member.Name));
                }
            }

            return warningMessagesBuilder.ToImmutableArray();
        }
    }
}
