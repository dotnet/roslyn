// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ImplementType
{
    internal enum ImplementTypeInsertionBehavior
    {
        WithOtherMembersOfTheSameKind = 0,
        AtTheEnd = 1,
    }

    internal static class ImplementTypeOptions
    {
        public static readonly PerLanguageOption<ImplementTypeInsertionBehavior> InsertionBehavior = 
            new PerLanguageOption<ImplementTypeInsertionBehavior>(
                nameof(ImplementTypeOptions), 
                nameof(InsertionBehavior), 
                defaultValue: ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
                storageLocations: new RoamingProfileStorageLocation(
                    $"TextEditor.%LANGUAGE%.{nameof(ImplementTypeOptions)}.{nameof(InsertionBehavior)}"));
    }
}