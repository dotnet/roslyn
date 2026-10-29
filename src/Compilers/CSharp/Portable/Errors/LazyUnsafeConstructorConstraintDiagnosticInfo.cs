// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class LazyUnsafeConstructorConstraintDiagnosticInfo : LazyDiagnosticInfo
{
    private readonly Binder _binder;
    private readonly TypeParameterSymbol _typeParameter;
    private readonly NamedTypeSymbol _typeArgument;
    private readonly Symbol _targetSymbol;

    internal LazyUnsafeConstructorConstraintDiagnosticInfo(Binder binder, TypeParameterSymbol typeParameter, NamedTypeSymbol typeArgument, Symbol targetSymbol)
    {
        _binder = binder;
        _typeParameter = typeParameter;
        _typeArgument = typeArgument;
        _targetSymbol = targetSymbol;
    }

    private LazyUnsafeConstructorConstraintDiagnosticInfo(LazyUnsafeConstructorConstraintDiagnosticInfo original, DiagnosticSeverity severity) : base(original, severity)
    {
        _binder = original._binder;
        _typeParameter = original._typeParameter;
        _typeArgument = original._typeArgument;
        _targetSymbol = original._targetSymbol;
    }

    protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
    {
        return new LazyUnsafeConstructorConstraintDiagnosticInfo(this, severity);
    }

    protected override DiagnosticInfo? ResolveInfo()
    {
        return _binder.GetUnsafeConstructorConstraintDiagnosticInfo(_typeParameter, _typeArgument, _targetSymbol);
    }
}
