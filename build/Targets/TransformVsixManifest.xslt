<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:vsix="http://schemas.microsoft.com/developer/vsx-schema/2011">

 <xsl:output omit-xml-declaration="yes"/>

    <!-- Match every node and copy it to the output -->
    <xsl:template match="node()|@*">
      <xsl:copy>
         <xsl:apply-templates select="node()|@*"/>
      </xsl:copy>
    </xsl:template>

    <!-- Now override that to do nothing if it matches our element, thus
         omitting it from the output -->
    <xsl:template match="vsix:Prerequisites"/>
</xsl:stylesheet>
