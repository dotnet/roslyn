// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Options
{
    public class CodeStyleOptionTests
    {
        [Fact]
        public void WithValue_Same_Bool()
        {
            Assert.Same(CodeStyleOption2.FalseWithSilentEnforcement.WithValue(true), CodeStyleOption2.TrueWithSilentEnforcement);
            Assert.Same(CodeStyleOption2.TrueWithSilentEnforcement.WithValue(false), CodeStyleOption2.FalseWithSilentEnforcement);

            Assert.Same(CodeStyleOption2.FalseWithSuggestionEnforcement.WithValue(true), CodeStyleOption2.TrueWithSuggestionEnforcement);
            Assert.Same(CodeStyleOption2.TrueWithSuggestionEnforcement.WithValue(false), CodeStyleOption2.FalseWithSuggestionEnforcement);
        }

        [Fact]
        public void WithValue_Same_Int()
        {
            var style = new CodeStyleOption2<int>(1, NotificationOption2.Error);
            Assert.Same(style.WithValue(1), style);
        }

        [Fact]
        public void WithValue_Same_Enum()
        {
            var style = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Error);
            Assert.Equal(style.WithValue(ExpressionBodyPreference.WhenOnSingleLine), style);
        }

        [Fact]
        public void WithValue_Same_String()
        {
            var style = new CodeStyleOption2<string>("abc", NotificationOption2.Error);
            Assert.Equal(style.WithValue("abc"), style);
        }

        [Fact]
        public void WithValue_Equal_Bool()
        {
            Assert.Equal(
                new CodeStyleOption2<bool>(true, NotificationOption2.Error).WithValue(false),
                new CodeStyleOption2<bool>(false, NotificationOption2.Error));
        }

        [Fact]
        public void WithValue_Equal_Int()
        {
            Assert.Equal(
                new CodeStyleOption2<int>(1, NotificationOption2.Error).WithValue(2),
                new CodeStyleOption2<int>(2, NotificationOption2.Error));
        }

        [Fact]
        public void WithValue_Equal_Enum()
        {
            Assert.Equal(
                new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.Error).WithValue(ExpressionBodyPreference.WhenPossible),
                new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Error));
        }

        [Fact]
        public void WithValue_Equal_String()
        {
            Assert.Equal(
                new CodeStyleOption2<string>("abc", NotificationOption2.Error).WithValue("xyz"),
                new CodeStyleOption2<string>("xyz", NotificationOption2.Error));
        }

        /// <summary>
        /// Verify that bool value can migrate to enum value.
        /// </summary>
        [Fact]
        public void ToFromXElement_BoolToEnum()
        {
            var option = new CodeStyleOption2<bool>(false, NotificationOption2.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption2<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.Never, deserialized.Value);

            option = new CodeStyleOption2<bool>(true, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<ExpressionBodyPreference>.FromXElement(serialized);

            Assert.Equal(ExpressionBodyPreference.WhenPossible, deserialized.Value);
        }

        /// <summary>
        /// Verify that enum value can migrate to bool value.
        /// </summary>
        [Fact]
        public void ToFromXElement_EnumToBool()
        {
            var option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.Never, NotificationOption2.Silent);
            var serialized = option.ToXElement();
            var deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.False(deserialized.Value);

            option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenPossible, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.True(deserialized.Value);

            // This new values can't actually translate back to a bool.  So we'll just get the default
            // value for this option.
            option = new CodeStyleOption2<ExpressionBodyPreference>(ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.Silent);
            serialized = option.ToXElement();
            deserialized = CodeStyleOption2<bool>.FromXElement(serialized);

            Assert.Equal(default, deserialized.Value);
        }
    }
}
