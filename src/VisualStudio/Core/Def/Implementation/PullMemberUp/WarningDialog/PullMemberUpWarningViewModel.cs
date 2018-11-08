// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    internal class PullMemberUpWarningViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<string> WarningMessageContainer { get; set; }

        internal PullMemberUpWarningViewModel(AnalysisResult analysisResult)
        {
            WarningMessageContainer = GenerateMessage(analysisResult);
        }

        private ImmutableArray<string> GenerateMessage(AnalysisResult analysisResult)
        {
            var warningMessagesBuilder = ImmutableArray.CreateBuilder<string>();
            foreach (var result in analysisResult.MembersAnalysisResults)
            {
                if (result.ChangeOriginToPublic)
                {
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_public_since_1_is_an_interface, result.Member.Name, analysisResult.Target));
                }

                if (result.ChangeOriginToNonStatic)
                {
                    warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_non_static_since_1_is_an_interface, result.Member.Name, analysisResult.Target));
                }
            }

            if (analysisResult.ChangeTargetAbstract)
            {
                warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_abstract, analysisResult.Target.Name));
            }
            return warningMessagesBuilder.ToImmutableArray();
        }
    }
}
