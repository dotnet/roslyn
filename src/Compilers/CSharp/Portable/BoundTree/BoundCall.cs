// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCall
    {
        public bool IsErroneousNode => ResultKind is not LookupResultKind.Viable;

        private partial void Validate()
        {
            Debug.Assert(ResultKind is not LookupResultKind.MemberGroup);
            Debug.Assert(ResultKind is not LookupResultKind.StaticInstanceMismatch);
            Debug.Assert(ResultKind is LookupResultKind.Viable || HasErrors);

            /* Tracking issue: https://github.com/dotnet/roslyn/issues/79426
            Debug.Assert(ResultKind is LookupResultKind.Viable ||
                         new StackTrace(fNeedFileInfo: false).GetFrame(2)?.GetMethod() switch
                         {
                             { Name: nameof(ErrorCall), DeclaringType: { } declaringType } => declaringType == typeof(BoundCall),
                             { Name: nameof(Update), DeclaringType: { } declaringType } => declaringType == typeof(BoundCall),
                             _ => false
                         });
            */
        }
    }
}
