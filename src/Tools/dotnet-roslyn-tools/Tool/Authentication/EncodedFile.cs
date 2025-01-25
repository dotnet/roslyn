// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.RoslynTools.Authentication
{
    public static class EncodedFile
    {
        public static int Create(string fileName, JToken jsonContent, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(Constants.RoslynToolsDirectory);
                var textBytes = Encoding.UTF8.GetBytes(jsonContent.ToString());
                var encodedContent = Convert.ToBase64String(textBytes);
                File.WriteAllText(Path.Combine(Constants.RoslynToolsDirectory, fileName), encodedContent);
                return Constants.SuccessCode;
            }
            catch (Exception exc)
            {
                logger.LogError(exc.Message);
                return Constants.ErrorCode;
            }
        }

        public static string Read(string fileName)
        {
            var encodedString = File.ReadAllText(Path.Combine(Constants.RoslynToolsDirectory, fileName));
            var encodedBytes = Convert.FromBase64String(encodedString);
            return Encoding.UTF8.GetString(encodedBytes);
        }
    }
}
