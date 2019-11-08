// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace IOperationGenerator
{
    internal sealed partial class IOperationClassWriter
    {
        private bool ModelHasErrors(Tree tree)
        {
            bool error = false;

            foreach (var abstractNode in tree.Types.OfType<AbstractNode>())
            {
                if (!abstractNode.Name.StartsWith("I"))
                {
                    Console.WriteLine($"All IOperation node names must start with I. {abstractNode.Name} does not.");
                    error = true;
                }

                if (!abstractNode.Name.EndsWith("Operation"))
                {
                    Console.WriteLine($"All IOperation node names must end with Operation. {abstractNode.Name} does not.");
                    error = true;
                }

                if (!_typeMap.ContainsKey(abstractNode.Base))
                {
                    Console.WriteLine($"{abstractNode.Name}'s base type is not an IOperation type.");
                    error = true;
                }

                if (abstractNode is { IsInternal: false, Obsolete: null })
                {
                    if (abstractNode.Comments?.Elements?[0].Name != "summary")
                    {
                        Console.WriteLine($"{abstractNode.Name} does not have correctly formatted comments, please ensure that there is a <summary> block for the type.");
                        error = true;
                    }

                    foreach (var prop in abstractNode.Properties)
                    {
                        if (prop.Comments?.Elements?[0].Name != "summary" && !prop.IsInternal)
                        {
                            Console.WriteLine($"{abstractNode.Name}.{prop.Name} does not have correctly formatted comments, please ensure that there is a <summary> block for the property.");
                            error = true;
                        }
                    }
                }

                if (!(abstractNode is Node node)) continue;
                if (node.SkipChildrenGeneration || node.SkipClassGeneration) continue;

                var properties = GetAllGeneratedIOperationProperties(node).Where(p => !p.IsInternal).Select(p => p.Name).ToList();

                if (properties.Count < 2)
                {
                    if (node.ChildrenOrder is string order)
                    {
                        var splitOrder = GetPropertyOrder(node);

                        if (splitOrder.Count != properties.Count || (properties.Count == 1 && properties[0] != splitOrder[0]))
                        {
                            Console.WriteLine($"{node.Name} has inconsistent ChildrenOrder and properties");
                            error = true;
                        }
                    }
                    continue;
                }


                if (node.ChildrenOrder is null)
                {
                    Console.WriteLine($"{node.Name} has more than 1 IOperation property and must declare an explicit ordering with the ChildrenOrder attribute.");
                    Console.WriteLine($"Properties: {string.Join(", ", properties)}");
                    error = true;
                    continue;
                }

                var childrenOrdered = GetPropertyOrder(node);
                foreach (var child in childrenOrdered)
                {
                    if (!properties.Remove(child))
                    {
                        Console.WriteLine($"{node.Name}'s ChildrenOrder contains unknown property {child}");
                        error = true;
                    }
                }

                foreach (var remainingProp in properties)
                {
                    Console.WriteLine($"{node.Name}'s ChildrenOrder is missing {remainingProp}");
                    error = true;
                }
            }

            return error;
        }
    }
}
