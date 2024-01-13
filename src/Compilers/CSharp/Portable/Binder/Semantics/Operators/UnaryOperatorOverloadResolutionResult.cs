// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UnaryOperatorOverloadResolutionResult
    {
        public readonly ArrayBuilder<UnaryOperatorAnalysisResult> Results;

        public UnaryOperatorOverloadResolutionResult()
        {
            this.Results = new ArrayBuilder<UnaryOperatorAnalysisResult>(10);
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

        public UnaryOperatorAnalysisResult Best
        {
            get
            {
                UnaryOperatorAnalysisResult best = default(UnaryOperatorAnalysisResult);
                foreach (var result in Results)
                {
                    if (result.IsValid)
                    {
                        if (best.IsValid)
                        {
                            // More than one best applicable method
                            return default(UnaryOperatorAnalysisResult);
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

        public static UnaryOperatorOverloadResolutionResult GetInstance()
        {
            return Pool.Allocate();
        }

        public void Free()
        {
            this.Results.Clear();
            Pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        public static readonly ObjectPool<UnaryOperatorOverloadResolutionResult> Pool = CreatePool();

        private static ObjectPool<UnaryOperatorOverloadResolutionResult> CreatePool()
        {
            ObjectPool<UnaryOperatorOverloadResolutionResult> pool = null;
            pool = new ObjectPool<UnaryOperatorOverloadResolutionResult>(() => new UnaryOperatorOverloadResolutionResult(), 10);
            return pool;
        }

        #endregion
    }
}
