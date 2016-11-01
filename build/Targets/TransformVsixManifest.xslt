<xsl:stylesheet version="1.0"
 xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
 xmlns:vsix="http://schemas.microsoft.com/developer/vsx-schema/2011">

 <xsl:output omit-xml-declaration="yes"/>

    <xsl:template match="node()|@*">
      <xsl:copy>
         <xsl:apply-templates select="node()|@*"/>
      </xsl:copy>
    </xsl:template>

    <xsl:template match="vsix:Prerequisites"/>
</xsl:stylesheet>
