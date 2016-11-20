// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ImplementType
{
    internal static class ImplementTypeOptions
    {
        public static readonly PerLanguageOption<bool> Keep_properties_events_and_methods_grouped_when_implementing_types = new PerLanguageOption<bool>(
            nameof(ImplementTypeOptions), 
            nameof(Keep_properties_events_and_methods_grouped_when_implementing_types), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.{nameof(Keep_properties_events_and_methods_grouped_when_implementing_types)}"));
    }
}
