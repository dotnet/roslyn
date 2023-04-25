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
    private static readonly DiagnosticDescriptor s_noGenericTypes = new(EnumDiagnosticIds.NoGenericTypes, "Cannot generate code for a generic type", "Attribute 'GenerateLinkedMembersAttribute' cannot be applied to a property within a generic type", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

                if (symbol.ContainingType.IsGenericType || symbol.ContainingType.ContainingType?.IsGenericType == true)
                {
                    // Generic types are currently not supported
                    var invalidAttributeApplication = context.Attributes.First().ApplicationSyntaxReference!;
                    diagnostics = diagnostics.Add(Diagnostic.Create(s_noGenericTypes, Location.Create(invalidAttributeApplication.SyntaxTree, invalidAttributeApplication.Span)));
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
                    IThrowOperation? unconditionalThrow = null;
                    foreach (var child in blockChildren)
                    {
                        if (child is IReturnOperation returnOperation)
                        {
                            if (returnOperation is { IsImplicit: true, ReturnedValue: IConversionOperation { IsImplicit: true, Operand: IThrowOperation throwOperation } })
                            {
                                // Locations => throw exception;
                                unconditionalThrow = throwOperation;
                            }
                            else
                            {
                                unconditionalReturn = returnOperation;
                            }

                            break;
                        }
                        else if (child is IThrowOperation throwOperation)
                        {
                            unconditionalThrow = throwOperation;
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
                    else if (unconditionalReturn is
                    {
                        ReturnedValue: IInvocationOperation
                        {
                            TargetMethod:
                            {
                                Name: nameof(ImmutableArray.Create),
                                Arity: 1,
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
                                ContainingType:
                                {
                                    Name: nameof(ImmutableArray),
                                    Arity: 0,
                                    ContainingType: null,
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
                            Arguments:
                            [
                                {
                                    Value: { } singleLocation
                                }
                            ],
                        },
                    })
                    {
                        // return ImmutableArray.Create<Location>(location)
                        recognizedPattern = RecognizedPattern.Single;
                        expression = singleLocation.Syntax.ToString();
                    }
                    else if (unconditionalReturn is { ReturnedValue: IFieldReferenceOperation fieldReference })
                    {
                        // return _locations
                        recognizedPattern = RecognizedPattern.Many;
                        expression = fieldReference.Syntax.ToString();
                    }
                    else if (unconditionalReturn is { ReturnedValue: IPropertyReferenceOperation { Property: { Name: nameof(ISymbol.Locations) } property, Instance: { } instance } }
                        && IsSameProperty(symbol, property))
                    {
                        // return ContainingType.Locations
                        recognizedPattern = RecognizedPattern.Delegating;
                        expression = instance.Syntax.ToString();
                    }
                    else if (unconditionalThrow is { Exception: { } exception })
                    {
                        // throw <expression>
                        recognizedPattern = RecognizedPattern.Throw;
                        expression = exception.Syntax.ToString();
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

                        using System;
                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.Symbols;
                        using Roslyn.Utilities;

                        namespace {{linkedSymbolInformation.NamespaceName}};


                        """;

                    var indent = "";

                    if (linkedSymbolInformation.ContainingTypeName is not null)
                    {
                        sourceTextStart +=
                            $$"""
                            {{indent}}partial class {{linkedSymbolInformation.ContainingTypeName}}
                            {{indent}}{

                            """;

                        indent += "    ";
                    }

                    sourceTextStart +=
                        $$"""
                        {{indent}}partial class {{linkedSymbolInformation.TypeName}}
                        {{indent}}{

                        """;

                    indent += "    ";

                    if (linkedSymbolInformation.Pattern == RecognizedPattern.Empty)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}public {{sealedText}}override int LocationsCount => SymbolLocationHelper.Empty.LocationsCount;

                            {{indent}}public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                            {{indent}}    => SymbolLocationHelper.Empty.GetCurrentLocation(slot, index);

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex);

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex);

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Single)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}public {{sealedText}}override int LocationsCount => SymbolLocationHelper.Single.LocationsCount;

                            {{indent}}public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                            {{indent}}    => SymbolLocationHelper.Single.GetCurrentLocation(slot, index, {{linkedSymbolInformation.Expression}});

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Single.MoveNextLocation(previousSlot, previousIndex);

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Single.MoveNextLocationReversed(previousSlot, previousIndex);

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Many)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}public {{sealedText}}override int LocationsCount => SymbolLocationHelper.Many.LocationsCount({{linkedSymbolInformation.Expression}});

                            {{indent}}public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                            {{indent}}    => SymbolLocationHelper.Many.GetCurrentLocation(slot, index, {{linkedSymbolInformation.Expression}});

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Many.MoveNextLocation(previousSlot, previousIndex, {{linkedSymbolInformation.Expression}});

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                            {{indent}}    => SymbolLocationHelper.Many.MoveNextLocationReversed(previousSlot, previousIndex, {{linkedSymbolInformation.Expression}});

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Delegating)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}public {{sealedText}}override int LocationsCount => {{linkedSymbolInformation.Expression}}.LocationsCount;

                            {{indent}}public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                            {{indent}}    => {{linkedSymbolInformation.Expression}}.GetCurrentLocation(slot, index);

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                            {{indent}}    => {{linkedSymbolInformation.Expression}}.MoveNextLocation(previousSlot, previousIndex);

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                            {{indent}}    => {{linkedSymbolInformation.Expression}}.MoveNextLocationReversed(previousSlot, previousIndex);

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Throw)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}public {{sealedText}}override int LocationsCount => throw {{linkedSymbolInformation.Expression}};

                            {{indent}}public {{sealedText}}override Location GetCurrentLocation(int slot, int index)
                            {{indent}}    => throw {{linkedSymbolInformation.Expression}};

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocation(int previousSlot, int previousIndex)
                            {{indent}}    => throw {{linkedSymbolInformation.Expression}};

                            {{indent}}public {{sealedText}}override (bool hasNext, int nextSlot, int nextIndex) MoveNextLocationReversed(int previousSlot, int previousIndex)
                            {{indent}}    => throw {{linkedSymbolInformation.Expression}};

                            """;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    // Outdent by 1
                    indent = indent[4..];

                    sourceTextEnd =
                        $$"""
                        {{indent}}}

                        """;

                    if (linkedSymbolInformation.ContainingTypeName is not null)
                    {
                        // Outdent by 1
                        indent = indent[4..];

                        sourceTextEnd +=
                            $$"""
                            {{indent}}}

                            """;
                    }
                }
                else
                {
                    var sealedText = linkedSymbolInformation.IsSealed ? "NotOverridable " : "";

                    extension = "vb";
                    sourceTextStart =
                        $$"""
                        ' <auto-generated/>

                        Imports Microsoft.CodeAnalysis
                        Imports Microsoft.CodeAnalysis.Symbols
                        Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
                        Imports Roslyn.Utilities

                        Namespace Global.{{linkedSymbolInformation.NamespaceName}}


                        """;

                    var indent = "    ";

                    if (linkedSymbolInformation.ContainingTypeName is not null)
                    {
                        sourceTextStart +=
                            $"""
                            {indent}Partial Class {linkedSymbolInformation.ContainingTypeName}


                            """;

                        indent += "    ";
                    }

                    sourceTextStart +=
                        $"""
                        {indent}Partial Class {linkedSymbolInformation.TypeName}


                        """;

                    indent += "    ";

                    if (linkedSymbolInformation.Pattern == RecognizedPattern.Empty)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                            {{indent}}    Get
                            {{indent}}        Return SymbolLocationHelper.Empty.LocationsCount
                            {{indent}}    End Get
                            {{indent}}End Property

                            {{indent}}Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                            {{indent}}    Return SymbolLocationHelper.Empty.GetCurrentLocation(slot, index)
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Empty.MoveNextLocation(previousSlot, previousIndex)
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Empty.MoveNextLocationReversed(previousSlot, previousIndex)
                            {{indent}}End Function

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Single)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                            {{indent}}    Get
                            {{indent}}        Return SymbolLocationHelper.Single.LocationsCount
                            {{indent}}    End Get
                            {{indent}}End Property

                            {{indent}}Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                            {{indent}}    Return SymbolLocationHelper.Single.GetCurrentLocation(slot, index, {{linkedSymbolInformation.Expression}})
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Single.MoveNextLocation(previousSlot, previousIndex)
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Single.MoveNextLocationReversed(previousSlot, previousIndex)
                            {{indent}}End Function

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Many)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                            {{indent}}    Get
                            {{indent}}        Return SymbolLocationHelper.Many.LocationsCount({{linkedSymbolInformation.Expression}})
                            {{indent}}    End Get
                            {{indent}}End Property

                            {{indent}}Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                            {{indent}}    Return SymbolLocationHelper.Many.GetCurrentLocation(slot, index, {{linkedSymbolInformation.Expression}})
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Many.MoveNextLocation(previousSlot, previousIndex, {{linkedSymbolInformation.Expression}})
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return SymbolLocationHelper.Many.MoveNextLocationReversed(previousSlot, previousIndex, {{linkedSymbolInformation.Expression}})
                            {{indent}}End Function

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Delegating)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                            {{indent}}    Get
                            {{indent}}        Return {{linkedSymbolInformation.Expression}}.LocationsCount
                            {{indent}}    End Get
                            {{indent}}End Property

                            {{indent}}Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                            {{indent}}    Return {{linkedSymbolInformation.Expression}}.GetCurrentLocation(slot, index)
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return {{linkedSymbolInformation.Expression}}.MoveNextLocation(previousSlot, previousIndex)
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Return {{linkedSymbolInformation.Expression}}.MoveNextLocationReversed(previousSlot, previousIndex)
                            {{indent}}End Function

                            """;
                    }
                    else if (linkedSymbolInformation.Pattern == RecognizedPattern.Throw)
                    {
                        sourceTextBody =
                            $$"""
                            {{indent}}Public {{sealedText}}Overrides ReadOnly Property LocationsCount As Integer
                            {{indent}}    Get
                            {{indent}}        Throw {{linkedSymbolInformation.Expression}}
                            {{indent}}    End Get
                            {{indent}}End Property

                            {{indent}}Public {{sealedText}}Overrides Function GetCurrentLocation(slot As Integer, index As Integer) As Location
                            {{indent}}    Throw {{linkedSymbolInformation.Expression}}
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocation(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Throw {{linkedSymbolInformation.Expression}}
                            {{indent}}End Function

                            {{indent}}Public {{sealedText}}Overrides Function MoveNextLocationReversed(previousSlot As Integer, previousIndex As Integer) As (hasNext As Boolean, nextSlot As Integer, nextIndex As Integer)
                            {{indent}}    Throw {{linkedSymbolInformation.Expression}}
                            {{indent}}End Function

                            """;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    // Outdent by 1
                    indent = indent[4..];

                    sourceTextEnd =
                        $$"""

                        {{indent}}End Class


                        """;

                    if (linkedSymbolInformation.ContainingTypeName is not null)
                    {
                        // Outdent by 1
                        indent = indent[4..];

                        sourceTextEnd +=
                            $$"""

                            {{indent}}End Class


                            """;
                    }

                    sourceTextEnd +=
                        """
                        End Namespace

                        """;
                }
                context.AddSource($"{linkedSymbolInformation.TypeName}_SymbolLocations.g.{extension}", sourceTextStart + sourceTextBody + sourceTextEnd);
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
        Single,
        Many,
        Delegating,
        Throw,
    }

    private record class LinkedSymbolInformation(string Language, string NamespaceName, string? ContainingTypeName, string TypeName, bool IsSealed, RecognizedPattern Pattern, string? Expression);
}
