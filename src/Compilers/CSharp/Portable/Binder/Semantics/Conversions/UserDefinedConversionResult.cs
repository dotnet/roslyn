﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum UserDefinedConversionResultKind : byte
    {
        NoApplicableOperators,
        NoBestSourceType,
        NoBestTargetType,
        Ambiguous,
        Valid
    }

    internal struct UserDefinedConversionResult
    {
        public readonly ImmutableArray<UserDefinedConversionAnalysis> Results;
        public readonly int Best;
        public readonly UserDefinedConversionResultKind Kind;

        public static UserDefinedConversionResult NoApplicableOperators(ImmutableArray<UserDefinedConversionAnalysis> results)
        {
            return new UserDefinedConversionResult(
                UserDefinedConversionResultKind.NoApplicableOperators,
                results,
                -1);
        }

        public static UserDefinedConversionResult NoBestSourceType(ImmutableArray<UserDefinedConversionAnalysis> results)
        {
            return new UserDefinedConversionResult(
                UserDefinedConversionResultKind.NoBestSourceType,
                results,
                -1);
        }

        public static UserDefinedConversionResult NoBestTargetType(ImmutableArray<UserDefinedConversionAnalysis> results)
        {
            return new UserDefinedConversionResult(
                UserDefinedConversionResultKind.NoBestTargetType,
                results,
                -1);
        }

        public static UserDefinedConversionResult Ambiguous(ImmutableArray<UserDefinedConversionAnalysis> results)
        {
            return new UserDefinedConversionResult(
                UserDefinedConversionResultKind.Ambiguous,
                results,
                -1);
        }

        public static UserDefinedConversionResult Valid(ImmutableArray<UserDefinedConversionAnalysis> results, int best)
        {
            return new UserDefinedConversionResult(
                UserDefinedConversionResultKind.Valid,
                results,
                best);
        }

        private UserDefinedConversionResult(
            UserDefinedConversionResultKind kind,
            ImmutableArray<UserDefinedConversionAnalysis> results,
            int best)
        {
            this.Kind = kind;
            this.Results = results;
            this.Best = best;
        }

#if DEBUG
        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("User defined conversion analysis results:");
            sb.AppendFormat("Summary: {0}\n", Kind);
            for (int i = 0; i < Results.Length; ++i)
            {
                sb.AppendFormat("{0} Conversion: {1} Result: {2}\n",
                    i == Best ? "*" : " ",
                    Results[i].Operator,
                    Results[i].Kind);
            }

            return sb.ToString();
        }

#endif
    }
}

