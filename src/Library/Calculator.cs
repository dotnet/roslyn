// Copyright (c) COMPANY-PLACEHOLDER. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Library
{
    using System;

    /// <summary>
    /// My first class.
    /// </summary>
    public static class Calculator
    {
        /// <summary>
        /// Adds two integers.
        /// </summary>
        /// <param name="a">The first integer.</param>
        /// <param name="b">The second integer.</param>
        /// <returns>The sum of the two integers.</returns>
        public static int Add(int a, int b) => a + b;

        /// <summary>
        /// Subtracts one integer from another.
        /// </summary>
        /// <param name="a">The original integer.</param>
        /// <param name="b">The integer to subtract.</param>
        /// <returns>The difference between the two integers.</returns>
        public static int Subtract(int a, int b) => a - b;
    }
}
