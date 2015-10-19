' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public MustInherit Class AbstractOutlinerTests
        Friend Sub AssertRegion(expected As OutliningSpan, actual As OutliningSpan)
            Assert.Equal(expected.TextSpan.Start, actual.TextSpan.Start)
            Assert.Equal(expected.TextSpan.End, actual.TextSpan.End)
            Assert.Equal(expected.HintSpan.Start, actual.HintSpan.Start)
            Assert.Equal(expected.HintSpan.End, actual.HintSpan.End)
            Assert.Equal(expected.BannerText, actual.BannerText)
            Assert.Equal(expected.AutoCollapse, actual.AutoCollapse)
            Assert.Equal(expected.IsDefaultCollapsed, actual.IsDefaultCollapsed)
        End Sub
    End Class
End Namespace
