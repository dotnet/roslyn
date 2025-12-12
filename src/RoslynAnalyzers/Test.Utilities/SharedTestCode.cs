// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Test.Utilities
{
    public static class SharedCode
    {
        public const string WrongSanitizer = """
            using System;
            using System.Data.SqlClient;
            using System.IO;
            using System.Web;

            public partial class WebForm : System.Web.UI.Page
            {
                protected void Page_Load(object sender, EventArgs e)
                {
                    string input = Request.Form["in"];
                    StringWriter w = new StringWriter();
                    Server.HtmlEncode(input, w);
                    Response.Write("<HTML>" + w.ToString() + "</HTML>");
                    // make sure it is not like any unknown method breaks the taint path, it should warn about sql injection
                    SqlCommand sqlCommand = new SqlCommand(w.ToString());
                }
            }
            """;
    }
}
