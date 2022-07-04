// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int ShowOutliningForDeclarationLevelConstructs
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs); }
            set { SetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForDeclarationLevelConstructs, value); }
        }

        public int ShowOutliningForCodeLevelConstructs
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs); }
            set { SetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForCodeLevelConstructs, value); }
        }

        public int ShowOutliningForCommentsAndPreprocessorRegions
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions); }
            set { SetBooleanOption(BlockStructureOptionsStorage.ShowOutliningForCommentsAndPreprocessorRegions, value); }
        }

        public int CollapseRegionsWhenCollapsingToDefinitions
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions); }
            set { SetBooleanOption(BlockStructureOptionsStorage.CollapseRegionsWhenCollapsingToDefinitions, value); }
        }

        public int ShowBlockStructureGuidesForDeclarationLevelConstructs
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs); }
            set { SetBooleanOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForDeclarationLevelConstructs, value); }
        }

        public int ShowBlockStructureGuidesForCodeLevelConstructs
        {
            get { return GetBooleanOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs); }
            set { SetBooleanOption(BlockStructureOptionsStorage.ShowBlockStructureGuidesForCodeLevelConstructs, value); }
        }
    }
}
