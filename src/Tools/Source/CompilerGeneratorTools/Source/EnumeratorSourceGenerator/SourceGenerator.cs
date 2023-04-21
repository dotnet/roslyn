// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace EnumeratorSourceGenerator;

[Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
internal sealed class SourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor s_invalidTarget = new(EnumDiagnosticIds.InvalidAttributeTarget, "Invalid attribute target", "Attribute 'GenerateLinkedMembersAttribute' cannot be applied to this property", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor s_noNestedTypes = new(EnumDiagnosticIds.NoNestedTypes, "Cannot generate code for a nested type", "Attribute 'GenerateLinkedMembersAttribute' cannot be applied to a property within a nested type", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor s_unknownPattern = new(EnumDiagnosticIds.UnknownPattern, "The implementation pattern for this property was not recognized", "Attribute 'GenerateLinkedMembersAttribute' can only be applied to a property with a recognized implementation pattern", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxAndDiagnostic = context.SyntaxProvider.ForAttributeWithMetadataName<(LinkedSymbolInformation? symbolInformation, ImmutableList<Diagnostic> diagnostics)>(
            "Roslyn.Utilities.GenerateLinkedMembersAttribute",
            predicate: (syntax, cancellationToken) => true,
            transform: (context, cancellationToken) =>
            {
                var diagnostics = ImmutableList<Diagnostic>.Empty;

                if (context.TargetSymbol is not IPropertySymbol { Name: "Locations", GetMethod: { } getMethod } symbol)
                {
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_invalidTarget, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
                    return (null, diagnostics);
                }

                if (symbol.ContainingType.ContainingType is not null)
                {
                    // Nested types are currently not supported
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_noNestedTypes, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
                    return (null, diagnostics);
                }

                var language = context.SemanticModel.Language;
                var getMethodSyntax = getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                var operation = context.SemanticModel.GetOperation(getMethodSyntax, cancellationToken);
                if (operation is null && language == LanguageNames.VisualBasic)
                {
                    // Move from GetAccessorStatement up to the containing GetAccessorBlock
                    operation = context.SemanticModel.GetOperation(getMethodSyntax.Parent!, cancellationToken);
                }

                if (operation is IMethodBodyOperation methodBodyOperation)
                {
                    operation = methodBodyOperation.BlockBody;
                }

                RecognizedPattern? recognizedPatternOpt = null;
                if (operation is IBlockOperation { ChildOperations: { } blockChildren })
                {
                    IReturnOperation? unconditionalReturn = null;
                    foreach (var child in blockChildren)
                    {
                        if (child is IReturnOperation returnOperation)
                        {
                            unconditionalReturn = returnOperation;
                            break;
                        }
                        else if (child is IVariableDeclarationGroupOperation)
                        {
                            // VB creates these implicitly. Just skip it.
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (unconditionalReturn is
                        {
                            ReturnedValue: IFieldReferenceOperation
                            {
                                Field:
                                {
                                    Name: nameof(ImmutableArray<Location>.Empty),
                                    ContainingType:
                                    {
                                        Name: nameof(ImmutableArray<Location>),
                                        Arity: 1,
                                        ContainingType: null,
                                        TypeArguments:
                                        [
                                            {
                                                Name: nameof(Location),
                                                ContainingType: null,
                                                ContainingNamespace:
                                                {
                                                    Name: nameof(Microsoft.CodeAnalysis),
                                                    ContainingNamespace:
                                                    {
                                                        Name: nameof(Microsoft),
                                                        ContainingNamespace.IsGlobalNamespace: true,
                                                    },
                                                },
                                            }
                                        ],
                                    },
                                    ContainingNamespace:
                                    {
                                        Name: nameof(System.Collections.Immutable),
                                        ContainingNamespace:
                                        {
                                            Name: nameof(System.Collections),
                                            ContainingNamespace:
                                            {
                                                Name: nameof(System),
                                                ContainingNamespace.IsGlobalNamespace: true,
                                            },
                                        },
                                    },
                                },
                            },
                        })
                    {
                        // return ImmutableArray<Location>.Empty
                        recognizedPatternOpt = RecognizedPattern.Empty;
                    }
                }

                if (recognizedPatternOpt is not { } recognizedPattern)
                {
                    // Nested types are currently not supported
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_unknownPattern, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
                    return (null, diagnostics);
                }

                var namespaceName = symbol.ContainingNamespace.ToDisplayString();
                var typeName = symbol.ContainingType.Name;
                return (symbolInformation: new LinkedSymbolInformation(language, namespaceName, typeName, symbol.IsSealed, recognizedPattern), diagnostics);
            });

        context.RegisterSourceOutput(
            syntaxAndDiagnostic.SelectMany((pair, cancellationToken) => pair.diagnostics),
            (context, diagnostic) =>
            {
                context.ReportDiagnostic(diagnostic);
            });

        context.RegisterSourceOutput(
            syntaxAndDiagnostic.SelectMany((pair, cancellationToken) => pair.Item1 is null ? ImmutableArray<LinkedSymbolInformation>.Empty : ImmutableArray.Create(pair.Item1)),
            (context, linkedSymbolInformation) =>
            {
                string extension;
                string sourceText;
                if (linkedSymbolInformation.Language == LanguageNames.CSharp)
                {
                    var sealedText = linkedSymbolInformation.IsSealed ? "sealed " : "";

                    extension = "cs";
                    sourceText =
                        $$"""
                        // <auto-generated/>

                        #nullable enable

                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.Symbols;

                        namespace {{linkedSymbolInformation.NamespaceName}};

                        partial class {{linkedSymbolInformation.TypeName}}
                        {
                            public {{sealedText}}override int LocationsCount => SymbolLocationHelper.Empty.LocationsCount;

                            public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                                => SymbolLocationHelper.Empty.GetCurrentLocation(slot, index);

                            public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                                => SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex);

                            public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                                => SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex);
                        }

                        """;
                }
                else
                {
                    var sealedText = linkedSymbolInformation.IsSealed ? "NotOverridable " : "";

                    extension = "vb";
                    sourceText =
                        $$"""
                        ' <auto-generated/>

                        Imports Microsoft.CodeAnalysis
                        Imports Microsoft.CodeAnalysis.Symbols

                        Namespace Global.{{linkedSymbolInformation.NamespaceName}}

                            Partial Class {{linkedSymbolInformation.TypeName}}

                                Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                                    Get
                                        Return SymbolLocationHelper.Empty.LocationsCount
                                    End Get
                                End Property

                                Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                                    Return SymbolLocationHelper.Empty.GetCurrentLocation(slot, index)
                                End Function

                                Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                                    Return SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex)
                                End Function

                                Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                                    Return SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex)
                                End Function

                            End Class

                        End Namespace

                        """;
                }
                context.AddSource($"{linkedSymbolInformation.NamespaceName}.{linkedSymbolInformation.TypeName}_SymbolLocations.g.{extension}", sourceText);
            });
    }

    private enum RecognizedPattern
    {
        Empty,
    }

    private record class LinkedSymbolInformation(string Language, string NamespaceName, string TypeName, bool IsSealed, RecognizedPattern Pattern);
}
