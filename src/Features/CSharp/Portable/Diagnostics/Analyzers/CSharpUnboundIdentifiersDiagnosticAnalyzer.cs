// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.AddImport;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUnboundIdentifiersDiagnosticAnalyzer : UnboundIdentifiersDiagnosticAnalyzerBase<SyntaxKind, SimpleNameSyntax, QualifiedNameSyntax, IncompleteMemberSyntax, LambdaExpressionSyntax>
    {
        private readonly LocalizableString _nameNotInContextMessageFormat =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.The_name_0_does_not_exist_in_the_current_context), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private readonly LocalizableString _constructorOverloadResolutionFailureMessageFormat =
            new LocalizableResourceString(nameof(CSharpFeaturesResources._0_does_not_contain_a_constructor_that_takes_that_many_arguments), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.IncompleteMember, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression);

        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => s_kindsOfInterest;

        protected override DiagnosticDescriptor DiagnosticDescriptor => GetDiagnosticDescriptor(IDEDiagnosticIds.UnboundIdentifierId, _nameNotInContextMessageFormat);

        protected override DiagnosticDescriptor DiagnosticDescriptor2 => GetDiagnosticDescriptor(IDEDiagnosticIds.UnboundConstructorId, _constructorOverloadResolutionFailureMessageFormat);

        protected override bool ConstructorDoesNotExist(SyntaxNode node, SymbolInfo info, SemanticModel model)
        {
            var argList = (node.Parent as ObjectCreationExpressionSyntax)?.ArgumentList?.Arguments;
            if (!argList.HasValue)
            {
                return false;
            }

            var args = argList.Value;

            var constructors = (info.Symbol?.OriginalDefinition as INamedTypeSymbol)?.Constructors;
            if (!constructors.HasValue)
            {
                return false;
            }

            var count = constructors.Value
            .WhereAsArray(constructor => constructor.Parameters.Length == args.Count)
            .WhereAsArray(constructor =>
            {
                for (var i = 0; i < constructor.Parameters.Length; i++)
                {
                    var typeInfo = model.GetTypeInfo(args[i].Expression);
                    if (!constructor.Parameters[i].Type.Equals(typeInfo.ConvertedType))
                    {
                        return false;
                    }
                }

                return true;
            }).Length;

            if (count == 0)
            {
                return true;
            }

            return false;
        }

        protected override bool IsNameOf(SyntaxNode node) => node.Parent is InvocationExpressionSyntax invocation && invocation.IsNameOfInvocation();
    }
}
