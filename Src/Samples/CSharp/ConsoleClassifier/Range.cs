// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

public class Range
{
    public ClassifiedSpan ClassifiedSpan { get; private set; }
    public string Text { get; private set; }

    public Range(string classification, TextSpan span, SourceText text) :
        this(classification, span, text.GetSubText(span).ToString())
    {
    }

    public Range(string classification, TextSpan span, string text) : 
        this(new ClassifiedSpan(classification, span), text)
    {
    }

    public Range(ClassifiedSpan classifiedSpan, string text)
    {
        this.ClassifiedSpan = classifiedSpan;
        this.Text = text;
    }

    public string ClassificationType
    {
        get { return ClassifiedSpan.ClassificationType; }
    }

    public TextSpan TextSpan
    {
        get { return ClassifiedSpan.TextSpan; }
    }
}