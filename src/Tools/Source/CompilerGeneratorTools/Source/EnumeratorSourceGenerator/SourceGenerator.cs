// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace EnumeratorSourceGenerator;

[Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
internal sealed class SourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor s_invalidTarget = new(EnumDiagnosticIds.InvalidAttributeTarget, "Invalid attribute target", "Attribute 'GenerateLinkedMembersAttribute' cannot be applied to this property", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor s_noMultiplyNestedTypes = new(EnumDiagnosticIds.NoMultiplyNestedTypes, "Cannot generate code for a multiply-nested type", "Attribute 'GenerateLinkedMembersAttribute' cannot be applied to a property within a multiply-nested type", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
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

                if (symbol.ContainingType.ContainingType?.ContainingType is not null)
                {
                    // Multiply-nested types are currently not supported
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_noMultiplyNestedTypes, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
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

                var recognizedPattern = RecognizedPattern.None;
                var expression = (string?)null;
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
                        recognizedPattern = RecognizedPattern.Empty;
                    }
                    else if (unconditionalReturn is { ReturnedValue: IPropertyReferenceOperation { Property: { Name: nameof(ISymbol.Locations) } property, Instance: { } instance } }
                        && IsSameProperty(symbol, property))
                    {
                        // return ContainingType.Locations
                        recognizedPattern = RecognizedPattern.Delegating;
                        expression = instance.Syntax.ToString();
                    }
                }

                if (recognizedPattern is RecognizedPattern.None)
                {
                    // Unknown pattern
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_unknownPattern, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
                    return (null, diagnostics);
                }

                var namespaceName = symbol.ContainingNamespace.ToDisplayString();
                var containingTypeName = symbol.ContainingType.ContainingType?.Name;
                var typeName = symbol.ContainingType.Name;
                return (symbolInformation: new LinkedSymbolInformation(language, namespaceName, containingTypeName, typeName, symbol.IsSealed, recognizedPattern, expression), diagnostics);
            });

        context.RegisterSourceOutput(
            syntaxAndDiagnostic.SelectMany((pair, cancellationToken) => pair.diagnostics),
            (context, diagnostic) =>
            {
                context.ReportDiagnostic(diagnostic);
            });

        context.RegisterSourceOutput(
            syntaxAndDiagnostic.SelectMany((pair, cancellationToken) => pair.symbolInformation is null ? ImmutableArray<LinkedSymbolInformation>.Empty : ImmutableArray.Create(pair.symbolInformation)),
            (context, linkedSymbolInformation) =>
            {
                string extension;
                string sourceTextStart;
                string sourceTextBody;
                string sourceTextEnd;
                if (linkedSymbolInformation.Language == LanguageNames.CSharp)
                {
                    var sealedText = linkedSymbolInformation.IsSealed ? "sealed " : "";
                    var containingTypeStart = linkedSymbolInformation.ContainingTypeName is not null ? $"partial class {linkedSymbolInformation.ContainingTypeName} {{" : "";
                    var containingTypeEnd = linkedSymbolInformation.ContainingTypeName is not null ? "}\r\n" : "";

                    extension = "cs";
                    sourceTextStart =
                        $$"""
                        // <auto-generated/>

                        #nullable enable

                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.Symbols;

                        namespace {{linkedSymbolInformation.NamespaceName}};
                        {{containingTypeStart}}
                        partial class {{linkedSymbolInformation.TypeName}}
                        {

                        """;

                    if (linkedSymbolInformation.Pattern == RecognizedPattern.Empty)
                    {
                        sourceTextBody =
                            $$"""
                                public {{sealedText}}override int LocationsCount => SymbolLocationHelper.Empty.LocationsCount;

                                public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                                    => SymbolLocationHelper.Empty.GetCurrentLocation(slot, index);

                                public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                                    => SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex);

                                public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                                    => SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex);

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Delegating)
                    {
                        sourceTextBody =
                            $$"""
                                public {{sealedText}}override int LocationsCount => {{linkedSymbolInformation.Expression}}.LocationsCount;

                                public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                                    => {{linkedSymbolInformation.Expression}}.GetCurrentLocation(slot, index);

                                public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                                    => {{linkedSymbolInformation.Expression}}.MoveNextLocation(previousSlot, previousIndex);

                                public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                                    => {{linkedSymbolInformation.Expression}}.MoveNextLocationReversed(previousSlot, previousIndex);

                            """;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    sourceTextEnd =
                        $$"""
                        }
                        {{containingTypeEnd}}
                        """;
                }
                else
                {
                    var sealedText = linkedSymbolInformation.IsSealed ? "NotOverridable " : "";
                    var containingTypeStart = linkedSymbolInformation.ContainingTypeName is not null ? $"Partial Class {linkedSymbolInformation.ContainingTypeName}" : "";
                    var containingTypeEnd = linkedSymbolInformation.ContainingTypeName is not null ? "End Class" : "";

                    extension = "vb";
                    sourceTextStart =
                        $$"""
                        ' <auto-generated/>

                        Imports Microsoft.CodeAnalysis
                        Imports Microsoft.CodeAnalysis.Symbols

                        Namespace Global.{{linkedSymbolInformation.NamespaceName}}
                        {{containingTypeStart}}
                            Partial Class {{linkedSymbolInformation.TypeName}}


                        """;

                    if (linkedSymbolInformation.Pattern == RecognizedPattern.Empty)
                    {
                        sourceTextBody =
                            $$"""
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

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Delegating)
                    {
                        sourceTextBody =
                            $$"""
                                    Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                                        Get
                                            Return {{linkedSymbolInformation.Expression}}.LocationsCount
                                        End Get
                                    End Property

                                    Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                                        Return {{linkedSymbolInformation.Expression}}.GetCurrentLocation(slot, index)
                                    End Function

                                    Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                                        Return {{linkedSymbolInformation.Expression}}.MoveNextLocation(previousSlot, previousIndex)
                                    End Function

                                    Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                                        Return {{linkedSymbolInformation.Expression}}.MoveNextLocationReversed(previousSlot, previousIndex)
                                    End Function

                            """;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    sourceTextEnd =
                        $$"""

                            End Class
                        {{containingTypeEnd}}
                        End Namespace

                        """;
                }
                context.AddSource($"{linkedSymbolInformation.NamespaceName}.{linkedSymbolInformation.TypeName}_SymbolLocations.g.{extension}", sourceTextStart + sourceTextBody + sourceTextEnd);
            });
    }

    private static bool IsSameProperty(IPropertySymbol first, IPropertySymbol second)
    {
        return SymbolEqualityComparer.Default.Equals(GetBaseDefinition(first), GetBaseDefinition(second));
    }

    private static IPropertySymbol GetBaseDefinition(IPropertySymbol overridingProperty)
    {
        var property = overridingProperty;
        while (property.OverriddenProperty is { } overriddenProperty)
            property = overriddenProperty;

        return property;
    }

    private enum RecognizedPattern
    {
        None,
        Empty,
        Delegating,
    }

    private record class LinkedSymbolInformation(string Language, string NamespaceName, string? ContainingTypeName, string TypeName, bool IsSealed, RecognizedPattern Pattern, string? Expression);
}
