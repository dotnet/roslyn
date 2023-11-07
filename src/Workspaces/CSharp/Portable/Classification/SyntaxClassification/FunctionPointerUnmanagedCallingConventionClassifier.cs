// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

internal sealed class FunctionPointerUnmanagedCallingConventionClassifier : AbstractSyntaxClassifier
{
    public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(typeof(FunctionPointerUnmanagedCallingConventionSyntax));

    public override void AddClassifications(
        SyntaxNode syntax,
        TextSpan textSpan,
        SemanticModel semanticModel,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        var callingConvention = (FunctionPointerUnmanagedCallingConventionSyntax)syntax;
        var name = callingConvention.Name.ValueText;
        if (!IsLegalCallingConvention(name))
            return;

        result.Add(new(ClassificationTypeNames.ClassName, callingConvention.Name.Span));
        return;

        bool IsLegalCallingConvention(string name)
        {
            if (name is "Cdecl" or "Stdcall" or "Thiscall" or "Fastcall")
                return true;

            var fullName = $"System.Runtime.CompilerServices.CallConv{name}";
            var type = semanticModel.Compilation.GetBestTypeByMetadataName(fullName);
            return type != null;
        }
    }
}
