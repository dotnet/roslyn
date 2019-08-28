// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BinaryOperatorOverloadResolutionResult
    {
        public readonly ArrayBuilder<BinaryOperatorAnalysisResult> Results;

        private BinaryOperatorOverloadResolutionResult()
        {
            this.Results = new ArrayBuilder<BinaryOperatorAnalysisResult>(10);
        }

        public bool AnyValid()
        {
            foreach (var result in Results)
            {
                if (result.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        public bool SingleValid()
        {
            bool oneValid = false;
            foreach (var result in Results)
            {
                if (result.IsValid)
                {
                    if (oneValid)
                    {
                        return false;
                    }

                    oneValid = true;
                }
            }

            return oneValid;
        }

        public BinaryOperatorAnalysisResult Best
        {
            get
            {
                BinaryOperatorAnalysisResult best = default(BinaryOperatorAnalysisResult);
                foreach (var result in Results)
                {
                    if (result.IsValid)
                    {
                        if (best.IsValid)
                        {
                            // More than one best applicable method
                            return default(BinaryOperatorAnalysisResult);
                        }
                        best = result;
                    }
                }

                return best;
            }
        }

#if DEBUG
        public string Dump()
        {
            if (Results.Count == 0)
            {
                return "Overload resolution failed because there were no candidate operators.";
            }

            var sb = new StringBuilder();
            if (this.Best.HasValue)
            {
                sb.AppendLine("Overload resolution succeeded and chose " + this.Best.Signature.ToString());
            }
            else if (CountKind(OperatorAnalysisResultKind.Applicable) > 1)
            {
                sb.AppendLine("Overload resolution failed because of ambiguous possible best operators.");
            }
            else
            {
                sb.AppendLine("Overload resolution failed because no operator was applicable.");
            }

            sb.AppendLine("Detailed results:");
            foreach (var result in Results)
            {
                sb.AppendFormat("operator: {0} reason: {1}\n", result.Signature.ToString(), result.Kind.ToString());
            }

            return sb.ToString();
        }

        private int CountKind(OperatorAnalysisResultKind kind)
        {
            int count = 0;
            for (int i = 0, n = this.Results.Count; i < n; i++)
            {
                if (this.Results[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
#endif

        #region "Poolable"

        public static BinaryOperatorOverloadResolutionResult GetInstance()
        {
            return Pool.Allocate();
        }

        public void Free()
        {
            Clear();
            Pool.Free(this);
        }

        public void Clear()
        {
            this.Results.Clear();
        }

        public static readonly ObjectPool<BinaryOperatorOverloadResolutionResult> Pool = CreatePool();

        private static ObjectPool<BinaryOperatorOverloadResolutionResult> CreatePool()
        {
            ObjectPool<BinaryOperatorOverloadResolutionResult> pool = null;
            pool = new ObjectPool<BinaryOperatorOverloadResolutionResult>(() => new BinaryOperatorOverloadResolutionResult(), 10);
            return pool;
        }

        #endregion
    }
}
