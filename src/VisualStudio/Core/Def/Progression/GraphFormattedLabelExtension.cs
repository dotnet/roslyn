// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

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

    private static string GetStringPropertyForGraphObject(GraphObject graphObject, string graphCommandDefinitionIdentifier, GraphProperty propertyWithoutContainingSymbol, GraphProperty propertyWithContainingSymbol)
    {

        if (graphObject is GraphNode graphNode)
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
