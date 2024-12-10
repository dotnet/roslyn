// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static InitializeParameterHelpersCore;

[ExportLanguageService(typeof(IInitializeParameterService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpInitializeParameterService() : AbstractInitializerParameterService<StatementSyntax>
{
    public override SyntaxNode GetBody(SyntaxNode methodNode)
        => InitializeParameterHelpers.GetBody(methodNode);

    protected override SyntaxNode? TryGetLastStatement(IBlockOperation? blockStatement)
        => InitializeParameterHelpers.TryGetLastStatement(blockStatement);

    protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode? statementToAddAfter, StatementSyntax statement)
        => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfter, statement);

    protected override bool TryUpdateTupleAssignment(
        IBlockOperation? blockStatement,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        SyntaxEditor editor)
    {
        if (blockStatement is null)
            return false;

        foreach (var (tupleLeft, tupleRight) in TryGetAssignmentExpressions(blockStatement))
        {
            if (tupleLeft.Syntax is TupleExpressionSyntax tupleLeftSyntax &&
                tupleRight.Syntax is TupleExpressionSyntax tupleRightSyntax)
            {
                var generator = editor.Generator;
                foreach (var (sibling, before) in GetSiblingParameters(parameter))
                {
                    if (TryFindSiblingAssignment(tupleLeft, tupleRight, sibling, out var index))
                    {
                        // If we found assignment to a parameter before us, then add after that.
                        var insertionPosition = before ? index + 1 : index;

                        var left = (ArgumentSyntax)generator.Argument(generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName(fieldOrProperty.Name)));
                        var right = (ArgumentSyntax)generator.Argument(generator.IdentifierName(parameter.Name));

                        editor.ReplaceNode(
                            tupleLeftSyntax,
                            tupleLeftSyntax.WithArguments(tupleLeftSyntax.Arguments.Insert(insertionPosition, left)));
                        editor.ReplaceNode(
                            tupleRightSyntax,
                            tupleRightSyntax.WithArguments(tupleRightSyntax.Arguments.Insert(insertionPosition, right)));

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryFindSiblingAssignment(
        ITupleOperation tupleLeft, ITupleOperation tupleRight, IParameterSymbol sibling, out int index)
    {
        for (int i = 0, n = tupleLeft.Elements.Length; i < n; i++)
        {
            // rhs tuple has to directly reference the sibling parameter.  lhs has to be a reference to a field/prop in this type.

            if (tupleRight.Elements[i] is IParameterReferenceOperation parameterReference && sibling.Equals(parameterReference.Parameter) &&
                IsFieldOrPropertyReference(tupleLeft.Elements[i], sibling.ContainingType, out _))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static IEnumerable<(ITupleOperation targetTuple, ITupleOperation valueTuple)> TryGetAssignmentExpressions(IBlockOperation blockOperation)
    {
        foreach (var operation in blockOperation.Operations)
        {
            if (TryGetPartsOfTupleAssignmentOperation(operation, out var targetTuple, out var valueTuple))
                yield return (targetTuple, valueTuple);
        }
    }
}
