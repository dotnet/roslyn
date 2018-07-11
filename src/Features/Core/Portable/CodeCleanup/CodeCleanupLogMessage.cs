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
                m["AreCodeCleanupRulesConfigured"] = docOptions.GetOption(CodeCleanupOptions.AreCodeCleanupRulesConfigured);
                m["AddAccessibilityModifiers"] = docOptions.GetOption(CodeCleanupOptions.AddAccessibilityModifiers);
                m["AddRemoveBracesForSingleLineControlStatements"] = docOptions.GetOption(CodeCleanupOptions.AddRemoveBracesForSingleLineControlStatements);
                m["ApplyExpressionBlockBodyPreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyExpressionBlockBodyPreferences);
                m["ApplyLanguageFrameworkTypePreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyLanguageFrameworkTypePreferences);
                m["ApplyImplicitExplicitTypePreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyImplicitExplicitTypePreferences);
                m["ApplyInlineOutVariablePreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyInlineOutVariablePreferences);
                m["ApplyObjectCollectionInitializationPreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyObjectCollectionInitializationPreferences);
                m["ApplyThisQualificationPreferences"] = docOptions.GetOption(CodeCleanupOptions.ApplyThisQualificationPreferences);
                m["MakePrivateFieldReadonlyWhenPossible"] = docOptions.GetOption(CodeCleanupOptions.MakePrivateFieldReadonlyWhenPossible);
                m["RemoveUnnecessaryCasts"] = docOptions.GetOption(CodeCleanupOptions.RemoveUnnecessaryCasts);
                m["RemoveUnusedImports"] = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedImports);
                m["RemoveUnusedVariables"] = docOptions.GetOption(CodeCleanupOptions.RemoveUnusedVariables);
                m["SortAccessibilityModifiers"] = docOptions.GetOption(CodeCleanupOptions.SortAccessibilityModifiers);
                m["SortImports"] = docOptions.GetOption(CodeCleanupOptions.SortImports);
                });
        }
    }
}
