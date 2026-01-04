// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class UninitializedNonNullableFieldDiagnosticInfo : DiagnosticInfoWithSymbols
{
    private readonly ErrorCode _messageCode;

    internal UninitializedNonNullableFieldDiagnosticInfo(ErrorCode messageCode, ErrorCode identityCode, object[] args, ImmutableArray<Location> additionalLocations)
        : base(identityCode, args, ImmutableArray<Symbol>.Empty)
    {
        _messageCode = messageCode;
        AdditionalLocations = additionalLocations.IsDefaultOrEmpty ? SpecializedCollections.EmptyReadOnlyList<Location>() : additionalLocations;
    }

    public override IReadOnlyList<Location> AdditionalLocations { get; }

    internal new ErrorCode Code => (ErrorCode)base.Code;

    public override string GetMessage(IFormatProvider? formatProvider = null)
    {
        string message = MessageProvider.LoadMessage((int)_messageCode, formatProvider as CultureInfo);
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return Arguments.Length == 0 ? message : string.Format(formatProvider, message, GetArgumentsToUse(formatProvider));
    }

    protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
    {
        return new UninitializedNonNullableFieldDiagnosticInfo(this, severity);
    }

    private UninitializedNonNullableFieldDiagnosticInfo(UninitializedNonNullableFieldDiagnosticInfo original, DiagnosticSeverity severity)
        : base(original, severity)
    {
        _messageCode = original._messageCode;
        AdditionalLocations = original.AdditionalLocations;
    }
}
