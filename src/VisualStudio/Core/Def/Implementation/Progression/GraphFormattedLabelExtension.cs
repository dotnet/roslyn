// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class GraphFormattedLabelExtension : IGraphFormattedLabel
    {
        public string Description(GraphObject graphObject, string graphCommandDefinitionIdentifier)
        {
            return GetStringPropertyForGraphObject(
                graphObject,
                graphCommandDefinitionIdentifier,
                RoslynGraphProperties.Description,
                RoslynGraphProperties.DescriptionWithContainingSymbol);
        }

        public string Label(GraphObject graphObject, string graphCommandDefinitionIdentifier)
        {
            return GetStringPropertyForGraphObject(
                graphObject,
                graphCommandDefinitionIdentifier,
                RoslynGraphProperties.FormattedLabelWithoutContainingSymbol,
                RoslynGraphProperties.FormattedLabelWithContainingSymbol);
        }

        private string GetStringPropertyForGraphObject(GraphObject graphObject, string graphCommandDefinitionIdentifier, GraphProperty propertyWithoutContainingSymbol, GraphProperty propertyWithContainingSymbol)
        {
            var graphNode = graphObject as GraphNode;

            if (graphNode != null)
            {
                if (graphCommandDefinitionIdentifier != GraphCommandDefinition.Contains.Id)
                {
                    return graphNode.GetValue<string>(propertyWithContainingSymbol);
                }
                else
                {
                    return graphNode.GetValue<string>(propertyWithoutContainingSymbol);
                }
            }

            return null;
        }
    }
}
