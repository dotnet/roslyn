// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static partial class Extensions
{
    internal static LinePositionSpan AddLineDelta(this LinePositionSpan span, int lineDelta)
        => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

    internal static SourceFileSpan AddLineDelta(this SourceFileSpan span, int lineDelta)
        => new(span.Path, span.Span.AddLineDelta(lineDelta));

    internal static int GetLineDelta(this LinePositionSpan oldSpan, LinePositionSpan newSpan)
        => newSpan.Start.Line - oldSpan.Start.Line;

    internal static bool Contains(this LinePositionSpan container, LinePositionSpan span)
        => span.Start >= container.Start && span.End <= container.End;

    public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
        => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));

    public static SourceSpan ToSourceSpan(this LinePositionSpan span)
        => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);

    /// <summary>
    /// True if the project supports Edit and Continue.
    /// Only depends on the language of the project and never changes.
    /// 
    /// Source generated files in the project must match the paths used by the compiler, otherwise
    /// different metadata might be emitted for file-scoped classes between compilation and EnC.
    /// </summary>
    public static bool SupportsEditAndContinue(this Project project, TraceLog? log = null)
    {
        if (project.FilePath == null)
        {
            LogReason("no file path");
            return false;
        }

        if (!project.SupportsCompilation)
        {
            LogReason("no compilation");
            return false;
        }

        if (project.Services.GetService<IEditAndContinueAnalyzer>() == null)
        {
            LogReason("no EnC service");
            return false;
        }

        if (!project.CompilationOutputInfo.HasEffectiveGeneratedFilesOutputDirectory)
        {
            LogReason("no generated files output directory");
            return false;
        }

        void LogReason(string message)
            => log?.Write($"Project '{project.GetLogDisplay()}' doesn't support EnC: {message}");

        return true;
    }

    public static string GetLogDisplay(this Project project)
        => project.FilePath != null
            ? $"'{project.FilePath}'" + (project.State.NameAndFlavor.flavor is { } flavor ? $" ('{flavor}')" : "")
            : $"'{project.Name}' ('{project.Id.DebugName}'";

    public static bool SupportsEditAndContinue(this TextDocumentState textDocumentState)
    {
        if (textDocumentState.Attributes.DesignTimeOnly)
        {
            return false;
        }

        if (!PathUtilities.IsAbsolute(textDocumentState.FilePath))
        {
            return false;
        }

        if (textDocumentState is DocumentState documentState)
        {
            if (!documentState.SupportsSyntaxTree)
            {
                return false;
            }

            // WPF design time documents are added to the Workspace by the Project System as regular documents,
            // although they are not compiled into the binary.
            if (IsWpfDesignTimeOnlyDocument(textDocumentState.FilePath, documentState.LanguageServices.Language))
            {
                return false;
            }

            // Razor generated documents are added to the Workspace by the Web Tools editor but aren't used at runtime,
            // so don't need to be considered for edit and continue.
            if (IsRazorDesignTimeOnlyDocument(textDocumentState.FilePath))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWpfDesignTimeOnlyDocument(string filePath, string language)
        => language switch
        {
            LanguageNames.CSharp => filePath.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase),
            LanguageNames.VisualBasic => filePath.EndsWith(".g.i.vb", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static bool IsRazorDesignTimeOnlyDocument(string filePath)
        => filePath.EndsWith(".razor.g.cs", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".cshtml.g.cs", StringComparison.OrdinalIgnoreCase);

    public static ManagedHotReloadDiagnostic ToHotReloadDiagnostic(this DiagnosticData data, ManagedHotReloadDiagnosticSeverity severity)
    {
        var fileSpan = data.DataLocation.MappedFileSpan;

        return new(
            data.Id,
            data.Message ?? FeaturesResources.Unknown_error_occurred,
            severity,
            fileSpan.Path ?? "",
            fileSpan.Span.ToSourceSpan());
    }

    public static bool IsSynthesized(this ISymbol symbol)
        => symbol.IsImplicitlyDeclared || symbol.IsSynthesizedAutoProperty() || symbol.IsSynthesizedParameter();

    public static bool IsSynthesizedAutoProperty(this IPropertySymbol property)
        => property is { GetMethod.IsImplicitlyDeclared: true, SetMethod.IsImplicitlyDeclared: true };

    public static bool IsSynthesizedAutoProperty(this ISymbol symbol)
        => symbol is IPropertySymbol property && property.IsSynthesizedAutoProperty();

    public static bool IsSynthesizedParameter(this ISymbol symbol)
        => symbol is IParameterSymbol parameter && parameter.IsSynthesizedParameter();

    /// <summary>
    /// True if the parameter is synthesized based on some other symbol (origin).
    /// In some cases <see cref="ISymbol.IsImplicitlyDeclared"/> of parameters of synthezied methods might be false.
    /// The parameter syntax in these cases is associated with multiple symbols.
    /// We pick one that is considered the origin and the others are considered synthesized based on it.
    /// 
    /// 1) Parameter of a record deconstructor
    ///    Considered synthesized since the primary parameter syntax represents the parameter of the primary constructor.
    ///    The deconstructor is synthesized based on the primary constructor.
    /// 2) Parameter of an Invoke method of a delegate type
    ///    The Invoke method is synthesized but its parameters represent the parameters of the delegate.
    ///    The parameters of BeginInvoke and EndInvoke are synthesized based on the Invoke method parameters.
    /// </summary>
    public static bool IsSynthesizedParameter(this IParameterSymbol parameter)
        => parameter.IsImplicitlyDeclared || parameter.ContainingSymbol.IsSynthesized() && parameter.ContainingSymbol != parameter.ContainingType.DelegateInvokeMethod;

    public static bool IsAutoProperty(this ISymbol symbol)
        => symbol is IPropertySymbol property && IsAutoProperty(property);

    public static bool IsAutoProperty(this IPropertySymbol property)
        => property.ContainingType.GetMembers().Any(static (member, property) => member is IFieldSymbol field && field.AssociatedSymbol == property, property);

    public static bool HasSynthesizedDefaultConstructor(this INamedTypeSymbol type)
        => !type.InstanceConstructors.Any(static c => !(c.Parameters is [] || c.ContainingType.IsRecord && c.IsCopyConstructor()));

    public static bool IsCopyConstructor(this ISymbol symbol)
        => symbol is IMethodSymbol { Parameters: [var parameter] } && SymbolEqualityComparer.Default.Equals(parameter.Type, symbol.ContainingType);

    public static bool HasDeconstructorSignature(this IMethodSymbol method, IMethodSymbol constructor)
        => method.Parameters.Length > 0 &&
           method.Parameters.Length == constructor.Parameters.Length &&
           method.Parameters.All(
               static (param, constructor) => param.RefKind == RefKind.Out && param.Type.Equals(constructor.Parameters[param.Ordinal].Type, SymbolEqualityComparer.Default),
               constructor);

    // TODO: use AssociatedSymbol to tie field to the parameter (see https://github.com/dotnet/roslyn/issues/69115)
    public static IFieldSymbol? GetPrimaryParameterBackingField(this IParameterSymbol parameter)
        => (IFieldSymbol?)parameter.ContainingType.GetMembers().FirstOrDefault(
            static (member, parameter) => member is IFieldSymbol field && ParsePrimaryParameterBackingFieldName(field.Name, out var paramName) && paramName == parameter.Name, parameter);

    private static bool ParsePrimaryParameterBackingFieldName(string fieldName, [NotNullWhen(true)] out string? parameterName)
    {
        int closing;
        if (fieldName.StartsWith("<") && (closing = fieldName.IndexOf(">P")) > 1)
        {
            parameterName = fieldName.Substring(1, closing - 1);
            return true;
        }

        parameterName = null;
        return false;
    }

    /// <summary>
    /// Returns a deconstructor that matches the parameters of the given <paramref name="constructor"/>, or null if there is none.
    /// </summary>
    public static IMethodSymbol? GetMatchingDeconstructor(this IMethodSymbol constructor)
        => (IMethodSymbol?)constructor.ContainingType.GetMembers(WellKnownMemberNames.DeconstructMethodName).FirstOrDefault(
            static (symbol, constructor) => symbol is IMethodSymbol method && HasDeconstructorSignature(method, constructor), constructor)?.PartialAsImplementation();

    /// <summary>
    /// Returns a partial implementation part of a partial member, or the member itself if it's not partial.
    /// </summary>
    public static ISymbol PartialAsImplementation(this ISymbol symbol)
        => PartialImplementationPart(symbol) ?? symbol;

    public static bool IsPartialDefinition(this ISymbol symbol)
        => symbol is IMethodSymbol { IsPartialDefinition: true } or IPropertySymbol { IsPartialDefinition: true };

    public static bool IsPartialImplementation(this ISymbol symbol)
        => symbol is IMethodSymbol { PartialDefinitionPart: not null } or IPropertySymbol { PartialDefinitionPart: not null };

    public static ISymbol? PartialDefinitionPart(this ISymbol symbol)
        => symbol switch
        {
            IMethodSymbol { PartialDefinitionPart: var def } => def,
            IPropertySymbol { PartialDefinitionPart: var def } => def,
            _ => null
        };

    public static ISymbol? PartialImplementationPart(this ISymbol symbol)
        => symbol switch
        {
            IMethodSymbol { PartialImplementationPart: var impl } => impl,
            IPropertySymbol { PartialImplementationPart: var impl } => impl,
            _ => null
        };

    /// <summary>
    /// Returns true if any member of the type implements an interface member explicitly.
    /// </summary>
    public static bool HasExplicitlyImplementedInterfaceMember(this INamedTypeSymbol type)
        => type.GetMembers().Any(static member => member.ExplicitInterfaceImplementations().Any());

    /// <summary>
    /// Finds a node that corresponds to the given <paramref name="node"/> in the tree rooted at <paramref name="otherRoot"/>.
    /// The trees must be identical except for trivia.
    /// </summary>
    public static SyntaxNode FindCorrespondingNodeInEquivalentTree(this SyntaxNode otherRoot, SyntaxNode node)
    {
        Contract.ThrowIfFalse(otherRoot.Parent == null);

        using var _ = ArrayBuilder<int>.GetInstance(out var childIndices);

        var parent = node.Parent;
        while (parent != null)
        {
            childIndices.Add(GetChildIndex(parent, node));
            node = parent;
            parent = parent.Parent;
        }

        var otherNode = otherRoot;
        for (var i = childIndices.Count - 1; i >= 0; i--)
        {
            otherNode = otherNode.ChildNodesAndTokens()[childIndices[i]].AsNode();
            Contract.ThrowIfNull(otherNode);
        }

        return otherNode;

        static int GetChildIndex(SyntaxNode parent, SyntaxNode node)
        {
            var i = 0;
            foreach (var child in parent.ChildNodesAndTokens())
            {
                if (child == node)
                {
                    return i;
                }

                i++;
            }

            throw ExceptionUtilities.Unreachable();
        }
    }
}
