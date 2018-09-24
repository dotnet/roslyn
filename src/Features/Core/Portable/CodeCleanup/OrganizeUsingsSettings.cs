// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal class OrganizeUsingsSet
    {
        public bool IsRemoveUnusedImportEnabled { get; private set; }
        public bool IsSortImportsEnabled { get; private set; }

        public bool IsEnabled { get { return IsRemoveUnusedImportEnabled || IsSortImportsEnabled; } }

        public OrganizeUsingsSet(DocumentOptionSet docOptions)
        {
            IsRemoveUnusedImportEnabled = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports);
            IsSortImportsEnabled = docOptions.GetOption(CodeCleanupOptions.SortImports);
        }

        public OrganizeUsingsSet(bool isRemoveUnusedImportEnabled, bool isSortImportsEnabled)
        {
            IsRemoveUnusedImportEnabled = isRemoveUnusedImportEnabled;
            IsSortImportsEnabled = IsSortImportsEnabled;
        }
    }
}
