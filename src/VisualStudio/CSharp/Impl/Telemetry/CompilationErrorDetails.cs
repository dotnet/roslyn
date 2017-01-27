// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal struct CompilationErrorDetails : IEquatable<CompilationErrorDetails>
    {
        public CompilationErrorDetails(string errorId, string fileName, string methodName, string unresolvedMemberName,
            string leftExpressionDocId, string[] leftExpressionBaseTypeDocIds, string[] genericArguments, string[] argumentTypes)
        {
            ErrorId = errorId;
            Filename = fileName;
            MethodName = methodName;
            UnresolvedMemberName = unresolvedMemberName;
            LeftExpressionDocId = leftExpressionDocId;
            LeftExpressionBaseTypeDocIds = leftExpressionBaseTypeDocIds;
            GenericArguments = genericArguments;
            ArgumentTypes = argumentTypes;
            _hashCode = null;
        }

        public readonly string Filename;
        public readonly string ErrorId;
        public readonly string MethodName;
        public readonly string UnresolvedMemberName;
        public readonly string LeftExpressionDocId;
        public readonly string[] LeftExpressionBaseTypeDocIds;
        public readonly string[] GenericArguments;
        public readonly string[] ArgumentTypes;

        private int? _hashCode;

        public override int GetHashCode()
        {
            if (_hashCode.HasValue)
            {
                return _hashCode.Value;
            }

            _hashCode = Hash.Combine(Filename,
                       Hash.Combine(ErrorId,
                       Hash.Combine(MethodName,
                       Hash.Combine(UnresolvedMemberName,
                       Hash.Combine(LeftExpressionDocId,
                       Hash.CombineValues(LeftExpressionBaseTypeDocIds,
                       Hash.CombineValues(GenericArguments,
                       Hash.CombineValues(ArgumentTypes, 0))))))));

            return _hashCode.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj is CompilationErrorDetails)
            {
                return Equals((CompilationErrorDetails)obj);
            }

            return base.Equals(obj);
        }

        public bool Equals(CompilationErrorDetails other)
        {
            // Avoid string comparisons unless the hashcodes match
            if (GetHashCode() != other.GetHashCode())
            {
                return false;
            }

            return Filename == other.Filename &&
                ErrorId == other.ErrorId &&
                MethodName == other.MethodName &&
                UnresolvedMemberName == other.UnresolvedMemberName &&
                LeftExpressionDocId == other.LeftExpressionDocId &&
                SameStringArray(LeftExpressionBaseTypeDocIds, other.LeftExpressionBaseTypeDocIds) &&
                SameStringArray(GenericArguments, other.GenericArguments) &&
                SameStringArray(ArgumentTypes, other.ArgumentTypes);
        }

        private static bool SameStringArray(string[] stringArray1, string[] stringArray2)
        {
            if (stringArray1 == null && stringArray2 == null)
            {
                return true;
            }
            else if (stringArray1 == null || stringArray2 == null)
            {
                return false;
            }

            if (stringArray1.Length != stringArray2.Length)
            {
                return false;
            }

            for (int i = 0; i < stringArray1.Length; i++)
            {
                if (stringArray1[i] != stringArray2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
