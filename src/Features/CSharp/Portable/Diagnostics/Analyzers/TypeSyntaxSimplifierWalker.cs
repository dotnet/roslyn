// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;

internal sealed class TypeSyntaxSimplifierWalker : CSharpSyntaxWalker, IDisposable
{
    /// <summary>
    /// This set contains the full names of types that have equivalent predefined names in the language.
    /// </summary>
    private static readonly ImmutableHashSet<string> s_predefinedTypeMetadataNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            nameof(Boolean),
            nameof(SByte),
            nameof(Byte),
            nameof(Int16),
            nameof(UInt16),
            nameof(Int32),
            nameof(UInt32),
            nameof(Int64),
            nameof(UInt64),
            nameof(Single),
            nameof(Double),
            nameof(Decimal),
            nameof(String),
            nameof(Char),
            nameof(Object));

    private readonly CSharpSimplifyTypeNamesDiagnosticAnalyzer _analyzer;
    private readonly SemanticModel _semanticModel;
    private readonly CSharpSimplifierOptions _options;
    private readonly AnalyzerOptions _analyzerOptions;
    private readonly TextSpanMutableIntervalTree? _ignoredSpans;
    private readonly CancellationToken _cancellationToken;

    private ImmutableArray<Diagnostic>.Builder? _diagnostics;

    /// <summary>
    /// Set of type and namespace names that have an alias associated with them.  i.e. if the
    /// user has <c>using X = System.DateTime</c>, then <c>DateTime</c> will be in this set.
    /// This is used so we can easily tell if we should try to simplify some identifier to an
    /// alias when we encounter it.
    /// </summary>
    private readonly PooledHashSet<string> _aliasedNames;

    public bool HasDiagnostics => _diagnostics?.Count > 0;

    public ImmutableArray<Diagnostic> Diagnostics => _diagnostics?.ToImmutable() ?? [];

    public ImmutableArray<Diagnostic>.Builder DiagnosticsBuilder
    {
        get
        {
            if (_diagnostics is null)
                Interlocked.CompareExchange(ref _diagnostics, ImmutableArray.CreateBuilder<Diagnostic>(), null);

            return _diagnostics;
        }
    }

    public TypeSyntaxSimplifierWalker(CSharpSimplifyTypeNamesDiagnosticAnalyzer analyzer, SemanticModel semanticModel, CSharpSimplifierOptions options, AnalyzerOptions analyzerOptions, TextSpanMutableIntervalTree? ignoredSpans, CancellationToken cancellationToken)
        : base(SyntaxWalkerDepth.StructuredTrivia)
    {
        _analyzer = analyzer;
        _semanticModel = semanticModel;
        _options = options;
        _analyzerOptions = analyzerOptions;
        _ignoredSpans = ignoredSpans;
        _cancellationToken = cancellationToken;

        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        _aliasedNames = PooledHashSet<string>.GetInstance();
        AddAliasedNames((CompilationUnitSyntax)root);
    }

    public void Dispose()
    {
        _aliasedNames.Free();
    }

    private void AddAliasedNames(CompilationUnitSyntax compilationUnit)
    {
        // Using `position: 0` gets all the global aliases defined in other files pulled in here.
        var scopes = _semanticModel.GetImportScopes(position: 0, _cancellationToken);
        foreach (var scope in scopes)
        {
            foreach (var alias in scope.Aliases)
            {
                var name = alias.Target.Name;
                if (!string.IsNullOrEmpty(name))
                    _aliasedNames.Add(name);
            }
        }

        foreach (var usingDirective in compilationUnit.Usings)
            AddAliasedName(usingDirective);

        foreach (var member in compilationUnit.Members)
        {
            if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                AddAliasedNames(namespaceDeclaration);
        }

        return;

        void AddAliasedName(UsingDirectiveSyntax usingDirective)
        {
            if (usingDirective.Alias is not null &&
                usingDirective.Name?.GetRightmostName() is IdentifierNameSyntax identifierName)
            {
                var identifierAlias = identifierName.Identifier.ValueText;
                if (!string.IsNullOrEmpty(identifierAlias))
                    _aliasedNames.Add(identifierAlias);
            }
        }

        void AddAliasedNames(BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            foreach (var usingDirective in namespaceDeclaration.Usings)
                AddAliasedName(usingDirective);

            foreach (var member in namespaceDeclaration.Members)
            {
                if (member is BaseNamespaceDeclarationSyntax memberNamespace)
                    AddAliasedNames(memberNamespace);
            }
        }
    }

    public override void VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        if (node.IsKind(SyntaxKind.QualifiedName) && TrySimplify(node))
        {
            // found a match. report it and stop processing.
            return;
        }

        // descend further.
        DefaultVisit(node);
    }

    public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        if (node.IsKind(SyntaxKind.AliasQualifiedName) && TrySimplify(node))
        {
            // found a match. report it and stop processing.
            return;
        }

        // descend further.
        DefaultVisit(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        if (TrySimplify(node))
        {
            // found a match. report it and stop processing.
            return;
        }

        // descend further.
        DefaultVisit(node);
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        // Always try to simplify identifiers with an 'Attribute' suffix.
        //
        // In other cases, don't bother looking at the right side of A.B or A::B. We will process those in
        // one of our other top level Visit methods (like VisitQualifiedName).
        var canTrySimplify = node.Identifier.ValueText.EndsWith("Attribute", StringComparison.Ordinal);
        if (!canTrySimplify && !node.IsRightSideOfDotOrArrowOrColonColon())
        {
            // The only possible simplifications to an unqualified identifier are replacement with an alias or
            // replacement with a predefined type.
            canTrySimplify = CanReplaceIdentifierWithAlias(node.Identifier.ValueText)
                || CanReplaceIdentifierWithPredefinedType(node.Identifier.ValueText);
        }

        if (canTrySimplify && TrySimplify(node))
        {
            // found a match. report it and stop processing.
            return;
        }

        // descend further.
        DefaultVisit(node);
        return;

        // Local functions
        bool CanReplaceIdentifierWithAlias(string identifier)
            => _aliasedNames.Contains(identifier);

        static bool CanReplaceIdentifierWithPredefinedType(string identifier)
            => s_predefinedTypeMetadataNames.Contains(identifier);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) && TrySimplify(node))
        {
            // found a match. report it and stop processing.
            return;
        }

        // descend further.
        DefaultVisit(node);
    }

    public override void VisitQualifiedCref(QualifiedCrefSyntax node)
    {
        if (_ignoredSpans?.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) ?? false)
        {
            return;
        }

        // First, just try to simplify the top-most qualified-cref alone. If we're able to do
        // this, then there's no need to process it's container.  i.e.
        //
        // if we have <see cref="A.B.C"/> and we simplify that to <see cref="C"/> there's no
        // point looking at `A.B`.
        if (node.IsKind(SyntaxKind.QualifiedCref) && TrySimplify(node))
        {
            // found a match on the qualified cref itself. report it and keep processing.
        }
        else
        {
            // couldn't simplify the qualified cref itself.  descend into the container portion
            // as that might have portions that can be simplified.
            Visit(node.Container);
        }

        // unilaterally process the member portion of the qualified cref.  These may have things
        // like parameters that could be simplified.  i.e. if we have:
        //
        //      <see cref="A.B.C(X.Y)"/>
        //
        // We can simplify both the qualified portion to just `C` and we can simplify the
        // parameter to just `Y`.
        Visit(node.Member);
    }

    /// <summary>
    /// This is the root helper that all other TrySimplify methods in this type must call
    /// through once they think there is a good chance something is simplifiable.  It does the
    /// work of actually going through the real simplification system to validate that the
    /// simplification is legal and does not affect semantics.
    /// </summary>
    private bool TrySimplify(SyntaxNode node)
    {
        if (!_analyzer.TrySimplify(_semanticModel, node, out var diagnostic, _options, _analyzerOptions, _cancellationToken))
            return false;

        DiagnosticsBuilder.Add(diagnostic);
        return true;
    }
}
