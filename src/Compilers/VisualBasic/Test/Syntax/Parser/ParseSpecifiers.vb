' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

<CLSCompliant(False)>
Public Class ParseSpecifiers

    <Fact>
    Public Sub ParseSpecifiersOnClass()
        ParseAndVerify(<![CDATA[
                public class c1
                End class

                private class c2
                end class

                protected class c3
                end class

                friend class c4
                end class

                public mustinherit class c5
                end class

                public overridable class c6
                end class

                public shared  class c7
                end class

                public notinheritable class c8
                end class
            ]]>)
    End Sub

End Class
