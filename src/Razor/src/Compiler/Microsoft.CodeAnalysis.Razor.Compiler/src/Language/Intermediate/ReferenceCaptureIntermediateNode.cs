// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ReferenceCaptureIntermediateNode : IntermediateNode
{
    private const string DefaultFieldTypeName = $"global::{ComponentsApi.ElementReference.FullTypeName}";
    private const string DefaultTypeName = $"global::System.Action<{DefaultFieldTypeName}>";

    private string? _componentCaptureTypeName;
    private string _fieldTypeName;
    private string _typeName;

    [MemberNotNullWhen(true, nameof(ComponentCaptureTypeName))]
    public bool IsComponentCapture { get; }

    public IntermediateToken IdentifierToken { get; }

    /// <remarks>
    /// This is not <c>global::</c>-prefixed, for that consider using <see cref="FieldTypeName"/> instead.
    /// </remarks>
    public string? ComponentCaptureTypeName => _componentCaptureTypeName;

    public string FieldTypeName => _fieldTypeName;

    public string TypeName => _typeName;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public ReferenceCaptureIntermediateNode(IntermediateToken identifierToken)
    {
        ArgHelper.ThrowIfNull(identifierToken);

        IdentifierToken = identifierToken;
        Source = IdentifierToken.Source;

        _fieldTypeName = DefaultFieldTypeName;
        _typeName = DefaultTypeName;
    }

    public ReferenceCaptureIntermediateNode(IntermediateToken identifierToken, string componentCaptureTypeName)
        : this(identifierToken)
    {
        IsComponentCapture = true;
        UpdateComponentCaptureTypeName(componentCaptureTypeName);
    }

    public void UpdateComponentCaptureTypeName(string componentCaptureTypeName)
    {
        ArgHelper.ThrowIfNullOrEmpty(componentCaptureTypeName);

        Debug.Assert(IsComponentCapture);

        _componentCaptureTypeName = componentCaptureTypeName;
        _fieldTypeName = TypeNameHelper.GetGloballyQualifiedNameIfNeeded(componentCaptureTypeName);
        _typeName = $"global::System.Action<{_fieldTypeName}>";
    }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitReferenceCapture(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(IdentifierToken.Content);

        formatter.WriteProperty(nameof(IdentifierToken), IdentifierToken.Content);
        formatter.WriteProperty(nameof(ComponentCaptureTypeName), ComponentCaptureTypeName);
    }
}
