// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class XUnitVerifierTests
    {
        [Fact]
        public void TestEmptyMessage()
        {
            var actual = new int[1];
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<EmptyException>(() => verifier.Empty("someCollectionName", actual));
            Assert.Same(actual, exception.Collection);
            Assert.Equal($"'someCollectionName' is not empty{Environment.NewLine}Assert.Empty() Failure{Environment.NewLine}Collection: [0]", exception.Message);
        }

        [Fact]
        public void TestEmptyMessageWithContext()
        {
            var actual = new int[1];
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<EmptyException>(() => verifier.Empty("someCollectionName", actual));
            Assert.Same(actual, exception.Collection);
            Assert.Equal($"Context: Known Context{Environment.NewLine}'someCollectionName' is not empty{Environment.NewLine}Assert.Empty() Failure{Environment.NewLine}Collection: [0]", exception.Message);
        }

        [Fact]
        public void TestEqualMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.Equal(expected, actual));
            Assert.Equal(expected.ToString(), exception.Expected);
            Assert.Equal(actual.ToString(), exception.Actual);
            Assert.Equal($"Assert.Equal() Failure{Environment.NewLine}Expected: 0{Environment.NewLine}Actual:   1", exception.Message);
        }

        [Fact]
        public void TestEqualMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.Equal(expected, actual));
            Assert.Equal(expected.ToString(), exception.Expected);
            Assert.Equal(actual.ToString(), exception.Actual);
            Assert.Equal($"Context: Known Context{Environment.NewLine}{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: 0{Environment.NewLine}Actual:   1", exception.Message);
        }

        [Fact]
        public void TestEqualCustomMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.Equal(expected, actual, "Custom message"));
            Assert.Equal(expected.ToString(), exception.Expected);
            Assert.Equal(actual.ToString(), exception.Actual);
            Assert.Equal($"Custom message{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: 0{Environment.NewLine}Actual:   1", exception.Message);
        }

        [Fact]
        public void TestEqualCustomMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.Equal(expected, actual, "Custom message"));
            Assert.Equal(expected.ToString(), exception.Expected);
            Assert.Equal(actual.ToString(), exception.Actual);
            Assert.Equal($"Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: 0{Environment.NewLine}Actual:   1", exception.Message);
        }

        [Fact]
        public void TestTrueMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.True(false));
            Assert.Equal($"Assert.True() Failure{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestTrueMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.True(false));
            Assert.Equal($"Context: Known Context{Environment.NewLine}{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestTrueCustomMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.True(false, "Custom message"));
            Assert.Equal($"Custom message{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestTrueCustomMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.True(false, "Custom message"));
            Assert.Equal($"Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestFalseMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.False(true));
            Assert.Equal($"Assert.False() Failure{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestFalseMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.False(true));
            Assert.Equal($"Context: Known Context{Environment.NewLine}{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestFalseCustomMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.False(true, "Custom message"));
            Assert.Equal($"Custom message{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestFalseCustomMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.False(true, "Custom message"));
            Assert.Equal($"Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestFailMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.Fail());
            Assert.Equal($"Assert.True() Failure{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestFailMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.Fail());
            Assert.Equal($"Context: Known Context{Environment.NewLine}{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestFailCustomMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.Fail("Custom message"));
            Assert.Equal($"Custom message{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestFailCustomMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<TrueException>(() => verifier.Fail("Custom message"));
            Assert.Equal($"Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}Expected: True{Environment.NewLine}Actual:   False", exception.Message);
        }

        [Fact]
        public void TestLanguageIsSupportedMessage()
        {
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.LanguageIsSupported("NonLanguage"));
            Assert.Equal($"Unsupported Language: 'NonLanguage'{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestLanguageIsSupportedMessageWithContext()
        {
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<FalseException>(() => verifier.LanguageIsSupported("NonLanguage"));
            Assert.Equal($"Context: Known Context{Environment.NewLine}Unsupported Language: 'NonLanguage'{Environment.NewLine}Expected: False{Environment.NewLine}Actual:   True", exception.Message);
        }

        [Fact]
        public void TestNotEmptyMessage()
        {
            var actual = new int[0];
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<NotEmptyException>(() => verifier.NotEmpty("someCollectionName", actual));
            Assert.Equal($"'someCollectionName' is empty{Environment.NewLine}Assert.NotEmpty() Failure", exception.Message);
        }

        [Fact]
        public void TestNotEmptyMessageWithContext()
        {
            var actual = new int[0];
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<NotEmptyException>(() => verifier.NotEmpty("someCollectionName", actual));
            Assert.Equal($"Context: Known Context{Environment.NewLine}'someCollectionName' is empty{Environment.NewLine}Assert.NotEmpty() Failure", exception.Message);
        }

        [Fact]
        public void TestSequenceEqualMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.SequenceEqual(expected, actual));
            Assert.Equal("Int32[] [0]", exception.Expected);
            Assert.Equal("Int32[] [1]", exception.Actual);
            Assert.Equal($"Assert.Equal() Failure{Environment.NewLine}Expected: Int32[] [0]{Environment.NewLine}Actual:   Int32[] [1]", exception.Message);
        }

        [Fact]
        public void TestSequenceEqualMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.SequenceEqual(expected, actual));
            Assert.Equal("Int32[] [0]", exception.Expected);
            Assert.Equal("Int32[] [1]", exception.Actual);
            Assert.Equal($"Context: Known Context{Environment.NewLine}{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: Int32[] [0]{Environment.NewLine}Actual:   Int32[] [1]", exception.Message);
        }

        [Fact]
        public void TestSequenceEqualCustomMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new XUnitVerifier();
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            Assert.Equal("Int32[] [0]", exception.Expected);
            Assert.Equal("Int32[] [1]", exception.Actual);
            Assert.Equal($"Custom message{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: Int32[] [0]{Environment.NewLine}Actual:   Int32[] [1]", exception.Message);
        }

        [Fact]
        public void TestSequenceEqualCustomMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new XUnitVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsAny<EqualException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            Assert.Equal("Int32[] [0]", exception.Expected);
            Assert.Equal("Int32[] [1]", exception.Actual);
            Assert.Equal($"Context: Known Context{Environment.NewLine}Custom message{Environment.NewLine}Assert.Equal() Failure{Environment.NewLine}Expected: Int32[] [0]{Environment.NewLine}Actual:   Int32[] [1]", exception.Message);
        }
    }
}
