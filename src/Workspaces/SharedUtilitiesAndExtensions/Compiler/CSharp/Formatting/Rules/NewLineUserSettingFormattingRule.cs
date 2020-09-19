// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class NewLineUserSettingFormattingRule : BaseFormattingRule
    {
        private readonly CachedOptions _options;

        public NewLineUserSettingFormattingRule()
            : this(new CachedOptions(null))
        {
        }

        private NewLineUserSettingFormattingRule(CachedOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(AnalyzerConfigOptions options)
        {
            var cachedOptions = new CachedOptions(options);

            if (cachedOptions == _options)
            {
                return this;
            }

            return new NewLineUserSettingFormattingRule(cachedOptions);
        }

        private static bool IsControlBlock(SyntaxNode node)
        {
            RoslynDebug.Assert(node != null);

            if (node.Kind() == SyntaxKind.SwitchStatement)
            {
                return true;
            }

            var parentKind = node.Parent?.Kind();

            switch (parentKind.GetValueOrDefault())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ElseClause:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.TryStatement:
                case SyntaxKind.CatchClause:
                case SyntaxKind.FinallyClause:
                case SyntaxKind.LockStatement:
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                case SyntaxKind.SwitchSection:
                case SyntaxKind.FixedStatement:
                case SyntaxKind.UnsafeStatement:
                    return true;
                default:
                    return false;
            }
        }

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            RoslynDebug.AssertNotNull(currentToken.Parent);

            var operation = nextOperation.Invoke(in previousToken, in currentToken);

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken)
                && currentToken.IsKind(SyntaxKind.ElseKeyword)
                && previousToken.Parent!.Parent == currentToken.Parent.Parent)
            {
                if (!_options.NewLineForElse)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * catch in the try catch context
            if (currentToken.IsKind(SyntaxKind.CatchKeyword))
            {
                if (!_options.NewLineForCatch)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * finally
            if (currentToken.IsKind(SyntaxKind.FinallyKeyword))
            {
                if (!_options.NewLineForFinally)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { in the type declaration context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (!_options.NewLinesForBracesInTypes)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Anonymous object creation
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (!_options.NewLinesForBracesInAnonymousTypes)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // new { - Object Initialization
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                if (!_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent is MemberDeclarationSyntax)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? _options.NewLinesForBracesInProperties
                    : _options.NewLinesForBracesInMethods;

                if (!option)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (!_options.NewLinesForBracesInAccessors)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                if (!_options.NewLinesForBracesInAnonymousMethods)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the local function context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null && currentTokenParentParent.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                if (!_options.NewLinesForBracesInMethods)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the Lambda context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentTokenParentParent != null &&
               (currentTokenParentParent.IsKind(SyntaxKind.SimpleLambdaExpression) || currentTokenParentParent.IsKind(SyntaxKind.ParenthesizedLambdaExpression)))
            {
                if (!_options.NewLinesForBracesInLambdaExpressionBody)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the switch expression context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.SwitchExpression))
            {
                if (!_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            // * { - in the control statement context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (!_options.NewLinesForBracesInControlBlocks)
                {
                    operation = CreateAdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
                }
            }

            return operation;
        }

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            RoslynDebug.AssertNotNull(currentToken.Parent);

            var operation = nextOperation.Invoke(in previousToken, in currentToken);

            // else condition is actually handled in the GetAdjustSpacesOperation()

            // For Object Initialization Expression
            if (previousToken.Kind() == SyntaxKind.CommaToken && previousToken.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                if (_options.NewLineForMembersInObjectInit)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    // we never force it to move up unless it is already on same line
                    return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                }
            }

            // For Anonymous Object Creation Expression
            if (previousToken.Kind() == SyntaxKind.CommaToken && previousToken.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                if (_options.NewLineForMembersInAnonymousTypes)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    // we never force it to move up unless it is already on same line
                    return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                }
            }

            // } else in the if else context
            if (previousToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.IsKind(SyntaxKind.ElseKeyword))
            {
                if (_options.NewLineForElse
                    || previousToken.Parent!.Parent != currentToken.Parent.Parent)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * catch in the try catch context
            if (currentToken.Kind() == SyntaxKind.CatchKeyword)
            {
                if (_options.NewLineForCatch)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * Finally
            if (currentToken.Kind() == SyntaxKind.FinallyKeyword)
            {
                if (_options.NewLineForFinally)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the type declaration context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && (currentToken.Parent is BaseTypeDeclarationSyntax || currentToken.Parent is NamespaceDeclarationSyntax))
            {
                if (_options.NewLinesForBracesInTypes)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new { - Anonymous object creation
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentToken.Parent.Kind() == SyntaxKind.AnonymousObjectCreationExpression)
            {
                if (_options.NewLinesForBracesInAnonymousTypes)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // new MyObject { - Object Initialization
            // new List<int> { - Collection Initialization
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken &&
                (currentToken.Parent.Kind() == SyntaxKind.ObjectInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.CollectionInitializerExpression))
            {
                if (_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // Array Initialization Expression
            // int[] arr = new int[] {
            //             new[] {
            //             { - Implicit Array
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) &&
                (currentToken.Parent.Kind() == SyntaxKind.ArrayInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ImplicitArrayCreationExpression))
            {
                return null;
            }

            var currentTokenParentParent = currentToken.Parent.Parent;

            // * { - in the member declaration context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent is MemberDeclarationSyntax)
            {
                var option = currentTokenParentParent is BasePropertyDeclarationSyntax
                    ? _options.NewLinesForBracesInProperties
                    : _options.NewLinesForBracesInMethods;

                if (option)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the property accessor context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent is AccessorDeclarationSyntax)
            {
                if (_options.NewLinesForBracesInAccessors)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the anonymous Method context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent.Kind() == SyntaxKind.AnonymousMethodExpression)
            {
                if (_options.NewLinesForBracesInAnonymousMethods)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the local function context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null && currentTokenParentParent.Kind() == SyntaxKind.LocalFunctionStatement)
            {
                if (_options.NewLinesForBracesInMethods)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the simple Lambda context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && currentTokenParentParent != null &&
               (currentTokenParentParent.Kind() == SyntaxKind.SimpleLambdaExpression || currentTokenParentParent.Kind() == SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (_options.NewLinesForBracesInLambdaExpressionBody)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the switch expression context
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.SwitchExpression))
            {
                if (_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // * { - in the control statement context
            if (currentToken.Kind() == SyntaxKind.OpenBraceToken && IsControlBlock(currentToken.Parent))
            {
                if (_options.NewLinesForBracesInControlBlocks)
                {
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }
                else
                {
                    return null;
                }
            }

            // Wrapping - Leave statements on same line (false): 
            // Insert a newline between the previous statement and this one.
            // ; *
            if (previousToken.Kind() == SyntaxKind.SemicolonToken
                && (previousToken.Parent is StatementSyntax && !previousToken.Parent.IsKind(SyntaxKind.ForStatement))
                && !_options.WrappingKeepStatementsOnSingleLine)
            {
                return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
            }

            return operation;
        }

        private readonly struct CachedOptions : IEquatable<CachedOptions>
        {
            public readonly bool NewLineForMembersInObjectInit;
            public readonly bool NewLineForMembersInAnonymousTypes;
            public readonly bool NewLineForElse;
            public readonly bool NewLineForCatch;
            public readonly bool NewLineForFinally;
            public readonly bool NewLinesForBracesInTypes;
            public readonly bool NewLinesForBracesInAnonymousTypes;
            public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;
            public readonly bool NewLinesForBracesInProperties;
            public readonly bool NewLinesForBracesInMethods;
            public readonly bool NewLinesForBracesInAccessors;
            public readonly bool NewLinesForBracesInAnonymousMethods;
            public readonly bool NewLinesForBracesInLambdaExpressionBody;
            public readonly bool NewLinesForBracesInControlBlocks;
            public readonly bool WrappingKeepStatementsOnSingleLine;

            public CachedOptions(AnalyzerConfigOptions? options)
            {
                NewLineForMembersInObjectInit = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLineForMembersInObjectInit);
                NewLineForMembersInAnonymousTypes = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes);
                NewLineForElse = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLineForElse);
                NewLineForCatch = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLineForCatch);
                NewLineForFinally = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLineForFinally);
                NewLinesForBracesInTypes = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInTypes);
                NewLinesForBracesInAnonymousTypes = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes);
                NewLinesForBracesInObjectCollectionArrayInitializers = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers);
                NewLinesForBracesInProperties = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInProperties);
                NewLinesForBracesInMethods = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInMethods);
                NewLinesForBracesInAccessors = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInAccessors);
                NewLinesForBracesInAnonymousMethods = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods);
                NewLinesForBracesInLambdaExpressionBody = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody);
                NewLinesForBracesInControlBlocks = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInControlBlocks);
                WrappingKeepStatementsOnSingleLine = GetOptionOrDefault(options, CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine);
            }

            public static bool operator ==(CachedOptions left, CachedOptions right)
                => left.Equals(right);

            public static bool operator !=(CachedOptions left, CachedOptions right)
                => !(left == right);

            private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, Option2<T> option)
            {
                if (options is null)
                    return option.DefaultValue;

                return options.GetOption(option);
            }

            public override bool Equals(object? obj)
                => obj is CachedOptions options && Equals(options);

            public bool Equals(CachedOptions other)
            {
                return NewLineForMembersInObjectInit == other.NewLineForMembersInObjectInit
                    && NewLineForMembersInAnonymousTypes == other.NewLineForMembersInAnonymousTypes
                    && NewLineForElse == other.NewLineForElse
                    && NewLineForCatch == other.NewLineForCatch
                    && NewLineForFinally == other.NewLineForFinally
                    && NewLinesForBracesInTypes == other.NewLinesForBracesInTypes
                    && NewLinesForBracesInAnonymousTypes == other.NewLinesForBracesInAnonymousTypes
                    && NewLinesForBracesInObjectCollectionArrayInitializers == other.NewLinesForBracesInObjectCollectionArrayInitializers
                    && NewLinesForBracesInProperties == other.NewLinesForBracesInProperties
                    && NewLinesForBracesInMethods == other.NewLinesForBracesInMethods
                    && NewLinesForBracesInAccessors == other.NewLinesForBracesInAccessors
                    && NewLinesForBracesInAnonymousMethods == other.NewLinesForBracesInAnonymousMethods
                    && NewLinesForBracesInLambdaExpressionBody == other.NewLinesForBracesInLambdaExpressionBody
                    && NewLinesForBracesInControlBlocks == other.NewLinesForBracesInControlBlocks
                    && WrappingKeepStatementsOnSingleLine == other.WrappingKeepStatementsOnSingleLine;
            }

            public override int GetHashCode()
            {
                var hashCode = 0;
                hashCode = (hashCode << 1) + (NewLineForMembersInObjectInit ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLineForMembersInAnonymousTypes ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLineForElse ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLineForCatch ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLineForFinally ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInTypes ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInAnonymousTypes ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInObjectCollectionArrayInitializers ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInProperties ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInMethods ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInAccessors ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInAnonymousMethods ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInLambdaExpressionBody ? 1 : 0);
                hashCode = (hashCode << 1) + (NewLinesForBracesInControlBlocks ? 1 : 0);
                hashCode = (hashCode << 1) + (WrappingKeepStatementsOnSingleLine ? 1 : 0);
                return hashCode;
            }
        }
    }
}
