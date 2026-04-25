// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public static class InjectDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "inject",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder
                .AddTypeToken(RazorExtensionsResources.InjectDirective_TypeToken_Name, RazorExtensionsResources.InjectDirective_TypeToken_Description)
                .AddMemberToken(RazorExtensionsResources.InjectDirective_MemberToken_Name, RazorExtensionsResources.InjectDirective_MemberToken_Description);

            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = RazorExtensionsResources.InjectDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder, bool considerNullabilityEnforcement)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive);
        builder.Features.Add(new Pass());
        builder.AddTargetExtension(new InjectTargetExtension(considerNullabilityEnforcement));
        return builder;
    }

    internal sealed class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        // Runs after the @model and @namespace directives
        public override int Order => 10;

        protected override void ExecuteCore(
            RazorCodeDocument codeDocument,
            DocumentIntermediateNode documentNode,
            CancellationToken cancellationToken)
        {
            if (documentNode.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
               documentNode.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
            {
                // Not a MVC file. Skip.
                return;
            }

            var visitor = new Visitor();
            visitor.Visit(documentNode);
            var modelType = ModelDirective.GetModelType(documentNode).Content;

            var properties = new HashSet<string>(StringComparer.Ordinal);

            for (var i = visitor.Directives.Count - 1; i >= 0; i--)
            {
                var directive = visitor.Directives[i];
                var tokens = directive.Children.OfType<DirectiveTokenIntermediateNode>().ToArray();
                var isMalformed = directive is MalformedDirectiveIntermediateNode;

                var hasType = tokens.Length > 0 && !string.IsNullOrWhiteSpace(tokens[0].Content);
                Debug.Assert(hasType || isMalformed);
                var typeName = hasType ? tokens[0].Content : string.Empty;
                var typeSpan = hasType ? tokens[0].Source : directive.Source?.GetZeroWidthEndSpan();

                var hasMemberName = tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1].Content);
                Debug.Assert(hasMemberName || isMalformed);
                var memberName = hasMemberName ? tokens[1].Content : null;
                var memberSpan = hasMemberName ? tokens[1].Source : null;

                if (hasMemberName && !properties.Add(memberName!))
                {
                    continue;
                }

                const string tModel = "<TModel>";
                if (typeName.EndsWith(tModel, StringComparison.Ordinal))
                {
                    typeName = typeName[..^tModel.Length] + "<" + modelType + ">";
                    if (typeSpan.HasValue)
                    {
                        typeSpan = new SourceSpan(typeSpan.Value.FilePath, typeSpan.Value.AbsoluteIndex, typeSpan.Value.LineIndex, typeSpan.Value.CharacterIndex, typeSpan.Value.Length - tModel.Length, typeSpan.Value.LineCount, typeSpan.Value.EndCharacterIndex - tModel.Length);
                    }
                }

                var injectNode = new InjectIntermediateNode()
                {
                    TypeName = typeName,
                    MemberName = memberName,
                    TypeSource = typeSpan,
                    MemberSource = memberSpan,
                    IsMalformed = isMalformed
                };

                visitor.Class!.Children.Add(injectNode);
            }
        }
    }

    private class Visitor : IntermediateNodeWalker
    {
        public ClassDeclarationIntermediateNode? Class { get; private set; }

        public IList<IntermediateNode> Directives { get; } = [];

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            Class ??= node;

            base.VisitClassDeclaration(node);
        }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            if (node.Directive == Directive)
            {
                Directives.Add(node);
            }
        }

        public override void VisitMalformedDirective(MalformedDirectiveIntermediateNode node)
        {
            if (node.Directive == Directive)
            {
                Directives.Add(node);
            }
        }
    }
}
