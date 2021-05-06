// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
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
            .WhereAsArray(constructor =>
            {
                if (constructor.Parameters.Length == args.Count)
                {
                    return true;
                }
                else
                {
                    var optionalCount = GetCountOfOptionalParameters(constructor);

                    return optionalCount + args.Count == constructor.Parameters.Length;
                }
            })
            .WhereAsArray(constructor =>
            {
                var argToParameterDictionary = ArgumentToParameterMapping(args, constructor.Parameters, model);
                return GetCountOfOptionalParameters(constructor) + argToParameterDictionary.Count == constructor.Parameters.Length;

            }).Length;

            if (count == 0)
            {
                return true;
            }

            return false;
        }

        protected override bool IsNameOf(SyntaxNode node) => node.Parent is InvocationExpressionSyntax invocation && invocation.IsNameOfInvocation();

        private static Dictionary<ArgumentSyntax, IParameterSymbol> ArgumentToParameterMapping(SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IParameterSymbol> parameters, SemanticModel model)
        {
            var argToParameterDictionary = new Dictionary<ArgumentSyntax, IParameterSymbol>();
            foreach (var argument in arguments)
            {
                var typeInfo = model.GetTypeInfo(argument.Expression);
                foreach (var parameter in parameters)
                {
                    if (parameter.Type.Equals(typeInfo.ConvertedType))
                    {
                        argToParameterDictionary.Add(argument, parameter);
                    }
                }
            }

            return argToParameterDictionary;
        }

        private static int GetCountOfOptionalParameters(IMethodSymbol constructor)
        {
            var optionalCount = 0;
            foreach (var parameter in constructor.Parameters)
            {
                if (parameter.IsOptional)
                {
                    optionalCount++;
                }
            }

            return optionalCount;
        }
    }
}
