// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using NUnit.Framework;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    [TestFixture]
    public class NUnitVerifierTests
    {
        [Test]
        public void TestEmptyMessage()
        {
            var actual = new int[1];
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.Empty("someCollectionName", actual));
            verifier.EqualOrDiff($"  Expected 'someCollectionName' to be empty, contains '1' elements{Environment.NewLine}  Expected: <empty>{Environment.NewLine}  But was:  < 0 >{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestEmptyMessageWithContext()
        {
            var actual = new int[1];
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.Empty("someCollectionName", actual));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}Expected 'someCollectionName' to be empty, contains '1' elements{Environment.NewLine}  Expected: <empty>{Environment.NewLine}  But was:  < 0 >{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestEqualMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.Equal(expected, actual));
            verifier.EqualOrDiff($"  Expected: 0{Environment.NewLine}  But was:  1{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestEqualMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.Equal(expected, actual));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}{Environment.NewLine}  Expected: 0{Environment.NewLine}  But was:  1{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestEqualCustomMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.Equal(expected, actual, "Custom message"));
            verifier.EqualOrDiff($"  Custom message{Environment.NewLine}  Expected: 0{Environment.NewLine}  But was:  1{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestEqualCustomMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.Equal(expected, actual, "Custom message"));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}  Expected: 0{Environment.NewLine}  But was:  1{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestTrueMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.True(false));
            verifier.EqualOrDiff($"  Expected: True{Environment.NewLine}  But was:  False{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestTrueMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.True(false));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}{Environment.NewLine}  Expected: True{Environment.NewLine}  But was:  False{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestTrueCustomMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.True(false, "Custom message"));
            verifier.EqualOrDiff($"  Custom message{Environment.NewLine}  Expected: True{Environment.NewLine}  But was:  False{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestTrueCustomMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.True(false, "Custom message"));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}  Expected: True{Environment.NewLine}  But was:  False{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFalseMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.False(true));
            verifier.EqualOrDiff($"  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFalseMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.False(true));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}{Environment.NewLine}  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFalseCustomMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.False(true, "Custom message"));
            verifier.EqualOrDiff($"  Custom message{Environment.NewLine}  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFalseCustomMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.False(true, "Custom message"));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFailMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.Fail());
            verifier.EqualOrDiff(string.Empty, exception.Message);
        }

        [Test]
        public void TestFailMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.Fail());
            verifier.EqualOrDiff($"Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestFailCustomMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.Fail("Custom message"));
            verifier.EqualOrDiff($"Custom message", exception.Message);
        }

        [Test]
        public void TestFailCustomMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.Fail("Custom message"));
            verifier.EqualOrDiff($"Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }

        [Test]
        public void TestLanguageIsSupportedMessage()
        {
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.LanguageIsSupported("NonLanguage"));
            verifier.EqualOrDiff($"  Unsupported Language: 'NonLanguage'{Environment.NewLine}  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestLanguageIsSupportedMessageWithContext()
        {
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.LanguageIsSupported("NonLanguage"));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}Unsupported Language: 'NonLanguage'{Environment.NewLine}  Expected: False{Environment.NewLine}  But was:  True{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestNotEmptyMessage()
        {
            var actual = new int[0];
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.NotEmpty("someCollectionName", actual));
            verifier.EqualOrDiff($"  expected 'someCollectionName' to be non-empty, contains{Environment.NewLine}  Expected: not <empty>{Environment.NewLine}  But was:  <empty>{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestNotEmptyMessageWithContext()
        {
            var actual = new int[0];
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.NotEmpty("someCollectionName", actual));
            verifier.EqualOrDiff($"  Context: Known Context{Environment.NewLine}expected 'someCollectionName' to be non-empty, contains{Environment.NewLine}  Expected: not <empty>{Environment.NewLine}  But was:  <empty>{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestSequenceEqualMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.SequenceEqual(expected, actual));
            verifier.EqualOrDiff(string.Empty, exception.Message);
        }

        [Test]
        public void TestSequenceEqualMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.SequenceEqual(expected, actual));
            verifier.EqualOrDiff($"Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [Test]
        public void TestSequenceEqualCustomMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new NUnitVerifier();
            var exception = Assert.Throws<AssertionException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            verifier.EqualOrDiff($"Custom message", exception.Message);
        }

        [Test]
        public void TestSequenceEqualCustomMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new NUnitVerifier().PushContext("Known Context");
            var exception = Assert.Throws<AssertionException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            verifier.EqualOrDiff($"Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }
    }
}
