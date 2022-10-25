// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SignatureHelp;

internal static class SignatureHelpOptionsStorage
{
    public static SignatureHelpOptions GetSignatureHelpOptions(this IGlobalOptionService globalOptions, string language)
      => new()
      {
          HideAdvancedMembers = globalOptions.GetOption(CompletionOptionsStorage.HideAdvancedMembers, language)
      };
}
