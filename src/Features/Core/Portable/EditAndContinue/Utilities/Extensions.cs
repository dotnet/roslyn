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
    extension(LinePositionSpan span)
    {
        internal LinePositionSpan AddLineDelta(int lineDelta)
        => new(new LinePosition(span.Start.Line + lineDelta, span.Start.Character), new LinePosition(span.End.Line + lineDelta, span.End.Character));

        public SourceSpan ToSourceSpan()
            => new(span.Start.Line, span.Start.Character, span.End.Line, span.End.Character);
    }

    extension(SourceFileSpan span)
    {
        internal SourceFileSpan AddLineDelta(int lineDelta)
        => new(span.Path, span.Span.AddLineDelta(lineDelta));
    }

    extension(LinePositionSpan oldSpan)
    {
        internal int GetLineDelta(LinePositionSpan newSpan)
        => newSpan.Start.Line - oldSpan.Start.Line;
    }

    extension(LinePositionSpan container)
    {
        internal bool Contains(LinePositionSpan span)
        => span.Start >= container.Start && span.End <= container.End;
    }

    extension(SourceSpan span)
    {
        public LinePositionSpan ToLinePositionSpan()
        => new(new(span.StartLine, span.StartColumn), new(span.EndLine, span.EndColumn));
    }

    extension(Project project)
    {
        /// <summary>
        /// True if the project supports Edit and Continue.
        /// Only depends on the language of the project and never changes.
        /// 
        /// Source generated files in the project must match the paths used by the compiler, otherwise
        /// different metadata might be emitted for file-scoped classes between compilation and EnC.
        /// </summary>
        public bool SupportsEditAndContinue(TraceLog? log = null)
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

        public string GetLogDisplay()
            => project.FilePath != null
                ? $"'{project.FilePath}'" + (project.State.NameAndFlavor.flavor is { } flavor ? $" ('{flavor}')" : "")
                : $"'{project.Name}' ('{project.Id.DebugName}'";
    }

    extension(TextDocumentState textDocumentState)
    {
        public bool SupportsEditAndContinue()
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

    extension(DiagnosticData data)
    {
        public ManagedHotReloadDiagnostic ToHotReloadDiagnostic(ManagedHotReloadDiagnosticSeverity severity)
        {
            var fileSpan = data.DataLocation.MappedFileSpan;

            return new(
                data.Id,
                data.Message ?? FeaturesResources.Unknown_error_occurred,
                severity,
                fileSpan.Path ?? "",
                fileSpan.Span.ToSourceSpan());
        }
    }

    extension(ISymbol symbol)
    {
        public bool IsSynthesized()
        => symbol.IsImplicitlyDeclared || symbol.IsSynthesizedAutoProperty() || symbol.IsSynthesizedParameter();

        public bool IsSynthesizedAutoProperty()
            => symbol is IPropertySymbol property && property.IsSynthesizedAutoProperty();

        public bool IsSynthesizedParameter()
            => symbol is IParameterSymbol parameter && parameter.IsSynthesizedParameter();

        public bool IsAutoProperty()
            => symbol is IPropertySymbol property && IsAutoProperty(property);

        public bool IsCopyConstructor()
            => symbol is IMethodSymbol { Parameters: [var parameter] } && SymbolEqualityComparer.Default.Equals(parameter.Type, symbol.ContainingType);

        /// <summary>
        /// Returns a partial implementation part of a partial member, or the member itself if it's not partial.
        /// </summary>
        public ISymbol PartialAsImplementation()
            => PartialImplementationPart(symbol) ?? symbol;

        public bool IsPartialDefinition()
            => symbol is IMethodSymbol { IsPartialDefinition: true } or IPropertySymbol { IsPartialDefinition: true };

        public bool IsPartialImplementation()
            => symbol is IMethodSymbol { PartialDefinitionPart: not null } or IPropertySymbol { PartialDefinitionPart: not null };

        public ISymbol? PartialDefinitionPart()
            => symbol switch
            {
                IMethodSymbol { PartialDefinitionPart: var def } => def,
                IPropertySymbol { PartialDefinitionPart: var def } => def,
                _ => null
            };

        public ISymbol? PartialImplementationPart()
            => symbol switch
            {
                IMethodSymbol { PartialImplementationPart: var impl } => impl,
                IPropertySymbol { PartialImplementationPart: var impl } => impl,
                _ => null
            };
    }

    extension(IPropertySymbol property)
    {
        public bool IsSynthesizedAutoProperty()
        => property is { GetMethod.IsImplicitlyDeclared: true, SetMethod.IsImplicitlyDeclared: true };

        public bool IsAutoProperty()
            => property.ContainingType.GetMembers().Any(static (member, property) => member is IFieldSymbol field && field.AssociatedSymbol == property, property);
    }

    extension(IParameterSymbol parameter)
    {
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
        public bool IsSynthesizedParameter()
            => parameter.IsImplicitlyDeclared || parameter.ContainingSymbol.IsSynthesized() && parameter.ContainingSymbol != parameter.ContainingType.DelegateInvokeMethod;

        // TODO: use AssociatedSymbol to tie field to the parameter (see https://github.com/dotnet/roslyn/issues/69115)
        public IFieldSymbol? GetPrimaryParameterBackingField()
            => (IFieldSymbol?)parameter.ContainingType.GetMembers().FirstOrDefault(
                static (member, parameter) => member is IFieldSymbol field && ParsePrimaryParameterBackingFieldName(field.Name, out var paramName) && paramName == parameter.Name, parameter);
    }

    extension(INamedTypeSymbol type)
    {
        public bool HasSynthesizedDefaultConstructor()
        => !type.InstanceConstructors.Any(static c => !(c.Parameters is [] || c.ContainingType.IsRecord && c.IsCopyConstructor()));

        /// <summary>
        /// Returns true if any member of the type implements an interface member explicitly.
        /// </summary>
        public bool HasExplicitlyImplementedInterfaceMember()
            => type.GetMembers().Any(static member => member.ExplicitInterfaceImplementations().Any());
    }

    extension(IMethodSymbol method)
    {
        public bool HasDeconstructorSignature(IMethodSymbol constructor)
        => method.Parameters.Length > 0 &&
           method.Parameters.Length == constructor.Parameters.Length &&
           method.Parameters.All(
               static (param, constructor) => param.RefKind == RefKind.Out && param.Type.Equals(constructor.Parameters[param.Ordinal].Type, SymbolEqualityComparer.Default),
               constructor);
    }

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

    extension(IMethodSymbol constructor)
    {
        /// <summary>
        /// Returns a deconstructor that matches the parameters of the given <paramref name="constructor"/>, or null if there is none.
        /// </summary>
        public IMethodSymbol? GetMatchingDeconstructor()
            => (IMethodSymbol?)constructor.ContainingType.GetMembers(WellKnownMemberNames.DeconstructMethodName).FirstOrDefault(
                static (symbol, constructor) => symbol is IMethodSymbol method && HasDeconstructorSignature(method, constructor), constructor)?.PartialAsImplementation();
    }
}
