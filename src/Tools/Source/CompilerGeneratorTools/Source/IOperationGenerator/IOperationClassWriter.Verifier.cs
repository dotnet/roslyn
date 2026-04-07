// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                if (abstractNode.IsInternal && !string.IsNullOrEmpty(abstractNode.ExperimentalUrl))
                {
                    Console.WriteLine($"{abstractNode.Name} is marked as internal and experimental. Internal nodes cannot be experimental.");
                    error = true;
                }

                if (!abstractNode.IsInternal && abstractNode.Obsolete is null)
                {
                    if (abstractNode.Comments?.Elements?[0].Name != "summary")
                    {
                        Console.WriteLine($"{abstractNode.Name} does not have correctly formatted comments, please ensure that there is a <summary> block for the type.");
                        error = true;
                    }

                    foreach (var prop in abstractNode.Properties)
                    {
                        if (prop.IsInternal && !string.IsNullOrEmpty(prop.ExperimentalUrl))
                        {
                            Console.WriteLine($"{abstractNode.Name}.{prop.Name} is marked as internal and experimental. Internal properties cannot be experimental.");
                            error = true;
                        }

                        if (prop.Comments?.Elements?[0].Name != "summary" && !prop.IsInternal && !prop.IsOverride)
                        {
                            Console.WriteLine($"{abstractNode.Name}.{prop.Name} does not have correctly formatted comments, please ensure that there is a <summary> block for the property.");
                            error = true;
                        }
                    }
                }

                foreach (var prop in GetAllGeneratedIOperationProperties(abstractNode))
                {
                    if (IsImmutableArray(prop.Type, out _) && prop.Type.Contains("?"))
                    {
                        Console.WriteLine($"{abstractNode.Name}.{prop.Name} has nullable IOperation elements. This is not allowed in IOperation and will mess up Children generation.");
                        error = true;
                    }
                }

                if (abstractNode is not Node node)
                    continue;
                if (node.SkipChildrenGeneration || node.SkipClassGeneration)
                    continue;

                if (node.HasTypeText is not (null or "true" or "false"))
                {
                    Console.WriteLine($"{node.Name} has unexpected value for {nameof(Node.HasType)}: {node.HasTypeText}");
                    error = true;
                }

                if (node.HasConstantValueText is not (null or "true" or "false"))
                {
                    Console.WriteLine($"{node.Name} has unexpected value for {nameof(Node.HasConstantValue)}: {node.HasConstantValueText}");
                    error = true;
                }

                if (node.HasConstantValue && !node.HasType)
                {
                    Console.WriteLine($"{node.Name} is marked as having a constant value without having a type");
                    error = true;
                }

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
