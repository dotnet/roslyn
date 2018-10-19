// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal class OrganizeUsingsSet
    {
        public bool IsRemoveUnusedImportEnabled { get; }
        public bool IsSortImportsEnabled { get; }

        public OrganizeUsingsSet(OptionSet optionSet, string language)
        {
            IsRemoveUnusedImportEnabled = optionSet.GetOption(CodeCleanupOptions.RemoveUnusedImports, language);
            IsSortImportsEnabled = optionSet.GetOption(CodeCleanupOptions.SortImports, language);
        }

        public OrganizeUsingsSet(bool isRemoveUnusedImportEnabled, bool isSortImportsEnabled)
        {
            IsRemoveUnusedImportEnabled = isRemoveUnusedImportEnabled;
            IsSortImportsEnabled = IsSortImportsEnabled;
        }
    }
}
