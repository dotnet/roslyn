// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class FunctionPointerCallingConventionClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(FunctionPointerUnmanagedCallingConventionSyntax));

        public override void AddClassifications(
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ClassificationOptions options,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (syntax.Parent is not FunctionPointerUnmanagedCallingConventionListSyntax list)
            {
                return;
            }

            var callingConventionSyntax = (FunctionPointerUnmanagedCallingConventionSyntax)syntax;
            if (list.CallingConventions.Count == 1 &&
                callingConventionSyntax.Name.ValueText is "Cdecl" or "Stdcall" or "Thiscall" or "Fastcall")
            {
                result.Add(new ClassifiedSpan(callingConventionSyntax.Span, ClassificationTypeNames.ClassName));
                return;
            }

            var corLibrary = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
            var type = corLibrary.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConv" + callingConventionSyntax.Name.ValueText);
            if (type is not null)
            {
                result.Add(new ClassifiedSpan(callingConventionSyntax.Name.Span, ClassificationTypeNames.ClassName));
            }
        }
    }
}
