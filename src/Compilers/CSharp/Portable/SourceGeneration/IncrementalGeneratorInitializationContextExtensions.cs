// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;

internal static partial class IncrementalGeneratorInitializationContextExtensions
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };

    /// <summary>
    /// Returns all syntax nodes of type <typeparamref name="T"/> if that node has an attribute on it that could
    /// possibly bind to the provided <paramref name="fullyQualifiedMetadataName"/>. <paramref
    /// name="fullyQualifiedMetadataName"/> should be the fully-qualified metadata name of the attribute, including the
    /// <c>Attribute</c> suffix.  For example <c>System.CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// <para>This provider understands <see langword="using"/> aliases and will find matches even when the attribute
    /// references an alias name.  For example, given:</para>
    /// <code>
    /// using XAttribute = System.CLSCompliantAttribute;
    /// [X]
    /// class C { }
    /// </code>
    /// Then
    /// <c>context.SyntaxProvider.CreateSyntaxProviderForAttribute&lt;ClassDeclarationSyntax&gt;(typeof(CLSCompliantAttribute).FullName)</c>
    /// will find the <c>C</c> class.
    /// </summary>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(this IncrementalGeneratorInitializationContext context, string fullyQualifiedMetadataName)
        where T : SyntaxNode
    {
        var metadataName = fullyQualifiedMetadataName.IndexOf('+') >= 0
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var simpleTypeName = metadataName.UnmangledTypeName;
        var nodesWithAttributesMatchingSimpleName = context.SyntaxProvider.CreateSyntaxProviderForAttribute<T>(simpleTypeName);

        var collectedNodes = nodesWithAttributesMatchingSimpleName.Collect().WithTrackingName("collectedNodes_ForAttributeWithMetadataName");

        var compilationAndCollectedNodesProvider = collectedNodes
            .Combine(context.CompilationProvider)
            .WithTrackingName("compilationAndCollectedNodes_ForAttributeWithMetadataName");

        return compilationAndCollectedNodesProvider.SelectMany((tuple, cancellationToken) =>
        {
            var nodes = tuple.Left;
            var compilation = tuple.Right;

            var result = ArrayBuilder<T>.GetInstance();
            try
            {
                foreach (var group in nodes.GroupBy(node => node.SyntaxTree))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var syntaxTree = group.Key;
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    foreach (var nodeInTree in group)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var symbol = semanticModel.GetDeclaredSymbol(nodeInTree, cancellationToken);
                        if (HasMatchingAttribute(symbol, fullyQualifiedMetadataName))
                            result.Add(nodeInTree);
                    }
                }

                return result.ToImmutable();
            }
            finally
            {
                result.Free();
            }
        }).WithTrackingName("result_ForAttributeWithMetadataName");
    }

    private static bool HasMatchingAttribute(ISymbol? symbol, string fullyQualifiedMetadataName)
    {
        if (symbol is not null)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is null)
                    continue;

                if (attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat) == fullyQualifiedMetadataName)
                    return true;
            }
        }

        return false;
    }
}
