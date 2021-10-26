// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal readonly record struct SignatureHelpOptions(
        bool HideAdvancedMembers)
    {
        public static SignatureHelpOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static SignatureHelpOptions From(OptionSet options, string language)
          => new(
              HideAdvancedMembers: options.GetOption(CompletionOptions.Metadata.HideAdvancedMembers, language));
    }
}
