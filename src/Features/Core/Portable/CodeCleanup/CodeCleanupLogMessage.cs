// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal static class CodeCleanupLogMessage
    {
        public static KeyValueLogMessage Create(DocumentOptionSet docOptions)
        {
            return KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[nameof(CodeCleanupOptions.CodeCleanupInfoBarShown)] = docOptions.GetOption(CodeCleanupOptions.CodeCleanupInfoBarShown);
                m[nameof(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain)] = docOptions.GetOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain);
                m[nameof(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting)] = docOptions.GetOption(CodeCleanupOptions.PerformAdditionalCodeCleanupDuringFormatting);
                m[nameof(CodeCleanupOptions.RemoveUnusedImports)] = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports);
                m[nameof(CodeCleanupOptions.SortImports)] = docOptions.GetOption(CodeCleanupOptions.SortImports);
                m[nameof(CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements)] = docOptions.GetOption(CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements);
                m[nameof(CodeCleanupOptions.AddAccessibilityModifiers)] = docOptions.GetOption(CodeCleanupOptions.AddAccessibilityModifiers);
                m[nameof(CodeCleanupOptions.SortAccessibilityModifiers)] = docOptions.GetOption(CodeCleanupOptions.SortAccessibilityModifiers);
                m[nameof(CodeCleanupOptions.ApplyExpressionBlockBodyPreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyExpressionBlockBodyPreferences);
                m[nameof(CodeCleanupOptions.ApplyImplicitExplicitTypePreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyImplicitExplicitTypePreferences);
                m[nameof(CodeCleanupOptions.ApplyInlineOutVariablePreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyInlineOutVariablePreferences);
                m[nameof(CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences);
                m[nameof(CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences);
                m[nameof(CodeCleanupOptions.ApplyThisQualificationPreferences)] = docOptions.GetOption(CodeCleanupOptions.ApplyThisQualificationPreferences);
                m[nameof(CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible)] = docOptions.GetOption(CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible);
                m[nameof(CodeCleanupOptions.RemoveUnnecessaryCasts)] = docOptions.GetOption(CodeCleanupOptions.RemoveUnnecessaryCasts);
                m[nameof(CodeCleanupOptions.RemoveUnusedVariables)] = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedVariables);
                });
        }
    }
}
