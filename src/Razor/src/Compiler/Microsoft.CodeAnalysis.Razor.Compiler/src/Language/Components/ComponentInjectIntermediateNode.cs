// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentInjectIntermediateNode : ExtensionIntermediateNode
{
    private static readonly ImmutableArray<string> s_injectedPropertyModifiers =
    [
        $"[global::{ComponentsApi.InjectAttribute.FullTypeName}]",
        "private" // Encapsulation is the default
    ];

    public ComponentInjectIntermediateNode(string typeName, string memberName, SourceSpan? typeSpan, SourceSpan? memberSpan, bool isMalformed)
    {
        TypeName = typeName;
        MemberName = memberName;
        TypeSpan = typeSpan;
        MemberSpan = memberSpan;
        IsMalformed = isMalformed;
     }

    public string TypeName { get; }

    public string MemberName { get; }

    public SourceSpan? TypeSpan { get; }

    public SourceSpan? MemberSpan { get; }

    public bool IsMalformed { get; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        AcceptExtensionNode(this, visitor);
    }

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (TypeName == string.Empty && TypeSpan.HasValue && !context.Options.DesignTime)
        {
            // if we don't even have a type name, just emit an empty mapped region so that intellisense still works
            using (context.BuildEnhancedLinePragma(TypeSpan.Value))
            {
            }
        }
        else
        {
            var memberName = MemberName ?? "Member_" + DefaultTagHelperTargetExtension.GetDeterministicId(context);

            if (!context.Options.DesignTime || !IsMalformed)
            {
                context.CodeWriter.WriteAutoPropertyDeclaration(
                    s_injectedPropertyModifiers,
                    TypeName,
                    memberName,
                    TypeSpan,
                    MemberSpan,
                    context,
                    defaultValue: true);
            }
        }
    }
}
