// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    [TestClass]
    public class MSTestVerifierTests
    {
        [TestMethod]
        public void TestEmptyMessage()
        {
            var actual = new int[1];
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Empty("someCollectionName", actual));
            Assert.AreEqual("Assert.IsFalse failed. expected 'someCollectionName' to be empty, contains '1' elements", exception.Message);
        }

        [TestMethod]
        public void TestEmptyMessageWithContext()
        {
            var actual = new int[1];
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Empty("someCollectionName", actual));
            Assert.AreEqual($"Assert.IsFalse failed. Context: Known Context{Environment.NewLine}expected 'someCollectionName' to be empty, contains '1' elements", exception.Message);
        }

        [TestMethod]
        public void TestEqualMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Equal(expected, actual));
            Assert.AreEqual($"Assert.AreEqual failed. Expected:<0>. Actual:<1>. ", exception.Message);
        }

        [TestMethod]
        public void TestEqualMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Equal(expected, actual));
            Assert.AreEqual($"Assert.AreEqual failed. Expected:<0>. Actual:<1>. Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [TestMethod]
        public void TestEqualCustomMessage()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Equal(expected, actual, "Custom message"));
            Assert.AreEqual($"Assert.AreEqual failed. Expected:<0>. Actual:<1>. Custom message", exception.Message);
        }

        [TestMethod]
        public void TestEqualCustomMessageWithContext()
        {
            var expected = 0;
            var actual = 1;
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Equal(expected, actual, "Custom message"));
            Assert.AreEqual($"Assert.AreEqual failed. Expected:<0>. Actual:<1>. Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }

        [TestMethod]
        public void TestTrueMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.True(false));
            Assert.AreEqual($"Assert.IsTrue failed. ", exception.Message);
        }

        [TestMethod]
        public void TestTrueMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.True(false));
            Assert.AreEqual($"Assert.IsTrue failed. Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [TestMethod]
        public void TestTrueCustomMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.True(false, "Custom message"));
            Assert.AreEqual($"Assert.IsTrue failed. Custom message", exception.Message);
        }

        [TestMethod]
        public void TestTrueCustomMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.True(false, "Custom message"));
            Assert.AreEqual($"Assert.IsTrue failed. Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }

        [TestMethod]
        public void TestFalseMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.False(true));
            Assert.AreEqual($"Assert.IsFalse failed. ", exception.Message);
        }

        [TestMethod]
        public void TestFalseMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.False(true));
            Assert.AreEqual($"Assert.IsFalse failed. Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [TestMethod]
        public void TestFalseCustomMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.False(true, "Custom message"));
            Assert.AreEqual($"Assert.IsFalse failed. Custom message", exception.Message);
        }

        [TestMethod]
        public void TestFalseCustomMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.False(true, "Custom message"));
            Assert.AreEqual($"Assert.IsFalse failed. Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }

        [TestMethod]
        public void TestFailMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Fail());
            Assert.AreEqual($"Assert.Fail failed. ", exception.Message);
        }

        [TestMethod]
        public void TestFailMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Fail());
            Assert.AreEqual($"Assert.Fail failed. Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [TestMethod]
        public void TestFailCustomMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Fail("Custom message"));
            Assert.AreEqual($"Assert.Fail failed. Custom message", exception.Message);
        }

        [TestMethod]
        public void TestFailCustomMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.Fail("Custom message"));
            Assert.AreEqual($"Assert.Fail failed. Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }

        [TestMethod]
        public void TestLanguageIsSupportedMessage()
        {
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.LanguageIsSupported("NonLanguage"));
            Assert.AreEqual($"Assert.IsFalse failed. Unsupported Language: 'NonLanguage'", exception.Message);
        }

        [TestMethod]
        public void TestLanguageIsSupportedMessageWithContext()
        {
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.LanguageIsSupported("NonLanguage"));
            Assert.AreEqual($"Assert.IsFalse failed. Context: Known Context{Environment.NewLine}Unsupported Language: 'NonLanguage'", exception.Message);
        }

        [TestMethod]
        public void TestNotEmptyMessage()
        {
            var actual = new int[0];
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.NotEmpty("someCollectionName", actual));
            Assert.AreEqual($"Assert.IsTrue failed. expected 'someCollectionName' to be non-empty, contains", exception.Message);
        }

        [TestMethod]
        public void TestNotEmptyMessageWithContext()
        {
            var actual = new int[0];
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.NotEmpty("someCollectionName", actual));
            Assert.AreEqual($"Assert.IsTrue failed. Context: Known Context{Environment.NewLine}expected 'someCollectionName' to be non-empty, contains", exception.Message);
        }

        [TestMethod]
        public void TestSequenceEqualMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.SequenceEqual(expected, actual));
            Assert.AreEqual($"Assert.Fail failed. ", exception.Message);
        }

        [TestMethod]
        public void TestSequenceEqualMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.SequenceEqual(expected, actual));
            Assert.AreEqual($"Assert.Fail failed. Context: Known Context{Environment.NewLine}", exception.Message);
        }

        [TestMethod]
        public void TestSequenceEqualCustomMessage()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new MSTestVerifier();
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            Assert.AreEqual($"Assert.Fail failed. Custom message", exception.Message);
        }

        [TestMethod]
        public void TestSequenceEqualCustomMessageWithContext()
        {
            var expected = new int[] { 0 };
            var actual = new int[] { 1 };
            var verifier = new MSTestVerifier().PushContext("Known Context");
            var exception = Assert.ThrowsException<AssertFailedException>(() => verifier.SequenceEqual(expected, actual, message: "Custom message"));
            Assert.AreEqual($"Assert.Fail failed. Context: Known Context{Environment.NewLine}Custom message", exception.Message);
        }
    }
}
