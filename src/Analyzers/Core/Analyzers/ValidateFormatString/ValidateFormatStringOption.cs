// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ValidateFormatString
{
    internal class ValidateFormatStringOption
    {
        public static PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls =
            new PerLanguageOption2<bool>(
                nameof(ValidateFormatStringOption),
                nameof(ReportInvalidPlaceholdersInStringDotFormatCalls),
                defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls"));
    }
}
