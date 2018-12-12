// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.WarningDialog
{
    internal class PullMemberUpWarningViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<string> WarningMessageContainer { get; set; }

        internal PullMemberUpWarningViewModel(PullMembersUpAnalysisResult analysisResult)
        {
            WarningMessageContainer = GenerateMessage(analysisResult);
        }

        private ImmutableArray<string> GenerateMessage(PullMembersUpAnalysisResult analysisResult)
        {
            var warningMessagesBuilder = ImmutableArray.CreateBuilder<string>();

            if (!analysisResult.Destination.IsAbstract &&
                analysisResult.MemberAnalysisResults.Any(result => result.ChangeDestinationToAbstract))
            {
                Logger.Log(FunctionId.PullMembersUpWarning_ChangeTargetToAbstract);
                warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_abstract, analysisResult.Destination.ToDisplayString()));
            }

            foreach (var result in analysisResult.MemberAnalysisResults)
            {
                if (result.ChangeOriginalToPublic)
                {
                    Logger.Log(FunctionId.PullMembersUpWarning_ChangeOriginToPublic);
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_public_since_1_is_an_interface, result.Member.ToDisplayString(), analysisResult.Destination.ToDisplayString()));
                }

                if (result.ChangeOriginalToNonStatic)
                {
                    Logger.Log(FunctionId.PullMembersUpWarning_ChangeOriginToNonStatic);
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_non_static_since_1_is_an_interface, result.Member.ToDisplayString(), analysisResult.Destination.ToDisplayString()));
                }
            }

            return warningMessagesBuilder.ToImmutableArray();
        }
    }
}
