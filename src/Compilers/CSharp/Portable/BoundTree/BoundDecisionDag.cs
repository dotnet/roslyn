// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDecisionDag
    {
        private HashSet<LabelSymbol> _reachableLabels;

        public HashSet<LabelSymbol> ReachableLabels
        {
            get
            {
                if (_reachableLabels == null)
                {
                    // compute the set of reachable labels
                    var result = new HashSet<LabelSymbol>();
                    processDag(this);
                    _reachableLabels = result;

                    // simulate the dispatch (setting pattern variables and jumping to labels) to
                    // all reachable switch labels
                    void processDag(BoundDecisionDag dag)
                    {
                        switch (dag)
                        {
                            case BoundEvaluationPoint x:
                                processDag(x.Next);
                                return;
                            case BoundDecisionPoint x:
                                processDag(x.WhenTrue);
                                processDag(x.WhenFalse);
                                return;
                            case BoundWhereClause x:
                                processDag(x.WhenTrue);
                                processDag(x.WhenFalse);
                                return;
                            case BoundDecision x:
                                result.Add(x.Label);
                                return;
                            case null:
                                return;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(dag.Kind);
                        }
                    }
                }

                return _reachableLabels;
            }
        }
    }

}
