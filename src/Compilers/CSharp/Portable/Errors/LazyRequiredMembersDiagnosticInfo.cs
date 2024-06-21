// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class LazyRequiredMembersDiagnosticsInfo : LazyDiagnosticInfo
{
    private readonly MethodSymbol _constructor;
    private readonly ImmutableArray<BoundExpression> _initializers;
    private readonly SyntaxNode _syntax;

    public LazyRequiredMembersDiagnosticsInfo(MethodSymbol constructor, ImmutableArray<BoundExpression> initializers, SyntaxNode syntaxNode)
    {
        _constructor = constructor;
        _initializers = initializers;
        _syntax = syntaxNode;
    }

    private LazyRequiredMembersDiagnosticsInfo(LazyRequiredMembersDiagnosticsInfo original, DiagnosticSeverity severity) : base(original, severity)
    {
        _constructor = original._constructor;
        _initializers = original._initializers;
        _syntax = original._syntax;
    }

    protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
    {
        return new LazyRequiredMembersDiagnosticsInfo(this, severity);
    }

    protected override DiagnosticInfo? ResolveInfo()
    {
        var diagnosticsBag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
        Binder.CheckRequiredMembersInObjectInitializer(_constructor, _initializers, _syntax, diagnosticsBag);

        var diagnostic = diagnosticsBag.HasAnyErrors() ? new CSDiagnosticInfo(ErrorCode.ERR_RequiredMembersAttributeErrors) : null;
        diagnosticsBag.Free();
        return diagnostic;
    }
}
