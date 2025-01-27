// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.SignatureHelp;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{
    public class FSharpSignatureHelpTriggerReasonTests
    {
        public static IEnumerable<object[]> enumValues()
        {
            foreach (var number in Enum.GetValues(typeof(SignatureHelpTriggerReason)))
            {
                yield return new object[] { number };
            }
        }

        internal static FSharpSignatureHelpTriggerReason GetExpectedTriggerReason(SignatureHelpTriggerReason triggerReason)
        {
            switch (triggerReason)
            {
                case SignatureHelpTriggerReason.InvokeSignatureHelpCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.InvokeSignatureHelpCommand;
                    }

                case SignatureHelpTriggerReason.RetriggerCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.RetriggerCommand;
                    }

                case SignatureHelpTriggerReason.TypeCharCommand:
                    {
                        return FSharpSignatureHelpTriggerReason.TypeCharCommand;
                    }

                default:
                    {
                        throw ExceptionUtilities.UnexpectedValue(triggerReason);
                    }
            }
        }

        [Theory]
        [MemberData(nameof(enumValues))]
        internal void MapsCorrectly(SignatureHelpTriggerReason triggerReason)
        {
            var actual = FSharpSignatureHelpTriggerReasonHelpers.ConvertFrom(triggerReason);
            var expected = GetExpectedTriggerReason(triggerReason);
            Assert.Equal(expected, actual);
        }
    }
}
