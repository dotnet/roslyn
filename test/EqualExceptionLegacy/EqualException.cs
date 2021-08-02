// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace EqualExceptionLegacy
{
    using System.Threading.Tasks;
    using Xunit;

    public class EqualException
    {
        [IdeFact]
        public void EqualsFailure()
        {
            Assert.Equal(0, 1);
        }

        [IdeFact]
        public void EqualsSucceeds()
        {
            Assert.Equal(0, 0);
        }

        [IdeFact]
        public async Task EqualsFailureAsync()
        {
            await Task.Yield();
            Assert.Equal(0, 1);
        }

        [IdeFact]
        public async Task EqualsSucceedsAsync()
        {
            await Task.Yield();
            Assert.Equal(0, 0);
        }
    }
}
