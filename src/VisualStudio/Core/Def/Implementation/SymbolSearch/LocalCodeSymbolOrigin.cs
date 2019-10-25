// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class LocalCodeSymbolOrigin : ISymbolOrigin
    {
        public string OrderedDefinitionId => PredefinedSymbolOrigins.LocalCode;

        public ImageId DisplayIcon => default;

        public string DescriptionText => "These symbols come from the opened solution";

        public string DisplayName { get; }

        internal LocalCodeSymbolOrigin(string solutionName)
        {
            this.DisplayName = solutionName;
        }
    }
}
