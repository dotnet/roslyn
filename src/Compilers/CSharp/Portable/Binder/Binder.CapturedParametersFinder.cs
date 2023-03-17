// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Binder
    {
        internal sealed class CapturedParametersFinder : IdentifierUsedAsValueFinder
        {
            private readonly SynthesizedPrimaryConstructor _primaryConstructor;
            private readonly HashSet<string> _namesToCheck;
            private readonly ArrayBuilder<ParameterSymbol> _captured;

            private CapturedParametersFinder(SynthesizedPrimaryConstructor primaryConstructor, HashSet<string> namesToCheck, ArrayBuilder<ParameterSymbol> captured)
            {
                this._primaryConstructor = primaryConstructor;
                this._namesToCheck = namesToCheck;
                this._captured = captured;
            }

            public static IReadOnlyDictionary<ParameterSymbol, FieldSymbol> GetCapturedParameters(SynthesizedPrimaryConstructor primaryConstructor)
            {
                var namesToCheck = PooledHashSet<string>.GetInstance();
                addParameterNames(namesToCheck);

                if (namesToCheck.Count == 0)
                {
                    namesToCheck.Free();
                    return SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
                }

                var captured = ArrayBuilder<ParameterSymbol>.GetInstance(primaryConstructor.Parameters.Length);

                var finder = new CapturedParametersFinder(primaryConstructor, namesToCheck, captured);

                var containingType = primaryConstructor.ContainingType;

                foreach (SourceMemberMethodSymbol sourceMethod in containingType.GetMethodsPossiblyCapturingPrimaryConstructorParameters())
                {
                    Binder? bodyBinder;
                    CSharpSyntaxNode? syntaxNode;

                    getBodyBinderAndSyntax(sourceMethod, out bodyBinder, out syntaxNode);
                    if (bodyBinder is null)
                    {
                        continue;
                    }

                    Debug.Assert(syntaxNode is not null);

                    bool keepChecking = checkParameterReferencesInMethodBody(syntaxNode, bodyBinder);
                    if (!keepChecking)
                    {
                        break;
                    }
                }

                finder.Free();
                namesToCheck.Free();

                if (captured.Count == 0)
                {
                    captured.Free();
                    return SpecializedCollections.EmptyReadOnlyDictionary<ParameterSymbol, FieldSymbol>();
                }

                var result = new Dictionary<ParameterSymbol, FieldSymbol>(ReferenceEqualityComparer.Instance);

                foreach (var parameter in captured)
                {
                    result.Add(parameter,
                               new SynthesizedPrimaryConstructorParameterBackingFieldSymbol(parameter,
                                                                                            GeneratedNames.MakePrimaryConstructorParameterFieldName(parameter.Name),
                                                                                            isReadOnly: containingType.IsReadOnly));
                }

                captured.Free();
                return result;

                void addParameterNames(PooledHashSet<string> namesToCheck)
                {
                    foreach (var parameter in primaryConstructor.Parameters)
                    {
                        if (parameter.Name.Length != 0)
                        {
                            namesToCheck.Add(parameter.Name);
                        }
                    }
                }

                void getBodyBinderAndSyntax(SourceMemberMethodSymbol sourceMethod, out Binder? bodyBinder, out CSharpSyntaxNode? syntaxNode)
                {
                    bodyBinder = null;
                    syntaxNode = null;

                    bodyBinder = sourceMethod.TryGetBodyBinder();

                    if (bodyBinder is null)
                    {
                        return;
                    }

                    syntaxNode = sourceMethod.SyntaxNode;
                }

                bool checkParameterReferencesInMethodBody(CSharpSyntaxNode syntaxNode, Binder bodyBinder)
                {
                    switch (syntaxNode)
                    {
                        case ConstructorDeclarationSyntax s:
                            return finder.CheckIdentifiersInNode(s.Initializer, bodyBinder) &&
                                   finder.CheckIdentifiersInNode(s.Body, bodyBinder) &&
                                   finder.CheckIdentifiersInNode(s.ExpressionBody, bodyBinder);

                        case BaseMethodDeclarationSyntax s:
                            return finder.CheckIdentifiersInNode(s.Body, bodyBinder) &&
                                   finder.CheckIdentifiersInNode(s.ExpressionBody, bodyBinder);

                        case AccessorDeclarationSyntax s:
                            return finder.CheckIdentifiersInNode(s.Body, bodyBinder) &&
                                   finder.CheckIdentifiersInNode(s.ExpressionBody, bodyBinder);

                        case ArrowExpressionClauseSyntax s:
                            return finder.CheckIdentifiersInNode(s, bodyBinder);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(syntaxNode);
                    }
                }
            }

            protected override bool IsIdentifierOfInterest(IdentifierNameSyntax id)
            {
                return _namesToCheck.Contains(id.Identifier.ValueText);
            }

            protected override bool CheckAndClearLookupResult(Binder enclosingBinder, IdentifierNameSyntax id, LookupResult lookupResult)
            {
                if (lookupResult.IsMultiViable)
                {
                    bool? isInsideNameof = null;
                    bool detectedCapture = false;

                    foreach (var candidate in lookupResult.Symbols)
                    {
                        if (candidate is ParameterSymbol parameter && parameter.ContainingSymbol == (object)_primaryConstructor)
                        {
                            isInsideNameof ??= enclosingBinder.IsInsideNameof;

                            if (isInsideNameof.GetValueOrDefault())
                            {
                                break;
                            }
                            else if (lookupResult.IsSingleViable)
                            {
                                Debug.Assert(lookupResult.SingleSymbolOrDefault == (object)parameter);

                                // Check for left of potential color color member access 
                                if (isTypeOrValueReceiver(enclosingBinder, id, parameter.Type, out SyntaxNode? memberAccessNode, out string? memberName, out int targetMemberArity, out bool invoked))
                                {
                                    lookupResult.Clear();
                                    if (TreatAsInstanceMemberAccess(enclosingBinder, parameter.Type, memberAccessNode, memberName, targetMemberArity, invoked, lookupResult))
                                    {
                                        _captured.Add(parameter);
                                        detectedCapture = true;
                                    }

                                    // We cleared the lookupResult and the candidate list within it.
                                    // Do not attempt to continue the enclosing foreach
                                    break;
                                }
                            }

                            _captured.Add(parameter);
                            detectedCapture = true;
                        }
                    }

                    if (detectedCapture)
                    {
                        _namesToCheck.Remove(id.Identifier.ValueText);

                        if (_namesToCheck.Count == 0)
                        {
                            return false;
                        }
                    }
                }

                lookupResult.Clear();
                return true;
            }
        }
    }
}
