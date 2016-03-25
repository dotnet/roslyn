// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class SignatureDescription
    {
        public string FullyQualifiedTypeName { get; set; }
        public string MemberName { get; set; }
        public string ExpectedSignature { get; set; }
    }

    internal class MetadataSignatureUnitTestHelper
    {
        /// <summary>
        /// Uses Reflection to verify that the specified member signatures are present in emitted metadata
        /// </summary>
        /// <param name="appDomainHost">Unit test AppDomain host</param>
        /// <param name="expectedSignatures">Baseline signatures - use the Signature() factory method to create instances of SignatureDescription</param>
        internal static void VerifyMemberSignatures(
            IRuntimeUtility appDomainHost, params SignatureDescription[] expectedSignatures)
        {
            Assert.NotNull(expectedSignatures);
            Assert.NotEmpty(expectedSignatures);

            var succeeded = true;
            var expected = new List<string>();
            var actual = new List<string>();

            foreach (var signature in expectedSignatures)
            {
                List<string> actualSignatures = null;
                var expectedSignature = signature.ExpectedSignature;

                if (!VerifyMemberSignatureHelper(
                    appDomainHost, signature.FullyQualifiedTypeName, signature.MemberName,
                    ref expectedSignature, out actualSignatures))
                {
                    succeeded = false;
                }

                expected.Add(expectedSignature);
                actual.AddRange(actualSignatures);
            }

            if (!succeeded)
            {
                TriggerSignatureMismatchFailure(expected, actual);
            }
        }

        /// <summary>
        /// Uses Reflection to verify that the specified member signature is present in emitted metadata
        /// </summary>
        /// <param name="appDomainHost">Unit test AppDomain host</param>
        /// <param name="fullyQualifiedTypeName">
        /// Fully qualified type name for member
        /// Names must be in format recognized by reflection
        /// e.g. MyType&lt;T&gt;.MyNestedType&lt;T, U&gt; => MyType`1+MyNestedType`2
        /// </param>
        /// <param name="memberName">
        /// Name of member on specified type whose signature needs to be verified
        /// Names must be in format recognized by reflection
        /// e.g. For explicitly implemented member - I1&lt;string&gt;.Method => I1&lt;System.String&gt;.Method
        /// </param>
        /// <param name="expectedSignature">
        /// Baseline string for signature of specified member
        /// Skip this argument to get an error message that shows all available signatures for specified member
        /// This argument is passed by reference and it will be updated with a formatted form of the baseline signature for error reporting purposes
        /// </param>
        /// <param name="actualSignatures">List of found signatures matching member name</param>
        /// <returns>True if a matching member signature was found, false otherwise</returns>
        private static bool VerifyMemberSignatureHelper(
            IRuntimeUtility appDomainHost, string fullyQualifiedTypeName, string memberName,
            ref string expectedSignature, out List<string> actualSignatures)
        {
            Assert.False(string.IsNullOrWhiteSpace(fullyQualifiedTypeName), "'fullyQualifiedTypeName' can't be null or empty");
            Assert.False(string.IsNullOrWhiteSpace(memberName), "'memberName' can't be null or empty");

            var retVal = true; actualSignatures = new List<string>();
            var signatures = appDomainHost.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName);
            var signatureAssertText = "Signature(\"" + fullyQualifiedTypeName + "\", \"" + memberName + "\", \"{0}\"),";

            if (!string.IsNullOrWhiteSpace(expectedSignature))
            {
                expectedSignature = expectedSignature.Replace("\"", "\\\"");
            }
            expectedSignature = string.Format(signatureAssertText, expectedSignature);

            if (signatures.Count > 1)
            {
                var found = false;
                foreach (var signature in signatures)
                {
                    var actualSignature = signature.Replace("\"", "\\\"");
                    actualSignature = string.Format(signatureAssertText, actualSignature);

                    if (actualSignature == expectedSignature)
                    {
                        actualSignatures.Clear();
                        actualSignatures.Add(actualSignature);
                        found = true; break;
                    }
                    else
                    {
                        actualSignatures.Add(actualSignature);
                    }
                }
                if (!found)
                {
                    retVal = false;
                }
            }
            else if (signatures.Count == 1)
            {
                var actualSignature = signatures.First().Replace("\"", "\\\"");
                actualSignature = string.Format(signatureAssertText, actualSignature);
                actualSignatures.Add(actualSignature);

                if (expectedSignature != actualSignature)
                {
                    retVal = false;
                }
            }
            else
            {
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Triggers assert when expected and actual signatures don't match
        /// </summary>
        /// <param name="expectedSignatures">List of baseline signature strings</param>
        /// <param name="actualSignatures">List of actually found signature strings</param>
        private static void TriggerSignatureMismatchFailure(List<string> expectedSignatures, List<string> actualSignatures)
        {
            var expectedText = string.Empty;
            var actualText = string.Empty;
            var distinctSignatures = new HashSet<string>();

            foreach (var signature in expectedSignatures)
            {
                // We need to preserve the order as well as prevent duplicates
                if (!distinctSignatures.Contains(signature))
                {
                    expectedText += "\n\t" + signature;
                    distinctSignatures.Add(signature);
                }
            }

            distinctSignatures.Clear();
            foreach (var signature in actualSignatures)
            {
                // We need to preserve the order as well as prevent duplicates
                if (!distinctSignatures.Contains(signature))
                {
                    actualText += "\n\t" + signature;
                    distinctSignatures.Add(signature);
                }
            }

            expectedText = expectedText.TrimEnd(',');
            actualText = actualText.TrimEnd(',');
            var diffText = DiffUtil.DiffReport(expectedText, actualText);

            Assert.True(false, "\n\nExpected:" + expectedText + "\n\nActual:" + actualText + "\n\nDifferences:\n" + diffText);
        }
    }
}
