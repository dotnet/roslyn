// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockStructureOptions
    {
        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCommentsAndPreprocessorRegions), defaultValue: false);

        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForDeclarationLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForDeclarationLevelConstructs), defaultValue: true);

        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForCodeLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowBlockStructureGuidesForCodeLevelConstructs), defaultValue: true);

        public static readonly PerLanguageOption<bool> ShowOutliningForCommentsAndPreprocessorRegions = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCommentsAndPreprocessorRegions), defaultValue: true);

        public static readonly PerLanguageOption<bool> ShowOutliningForDeclarationLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForDeclarationLevelConstructs), defaultValue: true);

        public static readonly PerLanguageOption<bool> ShowOutliningForCodeLevelConstructs = new PerLanguageOption<bool>(
            nameof(BlockStructureOptions), nameof(ShowOutliningForCodeLevelConstructs), defaultValue: true);
    }
}