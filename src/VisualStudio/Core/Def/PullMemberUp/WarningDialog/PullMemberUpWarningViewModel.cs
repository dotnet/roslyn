// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.WarningDialog;

internal class PullMemberUpWarningViewModel : AbstractNotifyPropertyChanged
{
    public ImmutableArray<string> WarningMessageContainer { get; set; }
    public string ProblemsListViewAutomationText => ServicesVSResources.Review_Changes;

    public PullMemberUpWarningViewModel(PullMembersUpOptions options)
        => WarningMessageContainer = GenerateMessage(options);

    private static ImmutableArray<string> GenerateMessage(PullMembersUpOptions options)
    {
        var warningMessagesBuilder = ImmutableArray.CreateBuilder<string>();

        if (!options.Destination.IsAbstract &&
            options.MemberAnalysisResults.Any(static result => result.ChangeDestinationTypeToAbstract))
        {
            Logger.Log(FunctionId.PullMembersUpWarning_ChangeTargetToAbstract);
            warningMessagesBuilder.Add(string.Format(ServicesVSResources._0_will_be_changed_to_abstract, options.Destination.Name));
        }

        foreach (var result in options.MemberAnalysisResults)
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
